using Microsoft.AspNetCore.Mvc;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using TaskEngineAPI.Data;
using TaskEngineAPI.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using TaskEngineAPI.Services;

namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskMasterController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly ITaskMasterService _TaskMasterService;
        private readonly ApplicationDbContext _context;
        public TaskMasterController(IConfiguration configuration, IJwtService jwtService, ITaskMasterService TaskMasterService)
        {

            _config = configuration;
            _jwtService = jwtService;
            _TaskMasterService = TaskMasterService;
        }

        [Authorize]
        [HttpPost]
        [Route("InsertTask")]
        public async Task<IActionResult> InsertTask([FromBody] pay request)
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
                var model = JsonConvert.DeserializeObject<TaskMasterDTO>(decryptedJson);

                  int insertedUserId = await _TaskMasterService.InsertTaskMasterAsync(model, cTenantID, username);

                if (insertedUserId <= 0)
                {
                    return StatusCode(500, new APIResponse
                    {
                        status = 500,
                        statusText = "Failed to create Task"
                    });
                }

                // Prepare response
                var apierDtls = new APIResponse
                {
                    status = 200,
                    statusText = "Task created successfully",
                    body = new object[] { new { UserID = insertedUserId } }
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
                    statusText = "Error creating Task",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);

            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetMetadetailbyid")]
        public async Task<IActionResult> GetMetadetailbyid([FromQuery] int processid)
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

                var json = await _TaskMasterService.GetAllProcessmetaAsync(cTenantID, processid);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getdepartmentroleposition")]
        public async Task<IActionResult> Getdepartmentroleposition([FromQuery] string table)
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

                var json = await _TaskMasterService.Getdepartmentroleposition(cTenantID, table);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getprocessengineprivilege")]
        public async Task<IActionResult> Getprocessengineprivilege([FromQuery] string? value, string cprivilege)
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

                var json = await _TaskMasterService.Getprocessengineprivilege(cTenantID, value, cprivilege);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Getdropdown")]
        public async Task<IActionResult> Getdropdown([FromQuery] string? column)
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

                var json = await _TaskMasterService.Getdropdown(cTenantID, column);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

       
        [Authorize]
        [HttpGet]
        [Route("Gettaskapprove")]
        public async Task<IActionResult> Gettaskapprove()
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
                string username = usernameClaim;
                var json = await _TaskMasterService.Gettaskapprove(cTenantID, username);
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        //[Authorize]
        //[HttpGet]
        //[Route("Gettaskhold")]
        //public async Task<IActionResult> Gettaskhold()
        //{
        //    try
        //    {
        //        var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        //        var handler = new JwtSecurityTokenHandler();
        //        var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

        //        var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
        //        var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
        //        if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
        //        {
        //            return EncryptedError(401, "Invalid or missing cTenantID in token.");
        //        }
        //        string username = usernameClaim;
        //       // var json = await _TaskMasterService.Gettaskhold(cTenantID, username);
        //       // var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

        //        var response = new APIResponse
        //        {
        //            body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
        //            statusText = data == null || !data.Any() ? "No data found" : "Successful",
        //            status = data == null || !data.Any() ? 204 : 200
        //        };

        //        string jsoner = JsonConvert.SerializeObject(response);
        //        var encrypted = AesEncryption.Encrypt(jsoner);
        //        return StatusCode(response.status, encrypted);
        //    }
        //    catch (Exception ex)
        //    {
        //        var apierrDtls = new APIResponse
        //        {
        //            status = 500,
        //            statusText = "Internal server Error",
        //            error = ex.Message
        //        };

        //        string jsoner = JsonConvert.SerializeObject(apierrDtls);
        //        var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
        //        return StatusCode(500, encryptapierrDtls);
        //    }
        //}

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
        [Route("DeptposrolecrudAsync")]
        public async Task<IActionResult> DeptposrolecrudAsync([FromBody] pay request)
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
                string username = usernameClaim;
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<DeptPostRoleDTO>(decryptedJson);


                var json = (await _TaskMasterService.DeptposrolecrudAsync(model, cTenantID, username)).ToString();
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("Processprivilegemapping")]
        public async Task<IActionResult> Processprivilegemapping([FromBody] pay request)
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
                string username = usernameClaim;
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<privilegeMappingDTO>(decryptedJson);


                var json = (await _TaskMasterService.Processprivilege_mapping(model, cTenantID, username)).ToString();
                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinitiator")]
        public async Task<IActionResult> Gettaskinitiator()
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
                string username = usernameClaim;
                var json = await _TaskMasterService.GetTaskInitiator(cTenantID, username);

              

                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("Gettaskinbox")]
        public async Task<IActionResult> Gettaskinbox()
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
                string username = usernameClaim;
                var json = await _TaskMasterService.Gettaskinbox(cTenantID, username);



                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }


        [Authorize]
        [HttpGet]
        [Route("GetboarddetailByid")]
        public async Task<IActionResult> GetTaskConditionBoard([FromQuery] int id)
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
                string username = usernameClaim;
                var data = await _TaskMasterService.GetTaskConditionBoard(cTenantID, id);
                var response = new APIResponse
                {
                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
                    status = data == null || !data.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Internal server Error",
                    error = ex.Message
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }



    }
}
