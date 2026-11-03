namespace ShopAPI.Models
{
    public class ProductRequest
    {
        public int Id { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RequestedBy { get; set; }
            public int Quantity { get; set; }   // <-- ADD

        public string? Status { get; set; }


public int? UserId { get; set; }
public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // ADD THIS

    public string? IdempotencyKey { get; set; }

 public User? User { get; set; }
    }
}