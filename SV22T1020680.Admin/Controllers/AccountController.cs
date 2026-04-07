using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Security;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        /// <summary>
        /// Trang đăng nhập của ứng dụng quản trị
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
        /// <param name="username">Email đăng nhập</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            ViewBag.Username = username;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("Error", "Vui lòng email và mật khẩu.");
                return View();
            }
            if (new System.Net.Mail.MailAddress(username).Address != username)
            {
                ModelState.AddModelError("Error", "Email không đúng định dạng");
                return View();
            }
            string hashedPassword = CryptHelper.HashMD5(password);
            
            var userAccount = await SecurityDataService.AuthorizeEmployeeAsync(username, hashedPassword);
            if (userAccount == null)
            {
                ModelState.AddModelError("Error", "Thông tin đăng nhập không hợp lệ hoặc tài khoản đã bị khóa.");
                return View();
            }
            //Thông tin ghi trên giấy chứng nhận
            var userData = new WebUserData()
            {
                UserId = userAccount.UserId,
                UserName = userAccount.UserName,
                DisplayName = userAccount.DisplayName,
                Email = userAccount.Email,
                Photo = userAccount.Photo,
                Roles = userAccount.RoleNames.Split(',').ToList()
            };
            //Tạo giấy chứng nhận
            var principal = userData.CreatePrincipal();
            //Cấp giấy chứng nhận cho người dùng
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

            return RedirectToAction("Index", "Home");
        }
        /// <summary>
        /// Đăng xuất khỏi hệ thống
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync();
            return RedirectToAction("Login");
        }
        /// <summary>
        /// Trang đổi mật khẩu
        /// </summary>
        /// <returns></returns>
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }
        /// <summary>
        /// Xử lý cập nhật thay đổi mật khẩu
        /// </summary>
        /// <param name="oldPassword">Mật khẩu cũ</param>
        /// <param name="newPassword">Mật khẩu mới</param>
        /// <param name="confirmPassword">Xác nhận mật khẩu mới</param>
        /// <returns></returns>
        [Authorize]
        public async Task<IActionResult> ConfirmChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            try
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
                bool result = await SecurityDataService.ChangePasswordEmployeeAsync(userData?.Email ?? "", newHashedPassword, oldHashedPassword);

                if (result)
                {
                    ViewBag.Message = "Đổi mật khẩu thành công!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("Error", "Mật khẩu cũ không chính xác.");
                    return View("ChangePassword");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Đã xảy ra lỗi: " + ex.Message);
                return View("ChangePassword");

            }
        }
        /// <summary>
        /// Trang báo lỗi khi không có quyền truy cập
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
