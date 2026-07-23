namespace ShopAPI.DTOs.Sales
{
    public class CreateSaleDto
    {
        public int UserId { get; set; }
        
       public decimal TotalAmount { get; set; }

public decimal PaidAmount { get; set; }

public decimal UdhariAmount { get; set; }

public string PaymentMode { get; set; } = "";
public string? CustomerName { get; set; }

public List<SaleItemDto> Items { get; set; } = new();
    }
}