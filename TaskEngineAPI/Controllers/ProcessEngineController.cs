using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IAdminService _AccountService;
        private readonly IProcessEngineService _processEngineService;

        private readonly ApplicationDbContext _context;
        public ProcessEngineController(IConfiguration configuration, IJwtService jwtService, IAdminService AccountService, IProcessEngineService processEngineService)
        {

            _config = configuration;
            _jwtService = jwtService;
            _AccountService = AccountService;
            _processEngineService = processEngineService;


        }

        [Authorize]
        [HttpGet]
        [Route("GetAllProcesstype")]
        public async Task<ActionResult> GetAllProcesstype()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {

                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var Processtypes = await _processEngineService.GetAllProcessenginetypeAsync(cTenantID);

                var response = new APIResponse
                {
                    body = Processtypes?.ToArray() ?? Array.Empty<object>(),
                    statusText = Processtypes == null || !Processtypes.Any() ? "No Process Engine Type found" : "Successful",
                    status = Processtypes == null || !Processtypes.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    body = Array.Empty<object>(),
                    statusText = $"Error: {ex.Message}",
                    status = 500
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        private ActionResult EncryptedError(int status, string message)
        {
            var response = new APIResponse { status = status, statusText = message };
            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return Ok(encrypted);
        }

        private ActionResult EncryptedSuccess(string message)
        {
            var response = new APIResponse { status = 200, statusText = message };
            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return Ok(encrypted);
        }


        [Authorize]
        [HttpPost]
        [Route("CreateProcessEngine")]
        public async Task<IActionResult> CreateProcessEngine([FromBody] pay request)
        {
            try
            {

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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
                    return StatusCode(401, $"\"{encryptedError}\"");
                }
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<ProcessEngineDTO>(decryptedJson);

                int insertedUserId = await _processEngineService.InsertProcessEngineAsync(model, cTenantID, username);

                if (insertedUserId <= 0)
                {
                    return StatusCode(500, new APIResponse
                    {
                        status = 500,
                        statusText = "Failed to create Process"
                    });
                }
                // Prepare response
                var apierDtls = new APIResponse
                {
                    status = 200,
                    statusText = "Process created successfully",
                    body = new object[] { new { processid = insertedUserId } }
                };
                string jsone = JsonConvert.SerializeObject(apierDtls);
                var encryptapierDtls = AesEncryption.Encrypt(jsone);
                //return StatusCode(200, encryptapierDtls);
                return StatusCode(200, $"\"{encryptapierDtls}\"");

            }
            catch (Exception ex)
            {

                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Error creating Super Admin ",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);

            }
        }


        [Authorize]
        [HttpGet]
        [Route("GetAllProcessEngine")]
        public async Task<ActionResult> GetAllProcessEngine()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {

                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var superAdmins = await _processEngineService.GetAllProcessengineAsync(cTenantID);



                var response = new APIResponse
                {
                    body = superAdmins?.ToArray() ?? Array.Empty<object>(),
                    statusText = superAdmins == null || !superAdmins.Any() ? "No SuperAdmins found" : "Successful",
                    status = superAdmins == null || !superAdmins.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);


                var encrypted = AesEncryption.Encrypt(jsoner);

                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    body = Array.Empty<object>(),
                    statusText = $"Error: {ex.Message}",
                    status = 500
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }



        [Authorize]
        [HttpGet]
        [Route("GetProcessEnginebyid")]
        public async Task<ActionResult> GetProcessEnginebyid([FromQuery] int id)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {

                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var superAdmins = await _processEngineService.GetProcessengineAsync(cTenantID, id);



                var response = new APIResponse
                {
                    body = superAdmins?.ToArray() ?? Array.Empty<object>(),
                    statusText = superAdmins == null || !superAdmins.Any() ? "No SuperAdmins found" : "Successful",
                    status = superAdmins == null || !superAdmins.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);


                var encrypted = AesEncryption.Encrypt(jsoner);

                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    body = Array.Empty<object>(),
                    statusText = $"Error: {ex.Message}",
                    status = 500
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPut]
        [Route("Updateprocessstatusdelete")]
        public async Task<IActionResult> Updateprocessstatusdelete([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
            string username = usernameClaim;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<updatestatusdeleteDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
            {
                throw new ArgumentException("Invalid ID provided");
            }

            bool success = await _processEngineService.UpdateProcessenginestatusdeleteAsync(model, cTenantID, username);

            var response = new APIResponse
            {
                status = success ? 200 : 400,
                statusText = success ? "updated successfully" : "data not found or update failed"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

       
        [Authorize]
        [HttpPost]
        [Route("CreateProcessmapping")]
        public async Task<IActionResult> CreateProcessMapping([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<createprocessmappingDTO>(decryptedJson);

                int insertedUserId = await _processEngineService.InsertprocessmappingAsync(model, cTenantID, username);

                if (insertedUserId == -1)
                {
                    var errorResponse = new APIResponse
                    {
                        status = 409,
                        statusText = $"Process privilege '{model.cprivilegeType}' is already assigned to this process. Please choose a different privilege number."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(409, encryptedError);
                }

                if (insertedUserId <= 0)
                {
                    return StatusCode(500, new APIResponse
                    {
                        status = 500,
                        statusText = "Failed to create Process mapping"
                    });
                }

                var apierDtls = new APIResponse
                {
                    status = 200,
                    statusText = "Process mapped successfully",
                    body = new object[] { new { processid = insertedUserId } }
                };
                string jsone = JsonConvert.SerializeObject(apierDtls);
                var encryptapierDtls = AesEncryption.Encrypt(jsone);
                return StatusCode(200, encryptapierDtls);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Error in Process mapping",
                    error = ex.Message
                };
                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }



        [Authorize]
        [HttpPut]
        [Route("Updateprocessmapping")]
        public async Task<IActionResult> Updateprocessmapping([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<updateprocessmappingDTO>(decryptedJson);

                if (model == null || model.cmappingid <= 0)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Invalid ID provided"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, encryptedError);
                }

                bool success = await _processEngineService.UpdateprocessmappingAsync(model, cTenantID, username);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "Updated successfully" : "Data not found or update failed"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("already assigned"))
            {
                var error = new APIResponse
                {
                    status = 409,
                    statusText = ex.Message
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(409, encryptedError);
            }
            catch (Exception ex)
            {
                var error = new APIResponse
                {
                    status = 500,
                    statusText = "Error updating process mapping",
                    error = ex.Message
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("DeleteProcessMapping")]
        public async Task<IActionResult> DeleteProcessMapping([FromQuery] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var deleteModel = JsonConvert.DeserializeObject<DeleteProcessMappingDTO>(decryptedJson);

                if (deleteModel?.MappingId <= 0)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Invalid mapping ID provided"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);

                    return StatusCode(400, encryptedError);
                }

                bool success = await _processEngineService.DeleteprocessmappingAsync(deleteModel.MappingId, cTenantID, username);

                if (!success)
                {
                    var error = new APIResponse
                    {
                        status = 404,
                        statusText = "Process mapping not found or you don't have permission to delete it"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(404, encryptedError);
                }

                var response = new APIResponse
                {
                    status = 200,
                    statusText = "Process mapping deleted successfully"
                };
                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var error = new APIResponse
                {
                    status = 500,
                    statusText = "Error deleting process mapping",
                    error = ex.Message
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("getMappingList")]
        public async Task<ActionResult> GetMappingList()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var mappingList = await _processEngineService.GetMappingListAsync(cTenantID);

                var response = new APIResponse
                {
                    body = mappingList?.ToArray() ?? Array.Empty<object>(),
                    statusText = mappingList == null || !mappingList.Any() ? "No mappings found" : "Successful",
                    status = mappingList == null || !mappingList.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);

                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    body = Array.Empty<object>(),
                    statusText = $"Error: {ex.Message}",
                    status = 500
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

    }
}