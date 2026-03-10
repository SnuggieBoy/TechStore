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
        /// <summary>Admin: update order/shipping status (Pending → Shipping → Delivered).</summary>
        Task<OrderDto> UpdateOrderStatusAsync(string orderPublicId, UpdateOrderStatusDto dto);
        /// <summary>Admin: update payment status (Unpaid → Paid → Cancelled).</summary>
        Task<OrderDto> UpdatePaymentStatusAsync(string orderPublicId, UpdatePaymentStatusDto dto);
        Task<OrderDto> CancelMyOrderAsync(int userId, string orderPublicId);
        /// <summary>Mock payment: simulate ~2s delay then set PaymentStatus from Unpaid to Paid.</summary>
        Task<OrderDto> PayOrderAsync(int userId, string orderPublicId);

        /// <summary>Confirm VNPay payment after IPN/callback verification.</summary>
        Task ConfirmVnPayPaymentAsync(int orderId);
    }
}
