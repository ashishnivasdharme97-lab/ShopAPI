namespace ShopAPI.DTOs.Category
{
    public class CategorySummaryDto
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int ProductCount { get; set; }
    }
}