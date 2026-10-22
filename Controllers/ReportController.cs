using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Data;
using HelloDoctor.Care.Models;
using System.Globalization;

namespace HelloDoctor.Care.Controllers
{
    /// <summary>
    /// Doctor-only Patient Reports:
    ///   - Patient visits list for the logged-in doctor (filterable by date, search, report status + printable)
    ///   - Medical visit reports (diagnosis / prescription / advice)
    ///   - Uploaded report files (PDF / images)
    /// </summary>
    [Authorize]
    public class ReportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        // Allowed upload types and max size (20 MB per file)
        private static readonly string[] AllowedExtensions = { ".pdf", ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 20 * 1024 * 1024;

        private sealed class DoctorSession
        {
            public int UserId { get; init; }       // tbl_adminuser.userid (stored in tbl_appointment.drid)
            public int DoctorId { get; init; }     // tbl_doctor.drid (stored in tbl_patientreport.drid)
            public string DoctorName { get; init; } = "Doctor";
            public int? DepId { get; init; }
        }

        public ReportController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int CurrentUserId()
        {
            return Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
        }

        private async Task<(DoctorSession? session, IActionResult? redirect)> RequireDoctorAsync()
        {
            int userId = CurrentUserId();
            if (userId == 0)
                return (null, RedirectToAction("Login", "Auth"));

            var user = await _context.AdminUsers.FindAsync(userId);
            if (user == null)
                return (null, RedirectToAction("Login", "Auth"));

            // Patient Report and Medical Reports are available only on the Doctor portal (roles 2 & 3)
            if ((user.Roleid != 2 && user.Roleid != 3) || !user.Drid.HasValue)
                return (null, RedirectToAction("Index", "Admin"));

            var doc = await _context.Doctors.FindAsync(user.Drid.Value);
            if (doc == null)
                return (null, RedirectToAction("Index", "Admin"));

            return (new DoctorSession
            {
                UserId = user.Userid,
                DoctorId = doc.Drid,
                DoctorName = string.IsNullOrWhiteSpace(doc.Drname) ? "Doctor" : doc.Drname,
                DepId = doc.Depid
            }, null);
        }

        private static DateTime TodayIst()
        {
            TimeZoneInfo ist;
            try { ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
            catch (TimeZoneNotFoundException) { ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);
        }

        private static string NowStamp()
        {
            return TodayIst().ToString("dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
        }

        private static bool TryParseFilterDate(string? date, out DateTime parsedDate)
        {
            parsedDate = TodayIst();
            if (string.IsNullOrWhiteSpace(date))
                return false;

            string trimmed = date.Trim();
            if (string.Equals(trimmed, "all", StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.Equals(trimmed, "today", StringComparison.OrdinalIgnoreCase))
            {
                parsedDate = TodayIst();
                return true;
            }

            string[] formats = { "yyyy-MM-dd", "dd-MMM-yyyy", "d-MMM-yyyy", "dd-MM-yyyy", "d-MM-yyyy" };
            if (DateTime.TryParseExact(trimmed, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate))
                return true;

            if (DateTime.TryParseExact(trimmed, formats, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsedDate))
                return true;

            return DateTime.TryParse(trimmed, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
                || DateTime.TryParse(trimmed, out parsedDate);
        }

        private static List<string> DateVariants(DateTime d)
        {
            return new[]
            {
                d.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture),
                d.ToString("d-MMM-yyyy", CultureInfo.InvariantCulture),
                d.ToString("dd-MMM-yyyy"),
                d.ToString("d-MMM-yyyy"),
                d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            }
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct()
            .ToList();
        }

        private static string NormalizeVisitDate(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
                return TodayIst().ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            if (TryParseFilterDate(rawDate, out DateTime parsed))
                return parsed.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);

            return rawDate.Trim();
        }

        private static string ToIsoDate(string? storedDate)
        {
            if (!string.IsNullOrWhiteSpace(storedDate) && TryParseFilterDate(storedDate, out DateTime parsed))
                return parsed.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return "";
        }

        private async Task<List<PatientVisitRow>> QueryDoctorVisitsAsync(
            DoctorSession docSession,
            bool hasDateFilter,
            DateTime selectedDate,
            string? search,
            string normalizedStatus)
        {
            // Note: tbl_appointment.drid stores tbl_adminuser.userid (docSession.UserId)
            var query = from a in _context.Appointments
                        join csd in _context.ConsultationSlotDetails on a.Slotdtid equals csd.Slotdtid into slotJoin
                        from csd in slotJoin.DefaultIfEmpty()
                        where a.Actv == true && a.Drid == docSession.UserId
                        select new { a, csd };

            if (hasDateFilter)
            {
                var dateMatches = DateVariants(selectedDate);
                query = query.Where(x => x.a.Aptmtdate != null && dateMatches.Contains(x.a.Aptmtdate));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(x =>
                    (x.a.Pname != null && x.a.Pname.Contains(s)) ||
                    (x.a.Mobnum != null && x.a.Mobnum.Contains(s)) ||
                    (x.a.Addr != null && x.a.Addr.Contains(s)));
            }

            var orderedQuery = hasDateFilter
                ? query.OrderBy(x => x.csd != null && x.csd.Token != null ? x.csd.Token.Value : 9999)
                       .ThenByDescending(x => x.a.Aptmtid)
                : query.OrderByDescending(x => x.a.Aptmtid);

            var visits = await orderedQuery.ToListAsync();

            var aptmtIds = visits.Select(x => x.a.Aptmtid).ToList();
            var reports = aptmtIds.Count > 0
                ? await _context.PatientReports
                    .Where(r => r.Drid == docSession.DoctorId && r.Aptmtid != null && aptmtIds.Contains(r.Aptmtid.Value))
                    .ToListAsync()
                : new List<PatientReport>();

            var reportsByAppointment = reports
                .GroupBy(r => r.Aptmtid!.Value)
                .ToDictionary(g => g.Key, g => g.ToList());

            var rows = visits.Select(x => new PatientVisitRow
            {
                Aptmtid = x.a.Aptmtid,
                PatientName = x.a.Pname,
                Mobile = x.a.Mobnum,
                Age = x.a.Age != null ? x.a.Age.ToString() : null,
                Address = x.a.Addr,
                Drid = docSession.DoctorId,
                DoctorName = docSession.DoctorName,
                VisitDate = x.a.Aptmtdate,
                SlotTime = x.csd != null && !string.IsNullOrEmpty(x.csd.Frmtymdt) ? x.csd.Frmtymdt : "-",
                Token = x.csd != null && x.csd.Token != null ? "#" + x.csd.Token : "-",
                Payment = x.a.Payment == true,
                Reports = reportsByAppointment.ContainsKey(x.a.Aptmtid)
                    ? reportsByAppointment[x.a.Aptmtid]
                    : new List<PatientReport>()
            }).ToList();

            if (normalizedStatus == "with")
                rows = rows.Where(r => r.ReportCount > 0).ToList();
            else if (normalizedStatus == "pending")
                rows = rows.Where(r => r.ReportCount == 0).ToList();

            return rows;
        }

        // ------------------------------------------------------------
        // REPORT 1: patient visits list for the logged-in doctor
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index(string? date, string? search, string? status)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            bool hasDateFilter = TryParseFilterDate(date, out DateTime selectedDate);
            string normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
            if (normalizedStatus != "with" && normalizedStatus != "pending")
                normalizedStatus = "all";

            var rows = await QueryDoctorVisitsAsync(docSession!, hasDateFilter, selectedDate, search, normalizedStatus);

            ViewBag.VisitRows = rows;
            ViewBag.TotalPatients = rows.Count;
            ViewBag.WithReports = rows.Count(r => r.ReportCount > 0);
            ViewBag.DoctorName = docSession!.DoctorName;
            ViewBag.AllDates = !hasDateFilter;
            ViewBag.SelectedDate = hasDateFilter ? selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
            ViewBag.TodayDate = TodayIst().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            ViewBag.DisplayDate = hasDateFilter ? selectedDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) : "All Dates";
            ViewBag.Search = search?.Trim() ?? "";
            ViewBag.Status = normalizedStatus;

            return View();
        }

        // Printable version of the doctor's patient visits report
        [HttpGet]
        public async Task<IActionResult> PrintPatients(string? date, string? search, string? status)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            bool hasDateFilter = TryParseFilterDate(date, out DateTime selectedDate);
            string normalizedStatus = (status ?? "all").Trim().ToLowerInvariant();
            if (normalizedStatus != "with" && normalizedStatus != "pending")
                normalizedStatus = "all";

            var rows = await QueryDoctorVisitsAsync(docSession!, hasDateFilter, selectedDate, search, normalizedStatus);

            ViewBag.VisitRows = rows;
            ViewBag.DoctorName = docSession!.DoctorName;
            ViewBag.DisplayDate = hasDateFilter ? selectedDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) : "All Dates";

            return View();
        }

        // ------------------------------------------------------------
        // REPORT 2 (detail): create a medical report for a patient visit
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Add(int? aptmtid)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            ViewBag.DoctorId = docSession!.DoctorId;
            ViewBag.DoctorName = docSession.DoctorName;

            string visitDateIso = TodayIst().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            if (aptmtid.HasValue)
            {
                var apt = await _context.Appointments
                    .FirstOrDefaultAsync(a => a.Aptmtid == aptmtid.Value && a.Drid == docSession.UserId);
                if (apt == null) return RedirectToAction("Index");

                ViewBag.Appointment = apt;
                string parsedIso = ToIsoDate(apt.Aptmtdate);
                if (!string.IsNullOrEmpty(parsedIso))
                    visitDateIso = parsedIso;
            }

            ViewBag.PrefillVisitDateIso = visitDateIso;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(int? aptmtid, string Pname, string? Mobnum, string? Age, string? Addr,
                                             int? Drid, string? Visitdate, string? Diagnosis, string? Prescription,
                                             string? Advice, List<IFormFile>? Files)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            if (string.IsNullOrWhiteSpace(Pname))
            {
                TempData["ErrorMsg"] = "Patient name is required.";
                return RedirectToAction("Add", new { aptmtid });
            }

            int? validAptmtId = null;
            if (aptmtid.HasValue)
            {
                bool ownsAppointment = await _context.Appointments
                    .AnyAsync(a => a.Aptmtid == aptmtid.Value && a.Drid == docSession!.UserId);
                if (ownsAppointment)
                    validAptmtId = aptmtid.Value;
            }

            var report = new PatientReport
            {
                Aptmtid = validAptmtId,
                Pname = Pname.Trim(),
                Mobnum = Mobnum?.Trim(),
                Age = Age?.Trim(),
                Addr = Addr?.Trim(),
                Drid = docSession!.DoctorId,
                Depid = docSession.DepId,
                Visitdate = NormalizeVisitDate(Visitdate),
                Diagnosis = Diagnosis?.Trim(),
                Prescription = Prescription?.Trim(),
                Advice = Advice?.Trim(),
                Createddate = NowStamp(),
                Updte = NowStamp()
            };

            _context.PatientReports.Add(report);
            await _context.SaveChangesAsync();

            await SaveFiles(report.Rptid, Files);

            TempData["SuccessMsg"] = "Patient report saved successfully!";
            return RedirectToAction("Details", new { id = report.Rptid });
        }

        // ------------------------------------------------------------
        // View / manage one report (with its uploaded files)
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var report = await _context.PatientReports
                .Include(r => r.Files)
                .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(r => r.Rptid == id && r.Drid == docSession!.DoctorId);

            if (report == null) return RedirectToAction("MedicalReports");
            return View(report);
        }

        // Upload more files to an existing report
        [HttpPost]
        public async Task<IActionResult> UploadFiles(int id, List<IFormFile>? Files)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var report = await _context.PatientReports
                .FirstOrDefaultAsync(r => r.Rptid == id && r.Drid == docSession!.DoctorId);
            if (report == null) return RedirectToAction("MedicalReports");

            int saved = await SaveFiles(id, Files);
            TempData[saved > 0 ? "SuccessMsg" : "ErrorMsg"] =
                saved > 0 ? $"{saved} file(s) uploaded successfully!" : "No valid file was uploaded (allowed: PDF, JPG, PNG, GIF, WEBP; max 20 MB).";

            report.Updte = NowStamp();
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId, int returnId)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var file = await _context.PatientReportFiles
                .Include(f => f.Report)
                .FirstOrDefaultAsync(f => f.Fileid == fileId && f.Report != null && f.Report.Drid == docSession!.DoctorId);
            if (file != null)
            {
                TryDeletePhysicalFile(file.StoredName);
                _context.PatientReportFiles.Remove(file);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "File deleted.";
            }
            return RedirectToAction("Details", new { id = returnId });
        }

        // ------------------------------------------------------------
        // Edit the written part of a report
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var report = await _context.PatientReports
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Rptid == id && r.Drid == docSession!.DoctorId);

            if (report == null) return RedirectToAction("MedicalReports");

            ViewBag.VisitDateIso = ToIsoDate(report.Visitdate);
            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string Pname, string? Mobnum, string? Age, string? Addr,
                                              string? Visitdate, string? Diagnosis, string? Prescription, string? Advice)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var report = await _context.PatientReports
                .FirstOrDefaultAsync(r => r.Rptid == id && r.Drid == docSession!.DoctorId);
            if (report == null) return RedirectToAction("MedicalReports");

            report.Pname = string.IsNullOrWhiteSpace(Pname) ? report.Pname : Pname.Trim();
            report.Mobnum = Mobnum?.Trim();
            report.Age = Age?.Trim();
            report.Addr = Addr?.Trim();
            report.Visitdate = string.IsNullOrWhiteSpace(Visitdate) ? report.Visitdate : NormalizeVisitDate(Visitdate);
            report.Diagnosis = Diagnosis?.Trim();
            report.Prescription = Prescription?.Trim();
            report.Advice = Advice?.Trim();
            report.Updte = NowStamp();

            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Report updated successfully!";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var report = await _context.PatientReports
                .Include(r => r.Files)
                .FirstOrDefaultAsync(r => r.Rptid == id && r.Drid == docSession!.DoctorId);
            if (report != null)
            {
                foreach (var f in report.Files.ToList())
                    TryDeletePhysicalFile(f.StoredName);

                _context.PatientReports.Remove(report);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Report deleted.";
            }
            return RedirectToAction("MedicalReports");
        }

        // ------------------------------------------------------------
        // REPORT 2 (list): medical reports for the logged-in doctor
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> MedicalReports(string? search, string? date)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            bool hasDateFilter = TryParseFilterDate(date, out DateTime selectedDate);

            var query = _context.PatientReports
                .Include(r => r.Files)
                .Include(r => r.Doctor)
                .Where(r => r.Drid == docSession!.DoctorId);

            if (hasDateFilter)
            {
                var dateMatches = DateVariants(selectedDate);
                query = query.Where(r => r.Visitdate != null && dateMatches.Contains(r.Visitdate));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(r =>
                    (r.Pname != null && r.Pname.Contains(s)) ||
                    (r.Mobnum != null && r.Mobnum.Contains(s)) ||
                    (r.Diagnosis != null && r.Diagnosis.Contains(s)));
            }

            ViewBag.Reports = await query.OrderByDescending(r => r.Rptid).ToListAsync();
            ViewBag.DoctorName = docSession!.DoctorName;
            ViewBag.Search = search?.Trim() ?? "";
            ViewBag.SelectedDate = hasDateFilter ? selectedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "";
            ViewBag.TodayDate = TodayIst().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            return View();
        }

        // Printable medical report (give this to the doctor / patient)
        [HttpGet]
        public async Task<IActionResult> PrintReport(int id)
        {
            var (docSession, redirect) = await RequireDoctorAsync();
            if (redirect != null) return redirect;

            var report = await _context.PatientReports
                .Include(r => r.Files)
                .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(r => r.Rptid == id && r.Drid == docSession!.DoctorId);

            if (report == null) return RedirectToAction("MedicalReports");
            return View(report);
        }

        // ------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------
        private async Task<int> SaveFiles(int reportId, List<IFormFile>? files)
        {
            if (files == null || files.Count == 0) return 0;

            string uploadsFolder = Path.Combine(_env.WebRootPath, "uploads", "reports");
            Directory.CreateDirectory(uploadsFolder);

            int saved = 0;
            foreach (var file in files)
            {
                if (file == null || file.Length == 0) continue;

                string ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext)) continue;
                if (file.Length > MaxFileSize) continue;

                string storedName = Guid.NewGuid().ToString() + ext;
                string filePath = Path.Combine(uploadsFolder, storedName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                _context.PatientReportFiles.Add(new PatientReportFile
                {
                    Rptid = reportId,
                    FileName = Path.GetFileName(file.FileName),
                    StoredName = storedName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedDate = NowStamp()
                });
                saved++;
            }

            if (saved > 0) await _context.SaveChangesAsync();
            return saved;
        }

        private void TryDeletePhysicalFile(string? storedName)
        {
            if (string.IsNullOrWhiteSpace(storedName)) return;
            try
            {
                string path = Path.Combine(_env.WebRootPath, "uploads", "reports", storedName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { /* file already gone - ignore */ }
        }
    }
}
