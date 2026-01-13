using System.Data.Common;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Controllers
{


    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase

    {
        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;
        private readonly IMinioService _minioService;
        public ProxyController(IHttpClientFactory httpClientFactory, IConfiguration config, IMinioService MinioService)
        {
            _httpClient = httpClientFactory.CreateClient();
            _baseUrl = config["Proxy:SheenlacApiBaseUrl"];
            _minioService = MinioService;
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


        [Authorize]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl.TrimEnd('/')}/Account/CreateUser");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/Account/GetAllUser");
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

        [Authorize]
        [HttpPut("updateUser")]
        public async Task<IActionResult> updateUser([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}Account/updateUser");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();
                string json = $"\"{body}\"";
                return StatusCode((int)response.StatusCode, body);

                //return StatusCode((int)response.StatusCode, json);
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
        [HttpDelete("Deleteuser")]
        public async Task<IActionResult> Deleteuser([FromQuery] pay request)
        {
            try
            {

                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Missing or invalid Authorization token.");
                }
                var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri($"{_baseUrl}Account/Deleteuser"),
                    Content = content
                };
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

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
                return StatusCode(500, $"\"{enc}\"");
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
        public async Task<IActionResult> verifyOtpAndExecute([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/verifyOtpAndExecute");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                var jsonContent = JsonConvert.SerializeObject(request);
                requestMessage.Content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
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

        [HttpPost("verifyOtpforforgetpassword")]
        public async Task<IActionResult> verifyOtpforforgetpassword([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/verifyOtpforforgetpassword", content);
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

        [HttpPost("Forgotpasswordmaster")]
        public async Task<IActionResult> Forgotpasswordmaster([FromBody] pay request)
        {
            try
            {
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync($"{_baseUrl}Account/Forgotpasswordmaster", content);
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
        [HttpPut("UpdateSuperAdminpassword")]
        public async Task<IActionResult> UpdateSuperAdminpassword([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}Account/UpdateSuperAdminpassword");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPut("UpdateUserpassword")]
        public async Task<IActionResult> UpdateUserpassword([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}Account/UpdateUserpassword");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("GetAllProcesstype")]
        public async Task<IActionResult> GetAllProcesstype()
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
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}ProcessEngine/GetAllProcesstype");
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
        [HttpPost("CreateProcessEngine")]
        public async Task<IActionResult> CreateProcessEngine([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}ProcessEngine/CreateProcessEngine");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("GetAllProcessEngine")]
        public async Task<IActionResult> GetAllProcessEngine(
     [FromQuery] string? searchText = null,
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10,
     [FromQuery] int? created_by = null,
     [FromQuery] string? priority = null,
     [FromQuery] int? status = null)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(searchText))
                    queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");

                queryParams.Add($"page={page}");
                queryParams.Add($"pageSize={pageSize}");

                if (created_by.HasValue)
                    queryParams.Add($"created_by={created_by.Value}");

                if (!string.IsNullOrWhiteSpace(priority))
                    queryParams.Add($"priority={Uri.EscapeDataString(priority)}");

                if (status.HasValue)
                    queryParams.Add($"status={status.Value}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";

                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_baseUrl}ProcessEngine/GetAllProcessEngine{queryString}");

                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());

                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();


                string jsonn = JsonConvert.SerializeObject(body);

                return StatusCode((int)response.StatusCode, jsonn);
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
        [HttpPost("InsertTask")]
        public async Task<IActionResult> InsertTask([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}TaskMaster/InsertTask");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("GetMetadetailbyid")]
        public async Task<IActionResult> GetMetadetailbyid([FromQuery] int processid)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GetMetadetailbyid?processid={processid}";



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

        [Authorize]
        [HttpGet("GetProcessMetadetailbyid")]
        public async Task<IActionResult> GetProcessMetadetailbyid([FromQuery] int metaid)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GetProcessMetadetailbyid?metaid={metaid}";



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






        [Authorize]
        [HttpGet("GetProcessEnginebyid")]
        public async Task<IActionResult> GetProcessEnginebyid([FromQuery] string id)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/ProcessEngine/GetProcessEnginebyid?id={id}";
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


        [Authorize]
        [HttpGet("Getdepartmentroleposition")]
        public async Task<IActionResult> Getdepartmentroleposition([FromQuery] string table)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getdepartmentroleposition?table={table}";
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

        [Authorize]
        [HttpGet("Getprocessengineprivilege")]
        public async Task<IActionResult> Getprocessengineprivilege([FromQuery] string? value, string cprivilege)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getprocessengineprivilege?value={value}&cprivilege={cprivilege}";
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

        [Authorize]
        [HttpPost("fileUpload")]
        public async Task<IActionResult> fileUpload([FromForm] FileUploadDTO request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/UserfileUpload");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("Getdropdown")]
        public async Task<IActionResult> Getdropdown([FromQuery] string? column)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getdropdown?column={column}";
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

        [Authorize]
        [HttpPost("GetDropDownFilter")]
        public async Task<IActionResult> GetDropDownFilter([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}TaskMaster/GetDropDownFilter");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("Gettaskinbox")]
        public async Task<IActionResult> Gettaskinbox([FromQuery] string? searchText = null, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskinbox?searchText={searchText}&pageNo={pageNo}&pageSize={pageSize}";
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

        [Authorize]
        [HttpGet("Gettaskapprove")]
        public async Task<IActionResult> Gettaskapprove([FromQuery] string? searchText = null, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskapprove?searchText={searchText}&pageNo={pageNo}&pageSize={pageSize}";
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

        [Authorize]
        [HttpGet("GettaskReject")]
        public async Task<IActionResult> GettaskReject([FromQuery] string? searchText = null, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskReject?searchText={searchText}&page={pageNo}&pageSize={pageSize}";
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

        [Authorize]
        [HttpGet("GettaskHold")]
        public async Task<IActionResult> GettaskHold([FromQuery] string? searchText = null, [FromQuery] int pageNo = 1, [FromQuery] int pageSize = 50)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskHold?searchText={searchText}&pageNo={pageNo}&pageSize={pageSize}";
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


        [Authorize]
        [HttpGet("Getopentasklist")]
        public async Task<IActionResult> Getopentasklist([FromQuery] string? searchText = null)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getopentasklist?searchText={searchText}";
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

        [Authorize]
        [HttpPost("DeptposrolecrudAsync")]
        public async Task<IActionResult> DeptposrolecrudAsync([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}TaskMaster/DeptposrolecrudAsync");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("Processprivilegemapping")]
        public async Task<IActionResult> Processprivilegemapping([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}TaskMaster/Processprivilege_mapping");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("Gettaskinitiator")]
        public async Task<IActionResult> Gettaskinitiator([FromQuery] string? searchText = null)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskinitiator?searchText={searchText}";
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




        [Authorize]
        [HttpPost("CreateUsersBulk")]
        public async Task<IActionResult> CreateUsersBulk([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreateUsersBulk3");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreateUserApi")]
        public async Task<IActionResult> CreateUserApi([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreateUserApi");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreateDepartmentsApi")]
        public async Task<IActionResult> CreateDepartmentsApi([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreateDepartmentsApi");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreateRolesApi")]
        public async Task<IActionResult> CreateRolesApi([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreateRolesApi");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreatePositionsApi")]
        public async Task<IActionResult> CreatePositionsApi([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreatePositionsApi");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreateDepartmentsBulk")]
        public async Task<IActionResult> CreateDepartmentsBulk([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreateDepartmentsBulk");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreateRolesBulk")]
        public async Task<IActionResult> CreateRolesBulk([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreateRolesBulk");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("CreatePositionsBulk")]
        public async Task<IActionResult> CreatePositionsBulk([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/CreatePositionsBulk");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPost("usersapisyncconfig")]
        public async Task<IActionResult> usersapisyncconfig([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/usersapisyncconfig");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPut("Updateprocessstatusdelete")]
        public async Task<IActionResult> Updateprocessstatusdelete([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}ProcessEngine/Updateprocessstatusdelete");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("GetPrivilegeTypeById")]
        public async Task<IActionResult> GetPrivilegeTypeById([FromQuery] int privilegeType)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/LookUp/GetPrivilegeTypeById?privilegeType={privilegeType}";
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

        [Authorize]
        [HttpPost("CreateProcessmapping")]
        public async Task<IActionResult> CreateProcessmapping([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}ProcessEngine/CreateProcessmapping");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPut("Updateprocessmapping")]
        public async Task<IActionResult> Updateprocessmapping([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}ProcessEngine/Updateprocessmapping");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpDelete("DeleteProcessMapping")]
        public async Task<IActionResult> DeleteProcessMapping([FromQuery] pay request)
        {
            try
            {

                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Missing or invalid Authorization token.");
                }
                var jwtToken = authHeader.Substring("Bearer ".Length).Trim();
                var content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");

                string encodedPayload = System.Net.WebUtility.UrlEncode(request.payload);

                string forwardingUri = $"{_baseUrl}ProcessEngine/DeleteProcessMapping?payload={encodedPayload}";

                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(forwardingUri)


                };
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

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
                return StatusCode(500, $"\"{enc}\"");
            }
        }

        [Authorize]
        [HttpGet("GetMappingList")]
        public async Task<IActionResult> GetMappingList()
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
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}ProcessEngine/GetMappingList");
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
        [HttpPut("UpdateProcessEngine")]
        public async Task<IActionResult> UpdateProcessEngine([FromBody] pay request)
        {
            try
            {
                // Extract token from incoming request
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl}ProcessEngine/UpdateProcessEngine");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("GetAllUsersApiSyncConfig")]
        public async Task<IActionResult> GetAllUsersApiSyncConfig(
        [FromQuery] string? searchText = null,
       [FromQuery] string? syncType = null,
       [FromQuery] string? apiMethod = null,
       [FromQuery] bool? isActive = null)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var queryParams = new List<string>();

                if (!string.IsNullOrWhiteSpace(searchText))
                    queryParams.Add($"searchText={Uri.EscapeDataString(searchText)}");

                if (!string.IsNullOrWhiteSpace(syncType))
                    queryParams.Add($"syncType={Uri.EscapeDataString(syncType)}");

                if (!string.IsNullOrWhiteSpace(apiMethod))
                    queryParams.Add($"apiMethod={Uri.EscapeDataString(apiMethod)}");

                //if (isActive.HasValue)
                //    queryParams.Add($"isActive={isActive.Value}");

                if (isActive.HasValue)
                    queryParams.Add($"isActive={isActive.Value.ToString().ToLower()}");

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";

                var requestMessage = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_baseUrl.TrimEnd('/')}/Account/GetAllUsersApiSyncConfig{queryString}");

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
        [HttpGet("GetAPISyncConfigByID")]
        public async Task<IActionResult> GetAPISyncConfigByID([FromQuery] int id)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/Account/GetAPISyncConfigByID?id={id}");
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
        [HttpPut("UpdateAPISyncConfig")]
        public async Task<IActionResult> UpdateAPISyncConfig([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl.TrimEnd('/')}/Account/UpdateAPISyncConfig");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();
               
                return StatusCode((int)response.StatusCode, body);
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
        [HttpPut("UpdateAPISyncConfigActiveStatus")]
        public async Task<IActionResult> UpdateAPISyncConfigActiveStatus([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl.TrimEnd('/')}/Account/UpdateAPISyncConfigActiveStatus");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, body);
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
        [HttpDelete("DeleteAPISyncConfig")]
        public async Task<IActionResult> DeleteAPISyncConfig([FromQuery] string payload)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payload))
                {
                    return BadRequest($"\"Payload parameter is required\"");
                }

                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return Unauthorized("Missing or invalid Authorization token.");
                }

                var jwtToken = authHeader.Substring("Bearer ".Length).Trim();

                string encodedPayload = System.Net.WebUtility.UrlEncode(payload);
                string forwardingUri = $"{_baseUrl}Account/DeleteAPISyncConfig?payload={encodedPayload}";

                var requestMessage = new HttpRequestMessage
                {
                    Method = HttpMethod.Delete,
                    RequestUri = new Uri(forwardingUri)
                };

                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                var response = await _httpClient.SendAsync(requestMessage);
                var body = await response.Content.ReadAsStringAsync();

                return StatusCode((int)response.StatusCode, body);
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
                return StatusCode(500, enc);
            }
        }


        [Authorize]
        [HttpGet("Gettaskinboxdatabyid")]
        public async Task<IActionResult> Gettaskinboxdatabyid([FromQuery] int id)
        {
            try
            {
                
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                           
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskinboxdatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GettaskReassigndatabyid")]
        public async Task<IActionResult> GettaskReassigndatabyid([FromQuery] int id)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");

                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskReassigndatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GettaskInitiatordatabyid ")]
        public async Task<IActionResult> GettaskInitiatordatabyid([FromQuery] int id)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");

                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskInitiatordatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GettaskApprovedatabyid")]
        public async Task<IActionResult> GettaskApprovedatabyid([FromQuery] int id)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");

                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskApprovedatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GettaskHolddatabyid")]
        public async Task<IActionResult> GettaskHolddatabyid([FromQuery] int id)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");

                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskHolddatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GettaskRejectdatabyid")]
        public async Task<IActionResult> GettaskRejectdatabyid([FromQuery] int id)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");

                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskRejectdatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("Getopentasklistdatabyid")]
        public async Task<IActionResult> Getopentasklistdatabyid([FromQuery] int id)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");

                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getopentasklistdatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GetboarddetailByid")]
        public async Task<IActionResult> GetboarddetailByid([FromQuery] int id)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GetboarddetailByid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GetmetalayoutByid")]
        public async Task<IActionResult> GetmetalayoutByid([FromQuery] int itaskno)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GetmetalayoutByid?itaskno={itaskno}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpPut("Updatetaskapprove")]
        public async Task<IActionResult> Updatetaskapprove([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl.TrimEnd('/')}/TaskMaster/Updatetaskapprove");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPut("UpdatetaskHold")]
        public async Task<IActionResult> UpdatetaskHold([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl.TrimEnd('/')}/TaskMaster/UpdatetaskHold");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpPut("UpdatetaskReject")]
        public async Task<IActionResult> UpdatetaskReject([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }

                var requestMessage = new HttpRequestMessage(HttpMethod.Put, $"{_baseUrl.TrimEnd('/')}/TaskMaster/UpdatetaskReject");
                requestMessage.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken.Split(" ").Last());
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(request), Encoding.UTF8, "application/json");
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
        [HttpGet("Getfile")]
        public async Task<IActionResult> Getfile(string fileName, string type)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                return EncryptedError(400, "Authorization token is missing");
            }
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
            string username = usernameClaim;

            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
            {
                var error = new APIResponse
                {
                    status = 401,
                    statusText = "Invalid or missing cTenantID in token."
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(401, encryptedError);
            }

            var (stream, contentType) = await _minioService.GetuserFileAsync(fileName,type, cTenantID);

            Response.Headers.Add("Content-Disposition", "inline");

            return File(stream, contentType);
        }
        private ActionResult EncryptedError(int status, string message)
        {
            var response = new APIResponse { status = status, statusText = message };
            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return Ok(encrypted);
        }
    
        [HttpGet("Getfileview")]
        public async Task<IActionResult> Getfileview(string fileName, string type,int tenant)
        {
            
            var (stream, contentType) = await _minioService.GetuserFileAsync(fileName, type, tenant);

            Response.Headers.Add("Content-Disposition", "inline");

            return File(stream, contentType);
        }


        [Authorize]
        [HttpGet("Getmetaviewdatabyid")]
        public async Task<IActionResult> Getmetaviewdatabyid([FromQuery] int id)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getmetaviewdatabyid?id={id}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
        [HttpGet("GettaskReassign")]
        public async Task<IActionResult> GettaskReassign([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskReassign?searchText={searchText}&page={page}&pageSize={pageSize}";
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


        [Authorize]
        [HttpGet("GettaskTimeline")]
        public async Task<IActionResult> GettaskTimeline([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskTimeline?searchText={searchText}&page={page}&pageSize={pageSize}";
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


        [Authorize]
        [HttpGet("GettaskTimelineDetails")]
        public async Task<IActionResult> GettaskTimelineDetails([FromQuery]  int itaskno)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GettaskTimelineDetails?itaskno={itaskno}";
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


        [Authorize]
        [HttpGet("Getworkflowdashboard")]
        public async Task<IActionResult> Getworkflowdashboard([FromQuery] string? searchText = null)
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Getworkflowdashboard?searchText={searchText}";
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

        [Authorize]
        [HttpGet("GetProcessmetadetailsbyid")]
        public async Task<IActionResult> GetProcessmetadetailsbyid([FromQuery] int itaskno, int processid)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");           
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/GetProcessmetadetailsbyid?processid={processid}&itaskno={itaskno}";
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, targetUrl);
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
    }
}