using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
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
using TaskEngineAPI.Data;
using TaskEngineAPI.DTO;
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
            string email = "", tenantID = "", roleid = "", username = "", hashedPassword = "", firstname = "", lastname = "", tenantname = "";

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
                bool isValid = BCrypt.Net.BCrypt.Verify(User.password, hashedPassword);

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
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);


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
                statusText = success ? "Update successful" : "SuperAdmin not found or update failed"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }

        [Authorize]
        [HttpDelete("DeleteSuperAdmin")]
        public async Task<IActionResult> DeleteSuperAdmin([FromQuery] pay request)
        {
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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
                statusText = success ? "SuperAdmin deleted successfully" : "SuperAdmin not found"
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

            string decrypted = AesEncryption.Decrypt(id)?.Trim();

            if (!int.TryParse(decrypted, out int userid))
                return BadRequest($"Invalid user id: {decrypted}");
            return BadRequest("Invalid user id");
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
                var handler = new JwtSecurityTokenHandler();
                var jsonToken = handler.ReadToken(jwtToken) as JwtSecurityToken;

                var tenantIdClaim = jsonToken?.Claims.SingleOrDefault(claim => claim.Type == "cTenantID")?.Value;
                if (string.IsNullOrWhiteSpace(tenantIdClaim) || !int.TryParse(tenantIdClaim, out int cTenantID))
                {
                    return BadRequest("Invalid or missing cTenantID in token.");
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
                // Extract token and tenant ID
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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
            var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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

        [Authorize]
        [HttpPost("verifyOtpAndExecute")]
        public async Task<ActionResult> VerifyOtpAndExecute([FromBody] pay request)
        {
            try
            {
                var jwtToken = HttpContext.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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

                            // Hash password
                            model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);

                            // Insert new admin
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
                        return EncryptedSuccess(updated ? "Update successful" : "Update failed");

                    case "DELETE":
                        var deleteModel = JsonConvert.DeserializeObject<OtpActionRequest<DeleteAdminDTO>>(decryptedJson);
                        bool deleted = await _AccountService.DeleteSuperAdminAsync(deleteModel.payload, cTenantID, username);
                        return EncryptedSuccess(deleted ? "Deleted successfully" : "Not found");

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

                string query = "SELECT top 1 cuser_name,cphoneno,cTenant_ID FROM AdminUsers WHERE cphoneno=@cphoneno";
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

        [Authorize]
        [HttpPost("CreateUsersBulk")]
        public async Task<IActionResult> CreateUsersBulk([FromBody] pay request)
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
                    var error = new APIResponse
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, $"\"{encryptedError}\"");
                }

                string username = usernameClaim;


                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var users = JsonConvert.DeserializeObject<List<CreateUserDTO>>(decryptedJson);

                if (users == null || !users.Any())
                    return BadRequest("No users provided");

                var validUsers = new List<CreateUserDTO>();
                var failedUsers = new List<object>();

                var duplicateUsernames = users.GroupBy(u => u.cuserid)
                                             .Where(g => g.Count() > 1)
                                             .Select(g => g.Key)
                                             .ToList();

                var duplicateEmails = users.GroupBy(u => u.cemail)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => g.Key)
                                          .ToList();

                var duplicatePhones = users.GroupBy(u => u.cphoneno)
                                          .Where(g => g.Count() > 1)
                                          .Select(g => g.Key)
                                          .ToList();

                foreach (var user in users)
                {
                    var errors = new List<string>();


                    if (user.cuserid <= 0) errors.Add("User ID is mandatory");
                    if (string.IsNullOrEmpty(user.cemail)) errors.Add("Email is mandatory");
                    if (string.IsNullOrEmpty(user.cphoneno)) errors.Add("Phone number is mandatory");

                    user.ctenantID = cTenantID;

                    if (duplicateUsernames.Contains(user.cuserid)) errors.Add("Duplicate username in this batch");
                    if (duplicateEmails.Contains(user.cemail)) errors.Add("Duplicate email in this batch");
                    if (duplicatePhones.Contains(user.cphoneno)) errors.Add("Duplicate phone in this batch");

                    if (errors.Any())
                    {
                        failedUsers.Add(new
                        {
                            user.cemail,
                            user.cuserid,
                            user.cphoneno,
                            reason = string.Join("; ", errors)
                        });
                    }
                    else
                    {
                        validUsers.Add(user);
                    }
                }

                int insertedCount = 0;
                if (validUsers.Any())
                {
                    insertedCount = await _AccountService.InsertUsersBulkAsync(validUsers, cTenantID,username);
                }

                var response = new
                {
                    status = 200,
                    statusText = "Bulk user creation completed",
                    body = new
                    {
                        total = users.Count,
                        success = insertedCount,
                        failure = failedUsers.Count,
                        inserted = validUsers.Select(u => new { u.cemail, u.cuserid }),
                        failed = failedUsers,
                    },
                    error = ""
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new { status = 500, statusText = "Error", error = ex.Message };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }

        [Authorize]
        [HttpPost("CreateUsersBulk3")]
        public async Task<IActionResult> CreateUsersBulk3([FromBody] pay request)
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
                    var errorResponse = new
                    {
                        status = 401,
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, $"{encryptedError}");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var users = JsonConvert.DeserializeObject<List<CreateUserDTO>>(decryptedJson);

                if (users == null || !users.Any())
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "No users provided in payload."
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest($"{encryptedError}");
                }

                var failedUsers = new List<object>();
                var validUsers = new List<CreateUserDTO>();
                bool hasAnyNullField = false;

                foreach (var user in users)
                {
                    var errors = new List<string>();

                    if (user.cuserid <= 0) errors.Add("cuserid: Must be positive number");
                    if (user.cusername == null) errors.Add("cusername: Field is null");
                    if (user.cemail == null) errors.Add("cemail: Field is null");
                    if (user.cpassword == null) errors.Add("cpassword: Field is null");
                    if (user.cfirstName == null) errors.Add("cfirstName: Field is null");
                    if (user.clastName == null) errors.Add("clastName: Field is null");
                    if (user.cphoneno == null) errors.Add("cphoneno: Field is null");
                    if (user.cAlternatePhone == null) errors.Add("cAlternatePhone: Field is null");
                    if (user.cMaritalStatus == null) errors.Add("cMaritalStatus: Field is null");
                    if (user.cnation == null) errors.Add("cnation: Field is null");
                    if (user.cgender == null) errors.Add("cgender: Field is null");
                    if (user.caddress == null) errors.Add("caddress: Field is null");
                    if (user.caddress1 == null) errors.Add("caddress1: Field is null");
                    if (user.caddress2 == null) errors.Add("caddress2: Field is null");
                    if (user.cpincode == null) errors.Add("cpincode: Field is null");
                    if (user.ccity == null) errors.Add("ccity: Field is null");
                    if (user.cstatecode == null) errors.Add("cstatecode: Field is null");
                    if (user.cstatedesc == null) errors.Add("cstatedesc: Field is null");
                    if (user.ccountrycode == null) errors.Add("ccountrycode: Field is null");
                    if (user.ProfileImage == null) errors.Add("ProfileImage: Field is null");
                    if (user.cbankName == null) errors.Add("cbankName: Field is null");
                    if (user.caccountNumber == null) errors.Add("caccountNumber: Field is null");
                    if (user.ciFSCCode == null) errors.Add("ciFSCCode: Field is null");
                    if (user.cpAN == null) errors.Add("cpAN: Field is null");
                    if (user.cemploymentStatus == null) errors.Add("cemploymentStatus: Field is null");
                    if (user.cempcategory == null) errors.Add("cempcategory: Field is null");
                    if (user.cworkloccode == null) errors.Add("cworkloccode: Field is null");
                    if (user.cworklocname == null) errors.Add("cworklocname: Field is null");
                    if (user.crolecode == null) errors.Add("crolecode: Field is null");
                    if (user.crolename == null) errors.Add("crolename: Field is null");
                    if (user.cgradecode == null) errors.Add("cgradecode: Field is null");
                    if (user.cgradedesc == null) errors.Add("cgradedesc: Field is null");
                    if (user.csubrolecode == null) errors.Add("csubrolecode: Field is null");
                    if (user.cdeptcode == null) errors.Add("cdeptcode: Field is null");
                    if (user.cdeptdesc == null) errors.Add("cdeptdesc: Field is null");
                    if (user.cjobcode == null) errors.Add("cjobcode: Field is null");
                    if (user.cjobdesc == null) errors.Add("cjobdesc: Field is null");
                    if (user.creportmgrcode == null) errors.Add("creportmgrcode: Field is null");
                    if (user.creportmgrname == null) errors.Add("creportmgrname: Field is null");
                    if (user.cRoll_id == null) errors.Add("cRoll_id: Field is null");
                    if (user.cRoll_name == null) errors.Add("cRoll_name: Field is null");
                    if (user.cRoll_Id_mngr == null) errors.Add("cRoll_Id_mngr: Field is null");
                    if (user.cRoll_Id_mngr_desc == null) errors.Add("cRoll_Id_mngr_desc: Field is null");
                    if (user.cReportManager_empcode == null) errors.Add("cReportManager_empcode: Field is null");
                    if (user.cReportManager_Poscode == null) errors.Add("cReportManager_Poscode: Field is null");
                    if (user.cReportManager_Posdesc == null) errors.Add("cReportManager_Posdesc: Field is null");
                    if (user.LastLoginIP == null) errors.Add("LastLoginIP: Field is null");
                    if (user.LastLoginDevice == null) errors.Add("LastLoginDevice: Field is null");
                    //if (user.ccreatedby == null) errors.Add("ccreatedby: Field is null");
                    //if (user.cmodifiedby == null) errors.Add("cmodifiedby: Field is null");
                    //if (user.cDeletedBy == null) errors.Add("cDeletedBy: Field is null");

                    if (!user.nIsActive.HasValue) errors.Add("nIsActive: Field is null");
                    if (!user.ldob.HasValue) errors.Add("ldob: Field is null");
                    if (!user.ldoj.HasValue) errors.Add("ldoj: Field is null");
                    if (!user.nnoticePeriodDays.HasValue) errors.Add("nnoticePeriodDays: Field is null");
                    if (!user.lresignationDate.HasValue) errors.Add("lresignationDate: Field is null");
                    if (!user.llastWorkingDate.HasValue) errors.Add("llastWorkingDate: Field is null");
                    //if (!user.croleID.HasValue) errors.Add("croleID: Field is null");
                    if (!user.nIsWebAccessEnabled.HasValue) errors.Add("nIsWebAccessEnabled: Field is null");
                    if (!user.nIsEventRead.HasValue) errors.Add("nIsEventRead: Field is null");
                    if (!user.lLastLoginAt.HasValue) errors.Add("lLastLoginAt: Field is null");
                    if (!user.nFailedLoginAttempts.HasValue) errors.Add("nFailedLoginAttempts: Field is null");
                    if (!user.cPasswordChangedAt.HasValue) errors.Add("cPasswordChangedAt: Field is null");
                    if (!user.nIsLocked.HasValue) errors.Add("nIsLocked: Field is null");
                    //if (!user.ccreateddate.HasValue) errors.Add("ccreateddate: Field is null");
                    if (!user.lmodifieddate.HasValue) errors.Add("lmodifieddate: Field is null");
                    // if (!user.nIsDeleted.HasValue) errors.Add("nIsDeleted: Field is null");
                    //if (!user.lDeletedDate.HasValue) errors.Add("lDeletedDate: Field is null");
                    if (errors.Any())
                    {
                        hasAnyNullField = true;
                        failedUsers.Add(new
                        {
                            cemail = user.cemail ?? "NULL",
                            cuserid = user.cuserid,
                            cphoneno = user.cphoneno ?? "NULL",
                            reason = string.Join("; ", errors.Take(';'))
                        });
                    }
                    else
                    {
                        validUsers.Add(user);
                    }
                }
                if (hasAnyNullField)
                {
                    var errorResponse = new
                    {
                        status = 400,
                        statusText = "Validation Failed - Null Fields Detected",
                        body = new
                        {
                            validation_type = "STRICT_VALIDATION",
                            database_operation = "NONE",
                            total_users_received = users.Count,
                            total_valid_users = validUsers.Count,
                            total_failed_users = failedUsers.Count,
                            failed_users = failedUsers,
                            valid_users = validUsers.Select(u => new { u.cemail, u.cuserid, u.cphoneno }),
                            note = "All 68 fields are mandatory. No users were inserted due to null fields."
                        },
                        message = "Bulk insertion aborted - null fields detected in one or more users"
                    };
                    string errorJson = JsonConvert.SerializeObject(errorResponse);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return BadRequest($"{encryptedError}");
                }

                int insertedCount = 0;
                if (validUsers.Any())
                {
                    insertedCount = await _AccountService.InsertUsersBulkAsync(validUsers, cTenantID, usernameClaim);
                }

                var response = new
                {
                    status = 200,
                    statusText = "Bulk user creation completed successfully",
                    body = new
                    {
                        total = users.Count,
                        success = insertedCount,
                        failure = 0,
                        inserted = validUsers.Select(u => new { u.cemail, u.cuserid }),
                        failed = new List<object>(),
                        note = "All 68 mandatory fields were validated successfully"
                    },
                    error = ""
                };

                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return Ok(encrypted);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "Internal Server Error",
                    error = ex.Message,
                    note = "No database operations were performed due to error"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }
    }
}