namespace ShopAPI.DTOs.Sales
{
    public class SaleItemDto
    {
        public int ProductId { get; set; }

        public decimal Rate { get; set; }

        public int Quantity { get; set; }

        public decimal Amount { get; set; }
    }
}