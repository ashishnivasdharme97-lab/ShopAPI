namespace ShopAPI.DTOs
{
    public class DashboardDto
    {
        // Today
        public decimal TodaySales { get; set; }
        public decimal TodayCollected { get; set; }
        public decimal TodayPending { get; set; }

        // Month
        public decimal MonthSales { get; set; }
        public decimal MonthCollected { get; set; }
        public decimal MonthPending { get; set; }

        // Year
        public decimal YearSales { get; set; }
        public decimal YearCollected { get; set; }
        public decimal YearPending { get; set; }

        // Overall
        public decimal TotalSales { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPending { get; set; }

        public int TotalBills { get; set; }
    }
}