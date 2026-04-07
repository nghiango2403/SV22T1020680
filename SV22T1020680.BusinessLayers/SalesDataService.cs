using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.DataLayers.SQLServer;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Sales;

namespace SV22T1020680.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm và phân trang</param>
        /// <returns>Kết quả tìm kiếm và phân trang</returns>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Thông tin đơn hàng hoặc null nếu không tồn tại</returns>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        //public static async Task<int> AddOrderAsync(Order data)
        //{
        //    data.Status = OrderStatusEnum.New;
        //    data.OrderTime = DateTime.Now;
        //
        //  return await orderDB.AddAsync(data);
        //}
        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        /// <param name="customerId">Mã khách hàng</param>
        /// <param name="province">Tỉnh thành giao hàng</param>
        /// <param name="address">Địa chỉ giao hàng</param>
        /// <returns>ID của đơn hàng vừa tạo</returns>
        public static async Task<int> AddOrderAsync(int customerId = 0, string province = "", string address = "")
        {
            var order = new Order()
            {
                CustomerID = customerId == 0 ? null : customerId,
                DeliveryProvince = province,
                DeliveryAddress = address,
                Status = OrderStatusEnum.New,
                OrderTime = DateTime.Now
            };
            return await orderDB.AddAsync(order);
    }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);

            if (order == null)
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");

            OrderStatusEnum status = (OrderStatusEnum)order.Status;

            if (status != OrderStatusEnum.New)
            {
                string statusDescription = OrderStatusExtensions.GetDescription(status);
                throw new Exception($"Không thể xóa đơn hàng đang ở trạng thái '{statusDescription}'.");
            }

            return await orderDB.DeleteAsync(orderID);
        }

        public static async Task<List<OrderViewInfo>> GetOrderByCustomerId(int customerId)
        {
            var order = await orderDB.GetByCustomerId(customerId);
            if (order == null)
                throw new Exception($"Mã khách hàng không tồn tại.");
            return order;
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="employeeID">Mã nhân viên duyệt</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");

            if (order.Status != OrderStatusEnum.New)
                throw new Exception($"Không thể duyệt đơn hàng đang ở trạng thái '{OrderStatusExtensions.GetDescription(order.Status)}'.");

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="employeeID">Mã nhân viên từ chối</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");

            if (order.Status != OrderStatusEnum.New)
                throw new Exception($"Chỉ có thể từ chối các đơn hàng ở trạng thái 'Vừa đặt'. Trạng thái hiện tại: '{OrderStatusExtensions.GetDescription(order.Status)}'.");

            order.EmployeeID = employeeID;
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> CancelOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted)
                throw new Exception($"Không thể hủy đơn hàng đang ở trạng thái '{OrderStatusExtensions.GetDescription(order.Status)}'.");

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="shipperID">Mã người giao hàng</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");

            if (order.Status != OrderStatusEnum.Accepted)
                throw new Exception($"Chỉ có thể chuyển giao hàng cho đơn hàng đã duyệt. Trạng thái hiện tại: '{OrderStatusExtensions.GetDescription(order.Status)}'.");

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");

            if (order.Status != OrderStatusEnum.Shipping)
                throw new Exception($"Chỉ có thể hoàn tất đơn hàng đang được giao. Trạng thái hiện tại: '{OrderStatusExtensions.GetDescription(order.Status)}'.");

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách mặt hàng</returns>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Thông tin chi tiết mặt hàng trong đơn hàng</returns>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        /// <param name="orderId">Mã đơn hàng</param>
        /// <param name="ProductID">Mã mặt hàng</param>
        /// <param name="Quantity">Số lượng</param>
        /// <param name="SalePrice">Giá bán</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> AddDetailAsync(int orderId, int ProductID, int Quantity, decimal SalePrice)
        {
            var data = new OrderDetail()
            {
                OrderID = orderId,
                ProductID = ProductID,
                Quantity = Quantity,
                SalePrice = SalePrice,
            };
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                throw new Exception("Đơn hàng không tồn tại.");

            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                throw new Exception("Trạng thái đơn hàng hiện tại không cho phép thêm mặt hàng.");
            }

            if (data.Quantity <= 0)
                throw new Exception("Số lượng mặt hàng phải lớn hơn 0.");

            if (data.SalePrice < 0)
                throw new Exception("Giá bán không được là số âm.");

            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết đơn hàng</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null)
                throw new Exception("Đơn hàng không tồn tại.");

            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                throw new Exception("Trạng thái đơn hàng hiện tại không cho phép thêm mặt hàng.");
            }

            if (data.Quantity <= 0)
                throw new Exception("Số lượng mặt hàng phải lớn hơn 0.");

            if (data.SalePrice < 0)
                throw new Exception("Giá bán không được là số âm.");
            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>True nếu thành công</returns>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
            {
                throw new Exception($"Đơn hàng mã {orderID} không tồn tại.");
            }
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                throw new Exception("Trạng thái đơn hàng hiện tại không cho phép xóa mặt hàng khỏi đơn hàng.");
            }

            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion
    }
}