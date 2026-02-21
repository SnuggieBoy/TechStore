using TechStore.Application.DTOs.Order;
using TechStore.Application.DTOs.Product;

namespace TechStore.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto);
        Task<PagedResult<OrderDto>> GetMyOrdersAsync(int userId, int page = 1, int pageSize = 10);
        Task<PagedResult<OrderDto>> GetAllOrdersAsync(int page = 1, int pageSize = 10); // Admin
        Task<OrderDto> GetByIdAsync(int id);
        Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
        Task<OrderDto> CancelMyOrderAsync(int userId, int orderId); // Customer self-cancel
    }
}
