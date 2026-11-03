

using ShopAPI.Data;
using ShopAPI.Models;
namespace ShopAPI.Models
{
public class Notification
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    public string Message { get; set; } = string.Empty;

    public bool IsRead { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    public string? EventKey { get; set; }
}}