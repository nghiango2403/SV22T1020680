using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using System.Data;

namespace SV22T1020680.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu cho mặt hàng (Products) trên SQL Server
    /// </summary>
    public class ProductRepository : IProductRepository
    {
        private readonly string _connectionString;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="connectionString"></param>
        public ProductRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        #region Product CRUD

        /// <summary>
        /// Thêm mặt hàng mới
        /// </summary>
        public async Task<int> AddAsync(Product data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO Products(ProductName, ProductDescription, SupplierID, CategoryID, Unit, Price, Photo, IsSelling)
                            VALUES(@ProductName, @ProductDescription, @SupplierID, @CategoryID, @Unit, @Price, @Photo, @IsSelling);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<int>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        public async Task<bool> UpdateAsync(Product data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE Products
                            SET ProductName = @ProductName,
                                ProductDescription = @ProductDescription,
                                SupplierID = @SupplierID,
                                CategoryID = @CategoryID,
                                Unit = @Unit,
                                Price = @Price,
                                Photo = @Photo,
                                IsSelling = @IsSelling
                            WHERE ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, data);
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Xóa mặt hàng
        /// </summary>
        public async Task<bool> DeleteAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                // Lưu ý: Trong thực tế nên xóa cả Photos và Attributes trước khi xóa Product nếu không dùng Cascade Delete ở DB
                var sql = "DELETE FROM Products WHERE ProductID = @ProductID";
                var rowsAffected = await connection.ExecuteAsync(sql, new { ProductID = productID });
                return rowsAffected > 0;
            }
        }

        /// <summary>
        /// Lấy thông tin 1 mặt hàng
        /// </summary>
        public async Task<Product?> GetAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM Products WHERE ProductID = @ProductID";
                return await connection.QueryFirstOrDefaultAsync<Product>(sql, new { ProductID = productID });
            }
        }

        /// <summary>
        /// Kiểm tra mặt hàng có dữ liệu liên quan trong đơn hàng (OrderDetails) không
        /// </summary>
        public async Task<bool> IsUsedAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"IF EXISTS (SELECT 1 FROM OrderDetails WHERE ProductID = @ProductID)
                                SELECT 1
                            ELSE
                                SELECT 0";
                return await connection.ExecuteScalarAsync<bool>(sql, new { ProductID = productID });
            }
        }

        /// <summary>
        /// Tìm kiếm và phân trang mặt hàng (Tối ưu hiệu suất)
        /// </summary>
        public async Task<PagedResult<Product>> ListAsync(ProductSearchInput input)
        {
            var result = new PagedResult<Product>() { Page = input.Page, PageSize = input.PageSize };
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    CategoryID = input.CategoryID,
                    SupplierID = input.SupplierID,
                    MinPrice = input.MinPrice,
                    MaxPrice = input.MaxPrice,
                    Offset = input.Offset,
                    PageSize = input.PageSize
                };

                var sql = @"
                    SELECT COUNT(*) FROM Products 
                    WHERE (ProductName LIKE @SearchValue)
                      AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                      AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                      AND (Price >= @MinPrice)
                      AND (@MaxPrice <= 0 OR Price <= @MaxPrice);

                    SELECT * FROM Products 
                    WHERE (ProductName LIKE @SearchValue)
                      AND (@CategoryID = 0 OR CategoryID = @CategoryID)
                      AND (@SupplierID = 0 OR SupplierID = @SupplierID)
                      AND (Price >= @MinPrice)
                      AND (@MaxPrice <= 0 OR Price <= @MaxPrice)
                    ORDER BY ProductName
                    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

                using (var multi = await connection.QueryMultipleAsync(sql, parameters))
                {
                    result.RowCount = await multi.ReadFirstAsync<int>();
                    result.DataItems = (await multi.ReadAsync<Product>()).ToList();
                }
            }
            return result;
        }

        #endregion

        #region Product Photos

        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách ảnh</returns>
        public async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductPhotos WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                return (await connection.QueryAsync<ProductPhoto>(sql, new { ProductID = productID })).ToList();
            }
        }

        /// <summary>
        /// Lấy thông tin một ảnh
        /// </summary>
        /// <param name="photoID">Mã ảnh</param>
        /// <returns>Thông tin ảnh</returns>
        public async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductPhotos WHERE PhotoID = @PhotoID";
                return await connection.QueryFirstOrDefaultAsync<ProductPhoto>(sql, new { PhotoID = photoID });
            }
        }

        /// <summary>
        /// Thêm ảnh cho mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu ảnh</param>
        /// <returns>ID của ảnh vừa tạo</returns>
        public async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO ProductPhotos(ProductID, Photo, Description, DisplayOrder, IsHidden)
                            VALUES(@ProductID, @Photo, @Description, @DisplayOrder, @IsHidden);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<long>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thông tin ảnh
        /// </summary>
        /// <param name="data">Dữ liệu ảnh</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE ProductPhotos 
                            SET Photo = @Photo, Description = @Description, DisplayOrder = @DisplayOrder, IsHidden = @IsHidden
                            WHERE PhotoID = @PhotoID";
                return (await connection.ExecuteAsync(sql, data)) > 0;
            }
        }

        /// <summary>
        /// Xóa ảnh
        /// </summary>
        /// <param name="photoID">Mã ảnh</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> DeletePhotoAsync(long photoID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM ProductPhotos WHERE PhotoID = @PhotoID";
                return (await connection.ExecuteAsync(sql, new { PhotoID = photoID })) > 0;
            }
        }

        #endregion

        #region Product Attributes

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng
        /// </summary>
        /// <param name="productID">Mã mặt hàng</param>
        /// <returns>Danh sách thuộc tính</returns>
        public async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductAttributes WHERE ProductID = @ProductID ORDER BY DisplayOrder";
                return (await connection.QueryAsync<ProductAttribute>(sql, new { ProductID = productID })).ToList();
            }
        }

        /// <summary>
        /// Lấy thông tin một thuộc tính
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính</param>
        /// <returns>Thông tin thuộc tính</returns>
        public async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "SELECT * FROM ProductAttributes WHERE AttributeID = @AttributeID";
                return await connection.QueryFirstOrDefaultAsync<ProductAttribute>(sql, new { AttributeID = attributeID });
            }
        }

        /// <summary>
        /// Thêm thuộc tính cho mặt hàng
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính</param>
        /// <returns>ID của thuộc tính vừa tạo</returns>
        public async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"INSERT INTO ProductAttributes(ProductID, AttributeName, AttributeValue, DisplayOrder)
                            VALUES(@ProductID, @AttributeName, @AttributeValue, @DisplayOrder);
                            SELECT SCOPE_IDENTITY();";
                return await connection.ExecuteScalarAsync<long>(sql, data);
            }
        }

        /// <summary>
        /// Cập nhật thuộc tính
        /// </summary>
        /// <param name="data">Dữ liệu thuộc tính</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = @"UPDATE ProductAttributes 
                            SET AttributeName = @AttributeName, AttributeValue = @AttributeValue, DisplayOrder = @DisplayOrder
                            WHERE AttributeID = @AttributeID";
                return (await connection.ExecuteAsync(sql, data)) > 0;
            }
        }

        /// <summary>
        /// Xóa thuộc tính
        /// </summary>
        /// <param name="attributeID">Mã thuộc tính</param>
        /// <returns>True nếu thành công</returns>
        public async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var sql = "DELETE FROM ProductAttributes WHERE AttributeID = @AttributeID";
                return (await connection.ExecuteAsync(sql, new { AttributeID = attributeID })) > 0;
            }
        }

        #endregion
    }
}