using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Common;
using SV22T1020680.Models.Partner;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho nhà cung cấp (Suppliers) trên SQL Server
    /// </summary>
    public class SupplierRepository : IGenericRepository<Supplier>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo Repository với chuỗi kết nối
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public SupplierRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Thêm một nhà cung cấp mới
        /// </summary>
        /// <param name="data">Dữ liệu nhà cung cấp</param>
        /// <returns>ID của nhà cung cấp vừa tạo</returns>
        public async Task<int> AddAsync(Supplier data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Suppliers(SupplierName, ContactName, Province, Address, Phone, Email)
                            VALUES(@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                            SELECT SCOPE_IDENTITY();";
                var id = await connection.ExecuteScalarAsync<int>(sql, data);
                return id;
            }
        }

        /// <summary>
        /// Xóa nhà cung cấp dựa trên mã ID
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"DELETE FROM Suppliers WHERE SupplierID = @SupplierID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { SupplierID = id });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một nhà cung cấp
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>Đối tượng Supplier hoặc null</returns>
        public async Task<Supplier?> GetAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"SELECT * FROM Suppliers WHERE SupplierID = @SupplierID";
                return await connection.QueryFirstOrDefaultAsync<Supplier>(sql, new { SupplierID = id });
            }
        }

        /// <summary>
        /// Kiểm tra xem nhà cung cấp có đang được sử dụng (có sản phẩm liên quan) hay không
        /// </summary>
        /// <param name="id">Mã nhà cung cấp</param>
        /// <returns>True nếu đã có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM Products WHERE SupplierID = @SupplierID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<bool>(sql, new { SupplierID = id });
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang danh sách nhà cung cấp
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả phân trang</returns>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            var result = new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Tạo tham số cho truy vấn
                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                // Câu lệnh SQL lấy dữ liệu phân trang và tổng số dòng
                var sql = @"
                    SELECT COUNT(*) FROM Suppliers 
                    WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);

                    SELECT * FROM Suppliers 
                    WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                    ORDER BY SupplierName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                if (input.PageSize == 0) // Trường hợp không phân trang
                {
                    sql = @"
                        SELECT COUNT(*) FROM Suppliers 
                        WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue);

                        SELECT * FROM Suppliers 
                        WHERE (SupplierName LIKE @SearchValue) OR (ContactName LIKE @SearchValue)
                        ORDER BY SupplierName;";
                }

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Supplier>()).ToList();
                }
            }

            return result;
        }

        /// <summary>
        /// Cập nhật thông tin nhà cung cấp
        /// </summary>
        /// <param name="data">Dữ liệu cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Suppliers
                            SET SupplierName = @SupplierName,
                                ContactName = @ContactName,
                                Province = @Province,
                                Address = @Address,
                                Phone = @Phone,
                                Email = @Email
                            WHERE SupplierID = @SupplierID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }
    }
}