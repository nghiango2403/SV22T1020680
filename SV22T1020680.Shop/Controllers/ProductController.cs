using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using SV22T1020680.Shop.AppCodes;
using System.Collections.Generic;
using System.Linq;

namespace SV22T1020680.Shop.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        private const String PRODUCT_SEARCH = "ProductSearchInput";
        /// <summary>
        /// Trang chủ sản phẩm (Hiển thị một số danh mục tiêu biểu)
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var categoriesToShow = new List<int> { 1, 2, 3 };
            var model = new Dictionary<int, List<Product>>();

            foreach (var id in categoriesToShow)
            {
                var input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 4,
                    CategoryID = id,
                    SearchValue = "",
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
                var result = await CatalogDataService.ListProductsAsync(input);
                model.Add(id, result.DataItems);
            }

            return View(model);
        }
        /// <summary>
        /// Tìm kiếm sản phẩm
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            ViewBag.Input = input;
            return View(result);
        }
        /// <summary>
        /// Xem chi tiết sản phẩm
        /// </summary>
        /// <param name="id">Mã sản phẩm</param>
        /// <returns></returns>
        public async Task<IActionResult> Detail(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
                return RedirectToAction("Index");

            var category = product.CategoryID.HasValue ? await CatalogDataService.GetCategoryAsync(product.CategoryID.Value) : null;
            var supplier = product.SupplierID.HasValue ? await PartnerDataService.GetSupplierAsync(product.SupplierID.Value) : null;
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            var photos = await CatalogDataService.ListPhotosAsync(id);

            var model = new SV22T1020680.Shop.Models.ProductDetailViewModel()
            {
                Product = product,
                CategoryName = category?.CategoryName ?? "Chưa rõ",
                SupplierName = supplier?.SupplierName ?? "Chưa rõ",
                Attributes = attributes ?? new List<ProductAttribute>(),
                Photos = photos?.Where(p => !p.IsHidden).ToList() ?? new List<ProductPhoto>()
            };

            return View(model);
        }
    }
}
