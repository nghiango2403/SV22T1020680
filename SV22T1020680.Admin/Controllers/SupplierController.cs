using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.HR;
using SV22T1020680.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator}, {WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        private const String SUPPLIER_SEARCH = "SupplierSearchInput";
        /// <summary>
        /// Trang quản lý nhà cung cấp
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SUPPLIER_SEARCH);
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
            var result = await PartnerDataService.ListSuppliersAsync(input);
            ApplicationContext.SetSessionData(SUPPLIER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Trang tạo mới nhà cung cấp
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin nhà cung cấp mới</returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin nhà cung cấp
        /// </summary>
        ///<param name="id">ID của nhà cung cấp cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của nhà cung cấp đã chọn</returns>
        public async Task<IActionResult> Edit(int id) 
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu nhà cung cấp (cả thêm mới và cập nhật)
        /// </summary>
        /// <param name="data">Thông tin nhà cung cấp</param>
        /// <returns></returns>
        public async Task<IActionResult> SaveData(Supplier data)
        {
            try
            {
                // 1. Thiết lập tiêu đề trang (Dùng lại khi có lỗi)
                ViewBag.Title = data.SupplierID == 0 ? "Bổ sung nhà cung cấp" : "Cập nhật nhà cung cấp";

                // 2. Kiểm tra dữ liệu đầu vào (Input Validation)
                if (string.IsNullOrWhiteSpace(data.SupplierName))
                    ModelState.AddModelError(nameof(data.SupplierName), "Tên nhà cung cấp không được để trống");

                if (string.IsNullOrWhiteSpace(data.ContactName))
                    ModelState.AddModelError(nameof(data.ContactName), "Tên giao dịch không được để trống");

                if (!string.IsNullOrWhiteSpace(data.Email))
                    if (!data.Email.Contains("@")) // Kiểm tra định dạng đơn giản
                    ModelState.AddModelError(nameof(data.Email), "Email không đúng định dạng");
                // 4. Nếu có bất kỳ lỗi nào, trả về View cùng với dữ liệu đã nhập
                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                // 5. Thực thi lưu dữ liệu nếu mọi thứ đều hợp lệ
                if (data.SupplierID == 0)
                    await PartnerDataService.AddSupplierAsync(data);
                else
                    await PartnerDataService.UpdateSupplierAsync(data);

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View("Edit", data);
            }
        }

        /// <summary>
        /// Xử lý xóa nhà cung cấp
        /// </summary>
        /// <param name="id">ID của nhà cung cấp cần xóa</param>
        /// <returns>Chuyển hướng về trang danh sách nhà cung cấp sau khi xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await PartnerDataService.DeleteSupplierAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.CanDelete = !await PartnerDataService.IsUsedSupplierAsync(id);
            ViewBag.Title = "Xóa nhà cung cấp";
            return View(model);
        }
    }
}
