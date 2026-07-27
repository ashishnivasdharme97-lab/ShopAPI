using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.DTOs.Product;
using ShopAPI.Models;

namespace ShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ProductController(AppDbContext context)
        {
            _context = context;
        }

        // Add Product


[HttpPost]
public async Task<IActionResult> Add([FromForm] CreateProductDto dto)
{
    try
    {
        if (!Directory.Exists("wwwroot/images"))
        {
            Directory.CreateDirectory("wwwroot/images");
        }

        string imagePath = "";

        if (dto.ProductImage != null)
        {
            var fileName = Guid.NewGuid() +
                           Path.GetExtension(dto.ProductImage.FileName);

            var path = Path.Combine("wwwroot/images", fileName);

            using (var stream = new FileStream(path, FileMode.Create))
            {
                await dto.ProductImage.CopyToAsync(stream);
            }

            imagePath = "images/" + fileName;
        }

        var product = new Product
        {
            ProductName = dto.ProductName,
            Description = dto.Description,
            Price = dto.Price,
            Quantity = dto.Quantity,
            CategoryId = dto.CategoryId,
            ProductImage = imagePath,
            CreatedDate = DateTime.UtcNow
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return Ok(product);
    }
    catch (Exception ex)
    {
    return BadRequest(ex.ToString());
    }
}
        // Get All Products

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var data = await _context.Products
                .Include(x => x.Category)
                .Select(x => new ProductResponseDto
                {
                    Id = x.Id,
                    ProductName = x.ProductName,
                    Description = x.Description,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    ProductImage = $"https://shopapi-5amg.onrender.com/{x.ProductImage}",
                    CategoryName = x.Category.CategoryName
                })
                .ToListAsync();

            return Ok(data);
        }


[HttpGet("low-stock")]
public async Task<IActionResult> GetLowStockProducts()
{
    var products = await _context.Products
        .Where(x => x.Quantity <= 3)
        .Select(x => new
        {
            x.Id,
            x.ProductName,
            x.Quantity,
            CategoryName = x.Category.CategoryName
        })
        .ToListAsync();

    return Ok(products);
}
        [HttpGet("category/{categoryId}")]
public IActionResult GetProductsByCategory(int categoryId)
{
    var products = _context.Products
        .Where(x => x.CategoryId == categoryId)
        .Select(x => new
        {
            x.Id,
            x.ProductName,
            x.Description,
            x.Price,
            x.Quantity,
            x.ProductImage,
            x.CategoryId,
            CategoryName = x.Category.CategoryName
        })
        .ToList();

    return Ok(products);
}
    }
}