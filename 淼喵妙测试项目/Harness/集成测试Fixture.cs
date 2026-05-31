using System;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace 淼喵妙测试项目.Harness
{
    public class 集成测试Fixture : IAsyncLifetime
    {
        public bool ChromaDB可用 { get; private set; }

        public async ValueTask InitializeAsync()
        {
            try
            {
                ChromaDB可用 = await 检测ChromaDB();
            }
            catch
            {
                ChromaDB可用 = false;
            }
        }

        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        private static async Task<bool> 检测ChromaDB()
        {
            try
            {
                using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                var response = await httpClient.GetAsync("http://localhost:8000/api/v1/heartbeat");
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
