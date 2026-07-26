using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShopAPI.Data;
using ShopAPI.DTOs.Category;
using ShopAPI.Models;
using System.IO;

namespace ShopAPI.Controllers
{
    [ApiController]
    [Route("api/category")]
   
    public class CategoryController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CategoryController(AppDbContext context)
        {
            _context = context;
        }

        // GET ALL
        
[HttpGet]
public async Task<IActionResult> Get()
{
    try
    {
        var categories = await _context.Categories
            .OrderBy(x => x.CategoryName)
            .Select(c => new
            {
                c.Id,
                c.CategoryName,
                CategoryImage = string.IsNullOrEmpty(c.CategoryImage)
                    ? ""
                    : $"{Request.Scheme}://{Request.Host}/images/{c.CategoryImage}"
            })
            .ToListAsync();

        return Ok(categories);
    }
    catch (Exception ex)
    {
        return StatusCode(500, ex.ToString());
    }
}

        [HttpPost]
public async Task<IActionResult> Create([FromForm] CreateCategoryDto dto)
{
    string? imageName = null;

    if (dto.CategoryImage != null)
    {
        var folderPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot",
            "images"
        );

        if (!Directory.Exists(folderPath))
        {
            Directory.CreateDirectory(folderPath);
        }

        imageName = Guid.NewGuid().ToString() +
                    Path.GetExtension(dto.CategoryImage.FileName);

        var filePath = Path.Combine(folderPath, imageName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await dto.CategoryImage.CopyToAsync(stream);
        }
    }

    var category = new Category
    {
        CategoryName = dto.Name,
        CategoryImage = imageName
    };

    _context.Categories.Add(category);
    await _context.SaveChangesAsync();

    return Ok(category);
}

        // UPDATE
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, UpdateCategoryDto dto)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            category.CategoryName = dto.Name;

            await _context.SaveChangesAsync();

            return Ok(category);
        }

        // DELETE
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var category = await _context.Categories.FindAsync(id);

            if (category == null)
                return NotFound();

            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Category Deleted Successfully"
            });
        }

        // WITH PRODUCTS
        [HttpGet("with-products")]
        public async Task<IActionResult> GetWithProducts()
        {
            var data = await _context.Categories
                .Include(c => c.Products)
                .ToListAsync();

            return Ok(data);
        }

        // SUMMARY
        [HttpGet("summary")]
        public async Task<IActionResult> Summary()
        {
            var data = await _context.Categories
                .Select(c => new
                {
                    c.Id,
                    c.CategoryName,
                    ProductCount = c.Products.Count
                })
                .ToListAsync();

            return Ok(data);
        }
    }
}