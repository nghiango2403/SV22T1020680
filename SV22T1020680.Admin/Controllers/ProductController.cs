using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using System;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles =$"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ProductController : Controller
    {
        #region Product
        /// <summary>
        /// Trang quản lý sản phẩm
        /// </summary>
        /// <returns>Trang hiển thị danh sách sản phẩm và các tùy chọn quản lý</returns>
        private const String PRODUCT_SEARCH = "ProductSearchInput";
        /// <summary>
        /// Trang quản lý sản phẩm
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = string.Empty,
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = 0,
                    MaxPrice = 0
                };
            }
            ;
            var categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = int.MaxValue,
                SearchValue = string.Empty
            });
            ViewBag.Categories = categories.DataItems;
            var supplier = await PartnerDataService.ListSuppliersAsync(new PaginationSearchInput
            {
                Page = 1,
                PageSize = int.MaxValue,
                SearchValue = string.Empty
            });
            ViewBag.Suppliers = supplier.DataItems;

            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả
        /// </summary>
        /// <param name="page">Trang hiện tại</param>
        /// <param name="searchValue">Giá trị tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }
        /// <summary>
        /// Trang tạo mới sản phẩm
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin sản phẩm mới</returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung hàng hóa";
            return View("Edit");
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của sản phẩm đã chọn</returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật hàng hóa";
            var model = await ProductDataService.GetProductAsync(id);
            if(model == null)
                return RedirectToAction("Index");
            ViewBag.Photos = await ProductDataService.ListPhotosAsync(id)
                     ?? new List<ProductPhoto>();
            ViewBag.Attributes = await ProductDataService.ListAttributesAsync(id)
                     ?? new List<ProductAttribute>();
            ViewBag.ProductID = id;
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu (thêm mới hoặc cập nhật) cho mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu về mặt hàng</param>
        /// <param name="uploadPhoto">File ảnh tải lên</param>
        /// <returns></returns>
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.ProductID == 0 ? "Bổ sung hàng hóa" : "Cập nhật hàng hóa";
                if (string.IsNullOrWhiteSpace(data.ProductName))
                    ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được để trống.");

                if (string.IsNullOrWhiteSpace(data.Unit))
                    ModelState.AddModelError(nameof(data.Unit), "Đơn vị tính không được để trống.");

                if (data.CategoryID <= 0 || data.CategoryID == null)
                    ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng.");

                if (data.SupplierID <= 0 || data.SupplierID == null)
                    ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp.");

                if (data.Price == default(decimal))
                    ModelState.AddModelError(nameof(data.Price), "Vui lòng nhập giá cho mặt hàng.");
                else if (data.Price < 0)
                    ModelState.AddModelError(nameof(data.Price), "Giá hàng không được nhỏ hơn 0.");

                if(string.IsNullOrWhiteSpace(data.ProductDescription))
                    ModelState.AddModelError(nameof(data.ProductDescription), "Giới thiệu không được để trống.");

                if (ModelState.IsValid)
                {
                    return View("Edit", data);
                }
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }
                if(string.IsNullOrWhiteSpace(data.ProductDescription))
                    data.ProductDescription = "";
                if(string.IsNullOrWhiteSpace(data.Photo))
                    data.Photo = "nophoto.png";
                data.Photo = "nophoto.png";

                if (data.ProductID == 0)
                {
                    // Trường hợp thêm mới
                    await ProductDataService.AddProductAsync(data);
                }
                else
                {
                    // Trường hợp cập nhật theo ID
                    await ProductDataService.UpdateProductAsync(data);
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return PartialView("Edit", data);
            }
        }
        /// <summary>
        /// Xử lý xóa sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xóa</param>
        /// <returns>Chuyển hướng về trang danh sách sản phẩm sau khi xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            // 1. Xử lý khi người dùng nhấn xác nhận xóa (POST)
            if (Request.Method == "POST")
            {
                try
                {
                    await ProductDataService.DeleteProductAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", "Hãy xóa các thuộc tính và ảnh của mặt hàng trước");
                }
            }

            // 2. Xử lý hiển thị trang xác nhận xóa (GET)
            var model = await ProductDataService.GetProductAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }

            ViewBag.CanDelete = !await ProductDataService.IsUsedProductAsync(id);
            ViewBag.CategoryName = (await CatalogDataService.GetCategoryAsync(model.CategoryID ?? 0))?.CategoryName;
            ViewBag.SupplierName = (await PartnerDataService.GetSupplierAsync(model.SupplierID ?? 0))?.SupplierName;
            ViewBag.Title = "Xóa mặt hàng";

            return View(model);
        }
        #endregion

        #region Attribute
        /// <summary>
        /// Trang quản lý thuộc tính của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần quản lý thuộc tính</param>
        /// <returns>Trang hiển thị danh sách thuộc tính của sản phẩm và các tùy chọn quản lý</returns>
        public async Task<IActionResult> ListAttributes(int id)
        {
            var model = await ProductDataService.ListAttributesAsync(id) ?? new List<ProductAttribute>();
            ViewBag.ProductID = id;
            return View(model);
        }
        /// <summary>
        /// Trang chỉnh sửa thuộc tính của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần chỉnh sửa thuộc tính</param>
        /// <param name="attributeId">ID của thuộc tính cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của thuộc tính đã chọn</returns>
        public async Task<IActionResult> EditAttribute(int id, int attributeId)
        {
            ViewBag.Title = "Cập nhật thuộc tính";
            var model = await ProductDataService.GetAttributeAsync(attributeId);
            if(model == null)
                return RedirectToAction("ListAttributes", new { id = id });
            return View(model);
        }
        /// <summary>
        /// Xử lý xóa thuộc tính của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xóa thuộc tính</param>
        /// <param name="attributeId">ID của thuộc tính cần xóa</param>
        /// <returns>Chuyển hướng về trang quản lý thuộc tính của sản phẩm sau khi xóa</returns>
        public async Task<IActionResult> DeleteAttribute(int id, int attributeId)
        {
            // 1. Xử lý khi người dùng nhấn xác nhận xóa (POST)
            if (Request.Method == "POST")
            {
                try
                {
                    await ProductDataService.DeleteAttributeAsync(attributeId);
                    return RedirectToAction("Edit", "Product", new { id = id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await ProductDataService.GetAttributeAsync(attributeId);
            if (model == null)
            {
                return RedirectToAction("Edit", "Product", new { id = id });
            }
            ViewBag.Title = "Xóa thuộc tính của mặt hàng";
            model.ProductID= id;
            return View(model);
        }
        /// <summary>
        /// Trang tạo mới thuộc tính của sản phẩm    
        /// </summary>
        /// <param name="id">ID của sản phẩm cần tạo thuộc tính</param>
        /// <returns>Trang cho phép người dùng nhập thông tin thuộc tính mới cho sản phẩm đã chọn</returns>
        public IActionResult CreateAttribute(int id)
        {
            ViewBag.Title = "Bổ sung thuộc tính";
            var model = new ProductAttribute()
            {
                ProductID = id,
                AttributeID = 0,      // ID = 0 để đánh dấu là thêm mới
                AttributeName = string.Empty,
                AttributeValue = string.Empty,
                DisplayOrder = 1
            };
            return View("EditAttribute", model);
        }
        /// <summary>
        /// Lưu dữ liệu (thêm mới hoặc cập nhật) cho thuộc tính của mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu về thuộc tính</param>
        /// <returns></returns>
        public async Task<IActionResult> SaveDataAttribute(ProductAttribute data)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(data.AttributeName))
                    ModelState.AddModelError(nameof(data.AttributeName), "Tên thuộc tính không được để trống.");

                if (string.IsNullOrWhiteSpace(data.AttributeValue))
                    ModelState.AddModelError(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống.");

                if (data.DisplayOrder <= 0)
                    ModelState.AddModelError(nameof(data.DisplayOrder), "Thứ tự hiển thị phải là số nguyên dương.");

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
                    return View("EditAttribute", data);
                }

                if (data.AttributeID == 0)
                {
                    await ProductDataService.AddAttributeAsync(data);
                }
                else
                {
                    await ProductDataService.UpdateAttributeAsync(data);
                }

                return RedirectToAction("Edit", "Product", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                //ModelState.AddModelError("Error", "Hệ thống đang bận hoặc dữ liệu không hợp lệ.");
                ModelState.AddModelError("Error", ex.Message);
                ViewBag.Title = data.AttributeID == 0 ? "Bổ sung thuộc tính" : "Cập nhật thuộc tính";
                return View("EditAttribute", data);
            }
        }
        #endregion

        #region Photo
        /// <summary>
        /// Trang quản lý hình ảnh của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần quản lý hình ảnh</param>
        /// <returns>Trang hiển thị danh sách hình ảnh của sản phẩm và các tùy chọn quản lý</returns>
        public async Task<IActionResult> ListPhotos(int id)
        {
            var model = await ProductDataService.ListPhotosAsync(id) ?? new List<ProductPhoto>(); 
            ViewBag.ProductID = id;
            return View(model);
        }
        /// <summary>
        /// Trang tạo mới hình ảnh của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần tạo hình ảnh</param>
        /// <returns>Trang cho phép người dùng nhập thông tin hình ảnh mới cho sản phẩm đã chọn</returns>
        public IActionResult CreatePhoto(int id)
        {
            ViewBag.Title = "Bổ sung hình ảnh";
            var model = new ProductPhoto()
            {
                ProductID = id,
                PhotoID = 0,      // ID = 0 để đánh dấu là thêm mới
                DisplayOrder = 1,
                IsHidden = false
            };
            return View("EditPhoto", model);
        }
        /// <summary>
        /// Xóa hình ảnh của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần xóa hình ảnh</param>
        /// <param name="photoId">ID của hình ảnh cần xóa</param>
        /// <returns>Chuyển hướng về trang quản lý hình ảnh của sản phẩm sau khi xóa</returns>
        public async Task<IActionResult> DeletePhoto(int id, int photoId) {
            // 1. Xử lý khi người dùng nhấn xác nhận xóa (POST)
            if (Request.Method == "POST")
            {
                try
                {
                    await ProductDataService.DeletePhotoAsync(photoId);
                    return RedirectToAction("Edit", "Product", new { id = id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Error", ex.Message);
                }
            }
            var model = await ProductDataService.GetPhotoAsync(photoId);
            if (model == null)
            {
                return RedirectToAction("Edit", "Product", new { id = id });
            }
            model.ProductID = id;
            ViewBag.Title = "Xóa  ảnh của mặt hàng";

            return View(model);
        }
        /// <summary>
        /// Trang chỉnh sửa hình ảnh của sản phẩm
        /// </summary>
        /// <param name="id">ID của sản phẩm cần chỉnh sửa hình ảnh</param>
        /// <param name="photoId">ID của hình ảnh cần chỉnh sửa</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của hình ảnh đã chọn</returns>
        public async Task<IActionResult> EditPhoto(int id, int photoId) {
            ViewBag.Title = "Cập nhật ảnh của mặt hàng";
            var model = await ProductDataService.GetPhotoAsync(photoId);

            if (model == null)
                return RedirectToAction("Edit", new { id = id });

            // Đảm bảo ảnh này thuộc về mặt hàng đang sửa
            //if (model.ProductID != id)
            //    return RedirectToAction("Edit", new { id = id });
            model.ProductID = id; 
            return View(model);
        }
        /// <summary>
        /// Lưu dữ liệu (thêm mới hoặc cập nhật) cho ảnh của mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu về ảnh</param>
        /// <param name="uploadPhoto">File ảnh tải lên kèm theo</param>
        /// <returns></returns>
        public async Task<IActionResult> SaveDataPhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            try
            {
                // 1. Kiểm tra mô tả
                if (string.IsNullOrWhiteSpace(data.Description))
                    ModelState.AddModelError(nameof(data.Description), "Vui lòng nhập mô tả ảnh.");

                // 2. Kiểm tra file ảnh: Nếu là thêm mới (ID=0) và không có file upload
                if (data.PhotoID == 0 && uploadPhoto == null)
                {
                    ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn file ảnh.");
                }

                if (!ModelState.IsValid)
                {
                    ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh" : "Cập nhật ảnh";
                    return View("EditPhoto", data);
                }

                // 3. Xử lý Upload file
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName; // Gán tên file mới vào model
                }

                // 4. Lưu dữ liệu
                if (data.PhotoID == 0)
                    await ProductDataService.AddPhotoAsync(data);
                else
                    await ProductDataService.UpdatePhotoAsync(data);

                return RedirectToAction("Edit", new { id = data.ProductID });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", "Lỗi hệ thống: " + ex.Message);
                ViewBag.Title = data.PhotoID == 0 ? "Bổ sung ảnh" : "Cập nhật ảnh";
                return View("EditPhoto", data);
            }
        }
        #endregion
    }

}
