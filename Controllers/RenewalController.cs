using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Data;
using HelloDoctor.Care.Models;
using System.Text.Json;
using Razorpay.Api;

namespace HelloDoctor.Care.Controllers
{
    public class RenewalController : Controller
    {
        private readonly ApplicationDbContext _context;

        public RenewalController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var plans = await _context.SubscriptionPlans.Where(p => p.Actv == true).ToListAsync();
            
            ViewBag.UserName = HttpContext.Session.GetString("username") ?? "Doctor";
            ViewBag.Email = HttpContext.Session.GetString("email") ?? "";
            ViewBag.Mobile = HttpContext.Session.GetString("mobile") ?? "";
            ViewBag.Address = HttpContext.Session.GetString("address") ?? "";

            return View(plans);
        }

        [HttpGet]
        public async Task<JsonResult> GetPlanDetails(int pid)
        {
            var plan = await _context.SubscriptionPlans.FindAsync(pid);
            if (plan != null)
            {
                decimal price = 0;
                if (!string.IsNullOrEmpty(plan.Amt))
                {
                    decimal.TryParse(plan.Amt, out price);
                }
                return Json(new { amount = price });
            }
            return Json(new { amount = 0 });
        }

        [HttpPost]
        public JsonResult GenerateOrder([FromBody] JsonElement data)
        {
            decimal amount = 0;
            if (data.TryGetProperty("amount", out JsonElement amountProp))
            {
                string amountStr = amountProp.ToString();
                decimal.TryParse(amountStr, out amount);
            }
            string key = "";
            string secret = "";
         
            RazorpayClient client = new RazorpayClient(key, secret);
             Order order = client.Order.Create(new Dictionary<string, object> { { "amount", amount * 100 }, { "currency", "INR" }, { "receipt", "receipt_1" } });
             string orderId = order["id"].ToString();
            
            
            return Json(new { orderId = orderId, amount = (amount * 100) });
        }

        [HttpPost]
        public async Task<IActionResult> PaymentSuccess(string razorpay_payment_id, string razorpay_order_id, string razorpay_signature, int planId, decimal amountPaid)
        {
             
            int userId = Convert.ToInt32(HttpContext.Session.GetString("subuserid") ?? "0");
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var istZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Kolkata");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, istZone);

       
            var existingSub = await _context.SubscriptionDetails
                .Where(s => s.Drid == userId)
                .OrderByDescending(s => s.Subtodt)
                .FirstOrDefaultAsync();

            DateTime startDate = now;
            if (existingSub != null && existingSub.Subtodt.HasValue && existingSub.Subtodt.Value > now)
            {
                startDate = existingSub.Subtodt.Value.AddDays(1);
            }
            DateTime endDate = startDate.AddMonths(1);  

            int nextId = (_context.SubscriptionDetails.Max(s => (int?)s.Dtid) ?? 0) + 1;

            var newSubDetail = new SubscriptionDetail
            {
                Dtid = nextId,
                Drid = userId,
                Pid = planId,
                Subamt = amountPaid,
                Subfrmdt = startDate,
                Subtodt = endDate,
                Updte = now.ToString("dd-MM-yyyy hh:mm:ss tt"),
                Actv = true
            };
            _context.SubscriptionDetails.Add(newSubDetail);

            
            var user = await _context.AdminUsers.FindAsync(userId);
            if (user != null)
            {
                user.Subactv = true;
            }

            await _context.SaveChangesAsync();
            
            TempData["SuccessMsg"] = "Subscription Renewed Successfully!";
            return RedirectToAction("Index", "Admin");
        }
    }
}
