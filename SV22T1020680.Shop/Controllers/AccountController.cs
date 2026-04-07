using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Admin;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Shop.AppCodes;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SV22T1020680.Shop.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Trang đăng nhập cho khách hàng
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        /// <summary>
        /// Xử lý đăng nhập
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("", "Vui lòng nhập Email và Mật khẩu.");
                return View();
            }
            if (new System.Net.Mail.MailAddress(email).Address != email)
            {
                ModelState.AddModelError("", "Email không đúng định dạng");
                return View();
            }
            String hashedPassword = CryptHelper.HashMD5(password);

            var userAccount = await SecurityDataService.AuthorizeCustomerAsync(email, hashedPassword);
            if (userAccount == null)
            {
                ModelState.AddModelError("", "Email hoặc Mật khẩu  không đúng hoặc tài khoản đã bị khóa.");
                return View();
            }

            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email
            };

            //Tạo giấy chứng nhận
            var principal = userData.CreatePrincipal();
            //Cấp giấy chứng nhận cho người dùng
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }

        /// <summary>
        /// Đăng xuất
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        public IActionResult ChangePassword()
        {
            return View();
        }
        public async Task<IActionResult> ConfirmChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword))
            {
                ModelState.AddModelError("Error", "Vui lòng nhập đầy đủ thông tin mật khẩu.");
                return View("ChangePassword");
            }

            // 2. Kiểm tra mật khẩu mới và xác nhận mật khẩu có khớp nhau không
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("Error", "Xác nhận mật khẩu mới không khớp.");
                return View("ChangePassword");
            }

            // 3. Lấy UserName của người dùng hiện tại từ Session/Cookie
            var userData = User.GetUserData();

            var oldHashedPassword = CryptHelper.HashMD5(oldPassword);
            var newHashedPassword = CryptHelper.HashMD5(newPassword);
            bool result = await SecurityDataService.ChangePasswordCustomerAsync(userData?.Email??"", newHashedPassword, oldHashedPassword);

            if (result)
            {
                ViewBag.Message = "Đổi mật khẩu thành công!";
                return RedirectToAction("Index", "Product");
            }
            else
            {
                ModelState.AddModelError("Error", "Mật khẩu cũ không chính xác.");
                return View("ChangePassword");
            }
        }
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
