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
                // Mirror the external API's status code + body             
                return StatusCode((int)response.StatusCode,body);
            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string json = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(json);
                return StatusCode(500, enc);
            }
        }

        [HttpPost("CreateSuperAdmin")]
        public async Task<IActionResult> CreateSuperAdmin([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/CreateSuperAdmin", content);
                var body = await response.Content.ReadAsStringAsync();
                // Mirror the external API's status code + body
                return StatusCode((int)response.StatusCode, body);

            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string json = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(json);
                return StatusCode(500, enc);
            }
        }


        [HttpGet("GetAllSuperAdmin")]
        public async Task<IActionResult> GetAllSuperAdmin([FromQuery] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/GetAllSuperAdmin", content);
               
                var body = await response.Content.ReadAsStringAsync();
                // Mirror the external API's status code + body
                return StatusCode((int)response.StatusCode, body);

            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string json = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(json);
                return StatusCode(500, enc);
            }
        }


        [HttpPut("UpdateSuperAdmin")]
        public async Task<IActionResult> UpdateSuperAdmin([FromQuery] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}Account/UpdateSuperAdmin", content);

                var body = await response.Content.ReadAsStringAsync();
                // Mirror the external API's status code + body
                return StatusCode((int)response.StatusCode, body);

            }
            catch (Exception ex)
            {
                var err = new APIResponse
                {
                    status = 500,
                    statusText = $"Error calling external API: {ex.Message}"
                };
                string json = JsonConvert.SerializeObject(err);
                string enc = AesEncryption.Encrypt(json);
                return StatusCode(500, enc);
            }
        }


       




    }
}