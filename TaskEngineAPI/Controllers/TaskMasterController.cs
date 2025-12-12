using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Services;

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

        public TaskMasterController(IConfiguration config, IJwtService jwtService, ITaskMasterService taskMasterService, ILogger<TaskMasterController> logger)
        {
            _config = config;
            _jwtService = jwtService;
            this.taskMasterService = taskMasterService;
            _logger = logger;
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
            var (cTenantID, _) = GetUserInfoFromToken();
            var json = await taskMasterService.GetAllProcessmetaAsync(cTenantID, processid);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return CreatedDataResponse(data);
        }

        [Authorize]
        [HttpGet]
        [Route("Getdepartmentroleposition")]
        public async Task<IActionResult> Getdepartmentroleposition([FromQuery] string table)
        {
            var (cTenantID, _) = GetUserInfoFromToken();
            var json = await taskMasterService.Getdepartmentroleposition(cTenantID, table);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return CreatedDataResponse(data);
        }

        [Authorize]
        [HttpGet]
        [Route("Getprocessengineprivilege")]
        public async Task<IActionResult> Getprocessengineprivilege([FromQuery] string value, string cprivilege)
        {
            var (cTenantID, _) = GetUserInfoFromToken();
            var json = await taskMasterService.Getprocessengineprivilege(cTenantID, value, cprivilege);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return CreatedDataResponse(data);
        }

        [Authorize]
        [HttpGet]
        [Route("Getdropdown")]
        public async Task<IActionResult> Getdropdown([FromQuery] string column)
        {
            var (cTenantID, _) = GetUserInfoFromToken();
            var json = await taskMasterService.Getdropdown(cTenantID, column);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return CreatedDataResponse(data);
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
        public async Task<IActionResult> Gettaskapprove()
        {
            var (cTenantID, username) = GetUserInfoFromToken();
            var json = await taskMasterService.Gettaskapprove(cTenantID, username);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            return CreatedDataResponse(data);
        }

        [Authorize]
        [HttpPost]
        [Route("DeptposrolecrudAsync")]
        public async Task<IActionResult> DeptposrolecrudAsync([FromBody] pay request)
        {
            var (cTenantID, username) = GetUserInfoFromToken();

            var model = DeserializePayload<DeptPostRoleDTO>(request.payload);
            var json = (await taskMasterService.DeptposrolecrudAsync(model, cTenantID, username)).ToString();
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            return CreatedDataResponse(data);
        }


        [Authorize]
        [HttpPost]
        [Route("Processprivilegemapping")]
        public async Task<IActionResult> Processprivilegemapping([FromBody] pay request)
        {
            try
            {
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
        public async Task<IActionResult> Gettaskinitiator()
        {
            var (cTenantID, username) = GetUserInfoFromToken();

            var json = await taskMasterService.GetTaskInitiator(cTenantID, username);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            return CreatedSuccessResponse(data);
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinbox")]
        public async Task<IActionResult> Gettaskinbox()
        {
            var (cTenantID, username) = GetUserInfoFromToken();

            var json = await taskMasterService.Gettaskinbox(cTenantID, username);
            var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

            return CreatedSuccessResponse(data);
        }


        [Authorize]
        [HttpGet]
        [Route("GetboarddetailByid")]
        public async Task<IActionResult> GetboarddetailByid([FromQuery] int id)
        {
            var (cTenantID, username) = GetUserInfoFromToken();

            var data = await taskMasterService.GetTaskConditionBoard(cTenantID, id);

            if (data == null || !data.Any())
            {
                return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
            }

            return CreatedSuccessResponse(data);
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinboxdatabyid")]
        public async Task<IActionResult> Gettaskinboxdatabyid([FromQuery] int id)
        {
            var (cTenantID, username) = GetUserInfoFromToken();

            var data = await taskMasterService.Gettaskinboxdatabyid(cTenantID, id);

            if (data == null || !data.Any())
            {
                return CreateEncryptedResponse(400, $"{id} not found.", new { status = 400, data = Array.Empty<object>() });
            }

            return CreatedSuccessResponse(data);
        }

        [Authorize]
        [HttpGet]
        [Route("GetmetalayoutByid")]
        public async Task<IActionResult> GetmetalayoutByid([FromQuery] int itaskno)
        {
            var (cTenantID, username) = GetUserInfoFromToken();

            var data = await taskMasterService.GetmetalayoutByid(cTenantID, itaskno);

            if (data == null || !data.Any())
            {
                return CreateEncryptedResponse(400, $"{itaskno} not found.", new { status = 400, data = Array.Empty<object>() });
            }

            return CreatedSuccessResponse(data);
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

    }
}