public class Pending
{
    public int Id { get; set; }

    public string CustomerName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string Status { get; set; } = "Pending";
}