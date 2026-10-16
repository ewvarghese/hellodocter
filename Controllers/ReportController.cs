using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Data;
using HelloDoctor.Care.Models;
using System.Globalization;

namespace HelloDoctor.Care.Controllers
{
    /// <summary>
    /// Patient Reports:
    ///   - Patients-per-doctor report (list + filter + print)
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

        public ReportController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        private int CurrentUserId()
        {
            return Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
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
            return TodayIst().ToString("dd-MM-yyyy hh:mm:ss tt");
        }

        // ------------------------------------------------------------
        // REPORT 1: patients-per-doctor list (filterable, printable)
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Index(int? doctorId, string? date)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var user = await _context.AdminUsers.FindAsync(CurrentUserId());

            // Doctors always see only their own patients
            if (user != null && user.Drid.HasValue)
            {
                doctorId = user.Drid.Value;
                ViewBag.IsDoctorUser = true;
            }

            // date: "yyyy-MM-dd" | "all" | empty (= today)
            bool allDates = string.Equals(date, "all", StringComparison.OrdinalIgnoreCase);
            DateTime selectedDate = TodayIst();
            if (!allDates && !string.IsNullOrEmpty(date))
                DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedDate);

            string aptmtdate = selectedDate.ToString("dd-MMM-yyyy");
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");
            ViewBag.DisplayDate = aptmtdate;
            ViewBag.AllDates = allDates;
            ViewBag.DoctorId = doctorId ?? 0;

            var query = from a in _context.Appointments
                        join d in _context.Doctors on a.Drid equals d.Drid
                        join csd in _context.ConsultationSlotDetails on a.Slotdtid equals csd.Slotdtid into slotJoin
                        from csd in slotJoin.DefaultIfEmpty()
                        where a.Actv == true
                        select new { a, d, csd };

            if (doctorId.HasValue && doctorId.Value > 0)
                query = query.Where(x => x.a.Drid == doctorId.Value);

            if (!allDates)
                query = query.Where(x => x.a.Aptmtdate == aptmtdate);

            var visits = await query
                .OrderBy(x => x.csd != null ? x.csd.Token : 9999)
                .ThenByDescending(x => x.a.Aptmtid)
                .ToListAsync();

            // Existing reports for these visits (to show status + links)
            var aptmtIds = visits.Select(x => x.a.Aptmtid).ToList();
            var reports = await _context.PatientReports
                .Where(r => r.Aptmtid != null && aptmtIds.Contains(r.Aptmtid.Value))
                .ToListAsync();
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
                Drid = x.a.Drid,
                DoctorName = x.d.Drname,
                VisitDate = x.a.Aptmtdate,
                SlotTime = x.csd != null ? x.csd.Frmtymdt : "-",
                Token = x.csd != null && x.csd.Token != null ? "#" + x.csd.Token : "-",
                Payment = x.a.Payment == true,
                Reports = reportsByAppointment.ContainsKey(x.a.Aptmtid)
                    ? reportsByAppointment[x.a.Aptmtid]
                    : new List<PatientReport>()
            }).ToList();

            ViewBag.VisitRows = rows;
            ViewBag.TotalPatients = rows.Count;
            ViewBag.WithReports = rows.Count(r => r.ReportCount > 0);

            ViewBag.Doctors = await _context.Doctors
                .Where(d => d.Actv == true)
                .OrderBy(d => d.Drname)
                .ToListAsync();
            ViewBag.DoctorName = doctorId.HasValue && doctorId.Value > 0
                ? await _context.Doctors.Where(d => d.Drid == doctorId.Value).Select(d => d.Drname).FirstOrDefaultAsync()
                : "All Doctors";

            return View();
        }

        // Printable version of the patients-per-doctor report
        [HttpGet]
        public async Task<IActionResult> PrintPatients(int? doctorId, string? date)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var user = await _context.AdminUsers.FindAsync(CurrentUserId());
            if (user != null && user.Drid.HasValue) doctorId = user.Drid.Value;

            bool allDates = string.Equals(date, "all", StringComparison.OrdinalIgnoreCase);
            DateTime selectedDate = TodayIst();
            if (!allDates && !string.IsNullOrEmpty(date))
                DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedDate);

            string aptmtdate = selectedDate.ToString("dd-MMM-yyyy");

            var query = from a in _context.Appointments
                        join d in _context.Doctors on a.Drid equals d.Drid
                        join csd in _context.ConsultationSlotDetails on a.Slotdtid equals csd.Slotdtid into slotJoin
                        from csd in slotJoin.DefaultIfEmpty()
                        where a.Actv == true
                        select new { a, d, csd };

            if (doctorId.HasValue && doctorId.Value > 0)
                query = query.Where(x => x.a.Drid == doctorId.Value);
            if (!allDates)
                query = query.Where(x => x.a.Aptmtdate == aptmtdate);

            var visits = await query
                .OrderBy(x => x.csd != null ? x.csd.Token : 9999)
                .ThenByDescending(x => x.a.Aptmtid)
                .ToListAsync();

            ViewBag.VisitRows = visits.Select(x => new PatientVisitRow
            {
                Aptmtid = x.a.Aptmtid,
                PatientName = x.a.Pname,
                Mobile = x.a.Mobnum,
                Age = x.a.Age != null ? x.a.Age.ToString() : null,
                Address = x.a.Addr,
                Drid = x.a.Drid,
                DoctorName = x.d.Drname,
                VisitDate = x.a.Aptmtdate,
                SlotTime = x.csd != null ? x.csd.Frmtymdt : "-",
                Token = x.csd != null && x.csd.Token != null ? "#" + x.csd.Token : "-",
                Payment = x.a.Payment == true
            }).ToList();

            ViewBag.DoctorName = doctorId.HasValue && doctorId.Value > 0
                ? await _context.Doctors.Where(d => d.Drid == doctorId.Value).Select(d => d.Drname).FirstOrDefaultAsync()
                : "All Doctors";
            ViewBag.DisplayDate = allDates ? "All Dates" : aptmtdate;

            return View();
        }

        // ------------------------------------------------------------
        // REPORT 2 (detail): create a medical report for a patient visit
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> Add(int? aptmtid)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            ViewBag.Doctors = await _context.Doctors
                .Where(d => d.Actv == true)
                .OrderBy(d => d.Drname)
                .ToListAsync();

            if (aptmtid.HasValue)
            {
                var apt = await _context.Appointments.FindAsync(aptmtid.Value);
                if (apt == null) return RedirectToAction("Index");

                var doc = await _context.Doctors.FindAsync(apt.Drid);
                ViewBag.Appointment = apt;
                ViewBag.PrefillDoctorId = apt.Drid;
                ViewBag.PrefillDepId = doc?.Depid;
                ViewBag.PrefillVisitDate = apt.Aptmtdate;
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Add(int? aptmtid, string Pname, string? Mobnum, string? Age, string? Addr,
                                             int Drid, string? Visitdate, string? Diagnosis, string? Prescription,
                                             string? Advice, List<IFormFile>? Files)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            if (string.IsNullOrWhiteSpace(Pname) || Drid <= 0)
            {
                TempData["ErrorMsg"] = "Patient name and doctor are required.";
                return RedirectToAction("Add", new { aptmtid });
            }

            var doc = await _context.Doctors.FindAsync(Drid);

            var report = new PatientReport
            {
                Aptmtid = aptmtid,
                Pname = Pname.Trim(),
                Mobnum = Mobnum?.Trim(),
                Age = Age?.Trim(),
                Addr = Addr?.Trim(),
                Drid = Drid,
                Depid = doc?.Depid,
                Visitdate = string.IsNullOrWhiteSpace(Visitdate) ? TodayIst().ToString("dd-MMM-yyyy") : Visitdate.Trim(),
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
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var report = await _context.PatientReports
                .Include(r => r.Files)
                .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(r => r.Rptid == id);

            if (report == null) return RedirectToAction("MedicalReports");
            return View(report);
        }

        // Upload more files to an existing report
        [HttpPost]
        public async Task<IActionResult> UploadFiles(int id, List<IFormFile>? Files)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var report = await _context.PatientReports.FindAsync(id);
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
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var file = await _context.PatientReportFiles.FindAsync(fileId);
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
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var report = await _context.PatientReports
                .Include(r => r.Doctor)
                .FirstOrDefaultAsync(r => r.Rptid == id);

            if (report == null) return RedirectToAction("MedicalReports");
            return View(report);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, string Pname, string? Mobnum, string? Age, string? Addr,
                                              string? Visitdate, string? Diagnosis, string? Prescription, string? Advice)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var report = await _context.PatientReports.FindAsync(id);
            if (report == null) return RedirectToAction("MedicalReports");

            report.Pname = string.IsNullOrWhiteSpace(Pname) ? report.Pname : Pname.Trim();
            report.Mobnum = Mobnum?.Trim();
            report.Age = Age?.Trim();
            report.Addr = Addr?.Trim();
            report.Visitdate = string.IsNullOrWhiteSpace(Visitdate) ? report.Visitdate : Visitdate.Trim();
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
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var report = await _context.PatientReports.Include(r => r.Files).FirstOrDefaultAsync(r => r.Rptid == id);
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
        // REPORT 2 (list): all medical reports
        // ------------------------------------------------------------
        [HttpGet]
        public async Task<IActionResult> MedicalReports(int? doctorId, string? search)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var user = await _context.AdminUsers.FindAsync(CurrentUserId());
            if (user != null && user.Drid.HasValue)
            {
                doctorId = user.Drid.Value;
                ViewBag.IsDoctorUser = true;
            }

            var query = _context.PatientReports
                .Include(r => r.Files)
                .Include(r => r.Doctor)
                .AsQueryable();

            if (doctorId.HasValue && doctorId.Value > 0)
                query = query.Where(r => r.Drid == doctorId.Value);

            if (!string.IsNullOrWhiteSpace(search))
            {
                string s = search.Trim();
                query = query.Where(r =>
                    (r.Pname != null && r.Pname.Contains(s)) ||
                    (r.Mobnum != null && r.Mobnum.Contains(s)));
            }

            ViewBag.Reports = await query.OrderByDescending(r => r.Rptid).ToListAsync();
            ViewBag.DoctorId = doctorId ?? 0;
            ViewBag.Search = search ?? "";
            ViewBag.Doctors = await _context.Doctors.Where(d => d.Actv == true).OrderBy(d => d.Drname).ToListAsync();

            return View();
        }

        // Printable medical report (give this to the doctor / patient)
        [HttpGet]
        public async Task<IActionResult> PrintReport(int id)
        {
            if (CurrentUserId() == 0) return RedirectToAction("Login", "Auth");

            var report = await _context.PatientReports
                .Include(r => r.Files)
                .Include(r => r.Doctor)
                .ThenInclude(d => d!.Department)
                .FirstOrDefaultAsync(r => r.Rptid == id);

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
