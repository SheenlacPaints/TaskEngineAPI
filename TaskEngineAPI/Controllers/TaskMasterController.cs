//using Microsoft.AspNetCore.Mvc;
//using TaskEngineAPI.DTO;
//using TaskEngineAPI.Helpers;
//using Microsoft.AspNetCore.Authorization;
//using Newtonsoft.Json;
//using TaskEngineAPI.Data;
//using TaskEngineAPI.Interfaces;
//using System.IdentityModel.Tokens.Jwt;
//using TaskEngineAPI.Services;
//using static System.Runtime.InteropServices.JavaScript.JSType;

//namespace TaskEngineAPI.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    public class TaskMasterController : ControllerBase
//    {
//        private readonly IConfiguration _config;
//        private readonly IConfiguration _configuration;
//        private readonly IJwtService _jwtService;
//        private readonly ITaskMasterService _TaskMasterService;
//        private readonly ApplicationDbContext _context;
//        public TaskMasterController(IConfiguration configuration, IJwtService jwtService, ITaskMasterService TaskMasterService)
//        {

//            _config = configuration;
//            _jwtService = jwtService;
//            _TaskMasterService = TaskMasterService;
//        }

//        [Authorize]
//        [HttpPost]
//        [Route("InsertTask")]
//        public async Task<IActionResult> InsertTask([FromBody] pay request)
//        {
//            try
//            {
//                if (request == null)
//                {
//                    return EncryptedError(400, "Request body cannot be null.");
//                }
//                if (string.IsNullOrWhiteSpace(request.payload))
//                {
//                    return EncryptedError(400, "Payload cannot be null or empty.");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing.");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                string username = usernameClaim;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    var error = new APIResponse
//                    {
//                        status = 401,
//                        statusText = "Invalid or missing cTenantID in token."
//                    };
//                    string errorJson = JsonConvert.SerializeObject(error);
//                    string encryptedError = AesEncryption.Encrypt(errorJson);
//                    return StatusCode(401, $"\"{encryptedError}\"");
//                }
//                string decryptedJson;
//                try
//                {
//                    decryptedJson = AesEncryption.Decrypt(request.payload);
//                }
//                catch (Exception ex)
//                {
//                    return EncryptedError(400, "Invalid encrypted payload format");
//                }

//                if (string.IsNullOrWhiteSpace(decryptedJson))
//                {
//                    return EncryptedError(400, "Decrypted payload is empty");
//                }

//                // Validate JSON deserialization
//                TaskMasterDTO model;
//                try
//                {
//                    model = JsonConvert.DeserializeObject<TaskMasterDTO>(decryptedJson);
//                }
//                catch (JsonException ex)
//                {
//                    return EncryptedError(400, "Invalid JSON format in payload");
//                }

//                if (model == null)
//                {
//                    return EncryptedError(400, "Failed to deserialize payload to TaskMasterDTO");
//                }
//                //string decryptedJson = AesEncryption.Decrypt(request.payload);
//                //var model = JsonConvert.DeserializeObject<TaskMasterDTO>(decryptedJson);

//                int insertedUserId = await _TaskMasterService.InsertTaskMasterAsync(model, cTenantID, username);

//                if (insertedUserId <= 0)
//                {
//                    return StatusCode(500, new APIResponse
//                    {
//                        status = 500,
//                        statusText = "Failed to create Task"
//                    });
//                }

//                // Prepare response
//                var apierDtls = new APIResponse
//                {
//                    status = 200,
//                    statusText = "Task created successfully",
//                    body = new object[] { new { UserID = insertedUserId } }
//                };
//                string jsone = JsonConvert.SerializeObject(apierDtls);
//                var encryptapierDtls = AesEncryption.Encrypt(jsone);
//                return StatusCode(200, encryptapierDtls);
//            }
//            catch (Exception ex)
//            {

//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Error creating Task",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);

//            }
//        }

//        [Authorize]
//        [HttpGet]
//        [Route("GetMetadetailbyid")]
//        public async Task<IActionResult> GetMetadetailbyid([FromQuery] int processid)
//        {
//            try
//            {
//                if (processid <= 0)
//                {
//                    return EncryptedError(400, "Process ID must be greater than 0.");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing.");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }

//                var json = await _TaskMasterService.GetAllProcessmetaAsync(cTenantID, processid);
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        [Authorize]
//        [HttpGet]
//        [Route("Getdepartmentroleposition")]
//        public async Task<IActionResult> Getdepartmentroleposition([FromQuery] string table)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(table))
//                {
//                    return EncryptedError(400, "Table parameter is required.");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing.");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }

//                var json = await _TaskMasterService.Getdepartmentroleposition(cTenantID, table);
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        [Authorize]
//        [HttpGet]
//        [Route("Getprocessengineprivilege")]
//        public async Task<IActionResult> Getprocessengineprivilege([FromQuery] string? value, string cprivilege)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(cprivilege))
//                {
//                    return EncryptedError(400, "cprivilege parameter is required");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }

//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }

//                var json = await _TaskMasterService.Getprocessengineprivilege(cTenantID, value, cprivilege);
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        [Authorize]
//        [HttpGet]
//        [Route("Getdropdown")]
//        public async Task<IActionResult> Getdropdown([FromQuery] string? column)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(column))
//                {
//                    return EncryptedError(400, "Column parameter is required");
//                }

//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }

//                var json = await _TaskMasterService.Getdropdown(cTenantID, column);
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }


//        [Authorize]
//        [HttpGet]
//        [Route("Gettaskapprove")]
//        public async Task<IActionResult> Gettaskapprove()
//        {
//            try
//            {
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;
//                var json = await _TaskMasterService.Gettaskapprove(cTenantID, username);
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        //[Authorize]
//        //[HttpGet]
//        //[Route("Gettaskhold")]
//        //public async Task<IActionResult> Gettaskhold()
//        //{
//        //    try
//        //    {
//        //        var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//        //        var handler = new JwtSecurityTokenHandler();
//        //        var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//        //        var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//        //        var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//        //        if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//        //        {
//        //            return EncryptedError(401, "Invalid or missing cTenantID in token.");
//        //        }
//        //        string username = usernameClaim;
//        //       // var json = await _TaskMasterService.Gettaskhold(cTenantID, username);
//        //       // var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//        //        var response = new APIResponse
//        //        {
//        //            body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//        //            statusText = data == null || !data.Any() ? "No data found" : "Successful",
//        //            status = data == null || !data.Any() ? 204 : 200
//        //        };

//        //        string jsoner = JsonConvert.SerializeObject(response);
//        //        var encrypted = AesEncryption.Encrypt(jsoner);
//        //        return StatusCode(response.status, encrypted);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        var apierrDtls = new APIResponse
//        //        {
//        //            status = 500,
//        //            statusText = "Internal server Error",
//        //            error = ex.Message
//        //        };

//        //        string jsoner = JsonConvert.SerializeObject(apierrDtls);
//        //        var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//        //        return StatusCode(500, encryptapierrDtls);
//        //    }
//        //}

//        private ActionResult EncryptedError(int status, string message)
//        {
//            var response = new APIResponse { status = status, statusText = message };
//            string json = JsonConvert.SerializeObject(response);
//            string encrypted = AesEncryption.Encrypt(json);
//            return Ok(encrypted);
//        }

//        private ActionResult EncryptedSuccess(string message)
//        {
//            var response = new APIResponse { status = 200, statusText = message };
//            string json = JsonConvert.SerializeObject(response);
//            string encrypted = AesEncryption.Encrypt(json);
//            return Ok(encrypted);
//        }

//        [Authorize]
//        [HttpPost]
//        [Route("DeptposrolecrudAsync")]
//        public async Task<IActionResult> DeptposrolecrudAsync([FromBody] pay request)
//        {
//            try
//            {
//                if (request == null)
//                {
//                    return EncryptedError(400, "Request body cannot be null");
//                }

//                if (string.IsNullOrWhiteSpace(request.payload))
//                {
//                    return EncryptedError(400, "Payload cannot be empty");
//                }

//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();

//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;
//                //string decryptedJson = AesEncryption.Decrypt(request.payload);
//                //var model = JsonConvert.DeserializeObject<DeptPostRoleDTO>(decryptedJson);


//                string decryptedJson;
//                try
//                {
//                    decryptedJson = AesEncryption.Decrypt(request.payload);
//                }
//                catch (Exception ex)
//                {
//                    return EncryptedError(400, "Invalid encrypted payload format");
//                }

//                if (string.IsNullOrWhiteSpace(decryptedJson))
//                {
//                    return EncryptedError(400, "Decrypted payload is empty");
//                }

//                DeptPostRoleDTO model;
//                try
//                {
//                    model = JsonConvert.DeserializeObject<DeptPostRoleDTO>(decryptedJson);
//                }
//                catch (JsonException ex)
//                {
//                    return EncryptedError(400, "Invalid JSON format in payload");
//                }

//                if (model == null)
//                {
//                    return EncryptedError(400, "Failed to deserialize payload to DeptPostRoleDTO");
//                }


//                var json = (await _TaskMasterService.DeptposrolecrudAsync(model, cTenantID, username)).ToString();
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        [Authorize]
//        [HttpPost]
//        [Route("Processprivilegemapping")]
//        public async Task<IActionResult> Processprivilegemapping([FromBody] pay request)
//        {
//            try
//            {

//                if (request == null)
//                {
//                    return EncryptedError(400, "Request body cannot be null");
//                }

//                if (string.IsNullOrWhiteSpace(request.payload))
//                {
//                    return EncryptedError(400, "Payload cannot be empty");
//                }

//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;
//                string decryptedJson = AesEncryption.Decrypt(request.payload);
//                var model = JsonConvert.DeserializeObject<privilegeMappingDTO>(decryptedJson);


//                var json = (await _TaskMasterService.Processprivilege_mapping(model, cTenantID, username)).ToString();
//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        [Authorize]
//        [HttpGet]
//        [Route("Gettaskinitiator")]
//        public async Task<IActionResult> Gettaskinitiator()
//        {
//            try
//            {
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;
//                var json = await _TaskMasterService.GetTaskInitiator(cTenantID, username);



//                var data = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

//                var response = new APIResponse
//                {
//                    body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                    statusText = data == null || !data.Any() ? "No data found" : "Successful",
//                    status = data == null || !data.Any() ? 204 : 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }

//        [Authorize]
//        [HttpGet]
//        [Route("Gettaskinbox")]
//        public async Task<IActionResult> Gettaskinbox()
//        {
//            try
//            {
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;

//                // Call service
//                var json = await _TaskMasterService.Gettaskinbox(cTenantID, username);

//                // Check if json is null or empty
//                if (string.IsNullOrWhiteSpace(json) || json == "[]" || json == "null")
//                {
//                    // Return 200 with empty array
//                    var emptyResponse = new APIResponse
//                    {
//                        body = Array.Empty<object>(),
//                        statusText = "No data found",
//                        status = 200
//                    };

//                    string jsoner = JsonConvert.SerializeObject(emptyResponse);
//                    var encrypted = AesEncryption.Encrypt(jsoner);
//                    return StatusCode(200, encrypted);
//                }

//                // Parse the JSON to check if it's valid
//                try
//                {
//                    var data = JsonConvert.DeserializeObject<List<GetTaskList>>(json);

//                    // Create response directly with the JSON string
//                    var response = new APIResponse
//                    {
//                        body = data?.Cast<object>().ToArray() ?? Array.Empty<object>(),
//                        statusText = data?.Any() == true ? "Successful" : "No data found",
//                        status = 200  // Always return 200
//                    };

//                    string jsoner = JsonConvert.SerializeObject(response);
//                    var encrypted = AesEncryption.Encrypt(jsoner);
//                    return StatusCode(200, encrypted);
//                }
//                catch (JsonException)
//                {
//                    // If JSON is invalid, return empty
//                    var emptyResponse = new APIResponse
//                    {
//                        body = Array.Empty<object>(),
//                        statusText = "No data found",
//                        status = 200
//                    };

//                    string jsoner = JsonConvert.SerializeObject(emptyResponse);
//                    var encrypted = AesEncryption.Encrypt(jsoner);
//                    return StatusCode(200, encrypted);
//                }
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }


//        [Authorize]
//        [HttpGet]
//        [Route("GetboarddetailByid")]
//        public async Task<IActionResult> GetTaskConditionBoard([FromQuery] int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return EncryptedError(400, "ID must be greater than 0");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing.");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;
//                var data = await _TaskMasterService.GetTaskConditionBoard(cTenantID, id);



//                var hasData = data != null && data.Any();

//                if (!hasData)
//                {

//                    var responsee = new APIResponse
//                    {
//                        body = new object[]
//                        {
//                    new
//                    {
//                        status = 400,
//                        data = Array.Empty<object>()
//                    }
//                        },
//                        statusText = $"{id} not found.",
//                        status = 400
//                    };
//                    string jsonerr = JsonConvert.SerializeObject(responsee);
//                    var encryptedd = AesEncryption.Encrypt(jsonerr);
//                    return StatusCode(400, encryptedd);
//                }
//                var response = new APIResponse
//                {
//                    body = data.Cast<object>().ToArray(),
//                    statusText = "Successful",
//                    status = 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }


//        //[Authorize]
//        //[HttpGet]
//        //[Route("Gettaskinboxdatabyid")]
//        //public async Task<IActionResult> Gettaskinboxdatabyid([FromQuery] int id)
//        //{
//        //    try
//        //    {
//        //        var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//        //        var handler = new JwtSecurityTokenHandler();
//        //        var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//        //        var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//        //        var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//        //        if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//        //        {
//        //            return EncryptedError(401, "Invalid or missing cTenantID in token.");
//        //        }
//        //        string username = usernameClaim;
//        //        var data = await _TaskMasterService.Gettaskinboxdatabyid(cTenantID, id);

//        //    var hasData = data != null && data.Any();        
//        //    var response = new APIResponse
//        //    {
//        //        body = hasData? data.Cast<object>().ToArray() : new object[] { "" },                                 
//        //        statusText = hasData ? "Successful" : "No data found",
//        //        status = hasData ? 200 : 204
//        //    };

//        //    string jsoner = JsonConvert.SerializeObject(response);
//        //        var encrypted = AesEncryption.Encrypt(jsoner);
//        //        return StatusCode(response.status, encrypted);
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        var apierrDtls = new APIResponse
//        //        {
//        //            status = 500,
//        //            statusText = "Internal server Error",
//        //            error = ex.Message
//        //        };

//        //        string jsoner = JsonConvert.SerializeObject(apierrDtls);
//        //        var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//        //        return StatusCode(500, encryptapierrDtls);
//        //    }
//        //}

//        [Authorize]
//        [HttpGet]
//        [Route("Gettaskinboxdatabyid")]
//        public async Task<IActionResult> Gettaskinboxdatabyid([FromQuery] int id)
//        {
//            try
//            {
//                if (id <= 0)
//                {
//                    return EncryptedError(400, "ID must be greater than 0");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing.");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
//                    string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }

//                string username = usernameClaim;
//                var data = await _TaskMasterService.Gettaskinboxdatabyid(cTenantID, id);


//                var hasData = data != null && data.Any();

//                if (!hasData)
//                {

//                    var response = new APIResponse
//                    {
//                        body = new object[]
//                        {
//                    new
//                    {
//                        status = 400,
//                        data = Array.Empty<object>()
//                    }
//                        },
//                        statusText = $"{id} not found.",
//                        status = 400
//                    };
//                    string jsoner = JsonConvert.SerializeObject(response);
//                    var encryptedd = AesEncryption.Encrypt(jsoner);
//                    return StatusCode(400, encryptedd);
//                }

//                var successResponse = new APIResponse
//                {
//                    body = data.Cast<object>().ToArray(),
//                    status = 200,
//                    statusText = "Successful"
//                };

//                string json = JsonConvert.SerializeObject(successResponse);
//                var encrypted = AesEncryption.Encrypt(json);

//                return StatusCode(200, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }


//        [Authorize]
//        [HttpGet]
//        [Route("GetmetalayoutByid")]
//        public async Task<IActionResult> GetmetalayoutByid([FromQuery] int itaskno)
//        {
//            try
//            {
//                if (itaskno <= 0)
//                {
//                    return EncryptedError(400, "Task number must be greater than 0");
//                }
//                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//                if (string.IsNullOrWhiteSpace(jwtToken))
//                {
//                    return EncryptedError(400, "Authorization token is missing.");
//                }
//                var handler = new JwtSecurityTokenHandler();
//                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
//                {
//                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
//                }
//                string username = usernameClaim;
//                var data = await _TaskMasterService.GetmetalayoutByid(cTenantID, itaskno);



//                var hasData = data != null && data.Any();

//                if (!hasData)
//                {

//                    var responsee = new APIResponse
//                    {
//                        body = new object[]
//                        {
//                    new
//                    {
//                        status = 400,
//                        data = Array.Empty<object>()
//                    }
//                        },
//                        statusText = $"{itaskno} not found.",
//                        status = 400
//                    };
//                    string jsonerr = JsonConvert.SerializeObject(responsee);
//                    var encryptedd = AesEncryption.Encrypt(jsonerr);
//                    return StatusCode(400, encryptedd);
//                }
//                var response = new APIResponse
//                {
//                    body = data.Cast<object>().ToArray(),
//                    statusText = "Successful",
//                    status = 200
//                };

//                string jsoner = JsonConvert.SerializeObject(response);
//                var encrypted = AesEncryption.Encrypt(jsoner);
//                return StatusCode(response.status, encrypted);
//            }
//            catch (Exception ex)
//            {
//                var apierrDtls = new APIResponse
//                {
//                    status = 500,
//                    statusText = "Internal server Error",
//                    error = ex.Message
//                };

//                string jsoner = JsonConvert.SerializeObject(apierrDtls);
//                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
//                return StatusCode(500, encryptapierrDtls);
//            }
//        }



//        [Authorize]
//        [HttpPut]
//        [Route("Updatetaskapprove")]
//        public async Task<IActionResult> Updatetaskapprove([FromBody] pay request)
//        {
//            if (request == null)
//            {
//                return EncryptedError(400, "Request body cannot be null");
//            }

//            if (string.IsNullOrWhiteSpace(request.payload))
//            {
//                return EncryptedError(400, "Payload cannot be empty");
//            }
//            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
//            if (string.IsNullOrWhiteSpace(jwtToken))
//            {
//                return EncryptedError(400, "Authorization token is missing");
//            }
//            var handler = new JwtSecurityTokenHandler();
//            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

//            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
//            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
//            string username = usernameClaim;
//            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
//            {
//                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
//            }

//            string decryptedJson = AesEncryption.Decrypt(request.payload);
//            var model = JsonConvert.DeserializeObject<updatetaskDTO>(decryptedJson);

//            if (model == null || model.ID <= 0)
//            {
//                throw new ArgumentException("Invalid ID provided");
//            }

//            bool success = await _TaskMasterService.UpdatetaskapproveAsync(model, cTenantID, username);

//            var response = new APIResponse
//            {
//                status = success ? 200 : 400,
//                statusText = success ? "updated successfully" : "data not found or update failed"
//            };

//            string json = JsonConvert.SerializeObject(response);
//            string encrypted = AesEncryption.Encrypt(json);
//            return StatusCode(response.status, encrypted);
//        }



//    }
//}





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
        //private IActionResult CreatedSuccessResponse(object data, string message = "Successful")
        //{
        //    var response = new APIResponse
        //    {
        //        status = 200,
        //        statusText = message,             

        //        body = data == null ? Array.Empty<object>() : new object[] { data },

        //    };
        //    string json = JsonConvert.SerializeObject(response);
        //    string encrypted = AesEncryption.Encrypt(json);
        //    return Ok(encrypted);
        //}


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
                _logger.LogWarning(ex, "Unauthorized access in InsertTask.");
                throw;
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task.");
                throw;
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetMetadetailsbyid")]
        public async Task<IActionResult> GetMetadetailsbyid([FromQuery] int processid)
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

        [Authorize]
        [HttpGet]
        [Route("GetMetadetailbyid")]
        public async Task<IActionResult> GetMetadetailbyid([FromQuery] int processid)
        {
            try
            {
                if (processid <= 0)
                {
                    return EncryptedError(400, "Process ID must be greater than 0.");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing.");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var json = await taskMasterService.GetAllProcessmetaAsync(cTenantID, processid);
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


    }
}