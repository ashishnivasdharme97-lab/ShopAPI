namespace ShopAPI.DTOs.Product
{
    public class CreateProductDto
    {
        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

    public IFormFile ProductImage { get; set; }   // 🔥 FIX

        public int CategoryId { get; set; }
    }
}