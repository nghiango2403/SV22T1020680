using SV22T1020680.Models.Partner;

namespace SV22T1020680.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Customer
    /// </summary>
    public interface ICustomerRepository : IGenericRepository<Customer>
    {
        /// <summary>
        /// Kiểm tra xem một địa chỉ email có hợp lệ hay không?
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="id">
        /// Nếu id = 0: Kiểm tra email của khách hàng mới.
        /// Nếu id <> 0: Kiểm tra email đối với khách hàng đã tồn tại
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int id = 0);
        /// <summary>
        /// Tạo mới một khách hàng và lưu vào CSDL. Trả về true nếu thành công, ngược lại trả về false
        /// </summary>
        /// <param name="customer">Thông tin khách hàng</param>
        /// <param name="password">Mật khẩu</param>
        /// <returns></returns>
        Task<bool> Register(Customer customer, string password);
        /// <summary>
        /// Đổi mật khẩu của tài khoản
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        Task<bool> ChangePasswordAsync(string userName, string password);
    }

}
