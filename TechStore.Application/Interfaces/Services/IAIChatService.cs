using System;
using System.Collections.Generic;
using System.Text;

namespace TechStore.Application.Interfaces.Services
{
    public interface IAIChatService
    {
        Task<string> AskAsync(string question);
    }
}
