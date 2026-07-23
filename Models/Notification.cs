

using ShopAPI.Data;
using ShopAPI.Models;
namespace ShopAPI.Models
{
public class Notification
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public string Message { get; set; }

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }
}}