using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Data;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Services;
using System.Net.Http;
using System.Security.Claims;

namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskMasterController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;
        private readonly ITaskMasterService taskMasterService;
        private readonly ILogger<TaskMasterController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        public TaskMasterController(IConfiguration config, IHttpClientFactory httpClientFactory, IJwtService jwtService, ITaskMasterService taskMasterService, ILogger<TaskMasterController> logger)
        {
            _config = config;
            _jwtService = jwtService;
            this.taskMasterService = taskMasterService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }
        private (int cTemantID, string username) GetUserInfoFromToken()
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                string.IsNullOrWhiteSpace(usernameClaim))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }
            return (cTenantID, usernameClaim);
        }
        private T DeserializePayload<T>(string encryptedPayload) where T : class
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(encryptedPayload);
                return JsonConvert.DeserializeObject<T>(decryptedJson);
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Failed to deserialize payload: {ex.Message}", ex);
            }
        }
        private IActionResult CreateEncryptedResponse(int statusCode, string message, object body = null, string error = null)
        {
            var response = new APIResponse
            {
                status = statusCode,
                statusText = message,
                body = body != null ? new object[] { body } : Array.Empty<object>(),
                error = error
            };
            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(statusCode, encrypted);
        }

        private IActionResult CreatedSuccessResponse(object data, string message = "Successful")
        {
            object[] responseBody;

            if (data == null)
            {
                responseBody = Array.Empty<object>();
            }

            else if (data is System.Collections.IEnumerable enumerableData)
            {
                responseBody = enumerableData.Cast<object>().ToArray();
            }
            else
            {
                responseBody = new object[] { data };
            }
            var response = new APIResponse
            {
                status = 200,
                statusText = message,
                body = responseBody,
            };
            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return Ok(encrypted);
        }

        private IActionResult CreatedDataResponse(List<Dictionary<string, object>> data, string noDataMessage = "No data found")
        {
            var hasData = data != null && data.Any();
            var response = new APIResponse
            {
                status = hasData ? 200 : 204,
                statusText = hasData ? "Successful" : noDataMessage,
                body = hasData ? data.Cast<object>().ToArray() : Array.Empty<object>(),
            };
            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [Authorize]
        [HttpPost]
        [Route("InsertTask")]
        public async Task<IActionResult> InsertTask([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Request payload is required");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var model = DeserializePayload<TaskMasterDTO>(request.payload);
                int insertedUserId = await taskMasterService.InsertTaskMasterAsync(model, cTenantID, username);
                if (insertedUserId <= 0)
                {
                    throw new InvalidOperationException("Task insertion failed.");
                }

                return CreatedSuccessResponse(new { UserID = insertedUserId }, "Task inserted successfully.");


            }
            catch (UnauthorizedAccessException ex)
            {
                throw;
            }

            catch (Exception ex)
            {
                throw;
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetMetadetailbyid")]
        public async Task<IActionResult> GetMetadetailbyid([FromQuery] int processid)
        {
            try
            {
                if (processid <= 0)
                {
                    return CreateEncryptedResponse(400, "processid must be greater than 0");
                }
                var (cTenantID, _) = GetUserInfoFromToken();
                var json = await taskMasterService.GetAllProcessmetaAsync(cTenantID, processid);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{processid} not found.", new { status = 400, data = Array.Empty<object>() });
                }
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetProcessMetadetailbyid")]
        public async Task<IActionResult> GetProcessMetadetailbyid([FromQuery] int metaid)
        {
            try
            {

                if (metaid <= 0)
                {
                    return CreateEncryptedResponse(400, "metaid must be greater than 0");
                }
                var (cTenantID, _) = GetUserInfoFromToken();
                var json = await taskMasterService.GetAllProcessmetadetailAsync(cTenantID, metaid);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        [Route("Getdepartmentroleposition")]
        public async Task<IActionResult> Getdepartmentroleposition([FromQuery] string table)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(table))
                {
                    return CreateEncryptedResponse(400, "table parameter is required");
                }
                var (cTenantID, _) = GetUserInfoFromToken();
                var json = await taskMasterService.Getdepartmentroleposition(cTenantID, table);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getprocessengineprivilege")]
        public async Task<IActionResult> Getprocessengineprivilege([FromQuery] string value, string cprivilege)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    return CreateEncryptedResponse(400, "value parameter is required");
                }
                if (string.IsNullOrWhiteSpace(cprivilege))
                {
                    return CreateEncryptedResponse(400, "cprivilege parameter is required");
                }             
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await taskMasterService.Getprocessengineprivilege(cTenantID, value, cprivilege, username);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getdropdown")]
        public async Task<IActionResult> Getdropdown([FromQuery] string column)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(column))
                {
                    return CreateEncryptedResponse(400, "column parameter is required");
                }
                var (cTenantID, _) = GetUserInfoFromToken();
                var json = await taskMasterService.Getdropdown(cTenantID, column);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("GetDropDownFilter")]
        public async Task<IActionResult> GetDropDownFilter([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Request payload is required");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var filterDto = JsonConvert.DeserializeObject<GetDropDownFilterDTO>(decryptedJson);

                if (filterDto == null || string.IsNullOrWhiteSpace(filterDto.filtervalue1))
                {
                    return CreateEncryptedResponse(400, "filtervalue1 (column name) is required");
                }

                var (cTenantID, _) = GetUserInfoFromToken();

                var json = await taskMasterService.GetDropDownFilterAsync(cTenantID, filterDto);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access");
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskapprove")]
        public async Task<IActionResult> Gettaskapprove([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await taskMasterService.Gettaskapprove(cTenantID, username, searchText, page, pageSize);
                var response = JsonConvert.DeserializeObject<TaskInboxResponse>(json);
                if (response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found in your inbox");
                }

                return CreatedSuccessResponse(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (JsonException jsonEx)
            {
                return CreateEncryptedResponse(500, "Invalid JSON response", error: jsonEx.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        [Route("GettaskHold")]
        public async Task<IActionResult> GettaskHold([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await taskMasterService.GettaskHold(cTenantID, username, searchText, page, pageSize);
                var response = JsonConvert.DeserializeObject<TaskInboxResponse>(json);
                if (response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found in your inbox");
                }
                return CreatedSuccessResponse(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        [Route("GettaskReject")]
        public async Task<IActionResult> GettaskReject([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await taskMasterService.GettaskReject(cTenantID, username, searchText, page, pageSize);
                var response = JsonConvert.DeserializeObject<TaskInboxResponse>(json);
                if (response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found in your inbox");
                }
                return CreatedSuccessResponse(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getopentasklist")]
        public async Task<IActionResult> Getopentasklist([FromQuery] string? searchText = null)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await taskMasterService.Getopentasklist(cTenantID, username,searchText);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("DeptposrolecrudAsync")]
        public async Task<IActionResult> DeptposrolecrudAsync([FromBody] pay request)
        {

            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Request payload is required");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var model = DeserializePayload<DeptPostRoleDTO>(request.payload);
                var json = (await taskMasterService.DeptposrolecrudAsync(model, cTenantID, username)).ToString();
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access");
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize]
        [HttpPost]
        [Route("Processprivilegemapping")]
        public async Task<IActionResult> Processprivilegemapping([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Request payload is required");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var model = DeserializePayload<privilegeMappingDTO>(request.payload);

                if (model == null)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                if (!model.privilege.HasValue)
                {
                    return CreateEncryptedResponse(400, "Privilege is required");
                }

                if (!model.cprocess_id.HasValue || model.cprocess_id <= 0)
                {
                    return CreateEncryptedResponse(400, "Valid Process ID is required");
                }

                if (string.IsNullOrWhiteSpace(model.cprocess_code))
                {
                    return CreateEncryptedResponse(400, "Process Code is required");
                }

                if (model.privilegeMapping == null || !model.privilegeMapping.Any())
                {
                    return CreateEncryptedResponse(400, "Privilege mapping cannot be empty");
                }

                int masterId = await taskMasterService.Processprivilege_mapping(model, cTenantID, username);

                if (masterId > 0)
                {
                    return CreatedSuccessResponse(new { masterId = masterId }, "Process privilege mapping created successfully");
                }
                else
                {
                    return CreateEncryptedResponse(500, "Failed to create process privilege mapping");
                }
            }
            catch (Exception ex)
            {

                return CreateEncryptedResponse(500, $"Internal server error: {ex.Message}");
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinitiator")]
        public async Task<IActionResult> Gettaskinitiator([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var json = await taskMasterService.GetTaskInitiatornew(cTenantID, username,searchText,page,pageSize);
                var response = JsonConvert.DeserializeObject<TaskInboxResponse>(json);

                if(response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found in your inbox");
                }

                return CreatedSuccessResponse(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinbox")]
        public async Task<IActionResult> Gettaskinbox([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                var json = await taskMasterService.Gettaskinbox(cTenantID, username, searchText, page, pageSize);

                var response = JsonConvert.DeserializeObject<TaskInboxResponse>(json);
                if (response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found in your inbox");
                }

                return CreatedSuccessResponse(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (JsonException jsonEx)
            {
                return CreateEncryptedResponse(500, "Invalid JSON response", error: jsonEx.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }



        [Authorize]
        [HttpGet]
        [Route("GetboarddetailByid")]
        public async Task<IActionResult> GetboarddetailByid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GetTaskConditionBoard(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinboxdatabyid")]
        public async Task<IActionResult> Gettaskinboxdatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.Gettaskinboxdatabyid(cTenantID, id, username);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GettaskApprovedatabyid")]
        public async Task<IActionResult> GettaskApprovedatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.Gettaskapprovedatabyid(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }


        [Authorize]
        [HttpGet]
        [Route("GettaskInitiatordatabyid")]
        public async Task<IActionResult> GettaskInitiatordatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GettaskInitiatordatabyid(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }

        [Authorize]
        [HttpGet]
        [Route("GettaskReassigndatabyid")]
        public async Task<IActionResult> GettaskReassigndatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GettaskReassigndatabyid(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }


        [Authorize]
        [HttpGet]
        [Route("GettaskHolddatabyid")]
        public async Task<IActionResult> GettaskHolddatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GettaskHolddatabyid(cTenantID, id,username);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }

        [Authorize]
        [HttpGet]
        [Route("GettaskRejectdatabyid")]
        public async Task<IActionResult> GettaskRejectdatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GettaskRejectdatabyid(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }

        [Authorize]
        [HttpGet]
        [Route("Getopentasklistdatabyid")]
        public async Task<IActionResult> Getopentasklistdatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.Getopentasklistdatabyid(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }

        [Authorize]
        [HttpGet]
        [Route("GetmetalayoutByid")]
        public async Task<IActionResult> GetmetalayoutByid([FromQuery] int itaskno)
        {
            try
            {
                if (itaskno <= 0)
                {
                    return CreateEncryptedResponse(400, "itaskno must be greater than 0");
                }
                if(!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GetmetalayoutByid(cTenantID, itaskno);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{itaskno} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpPut]
        [Route("Updatetaskapprove")]
        public async Task<IActionResult> Updatetaskapprove([FromBody] pay request)
        {
            try
            {
                if (request == null)
                {
                    return CreateEncryptedResponse(400, "Request body cannot be null");
                }

                if (string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Payload cannot be empty");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch (Exception ex)
                {
                    return CreateEncryptedResponse(400, "Invalid encrypted payload format");
                }

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    return CreateEncryptedResponse(400, "Decrypted payload is empty");
                }

                updatetaskDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<updatetaskDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null || model.ID <= 0)
                {
                    return CreateEncryptedResponse(400, "Invalid ID provided");
                }

                bool success = await taskMasterService.UpdatetaskapproveAsync(model, cTenantID, username);
                if (model.reassignto != null)
                {
                    bool successss = await taskMasterService.sendwhatappnotificationAsync(model, cTenantID, username);
                }
                if (model.status == "H")
                {
                    bool holdsuccessss = await taskMasterService.holdwhatappnotificationAsync(model, cTenantID, username);
                }

                


                if (!success)
                {
                    return CreateEncryptedResponse(404, "Data not found or update failed");
                }

                //return CreatedSuccessResponse(null, "Updated successfully");
                return CreatedSuccessResponse( model.ID, "Updated successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {

                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpPut]
        [Route("UpdatetaskHold")]
        public async Task<IActionResult> UpdatetaskHold([FromBody] pay request)
        {
            try
            {
                if (request == null)
                {
                    return CreateEncryptedResponse(400, "Request body cannot be null");
                }

                if (string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Payload cannot be empty");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch (Exception ex)
                {
                    return CreateEncryptedResponse(400, "Invalid encrypted payload format");
                }

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    return CreateEncryptedResponse(400, "Decrypted payload is empty");
                }

                updatetaskDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<updatetaskDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null || model.ID <= 0)
                {
                    return CreateEncryptedResponse(400, "Invalid ID provided");
                }

                bool success = await taskMasterService.UpdatetaskHoldAsync(model, cTenantID, username);

                if (!success)
                {
                    return CreateEncryptedResponse(404, "Data not found or update failed");
                }

                return CreatedSuccessResponse(null, "Updated successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {

                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpPut]
        [Route("UpdatetaskReject")]
        public async Task<IActionResult> UpdatetaskReject([FromBody] pay request)
        {
            try
            {
                if (request == null)
                {
                    return CreateEncryptedResponse(400, "Request body cannot be null");
                }

                if (string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Payload cannot be empty");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch (Exception ex)
                {
                    return CreateEncryptedResponse(400, "Invalid encrypted payload format");
                }

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    return CreateEncryptedResponse(400, "Decrypted payload is empty");
                }

                updatetaskDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<updatetaskDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null || model.ID <= 0)
                {
                    return CreateEncryptedResponse(400, "Invalid ID provided");
                }

                bool success = await taskMasterService.UpdatetaskRejectAsync(model, cTenantID, username);

                if (!success)
                {
                    return CreateEncryptedResponse(404, "Data not found or update failed");
                }

                return CreatedSuccessResponse(null, "Updated successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {

                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        [Route("Getmetaviewdatabyid")]
        public async Task<IActionResult> Getmetaviewdatabyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "id must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.Getmetaviewdatabyid(cTenantID, id);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

      


        [Authorize]
        [HttpGet]
        [Route("GettaskReassign")]
        public async Task<IActionResult> GettaskReassign([FromQuery] string? searchText = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();             
                var result = await taskMasterService.GettaskReassign(cTenantID, username, searchText, page, pageSize);

                
                if (result == null || result.data == null || result.data.Count == 0)
                {
                    return CreateEncryptedResponse(400, "No data found");
                }

                return CreatedSuccessResponse(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GettaskTimeline")]
        public async Task<IActionResult> GettaskTimeline([FromQuery] string? searchText = null, int pageNo = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                var json = await taskMasterService.Gettasktimeline(cTenantID, username, searchText, pageNo, pageSize);


                var response = JsonConvert.DeserializeObject<TaskInboxResponse>(json);
                if (response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found in your inbox");
                }

                return CreatedSuccessResponse(response);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (JsonException jsonEx)
            {
                return CreateEncryptedResponse(500, "Invalid JSON response", error: jsonEx.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }



        [Authorize]
        [HttpGet]
        [Route("GettaskTimelineDetails")]
        public async Task<IActionResult> GettaskTimelineDetails([FromQuery] int itaskno)
        {
            try
            {
                if (itaskno <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();

                var data = await taskMasterService.GettasktimelinedetailAsync(itaskno, username,cTenantID);

                if (data == null || !data.Any())
                {
                    return CreateEncryptedResponse(400, $"{itaskno} not found.", new { status = 400, data = Array.Empty<object>() });
                }

                return CreatedSuccessResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }

        }




        [Authorize]
        [HttpGet]
        [Route("Getworkflowdashboard")]
        public async Task<IActionResult> Getworkflowdashboard([FromQuery] string? searchtext)
        {
            try
            {
                
                
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await taskMasterService.Getworkflowdashboard(cTenantID, username, searchtext);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                return CreatedDataResponse(data);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }


        [Authorize]
        [HttpGet]
        [Route("GetProcessmetadetailsbyid")]
        public async Task<IActionResult> GetProcessmetadetailsbyid([FromQuery] int itaskno, [FromQuery] int processid)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                string jsonResult = await taskMasterService.GetProcessmetadetailsbyid(itaskno, cTenantID, processid);

                var dataList = JsonConvert.DeserializeObject<List<object>>(jsonResult);

                if (dataList == null || !dataList.Any())
                {
                    return CreateEncryptedResponse(404, $"Task No {itaskno} metadata not found.", new { status = 404, data = Array.Empty<object>() });
                }
                return CreatedSuccessResponse(dataList);
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getnotification")]
        public async Task<IActionResult> Getnotification()
        {
            try
            {
                var (cTenantID, username) = GetUserInfoFromToken();

                await SendWhatsAppNotificationAsync();

                return CreateEncryptedResponse(200, "Notification processed successfully");
            }
            catch (UnauthorizedAccessException ex)
            {
                return CreateEncryptedResponse(401, "Unauthorized access", error: ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Notification API failed");
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }


        private async Task SendWhatsAppNotificationAsync()
        {
            var url = "https://backend.api-wa.co/campaign/smartping/api/v2";

            var client = _httpClientFactory.CreateClient();

            var payload = new
            {
                apiKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpZCI6IjY5MTViZmQ3NjFiNDMzMGQ1Y2IzMGM0ZSIsIm5hbWUiOiJTaGVlbmxhYyBQYWludHMiLCJhcHBOYW1lIjoiQWlTZW5zeSIsImNsaWVudElkIjoiNjkxNWJmZDc2MWI0MzMwZDVjYjMwYzQ3IiwiYWN0aXZlUGxhbiI6Ik5PTkUiLCJpYXQiOjE3NjMwMzMwNDd9.Qpd0HmsXQxTGx_v0EkOHKTUN-gEAzoRDahaiMtT4lQU",
                campaignName = "reassighn new process",
                destination = "917402023513",
                userName = "Sheenlac Paintss",
                templateParams = new[] { "$FirstName", "$FirstName", "$FirstName", "$FirstName" },
                source = "new-landing-page form",
                media = new { },
                buttons = new string[] { },
                carouselCards = new string[] { },
                location = new { },
                attributes = new { },
                paramsFallbackValue = new { FirstName = "user",itaskno= "user" }
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