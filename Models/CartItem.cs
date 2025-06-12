namespace KD_Restaurant.Models
{
    public class CartItem
    {
        public int IdMenuItem { get; set; }
        public string? Title { get; set; }
        public string? Image { get; set; }
        public int Price { get; set; }
        public int Quantity { get; set; }
    }
}