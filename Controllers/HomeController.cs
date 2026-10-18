using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Data;
using HelloDoctor.Care.Models;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Linq;
using System.Reflection;

namespace HelloDoctor.Care.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;

        public HomeController(ApplicationDbContext context) { _context = context; }

        public async Task<IActionResult> Index()
        {
            var districts = await _context.Districts.Where(d => d.Actv == true).ToListAsync();
            return View(districts);
        }

        public async Task<IActionResult> Booking(int? did, int? cid, int? depid)
        {
            ViewBag.LocationName = await _context.Locations.Where(l => l.Cid == cid).Select(l => l.Cname).FirstOrDefaultAsync() ?? "Area";
            ViewBag.DeptName = await _context.Departments.Where(d => d.Depid == depid).Select(d => d.Depname).FirstOrDefaultAsync() ?? "Spec";
            return View();
        }

        public async Task<IActionResult> LiveQueue(int? did, int? cid, int? depid)
        {
            ViewBag.LocationName = await _context.Locations.Where(l => l.Cid == cid).Select(l => l.Cname).FirstOrDefaultAsync() ?? "Area";
            return View();
        }

         
        [HttpGet("Home/Doctor/{username}")]
        public async Task<IActionResult> Doctor(string username)
        {
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index");

            
            var result = await (from a in _context.AdminUsers
                                join d in _context.Doctors.Include(doc => doc.Department).Include(doc => doc.Location)
                                on a.Drid equals d.Drid
                                where (d.Mob2 == username || a.Uname == username) && d.Actv == true
                                select new { AdminUser = a, Doctor = d })
                                .FirstOrDefaultAsync();

            if (result == null) return RedirectToAction("Index");

            var adminUser = result.AdminUser;
            var doctor = result.Doctor;

            string currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("dd-MM-yyyy");

             
            var liveQueue = await _context.LiveQueues.FirstOrDefaultAsync(l => l.Drid == adminUser.Userid);
            int liveToken = (liveQueue != null && liveQueue.Updte != null && liveQueue.Updte.StartsWith(currentDate)) ? (liveQueue.LiveToken ?? 0) : 0;

            ViewBag.LiveToken = liveToken;

            return View(doctor);
        }

        [HttpGet]
        public async Task<JsonResult> GetDoctorLiveToken(string username)
        {
            if (string.IsNullOrEmpty(username)) return Json(new { liveToken = 0 });

             
            var adminUser = await (from a in _context.AdminUsers
                                   join d in _context.Doctors on a.Drid equals d.Drid
                                   where (d.Mob2 == username ) && d.Actv == true
                                   select a)
                                   .FirstOrDefaultAsync();

            if (adminUser == null) return Json(new { liveToken = 0 });

            string currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("dd-MM-yyyy");

            var liveQueue = await _context.LiveQueues.FirstOrDefaultAsync(l => l.Drid == adminUser.Userid);
            int liveToken = (liveQueue != null && liveQueue.Updte != null && liveQueue.Updte.StartsWith(currentDate)) ? (liveQueue.LiveToken ?? 0) : 0;

            return Json(new { liveToken = liveToken });
        }

     
   
    
        [HttpGet("dr/{username}")]
        public async Task<IActionResult> VanitySchedule(string username)
        {
            if (string.IsNullOrEmpty(username)) return RedirectToAction("Index");

           
            var result = await _context.Doctors
                .Include(doc => doc.Department)
                .Include(doc => doc.Location)
                .Join(_context.AdminUsers, 
                    d => d.Drid,
                    a => a.Drid,
                    (d, a) => new { Doctor = d, AdminUser = a })
                .FirstOrDefaultAsync(x => (x.Doctor.Mob2 == username  ) && x.Doctor.Actv == true);

            if (result == null) return RedirectToAction("Index");

            var doctor = result.Doctor;
            var adminUser = result.AdminUser;

           
            ViewBag.UserId = adminUser.Userid;
            ViewBag.DepId = doctor.Depid ?? 0;
            ViewBag.PaymentRequired = doctor.Payment ?? false;

             
            return View("Schedule", doctor);
        }

        public async Task<IActionResult> Schedule(int drid)
        {
            var doctor = await (from d in _context.Doctors
                                join a in _context.AdminUsers on d.Drid equals a.Drid
                                where a.Userid == drid
                                select d).Include(d => d.Department).Include(d => d.Location).FirstOrDefaultAsync();

            if (doctor == null) return RedirectToAction("Index");

            ViewBag.UserId = drid;
            ViewBag.DepId = doctor.Depid ?? 0;
            ViewBag.PaymentRequired = doctor.Payment ?? false;

            return View(doctor);
        }

        private string IsValidMobileNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return "";
            string validNumber = new string(number.Where(char.IsDigit).ToArray());
            if (validNumber.Length == 10)
            {
                validNumber = "91" + validNumber;
                return validNumber;
            }
            else if (validNumber.Length == 12 && validNumber.StartsWith("91"))
            {
                return validNumber;
            }
            else if (validNumber.Length == 11 && !validNumber.StartsWith("91"))
            {
                validNumber = "91" + validNumber.Substring(1);
                return validNumber;
            }
            else
            {
                return "";
            }
        }

        [HttpPost]
        public async Task<IActionResult> BookAppointment(int Drid, int Depid, string Aptmtdate, string Aptmtday, int Slotdtid, string Frmtymdt, string Pname, string Mobnum, int Age, string Addr, bool PaymentRequired)
        {
            try
            {
                if (Slotdtid == 0) return RedirectToAction("Schedule", new { drid = Drid });

                 
                var doc = await (from d in _context.Doctors
                                 join a in _context.AdminUsers on d.Drid equals a.Drid
                                 where a.Userid == Drid
                                 select d).Include(d => d.Department).FirstOrDefaultAsync();

                string docName = doc?.Drname ?? "";
                string depName = doc?.Department?.Depname ?? "";
                bool requiresPayment = doc?.Payment ?? false;
                //decimal consultationFee = doc?.cfee ?? 300;
                decimal consultationFee = 10;
                 
                var slotInfo = await (from csd in _context.ConsultationSlotDetails
                                      join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                                      join cd in _context.ConsultationDays on cs.Cnid equals cd.Cnid
                                      join wd in _context.Weekdays on cd.Dayid equals wd.Dayid
                                      where wd.Days == Aptmtday && cs.Userid == Drid && csd.Slotdtid == Slotdtid
                                      select new { cd.Dayid, csd.Token }).FirstOrDefaultAsync();

                int? fetchedDayId = slotInfo?.Dayid;
                int? fetchedToken = slotInfo?.Token;  

                int nextId = (_context.Appointments.Max(a => (int?)a.Aptmtid) ?? 0) + 1;

                var newApt = new Appointment
                {
                    Aptmtid = nextId,
                    Pname = Pname,
                    Mobnum = Mobnum,
                    Depid = Depid,
                    Drid = Drid,
                    Aptmtdate = Aptmtdate,
                    Aptmtday = Aptmtday,
                    Slotdtid = Slotdtid,
                    Addr = Addr,
                    Age = Age,
                    Actv = true,
                      Payment = !requiresPayment,
                    Updte = DateTime.Now.ToString("dd-MM-yyyy hh:mm:ss tt"),
                    Aptmtdayid = fetchedDayId
                };

                _context.Appointments.Add(newApt);
                await _context.SaveChangesAsync();

                string validmobilenumber = IsValidMobileNumber(Mobnum);

                 
                TempData["doctorName"] = docName;
                TempData["aptmtDate"] = Aptmtdate;
                TempData["slotTime"] = Frmtymdt;
                TempData["token"] = fetchedToken?.ToString() ?? "N/A";

                if (requiresPayment)
                {
                    
                    return Redirect($"/Home/Razorpay?name={Pname}&email={Mobnum}&mobile={Mobnum}&address={Addr}&amount={consultationFee}&aptmtid={newApt.Aptmtid}&drid={Drid}");
                }
                else
                {
                     if (!string.IsNullOrEmpty(validmobilenumber))
                    {
                        try
                        {
                            //using (HttpClient client = new HttpClient())
                            //{
                            //    var payload = new
                            //    {
                            //        whatsappnumber = validmobilenumber,
                            //        doctorname = docName,
                            //        department = depName,
                            //        appointmentdate = Aptmtdate,
                            //        slot = Frmtymdt,
                            //        token = fetchedToken?.ToString() ?? ""
                            //    };
                            //    var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                            //    await client.PostAsync("https://drwebhook.hellodoctor.care/api/Chat/BookingSuccessImage", content);
                            //}
                            string doctornamedtl=docName+"("+depName+")";
                            if (!string.IsNullOrEmpty(Mobnum) && Mobnum.Length >= 10)
                            {
                                try
                                {
                                    string message = $"Hello Doctor Appointment Confirmation Doctor:{doctornamedtl}\n Date: {Aptmtdate}\n Time:{Frmtymdt}\n Token: {fetchedToken.ToString()}\n SMS sent by ROYAL EDUCATIONAL";

                                   // string messagee = $"Online Doctor Booking Details :Doctor: {doctorName}({deptName})Date: {aptmtdate}Time: {timeSlot}Token: {fetchedToken}HelloDoctor";
                                    string encodedMessage = Uri.EscapeDataString(message);
                                    string strUrl = $"http://sapteleservices.com/SMS_API/sendsms.php?username= &password= &mobile={Mobnum}&sendername=THBKCN&message={encodedMessage}&routetype=1&tid=1207177201901576067\r\n";

                                    using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                                    {
                                        await client.GetAsync(strUrl);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine("SMS failed: " + ex.Message);
                                }
                            }
                        }
                        catch { }
                    }

                    return RedirectToAction("ThankYou");
                }
            }
            catch (Exception ex)
            {
                return Content($"<h3>Booking Setup Error</h3><p><strong>Message:</strong> {ex.Message}</p><p><strong>Inner Exception:</strong> {ex.InnerException?.Message}</p>", "text/html");
            }
        }

        public IActionResult ThankYou() => View();

        [HttpGet]
        public async Task<JsonResult> GetAvailableDates(int drid)
        {
            var workingDays = await (from cd in _context.ConsultationDays
                                     join wd in _context.Weekdays on cd.Dayid equals wd.Dayid
                                     where cd.Userid == drid && cd.Actv == true
                                     select wd.Days).ToListAsync();

            var leaves = await (from l in _context.Leaves
                                .Where(l => l.Drid == drid)
                                select new { l.Frmdt, l.Todt }).ToListAsync();

            var bkngDay = await _context.BookingDays.FirstOrDefaultAsync(b => b.Userid == drid);
            var bkngHrs = await _context.BookingHours.FirstOrDefaultAsync(b => b.Userid == drid);

            int dayLimit = bkngDay?.Daycnt ?? 30;
            if (dayLimit <= 0) dayLimit = 30;

            int hourNotice = bkngHrs?.Hrcnt ?? 1;

            TimeZoneInfo ist;
            try
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }
            var nowIST = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);

            return Json(new
            {
                workingDays,
                leaves,
                dayLimit = dayLimit,
                hourNotice = hourNotice,
                serverToday = nowIST.ToString("yyyy-MM-dd")
            });
        }

        [HttpGet]
        public async Task<JsonResult> GetSlots(int drid, string date)
        {
            if (!DateTime.TryParse(date, out DateTime selectedDate)) return Json(new List<object>());

            string dayOfWeek = selectedDate.DayOfWeek.ToString();
            string aptmtdate = selectedDate.ToString("dd-MMM-yyyy");

            var bookedSlots = await _context.Appointments
                .Where(a => a.Drid == drid &&
                            a.Aptmtdate == aptmtdate &&
                            a.Actv == true && a.Payment==true &&
                            a.Slotdtid != null)
                .Select(a => a.Slotdtid.Value)
                .ToListAsync();    

            var slots = await (from csd in _context.ConsultationSlotDetails
                               join cs in _context.ConsultationSlots on csd.Slotid equals cs.Slotid
                               join cd in _context.ConsultationDays on cs.Cnid equals cd.Cnid
                               join wd in _context.Weekdays on cd.Dayid equals wd.Dayid
                               where csd.Actv == true
                                  && wd.Days == dayOfWeek
                                  && cs.Userid == drid
                                  && !bookedSlots.Contains(csd.Slotdtid)
                               select new { csd.Slotdtid, csd.Frmtymdt })
                               .ToListAsync();

            TimeZoneInfo ist;
            try
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            }
            catch (TimeZoneNotFoundException)
            {
                ist = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            }

            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ist);

            if (selectedDate.Date == now.Date)
            {
                var bkngHrs = await _context.BookingHours.FirstOrDefaultAsync(b => b.Userid == drid);
                int hourNotice = bkngHrs?.Hrcnt ?? 0;

                string[] formats = { "h:mm tt", "hh:mm tt", "H:mm", "HH:mm" };

                var filteredSlots = slots.Where(s => {
                    if (DateTime.TryParseExact(s.Frmtymdt, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime slotTime))
                    {
                        var fullSlotTime = now.Date.Add(slotTime.TimeOfDay);
                        return (fullSlotTime - now).TotalHours > hourNotice;
                    }
                    return true;
                }).ToList();
                return Json(filteredSlots);
            }

            return Json(slots);
        }

        [HttpGet] public async Task<JsonResult> GetLocations(int did) => Json(await _context.Locations.Where(l => l.Did == did && l.Actv == true).Select(l => new { id = l.Cid, name = l.Cname }).ToListAsync());

        [HttpGet] public async Task<JsonResult> GetDepartments(int cid) => Json(await _context.Doctors.Where(dr => dr.Cid == cid && dr.Actv == true && dr.Department != null).Select(dr => dr.Department).Distinct().Select(d => new { id = d!.Depid, name = d.Depname }).ToListAsync());

        [HttpGet]
        public async Task<JsonResult> GetDoctors(int did, int cid, int depid)
        {
            string currentDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")).ToString("dd-MM-yyyy");

            var doctors = await (from d in _context.Doctors
                                 join a in _context.AdminUsers on d.Drid equals a.Drid
                                 join l in _context.LiveQueues on a.Userid equals l.Drid into lqGroup
                                 from l in lqGroup.DefaultIfEmpty()
                                 where d.Did == did && d.Cid == cid && d.Depid == depid && d.Actv == true
                                 select new
                                 {
                                     id = a.Userid,
                                     address = d.Address,
                                     name = d.Drname,
                                     designation = d.Designation,
                                     photo = d.Photo,
                                     locationName = d.Location != null ? d.Location.Cname : "",
                                     deptName = d.Department != null ? d.Department.Depname : "",
                                     liveToken = (l != null && l.Updte != null && l.Updte.StartsWith(currentDate)) ? l.LiveToken : 0,
                                     cfee = d.cfee
                                 }).ToListAsync();

            return Json(doctors);
        }

       

        [HttpGet]
        public IActionResult Razorpay()
        {
            return View();
        }

        public class RazorpayOrderRequest
        {
            public decimal amount { get; set; }
            public string name { get; set; }
            public string mobile { get; set; }
            public string email { get; set; }
            public int aptmtid { get; set; }
            public int drid { get; set; }
        }

        [HttpPost]
        public async Task<JsonResult> GenerateOrderBooking([FromBody] RazorpayOrderRequest request)
        {
            try
            {
                int am = Convert.ToInt32(request.amount);

                Dictionary<string, object> input = new Dictionary<string, object>();
                string orderss = DateTime.Now.Ticks.ToString();

                input.Add("amount", am * 100); // Amount in paise
                input.Add("currency", "INR");
                input.Add("receipt", orderss);
                input.Add("payment_capture", 1);

                string key = "";
                string secret = "";

                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

                global::Razorpay.Api.RazorpayClient client = new global::Razorpay.Api.RazorpayClient(key, secret);
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

                global::Razorpay.Api.Order order = client.Order.Create(input);
                string orderId = order["id"].ToString();

            
                string constr = _context.Database.GetDbConnection().ConnectionString;

                using (Microsoft.Data.SqlClient.SqlConnection con = new Microsoft.Data.SqlClient.SqlConnection(constr))
                {
                    string query = @"INSERT INTO tbl_PaymentOrder 
                        (emailid, orderid, whatsappnumber, updte, aptmtid, pname, drid, status) 
                        VALUES 
                        (@Email, @OrderId, @Mobile, @updte, @AptmtId, @Name, @drid, @status)";

                    using (Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@OrderId", orderId);
                        cmd.Parameters.AddWithValue("@AptmtId", request.aptmtid);
                        cmd.Parameters.AddWithValue("@Name", request.name ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Mobile", request.mobile ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@Email", request.email ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@updte", istTime.ToString("dd-MM-yyyy hh:mm:ss tt"));
                        cmd.Parameters.AddWithValue("@drid", request.drid.ToString());
                        cmd.Parameters.AddWithValue("@status", "Pending");

                        await con.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return Json(new { orderId = orderId, amount = am * 100 });
            }
            catch (Exception ex)
            {
                return Json(new { orderId = "", amount = 0, error = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> ReturnPayment(string razorpay_payment_id, string razorpay_order_id, string razorpay_signature)
        {
            try
            {
                string key = " ";
                string secret = "";

      
                Dictionary<string, string> attributes = new Dictionary<string, string>
                {
                    { "razorpay_payment_id", razorpay_payment_id },
                    { "razorpay_order_id", razorpay_order_id },
                    { "razorpay_signature", razorpay_signature }
                };

                global::Razorpay.Api.Utils.verifyPaymentSignature(attributes);

                string constr = _context.Database.GetDbConnection().ConnectionString;
                TimeZoneInfo istZone = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
                string updte = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone).ToString("dd-MM-yyyy hh:mm:ss tt");

                int? aptmtid = null;
                string mobile = "";

                using (Microsoft.Data.SqlClient.SqlConnection con = new Microsoft.Data.SqlClient.SqlConnection(constr))
                {
                    await con.OpenAsync();

                 
                    string queryOrder = "UPDATE tbl_PaymentOrder SET status = 'Success', updte = @updte WHERE orderid = @orderId";
                    using (Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand(queryOrder, con))
                    {
                        cmd.Parameters.AddWithValue("@orderId", razorpay_order_id);
                        cmd.Parameters.AddWithValue("@updte", updte);
                        await cmd.ExecuteNonQueryAsync();
                    }

                     
                    using (Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand("SELECT aptmtid, whatsappnumber FROM tbl_PaymentOrder WHERE orderid = @orderId", con))
                    {
                        cmd.Parameters.AddWithValue("@orderId", razorpay_order_id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                aptmtid = reader["aptmtid"] != DBNull.Value ? Convert.ToInt32(reader["aptmtid"]) : null;
                                mobile = reader["whatsappnumber"]?.ToString() ?? "";
                            }
                        }  
                    }

                     
                    if (aptmtid.HasValue)
                    {
                        string queryApt = "UPDATE tbl_appointment SET payment = 1, updte = @updte WHERE aptmtid = @aptmtid";
                        using (Microsoft.Data.SqlClient.SqlCommand cmd = new Microsoft.Data.SqlClient.SqlCommand(queryApt, con))
                        {
                            cmd.Parameters.AddWithValue("@aptmtid", aptmtid.Value);
                            cmd.Parameters.AddWithValue("@updte", updte);
                            await cmd.ExecuteNonQueryAsync();
                        }

                       
                        var aptDetails = await (from a in _context.Appointments
                                                join adm in _context.AdminUsers on a.Drid equals adm.Userid
                                                join d in _context.Doctors on adm.Drid equals d.Drid
                                                join dp in _context.Departments on d.Depid equals dp.Depid into deptGroup
                                                from dp in deptGroup.DefaultIfEmpty()
                                                join csd in _context.ConsultationSlotDetails on a.Slotdtid equals csd.Slotdtid into slotGroup
                                                from csd in slotGroup.DefaultIfEmpty()
                                                where a.Aptmtid == aptmtid.Value
                                                select new
                                                {
                                                    DoctorName = d.Drname,
                                                    DeptName = dp != null ? dp.Depname : "",
                                                    Aptmtdate = a.Aptmtdate,
                                                    Slot = csd != null ? csd.Frmtymdt : "",
                                                    Token = csd != null ? csd.Token.ToString() : ""
                                                }).FirstOrDefaultAsync();

                        if (aptDetails != null)
                        {
                            TempData["doctorName"] = aptDetails.DoctorName;
                            TempData["aptmtDate"] = aptDetails.Aptmtdate;
                            TempData["slotTime"] = aptDetails.Slot;
                            TempData["token"] = aptDetails.Token;

                            string validmobilenumber = IsValidMobileNumberForRazorpay(mobile);
                            if (!string.IsNullOrEmpty(validmobilenumber))
                            {
                                try
                                {
                                    //using (System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient())
                                    //{
                                    //    var payload = new
                                    //    {
                                    //        whatsappnumber = validmobilenumber,
                                    //        doctorname = aptDetails.DoctorName,
                                    //        department = aptDetails.DeptName,
                                    //        appointmentdate = aptDetails.Aptmtdate,
                                    //        slot = aptDetails.Slot,
                                    //        token = aptDetails.Token
                                    //    };
                                    //    var content = new System.Net.Http.StringContent(System.Text.Json.JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                                    //    await httpClient.PostAsync("https://drwebhook.hellodoctor.care/api/Chat/BookingSuccessImage", content);
                                    //}
                                    string doctornamedtl = aptDetails.DoctorName + "(" + aptDetails.DeptName + ")";
                                    if (!string.IsNullOrEmpty(mobile) && mobile.Length >= 10)
                                    {
                                        try
                                        {
                                            string message = $"Hello Doctor Appointment Confirmation Doctor:{doctornamedtl}\n Date: {aptDetails.Aptmtdate}\n Time:{aptDetails.Slot}\n Token: {aptDetails.Token.ToString()}\n SMS sent by ROYAL EDUCATIONAL";

                                              string encodedMessage = Uri.EscapeDataString(message);
                                            string strUrl = $"http://sapteleservices.com/SMS_API/sendsms.php?username= &password= &mobile={mobile}&sendername=THBKCN&message={encodedMessage}&routetype=1&tid=1207177201901576067\r\n";

                                            using (System.Net.Http.HttpClient client = new System.Net.Http.HttpClient())
                                            {
                                                await client.GetAsync(strUrl);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            Console.WriteLine("SMS failed: " + ex.Message);
                                        }
                                    }
                                }
                                catch { }
                            }
                        }
                    }
                }

                TempData["SuccessMessage"] = "Payment Successful! Your appointment is confirmed.";
                return RedirectToAction("ThankYou");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Error verifying payment: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private string IsValidMobileNumberForRazorpay(string number)
        {
            if (string.IsNullOrEmpty(number)) return "";
            string validNumber = new string(number.Where(char.IsDigit).ToArray());
            if (validNumber.Length == 10) return "91" + validNumber;
            if (validNumber.Length == 12 && validNumber.StartsWith("91")) return validNumber;
            if (validNumber.Length == 11 && !validNumber.StartsWith("91")) return "91" + validNumber.Substring(1);
            return "";
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}