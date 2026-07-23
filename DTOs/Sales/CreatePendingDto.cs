public class CreatePendingDto
{
    public string CustomerName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public string? Reason { get; set; }
}