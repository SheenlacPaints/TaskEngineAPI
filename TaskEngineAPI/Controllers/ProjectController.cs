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


namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
 
    public class ProjectController : ControllerBase
    {

        private readonly IConfiguration _config;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IProjectService _ProjectService;
        private readonly IMinioService _minioService;
       
        public ProjectController(IConfiguration configuration, IJwtService jwtService, IProjectService ProjectService, IMinioService MinioService)
        {

            _config = configuration;
            _jwtService = jwtService;
            _ProjectService = ProjectService;
            _minioService = MinioService;

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
        [HttpPost]
        [Route("CreateProjectMaster")]
        public async Task<IActionResult> CreateProjectMaster([FromBody] pay request)
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

                CreateProjectDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<CreateProjectDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null)
                {
                    return CreateEncryptedResponse(400, "Failed to deserialize payload to ProjectCreateDTO");
                }

                int insertedUserId = await _ProjectService.InsertProjectMasterAsync(model, cTenantID, username);

                if (insertedUserId <= 0)
                {
                    return CreateEncryptedResponse(500, "Failed to create Process");
                }

                return CreatedSuccessResponse(new { projectid = insertedUserId }, "Project created successfully");
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
        [Route("Getprojectmaster")]
        public async Task<IActionResult> Getprojectmaster([FromQuery] string? searchText = null, string? type = null, int page = 1, int pageSize = 50)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();

                var json = await _ProjectService.Getprojectmaster(cTenantID, username, type, searchText, page, pageSize);

                var response = JsonConvert.DeserializeObject<TaskProjectResponse>(json);
                if (response == null)
                {
                    return CreateEncryptedResponse(500, "Invalid response format from service");
                }

                if (response.TotalCount == 0)
                {
                    return CreateEncryptedResponse(404, "No tasks found");
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
        [Route("Getprojectdropdown")]
        public async Task<IActionResult> Getprojectdropdown([FromQuery] string? searchText = null, string? type = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(type))
                {
                    return CreateEncryptedResponse(400, "table parameter is required");
                }
                var (cTenantID, username) = GetUserInfoFromToken();
                var json = await _ProjectService.Getprojectdropdown(cTenantID, username, type, searchText);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
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
        [HttpPost]
        [Route("CreateProjectDetails")]
        public async Task<IActionResult> CreateProjectDetails([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return CreateEncryptedResponse(400, "Payload is required");

                var (cTenantID, username) = GetUserInfoFromToken();

                List<ProjectDetailRequest> model;
                try
                {
                    var decryptedJson = AesEncryption.Decrypt(request.payload);
                    model = JsonConvert.DeserializeObject<List<ProjectDetailRequest>>(decryptedJson);
                }
                catch
                {
                    return CreateEncryptedResponse(400, "Invalid encrypted payload");
                }

                await _ProjectService.InsertProjectDetails(model, cTenantID, username);

                return CreatedSuccessResponse("Project details inserted successfully");
            }
            catch (ArgumentException ex)
            {
                return CreateEncryptedResponse(400, ex.Message);
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", ex.Message);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("UpdateProjectDetails")]
        public async Task<IActionResult> UpdateProjectDetails([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return CreateEncryptedResponse(400, "Payload is required");

                var (tenantId, username) = GetUserInfoFromToken();

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                var model = JsonConvert.DeserializeObject<ProjectDetailRequest>(decryptedJson);

                if (model == null)
                    return CreateEncryptedResponse(400, "Invalid payload data");

                var result = await _ProjectService.UpdateProjectDetails(
                    model,
                    tenantId,
                    username);

                if (!result)
                    return CreateEncryptedResponse(500, "Update failed");

                return CreatedSuccessResponse("Project details updated successfully");
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, "Internal server error", error: ex.Message);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetProjectList")]
        public async Task<IActionResult> GetProjectList()
        {
            try
            {
                var (tenantId, username) = GetUserInfoFromToken();

                string json = await _ProjectService.GetProjectList(tenantId, username);

                if (string.IsNullOrWhiteSpace(json))
                    return CreateEncryptedResponse(404, "No projects found");

                string encrypted = AesEncryption.Encrypt(json);

                return Content($"\"{encrypted}\"", "application/json");
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
        [Route("GetProjectslistbyid")]
        public async Task<IActionResult> GetProjectslistbyid([FromQuery] int projectId)
        {
            try
            {
                var (tenantId, username) = GetUserInfoFromToken();

                string json = await _ProjectService.GetProjectById(tenantId, username, projectId);

                if (string.IsNullOrWhiteSpace(json))
                    return CreateEncryptedResponse(404, "Project not found");

                string encrypted = AesEncryption.Encrypt(json);

                return Content($"\"{encrypted}\"", "application/json");
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
        [Route("CreateProjectVersion")]
        public async Task<IActionResult> CreateProjectVersion([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return CreateEncryptedResponse(400, "Payload is required");

                var (cTenantID, username) = GetUserInfoFromToken();

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return CreateEncryptedResponse(400, "Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<CreateProjectVersionRequest>(decryptedJson);

                if (model == null)
                    return CreateEncryptedResponse(400, "Invalid payload data");

                if (model.ProjectId <= 0)
                    return CreateEncryptedResponse(400, "Valid ProjectId is required");

                if (string.IsNullOrEmpty(model.Description))
                    return CreateEncryptedResponse(400, "Description is required");

                int newVersionId = await _ProjectService.CreateProjectVersionAsync(
                    model.ProjectId,
                    model.Description,
                    model.ExpectedDate,
                    username
                );

                if (newVersionId <= 0)
                    return CreateEncryptedResponse(500, "Failed to create project version");

                return CreatedSuccessResponse(
                    new
                    {
                        versionId = newVersionId,
                        projectId = model.ProjectId,
                        createdBy = username,
                        createdAt = DateTime.UtcNow
                    },
                    "Project version created successfully"
                );
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
