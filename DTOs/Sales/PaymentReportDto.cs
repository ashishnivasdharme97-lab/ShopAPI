namespace ShopAPI.DTOs.Sales
{
    public class PaymentReportDto
    {
        public decimal TotalSales { get; set; }

        public decimal TotalCollection { get; set; }

        public decimal CashCollection { get; set; }

        public decimal UPICollection { get; set; }

        public decimal PendingCollection { get; set; }
    }
}