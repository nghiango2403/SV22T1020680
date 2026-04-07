using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SV22T1020680.Admin.AppCodes;
using SV22T1020680.BusinessLayers;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Sales;
using SV22T1020680.Shop.AppCodes;
using SV22T1020680.Shop.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SV22T1020680.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const String ORDER_SEARCH = "OrderCustomerSearchInput";
        /// <summary>
        /// Lịch sử đơn hàng của khách hàng
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> Index()
        {
            int customerId = int.Parse(User.GetUserData()?.UserId??"0");

            var result = await SalesDataService.GetOrderByCustomerId(customerId);
            if(result == null)
            {
                return NotFound();
            }
            return View(result);
        }
        /// <summary>
        /// Xử lý thanh toán và tạo đơn hàng
        /// </summary>
        /// <param name="deliveryProvince">Tỉnh thành giao hàng</param>
        /// <param name="deliveryAddress">Địa chỉ giao hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress)
        {
            if (string.IsNullOrEmpty(deliveryProvince) || string.IsNullOrEmpty(deliveryAddress))
            {
                ModelState.AddModelError("", "Vui lòng nhập đầy đủ địa chỉ giao hàng.");
                return RedirectToAction("Index");
            }

            var customerId = int.Parse(User.GetUserData()?.UserId!);
            var cart = ShoppingCartService.GetShoppingCart();
            if (cart.Count == 0)
            {
                return Json(new ApiResult(0, "Giỏ hàng đang trống"));
            }
            int orderID = await SalesDataService.AddOrderAsync(customerId, deliveryProvince, deliveryAddress);

            if (orderID > 0)
            {
                foreach (var item in cart)
                {
                    var product = await ProductDataService.GetProductAsync(item.ProductID);
                    if (product != null)
                    {
                        await SalesDataService.AddDetailAsync(orderID, item.ProductID, item.Quantity, item.SalePrice);
                    }
                }
                ShoppingCartService.ClearCart();
                TempData["SuccessMessage"] = "Chúc mừng! Đơn hàng của bạn đã được tiếp nhận.";
                return RedirectToAction("Details", "Order", new { id = orderID });
            }

            return View("Index");
        }

        /// <summary>
        /// Xem chi tiết đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        public async Task<IActionResult> Details(int id)
        {
            // 1. Lấy thông tin chung của đơn hàng (CustomerName, ShipperName...)
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            // 2. Lấy danh sách sản phẩm trong đơn hàng đó (ProductName, Photo...)
            ViewBag.OrderDetails = await SalesDataService.ListDetailsAsync(id);

            return View(order);
        }
        public IActionResult Edit(int id)
        {
            return View();
        }
        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        /// <param name="id">Mã đơn hàng</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> Cancel(int id)
        {
            bool result = await SalesDataService.CancelOrderAsync(id);

            if (result)
            {
                TempData["Message"] = "Đơn hàng đã được hủy thành công.";
            }
            else
            {
                TempData["Error"] = "Không thể hủy đơn hàng này (do trạng thái không cho phép hoặc lỗi hệ thống).";
            }

            return RedirectToAction("Details", new { id = id });
        }
        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        /// <param name="customerId"></param>
        /// <param name="province"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        
        /// <summary>
        /// Trang tạo đơn hàng (Giỏ hàng)
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            var cart = ShoppingCartService.GetShoppingCart();
            return View(cart);
        }
        /// <summary>
        /// Hiển thị nội dung giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ShowOrder()
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
        public async Task<IActionResult> AddCartItem(int productId, int quantity = 1)
        {
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
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
                SalePrice = product.Price,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = product.Photo ?? "nophoto.png"
            };
            ShoppingCartService.AddCartItem(item);
            return Json(new ApiResult(1));
        }
        /// <summary>
        /// Xử lý xóa hàng hóa khỏi giỏ hàng
        /// </summary>
        /// <param name="productId">ID của sản phẩm cần xóa khỏi giỏ hàng</param>
        /// <returns>Chuyển hướng về trang chi tiết đơn hàng sau khi xóa hàng hóa khỏi giỏ hàng</returns>
        public IActionResult DeleteCartItem(int productId = 0)
        {
            ShoppingCartService.RemoveCartItem(productId);
            var cart = ShoppingCartService.GetShoppingCart();

            // 3. Trả về Partial View chứa dữ liệu mới
            return PartialView("ShowOrder", cart);
            //return Json(new ApiResult(1));
        }
        /// <summary>
        /// Cập nhật số lượng mặt hàng trong giỏ hàng
        /// </summary>
        /// <param name="productId">Mã mặt hàng</param>
        /// <param name="quantity">Số lượng mới</param>
        /// <returns></returns>
        public IActionResult UpdateCartItem(int productId, int quantity)
        {
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng không hợp lệ"));
            }
            ShoppingCartService.UpdateCartItem(productId, quantity);
            var cart = ShoppingCartService.GetShoppingCart();

            // 3. Trả về Partial View chứa dữ liệu mới
            return PartialView("ShowOrder", cart);
        }
        /// <summary>
        /// Xử lý xóa toàn bộ giỏ hàng
        /// </summary>
        ///<returns>Chuyển hướng đến giỏ hàng trống rổng</returns>
        public IActionResult ClearCart()
        {
            ShoppingCartService.ClearCart();
            return RedirectToAction("Index", "Product");
        }
    }

}
