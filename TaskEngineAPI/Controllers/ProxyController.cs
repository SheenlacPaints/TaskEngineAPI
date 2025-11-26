using System.Data.Common;
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

                var requestMessage = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}Account/fileUpload");
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
        [HttpGet("Gettaskinbox")]
        public async Task<IActionResult> Gettaskinbox()
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskinbox";
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
        public async Task<IActionResult> Gettaskapprove()
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskapprove";
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
        [HttpGet("Gettaskhold")]
        public async Task<IActionResult> Gettaskhold()
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskhold";
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
        public async Task<IActionResult> Gettaskinitiator()
        {
            try
            {
                // 🔐 Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(jwtToken))
                    return Unauthorized("Missing Authorization token.");
                // 🔗 Build full URL with encrypted query             
                string targetUrl = $"{_baseUrl.TrimEnd('/')}/TaskMaster/Gettaskinitiator";
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
        [HttpGet("GetAllAPISyncConfigAsync")]
        public async Task<IActionResult> GetAllAPISyncConfigAsync()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return Unauthorized("Missing Authorization token.");
                }
                var requestMessage = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl.TrimEnd('/')}/Account/GetAllAPISyncConfigAsync");
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