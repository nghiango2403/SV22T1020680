using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.DataLayers.SQLServer;
using SV22T1020680.Models.Security;
using System.Threading.Tasks;

namespace SV22T1020680.BusinessLayers
{
    /// <summary>
    /// Các dịch vụ liên quan đến tài khoản người dùng (Đăng nhập, Đổi mật khẩu)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository employeeAccountDB;
        private static readonly IUserAccountRepository customerAccountDB;

        /// <summary>
        /// Khởi tạo Service
        /// </summary>
        static SecurityDataService()
        {
            string connectionString = Configuration.ConnectionString;
            employeeAccountDB = new EmployeeAccountRepository(connectionString);
            customerAccountDB = new CustomerAccountRepository(connectionString);
        }

        #region Employee Account

        /// <summary>
        /// Xác thực tài khoản nhân viên
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu đã mã hóa</param>
        /// <returns>Thông tin tài khoản hoặc null nếu không hợp lệ</returns>
        public static async Task<UserAccount?> AuthorizeEmployeeAsync(string userName, string password)
        {
            return await employeeAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu cho nhân viên
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="newPassword">Mật khẩu mới đã mã hóa</param>
        /// <param name="password">Mật khẩu cũ đã mã hóa (tùy chọn)</param>
        /// <returns>True nếu đổi mật khẩu thành công</returns>
        public static async Task<bool> ChangePasswordEmployeeAsync(string userName, string newPassword, string password)
        {
            return await employeeAccountDB.ChangePasswordAsync(userName, newPassword, password);
        }

        #endregion

        #region Customer Account

        /// <summary>
        /// Xác thực tài khoản khách hàng
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="password">Mật khẩu đã mã hóa</param>
        /// <returns>Thông tin tài khoản hoặc null nếu không hợp lệ</returns>
        public static async Task<UserAccount?> AuthorizeCustomerAsync(string userName, string password)
        {
            return await customerAccountDB.AuthorizeAsync(userName, password);
        }

        /// <summary>
        /// Đổi mật khẩu cho khách hàng
        /// </summary>
        /// <param name="userName">Tên đăng nhập (Email)</param>
        /// <param name="newPassword">Mật khẩu mới đã mã hóa</param>
        /// <param name="password">Mật khẩu cũ đã mã hóa (tùy chọn)</param>
        /// <returns>True nếu đổi mật khẩu thành công</returns>
        public static async Task<bool> ChangePasswordCustomerAsync(string userName, string newPassword, string password)
        {
            return await customerAccountDB.ChangePasswordAsync(userName, newPassword, password);
        }

        #endregion
    }
}