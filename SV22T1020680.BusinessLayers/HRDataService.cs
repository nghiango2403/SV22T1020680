using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.DataLayers.SQLServer;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.HR;

namespace SV22T1020680.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống    
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        /// <param name="input">
        /// Thông tin tìm kiếm và phân trang (từ khóa tìm kiếm, trang cần hiển thị, số dòng mỗi trang).
        /// </param>
        /// <returns>
        /// Kết quả tìm kiếm dưới dạng danh sách nhân viên có phân trang.
        /// </returns>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần tìm.</param>
        /// <returns>
        /// Đối tượng Employee nếu tìm thấy, ngược lại trả về null.
        /// </returns>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào hệ thống.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần bổ sung.</param>
        /// <returns>Mã nhân viên được tạo mới.</returns>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Dữ liệu nhân viên không được để trống.");
            if (string.IsNullOrWhiteSpace(data.FullName))
                throw new Exception("Tên nhân viên không được để trống.");
            if (string.IsNullOrWhiteSpace(data.Email))
                throw new Exception("Địa chỉ Email không được để trống.");
            if (data.BirthDate == DateTime.MinValue)
                throw new Exception("Vui lòng nhập ngày sinh.");
            var model = await employeeDB.ValidateEmailAsync(data.Email);
            if(!model)
                throw new Exception("Địa chỉ Email đã tồn tại trong hệ thống.");
            return await employeeDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        /// <param name="data">Thông tin nhân viên cần cập nhật.</param>
        /// <returns>
        /// True nếu cập nhật thành công, ngược lại False.
        /// </returns>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Dữ liệu nhân viên không được để trống.");
            if (string.IsNullOrWhiteSpace(data.FullName))
                throw new Exception("Tên nhân viên không được để trống.");
            if (string.IsNullOrWhiteSpace(data.Email))
                throw new Exception("Địa chỉ Email không được để trống.");
            if (data.BirthDate == DateTime.MinValue)
                throw new Exception("Vui lòng nhập ngày sinh.");
            var model = await employeeDB.ValidateEmailAsync(data.Email);
            if (!model)
                throw new Exception("Địa chỉ Email đã tồn tại trong hệ thống.");

            return await employeeDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một nhân viên dựa vào mã nhân viên.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần xóa.</param>
        /// <returns>
        /// True nếu xóa thành công, False nếu nhân viên đang được sử dụng
        /// hoặc việc xóa không thực hiện được.
        /// </returns>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        /// <param name="employeeID">Mã nhân viên cần kiểm tra.</param>
        /// <returns>
        /// True nếu nhân viên đang được sử dụng, ngược lại False.
        /// </returns>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            return await employeeDB.IsUsedAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// (không bị trùng với email của nhân viên khác).
        /// </summary>
        /// <param name="email">Địa chỉ email cần kiểm tra.</param>
        /// <param name="employeeID">
        /// Nếu employeeID = 0: kiểm tra email đối với nhân viên mới.
        /// Nếu employeeID khác 0: kiểm tra email của nhân viên có mã là employeeID.
        /// </param>
        /// <returns>
        /// True nếu email hợp lệ (không trùng), ngược lại False.
        /// </returns>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }
        /// <summary>
        /// Lấy Chức vụ của nhân viên dựa trên ID (EmployeeID)
        /// </summary>
        /// <param name="id">Id của nhân viên</param>
        /// <returns>Chuỗi danh sách các vai trò (cách nhau bằng dấu phẩy)</returns>
        public static async Task<string?> GetEmployeeRoleAsync(int id)
        {
            return await employeeDB.GetRole(id);
        }
        /// <summary>
        /// Đổi vai trò của nhân viên dựa trên ID (EmployeeID)
        /// </summary>
        /// <param name="id">Mã nhân viên</param>
        /// <param name="role">Chuỗi vai trò mới</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public static async Task<bool> ChangeRole(int id, string role = "")
        {
            return await employeeDB.ChangeRole(id, role);
        }
        /// <summary>
        /// Đổi mật khẩu của tài khoản nhân viên dựa trên tên đăng nhập (Email)
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static async Task<bool> ChangeEmployeePasswordAsync(string userName, string password)
        {
            return await employeeDB.ChangePasswordAsync(userName, password);
        }

        #endregion
    }
}