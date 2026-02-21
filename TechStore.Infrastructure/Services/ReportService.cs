using Microsoft.EntityFrameworkCore;
using TechStore.Application.DTOs.Report;
using TechStore.Application.Interfaces.Services;
using TechStore.Infrastructure.Persistence;

namespace TechStore.Infrastructure.Services
{
    public class ReportService : IReportService
    {
        private readonly TechStoreDbContext _context;

        public ReportService(TechStoreDbContext context)
        {
            _context = context;
        }

        public async Task<RevenueReportDto> GetRevenueReportAsync()
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                .ToListAsync();

            var completedOrders = orders.Where(o => o.Status == "Delivered").ToList();

            var monthlyRevenue = completedOrders
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new MonthlyRevenueDto
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count()
                })
                .OrderByDescending(m => m.Year)
                .ThenByDescending(m => m.Month)
                .ToList();

            return new RevenueReportDto
            {
                TotalRevenue = completedOrders.Sum(o => o.TotalAmount),
                TotalOrders = orders.Count,
                TotalProductsSold = completedOrders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity),
                PendingOrders = orders.Count(o => o.Status == "Pending" || o.Status == "Confirmed" || o.Status == "Shipped"),
                CompletedOrders = completedOrders.Count,
                CancelledOrders = orders.Count(o => o.Status == "Cancelled"),
                MonthlyRevenue = monthlyRevenue
            };
        }

        public async Task<List<BestSellingProductDto>> GetBestSellingProductsAsync(int top = 10)
        {
            var orderItems = await _context.OrderItems
                .Include(oi => oi.Product)
                    .ThenInclude(p => p.Category)
                .Include(oi => oi.Order)
                .Where(oi => oi.Order.Status != "Cancelled")
                .ToListAsync();

            return orderItems
                .GroupBy(oi => oi.ProductId)
                .Select(g => new BestSellingProductDto
                {
                    ProductId = g.Key,
                    ProductName = g.First().Product.Name,
                    ImageUrl = g.First().Product.ImageUrl,
                    Price = g.First().Product.Price,
                    CategoryName = g.First().Product.Category.Name,
                    TotalQuantitySold = g.Sum(oi => oi.Quantity),
                    TotalRevenue = g.Sum(oi => oi.Quantity * oi.UnitPrice)
                })
                .OrderByDescending(p => p.TotalQuantitySold)
                .Take(top)
                .ToList();
        }

        public async Task<DashboardDto> GetDashboardAsync()
        {
            var totalUsers = await _context.Users.CountAsync();
            var totalProducts = await _context.Products.CountAsync();
            var totalCategories = await _context.Categories.CountAsync();
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => o.TotalAmount);

            var topProducts = await GetBestSellingProductsAsync(5);

            var recentOrders = await _context.Orders
                .Include(o => o.User)
                .OrderByDescending(o => o.OrderDate)
                .Take(10)
                .Select(o => new RecentOrderDto
                {
                    Id = o.Id,
                    Username = o.User.Username,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    OrderDate = o.OrderDate
                })
                .ToListAsync();

            return new DashboardDto
            {
                TotalUsers = totalUsers,
                TotalProducts = totalProducts,
                TotalCategories = totalCategories,
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TopProducts = topProducts,
                RecentOrders = recentOrders
            };
        }
    }
}
