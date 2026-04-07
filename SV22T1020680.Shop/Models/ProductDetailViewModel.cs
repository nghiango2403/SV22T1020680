using SV22T1020680.Models.Catalog;
using System.Collections.Generic;

namespace SV22T1020680.Shop.Models
{
    /// <summary>
    /// ViewModel cho trang chi tiết sản phẩm
    /// </summary>
    public class ProductDetailViewModel
    {
        public Product Product { get; set; } = new Product();
        public string CategoryName { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public List<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public List<ProductPhoto> Photos { get; set; } = new List<ProductPhoto>();
    }
}
