using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Partner;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        /// <summary>
        /// Trang quản lý nhà vận chuyển
        /// </summary>
        /// <returns>Trang hiển thị danh sách nhà vận chuyển và các tùy chọn quản lý</returns>
        private const String SHIPPER_SEARCH = "ShipperSearchInput";
        /// <summary>
        /// Trang quản lý nhà vận chuyển
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SHIPPER_SEARCH);
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
            var result = await PartnerDataService.ListShippersAsync(input);
            ApplicationContext.SetSessionData(SHIPPER_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Trang tạo mới nhà vận chuyển
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin nhà vận chuyển mới</returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin nhà vận chuyển
        /// </summary>
        /// <param name="id">ID của nhà vận chuyển cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của nhà vận chuyển đã chọn</returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin người giao hàng";
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu người giao hàng (Thêm mới hoặc Cập nhật)
        /// </summary>
        /// <param name="data">Thông tin người giao hàng từ Form</param>
        /// <returns></returns>
        public async Task<IActionResult> SaveData(Shipper data)
        {
            try
            {
                // 1. Thiết lập tiêu đề trang cho giao diện
                ViewBag.Title = data.ShipperID == 0 ? "Bổ sung người giao hàng" : "Cập nhật người giao hàng";

                // 2. Kiểm tra dữ liệu đầu vào (Input Validation)
                // Kiểm tra tên người giao hàng
                if (string.IsNullOrWhiteSpace(data.ShipperName))
                {
                    ModelState.AddModelError(nameof(data.ShipperName), "Tên người giao hàng không được để trống");
                }


                // 3. Nếu có lỗi validation thì quay lại View để người dùng sửa
                if (!ModelState.IsValid)
                {
                    return View("Edit", data);
                }

                // 4. Thực thi lưu dữ liệu vào Database thông qua Service
                if (data.ShipperID == 0)
                {
                    // Trường hợp thêm mới
                    await PartnerDataService.AddShipperAsync(data);
                }
                else
                {
                    // Trường hợp cập nhật theo ID
                    await PartnerDataService.UpdateShipperAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View("Edit", data);
            }
        }
        /// <summary>
        /// Xử lý xóa nhà vận chuyển
        /// </summary>
        /// <param name="id">ID của nhà vận chuyển cần xóa</param>
        /// <returns>Chuyển hướng về trang danh sách nhà vận chuyển sau khi xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await PartnerDataService.DeleteShipperAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.CanDelete = !await PartnerDataService.IsUsedShipperAsync(id);
            ViewBag.Title = "Xóa người giao hàng";
            return View(model);
        }
    }
}
