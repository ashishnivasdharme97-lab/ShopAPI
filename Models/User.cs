
using ShopAPI.Data;
using ShopAPI.Models;
namespace ShopAPI.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }

        // 👇 Role system
        public string Role { get; set; } = "User"; // User / Admin
    }
}