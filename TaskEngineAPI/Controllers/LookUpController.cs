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
        public async Task<ActionResult> GetAllNotificationTypes()
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
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

        [HttpPost]
        [Route("CreateNotificationType")]
        public async Task<IActionResult> CreateNotificationType([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.payload))
            {
                throw new ArgumentException("Request payload is required");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<CreateNotificationTypeDTO>(decryptedJson);

            if (model == null || string.IsNullOrWhiteSpace(model.notification_type))
            {
                throw new ArgumentException("Notification type name is required");
            }

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

        [HttpPut]
        [Route("UpdateNotificationType")]
        public async Task<IActionResult> UpdateNotificationType([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<UpdateNotificationTypeDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
            {
                throw new ArgumentException("Invalid ID provided");
            }

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

        [Authorize]
        [HttpDelete]
        [Route("DeleteNotificationType")]
        public async Task<IActionResult> DeleteNotificationType([FromBody] pay request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.payload))
                throw new ArgumentException("Request payload is required");

            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                string.IsNullOrWhiteSpace(usernameClaim))
                throw new UnauthorizedAccessException("Invalid or missing cTenantID or username in token.");

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<DeleteNotificationTypeDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
                throw new ArgumentException("Invalid ID provided");

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


        [Authorize]
        [HttpGet]
        [Route("GetAllParticipantTypes")]
        public async Task<ActionResult> GetAllParticipantTypes()
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
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

        [HttpPost]
        [Route("CreateParticipantType")]
        public async Task<IActionResult> CreateParticipantType([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.payload))
            {
                throw new ArgumentException("Request payload is required");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<CreateParticipantTypeDTO>(decryptedJson);

            if (model == null || string.IsNullOrWhiteSpace(model.participant_type))
            {
                throw new ArgumentException("Participant type name is required");
            }

            bool success = await _lookUpService.CreateParticipantTypeAsync(model);

            var response = new APIResponse
            {
                status = success ? 200 : 400,
                statusText = success ? "Participant type created successfully" : "Failed to create participant type"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [HttpPut]
        [Route("UpdateParticipantType")]
        public async Task<IActionResult> UpdateParticipantType([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<UpdateParticipantTypeDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
            {
                throw new ArgumentException("Invalid ID provided");
            }

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

        [Authorize]
        [HttpDelete]
        [Route("DeleteParticipantType")]
        public async Task<IActionResult> DeleteParticipantType([FromBody] pay request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.payload))
                throw new ArgumentException("Request payload is required");

            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                string.IsNullOrWhiteSpace(usernameClaim))
                throw new UnauthorizedAccessException("Invalid or missing cTenantID or username in token.");

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<DeleteParticipantTypeDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
                throw new ArgumentException("Invalid ID provided");

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

        [Authorize]
        [HttpGet]
        [Route("GetAllProcessPrivilegeTypes")]
        public async Task<ActionResult> GetAllProcessPrivilegeTypes()
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            var processPrivilegeTypes = await _lookUpService.GetAllProcessPrivilegeTypesAsync(cTenantID);

            var response = new APIResponse
            {
                body = processPrivilegeTypes?.ToArray() ?? Array.Empty<object>(),
                statusText = processPrivilegeTypes == null || !processPrivilegeTypes.Any() ? "No process privilege types found" : "Successful",
                status = processPrivilegeTypes == null || !processPrivilegeTypes.Any() ? 204 : 200
            };

            string jsoner = JsonConvert.SerializeObject(response);
            var encrypted = AesEncryption.Encrypt(jsoner);
            return StatusCode(200, encrypted);
        }

        [HttpPost]
        [Route("CreateProcessPrivilegeType")]
        public async Task<IActionResult> CreateProcessPrivilegeType([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.payload))
            {
                throw new ArgumentException("Request payload is required");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<CreateProcessPrivilegeTypeDTO>(decryptedJson);

            if (model == null || string.IsNullOrWhiteSpace(model.cprocess_privilege))
            {
                throw new ArgumentException("Process privilege type name is required");
            }

            bool success = await _lookUpService.CreateProcessPrivilegeTypeAsync(model);

            var response = new APIResponse
            {
                status = success ? 200 : 400,
                statusText = success ? "Process privilege type created successfully" : "Failed to create process privilege type"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [HttpPut]
        [Route("UpdateProcessPrivilegeType")]
        public async Task<IActionResult> UpdateProcessPrivilegeType([FromBody] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
            {
                throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<UpdateProcessPrivilegeTypeDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
            {
                throw new ArgumentException("Invalid ID provided");
            }

            bool success = await _lookUpService.UpdateProcessPrivilegeTypeAsync(model);

            var response = new APIResponse
            {
                status = success ? 200 : 400,
                statusText = success ? "Process privilege type updated successfully" : "Process privilege type not found or update failed"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [Authorize]
        [HttpDelete]
        [Route("DeleteProcessPrivilegeType")]
        public async Task<IActionResult> DeleteProcessPrivilegeType([FromBody] pay request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.payload))
                throw new ArgumentException("Request payload is required");

            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                string.IsNullOrWhiteSpace(usernameClaim))
                throw new UnauthorizedAccessException("Invalid or missing cTenantID or username in token.");

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<DeleteProcessPrivilegeTypeDTO>(decryptedJson);

            if (model == null || model.ID <= 0)
                throw new ArgumentException("Invalid ID provided");

            bool success = await _lookUpService.DeleteProcessPrivilegeTypeAsync(model, cTenantID, usernameClaim);

            var response = new APIResponse
            {
                status = success ? 200 : 404,
                statusText = success ? "Process privilege type deleted successfully" : "Process privilege type not found",
                body = Array.Empty<object>()
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }
        [Authorize]
        [HttpGet]
        [Route("getPrivilegeList")]
        public async Task<ActionResult> GetPrivilegeList()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    throw new UnauthorizedAccessException("Invalid or missing cTenantID in token.");
                }

                var privilegeList = await _lookUpService.GetPrivilegeListAsync(cTenantID);

                var response = new APIResponse
                {
                    body = privilegeList?.ToArray() ?? Array.Empty<object>(),
                    statusText = privilegeList == null || !privilegeList.Any() ? "No privilege types found" : "Successful",
                    status = privilegeList == null || !privilegeList.Any() ? 204 : 200
                };

                string json = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(json);
                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    body = Array.Empty<object>(),
                    statusText = "Error retrieving privilege list",
                    error = ex.Message,
                    status = 500
                };

                string json = JsonConvert.SerializeObject(errorResponse);
                var encrypted = AesEncryption.Encrypt(json);
                return StatusCode(500, encrypted);
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
    }
}