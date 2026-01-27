using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    [Route("api/[controller]")]
    [ApiController]
    public class AnalyticalController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IAnalyticalService _AnalyticalService;
        private readonly IJwtService _jwtService;
        private readonly IProjectService _ProjectService;
        private readonly IMinioService _minioService;
       
        public AnalyticalController(IConfiguration configuration, IJwtService jwtService, IAnalyticalService AnalyticalService, IMinioService MinioService)
        {

            _config = configuration;
            _jwtService = jwtService;
            _AnalyticalService = AnalyticalService;
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
        [Route("CreateAnalyticalhub")]
        public async Task<IActionResult> CreateAnalyticalhub([FromBody] pay request)
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

                AnalyticalDTO model;
                try
                {
                  
                    model = JsonConvert.DeserializeObject<AnalyticalDTO>(decryptedJson);
                }
                catch (JsonException ex)
                {
                    return CreateEncryptedResponse(400, "Invalid JSON format in payload");
                }

                if (model == null)
                {
                    return CreateEncryptedResponse(400, "Failed to deserialize payload");
                }

                int insertedUserId = await _AnalyticalService.InsertAnalyticalhubAsync(model, cTenantID, username);

                if (insertedUserId <= 0)
                {
                    return CreateEncryptedResponse(500, "Failed to create Process");
                }

                return CreatedSuccessResponse(new { id = insertedUserId }, "data created successfully");
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
