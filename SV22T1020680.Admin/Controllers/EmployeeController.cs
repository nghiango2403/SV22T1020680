using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages.Manage;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.HR;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}")]
    public class EmployeeController : Controller
    {
        /// <summary>
        /// Trang quản lý nhân viên
        /// </summary>
        /// <returns>Trang hiển thị danh sách nhân viên và các tùy chọn quản lý</returns>
        private const String EMPLOYEE_SEARCH = "EmployeeSearchInput";
        /// <summary>
        /// Trang quản lý nhân viên
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(EMPLOYEE_SEARCH);
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
            var result = await HRDataService.ListEmployeesAsync(input);
            ApplicationContext.SetSessionData(EMPLOYEE_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Trang tạo mới nhân viên
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin nhân viên mới</returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin nhân viên
        /// </summary>
        /// <param name="id">ID của nhân viên cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của nhân viên đã chọn</returns>


        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu nhân viên (bổ sung hoặc cập nhật)
        /// </summary>
        /// <param name="data"></param>
        /// <param name="uploadPhoto"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");
                else if(new System.Net.Mail.MailAddress(data.Email).Address != data.Email)
                {
                    ModelState.AddModelError(nameof(data.Email), "Email không đúng định dạng");
                }

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    string password = CryptHelper.HashMD5("123456");
                    int id = await HRDataService.AddEmployeeAsync(data);
                    await HRDataService.ChangeEmployeePasswordAsync(data.Email, password);
                    string role = "employee";
                    await HRDataService.ChangeRole(id, role);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xử lý xóa nhân viên
        /// </summary>
        /// <param name="id">ID của nhân viên cần xóa</param>
        /// <returns>Chuyển hướng về trang danh sách nhân viên sau khi xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await HRDataService.DeleteEmployeeAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.CanDelete = !await HRDataService.IsUsedEmployeeAsync(id);
            ViewBag.Title = "Xóa nhân viên";
            return View(model);
        }
        /// <summary>
        /// Trang đổi mật khẩu nhân viên
        /// </summary>
        /// <param name="id">ID của nhân viên cần đổi mật khẩu</param>
        /// <returns>Trang cho phép người dùng nhập mật khẩu cũ và mật khẩu mới để thay đổi mật khẩu của nhân viên đã chọn</returns>
        [HttpGet]
        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
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
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            var hashNewPassword = CryptHelper.HashMD5(new_password);
            await HRDataService.ChangeEmployeePasswordAsync(model.Email, hashNewPassword);
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Trang đổi vai trò nhân viên
        /// </summary>
        /// <param name="id">ID của nhân viên cần đổi vai trò</param>
        /// <returns>Trang cho phép người dùng chọn vai trò mới cho nhân viên đã chọn</returns>
        [HttpGet]
        public async Task<IActionResult> ChangeRole(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            var roles = await HRDataService.GetEmployeeRoleAsync(id);
            if (roles == null) { 
                roles = "";
            }
            ViewBag.Roles = roles;
            return View(model);
        }
        /// <summary>
        /// Xử lý cập nhật vai trò cho nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="roles">Danh sách vai trò được chọn</param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(int id, string[] roles)
        {
            try
            {
                // 1. Chuyển mảng các quyền được chọn thành chuỗi cách nhau bằng dấu phẩy
                // Ví dụ: ["employee", "admin"] -> "employee,admin"
                string strRoles = (roles != null) ? string.Join(",", roles) : "";

                // 2. Gọi Service để cập nhật vào DB
                bool result = await HRDataService.ChangeRole(id, strRoles);

                if (result)
                {
                    // Có thể thêm thông báo thành công vào TempData nếu cần
                    // TempData["Message"] = "Cập nhật quyền thành công";
                    return RedirectToAction("Index");
                }
                else
                {
                    ModelState.AddModelError("", "Không tìm thấy nhân viên hoặc cập nhật thất bại.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Lỗi: " + ex.Message);
            }

            
            return RedirectToAction("ChangeRole", new {id});
        }
    }
}
