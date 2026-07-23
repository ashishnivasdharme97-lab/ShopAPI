using Microsoft.AspNetCore.Mvc;
using ShopAPI.Data;
using ShopAPI.Models;
using ShopAPI.DTOs.Sales;


namespace ShopAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SalesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public SalesController(AppDbContext context)
        {
            _context = context;
        }

[HttpPost]
public async Task<IActionResult> CreateSale([FromBody] CreateSaleDto dto)
{
    var sale = new Sale
    {
        BillNo = "BILL-" + DateTime.Now.Ticks,
        CustomerId = null, // नंतर Customer Table जोडू
        GrandTotal = dto.TotalAmount,
        PaidAmount = dto.PaidAmount,
        RemainingAmount = dto.UdhariAmount,
        PaymentMode = dto.PaymentMode,
        CreatedAt = DateTime.Now,
        CustomerName = dto.CustomerName,
    };

    foreach (var item in dto.Items)
    {
        sale.SaleItems.Add(new SaleItem
        {
            ProductId = item.ProductId,
            Rate = item.Rate,
            Quantity = item.Quantity,
            Amount = item.Amount
        });

        // Stock कमी कर
        var product = await _context.Products.FindAsync(item.ProductId);

        if (product != null)
        {
            product.Quantity -= item.Quantity;
        }
    }

    _context.Sales.Add(sale);

    await _context.SaveChangesAsync();

    return Ok(new
    {
        message = "Sale Added Successfully"
    });
}

[HttpGet("dashboard")]
public IActionResult Dashboard()
{
    var today = DateTime.Today;
    var now = DateTime.Now;

    var totalUsers = _context.Users.Count();

    // Today
    var todaySales = _context.Sales
        .Where(x => x.CreatedAt.Date == today)
        .ToList();

    // Month
    var monthSales = _context.Sales
        .Where(x => x.CreatedAt.Month == now.Month &&
                    x.CreatedAt.Year == now.Year)
        .ToList();

    // Year
    var yearSales = _context.Sales
        .Where(x => x.CreatedAt.Year == now.Year)
        .ToList();

    // Overall
    var allSales = _context.Sales.ToList();

    return Ok(new
    {
        totalUsers,

        // Today
        todaySales = todaySales.Sum(x => x.GrandTotal),
        todayCollected = todaySales.Sum(x => x.PaidAmount),
        todayPending = todaySales.Sum(x => x.RemainingAmount),

        todayCash = todaySales
            .Where(x => x.PaymentMode == "Cash")
            .Sum(x => x.PaidAmount),

        todayUpi = todaySales
            .Where(x => x.PaymentMode == "UPI")
            .Sum(x => x.PaidAmount),

        // Month
        monthSales = monthSales.Sum(x => x.GrandTotal),
        monthCollected = monthSales.Sum(x => x.PaidAmount),
        monthPending = monthSales.Sum(x => x.RemainingAmount),

        // Year
        yearSales = yearSales.Sum(x => x.GrandTotal),
        yearCollected = yearSales.Sum(x => x.PaidAmount),
        yearPending = yearSales.Sum(x => x.RemainingAmount),

        // Overall
        totalSales = allSales.Sum(x => x.GrandTotal),
        totalCollected = allSales.Sum(x => x.PaidAmount),
        totalPending = allSales.Sum(x => x.RemainingAmount),

        totalBills = allSales.Count
    });
}

[HttpGet("master-report")]
public IActionResult GetMasterReport(string type = "daily")
{
    var now = DateTime.Now;

    var sales = _context.Sales.AsQueryable();

    switch (type.ToLower())
    {
        case "daily":
            sales = sales.Where(x => x.CreatedAt.Date == now.Date);
            break;

        case "weekly":
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);

            sales = sales.Where(x =>
                x.CreatedAt.Date >= startOfWeek &&
                x.CreatedAt.Date <= now.Date);
            break;

        case "monthly":
            sales = sales.Where(x =>
                x.CreatedAt.Month == now.Month &&
                x.CreatedAt.Year == now.Year);
            break;

        case "yearly":
            sales = sales.Where(x =>
                x.CreatedAt.Year == now.Year);
            break;

        case "overall":
            break;

        default:
            return BadRequest("Invalid report type.");
    }

    var summary = new SalesSummaryDto
    {
        TotalSales = sales.Sum(x => x.GrandTotal),

        CollectedAmount = sales.Sum(x => x.PaidAmount),

        PendingAmount = sales.Sum(x => x.RemainingAmount),

        CashCollection = sales
            .Where(x => x.PaymentMode == "Cash")
            .Sum(x => x.PaidAmount),

        UPICollection = sales
            .Where(x => x.PaymentMode == "UPI")
            .Sum(x => x.PaidAmount),

        TotalBills = sales.Count(),

        ProductsSold = sales
            .SelectMany(x => x.SaleItems)
            .Sum(i => i.Quantity)
    };

    return Ok(summary);
}
[HttpGet("payment-report")]
public IActionResult GetPaymentReport(string type = "daily")
{
    var now = DateTime.Now;

    var sales = _context.Sales.AsQueryable();

    switch (type.ToLower())
    {
        case "daily":
            sales = sales.Where(x => x.CreatedAt.Date == now.Date);
            break;

        case "weekly":
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);

            sales = sales.Where(x =>
                x.CreatedAt.Date >= startOfWeek &&
                x.CreatedAt.Date <= now.Date);
            break;

        case "monthly":
            sales = sales.Where(x =>
                x.CreatedAt.Month == now.Month &&
                x.CreatedAt.Year == now.Year);
            break;

        case "yearly":
            sales = sales.Where(x =>
                x.CreatedAt.Year == now.Year);
            break;

        case "overall":
            break;
    }

    var result = new PaymentReportDto
    {
        TotalSales = sales.Sum(x => x.GrandTotal),

        TotalCollection = sales.Sum(x => x.PaidAmount),

        PendingCollection = sales.Sum(x => x.RemainingAmount),

        CashCollection = sales
            .Where(x => x.PaymentMode == "Cash")
            .Sum(x => x.PaidAmount),

        UPICollection = sales
            .Where(x => x.PaymentMode == "UPI")
            .Sum(x => x.PaidAmount)
    };

    return Ok(result);
}
[HttpGet("product-report")]
public IActionResult GetProductReport(string type = "daily")
{
    var now = DateTime.Now;

    var saleItems = _context.SaleItems.AsQueryable();

    switch (type.ToLower())
    {
        case "daily":
            saleItems = saleItems.Where(x =>
                x.Sale.CreatedAt.Date == now.Date);
            break;

        case "weekly":
            var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);

            saleItems = saleItems.Where(x =>
                x.Sale.CreatedAt.Date >= startOfWeek &&
                x.Sale.CreatedAt.Date <= now.Date);
            break;

        case "monthly":
            saleItems = saleItems.Where(x =>
                x.Sale.CreatedAt.Month == now.Month &&
                x.Sale.CreatedAt.Year == now.Year);
            break;

        case "yearly":
            saleItems = saleItems.Where(x =>
                x.Sale.CreatedAt.Year == now.Year);
            break;

        case "overall":
            break;
    }

    var result = saleItems
        .GroupBy(x => new
        {
            x.ProductId,
            x.Product.ProductName,
            x.Product.Quantity
        })
        .Select(g => new ProductReportDto
        {
            ProductId = g.Key.ProductId,
            ProductName = g.Key.ProductName,
            TotalQuantitySold = g.Sum(x => x.Quantity),
            TotalSaleAmount = g.Sum(x => x.Amount),
            CurrentStock = g.Key.Quantity
        })
        .OrderByDescending(x => x.TotalQuantitySold)
        .ToList();

    return Ok(result);
}
[HttpGet("report")]
public IActionResult GetReport(string type = "daily")
{
    var now = DateTime.Now;

    var sales = _context.Sales.AsQueryable();

    switch (type.ToLower())
    {
        case "daily":
            sales = sales.Where(x =>
                x.CreatedAt.Date == now.Date);
            break;

case "weekly":
{
    var startOfWeek = now.Date.AddDays(-(int)now.DayOfWeek);

    sales = sales.Where(x =>
        x.CreatedAt.Date >= startOfWeek &&
        x.CreatedAt.Date <= now.Date);

    break;
}
        case "monthly":
            sales = sales.Where(x =>
                x.CreatedAt.Month == now.Month &&
                x.CreatedAt.Year == now.Year);
            break;

        case "yearly":
            sales = sales.Where(x =>
                x.CreatedAt.Year == now.Year);
            break;

        case "overall":
            // Filter नाही
            break;
    }

    var result = sales
        .OrderByDescending(x => x.CreatedAt)
        .Select(x => new SaleReportDto
        {
            BillNo = x.BillNo,
            GrandTotal = x.GrandTotal,
            PaidAmount = x.PaidAmount,
            RemainingAmount = x.RemainingAmount,
            PaymentMode = x.PaymentMode,
            CreatedAt = x.CreatedAt
        })
        .ToList();

    return Ok(result);
}

[HttpGet]
public IActionResult GetAllSales()
{
    var sales = _context.Sales
    .Select(x => new
    {
        x.Id,
        x.BillNo,
        x.CustomerId,
        x.GrandTotal,
        x.PaidAmount,
        x.RemainingAmount,
        x.PaymentMode,
        x.CreatedAt,

        Items = x.SaleItems.Select(i => new
        {
            i.ProductId,
            i.Rate,
            i.Quantity,
            i.Amount
        })
    })
    .OrderByDescending(x => x.CreatedAt)
    .ToList();

return Ok(sales);
}
    }



}

