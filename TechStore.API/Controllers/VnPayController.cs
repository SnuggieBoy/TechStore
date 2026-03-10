using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TechStore.Application.DTOs.Common;
using TechStore.Application.DTOs.Order;
using TechStore.Application.DTOs.Payment;
using TechStore.Application.Interfaces.Services;

namespace TechStore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VnPayController : ControllerBase
    {
        private readonly IVnPayService _vnPayService;
        private readonly IOrderService _orderService;

        public VnPayController(IVnPayService vnPayService, IOrderService orderService)
        {
            _vnPayService = vnPayService;
            _orderService = orderService;
        }

        private int GetUserId() =>
            int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 1;

        /// <summary>
        /// Create VNPay payment URL for an existing Unpaid order.
        /// Returns URL to redirect customer to VNPay sandbox payment page.
        /// </summary>
        [HttpPost("create-payment-url")]
        //[Authorize] // TODO: BẬT LẠI SAU KHI TEST XONG
        [ProducesResponseType(typeof(ApiResponse<VnPayUrlResponseDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> CreatePaymentUrl([FromBody] CreateVnPayUrlDto dto)
        {
            try
            {
                var order = await _orderService.GetByPublicIdAsync(dto.OrderId);

                if (order.PaymentStatus != "Unpaid")
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        $"Đơn hàng không thể thanh toán. Trạng thái hiện tại: '{order.PaymentStatus}'"));

                var clientIp = HttpContext.Connection.RemoteIpAddress?.MapToIPv4().ToString() ?? "127.0.0.1";
                var orderInfo = $"Thanh toan don hang {order.Id}";

                var paymentUrl = _vnPayService.CreatePaymentUrl(order.Id, order.TotalAmount, orderInfo, clientIp);

                return Ok(ApiResponse<VnPayUrlResponseDto>.SuccessResponse(
                    new VnPayUrlResponseDto { PaymentUrl = paymentUrl },
                    "Tạo URL thanh toán VNPay thành công"));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ApiResponse<object>.ErrorResponse(ex.Message));
            }
        }

        /// <summary>
        /// VNPay IPN (Instant Payment Notification) — called by VNPay server.
        /// Must be publicly accessible. Returns RspCode for VNPay to acknowledge.
        /// </summary>
        [HttpGet("ipn")]
        [AllowAnonymous]
        public async Task<IActionResult> IpnCallback()
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var callback = _vnPayService.ProcessCallback(queryParams);

            if (!callback.IsSuccess)
                return Ok(new { RspCode = "97", Message = callback.Message });

            if (int.TryParse(callback.OrderId, out var orderId))
                await _orderService.ConfirmVnPayPaymentAsync(orderId);

            return Ok(new { RspCode = "00", Message = "Confirm Success" });
        }

        /// <summary>
        /// VNPay return URL — browser redirect after payment.
        /// Parses result and updates order if successful.
        /// </summary>
        [HttpGet("callback")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ApiResponse<VnPayCallbackDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> PaymentCallback()
        {
            var queryParams = Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString());
            var callback = _vnPayService.ProcessCallback(queryParams);

            if (callback.IsSuccess && int.TryParse(callback.OrderId, out var orderId))
                await _orderService.ConfirmVnPayPaymentAsync(orderId);

            return Ok(ApiResponse<VnPayCallbackDto>.SuccessResponse(callback,
                callback.IsSuccess ? "Thanh toán thành công" : "Thanh toán thất bại"));
        }
    }
}
