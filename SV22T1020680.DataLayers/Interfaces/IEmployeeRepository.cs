using SV22T1020680.Models.HR;

namespace SV22T1020680.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);
        /// <summary>
        /// Lấy vai trò của nhân viên dựa trên mã ID của nhân viên. Trả về null nếu không tìm thấy hoặc có lỗi xảy ra.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<string?> GetRole(int id);
        /// <summary>
        /// Đổi vai trò của nhân viên dựa trên mã ID của nhân viên. Trả về true nếu đổi thành công, ngược lại trả về false.
        /// </summary>
        public Task<bool> ChangeRole(int id, string role);
        /// <summary>
        /// Đổi mật khẩu của tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<bool> ChangePasswordAsync(string userName, string password);
    }
}
