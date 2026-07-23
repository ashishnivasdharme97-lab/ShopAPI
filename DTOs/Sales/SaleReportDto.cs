namespace ShopAPI.DTOs.Sales
{
    public class SaleReportDto
    {
        public string BillNo { get; set; } = "";

        public decimal GrandTotal { get; set; }

        public decimal PaidAmount { get; set; }

        public decimal RemainingAmount { get; set; }

        public string PaymentMode { get; set; } = "";

        public DateTime CreatedAt { get; set; }
    }
}