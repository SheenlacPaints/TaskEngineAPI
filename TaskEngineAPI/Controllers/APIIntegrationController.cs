using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Services;

namespace TaskEngineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class APIIntegrationController : ControllerBase
    {
        private readonly IApiProxyService _APIIntegrationService;

        public APIIntegrationController(IApiProxyService proxyService)
        {
            _APIIntegrationService = proxyService;
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

        
        [Authorize]
        [HttpPost("FetchIntegrationAPIAsync")]
        public async Task<IActionResult> FetchIntegrationAPIAsync([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return CreateEncryptedResponse(400, "Request payload is required");
                }

                if (!ModelState.IsValid)
                {
                    return CreateEncryptedResponse(400, "Invalid request payload");
                }

                var (cTenantID, username) = GetUserInfoFromToken();
                var model = DeserializePayload<APIFetchDTO>(request.payload);

                var json = await _APIIntegrationService.ExecuteIntegrationApi(model, cTenantID, username);

                var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);

                return CreatedDataResponse(list);
            }
            catch (UnauthorizedAccessException)
            {
                return CreateEncryptedResponse(401, "Unauthorized access");
            }
            catch (Exception ex)
            {
                return CreateEncryptedResponse(500, $"Internal server error: {ex.Message}");
            }
        }


        public record IntegrationRequest(int ApiId, string TenantId, object Payload);
    }
}


