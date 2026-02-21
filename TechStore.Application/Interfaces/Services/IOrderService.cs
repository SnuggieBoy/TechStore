using TechStore.Application.DTOs.Order;

namespace TechStore.Application.Interfaces.Services
{
    public interface IOrderService
    {
        Task<OrderDto> CreateOrderAsync(int userId, CreateOrderDto dto);
        Task<List<OrderDto>> GetMyOrdersAsync(int userId);
        Task<List<OrderDto>> GetAllOrdersAsync(); // Admin
        Task<OrderDto> GetByIdAsync(int id);
        Task<OrderDto> UpdateStatusAsync(int id, UpdateOrderStatusDto dto);
    }
}
