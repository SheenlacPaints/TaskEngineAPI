using System.Text;
using Microsoft.AspNetCore.Authorization;
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


        [Authorize]
        [HttpGet("getAllSuperAdmin")]
        public async Task<IActionResult> getAllSuperAdmin()
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                // Attach token to outbound request
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}Account/GetAllSuperAdmin");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());

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


        [HttpPut("updateSuperAdmin")]
        public async Task<IActionResult> updateSuperAdmin([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}Account/UpdateSuperAdmin", content);
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



        [HttpDelete("deleteSuperAdmin")]
        public async Task<IActionResult> deleteSuperAdmin([FromQuery] pay request)
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

      
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/CreateUser", content);
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


        [Authorize]
        [HttpPost("oTPGenerateAdmin")]
        public async Task<IActionResult> oTPGenerateAdmin()
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                // Attach token to outbound request
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/oTPGenerateAdmin");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());

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


        [Authorize]
        [HttpPost("verifyOtpAndExecute")]
        public async Task<IActionResult> verifyOtpAndExecute([FromBody] dynamic prms)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                // Attach token to outbound request
                // 🔧 Prepare outbound request
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/verifyOtpAndExecute");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());

                // 🔒 Attach encrypted payload
                string encryptedPayload = prms.ToString(); // already encrypted
                requestMessage.Content = new StringContent(encryptedPayload, Encoding.UTF8, "application/json");

                // 🌐 Send request
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


        [Authorize]
        [HttpGet("GetAllUser")]
        public async Task<IActionResult> GetAllUser()
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                // Attach token to outbound request
               // var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}Account/getAllUser");
                var requestMessage = new HttpRequestMessage( HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/Account/GetAllUser");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());

            
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

        [Authorize]
        [HttpGet("GetAllUserbyid")]
        public async Task<IActionResult> GetAllUserbyid([FromQuery] string id)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/Account/GetAllUserbyid?id={id}";



                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());

                // 📡 Forward request
                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();

                // 🔐 Wrap encrypted response in quotes
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

        
        
        [HttpPut("updateUser")]
        public async Task<IActionResult> updateUser([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"{_baseUrl}Account/updateUser", content);
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