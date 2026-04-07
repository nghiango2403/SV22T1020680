using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Partner;
using SV22T1020680.Models.Sales;
using System.Threading.Tasks;

namespace SV22T1020680.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Sales},{WebUserRoles.Administrator}")]
    public class OrderController : Controller
    {
        private string PRODUCT_SEARCH = "ProductSearch";
        /// <summary>
        /// Trang quản lý đơn hàng
        /// </summary>
        /// <returns>Trang hiển thị danh sách đơn hàng và các tùy chọn quản lý</returns>
        private const String ORDER_SEARCH = "OrderSearchInput";
        public async Task<IActionResult> Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(ORDER_SEARCH);
            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = string.Empty,
                    Status = 0,
                    DateFrom = null,
                    DateTo = null
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
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            var result = await SalesDataService.ListOrdersAsync(input);
            ApplicationContext.SetSessionData(ORDER_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Tìm kiếm mặt hàng để đưa vào giỏ hàng
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm</param>
        /// <returns></returns>
        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(PRODUCT_SEARCH, input);
            return View(result);
        }

        /// <summary>
        /// Trang tạo mới đơn hàng
        /// </summary>
        /// <returns>Trang cho phép người dùng nhập thông tin để tạo mới đơn hàng</returns>
        public async Task<IActionResult> Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(PRODUCT_SEARCH);
            if (input == null)
            {
                input = new ProductSearchInput
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = string.Empty,
                };
            }
            var customers = await PartnerDataService.ListCustomersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = string.Empty });
            ViewBag.Customers = customers.DataItems;
            return View(input);
        }
        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }
        /// <summary>
        /// Thêm hàng hóa vào giỏ hàng
        /// </summary>
        /// <param name="productId">Mã hàng hóa</param>
        /// <param name="quantity">Số lượng</param>
        /// <param name="price">Giá</param>
        /// <returns></returns>
        public async Task<IActionResult> AddCartItem(int productId, int quantity, decimal price)
        {
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            }
            if (price < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            }
            var product = await CatalogDataService.GetProductAsync(productId);
            if (product == null)
            {
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));
            }
            if (!product.IsSelling)
            {
                return Json(new ApiResult(0, "Mặt hàng đã ngừng bán"));
            }
            var item = new OrderDetailViewInfo()
            {
                ProductID = productId,
                Quantity = quantity,
                SalePrice = price,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png"
            };
            ShoppingCartService.AddCartItem(item);
            return Json(new ApiResult(1));
        }
        /// <summary>
        /// Trang hiển thị chi tiết đơn hàng
        /// </summary>
        /// <param name="id">ID của đơn hàng cần hiển thị chi tiết</param>
        /// <returns>Trang hiển thị thông tin chi tiết của đơn hàng đã chọn</returns>
        public async Task<IActionResult> Detail(int id)
        {
            var model = await SalesDataService.GetOrderAsync(id);
            if (model == null)
            {
                return RedirectToAction("Index");
            }
            var details = await SalesDataService.ListDetailsAsync(id);
            if (details == null)
            {
                return RedirectToAction("Index");
            }

            if (TempData.ContainsKey("Error"))
            {
                ModelState.AddModelError("Error", TempData["Error"]?.ToString() ?? "");
            }

            ViewBag.Order = model;
            return View(details);
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin hàng hóa trong giỏ hàng
        /// </summary>
        /// <param name="id">ID của giỏ hàng cần chỉnh sửa</param>
        /// <param name="productId">ID của sản phẩm cần chỉnh sửa trong giỏ hàng</param>
        /// <returns>Trang cho phép người dùng chỉnh sửa thông tin của sản phẩm đã chọn trong giỏ hàng</returns>
        public IActionResult EditCartItem(int id, int productId)
        {
            var item = ShoppingCartService.GetCartItem(productId);
            return View(item);
        }
        /// <summary>
        /// Cập nhật số lượng và giá bán của mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="productId">Mã mặt hàng</param>
        /// <param name="quantity">Số lượng mới</param>
        /// <param name="salePrice">Giá bán mới</param>
        /// <returns></returns>
        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            if (quantity <= 0)
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            if (salePrice < 0)
                return Json(new ApiResult(0, "Giá bán không hợp lệ"));
            ShoppingCartService.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1));
        }
        /// <summary>
        /// Xử lý xóa hàng hóa khỏi giỏ hàng
        /// </summary>
        /// <param name="productId">ID của sản phẩm cần xóa khỏi giỏ hàng</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi xóa hàng hóa khỏi giỏ hàng</returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.RemoveCartItem(productId);
                return Json(new ApiResult(1));
            }
            var item = ShoppingCartService.GetCartItem(productId);
            return PartialView(item);
        }
        /// <summary>
        /// Xử lý xóa toàn bộ giỏ hàng
        /// </summary>
        /// <returns>Chuyển hướng đến giỏ hàng trống rổng</returns>
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartService.ClearCart();
                return Json(new ApiResult(1));
            }
            return View();
        }
        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="province"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<IActionResult> CreateOrder(int customerId = 0, string province = "", string address = "")
        {
            try
            {
                var cart = ShoppingCartService.GetShoppingCart();
                if (cart.Count == 0)
                {
                    return Json(new ApiResult(0, "Giỏ hàng đang trống"));
                }
                if (customerId <= 0)
                {
                    return Json(new ApiResult(0, "Vui lòng chọn khách hàng"));
                }
                if(string.IsNullOrEmpty(province))
                {
                    return Json(new ApiResult(0, "Vui lòng nhập tỉnh thành"));
                }
                if (string.IsNullOrEmpty(address)) 
                { 
                    return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng"));
                }

                int orderId = await SalesDataService.AddOrderAsync(customerId, province, address);
                // Bổ sung chi tiết vào đơn hàng
                foreach (var item in cart)
                {
                    await SalesDataService.AddDetailAsync(orderId, item.ProductID, item.Quantity, item.SalePrice);
                }
                ShoppingCartService.ClearCart();
                //Trả về kết quả thành công với code là mã đơn hàng mới
                return Json(new ApiResult(orderId));
            }
            catch (Exception ex)
            {
                return Json(new ApiResult(0, ex.Message));
            }
        }
        /// <summary>
        /// Chấp nhận đơn hàng
        /// </summary>
        /// <param name="id">ID của đơn hàng cần chấp nhận</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi chấp nhận đơn hàng</returns>
        public async Task<IActionResult> Accept(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    int employeeId = int.Parse(User.GetUserData()?.UserId);
                    await SalesDataService.AcceptOrderAsync(id, employeeId);
                    return RedirectToAction("Detail", new { id });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Detail", new { id });
                }
            }
            ViewBag.OrderID = id;
            return PartialView();
        }
        /// <summary>
        /// Chuyển trạng thái đơn hàng sang đang giao hàng
        /// </summary>
        /// <param name="id">ID của đơn hàng cần chuyển trạng thái</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi chuyển trạng thái đơn hàng</returns>
        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            var model = await PartnerDataService.ListShippersAsync(new PaginationSearchInput() { Page = 1, PageSize = 0, SearchValue = string.Empty });
            if (model == null)
            {
                model = new PagedResult<Shipper>();
            }
            ViewBag.OrderID = id;
            return View(model);
        }
        /// <summary>
        /// Xác nhận chuyển đơn hàng sang trạng thái đang giao hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <param name="shipperId">Mã người giao hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Shipping(int id, int shipperId)
        {
            try
            {
                if (shipperId <= 0)
                    throw new Exception("Vui lòng chọn người giao hàng.");

                await SalesDataService.ShipOrderAsync(id, shipperId);
                return RedirectToAction("Detail", new { id });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Detail", new { id });
            }
        }
        /// <summary>
        /// Chuyển trạng thái đơn hàng sang đã hoàn thành
        /// </summary>
        /// <param name="id">ID của đơn hàng cần chuyển trạng thái</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi chuyển trạng thái đơn hàng</returns>
        public async Task<IActionResult> Finish(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await SalesDataService.CompleteOrderAsync(id);
                    return RedirectToAction("Detail", new { id });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Detail", new { id });
                }
            }

            ViewBag.OrderID = id;
            return PartialView();
        }
        /// <summary>
        /// Chuyển trạng thái đơn hàng sang đã từ chối
        /// </summary>
        /// <param name="id">ID của đơn hàng cần chuyển trạng thái</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi chuyển trạng thái đơn hàng</returns>
        public async Task<IActionResult> Reject(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    int employeeId = int.Parse(User.GetUserData()?.UserId);
                    await SalesDataService.RejectOrderAsync(id, employeeId);
                    return RedirectToAction("Detail", new { id });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Detail", new { id });
                }
            }
            ViewBag.OrderID = id;
            return PartialView();
        }
        /// <summary>
        /// Chuyển trạng thái đơn hàng sang đã hủy
        /// </summary>
        /// <param name="id">ID của đơn hàng cần chuyển trạng thái</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi chuyển trạng thái đơn hàng</returns>
        public async Task<IActionResult> Cancel(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await SalesDataService.CancelOrderAsync(id);
                    return RedirectToAction("Detail", new { id });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Detail", new { id });
                }
            }
            ViewBag.OrderID = id;
            return PartialView();
        }
        /// <summary>
        /// Xử lý xóa đơn hàng
        /// </summary>
        /// <param name="id">ID của đơn hàng cần xóa</param>
        /// <returns>Chuyển hướng về trang danh sách đơn hàng sau khi xóa</returns>
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await SalesDataService.DeleteOrderAsync(id);
                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Detail", new { id });
                }

            }
            ViewBag.OrderId = id;
            return PartialView();
        }
        /// <summary>
        /// Trang chỉnh sửa thông tin mặt hàng đã có trong đơn hàng (trước khi giao hàng)
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <param name="productId">Mã mặt hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> EditProductInOrder(int id, int productId)
        {
            var item = await SalesDataService.GetDetailAsync(id, productId);
            ViewBag.OrderID = id;
            return View(item);
        }
        /// <summary>
        /// Cập nhật thông tin mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <param name="ProductId">Mã mặt hàng</param>
        /// <param name="Quantity">Số lượng</param>
        /// <param name="SalePrice">Giá bán</param>
        /// <returns></returns>
        public async Task<IActionResult> UpdateProductInOrder(int id, int ProductId, int Quantity, decimal SalePrice)
        {
            if (Quantity <= 0)
                ModelState.AddModelError("Quantity", "Số lượng không hợp lệ");
            if (SalePrice < 0)
                ModelState.AddModelError("SalePrice", "Giá bán không hợp lệ");
            var data = new OrderDetail()
            {
                OrderID = id,
                ProductID = ProductId,
                Quantity = Quantity,
                SalePrice = SalePrice
            };
            if (!ModelState.IsValid)
            {
                return View("EditProductInOrder", data);
            }

            try
            {
                await SalesDataService.UpdateDetailAsync(data);
                return RedirectToAction("Detail", new { id = id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("Error", ex.Message);
                return View("EditProductInOrder", data);
            }
        }
        /// <summary>
        /// Xóa mặt hàng ra khỏi đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <param name="productId">Mã mặt hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> DeleteProductInOrder(int id, int productId)
        {
            if (Request.Method == "POST")
            {
                try
                {
                    await SalesDataService.DeleteDetailAsync(id, productId);
                    return RedirectToAction("Detail", new { id = id });
                }
                catch (Exception ex)
                {
                    TempData["Error"] = ex.Message;
                    return RedirectToAction("Detail", new { id = id });
                }
            }
            var item = await SalesDataService.GetDetailAsync(id, productId);
            ViewBag.OrderID = id;
            return View(item);
        }

    }
}
