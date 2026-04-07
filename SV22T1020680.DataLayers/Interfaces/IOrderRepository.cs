using SV22T1020680.Models.Common;
using SV22T1020680.Models.Sales;

namespace SV22T1020680.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các chức năng xử lý dữ liệu cho đơn hàng
    /// </summary>
    public interface IOrderRepository
    {
        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        /// <param name="input">Điều kiện tìm kiếm</param>
        /// <returns>Kết quả tìm kiếm và phân trang</returns>
        Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input);
        /// <summary>
        /// Lấy thông tin 1 đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Thông tin đơn hàng</returns>
        Task<OrderViewInfo?> GetAsync(int orderID);
        /// <summary>
        /// Bổ sung đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng</param>
        /// <returns>Mã đơn hàng được bổ sung</returns>
        Task<int> AddAsync(Order data);
        /// <summary>
        /// Cập nhật đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu đơn hàng</param>
        /// <returns>True nếu cập nhật thành công</returns>
        Task<bool> UpdateAsync(Order data);
        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        Task<bool> DeleteAsync(int orderID);


        /// <summary>
        /// Lấy danh sách mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <returns>Danh sách mặt hàng</returns>
        Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID);
        /// <summary>
        /// Lấy thông tin chi tiết của một mặt hàng trong một đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Thông tin chi tiết</returns>
        Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID);
        /// <summary>
        /// Bổ sung mặt hàng vào đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết</param>
        /// <returns>True nếu thành công</returns>
        Task<bool> AddDetailAsync(OrderDetail data);
        /// <summary>
        /// Cập nhật số lượng và giá bán của một mặt hàng trong đơn hàng
        /// </summary>
        /// <param name="data">Dữ liệu chi tiết</param>
        /// <returns>True nếu thành công</returns>
        Task<bool> UpdateDetailAsync(OrderDetail data);
        /// <summary>
        /// Xóa một mặt hàng khỏi đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>True nếu thành công</returns>
        Task<bool> DeleteDetailAsync(int orderID, int productID);
        /// <summary>
        /// Lấy danh sách đơn hàng của một khách hàng
        /// </summary>
        /// <param name="customerId"></param>
        /// <returns></returns>
        Task<List<OrderViewInfo>> GetByCustomerId(int customerId);
    }
}
