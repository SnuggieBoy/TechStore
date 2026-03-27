using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TechStore.Application.Interfaces.Services;

namespace TechStore.Infrastructure.Services
{
    public class OpenAIService : IOpenAIService
    {
        public async Task<string> GetResponseAsync(string prompt)
        {
            await Task.Delay(500); // giả lập AI delay

            var question = ExtractQuestion(prompt).ToLower();

            // 🎯 intent detection
            if (IsGreeting(question))
                return "👋 Xin chào! Tôi có thể giúp bạn tìm sản phẩm phù hợp.";

            if (IsAskPrice(question))
                return "💰 Bạn đang quan tâm mức giá bao nhiêu? (ví dụ: dưới 5 triệu, 10 triệu...)";

            if (IsAskProduct(question, "iphone"))
                return "📱 iPhone hiện có nhiều dòng như iPhone 11, 12, 13. Bạn muốn xem chi tiết không?";

            if (IsAskProduct(question, "laptop"))
                return "💻 Bên mình có Dell, HP, Asus. Bạn cần laptop học tập hay gaming?";

            if (IsCheap(question))
                return "💸 Bạn có thể tham khảo các sản phẩm dưới 5 triệu như tai nghe, phụ kiện.";

            return "🤖 Tôi chưa hiểu rõ câu hỏi. Bạn có thể hỏi về sản phẩm, giá hoặc loại thiết bị nhé!";
        }

        // 🔥 Tách câu hỏi từ prompt
        private string ExtractQuestion(string prompt)
        {
            var match = Regex.Match(prompt, @"Câu hỏi:\s*(.*)", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value : prompt;
        }

        // 🔥 Intent detection
        private bool IsGreeting(string q)
            => q.Contains("xin chào") || q.Contains("hello") || q.Contains("hi");

        private bool IsAskPrice(string q)
            => q.Contains("giá") || q.Contains("bao nhiêu");

        private bool IsCheap(string q)
            => q.Contains("rẻ") || q.Contains("dưới");

        private bool IsAskProduct(string q, string keyword)
            => q.Contains(keyword);
    }
}
