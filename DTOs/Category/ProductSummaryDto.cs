namespace ShopAPI.DTOs.Category
{
    public class ProductSummaryDto
    {
        public int Id { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public decimal Price { get; set; }

        public int Quantity { get; set; }

        public string ImageUrl { get; set; } = string.Empty;
    }
}