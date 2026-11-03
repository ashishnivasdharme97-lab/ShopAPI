namespace ShopAPI.Models
{
    public class FestivalSchedule
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DateOnly FestivalDate { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    }
}