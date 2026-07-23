namespace ShopAPI.DTOs.Sales
{
    public class SalesSummaryDto
    {
        public decimal TotalSales { get; set; }

        public decimal CollectedAmount { get; set; }

        public decimal PendingAmount { get; set; }

        public decimal CashCollection { get; set; }

        public decimal UPICollection { get; set; }

        public int TotalBills { get; set; }

        public int ProductsSold { get; set; }
    }
}