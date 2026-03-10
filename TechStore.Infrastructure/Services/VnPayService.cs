using System.Net;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using TechStore.Application.DTOs.Payment;
using TechStore.Application.Interfaces.Services;

namespace TechStore.Infrastructure.Services
{
    /// <summary>
    /// VNPay sandbox payment integration.
    /// Follows official VNPay C# demo: hash is computed on the URL-encoded query string.
    /// </summary>
    public class VnPayService : IVnPayService
    {
        private readonly string _tmnCode;
        private readonly string _hashSecret;
        private readonly string _payUrl;
        private readonly string _returnUrl;

        public VnPayService(IConfiguration configuration)
        {
            var section = configuration.GetSection("VnPaySettings");
            _tmnCode = section["TmnCode"] ?? throw new InvalidOperationException("VnPaySettings:TmnCode is not configured");
            _hashSecret = section["HashSecret"] ?? throw new InvalidOperationException("VnPaySettings:HashSecret is not configured");
            _payUrl = section["PayUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            _returnUrl = section["ReturnUrl"] ?? throw new InvalidOperationException("VnPaySettings:ReturnUrl is not configured");
        }

        public string CreatePaymentUrl(int orderId, decimal amount, string orderInfo, string clientIpAddress)
        {
            var requestData = new SortedDictionary<string, string>
            {
                { "vnp_Version", "2.1.0" },
                { "vnp_Command", "pay" },
                { "vnp_TmnCode", _tmnCode },
                { "vnp_Amount", ((long)(amount * 100)).ToString() },
                { "vnp_CurrCode", "VND" },
                { "vnp_TxnRef", orderId.ToString() },
                { "vnp_OrderInfo", orderInfo },
                { "vnp_OrderType", "other" },
                { "vnp_Locale", "vn" },
                { "vnp_ReturnUrl", _returnUrl },
                { "vnp_IpAddr", clientIpAddress },
                { "vnp_CreateDate", DateTime.UtcNow.AddHours(7).ToString("yyyyMMddHHmmss") },
                { "vnp_ExpireDate", DateTime.UtcNow.AddHours(7).AddMinutes(15).ToString("yyyyMMddHHmmss") }
            };

            // Build query string (URL-encoded) — following VNPay official demo
            var data = new StringBuilder();
            foreach (var kv in requestData)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var queryString = data.ToString();

            // Sign data = query string WITHOUT trailing '&' (this is how VNPay official demo does it)
            var signData = queryString;
            if (signData.Length > 0)
                signData = signData.Remove(signData.Length - 1, 1); // remove trailing '&'

            var vnpSecureHash = HmacSha512(_hashSecret, signData);

            return _payUrl + "?" + queryString + "vnp_SecureHashType=HMACSHA512&vnp_SecureHash=" + vnpSecureHash;
        }

        public VnPayCallbackDto ProcessCallback(IDictionary<string, string> queryParams)
        {
            var result = new VnPayCallbackDto();

            if (!queryParams.TryGetValue("vnp_SecureHash", out var receivedHash))
            {
                result.Message = "Missing secure hash";
                return result;
            }

            // Build sorted params excluding hash fields — use URL-encoded values for sign data
            var sorted = new SortedDictionary<string, string>();
            foreach (var kv in queryParams)
            {
                if (kv.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                    && !kv.Key.Equals("vnp_SecureHash", StringComparison.OrdinalIgnoreCase)
                    && !kv.Key.Equals("vnp_SecureHashType", StringComparison.OrdinalIgnoreCase))
                {
                    sorted[kv.Key] = kv.Value;
                }
            }

            var data = new StringBuilder();
            foreach (var kv in sorted)
            {
                if (!string.IsNullOrEmpty(kv.Value))
                {
                    data.Append(WebUtility.UrlEncode(kv.Key) + "=" + WebUtility.UrlEncode(kv.Value) + "&");
                }
            }

            var signData = data.ToString();
            if (signData.Length > 0)
                signData = signData.Remove(signData.Length - 1, 1);

            var computedHash = HmacSha512(_hashSecret, signData);

            if (!string.Equals(computedHash, receivedHash, StringComparison.OrdinalIgnoreCase))
            {
                result.Message = "Invalid signature";
                return result;
            }

            // Parse response fields
            queryParams.TryGetValue("vnp_TxnRef", out var txnRef);
            queryParams.TryGetValue("vnp_TransactionNo", out var transactionNo);
            queryParams.TryGetValue("vnp_ResponseCode", out var responseCode);
            queryParams.TryGetValue("vnp_Amount", out var amountStr);

            result.OrderId = txnRef ?? string.Empty;
            result.TransactionId = transactionNo ?? string.Empty;
            result.ResponseCode = responseCode ?? string.Empty;
            result.IsSuccess = responseCode == "00";
            result.Amount = long.TryParse(amountStr, out var amountLong) ? amountLong / 100m : 0;
            result.Message = result.IsSuccess ? "Thanh toán thành công" : $"Thanh toán thất bại (Mã: {responseCode})";

            return result;
        }

        #region Private Helpers

        private static string HmacSha512(string key, string data)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var dataBytes = Encoding.UTF8.GetBytes(data);
            using var hmac = new HMACSHA512(keyBytes);
            var hashBytes = hmac.ComputeHash(dataBytes);
            var sb = new StringBuilder(hashBytes.Length * 2);
            foreach (var b in hashBytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        #endregion
    }
}
