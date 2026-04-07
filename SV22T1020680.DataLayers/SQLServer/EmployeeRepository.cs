using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.HR;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhân viên (Employees) trên SQL Server
    /// </summary>
    public class EmployeeRepository : IEmployeeRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public EmployeeRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm nhân viên mới
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên</param>
        /// <returns>ID của nhân viên vừa tạo</returns>
        public async Task<int> AddAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
                            VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Xóa nhân viên dựa trên mã ID
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Employees WHERE EmployeeID = @EmployeeID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { EmployeeID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Thông tin nhân viên hoặc null</returns>
        public async Task<Employee?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Employees WHERE EmployeeID = @EmployeeID";
                return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { EmployeeID = id });
            }
        }

        /// <summary>
        /// Kiểm tra nhân viên có dữ liệu liên quan trong hệ thống (ví dụ: đã lập đơn hàng)
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu đang được sử dụng</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Orders WHERE EmployeeID = @EmployeeID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<bool>(sql, new { EmployeeID = id });
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách nhân viên
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm</param>
        /// <returns>Kết quả tìm kiếm và phân trang</returns>
        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Employee>()
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

                // Tối ưu: Đếm tổng số dòng và lấy dữ liệu trong 1 lần thực thi SQL
                var sql = @"
                    SELECT COUNT(*) FROM Employees 
                    WHERE (FullName LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue);

                    SELECT * FROM Employees 
                    WHERE (FullName LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue)
                    ORDER BY FullName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue);

                        SELECT * FROM Employees 
                        WHERE (FullName LIKE @SearchValue) OR (Phone LIKE @SearchValue) OR (Email LIKE @SearchValue)
                        ORDER BY FullName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Employee>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin nhân viên
        /// </summary>
        /// <param name="data">Dữ liệu nhân viên</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Employee data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Employees
                            SET FullName = @FullName,
                                BirthDate = @BirthDate,
                                Address = @Address,
                                Phone = @Phone,
                                Email = @Email,
                                Photo = @Photo,
                                IsWorking = @IsWorking
                            WHERE EmployeeID = @EmployeeID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Kiểm tra email có bị trùng lặp không (không tính email của chính nhân viên đang sửa)
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>True nếu email hợp lệ (không trùng)</returns>
        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Employees WHERE Email = @Email AND EmployeeID <> @EmployeeID)
                                SELECT 0 -- Đã tồn tại (False)
                            ELSE
                                SELECT 1 -- Hợp lệ (True)";
                return await connection.ExecuteScalarAsync<bool>(sql, new { Email = email, EmployeeID = id });
            }
        }
        /// <summary>
        /// Lấy các vai trò của nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <returns>Chuỗi các vai trò của nhân viên</returns>
        public async Task<string?> GetRole(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT RoleNames FROM Employees WHERE EmployeeID = @EmployeeID";
                return await connection.QueryFirstOrDefaultAsync<string?>(sql, new { EmployeeID = id });
            }
        }

        /// <summary>
        /// Thay đổi vai trò của nhân viên
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="role">Chuỗi các vai trò mới</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> ChangeRole(int id, string role)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Employees 
                    SET RoleNames = @RoleNames 
                    WHERE EmployeeID = @EmployeeID";

                int rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    EmployeeID = id,
                    RoleNames = role
                });

                // Nếu có ít nhất 1 dòng bị thay đổi, trả về true
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
                var sql = @"UPDATE Employees 
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