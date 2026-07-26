namespace ShopAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        public string ProductName { get; set; }

        public string Description { get; set; }

        public decimal Price { get; set; }

        public int Quantity { get; set; }

public string ProductImage { get; set; }

        public bool IsPopular { get; set; } = false;

        public bool IsRecent { get; set; } = true;

public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        // Foreign Key

        public int CategoryId { get; set; }

        public Category Category { get; set; }
    }
}
