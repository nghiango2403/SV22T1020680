using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Partner;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho người giao hàng (Shippers) trên SQL Server
    /// </summary>
    public class ShipperRepository : IGenericRepository<Shipper>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public ShipperRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một người giao hàng mới
        /// </summary>
        /// <param name="data">Dữ liệu người giao hàng</param>
        /// <returns>ID của người giao hàng vừa được tạo (Identity)</returns>
        public async Task<int> AddAsync(Shipper data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Shippers(ShipperName, Phone)
                            VALUES(@ShipperName, @Phone);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Xóa người giao hàng dựa trên mã ID
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Shippers WHERE ShipperID = @ShipperID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ShipperID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một người giao hàng
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>Đối tượng Shipper hoặc null nếu không tìm thấy</returns>
        public async Task<Shipper?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Shippers WHERE ShipperID = @ShipperID";
                return await connection.QueryFirstOrDefaultAsync<Shipper>(sql, new { ShipperID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem người giao hàng có đang được sử dụng trong bảng Orders hay không
        /// </summary>
        /// <param name="id">Mã người giao hàng</param>
        /// <returns>True nếu đã có đơn hàng sử dụng shipper này</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Orders WHERE ShipperID = @ShipperID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<bool>(sql, new { ShipperID = id });
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách người giao hàng.
        /// Sử dụng QueryMultiple để giảm thiểu round-trip đến database.
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả bao gồm danh sách và tổng số dòng tìm được</returns>
        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Shipper>()
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

                // Tối ưu: Đếm tổng số dòng và lấy dữ liệu trong 1 lần thực thi SQL duy nhất
                var sql = @"
                    SELECT COUNT(*) FROM Shippers 
                    WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);

                    SELECT * FROM Shippers 
                    WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                    ORDER BY ShipperName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                // Trường hợp đặc biệt: PageSize = 0 (lấy tất cả không phân trang)
                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Shippers 
                        WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue);

                        SELECT * FROM Shippers 
                        WHERE (ShipperName LIKE @SearchValue) OR (Phone LIKE @SearchValue)
                        ORDER BY ShipperName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Shipper>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin của người giao hàng
        /// </summary>
        /// <param name="data">Dữ liệu cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Shipper data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Shippers
                            SET ShipperName = @ShipperName,
                                Phone = @Phone
                            WHERE ShipperID = @ShipperID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}