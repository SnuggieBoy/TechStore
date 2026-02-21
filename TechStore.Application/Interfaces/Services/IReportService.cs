using TechStore.Application.DTOs.Report;

namespace TechStore.Application.Interfaces.Services
{
    public interface IReportService
    {
        Task<RevenueReportDto> GetRevenueReportAsync();
        Task<List<BestSellingProductDto>> GetBestSellingProductsAsync(int top = 10);
        Task<DashboardDto> GetDashboardAsync();
    }
}
