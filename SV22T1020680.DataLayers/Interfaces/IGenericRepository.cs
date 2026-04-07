using SV22T1020680.Models.Common;

namespace SV22T1020680.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu đơn giản trên một
    /// kiểu dữ liệu T nào đó (T là một Entity/DomainModel nào đó)
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IGenericRepository<T> where T : class
    {
        /// <summary>
        /// Truy vấn, tìm kiếm dữ liệu và trả về kết quả dưới dạng được phân trang
        /// </summary>
        /// <param name="input">Đầu vào tìm kiếm, phân trang</param>
        /// <returns>Kết quả tìm kiếm và phân trang</returns>
        Task<PagedResult<T>> ListAsync(PaginationSearchInput input);
        /// <summary>
        /// Lấy dữ liệu của một bản ghi có mã là id (trả về null nếu không có dữ liệu)
        /// </summary>
        /// <param name="id">Mã của dữ liệu cần lấy</param>
        /// <returns>Bản ghi tìm được hoặc null</returns>
        Task<T?> GetAsync(int id);
        /// <summary>
        /// Bổ sung một bản ghi vào bảng trong CSDL
        /// </summary>
        /// <param name="data">Dữ liệu cần bổ sung</param>
        /// <returns>Mã của dòng dữ liệu được bổ sung (thường là IDENTITY)</returns>
        Task<int> AddAsync(T data);
        /// <summary>
        /// Cập nhật một bản ghi trong bảng của CSDL
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        Task<bool> UpdateAsync(T data);
        /// <summary>
        /// Xóa bản ghi có mã là id
        /// </summary>
        /// <param name="id">Mã của bản ghi cần xóa</param>
        /// <returns>True nếu xóa thành công</returns>
        Task<bool> DeleteAsync(int id);
        /// <summary>
        /// Kiểm tra xem một bản ghi có mã là id có dữ liệu liên quan hay không?
        /// </summary>
        /// <param name="id">Mã của bản ghi cần kiểm tra</param>
        /// <returns>True nếu đang được sử dụng ở bảng khác</returns>
        Task<bool> IsUsedAsync(int id);
    }
}
