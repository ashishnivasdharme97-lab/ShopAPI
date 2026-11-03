using System.ComponentModel.DataAnnotations;

namespace ShopAPI.DTOs
{
    public class CreateFestivalScheduleDto
    {
        [Required]
        [StringLength(120, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public DateOnly FestivalDate { get; set; }
    }
}