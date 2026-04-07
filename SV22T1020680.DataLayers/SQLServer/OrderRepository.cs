using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Sales;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho đơn hàng trên SQL Server
    /// </summary>
    public class OrderRepository : IOrderRepository
    {
        private readonly string _connectionString;

        public OrderRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Order CRUD

        /// <summary>
        /// Thêm đơn hàng mới
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng</param>
        /// <returns>ID của đơn hàng vừa tạo</returns>
        public async Task<int> AddAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Orders(CustomerID, OrderTime, DeliveryProvince, DeliveryAddress, 
                                               EmployeeID, AcceptTime, ShipperID, ShippedTime, FinishedTime, Status)
                            VALUES(@CustomerID, @OrderTime, @DeliveryProvince, @DeliveryAddress, 
                                   @EmployeeID, @AcceptTime, @ShipperID, @ShippedTime, @FinishedTime, @Status);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Order data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Orders
                            SET CustomerID = @CustomerID,
                                OrderTime = @OrderTime,
                                DeliveryProvince = @DeliveryProvince,
                                DeliveryAddress = @DeliveryAddress,
                                EmployeeID = @EmployeeID,
                                AcceptTime = @AcceptTime,
                                ShipperID = @ShipperID,
                                ShippedTime = @ShippedTime,
                                FinishedTime = @FinishedTime,
                                Status = @Status
                            WHERE OrderID = @OrderID";
                return (await connection.ExecuteAsync(sql, data)) > 0;
            }
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Tối ưu: Xóa chi tiết đơn hàng trước khi xóa đơn hàng (nếu không có Cascade Delete)
                var sql = @"DELETE FROM OrderDetails WHERE OrderID = @OrderID;
                            DELETE FROM Orders WHERE OrderID = @OrderID;";
                return (await connection.ExecuteAsync(sql, new { OrderID = orderID })) > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Thông tin hiển thị của đơn hàng</returns>
        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT o.*, 
                                   c.CustomerName, c.ContactName AS CustomerContactName, c.Email AS CustomerEmail, 
                                   c.Phone AS CustomerPhone, c.Address AS CustomerAddress,
                                   e.FullName AS EmployeeName,
                                   s.ShipperName, s.Phone AS ShipperPhone
                            FROM Orders o
                            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                            WHERE o.OrderID = @OrderID";
                return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { OrderID = orderID });
            }
        }

        /// <summary>
        /// Tìm kiếm đơn hàng (phân trang)
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm</param>
        /// <returns>Danh sách đơn hàng và tổng số dòng</returns>
        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            var result = new PagedResult<OrderViewInfo>() { Page = input.Page, PageSize = input.PageSize };
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new
                {
                    Status = (int)input.Status,
                    DateFrom = input.DateFrom,
                    DateTo = input.DateTo,
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                var sql = @"
                    -- Đếm tổng số đơn hàng thỏa điều kiện lọc
                    SELECT COUNT(*) 
                    FROM Orders o
                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                    WHERE (@Status = 0 OR o.Status = @Status)
                      AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                      AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                      AND (c.CustomerName LIKE @SearchValue OR e.FullName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue);

                    -- Lấy dữ liệu phân trang
                    SELECT o.*, 
                           c.CustomerName, c.ContactName AS CustomerContactName, c.Email AS CustomerEmail, 
                           c.Phone AS CustomerPhone, c.Address AS CustomerAddress,
                           e.FullName AS EmployeeName,
                           s.ShipperName, s.Phone AS ShipperPhone
                    FROM Orders o
                    LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
                    LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
                    LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
                    WHERE (@Status = 0 OR o.Status = @Status)
                      AND (@DateFrom IS NULL OR o.OrderTime >= @DateFrom)
                      AND (@DateTo IS NULL OR o.OrderTime <= @DateTo)
                      AND (c.CustomerName LIKE @SearchValue OR e.FullName LIKE @SearchValue OR s.ShipperName LIKE @SearchValue)
                    ORDER BY o.OrderTime DESC, o.OrderID DESC
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<OrderViewInfo>()).ToList();
                }
            }
            return result;
        }

        /// <summary>
        /// Lấy danh sách đơn hàng của một khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        public async Task<List<OrderViewInfo>> GetByCustomerId(int customerId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var sql = @"
            SELECT o.*, 
                   c.CustomerName, c.ContactName AS CustomerContactName, c.Email AS CustomerEmail, 
                   c.Phone AS CustomerPhone, c.Address AS CustomerAddress,
                   e.FullName AS EmployeeName,
                   s.ShipperName, s.Phone AS ShipperPhone
            FROM Orders o
            LEFT JOIN Customers c ON o.CustomerID = c.CustomerID
            LEFT JOIN Employees e ON o.EmployeeID = e.EmployeeID
            LEFT JOIN Shippers s ON o.ShipperID = s.ShipperID
            WHERE o.CustomerID = @CustomerId
            ORDER BY o.OrderTime DESC, o.OrderID DESC;";

                var result = await connection.QueryAsync<OrderViewInfo>(sql, new { CustomerId = customerId });

                return result.ToList();
            }
        }

        #endregion

        #region Order Details CRUD

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách chi tiết đơn hàng</returns>
        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                            FROM OrderDetails od
                            JOIN Products p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID
                            ORDER BY p.ProductName";
                return (await connection.QueryAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID })).ToList();
            }
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Chi tiết mặt hàng hoặc null nếu không tồn tại</returns>
        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT od.*, p.ProductName, p.Unit, p.Photo
                            FROM OrderDetails od
                            JOIN Products p ON od.ProductID = p.ProductID
                            WHERE od.OrderID = @OrderID AND od.ProductID = @productID";
                return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { OrderID = orderID, ProductID = productID });
            }
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng (hoặc cập nhật nếu đã tồn tại)
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Tối ưu: Sử dụng MERGE hoặc kiểm tra trùng để tránh lỗi Primary Key (OrderID, ProductID)
                var sql = @"IF EXISTS (SELECT 1 FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID)
                                UPDATE OrderDetails 
                                SET Quantity = Quantity + @Quantity, SalePrice = @SalePrice
                                WHERE OrderID = @OrderID AND ProductID = @ProductID
                            ELSE
                                INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                                VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";
                return (await connection.ExecuteAsync(sql, data)) > 0;
            }
        }

        /// <summary>
        /// Cập nhật chi tiết đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE OrderDetails
                            SET Quantity = @Quantity, SalePrice = @SalePrice
                            WHERE OrderID = @OrderID AND ProductID = @ProductID";
                return (await connection.ExecuteAsync(sql, data)) > 0;
            }
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM OrderDetails WHERE OrderID = @OrderID AND ProductID = @ProductID";
                return (await connection.ExecuteAsync(sql, new { OrderID = orderID, ProductID = productID })) > 0;
            }
        }

        #endregion
    }
}