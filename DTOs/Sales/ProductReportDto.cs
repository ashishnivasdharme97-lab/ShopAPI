namespace ShopAPI.DTOs.Sales
{
    public class ProductReportDto
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = "";

        public int TotalQuantitySold { get; set; }

        public decimal TotalSaleAmount { get; set; }

        public int CurrentStock { get; set; }
    }
}