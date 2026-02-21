using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TechStore.Application.DTOs.Common;
using TechStore.Application.DTOs.Report;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ReportsController : ControllerBase
    {
        private readonly IReportService _reportService;

        public ReportsController(IReportService reportService)
        {
            _reportService = reportService;
        }

        /// <summary>
        /// Get revenue report with monthly breakdown (Admin only).
        /// </summary>
        [HttpGet("revenue")]
        [ProducesResponseType(typeof(ApiResponse<RevenueReportDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRevenueReport()
        {
            var report = await _reportService.GetRevenueReportAsync();
            return Ok(ApiResponse<RevenueReportDto>.SuccessResponse(report));
        }

        /// <summary>
        /// Get best-selling products (Admin only).
        /// </summary>
        [HttpGet("best-selling")]
        [ProducesResponseType(typeof(ApiResponse<List<BestSellingProductDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBestSellingProducts([FromQuery] int top = 10)
        {
            var products = await _reportService.GetBestSellingProductsAsync(top);
            return Ok(ApiResponse<List<BestSellingProductDto>>.SuccessResponse(products));
        }

        /// <summary>
        /// Get admin dashboard overview (Admin only).
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(ApiResponse<DashboardDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDashboard()
        {
            var dashboard = await _reportService.GetDashboardAsync();
            return Ok(ApiResponse<DashboardDto>.SuccessResponse(dashboard));
        }
    }
}
