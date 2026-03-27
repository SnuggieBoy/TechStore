using System;
using System.Collections.Generic;
using System.Text;

namespace TechStore.Application.Interfaces.Services
{
    public interface IOpenAIService
    {
        Task<string> GetResponseAsync(string prompt);
    }
}
