namespace ShopAPI.Models
{
    public class ProductRequest
    {
        public int Id { get; set; }
        public string ProductName { get; set; }
        public string Description { get; set; }
        public int RequestedBy { get; set; }
            public int Quantity { get; set; }   // <-- ADD

        public string? Status { get; set; }


public int UserId { get; set; }   // ADD THIS
public DateTime CreatedAt { get; set; } = DateTime.Now; // ADD THIS

 public User? User { get; set; }
    }
}