using Microsoft.AspNetCore.Http;

namespace ShopAPI.DTOs.Category
{
    public class CreateCategoryDto
    {
        public string Name { get; set; } = string.Empty;

        public IFormFile? CategoryImage { get; set; }
    }
}