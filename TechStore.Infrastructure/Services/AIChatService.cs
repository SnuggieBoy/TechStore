using System;
using System.Collections.Generic;
using System.Text;
using TechStore.Application.Interfaces.Repositories;
using TechStore.Application.Interfaces.Services;

namespace TechStore.Infrastructure.Services
{
    public class AIChatService : IAIChatService
    {
        private readonly IProductRepository _productRepo;
        private readonly IOpenAIService _openAI;

        public AIChatService(IProductRepository productRepo, IOpenAIService openAI)
        {
            _productRepo = productRepo;
            _openAI = openAI;
        }

        public async Task<string> AskAsync(string question)
        {
            var products = await _productRepo.GetAllAsync();

            if (string.IsNullOrWhiteSpace(question))
                return "Bạn hãy nhập câu hỏi nhé!";

            var q = question.ToLower();

            // 🔥 1. Greeting
            if (q.Contains("hi") || q.Contains("xin chào") || q.Contains("hello"))
            {
                return "👋 Xin chào! Tôi có thể giúp bạn tìm sản phẩm theo tên, hãng (Apple, Dell...), giá hoặc mô tả.";
            }

            // 🔥 2. Tìm theo BRAND (Apple, Dell, Asus...)
            var brandMatch = products
                .Where(p => !string.IsNullOrEmpty(p.BrandName) &&
                            q.Contains(p.BrandName.ToLower()))
                .ToList();

            if (brandMatch.Any())
            {
                return $"🏷️ Sản phẩm thuộc hãng:\n" +
                       string.Join("\n", brandMatch.Select(p =>
                           $"📦 {p.Name} - {p.Price} VND"));
            }

         // 🔥 3.CATEGORY: LAPTOP
            if (q.Contains("laptop"))
            {
                var laptops = products
                    .Where(p => p.Name.ToLower().Contains("laptop"))
                    .ToList();

                if (!laptops.Any())
                    return "❌ Không có laptop.";

                return "💻 Laptop hiện có:\n" +
                       string.Join("\n", laptops.Select(p =>
                           $"📦 {p.Name} - {p.Price} VND"));
            }

            // 🔥 3. CATEGORY: ĐIỆN THOẠI
            if (q.Contains("điện thoại") || q.Contains("phone"))
            {
                var phones = products
                    .Where(p => p.Name.ToLower().Contains("iphone") ||
                                p.Name.ToLower().Contains("samsung") ||
                                p.Name.ToLower().Contains("phone"))
                    .ToList();

                if (!phones.Any())
                    return "❌ Không có điện thoại.";

                return "📱 Điện thoại hiện có:\n" +
                       string.Join("\n", phones.Select(p =>
                           $"📦 {p.Name} - {p.Price} VND"));
            }

            // 🔥 4. Tìm theo NAME chính xác
            var exact = products
                .FirstOrDefault(p => q.Contains(p.Name.ToLower()));

            if (exact != null)
            {
                return $"📦 {exact.Name}\n💰 Giá: {exact.Price} VND\n📝 {exact.Description}";
            }

            // 🔥 5. Hỏi GIÁ
            foreach (var p in products)
            {
                if (q.Contains(p.Name.ToLower()) && (q.Contains("giá") || q.Contains("bao nhiêu")))
                {
                    return $"💰 {p.Name} có giá {p.Price} VND.";
                }
            }

            // 🔥 6. Tìm theo DESCRIPTION (rất hay)
            var descMatch = products
                .Where(p => !string.IsNullOrEmpty(p.Description) &&
                            p.Description.ToLower().Contains(q))
                .ToList();

            if (descMatch.Any())
            {
                return "🔍 Sản phẩm liên quan:\n" +
                       string.Join("\n", descMatch.Select(p =>
                           $"📦 {p.Name} - {p.Price} VND"));
            }

            // 🔥 7. Tìm theo giá (dưới X triệu)
            if (q.Contains("dưới"))
            {
                var numbers = System.Text.RegularExpressions.Regex
                    .Match(q, @"\d+")
                    .Value;

                if (int.TryParse(numbers, out int price))
                {
                    var filtered = products
                        .Where(p => p.Price <= price * 1000000)
                        .ToList();

                    if (filtered.Any())
                    {
                        return $"💰 Sản phẩm dưới {price} triệu:\n" +
        string.Join("\n", filtered.Select(p =>
                                   $"📦 {p.Name} - {p.Price} VND"));
                    }
                }
            }

            // 🔥 fallback
            return "🤖 Tôi có thể giúp bạn tìm theo hãng (Apple), loại (laptop), giá hoặc mô tả sản phẩm.";
        }
    }
}
