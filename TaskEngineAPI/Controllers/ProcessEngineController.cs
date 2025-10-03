using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Services;
using Microsoft.AspNetCore.Authorization;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Data;
using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Controllers
{
   
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



    }
}
