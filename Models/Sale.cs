
namespace ShopAPI.Models
{
public class Sale
{
    public int Id { get; set; }

    public string BillNo { get; set; } = "";

    public int? CustomerId { get; set; }

    public decimal GrandTotal { get; set; }

    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount { get; set; }

    public string PaymentMode { get; set; } = "";

    public DateTime CreatedAt { get; set; }
    public string? CustomerName { get; set; }

    public List<SaleItem> SaleItems { get; set; } = new();
}
}
