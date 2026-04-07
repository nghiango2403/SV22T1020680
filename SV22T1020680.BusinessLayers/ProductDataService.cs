using SV22T1020680.DataLayers.Interfaces;
using SV22T1020680.DataLayers.SQLServer;
using SV22T1020680.Models.Catalog;
using SV22T1020680.Models.Common;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SV22T1020680.BusinessLayers
{
    /// <summary>
    /// Các nghiệp vụ quản lý mặt hàng (Products)
    /// </summary>
    public static class ProductDataService
    {
        private static readonly IProductRepository productDB;

        static ProductDataService()
        {
            string connectionString = Configuration.ConnectionString;
            productDB = new ProductRepository(connectionString);
        }

        #region Product Logic

        /// <summary>
        /// Tìm kiếm và lấy danh sách mặt hàng (phân trang)
        /// </summary>
        public static async Task<PagedResult<Product>> ListProductsAsync(ProductSearchInput input)
        {
            return await productDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết một mặt hàng
        /// </summary>
        public static async Task<Product?> GetProductAsync(int productID)
        {
            return await productDB.GetAsync(productID);
        }

        /// <summary>
        /// Thêm mặt hàng mới
        /// </summary>
        public static async Task<int> AddProductAsync(Product data)
        {
            if (string.IsNullOrWhiteSpace(data.ProductName))
                throw new ArgumentNullException(nameof(data.ProductName), "Tên mặt hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(data.Unit))
                throw new ArgumentNullException(nameof(data.Unit), "Đơn vị tính không được để trống.");

            if (data.CategoryID <= 0 || data.CategoryID == null)
                throw new ArgumentNullException(nameof(data.CategoryID), "Vui lòng chọn loại hàng.");

            if (data.SupplierID <= 0 || data.SupplierID == null)
                throw new ArgumentNullException(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp.");

            if (data.Price < 0)
                throw new ArgumentNullException(nameof(data.Price), "Giá hàng không được nhỏ hơn 0.");

            if (string.IsNullOrWhiteSpace(data.ProductDescription))
                throw new ArgumentNullException(nameof(data.ProductDescription), "Giới thiệu không được để trống.");

            if (string.IsNullOrWhiteSpace(data.Photo))
                throw new ArgumentNullException(nameof(data.Photo), "Ảnh không được để trống.");
            return await productDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin mặt hàng
        /// </summary>
        public static async Task<bool> UpdateProductAsync(Product data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data), "Dữ liệu hàng hóa không được để trống.");
            if (string.IsNullOrWhiteSpace(data.ProductName))
                throw new ArgumentNullException(nameof(data.ProductName), "Tên mặt hàng không được để trống.");

            if (string.IsNullOrWhiteSpace(data.Unit))
                throw new ArgumentNullException(nameof(data.Unit), "Đơn vị tính không được để trống.");

            if (data.CategoryID <= 0 || data.CategoryID == null)
                throw new ArgumentNullException(nameof(data.CategoryID), "Vui lòng chọn loại hàng.");

            if (data.SupplierID <= 0 || data.SupplierID == null)
                throw new ArgumentNullException(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp.");

            if (data.Price < 0)
                throw new ArgumentNullException(nameof(data.Price), "Giá hàng không được nhỏ hơn 0.");

            if (string.IsNullOrWhiteSpace(data.ProductDescription))
                throw new ArgumentNullException(nameof(data.ProductDescription), "Giới thiệu không được để trống.");

            if (string.IsNullOrWhiteSpace(data.Photo))
                throw new ArgumentNullException(nameof(data.Photo), "Ảnh không được để trống.");
            return await productDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng (chỉ khi không có dữ liệu liên quan)
        /// </summary>
        public static async Task<bool> DeleteProductAsync(int productID)
        {
            if (await productDB.IsUsedAsync(productID))
                return false;

            return await productDB.DeleteAsync(productID);
        }

        /// <summary>
        /// Kiểm tra mặt hàng hiện có đang được sử dụng hay không
        /// </summary>
        public static async Task<bool> IsUsedProductAsync(int productID)
        {
            return await productDB.IsUsedAsync(productID);
        }

        #endregion

        #region Product Photos Logic

        /// <summary>
        /// Lấy danh sách ảnh của mặt hàng
        /// </summary>
        public static async Task<List<ProductPhoto>> ListPhotosAsync(int productID)
        {
            return await productDB.ListPhotosAsync(productID);
        }

        /// <summary>
        /// Lấy thông tin một ảnh
        /// </summary>
        public static async Task<ProductPhoto?> GetPhotoAsync(long photoID)
        {
            return await productDB.GetPhotoAsync(photoID);
        }

        /// <summary>
        /// Thêm ảnh cho mặt hàng
        /// </summary>
        public static async Task<long> AddPhotoAsync(ProductPhoto data)
        {
            return await productDB.AddPhotoAsync(data);
        }

        /// <summary>
        /// Cập nhật ảnh
        /// </summary>
        public static async Task<bool> UpdatePhotoAsync(ProductPhoto data)
        {
            return await productDB.UpdatePhotoAsync(data);
        }

        /// <summary>
        /// Xóa ảnh
        /// </summary>
        public static async Task<bool> DeletePhotoAsync(long photoID)
        {
            return await productDB.DeletePhotoAsync(photoID);
        }

        #endregion

        #region Product Attributes Logic

        /// <summary>
        /// Lấy danh sách thuộc tính của mặt hàng
        /// </summary>
        public static async Task<List<ProductAttribute>> ListAttributesAsync(int productID)
        {
            return await productDB.ListAttributesAsync(productID);
        }

        /// <summary>
        /// Lấy thông tin một thuộc tính
        /// </summary>
        public static async Task<ProductAttribute?> GetAttributeAsync(long attributeID)
        {
            return await productDB.GetAttributeAsync(attributeID);
        }

        /// <summary>
        /// Thêm thuộc tính cho mặt hàng
        /// </summary>
        public static async Task<long> AddAttributeAsync(ProductAttribute data)
        {
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                throw new ArgumentNullException(nameof(data.AttributeName), "Tên thuộc tính không được để trống.");

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                throw new ArgumentNullException(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống.");

            if (data.DisplayOrder <= 0)
                throw new ArgumentNullException(nameof(data.DisplayOrder), "Thứ tự hiển thị phải là số nguyên dương.");
            return await productDB.AddAttributeAsync(data);
        }

        /// <summary>
        /// Cập nhật thuộc tính
        /// </summary>
        public static async Task<bool> UpdateAttributeAsync(ProductAttribute data)
        {
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                throw new ArgumentNullException(nameof(data.AttributeName), "Tên thuộc tính không được để trống.");

            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                throw new ArgumentNullException(nameof(data.AttributeValue), "Giá trị thuộc tính không được để trống.");

            if (data.DisplayOrder <= 0)
                throw new ArgumentNullException(nameof(data.DisplayOrder), "Thứ tự hiển thị phải là số nguyên dương.");
            return await productDB.UpdateAttributeAsync(data);
        }

        /// <summary>
        /// Xóa thuộc tính
        /// </summary>
        public static async Task<bool> DeleteAttributeAsync(long attributeID)
        {
            return await productDB.DeleteAttributeAsync(attributeID);
        }

        #endregion
    }
}