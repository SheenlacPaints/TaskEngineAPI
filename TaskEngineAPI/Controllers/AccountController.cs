using BCrypt.Net;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using TaskEngineAPI.Data;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Repositories;
using TaskEngineAPI.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase

    {
        private readonly IConfiguration _config;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IAdminService _AccountService;
        private readonly ApplicationDbContext _context;
        public AccountController(IConfiguration configuration, IJwtService jwtService, IAdminService AccountService)
        {

            _config = configuration;
            _jwtService = jwtService;
            _AccountService = AccountService;
        }

        [HttpPost]
        [Route("Login")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> Login([FromBody] pay request)
        {
            string urlSafe1 = AesEncryption.Decrypt(request.payload);
            var User = JsonConvert.DeserializeObject<User>(urlSafe1);

            if (User == null || string.IsNullOrEmpty(User.userName) || string.IsNullOrEmpty(User.password))
            {
                var error = new APIResponse
                {
                    status = 400,
                    statusText = "Username and password must be provided."
                };
                string json = JsonConvert.SerializeObject(error);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(400, encrypted);
            }

            var connStr = _config.GetConnectionString("Database");
            string email = "", tenantID = "", roleid = "", username = "", hashedPassword = "", firstname = "", lastname = "", tenantname = "", cposition_name = "", cposition_code = "", role = "", role_name = "";

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_validate_Admin_login", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FilterValue1", User.userName);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                username = reader["cuser_name"]?.ToString();
                                firstname = reader["cfirst_name"]?.ToString();
                                lastname = reader["clast_name"]?.ToString();
                                roleid = reader["crole_id"]?.ToString();
                                tenantID = reader["ctenant_id"]?.ToString();
                                tenantname = reader["ctenant_code"]?.ToString();
                                email = reader["cemail"]?.ToString();
                                hashedPassword = reader["cpassword"]?.ToString();
                                cposition_code = reader["cposition_code"]?.ToString() ?? string.Empty;
                                cposition_name = reader["cposition_name"]?.ToString() ?? string.Empty;
                                role = reader["role"] == DBNull.Value ? "" : reader["role"]?.ToString() ?? "";
                                role_name = reader["role_name"] == DBNull.Value ? "" : reader["role_name"]?.ToString() ?? "";
                            }
                            else
                            {
                                var error = new APIResponse
                                {
                                    status = 404,
                                    statusText = "User does not exist."
                                };
                                string json = JsonConvert.SerializeObject(error);
                                string encrypted = AesEncryption.Encrypt(json);
                                return StatusCode(404, encrypted);
                            }
                        }
                    }
                }
                bool isValid = BCrypt.Net.BCrypt.Verify(User.password, hashedPassword.Trim());


                if (!isValid)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Incorrect Password"
                    };
                    string json = JsonConvert.SerializeObject(error);
                    string encrypted = AesEncryption.Encrypt(json);
                    return StatusCode(400, encrypted);
                }

                if (!int.TryParse(tenantID, out int tenantIdInt))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Invalid TenantID format."
                    };
                    string json = JsonConvert.SerializeObject(error);
                    string encrypted = AesEncryption.Encrypt(json);
                    return StatusCode(400, encrypted);
                }

                var accessToken = _jwtService.GenerateJwtToken(User.userName, tenantIdInt, out var tokenExpiry);
                var refreshToken = _jwtService.GenerateRefreshToken();
                var refreshExpiry = DateTime.Now.AddDays(1);

                var saved = await _jwtService.SaveRefreshTokenToDatabase(User.userName, refreshToken, refreshExpiry);
                if (!saved)
                {
                    var error = new APIResponse
                    {
                        status = 500,
                        statusText = "Failed to save refresh token"
                    };
                    string json = JsonConvert.SerializeObject(error);
                    string encrypted = AesEncryption.Encrypt(json);
                    return StatusCode(500, encrypted);
                }

                var loginDetails = new
                {
                    username = username,
                    firstname = firstname,
                    lastname = lastname,
                    roleID = roleid,
                    tenantID = tenantID,
                    tenantName = tenantname,
                    email = email,
                    position_name = cposition_name,
                    position_code = cposition_code,
                    role = role,
                    role_name = role_name,
                    token = accessToken,
                    refreshToken = refreshToken

                };

                var success = new APIResponse
                {
                    status = 200,
                    statusText = "Logged in Successfully",
                    body = new[] { loginDetails }
                };

                string successJson = JsonConvert.SerializeObject(success);
                string encryptedSuccess = AesEncryption.Encrypt(successJson);
                return StatusCode(200, encryptedSuccess);
            }
            catch (Exception ex)
            {
                var error = new APIResponse
                {
                    status = 500,
                    statusText = "An error occurred during login: " + ex.Message
                };
                string json = JsonConvert.SerializeObject(error);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(500, encrypted);
            }
        }

        [HttpPost]
        [Route("LoginOLD")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> LoginOLD([FromBody] pay request)
        {
            string urlSafe1 = AesEncryption.Decrypt(request.payload);
            var User = JsonConvert.DeserializeObject<User>(urlSafe1);

            APIResponse Objresponse = new APIResponse();

            if (User == null || string.IsNullOrEmpty(User.userName) || string.IsNullOrEmpty(User.password))
                return BadRequest("Username and password must be provided.");

            var connStr = _config.GetConnectionString("Database");
            string status = string.Empty;
            string email = "", TenantID = "", UserID = "", roleid = "", username = "";
            Console.WriteLine("DB Connection String: " + connStr);

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_validate_Admin_login_OLD", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@FilterValue1", User.userName);
                        cmd.Parameters.AddWithValue("@FilterValue2", User.password);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                status = reader["cstatus"]?.ToString() ?? "";
                                if (status == "valid user")
                                {
                                    username = reader["cusername"]?.ToString();
                                    roleid = reader["croleID"]?.ToString();
                                    TenantID = reader["cTenantID"]?.ToString();
                                    email = reader["cemail"]?.ToString();
                                }
                            }
                        }
                    }
                }

                if (status == "invalid password")
                {
                    Objresponse.statusText = "Incorrect Password";
                    Objresponse.status = 400;
                    return BadRequest(Objresponse);
                }

                if (status == "username not exist")
                {
                    Objresponse.statusText = "User does not exist.";
                    Objresponse.status = 404;
                    return NotFound(Objresponse);
                }

                if (status == "valid user")
                {
                    if (!int.TryParse(TenantID, out int tenantIdInt))
                    {
                        Objresponse.statusText = "Invalid TenantID format.";
                        Objresponse.status = 400;
                        return BadRequest(Objresponse);
                    }

                    var accessToken = _jwtService.GenerateJwtToken(User.userName, tenantIdInt, out var tokenExpiry);
                    var refreshToken = _jwtService.GenerateRefreshToken();
                    var refreshExpiry = DateTime.Now.AddDays(1);

                    var saved = await _jwtService.SaveRefreshTokenToDatabase(User.userName, refreshToken, refreshExpiry);
                    if (!saved)
                    {
                        Objresponse.statusText = "Failed to save refresh token";
                        Objresponse.status = 500;
                        return StatusCode(500, Objresponse);
                    }
                    var loginDetails = new
                    {
                        username = username,
                        roleID = roleid,
                        tenantID = TenantID,
                        tenantName = TenantID,
                        email = email,
                        token = accessToken,
                        refreshToken = refreshToken
                    };
                    Objresponse.body = new object[] { loginDetails };
                    Objresponse.statusText = "Logged in Successfully";
                    Objresponse.status = 200;
                    var apiDtls = new APIResponse
                    {
                        status = 200,
                        statusText = "Logged in Successfully",
                        body = new[] { loginDetails }
                    };
                    string json = JsonConvert.SerializeObject(apiDtls);
                    var encryptCartDtls1 = AesEncryption.Encrypt(json);
                    return StatusCode(200, encryptCartDtls1);
                }

                var apierDtls = new APIResponse
                {
                    status = 500,
                    statusText = "Unexpected login status",
                };
                string jsone = JsonConvert.SerializeObject(apierDtls);
                var encryptapierDtls = AesEncryption.Encrypt(jsone);
                return StatusCode(500, encryptapierDtls);

            }
            catch (Exception ex)
            {

                var apierrDtls = new APIResponse
                {
                    status = 500,
                    statusText = "An error occurred during login: " + ex.Message,
                };

                string jsoner = JsonConvert.SerializeObject(apierrDtls);
                var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                return StatusCode(500, encryptapierrDtls);
            }
        }


        [HttpPost]
        [Route("CreateSuperAdmin")]
        public async Task<IActionResult> CreateSuperAdmin([FromBody] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<CreateAdminDTO>(decryptedJson);
                // Insert into database             
                //string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);


                bool emailExists = await _AccountService.CheckEmailExistsAsync(model.cemail, model.ctenant_Id);
                if (emailExists)
                {
                    var conflictResponse = new
                    {
                        status = 409,
                        error = "Conflict",
                        message = "Email already exists"
                    };
                    string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                    var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                    return StatusCode(409, encryptedConflict);
                }
                bool usernameExists = await _AccountService.CheckUsernameExistsAsync(model.cuserid, model.ctenant_Id);
                if (usernameExists)
                {
                    var conflictResponse = new
                    {
                        status = 409,
                        error = "Conflict",
                        message = "Username already exists"
                    };
                    string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                    var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                    return StatusCode(409, encryptedConflict);
                }

                bool phenonoExists = await _AccountService.CheckPhenonoExistsAsync(model.cphoneno, model.ctenant_Id);
                if (phenonoExists)
                {
                    var conflictResponse = new
                    {
                        status = 409,
                        error = "Conflict",
                        message = "Phoneno already exists"
                    };
                    string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                    var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                    return StatusCode(409, encryptedConflict);
                }

                // Insert into database
                int insertedUserId = await _AccountService.InsertSuperAdminAsync(model);

                if (insertedUserId <= 0)
                {
                    return StatusCode(500, new APIResponse
                    {
                        status = 500,
                        statusText = "Failed to create Super Admin"
                    });
                }

                // Prepare response
                var apierDtls = new APIResponse
                {
                    status = 200,
                    statusText = "Super Admin created successfully",
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
        [Route("GetAllSuperAdmin")]
        public async Task<ActionResult> GetAllSuperAdmin()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {

                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var superAdmins = await _AccountService.GetAllSuperAdminsAsync(cTenantID);



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

        [HttpPut("UpdateSuperAdmin")]
        public async Task<IActionResult> UpdateSuperAdmin([FromForm] InputDTO request)
        {

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<UpdateAdminDTO>(decryptedJson);
            bool success = await _AccountService.UpdateSuperAdminAsync(model);

            var response = new APIResponse
            {
                status = success ? 200 : 404,
                statusText = success ? "Update successful" : "SuperAdmin not found or update failed",
                body = new object[] { new { UserID = model.cid } }
            };



            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [Authorize]
        [HttpDelete("DeleteSuperAdmin")]
        public async Task<IActionResult> DeleteSuperAdmin([FromQuery] pay request)
        {
            if (request == null)
            {
                return EncryptedError(400, "Request body cannot be null");
            }

            if (string.IsNullOrWhiteSpace(request.payload))
            {
                return EncryptedError(400, "Payload cannot be empty");
            }
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                return EncryptedError(400, "Authorization token is missing");
            }
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
            string username = usernameClaim;

            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                 string.IsNullOrWhiteSpace(usernameClaim))

            {
                var error = new APIResponse
                {
                    status = 401,
                    statusText = "Invalid or missing cTenantID in token."
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(400, $"\"{encryptedError}\"");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<DeleteAdminDTO>(decryptedJson);
            bool success = await _AccountService.DeleteSuperAdminAsync(model, cTenantID, username);

            var response = new APIResponse
            {
                status = success ? 200 : 404,
                statusText = success ? "SuperAdmin deleted successfully" : "SuperAdmin not found",
                body = new object[] { new { UserID = model.cid } }
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, $"\"{encrypted}\"");
        }

        [Authorize]
        [HttpGet]
        [Route("GetAllUser")]
        public async Task<ActionResult> GetAllUser()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {

                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                var superAdmins = await _AccountService.GetAllUserAsync(cTenantID);



                var response = new APIResponse
                {
                    body = superAdmins?.ToArray() ?? Array.Empty<object>(),
                    statusText = superAdmins == null || !superAdmins.Any() ? "No Users found" : "Successful",
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
        [Route("GetAllUserbyid")]
        public async Task<ActionResult> GetAllUserbyid([FromQuery] string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id) || !int.TryParse(id, out int parsedId) || parsedId <= 0)
                {
                    var invalidIdResponse = new APIResponse
                    {
                        body = Array.Empty<object>(),
                        statusText = "Valid ID parameter is required",
                        status = 400
                    };

                    string invalidIdJson = JsonConvert.SerializeObject(invalidIdResponse);
                    var encryptedInvalidId = AesEncryption.Encrypt(invalidIdJson);
                    return StatusCode(400, encryptedInvalidId);
                }

                int userid = parsedId;

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    var tokenMissingResponse = new APIResponse
                    {
                        body = Array.Empty<object>(),
                        statusText = "Authorization token is missing",
                        status = 400
                    };

                    string tokenMissingJson = JsonConvert.SerializeObject(tokenMissingResponse);
                    var encryptedTokenMissing = AesEncryption.Encrypt(tokenMissingJson);
                    return StatusCode(400, encryptedTokenMissing);
                }

                var handler = new JwtSecurityTokenHandler();
                JwtSecurityToken jsonToken;

                try
                {
                    jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
                }
                catch (Exception tokenEx)
                {
                    var tokenErrorResponse = new APIResponse
                    {
                        body = Array.Empty<object>(),
                        statusText = $"Invalid JWT token: {tokenEx.Message}",
                        status = 400
                    };

                    string tokenErrorJson = JsonConvert.SerializeObject(tokenErrorResponse);
                    var encryptedTokenError = AesEncryption.Encrypt(tokenErrorJson);
                    return StatusCode(400, encryptedTokenError);
                }

                if (jsonToken == null)
                {
                    var invalidTokenResponse = new APIResponse
                    {
                        body = Array.Empty<object>(),
                        statusText = "Invalid JWT token format",
                        status = 400
                    };

                    string invalidTokenJson = JsonConvert.SerializeObject(invalidTokenResponse);
                    var encryptedInvalidToken = AesEncryption.Encrypt(invalidTokenJson);
                    return StatusCode(400, encryptedInvalidToken);
                }

                var tenantIdClaim = jsonToken.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    var tenantIdResponse = new APIResponse
                    {
                        body = Array.Empty<object>(),
                        statusText = "Invalid or missing cTenantID in token.",
                        status = 400
                    };

                    string tenantIdJson = JsonConvert.SerializeObject(tenantIdResponse);
                    var encryptedTenantId = AesEncryption.Encrypt(tenantIdJson);
                    return StatusCode(400, encryptedTenantId);
                }

                var superAdmins = await _AccountService.GetAllUserIdAsync(cTenantID, userid);

                var response = new APIResponse
                {
                    body = superAdmins?.ToArray() ?? Array.Empty<object>(),
                    statusText = superAdmins == null || !superAdmins.Any() ? "No Users found" : "Successful",
                    status = superAdmins == null || !superAdmins.Any() ? 204 : 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(response.status, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    body = Array.Empty<object>(),
                    statusText = $"Unexpected error: {ex.Message}",
                    status = 500
                };

                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreateUser")]
        public async Task<IActionResult> CreateUser([FromBody] pay request)
        {
            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<CreateUserDTO>(decryptedJson);

            bool usernameExists = await _AccountService.CheckuserUsernameExistsAsync(model.cuserid, model.ctenantID);
            if (usernameExists)
            {
                var conflictResponse = new
                {
                    status = 409,
                    error = "Conflict",
                    message = "Username already exists"
                };
                string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                return StatusCode(409, encryptedConflict);
            }

            bool useremailExists = await _AccountService.CheckuserEmailExistsAsync(model.cemail, model.ctenantID);
            if (useremailExists)
            {
                var conflictResponse = new
                {
                    status = 409,
                    error = "Conflict",
                    message = "Email already exists"
                };
                string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                return StatusCode(409, encryptedConflict);
            }

            bool userphonenoExists = await _AccountService.CheckuserPhonenoExistsAsync(model.cphoneno, model.ctenantID);
            if (userphonenoExists)
            {
                var conflictResponse = new
                {
                    status = 409,
                    error = "Conflict",
                    message = "Phoneno already exists"
                };
                string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                return StatusCode(409, encryptedConflict);
            }

            int result = await _AccountService.InsertUserAsync(model);

            var response = new APIResponse
            {
                status = result > 0 ? 200 : 400,
                statusText = result > 0 ? "User created successfully" : "User creation failed",
                body = new object[] { new { UserID = result } }
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [Authorize]
        [HttpPut("UpdateUser")]
        public async Task<IActionResult> UpdateUser([FromBody] pay request)
        {
            try
            {
                if (request == null)
                {
                    return EncryptedError(400, "Request body cannot be null");
                }

                if (string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Payload cannot be empty");
                }
                // Extract token and tenant ID
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    var error = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var model = JsonConvert.DeserializeObject<UpdateUserDTO>(decryptedJson);

                model.ctenantID = cTenantID;

                bool usernameExists = await _AccountService.CheckuserUsernameExistsputAsync(model.cusername, model.ctenantID, model.id);
                if (usernameExists)
                {
                    var conflictResponse = new
                    {
                        status = 409,
                        error = "Conflict",
                        message = "Username already exists"
                    };
                    string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                    var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                    return StatusCode(409, encryptedConflict);
                }

                bool useremailExists = await _AccountService.CheckuserEmailExistsputAsync(model.cemail, model.ctenantID, model.id);
                if (useremailExists)
                {
                    var conflictResponse = new
                    {
                        status = 409,
                        error = "Conflict",
                        message = "Email already exists"
                    };
                    string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                    var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                    return StatusCode(409, encryptedConflict);
                }

                bool userphonenoExists = await _AccountService.CheckuserPhonenoExistsputAsync(model.cphoneno, model.ctenantID, model.id);
                if (userphonenoExists)
                {
                    var conflictResponse = new
                    {
                        status = 409,
                        error = "Conflict",
                        message = "Phoneno already exists"
                    };
                    string conflictJson = JsonConvert.SerializeObject(conflictResponse);
                    var encryptedConflict = AesEncryption.Encrypt(conflictJson);
                    return StatusCode(409, encryptedConflict);
                }



                bool updated = await _AccountService.UpdateUserAsync(model, cTenantID);

                var response = new APIResponse
                {
                    status = updated ? 200 : 204,
                    statusText = updated ? "User updated successfully" : "User not found or update failed"
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, $"\"{encrypted}\"");
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error updating user: {ex.Message}"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, $"\"{encryptedError}\"");
            }
        }

        [Authorize]
        [HttpDelete("Deleteuser")]
        public async Task<IActionResult> Deleteuser([FromQuery] pay request)
        {
            if (request == null)
            {
                return EncryptedError(400, "Request body cannot be null");
            }

            if (string.IsNullOrWhiteSpace(request.payload))
            {
                return EncryptedError(400, "Payload cannot be empty");
            }
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (string.IsNullOrWhiteSpace(jwtToken))
            {
                return EncryptedError(400, "Authorization token is missing");
            }
            var handler = new JwtSecurityTokenHandler();
            var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

            var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
            var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
            string username = usernameClaim;
            if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                 string.IsNullOrWhiteSpace(usernameClaim))

            {
                var error = new APIResponse
                {
                    status = 401,
                    statusText = "Invalid or missing cTenantID in token."
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(400, $"\"{encryptedError}\"");
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<DeleteuserDTO>(decryptedJson);
            bool success = await _AccountService.DeleteuserAsync(model, cTenantID, username);

            var response = new APIResponse
            {
                status = success ? 200 : 204,
                statusText = success ? "User deleted successfully" : "User not found"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, $"\"{encrypted}\"");
        }

        [Authorize]
        [HttpPost]
        [Route("oTPGenerateAdmin")]
        public async Task<ActionResult> oTPGenerateAdmin()
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    var errorResponse = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID in token."
                    };
                    string json = JsonConvert.SerializeObject(errorResponse);
                    string encrypted = AesEncryption.Encrypt(json);
                    return Ok(encrypted);
                }
                string query = "SELECT cphoneno,cuserid FROM AdminUsers WHERE crole_id = 1 AND ctenant_Id = @tenantID";
                DataSet ds1 = new DataSet();

                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@tenantID", cTenantID);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(ds1);
                }

                string op = JsonConvert.SerializeObject(ds1.Tables[0], Formatting.Indented);
                var model = JsonConvert.DeserializeObject<List<DTO.createCustomerMadel>>(op);

                if (model.Count == 0)
                {
                    var responsee = new APIResponse { status = 500, statusText = "Mobile Number Not Registered" };
                    string json = JsonConvert.SerializeObject(responsee);
                    string encrypted = AesEncryption.Encrypt(json);
                    return Ok(encrypted);
                }

                string mobile = model[0].cphoneno;
                int id = Convert.ToInt32(model[0].cuserid);

                int otp = new Random().Next(100000, 999999);

                var url = "https://44d5837031a337405506c716260bed50bd5cb7d2b25aa56c:57bbd9d33fb4411f82b2f9b324025c8a63c75a5b237c745a@api.exotel.com/v1/Accounts/sheenlac2/Sms/send%20?From=08047363322&To=" + mobile + "&Body=Your Verification Code is  " + otp + " - Allpaints.in";

                var client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("44d5837031a337405506c716260bed50bd5cb7d2b25aa56c:57bbd9d33fb4411f82b2f9b324025c8a63c75a5b237c745a");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                var response = await client.PostAsync(url, null);
                var result = await response.Content.ReadAsStringAsync();
                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO OTP_Validation 
                (ctenantID, cuserid, cotpcode, cpurpose, nIsUsed, lusedAt, cexpiryDate) 
                VALUES (@tenantID, @userID, @otp, @purpose, @isUsed, @usedAt, @expiry)", con))
                    {
                        cmd.Parameters.AddWithValue("@tenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@userID", id);
                        cmd.Parameters.AddWithValue("@otp", otp);
                        cmd.Parameters.AddWithValue("@purpose", "AdminLogin");
                        cmd.Parameters.AddWithValue("@isUsed", 0);
                        cmd.Parameters.AddWithValue("@usedAt", DBNull.Value);
                        cmd.Parameters.AddWithValue("@expiry", DateTime.Now.AddMinutes(5));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                var response1 = new APIResponse { status = 200, statusText = "OTP Sent Successfully" };
                string json2 = JsonConvert.SerializeObject(response1);
                string encryptedResponse = AesEncryption.Encrypt(json2);
                return Ok(encryptedResponse);
            }
            catch (Exception ex)
            {
                var error = new APIResponse { status = 500, statusText = "Error: " + ex.Message };
                string json = JsonConvert.SerializeObject(error);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(500, encrypted);
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
        private ActionResult EncryptedResponse(string message, object body = null)
        {
            var response = new APIResponse
            {
                status = 200,
                statusText = message,
                body = body != null ? new object[] { body } : null
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return Ok(encrypted);
        }


        [Authorize]
        [HttpPost("verifyOtpAndExecute")]
        public async Task<ActionResult> VerifyOtpAndExecute([FromBody] pay request)
        {
            try
            {
                if (request == null)
                {
                    return EncryptedError(400, "Request body cannot be null");
                }

                if (string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Payload cannot be empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;
                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
                string username = usernameClaim;
                if (!int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                    return EncryptedError(401, "Invalid token claims");

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var baseRequest = JsonConvert.DeserializeObject<OtpActionRequest<object>>(decryptedJson);

                if (baseRequest == null)
                    return EncryptedError(400, "Invalid request format");

                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await con.OpenAsync();
                    using (var tx = con.BeginTransaction())
                    {
                        string query = @"SELECT * FROM OTP_Validation 
                                 WHERE ctenantID = @tenantID AND cotpcode = @otp AND cpurpose = 'AdminLogin'";

                        using (SqlCommand cmd = new SqlCommand(query, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@tenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@otp", baseRequest.otp);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (!reader.HasRows)
                                    return EncryptedError(404, "OTP not found");

                                await reader.ReadAsync();
                                if (Convert.ToBoolean(reader["nIsUsed"]))
                                    return EncryptedError(403, "OTP already used");

                                if (DateTime.Now > Convert.ToDateTime(reader["cexpiryDate"]))
                                    return EncryptedError(410, "OTP expired");
                            }
                        }

                        string updateQuery = @"UPDATE OTP_Validation 
                                       SET nIsUsed = 1, lusedAt = GETDATE() 
                                       WHERE ctenantID = @tenantID AND cpurpose = 'AdminLogin' AND cotpcode = @otp";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con, tx))
                        {
                            updateCmd.Parameters.AddWithValue("@tenantID", cTenantID);
                            updateCmd.Parameters.AddWithValue("@otp", baseRequest.otp);
                            await updateCmd.ExecuteNonQueryAsync();
                        }
                        tx.Commit();
                    }
                }
                // Handle actions
                switch (baseRequest.action?.ToUpper())
                {
                    case "POST":
                        try
                        {

                            var otpRequest = JsonConvert.DeserializeObject<OtpActionRequest<CreateAdminDTO>>(decryptedJson);
                            if (otpRequest?.payload == null)
                                return EncryptedError(400, "Invalid request data");

                            var model = otpRequest.payload;

                            int insertedUserId = await _AccountService.InsertSuperAdminAsync(model);

                            if (insertedUserId <= 0)
                                return EncryptedError(500, "Failed to create Super Admin");


                            var apierDtls = new APIResponse
                            {
                                status = 200,
                                statusText = "Super Admin created successfully",
                                body = new object[] { new { UserID = insertedUserId } }
                            };
                            string jsone = JsonConvert.SerializeObject(apierDtls);
                            var encryptapierDtls = AesEncryption.Encrypt(jsone);
                            return StatusCode(200, encryptapierDtls);


                            var response1 = new APIResponse { status = 200, statusText = "OTP Sent Successfully" };
                            string json2 = JsonConvert.SerializeObject(response1);


                        }
                        catch (Exception ex)
                        {
                            var apierrDtls = new APIResponse
                            {
                                status = 500,
                                statusText = "Error creating Super Admin",
                                error = ex.Message
                            };
                            string jsoner = JsonConvert.SerializeObject(apierrDtls);
                            var encryptapierrDtls = AesEncryption.Encrypt(jsoner);
                            return StatusCode(500, encryptapierrDtls);
                        }
                        break;
                    case "PUT":
                        var updateModel = JsonConvert.DeserializeObject<OtpActionRequest<UpdateAdminDTO>>(decryptedJson);
                        bool updated = await _AccountService.UpdateSuperAdminAsync(updateModel.payload);

                        return EncryptedResponse(
                            updated ? "Super Admin updated successfully" : "Update failed",
                            new { UserID = updateModel.payload.cid }
                        );
                    case "DELETE":
                        var deleteModel = JsonConvert.DeserializeObject<OtpActionRequest<DeleteAdminDTO>>(decryptedJson);
                        bool deleted = await _AccountService.DeleteSuperAdminAsync(deleteModel.payload, cTenantID, username);

                        return EncryptedResponse(
                            deleted ? "Super Admin deleted successfully" : "Delete failed",
                            new { UserID = deleteModel.payload.cid }
                        );
                    default:
                        return EncryptedError(400, "Invalid action");
                }
            }
            catch (Exception ex)
            {

                return EncryptedError(500, "Something went wrong");
            }
        }

        [HttpPost("verifyOtpforforgetpassword")]
        public async Task<ActionResult> VerifyOtpforforgetpassword([FromBody] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var modeld = JsonConvert.DeserializeObject<ForgotOtpverify>(decryptedJson);

                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await con.OpenAsync();
                    using (var tx = con.BeginTransaction())
                    {
                        string query = @"SELECT TOP 1 ctenantID, cuserid, cpurpose, nIsUsed, cexpiryDate 
                                 FROM OTP_Validation 
                                 WHERE cotpcode = @otp AND cpurpose = 'Forgot Password'";

                        int tenantId = 0;
                        string userName = null;

                        using (SqlCommand cmd = new SqlCommand(query, con, tx))
                        {
                            cmd.Parameters.AddWithValue("@otp", modeld.otp);

                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                if (!reader.HasRows)
                                    return EncryptedError(404, "OTP not found");

                                await reader.ReadAsync();

                                if (Convert.ToBoolean(reader["nIsUsed"]))
                                    return EncryptedError(403, "OTP already used");

                                if (DateTime.Now > Convert.ToDateTime(reader["cexpiryDate"]))
                                    return EncryptedError(410, "OTP expired");

                                tenantId = Convert.ToInt32(reader["ctenantID"]);
                                userName = reader["cuserid"].ToString();
                            }
                        }

                        // Mark OTP as used
                        string updateQuery = @"UPDATE OTP_Validation 
                                       SET nIsUsed = 1, lusedAt = GETDATE() 
                                       WHERE cotpcode = @otp AND cpurpose = 'Forgot Password'";
                        using (SqlCommand updateCmd = new SqlCommand(updateQuery, con, tx))
                        {
                            updateCmd.Parameters.AddWithValue("@otp", modeld.otp);
                            await updateCmd.ExecuteNonQueryAsync();
                        }

                        // Generate JWT token using values from OTP_Validation
                        var accessToken = _jwtService.GenerateJwtToken(userName, tenantId, out var tokenExpiry);

                        tx.Commit();

                        // Return OTP verified message + JWT token
                        var response1 = new APIResponse
                        {
                            status = 200,
                            statusText = "OTP Verified Successfully",
                            body = new object[] { new { token = accessToken, expiresAt = tokenExpiry } }

                        };

                        string json2 = JsonConvert.SerializeObject(response1);
                        string encryptedResponse = AesEncryption.Encrypt(json2);
                        return Ok(encryptedResponse);
                    }
                }
            }
            catch (Exception ex)
            {
                var error = new APIResponse { status = 500, statusText = "Error: " + ex.Message };
                string json = JsonConvert.SerializeObject(error);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(500, encrypted);
            }
        }

        [HttpPost]
        [Route("Forgotpasswordmaster")]
        public async Task<ActionResult> Forgotpasswordmaster([FromBody] pay request)
        {
            try
            {
                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var modeld = JsonConvert.DeserializeObject<forgototp>(decryptedJson);

                string query = "SELECT top 1 cuserid,cphoneno,cTenant_ID FROM AdminUsers WHERE cphoneno=@cphoneno";
                DataSet ds1 = new DataSet();

                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.Parameters.AddWithValue("@cphoneno", modeld.cphoneno);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    adapter.Fill(ds1);
                }

                string op = JsonConvert.SerializeObject(ds1.Tables[0], Formatting.Indented);
                var model = JsonConvert.DeserializeObject<List<DTO.forgototpModel>>(op);

                if (model.Count == 0)
                {
                    var responsee = new APIResponse { status = 500, statusText = "Mobile Number Not Registered" };
                    string json = JsonConvert.SerializeObject(responsee);
                    string encrypted = AesEncryption.Encrypt(json);
                    return Ok(encrypted);
                }

                string mobile = model[0].cphoneno;
                int id = Convert.ToInt32(model[0].cuser_name);
                int cTenantID = Convert.ToInt32(model[0].cTenant_ID);
                int otp = new Random().Next(100000, 999999);

                var url = "https://44d5837031a337405506c716260bed50bd5cb7d2b25aa56c:57bbd9d33fb4411f82b2f9b324025c8a63c75a5b237c745a@api.exotel.com/v1/Accounts/sheenlac2/Sms/send%20?From=08047363322&To=" + mobile + "&Body=Your Verification Code is  " + otp + " - Allpaints.in";

                var client = new HttpClient();

                var byteArray = Encoding.ASCII.GetBytes("44d5837031a337405506c716260bed50bd5cb7d2b25aa56c:57bbd9d33fb4411f82b2f9b324025c8a63c75a5b237c745a");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));
                var response = await client.PostAsync(url, null);
                var result = await response.Content.ReadAsStringAsync();
                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(@"INSERT INTO OTP_Validation 
         (ctenantID, cuserid, cotpcode, cpurpose, nIsUsed, lusedAt, cexpiryDate) 
         VALUES (@tenantID, @userID, @otp, @purpose, @isUsed, @usedAt, @expiry)", con))
                    {
                        cmd.Parameters.AddWithValue("@tenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@userID", id);
                        cmd.Parameters.AddWithValue("@otp", otp);
                        cmd.Parameters.AddWithValue("@purpose", "Forgot Password");
                        cmd.Parameters.AddWithValue("@isUsed", 0);
                        cmd.Parameters.AddWithValue("@usedAt", DBNull.Value);
                        cmd.Parameters.AddWithValue("@expiry", DateTime.Now.AddMinutes(5));

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
                var response1 = new APIResponse { status = 200, statusText = "OTP Sent Successfully" };
                string json2 = JsonConvert.SerializeObject(response1);
                string encryptedResponse = AesEncryption.Encrypt(json2);
                return Ok(encryptedResponse);
            }
            catch (Exception ex)
            {
                var error = new APIResponse { status = 500, statusText = "Error: " + ex.Message };
                string json = JsonConvert.SerializeObject(error);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(500, encrypted);
            }
        }

        //[Authorize]
        //[HttpPut("UpdateSuperAdminpassword")]
        //public async Task<IActionResult> UpdateSuperAdminpassword([FromBody] pay request)
        //{
        //    var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        //    var handler = new JwtSecurityTokenHandler();
        //    var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

        //    var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
        //    var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;
        //    string username = usernameClaim;
        //    if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
        //    {
        //        var error = new APIResponse
        //        {
        //            status = 401,
        //            statusText = "Invalid or missing cTenantID in token."
        //        };
        //        string errorJson = JsonConvert.SerializeObject(error);
        //        string encryptedError = AesEncryption.Encrypt(errorJson);
        //        return StatusCode(401, $"\"{encryptedError}\"");
        //    }
        //    string decryptedJson = AesEncryption.Decrypt(request.payload);
        //    var model = JsonConvert.DeserializeObject<UpdateadminPassword>(decryptedJson);
        //    bool success = await _AccountService.UpdatePasswordSuperAdminAsync(model, cTenantID, username);

        //    var response = new APIResponse
        //    {
        //        status = success ? 200 : 204,
        //        statusText = success ? "Update successful" : "SuperAdmin not found or update failed"
        //    };

        //    string json = JsonConvert.SerializeObject(response);
        //    string encrypted = AesEncryption.Encrypt(json);
        //    return StatusCode(response.status, encrypted);
        //}


        [Authorize]
        [HttpPut("UpdateSuperAdminpassword")]
        public async Task<IActionResult> UpdateSuperAdminpassword([FromBody] pay request)
        {
            if (request == null)
            {
                return EncryptedError(400, "Request body cannot be null");
            }

            if (string.IsNullOrWhiteSpace(request.payload))
            {
                return EncryptedError(400, "Payload cannot be empty");
            }
            if (!HttpContext.Items.TryGetValue("cTenantID", out var tenantIdObj) ||
                !HttpContext.Items.TryGetValue("username", out var usernameObj) ||
                !(tenantIdObj is int cTenantID) || string.IsNullOrWhiteSpace(usernameObj?.ToString()))
            {
                var error = new APIResponse
                {
                    status = 401,
                    statusText = "Invalid or missing token claims."
                };
                string errorJson = JsonConvert.SerializeObject(error);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(401, encryptedError);
            }

            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<UpdateadminPassword>(decryptedJson);
            bool success = await _AccountService.UpdatePasswordSuperAdminAsync(model, cTenantID, usernameObj.ToString());

            var response = new APIResponse
            {
                status = success ? 200 : 204,
                statusText = success ? "Update successful" : "SuperAdmin not found or update failed"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [Authorize]
        [HttpPost("fileUpload")]
        public async Task<IActionResult> fileUpload([FromForm] FileUploadDTO model)
        {
            if(model == null)
            {
                return EncryptedError(400, "Request body cannot be null");
            }
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if(string.IsNullOrWhiteSpace(jwtToken))
            {
               return EncryptedError(400, "Authorization token is missing");    
            }
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

            try
            {
                if (model.file == null || model.file.Length == 0)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "File not selected."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                // Step 1: Determine upload path

                string basePath = model.type switch
                {
                    "Superadmin" => _config["UploadSettings:SuperadminUploadPath"],
                    "user" => _config["UploadSettings:userUploadPath"],
                    _ => throw new Exception("Invalid type. Must be 'Superadmin' or 'user'.")
                };

                if (!Directory.Exists(basePath))
                    Directory.CreateDirectory(basePath);

                string sanitizedFileName = Path.GetFileName(model.file.FileName);
                string fileName = $"{model.id}_{sanitizedFileName}";
                string fullPath = Path.Combine(basePath, fileName);

                // Step 2: Save file to disk
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    await model.file.CopyToAsync(stream);
                }

                // Step 3: Update database (optional success)
                try
                {


                    string connStr = _config.GetConnectionString("Database");
                    using (var conn = new SqlConnection(connStr))
                    {
                        await conn.OpenAsync();

                        string targetTable = model.type.ToLower() switch
                        {
                            "user" => "Users",
                            "superadmin" => "AdminUsers",
                            _ => throw new Exception("Invalid type. Must be 'Superadmin' or 'user'.")
                        };

                        string query = $@"
                    UPDATE {targetTable}
                    SET cprofile_image_name = @ProfilePath,
                        cprofile_image_path = @FilePath
                    WHERE id = @UserId";

                        using (var cmd = new SqlCommand(query, conn))
                        {
                            cmd.Parameters.AddWithValue("@ProfilePath", fileName);
                            cmd.Parameters.AddWithValue("@FilePath", fullPath);
                            cmd.Parameters.AddWithValue("@UserId", model.id);

                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                }
                catch (Exception dbEx)
                {
                    // Log DB error if needed, but don't block success response
                }

                // Step 4: Return success based on file upload
                var response = new APIResponse
                {
                    status = 200,
                    statusText = "File uploaded successfully."
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Error occurred: {ex.Message}"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        //[Authorize]
        //[HttpPost("CreateUsersBulk")]
        //public async Task<IActionResult> CreateUsersBulk([FromBody] pay request)
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
        //            var error = new APIResponse
        //            {
        //                status = 401,
        //                statusText = "Invalid or missing cTenantID in token."
        //            };
        //            string errorJson = JsonConvert.SerializeObject(error);
        //            string encryptedError = AesEncryption.Encrypt(errorJson);
        //            return StatusCode(401, $"\"{encryptedError}\"");
        //        }

        //        string username = usernameClaim;


        //        string decryptedJson = AesEncryption.Decrypt(request.payload);
        //        var users = JsonConvert.DeserializeObject<List<CreateUserDTO>>(decryptedJson);

        //        if (users == null || !users.Any())
        //            return BadRequest("No users provided");

        //        var validUsers = new List<CreateUserDTO>();
        //        var failedUsers = new List<object>();

        //        var duplicateUsernames = users.GroupBy(u => u.cuserid)
        //                                     .Where(g => g.Count() > 1)
        //                                     .Select(g => g.Key)
        //                                     .ToList();

        //        var duplicateEmails = users.GroupBy(u => u.cemail)
        //                                  .Where(g => g.Count() > 1)
        //                                  .Select(g => g.Key)
        //                                  .ToList();

        //        var duplicatePhones = users.GroupBy(u => u.cphoneno)
        //                                  .Where(g => g.Count() > 1)
        //                                  .Select(g => g.Key)
        //                                  .ToList();

        //        foreach (var user in users)
        //        {
        //            var errors = new List<string>();


        //            if (user.cuserid <= 0) errors.Add("User ID is mandatory");
        //            if (string.IsNullOrEmpty(user.cemail)) errors.Add("Email is mandatory");
        //            if (string.IsNullOrEmpty(user.cphoneno)) errors.Add("Phone number is mandatory");

        //            user.ctenantID = cTenantID;

        //            if (duplicateUsernames.Contains(user.cuserid)) errors.Add("Duplicate username in this batch");
        //            if (duplicateEmails.Contains(user.cemail)) errors.Add("Duplicate email in this batch");
        //            if (duplicatePhones.Contains(user.cphoneno)) errors.Add("Duplicate phone in this batch");

        //            if (errors.Any())
        //            {
        //                failedUsers.Add(new
        //                {
        //                    user.cemail,
        //                    user.cuserid,
        //                    user.cphoneno,
        //                    reason = string.Join("; ", errors)
        //                });
        //            }
        //            else
        //            {
        //                validUsers.Add(user);
        //            }
        //        }

        //        int insertedCount = 0;
        //        if (validUsers.Any())
        //        {
        //            insertedCount = await _AccountService.InsertUsersBulkAsync(validUsers, cTenantID,username);
        //        }

        //        var response = new
        //        {
        //            status = 200,
        //            statusText = "Bulk user creation completed",
        //            body = new
        //            {
        //                total = users.Count,
        //                success = insertedCount,
        //                failure = failedUsers.Count,
        //                inserted = validUsers.Select(u => new { u.cemail, u.cuserid }),
        //                failed = failedUsers,
        //            },
        //            error = ""
        //        };

        //        string json = JsonConvert.SerializeObject(response);
        //        string encrypted = AesEncryption.Encrypt(json);
        //        return Ok(encrypted);
        //    }
        //    catch (Exception ex)
        //    {
        //        var errorResponse = new { status = 500, statusText = "Error", error = ex.Message };
        //        string errorJson = JsonConvert.SerializeObject(errorResponse);
        //        var encryptedError = AesEncryption.Encrypt(errorJson);
        //        return StatusCode(500, encryptedError);
        //    }
        //}


        [Authorize]
        [HttpPost("CreateUsersBulk3")]
        public async Task<IActionResult> CreateUsersBulk3([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var users = JsonConvert.DeserializeObject<List<BulkUserDTO>>(decryptedJson);

                if (users == null || !users.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No users provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupUserIds = users.GroupBy(u => u.cuserid).Where(g => g.Count() > 1 && g.Key > 0).Select(g => g.Key).ToList();
                var dupUsernames = users.GroupBy(u => u.cusername).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();
                var dupEmails = users.GroupBy(u => u.cemail).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();
                var dupPhones = users.GroupBy(u => u.cphoneno).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();

                if (dupUserIds.Any() || dupUsernames.Any() || dupEmails.Any() || dupPhones.Any())
                {
                    if (dupUserIds.Any()) duplicateErrors.Add($"Duplicate cuserid(s): {string.Join(", ", dupUserIds)}");
                    if (dupUsernames.Any()) duplicateErrors.Add($"Duplicate cusername(s): {string.Join(", ", dupUsernames)}");
                    if (dupEmails.Any()) duplicateErrors.Add($"Duplicate cemail(s): {string.Join(", ", dupEmails)}");
                    if (dupPhones.Any()) duplicateErrors.Add($"Duplicate cphoneno(s): {string.Join(", ", dupPhones)}");

                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for one or more mandatory fields. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedUsers = new List<object>();
                var validUsers = new List<BulkUserDTO>();
                bool hasValidationErrors = false;

                foreach (var user in users)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (user.cuserid <= 0)
                    {
                        errors.Add("cuserid: Must be positive number");
                        nullMandatoryFields.Add("cuserid");
                    }
                    if (string.IsNullOrEmpty(user.cusername))
                    {
                        errors.Add("cusername: Field is required");
                        nullMandatoryFields.Add("cusername");
                    }
                    if (string.IsNullOrEmpty(user.cemail))
                    {
                        errors.Add("cemail: Field is required");
                        nullMandatoryFields.Add("cemail");
                    }
                    if (string.IsNullOrEmpty(user.cpassword))
                    {
                        errors.Add("cpassword: Field is required");
                        nullMandatoryFields.Add("cpassword");
                    }
                    if (string.IsNullOrEmpty(user.cphoneno))
                    {
                        errors.Add("cphoneno: Field is required");
                        nullMandatoryFields.Add("cphoneno");
                    }

                    var optionalFields = new Dictionary<string, object?>
            {
                { "cfirstName", user.cfirstName },
                { "clastName", user.clastName },
                { "cAlternatePhone", user.cAlternatePhone },
                { "ldob", user.ldob },
                { "cMaritalStatus", user.cMaritalStatus },
                { "cnation", user.cnation },
                { "cgender", user.cgender },
                { "caddress", user.caddress },
                { "caddress1", user.caddress1 },
                { "caddress2", user.caddress2 },
                { "cpincode", user.cpincode },
                { "ccity", user.ccity },
                { "cstatecode", user.cstatecode },
                { "cstatedesc", user.cstatedesc },
                { "ccountrycode", user.ccountrycode },
                { "cbankName", user.cbankName },
                { "caccountNumber", user.caccountNumber },
                { "ciFSC_code", user.ciFSC_code },
                { "cpAN", user.cpAN },
                { "ldoj", user.ldoj },
                { "cemploymentStatus", user.cemploymentStatus },
                { "nnoticePeriodDays", user.nnoticePeriodDays },
                { "cempcategory", user.cempcategory },
                { "cworkloccode", user.cworkloccode },
                { "cworklocname", user.cworklocname },
                { "cgradecode", user.cgradecode },
                { "cgradedesc", user.cgradedesc },
                { "csubrolecode", user.csubrolecode },
                { "cdeptcode", user.cdeptcode },
                { "cdeptdesc", user.cdeptdesc },
                { "cjobcode", user.cjobcode },
                { "cjobdesc", user.cjobdesc },
                { "creportmgrcode", user.creportmgrcode },
                { "creportmgrname", user.creportmgrname },
                { "croll_id", user.croll_id },
                { "croll_name", user.croll_name },
                { "croll_id_mngr", user.croll_id_mngr },
                { "croll_id_mngr_desc" , user.croll_id_mngr_desc },
                { "cReportManager_empcode", user.cReportManager_empcode },
                { "cReportManager_Poscode", user.cReportManager_Poscode },
                { "cReportManager_Posdesc", user.cReportManager_Posdesc }
            };

                    var missingOptionalFields = optionalFields
                        .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                        .Select(f => f.Key)
                        .ToList();

                    if (!string.IsNullOrEmpty(user.cemail))
                    {
                        try
                        {
                            var addr = new System.Net.Mail.MailAddress(user.cemail);
                            if (addr.Address != user.cemail)
                                errors.Add("cemail: Invalid email format");
                        }
                        catch
                        {
                            errors.Add("cemail: Invalid email format");
                        }
                    }

                    if (!string.IsNullOrEmpty(user.cphoneno) && !System.Text.RegularExpressions.Regex.IsMatch(user.cphoneno, @"^[0-9]{10}$"))
                        errors.Add("cphoneno: Invalid phone number format (must be 10 digits)");

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedUsers.Add(new
                        {
                            cemail = user.cemail ?? "NULL",
                            cuserid = user.cuserid,
                            cphoneno = user.cphoneno ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" },
                            missing_optional_fields = missingOptionalFields.Any() ? missingOptionalFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validUsers.Add(user);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_users_received = users.Count,
                            total_valid_users = validUsers.Count,
                            total_failed_users = failedUsers.Count,
                            failed_users = failedUsers,
                            valid_users = validUsers.Select(u => new
                            {
                                u.cemail,
                                u.cuserid,
                                u.cphoneno,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<BulkUserDTO> successfullyInsertedUsers = new List<BulkUserDTO>();
                List<object> databaseFailedUsers = new List<object>();

                if (validUsers.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertUsersBulkAsync(validUsers, cTenantID, usernameClaim);

                        successfullyInsertedUsers = validUsers;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedUsers.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some users may already exist in the database. Check unique constraints (cuserid, cusername, cemail, cphoneno)."
                        });

                        insertedCount = 0;
                        successfullyInsertedUsers = new List<BulkUserDTO>();
                    }
                }

                var insertedUsersWithDetails = successfullyInsertedUsers.Select(u => new
                {
                    u.cemail,
                    u.cuserid,
                    u.cphoneno,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" },
                    missing_optional_fields = new Dictionary<string, object?>
            {
                { "cfirstName", u.cfirstName },
                { "clastName", u.clastName },
                { "cAlternatePhone", u.cAlternatePhone },
                { "ldob", u.ldob },
                { "cMaritalStatus", u.cMaritalStatus },
                { "cnation", u.cnation },
                { "cgender", u.cgender },
                { "caddress", u.caddress },
                { "caddress1", u.caddress1 },
                { "caddress2", u.caddress2 },
                { "cpincode", u.cpincode },
                { "ccity", u.ccity },
                { "cstatecode", u.cstatecode },
                { "cstatedesc", u.cstatedesc },
                { "ccountrycode", u.ccountrycode },
                { "cbankName", u.cbankName },
                { "caccountNumber", u.caccountNumber },
                { "ciFSC_code", u.ciFSC_code },
                { "cpAN" , u.cpAN },
                { "ldoj", u.ldoj },
                { "cemploymentStatus", u.cemploymentStatus },
                { "nnoticePeriodDays", u.nnoticePeriodDays },
                { "cempcategory", u.cempcategory },
                { "cworkloccode", u.cworkloccode },
                { "cworklocname", u.cworklocname },
                { "cgradecode", u.cgradecode },
                { "cgradedesc", u.cgradedesc },
                { "csubrolecode", u.csubrolecode },
                { "cdeptcode", u.cdeptcode },
                { "cdeptdesc", u.cdeptdesc },
                { "cjobcode", u.cjobcode },
                { "cjobdesc", u.cjobdesc },
                { "creportmgrcode", u.creportmgrcode },
                { "creportmgrname", u.creportmgrname },
                { "croll_name", u.croll_name },
                { "croll_id_mngr", u.croll_id_mngr },
                { "croll_id_mngr_desc" , u.croll_id_mngr_desc },
                { "cReportManager_empcode", u.cReportManager_empcode },
                { "cReportManager_Poscode", u.cReportManager_Poscode },
                { "cReportManager_Posdesc", u.cReportManager_Posdesc }
            }
                    .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                    .Select(f => f.Key)
                    .ToList()
                }).ToList();

                object response;

                if (databaseFailedUsers.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_users_received = users.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedUsers,
                            note = "Database insertion failed due to constraint violations. Some users may already exist."
                        },
                        error = "Check unique constraints (cuserid, cusername, cemail, cphoneno)"
                    };
                }
                else if (insertedCount == validUsers.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk user creation completed successfully",
                        body = new
                        {
                            total = users.Count,
                            success = insertedCount,
                            failure = users.Count - insertedCount,
                            inserted = insertedUsersWithDetails,
                            failed = failedUsers.Any() ? failedUsers : new List<object> { new { message = "No validation failures" } },
                            note = "All users passed JSON validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = users.Count,
                            success = insertedCount,
                            failure = users.Count - insertedCount,
                            inserted = insertedUsersWithDetails,
                            failed = failedUsers,
                            note = "Some users were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreateUserApi")]
        public async Task<IActionResult> CreateUserApi([FromBody] pay request)
        {
            try
            {
                if(request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var users = JsonConvert.DeserializeObject<List<UserApiDTO>>(decryptedJson);

                if (users == null || !users.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No users provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupUserIds = users.GroupBy(u => u.cuserid).Where(g => g.Count() > 1 && g.Key > 0).Select(g => g.Key).ToList();
                var dupUsernames = users.GroupBy(u => u.cusername).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();
                var dupEmails = users.GroupBy(u => u.cemail).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();
                var dupPhones = users.GroupBy(u => u.cphoneno).Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key)).Select(g => g.Key).ToList();

                if (dupUserIds.Any() || dupUsernames.Any() || dupEmails.Any() || dupPhones.Any())
                {
                    if (dupUserIds.Any()) duplicateErrors.Add($"Duplicate cuserid(s): {string.Join(", ", dupUserIds)}");
                    if (dupUsernames.Any()) duplicateErrors.Add($"Duplicate cusername(s): {string.Join(", ", dupUsernames)}");
                    if (dupEmails.Any()) duplicateErrors.Add($"Duplicate cemail(s): {string.Join(", ", dupEmails)}");
                    if (dupPhones.Any()) duplicateErrors.Add($"Duplicate cphoneno(s): {string.Join(", ", dupPhones)}");

                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for one or more mandatory fields. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedUsers = new List<object>();
                var validUsers = new List<UserApiDTO>();
                bool hasValidationErrors = false;

                foreach (var user in users)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (user.cuserid <= 0)
                    {
                        errors.Add("cuserid: Must be positive number");
                        nullMandatoryFields.Add("cuserid");
                    }
                    if (string.IsNullOrEmpty(user.cusername))
                    {
                        errors.Add("cusername: Field is required");
                        nullMandatoryFields.Add("cusername");
                    }
                    if (string.IsNullOrEmpty(user.cemail))
                    {
                        errors.Add("cemail: Field is required");
                        nullMandatoryFields.Add("cemail");
                    }
                    if (string.IsNullOrEmpty(user.cpassword))
                    {
                        errors.Add("cpassword: Field is required");
                        nullMandatoryFields.Add("cpassword");
                    }
                    if (string.IsNullOrEmpty(user.cphoneno))
                    {
                        errors.Add("cphoneno: Field is required");
                        nullMandatoryFields.Add("cphoneno");
                    }

                    var optionalFields = new Dictionary<string, object?>
            {
                { "cfirstName", user.cfirstName },
                { "clastName", user.clastName },
                { "cAlternatePhone", user.cAlternatePhone },
                { "ldob", user.ldob },
                { "cMaritalStatus", user.cMaritalStatus },
                { "cnation", user.cnation },
                { "cgender", user.cgender },
                { "caddress", user.caddress },
                { "caddress1", user.caddress1 },
                { "caddress2", user.caddress2 },
                { "cpincode", user.cpincode },
                { "ccity", user.ccity },
                { "cstatecode", user.cstatecode },
                { "cstatedesc", user.cstatedesc },
                { "ccountrycode", user.ccountrycode },
                { "cbankName", user.cbankName },
                { "caccountNumber", user.caccountNumber },
                { "ciFSC_code", user.ciFSC_code },
                { "cpAN", user.cpAN },
                { "ldoj", user.ldoj },
                { "cemploymentStatus", user.cemploymentStatus },
                { "nnoticePeriodDays", user.nnoticePeriodDays },
                { "cempcategory", user.cempcategory },
                { "cworkloccode", user.cworkloccode },
                { "cworklocname", user.cworklocname },
                { "cgradecode", user.cgradecode },
                { "cgradedesc", user.cgradedesc },
                { "csubrolecode", user.csubrolecode },
                { "cdeptcode", user.cdeptcode },
                { "cdeptdesc", user.cdeptdesc },
                { "cjobcode", user.cjobcode },
                { "cjobdesc", user.cjobdesc },
                { "creportmgrcode", user.creportmgrcode },
                { "creportmgrname", user.creportmgrname },
                { "croll_id", user.croll_id },
                { "croll_name", user.croll_name },
                { "croll_id_mngr", user.croll_id_mngr },
                { "croll_id_mngr_desc" , user.croll_id_mngr_desc },
                { "cReportManager_empcode", user.cReportManager_empcode },
                { "cReportManager_Poscode", user.cReportManager_Poscode },
                { "cReportManager_Posdesc", user.cReportManager_Posdesc }
            };

                    var missingOptionalFields = optionalFields
                        .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                        .Select(f => f.Key)
                        .ToList();

                    if (!string.IsNullOrEmpty(user.cemail))
                    {
                        try
                        {
                            var addr = new System.Net.Mail.MailAddress(user.cemail);
                            if (addr.Address != user.cemail)
                                errors.Add("cemail: Invalid email format");
                        }
                        catch
                        {
                            errors.Add("cemail: Invalid email format");
                        }
                    }

                    if (!string.IsNullOrEmpty(user.cphoneno) && !System.Text.RegularExpressions.Regex.IsMatch(user.cphoneno, @"^[0-9]{10}$"))
                        errors.Add("cphoneno: Invalid phone number format (must be 10 digits)");

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedUsers.Add(new
                        {
                            cemail = user.cemail ?? "NULL",
                            cuserid = user.cuserid,
                            cphoneno = user.cphoneno ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" },
                            missing_optional_fields = missingOptionalFields.Any() ? missingOptionalFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validUsers.Add(user);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_users_received = users.Count,
                            total_valid_users = validUsers.Count,
                            total_failed_users = failedUsers.Count,
                            failed_users = failedUsers,
                            valid_users = validUsers.Select(u => new
                            {
                                u.cemail,
                                u.cuserid,
                                u.cphoneno,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<UserApiDTO> successfullyInsertedUsers = new List<UserApiDTO>();
                List<object> databaseFailedUsers = new List<object>();

                if (validUsers.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertUserApiAsync(validUsers, cTenantID, usernameClaim);

                        successfullyInsertedUsers = validUsers;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedUsers.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some users may already exist in the database. Check unique constraints (cuserid, cusername, cemail, cphoneno)."
                        });

                        insertedCount = 0;
                        successfullyInsertedUsers = new List<UserApiDTO>();
                    }
                }

                var insertedUsersWithDetails = successfullyInsertedUsers.Select(u => new
                {
                    u.cemail,
                    u.cuserid,
                    u.cphoneno,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" },
                    missing_optional_fields = new Dictionary<string, object?>
            {
                { "cfirstName", u.cfirstName },
                { "clastName", u.clastName },
                { "cAlternatePhone", u.cAlternatePhone },
                { "ldob", u.ldob },
                { "cMaritalStatus", u.cMaritalStatus },
                { "cnation", u.cnation },
                { "cgender", u.cgender },
                { "caddress", u.caddress },
                { "caddress1", u.caddress1 },
                { "caddress2", u.caddress2 },
                { "cpincode", u.cpincode },
                { "ccity", u.ccity },
                { "cstatecode", u.cstatecode },
                { "cstatedesc", u.cstatedesc },
                { "ccountrycode", u.ccountrycode },
                { "cbankName", u.cbankName },
                { "caccountNumber", u.caccountNumber },
                { "ciFSC_code", u.ciFSC_code },
                { "cpAN" , u.cpAN },
                { "ldoj", u.ldoj },
                { "cemploymentStatus", u.cemploymentStatus },
                { "nnoticePeriodDays", u.nnoticePeriodDays },
                { "cempcategory", u.cempcategory },
                { "cworkloccode", u.cworkloccode },
                { "cworklocname", u.cworklocname },
                { "cgradecode", u.cgradecode },
                { "cgradedesc", u.cgradedesc },
                { "csubrolecode", u.csubrolecode },
                { "cdeptcode", u.cdeptcode },
                { "cdeptdesc", u.cdeptdesc },
                { "cjobcode", u.cjobcode },
                { "cjobdesc", u.cjobdesc },
                { "creportmgrcode", u.creportmgrcode },
                { "creportmgrname", u.creportmgrname },
                { "croll_name", u.croll_name },
                { "croll_id_mngr", u.croll_id_mngr },
                { "croll_id_mngr_desc" , u.croll_id_mngr_desc },
                { "cReportManager_empcode", u.cReportManager_empcode },
                { "cReportManager_Poscode", u.cReportManager_Poscode },
                { "cReportManager_Posdesc", u.cReportManager_Posdesc }
            }
                    .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                    .Select(f => f.Key)
                    .ToList()
                }).ToList();

                object response;

                if (databaseFailedUsers.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_users_received = users.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedUsers,
                            note = "Database insertion failed due to constraint violations. Some users may already exist."
                        },
                        error = "Check unique constraints (cuserid, cusername, cemail, cphoneno)"
                    };
                }
                else if (insertedCount == validUsers.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk user creation completed successfully",
                        body = new
                        {
                            total = users.Count,
                            success = insertedCount,
                            failure = users.Count - insertedCount,
                            inserted = insertedUsersWithDetails,
                            failed = failedUsers.Any() ? failedUsers : new List<object> { new { message = "No validation failures" } },
                            note = "All users passed JSON validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = users.Count,
                            success = insertedCount,
                            failure = users.Count - insertedCount,
                            inserted = insertedUsersWithDetails,
                            failed = failedUsers,
                            note = "Some users were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost]
        [Route("usersapisyncconfig")]
        public async Task<IActionResult> usersapisyncconfig([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if(string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    var errorResponse = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    var errorResponse = new APIResponse
                    {
                        status = 400,
                        statusText = "Request payload is required"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    var errorResponse = new APIResponse
                    {
                        status = 400,
                        statusText = "Decrypted payload is empty or invalid"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var model = JsonConvert.DeserializeObject<usersapisyncDTO>(decryptedJson);

                if (model == null)
                {
                    var errorResponse = new APIResponse
                    {
                        status = 400,
                        statusText = "Invalid JSON format for usersapisyncDTO"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                bool success = await _AccountService.InsertusersapisyncconfigAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 400,
                    statusText = success ? "API sync config created successfully" : "Failed to create API sync config"

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
                    statusText = $"Internal server error: {ex.Message}"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreateDepartmentsBulk")]
        public async Task<IActionResult> CreateDepartmentsBulk([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                var invalidFields = new List<string>();
                try
                {
                    var validFields = new HashSet<string>
            {
                "cdepartment_code",
                "cdepartment_name",
                "cdepartment_desc",
                "cdepartmentslug",
                "cdepartment_manager_rolecode",
                "cdepartment_manager_position_code",
                "cdepartment_manager_name",
                "cdepartment_email",
                "cdepartment_phone",
                "nis_active",
                 "ID",
                "ctenant_id",
                "cdepartmentslug",
                "nis_deleted",
                "cdeleted_by",
                "ldeleted_date",
                "ccreated_by",
                "lcreated_date",
                "cmodified_by",
                "lmodified_date"
            };

                    var jArray = JArray.Parse(decryptedJson);

                    foreach (var item in jArray)
                    {
                        if (item is JObject jObject)
                        {
                            foreach (var property in jObject.Properties())
                            {
                                if (!validFields.Contains(property.Name))
                                {
                                    invalidFields.Add(property.Name);
                                }
                            }
                        }
                    }
                }
                catch (JsonException ex)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Invalid JSON format",
                        message = $"Invalid JSON format: {ex.Message}",
                        note = "Please check your JSON syntax."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                if (invalidFields.Any())
                {
                    var distinctInvalidFields = invalidFields.Distinct().ToList();
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Invalid JSON fields",
                        message = $"Invalid field(s) found in JSON: {string.Join(", ", distinctInvalidFields)}",
                        note = "Please check the field names in your JSON payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var departments = JsonConvert.DeserializeObject<List<BulkDepartmentDTO>>(decryptedJson);

                if (departments == null || !departments.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No departments provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupDepartmentCodes = departments.GroupBy(d => d.cdepartment_code)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .Select(g => g.Key)
                    .ToList();

                if (dupDepartmentCodes.Any())
                {
                    duplicateErrors.Add($"Duplicate cdepartment_code(s): {string.Join(", ", dupDepartmentCodes)}");
                }

                if (duplicateErrors.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for department code. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedDepartments = new List<object>();
                var validDepartments = new List<BulkDepartmentDTO>();
                bool hasValidationErrors = false;

                foreach (var dept in departments)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (string.IsNullOrEmpty(dept.cdepartment_code))
                    {
                        errors.Add("cdepartment_code: Field is required");
                        nullMandatoryFields.Add("cdepartment_code");
                    }
                    else if (dept.cdepartment_code.Length > 50)
                    {
                        errors.Add("cdepartment_code: Maximum length is 50 characters");
                    }

                    var optionalFields = new Dictionary<string, object?>
            {
                { "cdepartment_name", dept.cdepartment_name },
                { "cdepartment_desc", dept.cdepartment_desc },
                { "cdepartment_email", dept.cdepartment_email },
                { "cdepartment_manager_rolecode", dept.cdepartment_manager_rolecode },
                { "cdepartment_manager_position_code", dept.cdepartment_manager_position_code },
                { "cdepartment_manager_name", dept.cdepartment_manager_name },
                { "cdepartment_phone", dept.cdepartment_phone },
                { "nis_active", dept.nis_active }
            };

                    var missingOptionalFields = optionalFields
                        .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                        .Select(f => f.Key)
                        .ToList();

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedDepartments.Add(new
                        {
                            cdepartment_code = dept.cdepartment_code ?? "NULL",
                            cdepartment_name = dept.cdepartment_name ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" },
                            missing_optional_fields = missingOptionalFields.Any() ? missingOptionalFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validDepartments.Add(dept);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_departments_received = departments.Count,
                            total_valid_departments = validDepartments.Count,
                            total_failed_departments = failedDepartments.Count,
                            failed_departments = failedDepartments,
                            valid_departments = validDepartments.Select(d => new
                            {
                                d.cdepartment_code,
                                d.cdepartment_name,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var existingDepartmentCodes = await _AccountService.CheckExistingDepartmentCodesAsync(
                    validDepartments.Select(d => d.cdepartment_code).ToList(),
                    cTenantID
                );

                if (existingDepartmentCodes.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in database.",
                        body = new
                        {
                            validation_type = "DATABASE_DUPLICATE_CHECK",
                            message = $"Department codes already exist in database: {string.Join(", ", existingDepartmentCodes)}",
                            note = "Remove duplicate department codes before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<BulkDepartmentDTO> successfullyInsertedDepartments = new List<BulkDepartmentDTO>();
                List<object> databaseFailedDepartments = new List<object>();

                if (validDepartments.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertDepartmentsBulkAsync(validDepartments, cTenantID, usernameClaim);
                        successfullyInsertedDepartments = validDepartments;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedDepartments.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some departments may already exist in the database. Check unique constraints (cdepartment_code)."
                        });

                        insertedCount = 0;
                        successfullyInsertedDepartments = new List<BulkDepartmentDTO>();
                    }
                }

                var insertedDepartmentsWithDetails = successfullyInsertedDepartments.Select(d => new
                {
                    d.cdepartment_code,
                    d.cdepartment_name,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" },
                    missing_optional_fields = new Dictionary<string, object?>
            {
                { "cdepartment_desc", d.cdepartment_desc },
                { "cdepartment_email", d.cdepartment_email },
                { "cdepartment_manager_rolecode", d.cdepartment_manager_rolecode },
                { "cdepartment_manager_position_code", d.cdepartment_manager_position_code },
                { "cdepartment_manager_name", d.cdepartment_manager_name },
                { "cdepartment_phone", d.cdepartment_phone },
                { "nis_active", d.nis_active }
            }
                    .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                    .Select(f => f.Key)
                    .ToList()
                }).ToList();

                object response;

                if (databaseFailedDepartments.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_departments_received = departments.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedDepartments,
                            note = "Database insertion failed due to constraint violations. Some departments may already exist."
                        },
                        error = "Check unique constraints (cdepartment_code)"
                    };
                }
                else if (insertedCount == validDepartments.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk department creation completed successfully",
                        body = new
                        {
                            total = departments.Count,
                            success = insertedCount,
                            failure = departments.Count - insertedCount,
                            inserted = insertedDepartmentsWithDetails,
                            failed = failedDepartments.Any() ? failedDepartments : new List<object> { new { message = "No validation failures" } },
                            note = "All departments passed validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = departments.Count,
                            success = insertedCount,
                            failure = departments.Count - insertedCount,
                            inserted = insertedDepartmentsWithDetails,
                            failed = failedDepartments,
                            note = "Some departments were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }


        [Authorize]
        [HttpPost("CreateRolesBulk")]
        public async Task<IActionResult> CreateRolesBulk([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if(string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                var invalidfields = new List<string>();
                try
                {
                    var validFields = new HashSet<string>
            {
                "crole_code",
                "crole_name",
                "crole_level",
                "cdepartment_code",
                "creporting_manager_code",
                "creporting_manager_name",
                "crole_description",
                 "ID","cslug","nis_active",
                "ctenant_id",
                "nis_deleted",
                "cdeleted_by",
                "ldeleted_date",
                "ccreated_by",
                "lcreated_date",
                "cmodified_by",
                "lmodified_date"
            };
                    var jArray = JArray.Parse(decryptedJson);
                    foreach (var item in jArray)
                    {
                        var jObject = (JObject)item;
                        foreach (var prop in jObject.Properties())
                        {
                            if (!validFields.Contains(prop.Name))
                            {
                                invalidfields.Add(prop.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Invalid JSON format.",
                        body = new
                        {
                            validation_type = "JSON_FORMAT_VALIDATION",
                            message = ex.Message,
                            note = "Ensure the JSON payload is properly formatted."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                if (invalidfields.Any())
                {
                    var distinctInvalidFields = invalidfields.Distinct().ToList();
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Invalid JSON structure.",
                        message = $"Invalid field(s) found in JSON: {string.Join(", ", distinctInvalidFields)}",
                        note = "Please check the field names in your JSON payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var roles = JsonConvert.DeserializeObject<List<BulkRoleDTO>>(decryptedJson);

                if (roles == null || !roles.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No roles provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupRoleCodes = roles.GroupBy(r => r.crole_code)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .Select(g => g.Key)
                    .ToList();

                if (dupRoleCodes.Any())
                {
                    duplicateErrors.Add($"Duplicate crole_code(s): {string.Join(", ", dupRoleCodes)}");
                }

                if (duplicateErrors.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for role code. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedRoles = new List<object>();
                var validRoles = new List<BulkRoleDTO>();
                bool hasValidationErrors = false;

                foreach (var role in roles)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (string.IsNullOrEmpty(role.crole_code))
                    {
                        errors.Add("crole_code: Field is required");
                        nullMandatoryFields.Add("crole_code");
                    }
                    else if (role.crole_code.Length > 50)
                    {
                        errors.Add("crole_code: Maximum length is 50 characters");
                    }

                    var optionalFields = new Dictionary<string, object?>
            {
                { "crole_name", role.crole_name },
                { "crole_level", role.crole_level },
                { "cdepartment_code", role.cdepartment_code },
                { "creporting_manager_code", role.creporting_manager_code },
                { "creporting_manager_name", role.creporting_manager_name },
                { "crole_description", role.crole_description },
                { "nis_active", role.nis_active   }
            };

                    var missingOptionalFields = optionalFields
                        .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                        .Select(f => f.Key)
                        .ToList();

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedRoles.Add(new
                        {
                            crole_code = role.crole_code ?? "NULL",
                            crole_name = role.crole_name ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" },
                            missing_optional_fields = missingOptionalFields.Any() ? missingOptionalFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validRoles.Add(role);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_roles_received = roles.Count,
                            total_valid_roles = validRoles.Count,
                            total_failed_roles = failedRoles.Count,
                            failed_roles = failedRoles,
                            valid_roles = validRoles.Select(r => new
                            {
                                r.crole_code,
                                r.crole_name,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var existingRoleCodes = await _AccountService.CheckExistingRoleCodesAsync(
                    validRoles.Select(r => r.crole_code).ToList(),
                    cTenantID
                );

                if (existingRoleCodes.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in database.",
                        body = new
                        {
                            validation_type = "DATABASE_DUPLICATE_CHECK",
                            message = $"Role codes already exist in database: {string.Join(", ", existingRoleCodes)}",
                            note = "Remove duplicate role codes before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<BulkRoleDTO> successfullyInsertedRoles = new List<BulkRoleDTO>();
                List<object> databaseFailedRoles = new List<object>();

                if (validRoles.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertRolesBulkAsync(validRoles, cTenantID, usernameClaim);
                        successfullyInsertedRoles = validRoles;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedRoles.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some roles may already exist in the database. Check unique constraints (crole_code)."
                        });

                        insertedCount = 0;
                        successfullyInsertedRoles = new List<BulkRoleDTO>();
                    }
                }

                var insertedRolesWithDetails = successfullyInsertedRoles.Select(r => new
                {
                    r.crole_code,
                    r.crole_name,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" },
                    missing_optional_fields = new Dictionary<string, object?>
            {
                { "crole_level", r.crole_level },
                { "cdepartment_code", r.cdepartment_code },
                { "creporting_manager_code", r.creporting_manager_code },
                { "creporting_manager_name", r.creporting_manager_name },
                { "crole_description", r.crole_description },
                { "nis_active", r.nis_active    }
            }
                    .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                    .Select(f => f.Key)
                    .ToList()
                }).ToList();

                object response;

                if (databaseFailedRoles.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_roles_received = roles.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedRoles,
                            note = "Database insertion failed due to constraint violations. Some roles may already exist."
                        },
                        error = "Check unique constraints (crole_code)"
                    };
                }
                else if (insertedCount == validRoles.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk role creation completed successfully",
                        body = new
                        {
                            total = roles.Count,
                            success = insertedCount,
                            failure = roles.Count - insertedCount,
                            inserted = insertedRolesWithDetails,
                            failed = failedRoles.Any() ? failedRoles : new List<object> { new { message = "No validation failures" } },
                            note = "All roles passed validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = roles.Count,
                            success = insertedCount,
                            failure = roles.Count - insertedCount,
                            inserted = insertedRolesWithDetails,
                            failed = failedRoles,
                            note = "Some roles were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreatePositionsBulk")]
        public async Task<IActionResult> CreatePositionsBulk([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if(string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                var invalidfields = new List<string>();
                try
                {
                    var validFields = new HashSet<string>
            {
                "cposition_code",
                "cposition_name",
                "cposition_decsription",
                "cdepartment_code",
                "creporting_manager_positionid",
                "creporting_manager_name",
                "nis_active",
                 "ID",
                "ctenant_id",
                "cposition_slug",
                "nis_deleted",
                "cdeleted_by",
                "ldeleted_date",
                "ccreated_by",
                "lcreated_date",
                "cmodified_by",
                "lmodified_date"
            };
                    var jArray = JArray.Parse(decryptedJson);
                    foreach (var item in jArray)
                    {
                        var jObject = (JObject)item;
                        foreach (var prop in jObject.Properties())
                        {
                            if (!validFields.Contains(prop.Name))
                            {
                                invalidfields.Add(prop.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Invalid JSON format.",
                        body = new
                        {
                            validation_type = "JSON_FORMAT_VALIDATION",
                            message = ex.Message,
                            note = "Ensure the JSON payload is properly formatted."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                if (invalidfields.Any())
                {
                    var distinctInvalidFields = invalidfields.Distinct().ToList();
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Invalid JSON structure.",
                        message = $"Invalid field(s) found in JSON: {string.Join(", ", distinctInvalidFields)}",
                        note = "Please check the field names in your JSON payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var positions = JsonConvert.DeserializeObject<List<BulkPositionDTO>>(decryptedJson);

                if (positions == null || !positions.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No positions provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupPositionCodes = positions.GroupBy(p => p.cposition_code)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .Select(g => g.Key)
                    .ToList();

                if (dupPositionCodes.Any())
                {
                    duplicateErrors.Add($"Duplicate cposition_code(s): {string.Join(", ", dupPositionCodes)}");
                }

                if (duplicateErrors.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for position code. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedPositions = new List<object>();
                var validPositions = new List<BulkPositionDTO>();
                bool hasValidationErrors = false;

                foreach (var position in positions)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (string.IsNullOrEmpty(position.cposition_code))
                    {
                        errors.Add("cposition_code: Field is required");
                        nullMandatoryFields.Add("cposition_code");
                    }
                    else if (position.cposition_code.Length > 50)
                    {
                        errors.Add("cposition_code: Maximum length is 50 characters");
                    }

                    var optionalFields = new Dictionary<string, object?>
            {
                { "cposition_name", position.cposition_name },
                { "cposition_decsription", position.cposition_decsription },
                { "cdepartment_code", position.cdepartment_code },
                { "creporting_manager_positionid", position.creporting_manager_positionid },
                { "creporting_manager_name", position.creporting_manager_name },
                { "nis_active", position.nis_active }
            };

                    var missingOptionalFields = optionalFields
                        .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                        .Select(f => f.Key)
                        .ToList();

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedPositions.Add(new
                        {
                            cposition_code = position.cposition_code ?? "NULL",
                            cposition_name = position.cposition_name ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" },
                            missing_optional_fields = missingOptionalFields.Any() ? missingOptionalFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validPositions.Add(position);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_positions_received = positions.Count,
                            total_valid_positions = validPositions.Count,
                            total_failed_positions = failedPositions.Count,
                            failed_positions = failedPositions,
                            valid_positions = validPositions.Select(p => new
                            {
                                p.cposition_code,
                                p.cposition_name,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var existingPositionCodes = await _AccountService.CheckExistingPositionCodesAsync(
                    validPositions.Select(p => p.cposition_code).ToList(),
                    cTenantID
                );

                if (existingPositionCodes.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in database.",
                        body = new
                        {
                            validation_type = "DATABASE_DUPLICATE_CHECK",
                            message = $"Position codes already exist in database: {string.Join(", ", existingPositionCodes)}",
                            note = "Remove duplicate position codes before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<BulkPositionDTO> successfullyInsertedPositions = new List<BulkPositionDTO>();
                List<object> databaseFailedPositions = new List<object>();

                if (validPositions.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertPositionsBulkAsync(validPositions, cTenantID, usernameClaim);
                        successfullyInsertedPositions = validPositions;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedPositions.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some positions may already exist in the database. Check unique constraints (cposition_code)."
                        });

                        insertedCount = 0;
                        successfullyInsertedPositions = new List<BulkPositionDTO>();
                    }
                }

                var insertedPositionsWithDetails = successfullyInsertedPositions.Select(p => new
                {
                    p.cposition_code,
                    p.cposition_name,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" },
                    missing_optional_fields = new Dictionary<string, object?>
            {
                { "cposition_decsription", p.cposition_decsription },
                { "cdepartment_code", p.cdepartment_code },
                { "creporting_manager_positionid", p.creporting_manager_positionid },
                { "creporting_manager_name", p.creporting_manager_name },
                        { "nis_active", p.nis_active }
            }
                    .Where(f => f.Value == null || (f.Value is string str && string.IsNullOrWhiteSpace(str)))
                    .Select(f => f.Key)
                    .ToList()
                }).ToList();

                object response;

                if (databaseFailedPositions.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_positions_received = positions.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedPositions,
                            note = "Database insertion failed due to constraint violations. Some positions may already exist."
                        },
                        error = "Check unique constraints (cposition_code)"
                    };
                }
                else if (insertedCount == validPositions.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk position creation completed successfully",
                        body = new
                        {
                            total = positions.Count,
                            success = insertedCount,
                            failure = positions.Count - insertedCount,
                            inserted = insertedPositionsWithDetails,
                            failed = failedPositions.Any() ? failedPositions : new List<object> { new { message = "No validation failures" } },
                            note = "All positions passed validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = positions.Count,
                            success = insertedCount,
                            failure = positions.Count - insertedCount,
                            inserted = insertedPositionsWithDetails,
                            failed = failedPositions,
                            note = "Some positions were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpGet]
        [Route("GetAllUsersApiSyncConfig")]
        public async Task<ActionResult> GetAllUsersApiSyncConfig([FromQuery] string searchText= null,[FromQuery] string syncType = null,[FromQuery] string apiMethod = null,[FromQuery] bool? isActive = null)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"]
                                    .FirstOrDefault()?
                                    .Split(" ")
                                    .Last();

                //if (jwtToken == null)
                //    return EncryptedError(401, "Authorization token missing");
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }

                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims
                    .SingleOrDefault(c => c.Type == "cTenantID")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim)
                    || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }
                if (string.IsNullOrWhiteSpace(tenantIdClaim))
                {
                    return EncryptedError(400, "cTenantID claim is missing in token");
                }

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    if (searchText.Length > 200)
                    {
                        return EncryptedError(400, "Search text cannot exceed 200 characters");
                    }

                    var sqlInjectionPatterns = new[] { "--", ";", "'", "/*", "*/", "@@", "char(", "nchar(", "varchar(", "nvarchar(" };
                    foreach (var pattern in sqlInjectionPatterns)
                    {
                        if (searchText.Contains(pattern, StringComparison.OrdinalIgnoreCase))
                        {
                            return EncryptedError(400, "Search text contains invalid characters");
                        }
                    }
                }

                if (!string.IsNullOrWhiteSpace(syncType))
                {
                    var validSyncTypes = new[] { "Full", "Incremental", "Manual", "Scheduled", "RealTime" };

                    if (!validSyncTypes.Contains(syncType, StringComparer.OrdinalIgnoreCase))
                    {
                        return EncryptedError(400, $"Invalid syncType. Must be one of: {string.Join(", ", validSyncTypes)}");
                    }

                    if (syncType.Length > 50)
                    {
                        return EncryptedError(400, "syncType cannot exceed 50 characters");
                    }
                }

                if (!string.IsNullOrWhiteSpace(apiMethod))
                {
                    var validApiMethods = new[] { "GET", "POST", "PUT", "PATCH", "DELETE" };

                    if (!validApiMethods.Contains(apiMethod.ToUpper()))
                    {
                        return EncryptedError(400, $"Invalid apiMethod. Must be one of: {string.Join(", ", validApiMethods)}");
                    }

                    if (apiMethod.Length > 10)
                    {
                        return EncryptedError(400, "apiMethod cannot exceed 10 characters");
                    }
                }
                var apiConfigs = await _AccountService.GetAllAPISyncConfigAsync(cTenantID,searchText,syncType,apiMethod,isActive);

                var processed = apiConfigs.Select(config =>
                {
                    string extractedSyncType = null;

                    if (!string.IsNullOrWhiteSpace(config.capi_settings))
                    {
                        try
                        {
                            var settings = JsonConvert.DeserializeObject<dynamic>(config.capi_settings);
                            extractedSyncType = settings?.syncType?.ToString();
                        }
                        catch { }
                    }

                    return new
                    {
                        id = config.ID,
                        ctenant_id = config.ctenant_id,
                        capi_method = config.capi_method,
                        capi_type = config.capi_type,
                        capi_url = config.capi_url,
                        cname = config.cname,
                        sync_type = extractedSyncType,
                        nis_active = config.nis_active,
                        ccreated_by = config.ccreated_by,
                        lcreated_date = config.lcreated_date,
                        cmodified_by = config.cmodified_by,
                        lmodified_date = config.lmodified_date
                    };
                }).Where(x=>x!=null).ToList();

                var response = new APIResponse
                {
                    body = processed.ToArray(),
                    statusText = processed.Any() ? "Successful": "No API sync configurations found",
                    status = processed.Any() ? 200 :204
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
                    statusText = $"Error: {ex.Message}",
                    status = 500
                };

                string json = JsonConvert.SerializeObject(errorResponse);
                var encrypted = AesEncryption.Encrypt(json);

                return StatusCode(500, encrypted);
            }
        }


        [Authorize]
        [HttpGet("GetAPISyncConfigByID")]
        public async Task<IActionResult> GetAPISyncConfigByID(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return EncryptedError(400, "ID must be greater than 0.");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if(string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return EncryptedError(401, "Invalid or missing cTenantID in token.");
                }

                if (id <= 0)
                {
                    return EncryptedError(400, "ID must be greater than 0.");
                }

                var apiConfig = await _AccountService.GetAPISyncConfigByIDAsync(id, cTenantID);

                if (apiConfig == null)
                {
                    return EncryptedError(404, "API sync configuration not found.");
                }
                Func<string, dynamic> safeDeserialize = (jsonString) =>
                {
                    if (string.IsNullOrWhiteSpace(jsonString))
                        return null;
                    try
                    {
                        // Deserialize the string into a dynamic object (JObject, Dictionary, etc.)
                        return JsonConvert.DeserializeObject<dynamic>(jsonString);
                    }
                    catch
                    {
                        // Return null or the original string if deserialization fails
                        return jsonString;
                    }
                };
                var response = new APIResponse
                {
                    data = new
                    {
                        id = apiConfig.ID,
                        ctenant_id = apiConfig.ctenant_id,
                        capi_method = apiConfig.capi_method,
                        capi_type = apiConfig.capi_type,
                        capi_url = apiConfig.capi_url,
                        capi_params = safeDeserialize(apiConfig.capi_params),
                        capi_headers = safeDeserialize(apiConfig.capi_headers),
                        capi_config = safeDeserialize(apiConfig.capi_config),
                        capi_settings = safeDeserialize(apiConfig.capi_settings),
                        cbody = safeDeserialize(apiConfig.cbody),
                        cname = apiConfig.cname,
                        nis_active = apiConfig.nis_active,
                        ccreated_by = apiConfig.ccreated_by,
                        lcreated_date = apiConfig.lcreated_date,
                        cmodified_by = apiConfig.cmodified_by,
                        lmodified_date = apiConfig.lmodified_date

                    },
                    statusText = "Successful",
                    status = 200
                };

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(200, encrypted);
            }
            catch (Exception ex)
            {
                return EncryptedError(500, $"Internal server error: {ex.Message}");
            }
        }


        [Authorize]
        [HttpDelete("DeleteAPISyncConfig")]
        public async Task<IActionResult> DeleteAPISyncConfig([FromQuery] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Request payload is required"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                     string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var error = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, $"\"{encryptedError}\"");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Decrypted payload is empty or invalid"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                DeleteAPISyncConfigDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<DeleteAPISyncConfigDTO>(decryptedJson);
                }
                catch (JsonException jsonEx)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = $"Invalid JSON format: {jsonEx.Message}"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                if (model == null)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Failed to deserialize JSON payload"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                if (model.ID <= 0)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "ID field is required and must be greater than 0"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                bool success = await _AccountService.DeleteAPISyncConfigAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "API sync config deleted successfully" : "API sync config not found",
                    body = new object[] { new { ConfigID = model.ID } }
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, $"\"{encrypted}\"");
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Internal server error: {ex.Message}"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, $"\"{encryptedError}\"");
            }
        }

        [Authorize]
        [HttpPut("UpdateAPISyncConfig")]
        public async Task<IActionResult> UpdateAPISyncConfig([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Request payload is required"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if(string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var error = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, $"\"{encryptedError}\"");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Decrypted payload is empty or invalid"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                UpdateAPISyncConfigDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<UpdateAPISyncConfigDTO>(decryptedJson);
                }
                catch (JsonException jsonEx)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = $"Invalid JSON format: {jsonEx.Message}"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                if (model == null)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Failed to deserialize JSON payload"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }
                if (model.ID <= 0)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "ID field is required and must be greater than 0"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                bool success = await _AccountService.UpdateAPISyncConfigAsync(model, cTenantID, usernameClaim);

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ? "API sync config updated successfully" : "API sync config not found",
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, $"\"{encrypted}\"");
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Internal server error: {ex.Message}"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, $"\"{encryptedError}\"");
            }
        }

        [Authorize]
        [HttpPut("UpdateAPISyncConfigActiveStatus")]
        public async Task<IActionResult> UpdateAPISyncConfigActiveStatus([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Request payload is required"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if(string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) ||
                    string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var error = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, $"\"{encryptedError}\"");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                if (string.IsNullOrWhiteSpace(decryptedJson))
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Decrypted payload is empty or invalid"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                UpdateAPISyncConfigActiveStatusAsyncDTO model;
                try
                {
                    model = JsonConvert.DeserializeObject<UpdateAPISyncConfigActiveStatusAsyncDTO>(decryptedJson);
                }
                catch (JsonException jsonEx)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = $"Invalid JSON format: {jsonEx.Message}"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                if (model == null)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "Failed to deserialize JSON payload"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }

                if (model.ID <= 0)
                {
                    var error = new APIResponse
                    {
                        status = 400,
                        statusText = "ID field is required and must be greater than 0"
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(400, $"\"{encryptedError}\"");
                }
                
                bool success = await _AccountService.UpdateAPISyncConfigActiveStatusAsync(
                    model.ID,
                    model.nis_active,
                    cTenantID,
                    usernameClaim
                );

                var response = new APIResponse
                {
                    status = success ? 200 : 404,
                    statusText = success ?
                        $"API sync config status updated to {(model.nis_active ? "Active" : "Inactive")} successfully" :
                        "API sync config not found or update failed",
                    body = new object[] { new {
                //ID = model.ID,
                //nis_active = model.nis_active,
                Status = model.nis_active ? "Active" : "Inactive"
            } }
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return StatusCode(response.status, $"\"{encrypted}\"");
            }
            catch (Exception ex)
            {
                var errorResponse = new APIResponse
                {
                    status = 500,
                    statusText = $"Internal server error: {ex.Message}"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                string encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, $"\"{encryptedError}\"");
            }
        }

        [Authorize]
        [HttpPost("CreateDepartmentsApi")]
        public async Task<IActionResult> CreateDepartmentsApi([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                List<DepartmentDTO> departments;
                try
                {
                    departments = JsonConvert.DeserializeObject<List<DepartmentDTO>>(decryptedJson);
                }
                catch (JsonException)
                {
                    var singleDept = JsonConvert.DeserializeObject<DepartmentDTO>(decryptedJson);
                    departments = new List<DepartmentDTO> { singleDept };
                }

                if (departments == null || !departments.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No departments provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupDepartmentCodes = departments.GroupBy(d => d.cdepartment_code)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .Select(g => g.Key)
                    .ToList();

                if (dupDepartmentCodes.Any())
                {
                    duplicateErrors.Add($"Duplicate cdepartment_code(s): {string.Join(", ", dupDepartmentCodes)}");
                }

                if (duplicateErrors.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for department code. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedDepartments = new List<object>();
                var validDepartments = new List<DepartmentDTO>();
                bool hasValidationErrors = false;

                foreach (var dept in departments)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (string.IsNullOrEmpty(dept.cdepartment_code))
                    {
                        errors.Add("cdepartment_code: Field is required");
                        nullMandatoryFields.Add("cdepartment_code");
                    }
                    else if (dept.cdepartment_code.Length > 50)
                    {
                        errors.Add("cdepartment_code: Maximum length is 50 characters");
                    }

                    if (string.IsNullOrEmpty(dept.cdepartment_name))
                    {
                        errors.Add("cdepartment_name: Field is required");
                        nullMandatoryFields.Add("cdepartment_name");
                    }
                    else if (dept.cdepartment_name.Length > 100)
                    {
                        errors.Add("cdepartment_name: Maximum length is 100 characters");
                    }

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedDepartments.Add(new
                        {
                            cdepartment_code = dept.cdepartment_code ?? "NULL",
                            cdepartment_name = dept.cdepartment_name ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validDepartments.Add(dept);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_departments_received = departments.Count,
                            total_valid_departments = validDepartments.Count,
                            total_failed_departments = failedDepartments.Count,
                            failed_departments = failedDepartments,
                            valid_departments = validDepartments.Select(d => new
                            {
                                d.cdepartment_code,
                                d.cdepartment_name,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var existingDepartmentCodes = await _AccountService.CheckExistingDepartmentCodesAsync(
                    validDepartments.Select(d => d.cdepartment_code).ToList(),
                    cTenantID
                );

                if (existingDepartmentCodes.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in database.",
                        body = new
                        {
                            validation_type = "DATABASE_DUPLICATE_CHECK",
                            message = $"Department codes already exist in database: {string.Join(", ", existingDepartmentCodes)}",
                            note = "Remove duplicate department codes before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<DepartmentDTO> successfullyInsertedDepartments = new List<DepartmentDTO>();
                List<object> databaseFailedDepartments = new List<object>();

                if (validDepartments.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertDepartmentsAsync(validDepartments, cTenantID, usernameClaim);
                        successfullyInsertedDepartments = validDepartments;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedDepartments.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some departments may already exist in the database. Check unique constraints (cdepartment_code)."
                        });

                        insertedCount = 0;
                        successfullyInsertedDepartments = new List<DepartmentDTO>();
                    }
                }

                var insertedDepartmentsWithDetails = successfullyInsertedDepartments.Select(d => new
                {
                    d.cdepartment_code,
                    d.cdepartment_name,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" }
                }).ToList();

                object response;

                if (databaseFailedDepartments.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_departments_received = departments.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedDepartments,
                            note = "Database insertion failed due to constraint violations. Some departments may already exist."
                        },
                        error = "Check unique constraints (cdepartment_code)"
                    };
                }
                else if (insertedCount == validDepartments.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk department creation completed successfully",
                        body = new
                        {
                            total = departments.Count,
                            success = insertedCount,
                            failure = departments.Count - insertedCount,
                            inserted = insertedDepartmentsWithDetails,
                            failed = failedDepartments.Any() ? failedDepartments : new List<object> { new { message = "No validation failures" } },
                            note = "All departments passed validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = departments.Count,
                            success = insertedCount,
                            failure = departments.Count - insertedCount,
                            inserted = insertedDepartmentsWithDetails,
                            failed = failedDepartments,
                            note = "Some departments were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreateRolesApi")]
        public async Task<IActionResult> CreateRolesApi([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if( string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                List<RoleDTO> roles;
                try
                {
                    roles = JsonConvert.DeserializeObject<List<RoleDTO>>(decryptedJson);
                }
                catch (JsonException)
                {
                    var singleRole = JsonConvert.DeserializeObject<RoleDTO>(decryptedJson);
                    roles = new List<RoleDTO> { singleRole };
                }

                if (roles == null || !roles.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No roles provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupRoleCodes = roles.GroupBy(r => r.crole_code)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .Select(g => g.Key)
                    .ToList();

                if (dupRoleCodes.Any())
                {
                    duplicateErrors.Add($"Duplicate crole_code(s): {string.Join(", ", dupRoleCodes)}");
                }

                if (duplicateErrors.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for role code. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedRoles = new List<object>();
                var validRoles = new List<RoleDTO>();
                bool hasValidationErrors = false;

                foreach (var role in roles)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (string.IsNullOrEmpty(role.crole_code))
                    {
                        errors.Add("crole_code: Field is required");
                        nullMandatoryFields.Add("crole_code");
                    }
                    else if (role.crole_code.Length > 50)
                    {
                        errors.Add("crole_code: Maximum length is 50 characters");
                    }

                    if (string.IsNullOrEmpty(role.crole_level))
                    {
                        errors.Add("crole_level: Field is required");
                        nullMandatoryFields.Add("crole_level");
                    }
                    else if (role.crole_level.Length > 50)
                    {
                        errors.Add("crole_level: Maximum length is 50 characters");
                    }

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedRoles.Add(new
                        {
                            crole_code = role.crole_code ?? "NULL",
                            crole_level = role.crole_level ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validRoles.Add(role);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_roles_received = roles.Count,
                            total_valid_roles = validRoles.Count,
                            total_failed_roles = failedRoles.Count,
                            failed_roles = failedRoles,
                            valid_roles = validRoles.Select(r => new
                            {
                                r.crole_code,
                                r.crole_level,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var existingRoleCodes = await _AccountService.CheckExistingRoleCodesAsync(
                    validRoles.Select(r => r.crole_code).ToList(),
                    cTenantID
                );

                if (existingRoleCodes.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in database.",
                        body = new
                        {
                            validation_type = "DATABASE_DUPLICATE_CHECK",
                            message = $"Role codes already exist in database: {string.Join(", ", existingRoleCodes)}",
                            note = "Remove duplicate role codes before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<RoleDTO> successfullyInsertedRoles = new List<RoleDTO>();
                List<object> databaseFailedRoles = new List<object>();

                if (validRoles.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertRolesAsync(validRoles, cTenantID, usernameClaim);
                        successfullyInsertedRoles = validRoles;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedRoles.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some roles may already exist in the database. Check unique constraints (crole_code)."
                        });

                        insertedCount = 0;
                        successfullyInsertedRoles = new List<RoleDTO>();
                    }
                }

                var insertedRolesWithDetails = successfullyInsertedRoles.Select(r => new
                {
                    r.crole_code,
                    r.crole_level,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" }
                }).ToList();

                object response;

                if (databaseFailedRoles.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_roles_received = roles.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedRoles,
                            note = "Database insertion failed due to constraint violations. Some roles may already exist."
                        },
                        error = "Check unique constraints (crole_code)"
                    };
                }
                else if (insertedCount == validRoles.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk role creation completed successfully",
                        body = new
                        {
                            total = roles.Count,
                            success = insertedCount,
                            failure = roles.Count - insertedCount,
                            inserted = insertedRolesWithDetails,
                            failed = failedRoles.Any() ? failedRoles : new List<object> { new { message = "No validation failures" } },
                            note = "All roles passed validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = roles.Count,
                            success = insertedCount,
                            failure = roles.Count - insertedCount,
                            inserted = insertedRolesWithDetails,
                            failed = failedRoles,
                            note = "Some roles were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreatePositionsApi")]
        public async Task<IActionResult> CreatePositionsApi([FromBody] pay request)
        {
            try
            {
                if (request == null || string.IsNullOrWhiteSpace(request.payload))
                {
                    return EncryptedError(400, "Request body cannot be null or empty");
                }
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                if (string.IsNullOrWhiteSpace(jwtToken))
                {
                    return EncryptedError(400, "Authorization token is missing");
                }
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                var usernameClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "username")?.Value;

                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID) || string.IsNullOrWhiteSpace(usernameClaim))
                {
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, encryptedError);
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);

                List<PositionDTO> positions;
                try
                {
                    positions = JsonConvert.DeserializeObject<List<PositionDTO>>(decryptedJson);
                }
                catch (JsonException)
                {
                    var singlePosition = JsonConvert.DeserializeObject<PositionDTO>(decryptedJson);
                    positions = new List<PositionDTO> { singlePosition };
                }

                if (positions == null || !positions.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No positions provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var duplicateErrors = new List<string>();

                var dupPositionCodes = positions.GroupBy(p => p.cposition_code)
                    .Where(g => g.Count() > 1 && !string.IsNullOrEmpty(g.Key))
                    .Select(g => g.Key)
                    .ToList();

                if (dupPositionCodes.Any())
                {
                    duplicateErrors.Add($"Duplicate cposition_code(s): {string.Join(", ", dupPositionCodes)}");
                }

                if (duplicateErrors.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in JSON payload.",
                        body = new
                        {
                            validation_type = "DUPLICATE_CHECK",
                            message = string.Join("; ", duplicateErrors),
                            note = "Duplicates detected for position code. Remove duplicates before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var failedPositions = new List<object>();
                var validPositions = new List<PositionDTO>();
                bool hasValidationErrors = false;

                foreach (var position in positions)
                {
                    var errors = new List<string>();
                    var nullMandatoryFields = new List<string>();

                    if (string.IsNullOrEmpty(position.cposition_code))
                    {
                        errors.Add("cposition_code: Field is required");
                        nullMandatoryFields.Add("cposition_code");
                    }
                    else if (position.cposition_code.Length > 50)
                    {
                        errors.Add("cposition_code: Maximum length is 50 characters");
                    }

                    if (string.IsNullOrEmpty(position.cposition_name))
                    {
                        errors.Add("cposition_name: Field is required");
                        nullMandatoryFields.Add("cposition_name");
                    }
                    else if (position.cposition_name.Length > 100)
                    {
                        errors.Add("cposition_name: Maximum length is 100 characters");
                    }

                    if (errors.Any())
                    {
                        hasValidationErrors = true;
                        failedPositions.Add(new
                        {
                            cposition_code = position.cposition_code ?? "NULL",
                            cposition_name = position.cposition_name ?? "NULL",
                            reason = string.Join("; ", errors),
                            null_mandatory_fields = nullMandatoryFields.Any() ? nullMandatoryFields : new List<string> { "None" }
                        });
                    }
                    else
                    {
                        validPositions.Add(position);
                    }
                }

                if (hasValidationErrors)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Missing or Invalid Fields",
                        body = new
                        {
                            validation_type = "FIELD_VALIDATION",
                            database_operation = "NONE",
                            total_positions_received = positions.Count,
                            total_valid_positions = validPositions.Count,
                            total_failed_positions = failedPositions.Count,
                            failed_positions = failedPositions,
                            valid_positions = validPositions.Select(p => new
                            {
                                p.cposition_code,
                                p.cposition_name,
                                validation_status = "PASSED"
                            }),
                            note = "Mandatory fields validated. Shows exactly which fields are null or invalid."
                        },
                        message = "Bulk insertion aborted - validation failed"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                var existingPositionCodes = await _AccountService.CheckExistingPositionCodesAsync(
                    validPositions.Select(p => p.cposition_code).ToList(),
                    cTenantID
                );

                if (existingPositionCodes.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Duplicate values found in database.",
                        body = new
                        {
                            validation_type = "DATABASE_DUPLICATE_CHECK",
                            message = $"Position codes already exist in database: {string.Join(", ", existingPositionCodes)}",
                            note = "Remove duplicate position codes before retrying."
                        }
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest(encryptedError);
                }

                int insertedCount = 0;
                List<PositionDTO> successfullyInsertedPositions = new List<PositionDTO>();
                List<object> databaseFailedPositions = new List<object>();

                if (validPositions.Any())
                {
                    try
                    {
                        insertedCount = await _AccountService.InsertPositionsAsync(validPositions, cTenantID, usernameClaim);
                        successfullyInsertedPositions = validPositions;
                    }
                    catch (Exception dbEx)
                    {
                        databaseFailedPositions.Add(new
                        {
                            error_type = "DATABASE_CONSTRAINT_VIOLATION",
                            message = dbEx.Message,
                            note = "Some positions may already exist in the database. Check unique constraints (cposition_code)."
                        });

                        insertedCount = 0;
                        successfullyInsertedPositions = new List<PositionDTO>();
                    }
                }

                var insertedPositionsWithDetails = successfullyInsertedPositions.Select(p => new
                {
                    p.cposition_code,
                    p.cposition_name,
                    validation_status = "PASSED",
                    null_mandatory_fields = new List<string> { "None" }
                }).ToList();

                object response;

                if (databaseFailedPositions.Any())
                {
                    response = new
                    {
                        status = 500,
                        statusText = "Database Insertion Failed",
                        body = new
                        {
                            total_positions_received = positions.Count,
                            successful_inserts = insertedCount,
                            database_errors = databaseFailedPositions,
                            note = "Database insertion failed due to constraint violations. Some positions may already exist."
                        },
                        error = "Check unique constraints (cposition_code)"
                    };
                }
                else if (insertedCount == validPositions.Count)
                {
                    response = new
                    {
                        status = 200,
                        statusText = "Bulk position creation completed successfully",
                        body = new
                        {
                            total = positions.Count,
                            success = insertedCount,
                            failure = positions.Count - insertedCount,
                            inserted = insertedPositionsWithDetails,
                            failed = failedPositions.Any() ? failedPositions : new List<object> { new { message = "No validation failures" } },
                            note = "All positions passed validation and were inserted successfully."
                        },
                        error = ""
                    };
                }
                else
                {
                    response = new
                    {
                        status = 207,
                        statusText = "Partial completion",
                        body = new
                        {
                            total = positions.Count,
                            success = insertedCount,
                            failure = positions.Count - insertedCount,
                            inserted = insertedPositionsWithDetails,
                            failed = failedPositions,
                            note = "Some positions were inserted successfully."
                        },
                        error = ""
                    };
                }

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);

                if (((dynamic)response).status == 500)
                    return StatusCode(500, encrypted);
                else if (((dynamic)response).status == 207)
                    return StatusCode(207, encrypted);
                else
                    return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "Operation failed due to unexpected error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }
    }
}