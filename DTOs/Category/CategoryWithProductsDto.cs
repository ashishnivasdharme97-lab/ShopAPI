namespace ShopAPI.DTOs.Category
{
    public class CategoryWithProductsDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public List<ProductSummaryDto> Products { get; set; }
            = new List<ProductSummaryDto>();
    }
}