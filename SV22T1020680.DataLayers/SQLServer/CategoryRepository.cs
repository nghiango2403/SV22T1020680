using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho loại hàng (Categories) trên SQL Server
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Bổ sung một loại hàng mới
        /// </summary>
        /// <param name="data">Dữ liệu loại hàng</param>
        /// <returns>ID của loại hàng vừa tạo (Identity)</returns>
        public async Task<int> AddAsync(Category data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Categories(CategoryName, Description)
                            VALUES(@CategoryName, @Description);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Xóa loại hàng dựa trên mã ID
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Categories WHERE CategoryID = @CategoryID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { CategoryID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một loại hàng
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Đối tượng Category hoặc null</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Categories WHERE CategoryID = @CategoryID";
                return await connection.QueryFirstOrDefaultAsync<Category>(sql, new { CategoryID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem loại hàng có đang được sử dụng bởi mặt hàng (Products) nào không
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu đã có sản phẩm thuộc loại hàng này</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Products WHERE CategoryID = @CategoryID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<bool>(sql, new { CategoryID = id });
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách loại hàng
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Category>()
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

                // Sử dụng QueryMultipleAsync để tối ưu hiệu suất (giảm Round-trip)
                var sql = @"
                    SELECT COUNT(*) FROM Categories 
                    WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue);

                    SELECT * FROM Categories 
                    WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue)
                    ORDER BY CategoryName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0)
                {
                    sql = @"
                        SELECT COUNT(*) FROM Categories 
                        WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue);

                        SELECT * FROM Categories 
                        WHERE (CategoryName LIKE @SearchValue) OR (Description LIKE @SearchValue)
                        ORDER BY CategoryName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Category>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Dữ liệu cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Categories
                            SET CategoryName = @CategoryName,
                                Description = @Description
                            WHERE CategoryID = @CategoryID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}