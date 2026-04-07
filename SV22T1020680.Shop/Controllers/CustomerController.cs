using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Models.Partner;
using SV22T1020680.Shop.AppCodes;

namespace SV22T1020680.Shop.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        /// <summary>
        /// Trang thông tin khách hàng
        /// </summary>
        /// <returns>Trang cho phép người dùng xem thông tin</returns>
        public async Task<IActionResult> Index()
        {
            var userData = User.GetUserData();
            if (userData == null || string.IsNullOrEmpty(userData.UserId) || !int.TryParse(userData.UserId, out int id))
            {
                return RedirectToAction("Login", "Account");
            }
            ViewBag.Title = "Thông tin cá nhân";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Login", "Account");
            }
            return View(model);
        }

        /// <summary>
        /// Trang chỉnh sửa thông tin
        /// </summary>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin</returns>
        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            ViewBag.Title = "Cập nhật thông tin cá nhân";

            var userData = User.GetUserData();

            if (userData == null || !int.TryParse(userData.UserId, out int id))
            {
                return RedirectToAction("Login", "Account");
            }

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(model);
        }

        /// <summary>
        /// Xử lý cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="model">Dữ liệu khách hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Edit(Customer model)
        {
            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError(nameof(model.CustomerName), "Tên khách hàng không được để trống");

            if (string.IsNullOrWhiteSpace(model.ContactName))
                model.ContactName = model.CustomerName;

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email không được để trống.");
            }
            else
            {
                var emailChecker = new System.ComponentModel.DataAnnotations.EmailAddressAttribute();
                if (!emailChecker.IsValid(model.Email))
                {
                    ModelState.AddModelError(nameof(model.Email), "Email không đúng định dạng (Ví dụ: abc@gmail.com).");
                }
            }
            if(model.Province == null)
            {
                ModelState.AddModelError(nameof(model.Province), "Tỉnh/Thành phố không được để trống.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.Province = model.Province ?? "";
                model.Address = model.Address ?? "";
                model.Phone = model.Phone ?? "";
                model.ContactName = model.ContactName ?? "";

                bool isUpdated = await PartnerDataService.UpdateCustomerAsync(model);

                if (isUpdated)
                {
                    var userData = new WebUserData()
                    {
                        UserId = model.CustomerID.ToString(),
                        UserName = model.CustomerName,
                        DisplayName = model.ContactName,
                        Email = model.Email,
                    };

                    // Ghi đè Cookie hiện tại bằng thông tin mới
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        userData.CreatePrincipal(),
                        new AuthenticationProperties { IsPersistent = true }
                    );

                    TempData["Message"] = "Cập nhật thông tin cá nhân thành công!";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Cập nhật dữ liệu thất bại. Vui lòng thử lại.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Hệ thống đang bảo trì chức năng này, hãy thử lại sau");
            }

            return View(model);
        }
        /// <summary>
        /// Trang đăng ký tài khoản khách hàng
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        /// <summary>
        /// Xử lý đăng ký tài khoản khách hàng
        /// </summary>
        /// <param name="model">Thông tin khách hàng</param>
        /// <param name="password">Mật khẩu</param>
        /// <param name="confirm_password">Xác nhận mật khẩu</param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Create(Customer model, string password, string confirm_password = "")
        {
            if (string.IsNullOrEmpty(password))
            {
                ModelState.AddModelError("Password", "Mật khẩu không được để trống.");
            }
            else if (password != confirm_password)
            {
                ModelState.AddModelError("confirm_password", "Mật khẩu xác nhận không khớp.");
            }

            // 2. Kiểm tra các ràng buộc khác
            if (string.IsNullOrWhiteSpace(model.CustomerName))
                ModelState.AddModelError(nameof(model.CustomerName), "Tên khách hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(model.Email))
            {
                ModelState.AddModelError(nameof(model.Email), "Email không được để trống.");
            }
            else
            {
                var emailRegex = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!System.Text.RegularExpressions.Regex.IsMatch(model.Email, emailRegex))
                    ModelState.AddModelError(nameof(model.Email), "Địa chỉ Email không hợp lệ.");
            }

            if (!ModelState.IsValid)
                return View(model);

            try
            {
                // 3. Chuẩn hóa dữ liệu trước khi gọi Service
                model.Province = model.Province ?? "";
                model.Address = model.Address ?? "";
                model.Phone = model.Phone ?? "";
                model.ContactName = model.ContactName ?? "";
                model.IsLocked = false;

                // Mã hóa mật khẩu trước khi lưu
                password = CryptHelper.HashMD5(password??"");

                // 4. Gọi Service
                bool resuilt = await PartnerDataService.RegisterCustomer(model, password);

                if (resuilt)
                {
                    TempData["Message"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login", "Account");
                }
                else
                {
                    ModelState.AddModelError("", "Đăng ký thất bại. Vui lòng thử lại sau.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(model);
        }
    }
}
