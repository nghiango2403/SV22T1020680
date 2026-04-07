using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles =$"{WebUserRoles.Administrator}")]
    public class CustomerController : Controller
    {
        ///<summary>
        /// Nhập đầu vào tìm kiếm -> Hiển thị kết quả tìm kiếm
        ///</summary>
        /// <returns>Trang hiển thị danh sách khách hàng và các tùy chọn quản lý</returns>
        private const String CUSTOMER_SEARCH = "CustomerSearchInput";
        /// <summary>
        /// Trang quản lý khách hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CUSTOMER_SEARCH);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = string.Empty
                };
            }
            ;

            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <param name="page">Trang hiện tại</param>
        /// <param name="searchValue">Giá trị tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            var result = await PartnerDataService.ListCustomersAsync(input);
            ApplicationContext.SetSessionData(CUSTOMER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Trang tạo mới khách hàng
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin khách hàng mới</returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung khách hàng mới";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin khách hàng
        /// </summary>
        /// <param name="id">ID của khách hàng cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của khách hàng đã chọn</returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }

        /// <summary>
        /// Lưu thông tin khách hàng (thêm mới hoặc cập nhật)
        /// </summary>
        /// <param name="data">Thông tin khách hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            try
            {
                ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng mới" : "Cập nhật thông tin khách hàng";
                // Kiểm tra tên khách hàng
                if (string.IsNullOrWhiteSpace(data.CustomerName))
                    ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng");

                // Kiểm tra tên giao dịch
                if (string.IsNullOrWhiteSpace(data.ContactName))
                    ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");

                // Kiểm tra tỉnh thành (Phải chọn từ Dropdown)
                if (string.IsNullOrWhiteSpace(data.Province))
                    ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn tỉnh/thành phố");

                // Kiểm tra Email (định dạng cơ bản)
                if (string.IsNullOrWhiteSpace(data.Email))
                {
                    ModelState.AddModelError(nameof(data.Email), "Email không được để trống");
                }
                else if (new System.Net.Mail.MailAddress(data.Email).Address != data.Email)
                {
                    ModelState.AddModelError(nameof(data.Email), "Email không đúng định dạng");
                }
                if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = data.CustomerName;
                if (string.IsNullOrWhiteSpace(data.Phone)) data.Phone = "";
                if (string.IsNullOrWhiteSpace(data.Address)) data.Address = "";
                // 2. Kiểm tra logic nghiệp vụ (Ví dụ: Email đã tồn tại trong DB chưa)
                // Giả sử bạn có service kiểm tra trùng lặp
                Console.WriteLine($"Kiểm tra trùng email cho khách hàng mới: {data.Email}");
                Console.WriteLine($"CustomerID: {data.CustomerID}");
                if (data.CustomerID == 0) // Nếu là thêm mới, kiểm tra tất cả email
                {
                    Console.WriteLine($"Kiểm tra trùng email cho khách hàng mới: {data.Email}");
                    Console.WriteLine($"CustomerID: {data.CustomerID}");
                    bool isEmailExist = await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID);
                    if (!isEmailExist)
                    {
                        ModelState.AddModelError(nameof(data.Email), "Email này bị trùng");
                    }
                }
                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }
                data.IsLocked = !data.IsLocked;
                if (data.CustomerID == 0)
                {
                    var password = CryptHelper.HashMD5("123456");
                    // Thêm mới
                    await PartnerDataService.RegisterCustomer(data, password);
                }
                else
                {
                    // Cập nhật
                    await PartnerDataService.UpdateCustomerAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết
                ModelState.AddModelError("Error", ex.Message);
                return View("Edit", data);
            }



        }
        /// <summary>
        /// Trang xử lý xóa khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await PartnerDataService.DeleteCustomerAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.CanDelete = !await PartnerDataService.IsUsedCustomerAsync(id);
            ViewBag.Title = "Xóa khách hàng";
            return View(model);
        }
        /// <summary>
        /// DĐổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id">ID của khách hàng cần đổi mật khẩu</param>
        /// <returns>Trang cho phép người dùng nhập mật khẩu cũ và mật khẩu mới để thay đổi mật khẩu của khách hàng đã chọn</returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }
        /// <summary>
        /// Xử lý đổi mật khẩu khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <param name="new_password">Mật khẩu mới</param>
        /// <param name="confirm_password">Xác nhận mật khẩu mới</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string new_password, string confirm_password)
        {
            if (string.IsNullOrEmpty(new_password))
            {
                ModelState.AddModelError(nameof(new_password), "Mật khẩu mới không được để trống.");
            }
            else if (new_password != confirm_password)
            {
                ModelState.AddModelError("confirm_password", "Mật khẩu xác nhận không khớp.");
            }
            if (!ModelState.IsValid)
            {
                return View();
            }
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            var hashNewPassword = CryptHelper.HashMD5(new_password);
            await PartnerDataService.ChangeCustomerPasswordAsync(model.Email, hashNewPassword);
            return RedirectToAction("Index");
        }
    }
}
