using SV22T1020680.Models.Catalog;

namespace SV22T1020680.Shop.Models
{
    public class CartItem: Product
    {
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
    }
}
