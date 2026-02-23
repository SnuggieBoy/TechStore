using TechStore.Application.DTOs.Order;
using TechStore.Application.DTOs.Product;

namespace TechStore.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto);
        Task<PagedResult<OrderDto>> GetMyOrdersAsync(int userId, int page = 1, int pageSize = 10);
        Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 10); // Admin
        Task<OrderDto> GetByPublicIdAsync(string publicId);
        Task<OrderDto> UpdateStatusAsync(string orderPublicId, UpdateOrderStatusDto dto);
        Task<OrderDto> CancelMyOrderAsync(int userId, string orderPublicId);
        /// <summary>Mock payment: simulate ~2s delay then set order status from Pending to Paid.</summary>
        Task<OrderDto> PayOrderAsync(int userId, string orderPublicId);
    }
}
