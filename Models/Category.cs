namespace ShopAPI.Models
{
    public class Category
    {
        public int Id { get; set; }

        public string CategoryName { get; set; } = string.Empty;

            public string? CategoryImage { get; set; }


        public ICollection<Product> Products { get; set; }
            = new List<Product>();
    }
}