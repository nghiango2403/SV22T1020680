using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles =$"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        /// <summary>
        /// Trang quản lý danh mục sản phẩm
        /// </summary>
        /// <returns>Trang hiển thị danh sách danh mục sản phẩm và các tùy chọn quản lý</returns>
        private const String CATEGORY_SEARCH = "CategorySearchInput";
        /// <summary>
        /// Trang quản lý loại hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(CATEGORY_SEARCH);
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
            var result = await CatalogDataService.ListCategoriesAsync(input);
            ApplicationContext.SetSessionData(CATEGORY_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Trang tạo mới danh mục sản phẩm
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin danh mục sản phẩm mới</returns>
        public IActionResult Create()
        {
            return View("Edit");
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin danh mục sản phẩm
        /// </summary>
        /// <param name="id">ID của danh mục sản phẩm cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của danh mục sản phẩm đã chọn</returns>
        public async Task<IActionResult> Edit(int id)
        {
            var model = await CatalogDataService.GetCategoryAsync(id);
            if(model == null)
            {
                return RedirectToAction("Index");
            }
            return View(model);
        }
        /// <summary>
        /// Lưu thông tin loại hàng (bổ sung hoặc cập nhật)
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> SaveData(Category data)
        {
            try
            {
                ViewBag.Title = data.CategoryID == 0 ? "Bổ sung loại hàng" : "Cập nhật loại hàng";
                if (string.IsNullOrWhiteSpace(data.CategoryName))
                {
                    ModelState.AddModelError(nameof(data.CategoryName), "Tên loại hàng không được để trống");
                    return View("Edit", data);
                }

                if (data.CategoryID == 0)
                {
                    // Trường hợp thêm mới
                    await CatalogDataService.AddCategoryAsync(data);
                }
                else
                {
                    // Trường hợp cập nhật theo ID
                    await CatalogDataService.UpdateCategoryAsync(data);
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
        /// Xử lý xóa danh mục sản phẩm
        /// </summary>
        /// <param name="id">ID của danh mục sản phẩm cần xóa</param>
        /// <returns>Chuyển hướng về trang danh sách danh mục sản phẩm sau khi xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await CatalogDataService.DeleteCategoryAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            ViewBag.CanDelete = !await CatalogDataService.IsUsedCategoryAsync(id);
            ViewBag.Title = "Xóa loại hàng";
            return View(model);
        }
    }
}
