using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Security;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tài khoản người dùng trên SQL Server
    /// </summary>
    public class EmployeeAccountRepository : IUserAccountRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString"></param>
        public EmployeeAccountRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Xác thực tài khoản người dùng (Đăng nhập)
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns>Thông tin tài khoản nếu hợp lệ, ngược lại trả về null</returns>
        public async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Lưu ý: Cấu trúc bảng và tên cột dựa trên giả định hệ thống quản lý nhân viên/khách hàng có tài khoản
                // Ở đây thường sử dụng bảng Employees hoặc một bảng Users riêng biệt.
                // Giả định bảng Employees có thêm cột Password và RoleNames.
                var sql = @"SELECT 
                                CAST(EmployeeID AS NVARCHAR) AS UserId,
                                Email AS UserName,
                                FullName AS DisplayName,
                                Email,
                                Photo,
                                RoleNames
                            FROM Employees
                            WHERE Email = @UserName AND Password = @Password AND IsWorking = 1";

                return await connection.QueryFirstOrDefaultAsync<UserAccount>(sql, new
                {
                    UserName = userName,
                    Password = password
                });
            }
        }

        /// <summary>
        /// Thay đổi mật khẩu của tài khoản
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu mới</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> ChangePasswordAsync(string userName, string password, string oldPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Employees 
                            SET Password = @NewPassword 
                            WHERE Email = @UserName AND Password = @OldPassword";

                var rowsAffected = await connection.ExecuteAsync(sql, new
                {
                    UserName = userName,
                    OldPassword = oldPassword,
                    NewPassword = password
                });
                return rowsAffected > 0;
            }
        }
    }
}