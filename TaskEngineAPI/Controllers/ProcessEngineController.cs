using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using TaskEngineAPI.Data;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Services;

namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessEngineController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IJwtService _jwtService;
        private readonly IAdminService _AccountService;
        private readonly IProcessEngineService _processEngineService;
        private readonly ILogger<ProcessEngineController> _logger;

        public ProcessEngineController(
            IConfiguration configuration,
            IJwtService jwtService,
            IAdminService AccountService,
            IProcessEngineService processEngineService,
            ILogger<ProcessEngineController> logger)
        {
            _config = configuration;
            _jwtService = jwtService;
            _AccountService = AccountService;
            _processEngineService = processEngineService;
            _logger = logger;
        }

        private (int cTenantID, string username) GetUserInfoFromToken()
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
            else if (data is System.Collections.IEnumerable enumerableData && !(data is string))
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

        private IActionResult CreatedSuccessResponse<T>(List<T> data, string noDataMessage = "No data found")
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
        [HttpGet]
        [Route("GetAllProcesstype")]
        public async Task<IActionResult> GetAllProcesstype()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, _) = GetUserInfoFromToken();
                var processtypes = await _processEngineService.GetAllProcessenginetypeAsync(cTenantID);

                return CreatedSuccessResponse(processtypes, "No Process Engine Type found");
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
        [Route("CreateProcessEngine")]
        public async Task<IActionResult> CreateProcessEngine([FromBody] pay request)
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

                ProcessEngineDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<ProcessEngineDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null)
                {
                    return CreateEncryptedResponse(400, "Failed to deserialize payload to ProcessEngineDTO");
                }

                int insertedUserId = await _processEngineService.InsertProcessEngineAsync(model, cTenantID, username);

                if (insertedUserId <= 0)
                {
                    return CreateEncryptedResponse(500, "Failed to create Process");
                }

                return CreatedSuccessResponse(new { processid = insertedUserId }, "Process created successfully");
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
        [Route("UpdateProcessEngine")]
        public async Task<IActionResult> UpdateProcessEngine([FromBody] pay request)
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

                UpdateProcessEngineDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<UpdateProcessEngineDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null || model.ID <= 0)
                {
                    return CreateEncryptedResponse(400, "Invalid ID provided");
                }

                bool success = await _processEngineService.UpdateProcessEngineAsync(model, cTenantID, username);

                if (!success)
                {
                    return CreateEncryptedResponse(404, "Data not found or update failed");
                }

                return CreatedSuccessResponse(model.ID, "Updated successfully");
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already assigned"))
            {
                return CreateEncryptedResponse(409, ex.Message);
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
        [Route("Updateprocessmapping")]
        public async Task<IActionResult> Updateprocessmapping([FromBody] pay request)
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

                updateprocessmappingDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<updateprocessmappingDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null)
                {
                    return CreateEncryptedResponse(400, "Invalid process mapping data");
                }

                bool success = await _processEngineService.UpdateprocessmappingAsync(model, cTenantID, username);

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
        [Route("GetAllProcessEngine")]
        public async Task<IActionResult> GetAllProcessEngine(
            string? searchText = null,
            int page = 1,
            int pageSize = 10,
            int? created_by = null,
            string? priority = null,
            int? status = null)
        {
            try
            {
                var (cTenantID, username) = GetUserInfoFromToken();

                if (page < 1)
                {
                    return CreateEncryptedResponse(400, "Page number must be greater than or equal to 1");
                }

                if (pageSize < 1 || pageSize > 100)
                {
                    return CreateEncryptedResponse(400, "Page size must be between 1 and 100");
                }

                if (!string.IsNullOrWhiteSpace(searchText) && searchText.Length > 200)
                {
                    return CreateEncryptedResponse(400, "Search text cannot exceed 200 characters");
                }

                var engines = await _processEngineService.GetAllProcessengineAsync(
                    cTenantID, username, searchText, page, pageSize, created_by, priority, status);


               
                return CreatedSuccessResponse(engines, "Successful");
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
        [Route("GetProcessEnginebyid")]
        public async Task<IActionResult> GetProcessEnginebyid([FromQuery] int id)
        {
            try
            {
                if (id <= 0)
                {
                    return CreateEncryptedResponse(400, "ID must be greater than 0");
                }

                var (cTenantID, username) = GetUserInfoFromToken();
                var processEngine = await _processEngineService.GetProcessengineAsync(cTenantID, id);

                if (processEngine == null || !processEngine.Any())
                {
                    return CreateEncryptedResponse(404, $"Process engine with ID {id} not found");
                }

                return CreatedSuccessResponse(processEngine, "No data found");
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
        [Route("Updateprocessstatusdelete")]
        public async Task<IActionResult> Updateprocessstatusdelete([FromBody] pay request)
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

                updatestatusdeleteDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<updatestatusdeleteDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null || model.ID <= 0)
                {
                    return CreateEncryptedResponse(400, "Invalid ID provided");
                }

                bool success = await _processEngineService.UpdateProcessenginestatusdeleteAsync(model, cTenantID, username);

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
        [HttpPost]
        [Route("CreateProcessmapping")]
        public async Task<IActionResult> CreateProcessMapping([FromBody] pay request)
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

                createprocessmappingDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<createprocessmappingDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null)
                {
                    return CreateEncryptedResponse(400, "Invalid process mapping data");
                }

                int insertedUserId = await _processEngineService.InsertprocessmappingAsync(model, cTenantID, username);

                if (insertedUserId == -1)
                {
                    return CreateEncryptedResponse(409, $"Process privilege '{model.cprivilegeType}' is already assigned to this process. Please choose a different privilege number.");
                }

                if (insertedUserId <= 0)
                {
                    return CreateEncryptedResponse(500, "Failed to create Process mapping");
                }

                return CreatedSuccessResponse(new { processid = insertedUserId }, "Process mapped successfully");
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
        [HttpDelete]
        [Route("DeleteProcessMapping")]
        public async Task<IActionResult> DeleteProcessMapping([FromQuery] pay request)
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

                DeleteProcessMappingDTO deleteModel;
                try
                {
                    deleteModel = JsonConvert.DeserializeObject<DeleteProcessMappingDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (deleteModel?.MappingId <= 0)
                {
                    return CreateEncryptedResponse(400, "Invalid mapping ID provided");
                }

                bool success = await _processEngineService.DeleteprocessmappingAsync(deleteModel.MappingId, cTenantID, username);

                if (!success)
                {
                    return CreateEncryptedResponse(404, "Process mapping not found or you don't have permission to delete it");
                }

                return CreatedSuccessResponse(null, "Process mapping deleted successfully");
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
        [Route("getMappingList")]
        public async Task<IActionResult> GetMappingList()
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var mappingList = await _processEngineService.GetMappingListAsync(cTenantID);

                return CreatedSuccessResponse(mappingList, "No mappings found");
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

    }
}