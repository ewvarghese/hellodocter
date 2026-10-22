using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Data;
using HelloDoctor.Care.Models;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Text.Json;

namespace HelloDoctor.Care.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string cryptoKey = "&%#@?,:*";  
        private byte[] key = { };
        private byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xcd, 0xef };

        public AdminController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            var user = await _context.AdminUsers.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            ViewBag.User = user;

            if (user.Roleid == 2 || user.Roleid == 3)
            {
                var doc = await _context.Doctors.Include(d => d.Department).Include(d => d.Location).Include(d => d.District).FirstOrDefaultAsync(d => d.Drid == user.Drid);
                ViewBag.Doctor = doc;
                ViewBag.Districts = await _context.Districts.Where(d => d.Actv == true).ToListAsync();
                ViewBag.Departments = await _context.Departments.Where(d => d.Actv == true).ToListAsync();
                ViewBag.Roles = await _context.Roles.Where(r => r.Actv == true).ToListAsync();
            }

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLiveToken(int LiveToken, string Date)
        {
            try
            {
                int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
                if (userId == 0) return RedirectToAction("Login", "Auth");

                var adminUser = await _context.AdminUsers.FindAsync(userId);

                if (adminUser != null)
                {
                    int dridForQueue = adminUser.Userid;  
                    var liveQueue = await _context.LiveQueues.FirstOrDefaultAsync(l => l.Drid == dridForQueue);

                    TimeZoneInfo tzi = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                    DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi);
                    string now = istTime.ToString("dd-MM-yyyy hh:mm:ss tt");

                    if (liveQueue != null)
                    {
                        liveQueue.LiveToken = LiveToken;
                        liveQueue.Updte = now;
                    }
                    else
                    {
                        int nextId = (_context.LiveQueues.Max(l => (int?)l.Id) ?? 0) + 1;
                        _context.LiveQueues.Add(new LiveQueue
                        {
                            Id = nextId,
                            Drid = dridForQueue,
                            LiveToken = LiveToken,
                            Updte = now
                        });
                    }

                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Live Queue Token updated to #{LiveToken}";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error updating live token: " + ex.Message;
            }

            return RedirectToAction("Appointments", new { date = Date });
        }

        [HttpGet]
        public async Task<IActionResult> Settings()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var bkngDay = await _context.BookingDays.FirstOrDefaultAsync(b => b.Userid == userId);
            var bkngHrs = await _context.BookingHours.FirstOrDefaultAsync(b => b.Userid == userId);

            ViewBag.DayCount = bkngDay?.Daycnt ?? 30;  
            ViewBag.HourCount = bkngHrs?.Hrcnt ?? 1;   

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDayCount(int dayCount)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var bkngDay = await _context.BookingDays.FirstOrDefaultAsync(b => b.Userid == userId);
            if (bkngDay != null)
            {
                bkngDay.Daycnt = dayCount;
            }
            else
            {
                int nextId = (_context.BookingDays.Max(b => (int?)b.Id) ?? 0) + 1;
                _context.BookingDays.Add(new BookingDay { Id = nextId, Userid = userId, Daycnt = dayCount });
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Day Count Updated Successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateHourCount(int hourCount)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var bkngHrs = await _context.BookingHours.FirstOrDefaultAsync(b => b.Userid == userId);
            if (bkngHrs != null)
            {
                bkngHrs.Hrcnt = hourCount;
            }
            else
            {
                int nextId = (_context.BookingHours.Max(b => (int?)b.Id) ?? 0) + 1;
                _context.BookingHours.Add(new BookingHour { Id = nextId, Userid = userId, Hrcnt = hourCount });
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Hour Count Updated Successfully!";
            return RedirectToAction("Settings");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmail(string email)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            var user = await _context.AdminUsers.FindAsync(userId);
            if (user != null)
            {
                user.Email = email;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Email Updated Successfully";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string newPassword)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            var user = await _context.AdminUsers.FindAsync(userId);
            if (user != null)
            {
                user.Pwd = EncryptPass(newPassword, cryptoKey);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Password Updated Successfully";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EditDoctor(int Drid, string Drname, int Depid, int Did, int Cid, int Roleid, string Designation, decimal cfee, string Address, string WorkingHospital, string Drcode, string Email, string Mob1, IFormFile PhotoFile)
        {
            var doctor = await _context.Doctors.FindAsync(Drid);
            if (doctor != null)
            {
                doctor.Drname = Drname;
                doctor.Depid = Depid;
                doctor.Did = Did;
                doctor.Cid = Cid;
                doctor.Designation = Designation;
                doctor.Address = Address;
                doctor.WorkingHospital = WorkingHospital;
                doctor.Mob1 = Mob1;
                doctor.cfee = cfee;

                if (PhotoFile != null && PhotoFile.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                    Directory.CreateDirectory(uploadsFolder);
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + PhotoFile.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await PhotoFile.CopyToAsync(fileStream);
                    }
                    doctor.Photo = uniqueFileName;
                }

                var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Drid == Drid);
                if (user != null)
                {
                    user.Email = Email;
                    user.Roleid = Roleid;
                    user.Drcode = Drcode;
                    user.Pwd = EncryptPass(Mob1, cryptoKey);
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Doctor Profile Updated Successfully";
            }
            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<JsonResult> CheckWhatsapid([FromBody] JsonElement data)
        {
            string username = data.GetProperty("username").GetString() ?? "";
            bool exists = await _context.AdminUsers.AnyAsync(u => u.Drcode == username);
            return Json(!exists);
        }

        [HttpGet]
        public async Task<IActionResult> ConsultationDays()
        {
            int drId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (drId == 0) return RedirectToAction("Login", "Auth");

            ViewBag.Weekdays = await _context.Weekdays.ToListAsync();
            var userId = drId;// await _context.AdminUsers.Where(a => a.Drid == drId).Select(a => a.Userid)    .FirstOrDefaultAsync();
            ViewBag.MyDays = await _context.ConsultationDays
                .Where(c => c.Userid == userId)
                .ToListAsync();

            ViewBag.ActiveDaysList = await (from c in _context.ConsultationDays
                                            join w in _context.Weekdays on c.Dayid equals w.Dayid
                                            where c.Userid == userId && c.Actv == true
                                            orderby c.Dayid
                                            select new { c.Cnid, w.Days }).ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UpdateConsultationDays(List<int>? selectedDays)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            selectedDays ??= new List<int>();
            var existingDays = await _context.ConsultationDays.Where(c => c.Userid == userId).ToListAsync();
            var allWeekdays = await _context.Weekdays.ToListAsync();
            string now = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
            int nextId = (await _context.ConsultationDays.MaxAsync(c => (int?)c.Cnid) ?? 0) + 1;

            foreach (var wd in allWeekdays)
            {
                bool isSelected = selectedDays.Contains(wd.Dayid);
                var existingRecord = existingDays.FirstOrDefault(c => c.Dayid == wd.Dayid);

                if (isSelected)
                {
                    if (existingRecord != null)
                    {
                        existingRecord.Actv = true;
                        existingRecord.Updte = now;
                    }
                    else
                    {
                        _context.ConsultationDays.Add(new ConsultationDay
                        {
                            Cnid = nextId++,
                            Dayid = wd.Dayid,
                            Userid = userId,
                            Actv = true,
                            Updte = now
                        });
                    }
                }
                else
                {
                    if (existingRecord != null)
                    {
                        existingRecord.Actv = false;
                        existingRecord.Updte = now;
                    }
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Consultation Days Updated Successfully! Now click the + button beside each day below to add time slots.";
            return RedirectToAction("ConsultationDays");
        }

        [HttpGet]
        public async Task<IActionResult> Slots(int cnid)
        {
            int drId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (drId == 0) return RedirectToAction("Login", "Auth");


            var userId = drId;// await _context.AdminUsers.Where(a => a.Drid == drId).Select(a => a.Userid).FirstOrDefaultAsync();

            var consultationDay = await _context.ConsultationDays.FirstOrDefaultAsync(c => c.Cnid == cnid && c.Userid == userId);
            if (consultationDay == null) return RedirectToAction("ConsultationDays");

            var activeDays = await (from c in _context.ConsultationDays
                                    join w in _context.Weekdays on c.Dayid equals w.Dayid
                                    where c.Userid == userId && c.Actv == true
                                    select new SelectListItem { Value = c.Cnid.ToString(), Text = w.Days }).ToListAsync();

            ViewBag.ActiveDays = activeDays;
            ViewBag.Cnid = cnid;

            var slots = await (from csd in _context.ConsultationSlotDetails
                               join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                               where cs.Cnid == cnid && cs.Userid == userId
                               orderby csd.Slotdtid
                               select new { csd.Slotdtid, csd.Frmtymdt, csd.Token, csd.Actv }).ToListAsync();

            ViewBag.Slots = slots;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> GenerateSlots(int cnid, string startTime, string endTime, int duration)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            string[] timeFormats = { "hh:mm tt", "h:mm tt" };
            if (!DateTime.TryParseExact(startTime?.Trim(), timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime start) ||
                !DateTime.TryParseExact(endTime?.Trim(), timeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime end))
            {
                TempData["ErrorMsg"] = "Invalid time selection.";
                return RedirectToAction("Slots", new { cnid });
            }

            if (end <= start || duration <= 0 || start.AddMinutes(duration) > end)
            {
                TempData["ErrorMsg"] = "To Time must be later than From Time.";
                return RedirectToAction("Slots", new { cnid });
            }

            int nextSlotId = (await _context.ConsultationSlots.MaxAsync(s => (int?)s.Slotid) ?? 0) + 1;

            var masterSlot = new ConsultationSlot { Slotid = nextSlotId, Cnid = cnid, Userid = userId };
            _context.ConsultationSlots.Add(masterSlot);

            int existingMaxToken = await (from csd in _context.ConsultationSlotDetails
                                          join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                                          where cs.Cnid == cnid && cs.Userid == userId
                                          select (int?)csd.Token).MaxAsync() ?? 0;
            int tokenCounter = existingMaxToken + 1;
            int nextDetailId = (await _context.ConsultationSlotDetails.MaxAsync(s => (int?)s.Slotdtid) ?? 0) + 1;

            while (start < end && start.AddMinutes(duration) <= end)
            {
                DateTime next = start.AddMinutes(duration);
                string timeStr = $"{start.ToString("hh:mm tt", CultureInfo.InvariantCulture)}-{next.ToString("hh:mm tt", CultureInfo.InvariantCulture)}";

                _context.ConsultationSlotDetails.Add(new ConsultationSlotDetail
                {
                    Slotdtid = nextDetailId++,
                    Slotid = nextSlotId,
                    Frmtymdt = timeStr,
                    Actv = true,
                    Token = tokenCounter++
                });
                start = next;
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Slots Generated Successfully!";
            return RedirectToAction("Slots", new { cnid });
        }

        [HttpPost]
        public async Task<IActionResult> ManageSlots(int cnid, List<int> selectedSlots, string actionType)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            if (selectedSlots != null && selectedSlots.Any())
            {
                var details = await _context.ConsultationSlotDetails.Where(d => selectedSlots.Contains(d.Slotdtid)).ToListAsync();

                if (actionType == "delete")
                {
                    _context.ConsultationSlotDetails.RemoveRange(details);
                    TempData["SuccessMsg"] = "Slots Deleted Successfully!";
                }
                else if (actionType == "sleep")
                {
                    details.ForEach(d => d.Actv = false);
                    TempData["SuccessMsg"] = "Slots marked as Sleep Time!";
                }
                else if (actionType == "unsleep")
                {
                    details.ForEach(d => d.Actv = true);
                    TempData["SuccessMsg"] = "Slots Activated Successfully!";
                }
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Slots", new { cnid });
        }

        [HttpGet]
        public async Task<IActionResult> ToggleSlotStatus(int slotdtid, int cnid)
        {
            var slot = await _context.ConsultationSlotDetails.FindAsync(slotdtid);
            if (slot != null)
            {
                slot.Actv = !slot.Actv;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Slots", new { cnid });
        }

        [HttpGet]
        public async Task<IActionResult> Appointments(string date)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var user = await _context.AdminUsers.FindAsync(userId);
            ViewBag.ShowProfileEdit = (user?.Roleid == 2 || user?.Roleid == 3);

            if (user != null && user.Drid.HasValue)
            {
                var liveQ = await _context.LiveQueues.FirstOrDefaultAsync(l => l.Drid == user.Drid.Value);
                ViewBag.LiveToken = liveQ?.LiveToken ?? 0;
            }

             
            TimeZoneInfo ist;
            try
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            DateTime selectedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);

            if (!string.IsNullOrEmpty(date))
            {
                DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedDate);
            }

            string aptmtdate = selectedDate.ToString("dd-MMM-yyyy");
            ViewBag.SelectedDate = selectedDate.ToString("yyyy-MM-dd");  
            ViewBag.DisplayDate = aptmtdate;

            var appointments = await (from a in _context.Appointments
                                      join csd in _context.ConsultationSlotDetails on a.Slotdtid equals csd.Slotdtid
                                      where a.Drid == userId && a.Aptmtdate == aptmtdate && a.Actv == true && a.Payment==true
                                      orderby csd.Token
                                      select new
                                      {
                                          a.Aptmtid,
                                          a.Pname,
                                          a.Addr,
                                          a.Age,
                                          a.Mobnum,
                                          csd.Token,
                                          a.Aptmtdate,
                                          csd.Frmtymdt,
                                          a.Payment
                                      }).ToListAsync();

            ViewBag.Appointments = appointments;

            ViewBag.ActiveDates = await _context.Appointments
                                        .Where(a => a.Drid == userId && a.Actv == true)
                                        .Select(a => a.Aptmtdate)
                                        .Distinct()
                                        .ToListAsync();

            ViewBag.LeaveDates = await _context.Leaves
                                        .Where(l => l.Drid == userId)
                                        .Select(l => l.Frmdt)
                                        .ToListAsync();

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> DeleteAppointment(int aptmtid, string returnDate)
        {
            var apt = await _context.Appointments.FindAsync(aptmtid);
            if (apt != null)
            {
                _context.Appointments.Remove(apt);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Appointment Deleted Successfully";
            }
            return RedirectToAction("Appointments", new { date = returnDate });
        }

        [HttpGet]
        public async Task<IActionResult> PrintAppointments(string date)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

             
            TimeZoneInfo ist;
            try
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            DateTime selectedDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);

            if (!string.IsNullOrEmpty(date))
            {
                DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedDate);
            }
            string aptmtdate = selectedDate.ToString("dd-MMM-yyyy");

            var appointments = await (from a in _context.Appointments
                                      join csd in _context.ConsultationSlotDetails on a.Slotdtid equals csd.Slotdtid
                                      where a.Drid == userId && a.Aptmtdate == aptmtdate && a.Actv == true && a.Payment==true 
                                      orderby csd.Token
                                      select new
                                      {
                                          a.Aptmtid,
                                          a.Pname,
                                          a.Addr,
                                          a.Age,
                                          a.Mobnum,
                                          csd.Token,
                                          a.Aptmtdate,
                                          csd.Frmtymdt,
                                          a.Payment
                                      }).ToListAsync();

            ViewBag.Appointments = appointments;
            ViewBag.DisplayDate = aptmtdate;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageLeave()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var leaves = await _context.Leaves
                .Where(l => l.Drid == userId)
                .OrderByDescending(l => l.Lid)
                .ToListAsync();

            return View(leaves);
        }

        [HttpPost]
        public async Task<IActionResult> AddLeave(DateTime leaveDate)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            if (leaveDate != default)
            {
                int nextId = (_context.Leaves.Max(l => (int?)l.Lid) ?? 0) + 1;
                string dateStr = leaveDate.ToString("dd-MMM-yyyy");
                string nowStr = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");

                _context.Leaves.Add(new DoctorLeave
                {
                    Lid = nextId,
                    Drid = userId,
                    Frmdt = dateStr,
                    Todt = dateStr,
                    Updte = nowStr
                });

                var aptsToCancel = await _context.Appointments
                    .Where(a => a.Drid == userId && a.Aptmtdate == dateStr && a.Actv == true)
                    .ToListAsync();

                aptsToCancel.ForEach(a => a.Actv = false);

                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Leave Added Successfully! Affected appointments were deactivated.";
            }
            return RedirectToAction("ManageLeave");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLeaves(List<int> selectedLeaves)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            if (selectedLeaves != null && selectedLeaves.Any())
            {
                var leavesToDelete = await _context.Leaves
                    .Where(l => selectedLeaves.Contains(l.Lid) && l.Drid == userId)
                    .ToListAsync();

                _context.Leaves.RemoveRange(leavesToDelete);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Selected Leaves Deleted Successfully";
            }
            return RedirectToAction("ManageLeave");
        }

        private string EncryptPass(string stringToEncrypt, string SEncryptionKey)
        {
            try
            {
                key = System.Text.Encoding.UTF8.GetBytes(SEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                byte[] inputByteArray = Encoding.UTF8.GetBytes(stringToEncrypt);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateEncryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                return Convert.ToBase64String(ms.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        [HttpGet]
        public async Task<IActionResult> BookAppointment()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var user = await _context.AdminUsers.FindAsync(userId);
            if (user == null || user.Drid == null) return RedirectToAction("Login", "Auth");

            var docInfo = await (from d in _context.Doctors
                                 join dp in _context.Departments on d.Depid equals dp.Depid into deptGroup
                                 from dp in deptGroup.DefaultIfEmpty()
                                 where d.Drid == user.Drid
                                 select new
                                 {
                                     Name = d.Drname,
                                     Dept = dp != null ? dp.Depname : ""
                                 }).FirstOrDefaultAsync();

            var slotDays = await (from csd in _context.ConsultationSlotDetails
                                  join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                                  join cd in _context.ConsultationDays on cs.Cnid equals cd.Cnid
                                  join wd in _context.Weekdays on cd.Dayid equals wd.Dayid
                                  where cs.Userid == user.Userid && cd.Actv == true && csd.Actv == true && wd.Days != null
                                  orderby wd.Dayid
                                  select wd.Days!).Distinct().ToListAsync();

            ViewBag.DoctorId = user.Userid;
            ViewBag.DoctorName = docInfo != null ? $"{docInfo.Name} ({docInfo.Dept})" : "Doctor";
            ViewBag.SlotDays = slotDays;
            ViewBag.HasSlots = slotDays.Count > 0;

            return View();
        }

        [HttpGet]
        public async Task<JsonResult> GetDoctorSlots(int drid, string date)
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime selectedDate)
                && !DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out selectedDate)
                && !DateTime.TryParse(date, out selectedDate))
            {
                return Json(new List<object>());
            }

            string dayOfWeek = selectedDate.DayOfWeek.ToString();
            string aptmtdateInvariant = selectedDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture);
            string aptmtdateLocal = selectedDate.ToString("dd-MMM-yyyy");

            bool onLeave = await _context.Leaves.AnyAsync(l =>
                l.Drid == drid && (l.Frmdt == aptmtdateInvariant || l.Frmdt == aptmtdateLocal));
            if (onLeave)
                return Json(new List<object>());

            var bookedSlots = await _context.Appointments
                .Where(a => a.Drid == drid
                         && (a.Aptmtdate == aptmtdateInvariant || a.Aptmtdate == aptmtdateLocal)
                         && a.Actv == true
                         && a.Slotdtid != null)
                .Select(a => a.Slotdtid)
                .ToListAsync();

            var slots = await (from csd in _context.ConsultationSlotDetails
                               join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                               join cd in _context.ConsultationDays on cs.Cnid equals cd.Cnid
                               join wd in _context.Weekdays on cd.Dayid equals wd.Dayid
                               where wd.Days == dayOfWeek
                                  && cs.Userid == drid
                                  && cd.Actv == true
                                  && csd.Actv == true
                                  && !bookedSlots.Contains(csd.Slotdtid)
                               orderby csd.Slotdtid
                               select new
                               {
                                   slotdtid = csd.Slotdtid,
                                   frmtymdt = csd.Frmtymdt,
                                   token = csd.Token
                               }).ToListAsync();

            return Json(slots);
        }

        [HttpPost]
        public async Task<IActionResult> SubmitBooking(string PatientName, string Mobile, int Age, string Address, int DoctorId, string Date, int Slotdtid)
        {
            try
            {
                DateTime parsedDate;
                bool okDate = DateTime.TryParseExact(Date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
                           || DateTime.TryParse(Date, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate)
                           || DateTime.TryParse(Date, out parsedDate);
                string aptmtdate = okDate ? parsedDate.ToString("dd-MMM-yyyy", CultureInfo.InvariantCulture) : Date;
                string aptmtday = okDate ? parsedDate.DayOfWeek.ToString() : "";

                var slotInfo = await (from csd in _context.ConsultationSlotDetails
                                      join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                                      join cd in _context.ConsultationDays on cs.Cnid equals cd.Cnid
                                      where csd.Slotdtid == Slotdtid && cs.Userid == DoctorId
                                      select new { cd.Dayid, csd.Token, csd.Frmtymdt }).FirstOrDefaultAsync();

                int? fetchedDayId = slotInfo?.Dayid;
                int? fetchedToken = slotInfo?.Token;
                string timeSlot = slotInfo?.Frmtymdt ?? "";

                var docInfo = await (from adm in _context.AdminUsers
                                     join d in _context.Doctors on adm.Drid equals d.Drid
                                     join dp in _context.Departments on d.Depid equals dp.Depid into deptGroup
                                     from dp in deptGroup.DefaultIfEmpty()
                                     where adm.Userid == DoctorId
                                     select new
                                     {
                                         DoctorName = d.Drname,
                                         DeptName = dp != null ? dp.Depname : "",
                                         DepId = d.Depid
                                     }).FirstOrDefaultAsync();

                string doctorName = docInfo?.DoctorName ?? "";
                string deptName = docInfo?.DeptName ?? "";
                int depId = docInfo?.DepId ?? 0;

                int nextId = (await _context.Appointments.MaxAsync(a => (int?)a.Aptmtid) ?? 0) + 1;
                TimeZoneInfo tzi;
                try { tzi = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time"); }
                catch (TimeZoneNotFoundException) { tzi = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata"); }
                string updte = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tzi).ToString("dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);

                var newApt = new Appointment
                {
                    Aptmtid = nextId,
                    Pname = PatientName,
                    Mobnum = Mobile,
                    Depid = depId,
                    Drid = DoctorId,
                    Aptmtdate = aptmtdate,
                    Aptmtday = aptmtday,
                    Slotdtid = Slotdtid,
                    Addr = Address,
                    Age = Age,
                    Actv = true,
                    Payment = true,
                    Updte = updte,
                    Aptmtdayid = fetchedDayId
                };

                _context.Appointments.Add(newApt);
                await _context.SaveChangesAsync();

                if (!string.IsNullOrEmpty(Mobile) && Mobile.Length >= 10)
                {
                    try
                    {
                        string message = $"Online Doctor Booking Details :Doctor: {doctorName}({deptName})Date: {aptmtdate}Time: {timeSlot}Token: {fetchedToken}HelloDoctor";
                        string encodedMessage = Uri.EscapeDataString(message);
                        string strUrl = $"http://sapteleservices.com/SMS_API/sendsms.php?username=&password=&mobile={Mobile}&sendername=APMTDR&message={encodedMessage}&routetype=1&tid=1207162184684214850";

                        using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient { Timeout = TimeSpan.FromSeconds(3) })
                        {
                            await client.GetAsync(strUrl);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("SMS failed: " + ex.Message);
                    }
                }

                TempData["SuccessMessage"] = "Appointment booked successfully!";
                return RedirectToAction("Appointments", new { date = okDate ? parsedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) : "" });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error booking appointment: " + ex.Message;
                return RedirectToAction("BookAppointment");
            }
        }

        [HttpGet]
        public async Task<IActionResult> Departments()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var departments = await _context.Departments.OrderBy(d => d.Priority).ToListAsync();
            return View(departments);
        }

        [HttpPost]
        public async Task<IActionResult> AddDepartment(string Depname)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            int nextId = (_context.Departments.Max(d => (int?)d.Depid) ?? 0) + 1;
            int nextPriority = (_context.Departments.Max(d => (int?)d.Priority) ?? 0) + 1;

            var dept = new Department
            {
                Depid = nextId,
                Depname = Depname,
                Priority = nextPriority,
                Actv = true,
                Userid = userId,
                Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt")
            };

            _context.Departments.Add(dept);
            await _context.SaveChangesAsync();

            TempData["SuccessMsg"] = "Department Added Successfully";
            return RedirectToAction("Departments");
        }

        [HttpPost]
        public async Task<IActionResult> EditDepartment(int Depid, string Depname)
        {
            var dept = await _context.Departments.FindAsync(Depid);
            if (dept != null)
            {
                dept.Depname = Depname;
                dept.Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Department Updated Successfully";
            }
            return RedirectToAction("Departments");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDepartmentPriorities(List<int> depids, List<int> priorities)
        {
            for (int i = 0; i < depids.Count; i++)
            {
                if (i < priorities.Count)
                {
                    var dept = await _context.Departments.FindAsync(depids[i]);
                    if (dept != null) dept.Priority = priorities[i];
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Priority Updated Successfully";
            return RedirectToAction("Departments");
        }

        [HttpGet]
        public async Task<IActionResult> ToggleDepartmentStatus(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                dept.Actv = !dept.Actv;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = dept.Actv == true ? "Status Activated Successfully" : "Status Deactivated Successfully";
            }
            return RedirectToAction("Departments");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept != null)
            {
                _context.Departments.Remove(dept);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Deleted Successfully";
            }
            return RedirectToAction("Departments");
        }

        [HttpGet]
        public async Task<IActionResult> Districts()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var districts = await _context.Districts.OrderBy(d => d.Priority).ToListAsync();
            return View(districts);
        }

        [HttpPost]
        public async Task<IActionResult> AddDistrict(string Dname)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            int nextId = (_context.Districts.Max(d => (int?)d.Did) ?? 0) + 1;
            int nextPriority = (_context.Districts.Max(d => (int?)d.Priority) ?? 0) + 1;

            var dist = new District
            {
                Did = nextId,
                Dname = Dname,
                Priority = nextPriority,
                Actv = true,
                Userid = userId,
                Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt")
            };

            _context.Districts.Add(dist);
            await _context.SaveChangesAsync();

            TempData["SuccessMsg"] = "District Added Successfully";
            return RedirectToAction("Districts");
        }

        [HttpPost]
        public async Task<IActionResult> EditDistrict(int Did, string Dname)
        {
            var dist = await _context.Districts.FindAsync(Did);
            if (dist != null)
            {
                dist.Dname = Dname;
                dist.Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "District Updated Successfully";
            }
            return RedirectToAction("Districts");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateDistrictPriorities(List<int> dids, List<int> priorities)
        {
            for (int i = 0; i < dids.Count; i++)
            {
                if (i < priorities.Count)
                {
                    var dist = await _context.Districts.FindAsync(dids[i]);
                    if (dist != null) dist.Priority = priorities[i];
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Priority Updated Successfully";
            return RedirectToAction("Districts");
        }

        [HttpGet]
        public async Task<IActionResult> ToggleDistrictStatus(int id)
        {
            var dist = await _context.Districts.FindAsync(id);
            if (dist != null)
            {
                dist.Actv = !dist.Actv;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = dist.Actv == true ? "Status Activated Successfully" : "Status Deactivated Successfully";
            }
            return RedirectToAction("Districts");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDistrict(int id)
        {
            var dist = await _context.Districts.FindAsync(id);
            if (dist != null)
            {
                _context.Districts.Remove(dist);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Deleted Successfully";
            }
            return RedirectToAction("Districts");
        }

        [HttpGet]
        public async Task<IActionResult> Locations(int? filterDid)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var districts = await _context.Districts.Where(d => d.Actv == true).OrderBy(d => d.Priority).ToListAsync();
            ViewBag.Districts = districts;
            ViewBag.FilterDid = filterDid ?? 0;

            var locationsQuery = _context.Locations.Include(l => l.District).AsQueryable();

            if (filterDid.HasValue && filterDid.Value > 0)
            {
                locationsQuery = locationsQuery.Where(l => l.Did == filterDid.Value);
            }

            var locations = await locationsQuery.OrderBy(l => l.Priority).ToListAsync();
            return View(locations);
        }

        [HttpPost]
        public async Task<IActionResult> AddLocation(string Cname, int Did, int? filterDid)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            int nextId = (_context.Locations.Max(l => (int?)l.Cid) ?? 0) + 1;
            int nextPriority = (_context.Locations.Where(l => l.Did == Did).Max(l => (int?)l.Priority) ?? 0) + 1;

            var loc = new Location
            {
                Cid = nextId,
                Cname = Cname,
                Did = Did,
                Priority = nextPriority,
                Actv = true,
                Userid = userId,
                Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt")
            };

            _context.Locations.Add(loc);
            await _context.SaveChangesAsync();

            TempData["SuccessMsg"] = "Location Added Successfully";
            return RedirectToAction("Locations", new { filterDid = filterDid });
        }

        [HttpPost]
        public async Task<IActionResult> EditLocation(int Cid, int Did, string Cname, int? filterDid)
        {
            var loc = await _context.Locations.FindAsync(Cid);
            if (loc != null)
            {
                loc.Cname = Cname;
                loc.Did = Did;
                loc.Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Location Updated Successfully";
            }
            return RedirectToAction("Locations", new { filterDid = filterDid });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateLocationPriorities(List<int> cids, List<int> priorities, int? filterDid)
        {
            for (int i = 0; i < cids.Count; i++)
            {
                if (i < priorities.Count)
                {
                    var loc = await _context.Locations.FindAsync(cids[i]);
                    if (loc != null) loc.Priority = priorities[i];
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Priority Updated Successfully";
            return RedirectToAction("Locations", new { filterDid = filterDid });
        }

        [HttpGet]
        public async Task<IActionResult> ToggleLocationStatus(int id, int? filterDid)
        {
            var loc = await _context.Locations.FindAsync(id);
            if (loc != null)
            {
                loc.Actv = !loc.Actv;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = loc.Actv == true ? "Status Activated Successfully" : "Status Deactivated Successfully";
            }
            return RedirectToAction("Locations", new { filterDid = filterDid });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteLocation(int id, int? filterDid)
        {
            var loc = await _context.Locations.FindAsync(id);
            if (loc != null)
            {
                _context.Locations.Remove(loc);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Deleted Successfully";
            }
            return RedirectToAction("Locations", new { filterDid = filterDid });
        }

        [HttpGet]
        public async Task<IActionResult> Doctors(int? filterDid, int? filterCid, int? filterDepid)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");
            var user = await _context.AdminUsers.FindAsync(userId);
            if (user == null) return RedirectToAction("Login", "Auth");

            ViewBag.Districts = await _context.Districts.Where(d => d.Actv == true).OrderBy(d => d.Priority).ToListAsync();
            ViewBag.Departments = await _context.Departments.Where(d => d.Actv == true).OrderBy(d => d.Priority).ToListAsync();
            ViewBag.Roles = await _context.Roles.Where(r => r.Actv == true).ToListAsync();

            ViewBag.FilterDid = filterDid ?? 0;
            ViewBag.FilterCid = filterCid ?? 0;
            ViewBag.FilterDepid = filterDepid ?? 0;

            if (filterDid.HasValue && filterDid > 0)
            {
                ViewBag.Locations = await _context.Locations.Where(l => l.Did == filterDid && l.Actv == true).ToListAsync();
            }
            else
            {
                ViewBag.Locations = new List<Location>();
            }

            var query = _context.Doctors.Include(d => d.Department).Include(d => d.Location).Include(d => d.District).AsQueryable();

            if (user.Roleid == 2 || user.Roleid == 3)
            {
                query = query.Where(d => d.Drid == user.Drid);
            }

            if (filterDid > 0) query = query.Where(d => d.Did == filterDid);
            if (filterCid > 0) query = query.Where(d => d.Cid == filterCid);
            if (filterDepid > 0) query = query.Where(d => d.Depid == filterDepid);

            var doctors = await query.OrderByDescending(d => d.Drid).ToListAsync();

            var adminUsers = await _context.AdminUsers.Where(u => u.Drid != null).ToDictionaryAsync(u => u.Drid, u => u);
            ViewBag.AdminUsers = adminUsers;

            return View(doctors);
        }

        [HttpGet]
        public async Task<JsonResult> GetLocationsByDistrict(int did)
        {
            var locs = await _context.Locations.Where(l => l.Did == did && l.Actv == true).Select(l => new { cid = l.Cid, name = l.Cname }).ToListAsync();
            return Json(locs);
        }

        [HttpPost]
        public async Task<JsonResult> CheckDoctorUserName([FromBody] JsonElement data)
        {
            string username = data.GetProperty("username").GetString() ?? "";
            bool exists = await _context.Doctors.AnyAsync(u => u.Mob2 == username);
            return Json(!exists);
        }

        [HttpPost]
        public async Task<IActionResult> AddManageDoctor(int Did, int Cid, int Depid, int Roleid, string Drname, string Designation, string Mob2, string Drcode, string Address, string WorkingHospital, decimal cfee, string Email, string Mob1, IFormFile filedoctor)
        {
            try
            {
                string now = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");

                int nextDrid = (_context.Doctors.Max(d => (int?)d.Drid) ?? 0) + 1;
                int priority = (_context.Doctors.Where(d => d.Depid == Depid).Max(d => (int?)d.Priority) ?? 0) + 1;

                string photoName = "docimg.jpg";
                if (filedoctor != null && filedoctor.Length > 0)
                {
                    string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                    Directory.CreateDirectory(uploadsFolder);
                    photoName = Guid.NewGuid().ToString() + "_" + filedoctor.FileName;
                    using (var stream = new FileStream(Path.Combine(uploadsFolder, photoName), FileMode.Create))
                    {
                        await filedoctor.CopyToAsync(stream);
                    }
                }

                var doc = new Doctor
                {
                    Drid = nextDrid,
                    Did = Did,
                    Cid = Cid,
                    Depid = Depid,
                    Drname = Drname,
                    Designation = Designation,
                    Address = Address,
                    WorkingHospital = WorkingHospital,
                    Email = Email,
                    Mob1 = Mob1,
                    Mob2 = Mob2,
                    cfee = cfee,
                    Photo = photoName,
                    Priority = priority,
                    Actv = true,
                    Payment = true,
                    Updte = now
                };
                _context.Doctors.Add(doc);

                int nextUserId = (_context.AdminUsers.Max(u => (int?)u.Userid) ?? 0) + 1;
                string encryp_pass = EncryptPass(Mob1, cryptoKey);
                var aUser = new AdminUser
                {
                    Userid = nextUserId,
                    Drid = nextDrid,
                    Email = Email,
                    Uname = Email,
                    Drcode = Drcode,
                    Pwd = encryp_pass,
                    Roleid = Roleid,
                    Actv = true,
                    Updte = now,
                    Subactv = true,
                    Payment = true
                };
                _context.AdminUsers.Add(aUser);

                int nextBkDayId = (_context.BookingDays.Max(b => (int?)b.Id) ?? 0) + 1;
                _context.BookingDays.Add(new BookingDay { Id = nextBkDayId, Userid = nextUserId, Daycnt = 30 });

                int nextBkHrId = (_context.BookingHours.Max(b => (int?)b.Id) ?? 0) + 1;
                _context.BookingHours.Add(new BookingHour { Id = nextBkHrId, Userid = nextUserId, Hrcnt = 1 });

                int nextSubId = (_context.SubscriptionDetails.Max(s => (int?)s.Dtid) ?? 0) + 1;
                _context.SubscriptionDetails.Add(new SubscriptionDetail
                {
                    Dtid = nextSubId,
                    Drid = nextDrid,
                    Pid = 1,
                    Subamt = 100,
                    Actv = true,
                    Subfrmdt = DateTime.Now,
                    Subtodt = DateTime.Now.AddMonths(1),
                    Updte = now
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Doctor Added Successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "Error adding doctor: " + ex.Message;
            }
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateManageDoctor(int Drid, int Did, int Cid, int Depid, int Roleid, string Drname, string Designation, string Drcode, string Address, string WorkingHospital, decimal cfee, string Email, string Mob1, IFormFile fileuploaddred)
        {
            try
            {
                var doc = await _context.Doctors.FindAsync(Drid);
                var aUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Drid == Drid);

                if (doc != null)
                {
                    doc.Did = Did; doc.Cid = Cid; doc.Depid = Depid;
                    doc.Drname = Drname; doc.Designation = Designation; doc.Address = Address;
                    doc.WorkingHospital = WorkingHospital; doc.Email = Email; doc.Mob1 = Mob1;
                    doc.cfee = cfee; doc.Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");

                    if (fileuploaddred != null && fileuploaddred.Length > 0)
                    {
                        string uploadsFolder = Path.Combine(_env.WebRootPath, "images");
                        Directory.CreateDirectory(uploadsFolder);
                        string photoName = Guid.NewGuid().ToString() + "_" + fileuploaddred.FileName;
                        using (var stream = new FileStream(Path.Combine(uploadsFolder, photoName), FileMode.Create))
                        {
                            await fileuploaddred.CopyToAsync(stream);
                        }
                        doc.Photo = photoName;
                    }
                }

                if (aUser != null)
                {
                    aUser.Roleid = Roleid;
                    aUser.Drcode = Drcode;
                    aUser.Email = Email;
                    aUser.Pwd = EncryptPass(Mob1, cryptoKey);
                    aUser.Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
                }

                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Doctor Updated Successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMsg"] = "Error updating doctor: " + ex.Message;
            }
            return RedirectToAction("Doctors");
        }

        [HttpGet]
        public async Task<IActionResult> ToggleDoctorActv(int id)
        {
            var doc = await _context.Doctors.FindAsync(id);
            var aUser = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Drid == id);
            if (doc != null)
            {
                doc.Actv = !doc.Actv;
                if (aUser != null) aUser.Actv = doc.Actv;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = doc.Actv == true ? "Account Activated!" : "Account Suspended!";
            }
            return RedirectToAction("Doctors");
        }

        [HttpGet]
        public async Task<IActionResult> ToggleDoctorPayment(int id)
        {
            var doc = await _context.Doctors.FindAsync(id);
            if (doc != null)
            {
                doc.Payment = !doc.Payment;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = doc.Payment == true ? "Payment Enabled!" : "Payment Disabled!";
            }
            var result = await (
      from admin in _context.AdminUsers
      join doctor in _context.Doctors
          on admin.Drid equals doctor.Drid
      where admin.Drid == id
      select new { admin, doctor }
  ).FirstOrDefaultAsync();

            if (result != null)
            {
                result.admin.Payment = !result.admin.Payment;

               
                await _context.SaveChangesAsync();

                TempData["SuccessMsg"] = (bool)result.admin.Payment
                    ? "Payment Enabled!"
                    : "Payment Disabled!";
            }
            return RedirectToAction("Doctors");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteDoctor(int id)
        {
            var doc = await _context.Doctors.FindAsync(id);
            if (doc != null)
            {
                var adminUsers = await _context.AdminUsers
           .Where(a => a.Drid == id)
           .ToListAsync();

               
                if (adminUsers.Any())
                {
                    _context.AdminUsers.RemoveRange(adminUsers);
                }
                _context.Doctors.Remove(doc);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Doctor Deleted Successfully!";
            }
            return RedirectToAction("Doctors");
        }

        [HttpGet]
        public async Task<IActionResult> SubscriptionPlans()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var plans = await _context.SubscriptionPlans.OrderBy(p => p.Priority).ToListAsync();
            return View(plans);
        }

        [HttpPost]
        public async Task<IActionResult> AddSubscriptionPlan(string Pname, string Amt, string Disc)
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            int nextId = (_context.SubscriptionPlans.Max(p => (int?)p.Pid) ?? 0) + 1;
            int nextPriority = (_context.SubscriptionPlans.Max(p => (int?)p.Priority) ?? 0) + 1;

            var plan = new SubscriptionPlan
            {
                Pid = nextId,
                Pname = Pname,
                Amt = Amt,
                Disc = Disc,
                Priority = nextPriority,
                Actv = true,
                Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt")
            };

            _context.SubscriptionPlans.Add(plan);
            await _context.SaveChangesAsync();

            TempData["SuccessMsg"] = "Plan Added Successfully";
            return RedirectToAction("SubscriptionPlans");
        }

        [HttpPost]
        public async Task<IActionResult> EditSubscriptionPlan(int Pid, string Pname, string Amt, string Disc)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(Pid);
            if (plan != null)
            {
                plan.Pname = Pname;
                plan.Amt = Amt;
                plan.Disc = Disc;
                plan.Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt");
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Plan Updated Successfully";
            }
            return RedirectToAction("SubscriptionPlans");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateSubscriptionPlanPriorities(List<int> pids, List<int> priorities)
        {
            for (int i = 0; i < pids.Count; i++)
            {
                if (i < priorities.Count)
                {
                    var plan = await _context.SubscriptionPlans.FindAsync(pids[i]);
                    if (plan != null) plan.Priority = priorities[i];
                }
            }
            await _context.SaveChangesAsync();
            TempData["SuccessMsg"] = "Priority Updated Successfully";
            return RedirectToAction("SubscriptionPlans");
        }

        [HttpGet]
        public async Task<IActionResult> ToggleSubscriptionPlanStatus(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan != null)
            {
                plan.Actv = !plan.Actv;
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = plan.Actv == true ? "Status Activated Successfully" : "Status Deactivated Successfully";
            }
            return RedirectToAction("SubscriptionPlans");
        }

        [HttpPost]
        public async Task<IActionResult> DeleteSubscriptionPlan(int id)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(id);
            if (plan != null)
            {
                _context.SubscriptionPlans.Remove(plan);
                await _context.SaveChangesAsync();
                TempData["SuccessMsg"] = "Deleted Successfully";
            }
            return RedirectToAction("SubscriptionPlans");
        }

        [HttpGet]
        public async Task<IActionResult> Patients()
        {
            int userId = Convert.ToInt32(HttpContext.Session.GetString("userid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var patients = await (from a in _context.Appointments
                                  join d in _context.Doctors on a.Drid equals d.Drid
                                  where a.Payment == true
                                  orderby a.Aptmtid descending
                                  select new
                                  {
                                      PatientName = a.Pname,
                                      Mobile = a.Mobnum,
                                      Email = "N/A",
                                      DoctorName = d.Drname,
                                      Date = a.Updte
                                  }).ToListAsync();

            return View(patients);
        }
    }
}