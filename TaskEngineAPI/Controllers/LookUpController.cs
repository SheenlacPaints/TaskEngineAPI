using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        private IActionResult EncryptedError(int statusCode, string message)
        {
            var errorResponse = new APIResponse
            {
                body = Array.Empty<object>(),
                statusText = message,
                status = statusCode
            };
            string errorJson = JsonConvert.SerializeObject(errorResponse);
            var encryptedError = AesEncryption.Encrypt(errorJson);
            return StatusCode(statusCode, encryptedError);
        }


        [Authorize]
        [HttpGet]
        [Route("GetAllNotificationTypes")]
        public async Task<ActionResult> GetAllNotificationTypes()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return (ActionResult)EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var notificationTypes = await _lookUpService.GetAllNotificationTypesAsync(cTenantID);

                var response = new APIResponse
                {
                    body = notificationTypes?.ToArray() ?? Array.Empty<object>(),
                    statusText = notificationTypes == null || !notificationTypes.Any() ? "No notification types found" : "Successful",
                    status = notificationTypes == null || !notificationTypes.Any() ? 204 : 200
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

        [HttpPost]
        [Route("CreateNotificationType")]
        public async Task<IActionResult> CreateNotificationType([FromForm] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<CreateNotificationTypeDTO>(decryptedJson);
                bool success = await _lookUpService.CreateNotificationTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "Notification type created successfully" : "Failed to create notification type"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error: {ex.Message}"
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [HttpPut]
        [Route("UpdateNotificationType")]
        public async Task<IActionResult> UpdateNotificationType([FromForm] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<UpdateNotificationTypeDTO>(decryptedJson);
                bool success = await _lookUpService.UpdateNotificationTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "Notification type updated successfully" : "Notification type not found or update failed"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error: {ex.Message}"
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpDelete]
        [Route("DeleteNotificationType")]
        public async Task<IActionResult> DeleteNotificationType([FromForm] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return EncryptedError(400, "Request payload is required");

                // Extract token
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return EncryptedError(401, "Invalid or missing cTenantID or username in token.");

                // Decrypt payload
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<DeleteNotificationTypeDTO>(decryptedJson);

                if (model == null || model.ID <= 0)
                    return EncryptedError(400, "Invalid ID provided");

                bool success = await _lookUpService.DeleteNotificationTypeAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "Notification type deleted successfully" : "Notification type not found",
                    body = Array.Empty<object>()
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                return EncryptedError(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize]
        [HttpGet]
        [Route("GetAllProcessPriorityLabels")]
        public async Task<ActionResult> GetAllProcessPriorityLabels()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return (ActionResult)EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var priorityLabels = await _lookUpService.GetAllProcessPriorityLabelsAsync(cTenantID);

                var response = new APIResponse
                {
                    body = priorityLabels?.ToArray() ?? Array.Empty<object>(),
                    statusText = priorityLabels == null || !priorityLabels.Any() ? "No process priority labels found" : "Successful",
                    status = priorityLabels == null || !priorityLabels.Any() ? 204 : 200
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

        [HttpPost]
        [Route("CreateProcessPriorityLabel")]
        public async Task<IActionResult> CreateProcessPriorityLabel([FromForm] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<CreateProcessPriorityLabelDTO>(decryptedJson);
                bool success = await _lookUpService.CreateProcessPriorityLabelAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "Process priority label created successfully" : "Failed to create process priority label"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error: {ex.Message}"
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [HttpPut]
        [Route("UpdateProcessPriorityLabel")]
        public async Task<IActionResult> UpdateProcessPriorityLabel([FromForm] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<UpdateProcessPriorityLabelDTO>(decryptedJson);
                bool success = await _lookUpService.UpdateProcessPriorityLabelAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "Process priority label updated successfully" : "Process priority label not found or update failed"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error: {ex.Message}"
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }
        [Authorize]
        [HttpDelete]
        [Route("DeleteProcessPriorityLabel")]
        public async Task<IActionResult> DeleteProcessPriorityLabel([FromForm] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return EncryptedError(400, "Request payload is required");

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return EncryptedError(401, "Invalid or missing cTenantID or username in token.");

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<DeleteProcessPriorityLabelDTO>(decryptedJson);

                if (model == null || model.ID <= 0)
                    return EncryptedError(400, "Invalid ID provided");

                bool success = await _lookUpService.DeleteProcessPriorityLabelAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "Process priority label deleted successfully" : "Process priority label not found",
                    body = Array.Empty<object>()
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                return EncryptedError(500, $"Internal server error: {ex.Message}");
            }
        }



        [Authorize]
        [HttpGet]
        [Route("GetAllParticipantTypes")]
        public async Task<ActionResult> GetAllParticipantTypes()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return (ActionResult)EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var participantTypes = await _lookUpService.GetAllParticipantTypesAsync(cTenantID);

                var response = new APIResponse
                {
                    body = participantTypes?.ToArray() ?? Array.Empty<object>(),
                    statusText = participantTypes == null || !participantTypes.Any() ? "No participant types found" : "Successful",
                    status = participantTypes == null || !participantTypes.Any() ? 204 : 200
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

        [HttpPost]
        [Route("CreateParticipantType")]
        public async Task<IActionResult> CreateParticipantType([FromForm] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request payload is required");
                }

                string decryptedJson;
                try
                {
                    decryptedJson = AesEncryption.Decrypt(request.payload);
                   
                }
                catch (Exception decryptEx)
                {
                    return EncryptedError(400, $"Decryption failed. Please check your payload format.");
                }

                if (string.IsNullOrWhiteSpace(decryptedJson) ||
                    !decryptedJson.Trim().StartsWith("{") ||
                    !decryptedJson.Trim().EndsWith("}"))
                {
                    return EncryptedError(400, "Invalid JSON format received");
                }

                CreateParticipantTypeDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<CreateParticipantTypeDTO>(decryptedJson);
                }
                catch (JsonException jsonEx)
                {
                    return EncryptedError(400, $"Invalid JSON structure for participant type. Expected: ctenent_id, participant_type, nis_active, ccreated_by");
                }

                if (model == null)
                {
                    return EncryptedError(400, "Failed to parse participant type data");
                }


                if (string.IsNullOrWhiteSpace(model.participant_type))
                {
                    return EncryptedError(400, "Participant type name (participant_type) is required");
                }

                if (model.ctenent_id <= 0)
                {
                    return EncryptedError(400, "Valid tenant ID (ctenent_id) is required");
                }

                if (string.IsNullOrWhiteSpace(model.ccreated_by))
                {
                    return EncryptedError(400, "Created by user (ccreated_by) is required");
                }

                bool success = await _lookUpService.CreateParticipantTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "Participant type created successfully" : "Failed to create participant type",
                    body = success ? new object[] { new { message = "Created successfully", id = "new" } } : Array.Empty<object>()
                };
                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                return EncryptedError(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut]
        [Route("UpdateParticipantType")]
        public async Task<IActionResult> UpdateParticipantType([FromForm] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<UpdateParticipantTypeDTO>(decryptedJson);
                bool success = await _lookUpService.UpdateParticipantTypeAsync(model);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "Participant type updated successfully" : "Participant type not found or update failed"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error: {ex.Message}"
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }


        [Authorize]
        [HttpDelete]
        [Route("DeleteParticipantType")]
        public async Task<IActionResult> DeleteParticipantType([FromForm] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                    return EncryptedError(400, "Request payload is required");

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                    return EncryptedError(401, "Invalid or missing cTenantID or username in token.");

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<DeleteParticipantTypeDTO>(decryptedJson);

                if (model == null || model.ID <= 0)
                    return EncryptedError(400, "Invalid ID provided");

                bool success = await _lookUpService.DeleteParticipantTypeAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "Participant type deleted successfully" : "Participant type not found",
                    body = Array.Empty<object>()
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                return EncryptedError(500, $"Internal server error: {ex.Message}");
            }
        }
       

    }
}