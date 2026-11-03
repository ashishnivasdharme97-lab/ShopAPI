using System.ComponentModel.DataAnnotations;

namespace ShopAPI.DTOs
{
    public class DeviceTokenRequestDto
    {
        [Required]
        [StringLength(4096, MinimumLength = 20)]
        public string Token { get; set; } = string.Empty;
    }
}