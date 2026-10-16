using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HelloDoctor.Care.Data;
using HelloDoctor.Care.Models;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using System.Net;

namespace HelloDoctor.Care.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly string cryptoKey = "&%#@?,:*"; // Legacy Crypto Key

        public AuthController(ApplicationDbContext context) 
        { 
            _context = context; 
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null) 
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
            {
                string encryptedPassword = EncryptPass(password, cryptoKey);
                var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Uname == username && u.Pwd == encryptedPassword);

                if (user != null)
                {
                    if (user.Subactv == false) {
                         
                        HttpContext.Session.SetString("subuserid", user.Userid.ToString());
                        HttpContext.Session.SetString("username", user.Uname ?? "");
                        HttpContext.Session.SetString("email", user.Email ?? "");
                        
                        HttpContext.Session.SetString("mobile", "9999999999");
                        HttpContext.Session.SetString("address", "Registered Address");
                        
                        return RedirectToAction("Index", "Renewal");
                    }

                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Uname ?? ""),
                        new Claim("UserId", user.Userid.ToString()),
                        new Claim(ClaimTypes.Role, user.Roleid?.ToString() ?? "0")
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, 
                        new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) });

                    HttpContext.Session.SetString("subuserid", user.Userid.ToString());
                    HttpContext.Session.SetString("roleid", user.Roleid?.ToString() ?? "");
                    HttpContext.Session.SetString("username", user.Uname ?? "");
                    HttpContext.Session.SetString("userid", user.Userid.ToString());
                    HttpContext.Session.SetString("edrid", user.Drid?.ToString() ?? "");
                    HttpContext.Session.SetString("email", user.Email ?? "");

                    var cookieOptions = new CookieOptions { Expires = DateTime.Now.AddDays(10) };
                    Response.Cookies.Append("userInfo_UserName", user.Uname ?? "", cookieOptions);
                    Response.Cookies.Append("userInfo_userid", user.Userid.ToString(), cookieOptions);

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
                    
                    
                    return RedirectToAction("Index", "Admin"); 
                }
            }
            ViewBag.Error = "Invalid Username or Password!!";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            var user = await _context.AdminUsers.FirstOrDefaultAsync(u => u.Email == email);
            if (user != null)
            {
                string decryptedPassword = DecryptPass(user.Pwd ?? "", cryptoKey);
                bool isSent = Email_To_Admin(decryptedPassword, user.Uname ?? "", user.Email ?? "");
                if (isSent) TempData["SuccessMsg"] = "PLEASE CHECK YOUR EMAIL";
                else TempData["ErrorMsg"] = "Failed to send email. Check SMTP settings.";
            }
            else TempData["ErrorMsg"] = "INVALID EMAIL ID";
            
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult CreateRole() => View();

        [HttpPost]
        public async Task<IActionResult> CreateRole(Role role)
        {
            if (ModelState.IsValid)
            {
                _context.Roles.Add(role);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(role);
        }

        private byte[] key = { };
        private byte[] IV = { 0x12, 0x34, 0x56, 0x78, 0x90, 0xab, 0xcd, 0xef };

        public string DecryptPass(string stringToDecrypt, string sEncryptionKey)
        {
            byte[] inputByteArray = new byte[stringToDecrypt.Length + 1];
            try
            {
                key = System.Text.Encoding.UTF8.GetBytes(sEncryptionKey.Substring(0, 8));
                DESCryptoServiceProvider des = new DESCryptoServiceProvider();
                inputByteArray = Convert.FromBase64String(stringToDecrypt);
                MemoryStream ms = new MemoryStream();
                CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(key, IV), CryptoStreamMode.Write);
                cs.Write(inputByteArray, 0, inputByteArray.Length);
                cs.FlushFinalBlock();
                System.Text.Encoding encoding = System.Text.Encoding.UTF8;
                return encoding.GetString(ms.ToArray());
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public string EncryptPass(string stringToEncrypt, string SEncryptionKey)
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

        private bool Email_To_Admin(string pwd, string name, string emailId)
        {
            try
            {
                string headph = "+91 0000000000"; 
                string heademail1 = "support@hellodoctor.care"; 

                StringBuilder sb = new StringBuilder();
                sb.Append("<center style='width:100%;'><div style='width:680px; margin:auto;' class='email-container'>");
                sb.Append("<table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center' width='680px' style='width:680px;'><tbody>");
                sb.Append("<tr><td align='right' bgcolor='#333333' style='padding:8px'></td></tr>");
                sb.Append("<tr><td bgcolor='#EEEEEE' style='padding:12px 0;text-align:center'><h2 style='color:#F16622;font-family:sans-serif;font-weight:900;'>HELLO DOCTOR</h2></td></tr></tbody></table>");
                sb.Append("<table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center' width='100%' style='max-width:680px;'><tbody>");
                sb.Append("<tr><td bgcolor='#3eb2cf' align='center'><p style='font-size:40px;font-weight:bold;color:#fff;margin:50px 0 10px; font-family:sans-serif;'>Password Recovered !</p><p style='margin:10px 25px 50px;color: #fff; font-family:sans-serif;'>Your Password Recovered.</p></td></tr>");
                sb.Append("<tr><td style='padding:30px 25px;text-align:center;font-family:sans-serif;color: #333333;background:#EEEEEE;'><table width='100%' border='1' bordercolor='#B9B9B9' cellspacing='0' cellpadding='8'><tbody>");
                sb.Append($"<tr><th width='27%' align='left' bgcolor='#FFFFFF'>Email</th><td width='73%' align='left' bgcolor='#FFFFFF'>{emailId}</td></tr>");
                sb.Append($"<tr><th align='left' bgcolor='#FFFFFF'>Username</th><td align='left' bgcolor='#FFFFFF'>{name}</td></tr>");
                sb.Append($"<tr><th align='left' bgcolor='#FFFFFF'>Password</th><td align='left' bgcolor='#FFFFFF'>{pwd}</td></tr>");
                sb.Append("</tbody></table></td></tr></tbody></table>");
                sb.Append("<table role='presentation' cellspacing='0' cellpadding='0' border='0' align='center' width='100%' style='max-width:680px;'><tbody>");
                sb.Append("<tr><td style='padding:25px 20px; background:#333333;'><table width='100%' border='0' cellspacing='0' cellpadding='0' align='center' style='font-size:12px;text-align:center; color:#fff;'><tbody>");
                sb.Append($"<tr><td> Call Us: {headph} &nbsp;.&nbsp; <a href='mailto:{heademail1}' style='color:#ffffff;'>{heademail1}</a> &nbsp;.&nbsp; Visit Website:<a href='http://hellodoctor.care' style='color:#ffffff;'>hellodoctor.care</a></td></tr>");
                sb.Append($"<tr><td style='color:#ffffff; padding:0;'>Copyright &copy; {DateTime.Now.Year} hellodoctor.care. All Rights Reserved.</td></tr>");
                sb.Append("</tbody></table></td></tr></table></div></center>");

                return true; 
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
