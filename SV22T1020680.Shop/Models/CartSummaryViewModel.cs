namespace SV22T1020680.Shop.Models
{
    public class CartSummaryViewModel
    {
        public List<CartItem> Items { get; set; } = new();
        public decimal SubTotal => Items.Sum(i => i.Total);
    }
}
