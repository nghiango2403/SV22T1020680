using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Partner;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho khách hàng (Customers) trên SQL Server
    /// </summary>
    public class CustomerRepository : ICustomerRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public CustomerRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một khách hàng mới
        /// </summary>
        /// <param name="data">Dữ liệu khách hàng</param>
        /// <returns>Mã khách hàng vừa tạo (Identity)</returns>
        public async Task<int> AddAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, IsLocked)
                            VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, @IsLocked);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Xóa một khách hàng dựa trên ID
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Customers WHERE CustomerID = @CustomerID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { CustomerID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một khách hàng
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>Đối tượng Customer hoặc null</returns>
        public async Task<Customer?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Customers WHERE CustomerID = @CustomerID";
                return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { CustomerID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem khách hàng có dữ liệu liên quan (đơn hàng) hay không
        /// </summary>
        /// <param name="id">Mã khách hàng</param>
        /// <returns>True nếu đã phát sinh đơn hàng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Orders WHERE CustomerID = @CustomerID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<bool>(sql, new { CustomerID = id });
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách khách hàng (Tối ưu hiệu suất bằng QueryMultiple)
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang</returns>
        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Customer>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                // Thực thi 2 câu lệnh SQL trong 1 lần kết nối duy nhất
                var sql = @"
                    SELECT COUNT(*) FROM Customers 
                    WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue);

                    SELECT * FROM Customers 
                    WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue)
                    ORDER BY CustomerName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue);

                        SELECT * FROM Customers 
                        WHERE (CustomerName LIKE @SearchValue) OR (ContactName LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY CustomerName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Customer>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Customer data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Customers
                            SET CustomerName = @CustomerName,
                                ContactName = @ContactName,
                                Province = @Province,
                                Address = @Address,
                                Phone = @Phone,
                                Email = @Email,
                                IsLocked = @IsLocked
                            WHERE CustomerID = @CustomerID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra xem email có bị trùng lặp hay không (Tối ưu bằng EXISTS)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">Mã khách hàng (0 nếu là thêm mới, khác 0 nếu là cập nhật)</param>
        /// <returns>True nếu email hợp lệ (không trùng)</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Customers WHERE Email = @Email AND CustomerID <> @CustomerID)
                                SELECT 0 -- Đã tồn tại, không hợp lệ
                            ELSE
                                SELECT 1 -- Hợp lệ";
                return await connection.ExecuteScalarAsync<bool>(sql, new { Email = email, CustomerID = id });
            }
        }
        public async Task<bool> Register(Customer customer, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, IsLocked, Password)
                            VALUES(@CustomerName, @ContactName, @Province, @Address, @Phone, @Email, 0, @Password)";
                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    customer.CustomerName,
                    customer.ContactName,
                    customer.Province,
                    customer.Address,
                    customer.Phone,
                    customer.Email,
                    Password = password
                });
                return rowsAffected > 0;
            }
        }
        /// <summary>
        /// Đổi mật khẩu của nhân viên
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Customers 
                            SET Password = @NewPassword 
                            WHERE Email = @UserName";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    UserName = userName,
                    NewPassword = password
                });
                return rowsAffected > 0;
            }
        }
    }
}