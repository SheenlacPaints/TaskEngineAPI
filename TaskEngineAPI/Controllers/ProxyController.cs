using System.Text;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;

namespace TaskEngineAPI.Controllers
{
    

        [ApiController]
        [Route("api/[controller]")]
        public class ProxyController : ControllerBase

        { 
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration config)
        {
            _httpClient = httpClientFactory.CreateClient();
            _baseUrl = config["Proxy:SheenlacApiBaseUrl"];
        }
   
        [HttpPost("adminLogin")]
        public async Task<IActionResult> Adminlogin([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/Login", content);
                var body = await response.Content.ReadAsStringAsync();                                   
                string json = $"\"{body}\"";
                return StatusCode((int)response.StatusCode, json);
            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string jsonn = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(jsonn);
                string encc = $"\"{enc}\"";
                return StatusCode(500, encc);
            }
        }
     
        [HttpPost("createSuperAdmin")]
        public async Task<IActionResult> createSuperAdmin([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/CreateSuperAdmin", content);
                var body = await response.Content.ReadAsStringAsync();
                string json = $"\"{body}\"";
                return StatusCode((int)response.StatusCode, json);
            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string jsonn = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(jsonn);
                string encc = $"\"{enc}\"";
                return StatusCode(500, encc);
            }
        }


        [HttpGet("getAllSuperAdmin")]
        public async Task<IActionResult> getAllSuperAdmin([FromQuery] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/GetAllSuperAdmin", content);             
                var body = await response.Content.ReadAsStringAsync();
                // Mirror the external API's status code + body
                string json = $"\"{body}\"";
                return StatusCode((int)response.StatusCode, json);          
            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string jsonn = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(jsonn);
                string encc = $"\"{enc}\"";
                return StatusCode(500, encc);
            }
        }


        [HttpPut("updateSuperAdmin")]
        public async Task<IActionResult> updateSuperAdmin([FromQuery] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}Account/UpdateSuperAdmin", content);
                var body = await response.Content.ReadAsStringAsync();
                // Mirror the external API's status code + body
                string json = $"\"{body}\"";
                return StatusCode((int)response.StatusCode, json);
              
            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string jsonn = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(jsonn);
                string encc = $"\"{enc}\"";
                return StatusCode(500, encc);
            }
        }



        [HttpDelete("deleteSuperAdmin")]
        public async Task<IActionResult> deleteSuperAdmin([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{_baseUrl}Account/DeleteSuperAdmin"),
                    Content = content
                };
                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();
                string json = $"\"{body}\"";
                return StatusCode((int)response.StatusCode, json);
            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string jsonn = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(jsonn);
                string encc = $"\"{enc}\"";
                return StatusCode(500, encc);
            }
        }





    }
}