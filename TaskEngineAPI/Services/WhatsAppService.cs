using System.Net.Http;
using TaskEngineAPI.WebSockets;
using System.Net.Http.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace TaskEngineAPI.Services
{
    public class WhatsAppService
    {
        private readonly WebSocketConnectionManager _connectionManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<WhatsAppService> _logger;
        private readonly IConfiguration _configuration;
        public WhatsAppService(
           IHttpClientFactory httpClientFactory,
           ILogger<WhatsAppService> logger,
           IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _configuration = configuration;
        }

        private async Task SendWhatsAppNotificationAsync()
        {
            var url = "https://backend.api-wa.co/campaign/smartping/api/v2";

            var client = _httpClientFactory.CreateClient();

            var payload = new
            {
                apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY5MTViZmQ3NjFiNDMzMGQ1Y2IzMGM0ZSIsIm5hbWUiOiJTaGVlbmxhYyBQYWludHMiLCJhcHBOYW1lIjoiQWlTZW5zeSIsImNsaWVudElkIjoiNjkxNWJmZDc2MWI0MzMwZDVjYjMwYzQ3IiwiYWN0aXZlUGxhbiI6Ik5PTkUiLCJpYXQiOjE3NjMwMzMwNDd9.Qpd0HmsXQxTGx_v0EkOHKTUN-gEAzoRDahaiMtT4lQU",
                campaignName = "reassighn new process",
                destination = "918220237725",
                userName = "Sheenlac Paintss",
                templateParams = new[] { "$FirstName", "$FirstName", "$FirstName", "$FirstName" },
                source = "new-landing-page form",
                media = new { },
                buttons = new string[] { },
                carouselCards = new string[] { },
                location = new { },
                attributes = new { },
                paramsFallbackValue = new { FirstName = "user" }
            };

            var response = await client.PostAsJsonAsync(url, payload);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Background WhatsApp sent successfully at {Time}", DateTime.Now);
            }
            else
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Background WhatsApp failed with Status {Status}: {Error}", response.StatusCode, error);
            }
        }



    }
}
