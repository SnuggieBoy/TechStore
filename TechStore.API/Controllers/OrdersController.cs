using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechStore.Application.DTOs.Common;
using TechStore.Application.DTOs.Order;
using TechStore.Application.DTOs.Product;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;

        public OrdersController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        private int GetUserId() =>
            int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Place a new order (Customer). Validates stock and deducts automatically.
        /// Duplicate product items are merged. Uses DB transaction for safety.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto dto)
        {
            try
            {
                var order = await _orderService.CreateOrderAsync(GetUserId(), dto);
                return CreatedAtAction(nameof(GetById), new { id = order.PublicId },
                    ApiResponse<OrderDto>.SuccessResponse(order, "Order placed successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Get my order history with pagination (Customer).
        /// </summary>
        [HttpGet("my-orders")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetMyOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _orderService.GetMyOrdersAsync(GetUserId(), page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderDto>>.SuccessResponse(orders));
        }

        /// <summary>
        /// Get all orders with pagination (Admin only).
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            var orders = await _orderService.GetAllOrdersAsync(page, pageSize);
            return Ok(ApiResponse<PagedResult<OrderDto>>.SuccessResponse(orders));
        }

        /// <summary>
        /// Get order detail by ID.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetById(string id)
        {
            try
            {
                var order = await _orderService.GetByPublicIdAsync(id);

                // Customer can only see their own orders
                var role = User.FindFirstValue(ClaimTypes.Role);
                if (role != "Admin" && order.UserId != GetUserId())
                    return Forbid();

                return Ok(ApiResponse<OrderDto>.SuccessResponse(order));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Update order status (Admin only). Validates status transitions.
        /// </summary>
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateOrderStatusDto dto)
        {
            try
            {
                var order = await _orderService.UpdateStatusAsync(id, dto);
                return Ok(ApiResponse<OrderDto>.SuccessResponse(order, $"Order status updated to {dto.Status}"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Mock payment: simulate payment processing (~2s delay) then set order status to Paid.
        /// </summary>
        [HttpPost("pay")]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PayOrder([FromBody] PayOrderDto dto)
        {
            try
            {
                var order = await _orderService.PayOrderAsync(GetUserId(), dto.OrderId);
                return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Success"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// Cancel my own Pending order (Customer). Restores stock automatically.
        /// </summary>
        [HttpPut("{id}/cancel")]
        [ProducesResponseType(typeof(ApiResponse<OrderDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CancelMyOrder(string id)
        {
            try
            {
                var order = await _orderService.CancelMyOrderAsync(GetUserId(), id);
                return Ok(ApiResponse<OrderDto>.SuccessResponse(order, "Order cancelled successfully"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }
    }
}
