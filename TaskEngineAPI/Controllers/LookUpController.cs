using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.IdentityModel.Tokens.Jwt;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Helpers;

namespace TaskEngineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LookUpController : ControllerBase
    {
        private readonly ILookUpService _lookUpService;

        public LookUpController(ILookUpService lookUpService)
        {
            _lookUpService = lookUpService;
        }

        [Authorize]
        [HttpGet]
        [Route("GetAllNotificationTypes")]
        public async Task<IActionResult> GetAllNotificationTypes()
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader))
                {
                    return BadRequest("Authorization header is missing");
                }

                if (!authHeader.StartsWith("Bearer "))
                {
                    return BadRequest("Authorization header must be in 'Bearer <token>' format");
                }

                var jwtToken = authHeader.Split(" ").Last();

                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(jwtToken))
                {
                    return BadRequest("Invalid JWT token");
                }

                var jsonToken = handler.ReadJwtToken(jwtToken);

                var tenantIdClaim = jsonToken.Claims
                    .SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim))
                {
                    return BadRequest("cTenantID claim missing in token");
                }

                if (!int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return BadRequest("cTenantID must be a valid integer");
                }

                var notificationTypes = await _lookUpService.GetAllNotificationTypesAsync(cTenantID);

                var response = new APIResponse
                {
                    body = notificationTypes?.ToArray() ?? Array.Empty<object>(),
                    statusText = notificationTypes == null || !notificationTypes.Any()
                        ? "No notification types found"
                        : "Successful",
                    status = notificationTypes == null || !notificationTypes.Any() ? 204 : 200
                };

                var encrypted = AesEncryption.Encrypt(JsonConvert.SerializeObject(response));

                return StatusCode(response.status, encrypted);
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
        }

        [Authorize]
        [HttpPost]
        [Route("CreateNotificationType")]
        public async Task<IActionResult> CreateNotificationType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out _))
                    return BadRequest("Invalid or missing cTenantID");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<CreateNotificationTypeDTO>(decryptedJson);

                if (model == null || string.IsNullOrWhiteSpace(model.notification_type))
                    return BadRequest("Notification type name is required");

                var success = await _lookUpService.CreateNotificationTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success
                        ? "Notification type created successfully"
                        : "Failed to create notification type"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
        }


        [Authorize]
        [HttpPut]
        [Route("UpdateNotificationType")]
        public async Task<IActionResult> UpdateNotificationType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();

                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out _))
                    return BadRequest("Invalid or missing cTenantID");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<UpdateNotificationTypeDTO>(decryptedJson);

                if (model == null || model.ID <= 0)
                    return BadRequest("Invalid ID provided");

                var success = await _lookUpService.UpdateNotificationTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success
                        ? "Notification type updated successfully"
                        : "Notification type not found or update failed"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                return StatusCode(500, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 500,
                        statusText = "Error retrieving privileges",
                        error = ex.Message
                    })));
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("DeleteNotificationType")]
        public async Task<IActionResult> DeleteNotificationType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return BadRequest("Invalid or missing cTenantID or username");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<DeleteNotificationTypeDTO>(decryptedJson);
                if (model == null || model.ID <= 0)
                    return BadRequest("Invalid ID provided");

                var success = await _lookUpService.DeleteNotificationTypeAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "Notification type deleted successfully" : "Notification type not found",
                    body = Array.Empty<object>()
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                return StatusCode(500, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 500,
                        statusText = "Error retrieving privileges",
                        error = ex.Message
                    })));
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetAllParticipantTypes")]
        public async Task<IActionResult> GetAllParticipantTypes()
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                    return BadRequest("Invalid or missing cTenantID");

                var participantTypes = await _lookUpService.GetAllParticipantTypesAsync(cTenantID);

                var response = new APIResponse
                {
                    body = participantTypes?.ToArray() ?? Array.Empty<object>(),
                    status = participantTypes == null || !participantTypes.Any() ? 204 : 200,
                    statusText = participantTypes == null || !participantTypes.Any()
                        ? "No participant types found"
                        : "Successful"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                return StatusCode(500, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 500,
                        statusText = "Error retrieving privileges",
                        error = ex.Message
                    })));
            }
        }

        [Authorize]
        [HttpPost]
        [Route("CreateParticipantType")]
        public async Task<IActionResult> CreateParticipantType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out _))
                    return BadRequest("Invalid or missing cTenantID");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<CreateParticipantTypeDTO>(decryptedJson);
                if (model == null || string.IsNullOrWhiteSpace(model.participant_type))
                    return BadRequest("Participant type name is required");

                var success = await _lookUpService.CreateParticipantTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success
                        ? "Participant type created successfully"
                        : "Failed to create participant type"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }


        [Authorize]
        [HttpPut]
        [Route("UpdateParticipantType")]
        public async Task<IActionResult> UpdateParticipantType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out _))
                    return BadRequest("Invalid or missing cTenantID");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<UpdateParticipantTypeDTO>(decryptedJson);
                if (model == null || model.ID <= 0)
                    return BadRequest("Invalid ID provided");

                var success = await _lookUpService.UpdateParticipantTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success
                        ? "Participant type updated successfully"
                        : "Participant type not found or update failed"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }


        [Authorize]
        [HttpDelete]
        [Route("DeleteParticipantType")]
        public async Task<IActionResult> DeleteParticipantType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return BadRequest("Invalid or missing cTenantID or username");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<DeleteParticipantTypeDTO>(decryptedJson);
                if (model == null || model.ID <= 0)
                    return BadRequest("Invalid ID provided");

                var success = await _lookUpService.DeleteParticipantTypeAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success
                        ? "Participant type deleted successfully"
                        : "Participant type not found",
                    body = Array.Empty<object>()
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }

        [Authorize]
        [HttpGet]
        [Route("GetAllProcessPrivilegeTypes")]
        public async Task<IActionResult> GetAllProcessPrivilegeTypes()
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                    return BadRequest("Invalid or missing cTenantID");

                var data = await _lookUpService.GetAllProcessPrivilegeTypesAsync(cTenantID);

                var response = new APIResponse
                {
                    body = data?.ToArray() ?? Array.Empty<object>(),
                    status = data == null || !data.Any() ? 204 : 200,
                    statusText = data == null || !data.Any()
                        ? "No process privilege types found"
                        : "Successful"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }

        [Authorize]
        [HttpPost]
        [Route("CreateProcessPrivilegeType")]
        public async Task<IActionResult> CreateProcessPrivilegeType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out _) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return BadRequest("Invalid or missing cTenantID or username");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<CreateProcessPrivilegeTypeDTO>(decryptedJson);
                if (model == null || string.IsNullOrWhiteSpace(model.cprocess_privilege))
                    return BadRequest("Process privilege type name is required");

                var success = await _lookUpService.CreateProcessPrivilegeTypeAsync(model, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success
                        ? "Process privilege type created successfully"
                        : "Failed to create process privilege type"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }

        [Authorize]
        [HttpPut]
        [Route("UpdateProcessPrivilegeType")]
        public async Task<IActionResult> UpdateProcessPrivilegeType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out _))
                    return BadRequest("Invalid or missing cTenantID");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<UpdateProcessPrivilegeTypeDTO>(decryptedJson);
                if (model == null || model.ID <= 0)
                    return BadRequest("Invalid ID provided");

                var success = await _lookUpService.UpdateProcessPrivilegeTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success
                        ? "Process privilege type updated successfully"
                        : "Process privilege type not found or update failed"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }



        [Authorize]
        [HttpDelete]
        [Route("DeleteProcessPrivilegeType")]
        public async Task<IActionResult> DeleteProcessPrivilegeType([FromBody] pay request)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return BadRequest("Invalid or missing cTenantID or username");

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return BadRequest("Request payload is required");

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                }
                catch
                {
                    return BadRequest("Invalid encrypted payload");
                }

                var model = JsonConvert.DeserializeObject<DeleteProcessPrivilegeTypeDTO>(decryptedJson);
                if (model == null || model.ID <= 0)
                    return BadRequest("Invalid ID provided");

                var success = await _lookUpService.DeleteProcessPrivilegeTypeAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success
                        ? "Process privilege type deleted successfully"
                        : "Process privilege type not found",
                    body = Array.Empty<object>()
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }


        [Authorize]
        [HttpGet]
        [Route("getPrivilegeList")]
        public async Task<IActionResult> GetPrivilegeList()
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                    return BadRequest("Invalid or missing cTenantID");

                var privilegeList = await _lookUpService.GetPrivilegeListAsync(cTenantID);

                var response = new APIResponse
                {
                    body = privilegeList?.ToArray() ?? Array.Empty<object>(),
                    status = privilegeList == null || !privilegeList.Any() ? 204 : 200,
                    statusText = privilegeList == null || !privilegeList.Any()
                        ? "No privilege types found"
                        : "Successful"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }

        }


        [Authorize]
        [HttpGet]
        [Route("GetPrivilegeTypeById")]
        public async Task<IActionResult> GetPrivilegeTypeById([FromQuery] int privilegeType)
        {
            try
            {
                var authHeader = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
                if (string.IsNullOrWhiteSpace(authHeader) || !authHeader.StartsWith("Bearer "))
                    return BadRequest("Invalid Authorization header");

                var token = authHeader.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return BadRequest("Invalid JWT token");

                var jsonToken = handler.ReadJwtToken(token);
                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(x => x.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                    return BadRequest("Invalid or missing cTenantID");

                var privilegeItems = await _lookUpService.GetPrivilegeTypeByIdAsync(privilegeType, cTenantID);

                var response = new APIResponse
                {
                    body = privilegeItems?.ToArray() ?? Array.Empty<object>(),
                    status = privilegeItems == null || !privilegeItems.Any() ? 204 : 200,
                    statusText = privilegeItems == null || !privilegeItems.Any()
                        ? "No privileges found"
                        : "Successful"
                };

                return StatusCode(response.status,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(401, AesEncryption.Encrypt(JsonConvert.SerializeObject(
                    new APIResponse
                    {
                        status = 401,
                        statusText = "Unauthorized",
                        error = ex.Message
                    })));
            }
            catch (Exception ex)
            {
                var response = new APIResponse
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message
                };

                return StatusCode(500,
                    AesEncryption.Encrypt(JsonConvert.SerializeObject(response)));
            }



        }
    }

    public class pay
    {
        public string payload { get; set; } = string.Empty;
    }

    public class APIResponse
    {
        public object[] body { get; set; } = Array.Empty<object>();
        public string statusText { get; set; } = string.Empty;
        public string error { get; set; } = string.Empty;
        public int status { get; set; }
        public object? data { get; set; }
    }
}