using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Services
{
    public interface IWhatsAppSchedulerService
    {
        Task SendMessage(WhatsAppPayload payload);
    }

    public class WhatsAppSchedulerService : IWhatsAppSchedulerService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<WhatsAppSchedulerService> _logger;

        public WhatsAppSchedulerService(HttpClient httpClient, ILogger<WhatsAppSchedulerService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendMessage(WhatsAppPayload payload)
        {
            try
            {
                var url = "https://backend.api-wa.co/campaign/smartping/api/v2";

                payload.TemplateName = GenerateTemplateName(payload.CampaignName);

                _logger.LogInformation("Sending WhatsApp to {Destination}, Template: {Template}",
                    payload.Destination, payload.TemplateName);

                if (payload.ParamsFallbackValue == null || payload.ParamsFallbackValue.Count == 0)
                {
                    payload.ParamsFallbackValue = new Dictionary<string, string>
                    {
                        { "FirstName", "user" }
                    };
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
                };

                var response = await _httpClient.PostAsJsonAsync(url, payload, jsonOptions);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogError("WhatsApp API error {StatusCode}: {Error}",
                        response.StatusCode, errorContent);
                    throw new HttpRequestException($"WhatsApp API error: {response.StatusCode} - {errorContent}");
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("WhatsApp message sent successfully. Response: {Response}", responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send WhatsApp message to {Destination}", payload.Destination);
                throw;
            }
        }

        private string GenerateTemplateName(string campaignName)
        {
            if (string.IsNullOrEmpty(campaignName))
                return "default_template";

            return campaignName
                .ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Trim();
        }
    }
}