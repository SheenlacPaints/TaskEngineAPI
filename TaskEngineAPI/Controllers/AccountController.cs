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
        public IActionResult CreateUsersBulk3([FromBody] pay request)
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
                        statusText = "Invalid or missing cTenantID or username in token."
                    };
                    string errorJson = JsonConvert.SerializeObject(error);
                    string encryptedError = AesEncryption.Encrypt(errorJson);
                    return StatusCode(401, $"\"{encryptedError}\"");
                }

                string decryptedJson = AesEncryption.Decrypt(request.payload);
                var users = JsonConvert.DeserializeObject<List<CreateUserDTO>>(decryptedJson);

                if (users == null || !users.Any())
                    return BadRequest("No users provided.");



                var validationResults = new List<object>();
                var validUsers = new List<CreateUserDTO>();
                var failedUsers = new List<object>();

                foreach (var user in users)
                {
                    var errors = new List<string>();

                    if (user.cusername == null) errors.Add("cusername: Field is missing from JSON");
                    if (user.cemail == null) errors.Add("cemail: Field is missing from JSON");
                    if (user.cpassword == null) errors.Add("cpassword: Field is missing from JSON");
                    if (user.cfirstName == null) errors.Add("cfirstName: Field is missing from JSON");
                    if (user.clastName == null) errors.Add("clastName: Field is missing from JSON");
                    if (user.cphoneno == null) errors.Add("cphoneno: Field is missing from JSON");
                    if (user.cAlternatePhone == null) errors.Add("cAlternatePhone: Field is missing from JSON");
                    if (user.cMaritalStatus == null) errors.Add("cMaritalStatus: Field is missing from JSON");
                    if (user.cnation == null) errors.Add("cnation: Field is missing from JSON");
                    if (user.cgender == null) errors.Add("cgender: Field is missing from JSON");
                    if (user.caddress == null) errors.Add("caddress: Field is missing from JSON");
                    if (user.caddress1 == null) errors.Add("caddress1: Field is missing from JSON");
                    if (user.caddress2 == null) errors.Add("caddress2: Field is missing from JSON");
                    if (user.cpincode == null) errors.Add("cpincode: Field is missing from JSON");
                    if (user.ccity == null) errors.Add("ccity: Field is missing from JSON");
                    if (user.cstatecode == null) errors.Add("cstatecode: Field is missing from JSON");
                    if (user.cstatedesc == null) errors.Add("cstatedesc: Field is missing from JSON");
                    if (user.ccountrycode == null) errors.Add("ccountrycode: Field is missing from JSON");
                    if (user.ProfileImage == null) errors.Add("ProfileImage: Field is missing from JSON");
                    if (user.cbankName == null) errors.Add("cbankName: Field is missing from JSON");
                    if (user.caccountNumber == null) errors.Add("caccountNumber: Field is missing from JSON");
                    if (user.ciFSCCode == null) errors.Add("ciFSCCode: Field is missing from JSON");
                    if (user.cpAN == null) errors.Add("cpAN: Field is missing from JSON");
                    if (user.cemploymentStatus == null) errors.Add("cemploymentStatus: Field is missing from JSON");
                    if (user.cempcategory == null) errors.Add("cempcategory: Field is missing from JSON");
                    if (user.cworkloccode == null) errors.Add("cworkloccode: Field is missing from JSON");
                    if (user.cworklocname == null) errors.Add("cworklocname: Field is missing from JSON");
                    if (user.crolecode == null) errors.Add("crolecode: Field is missing from JSON");
                    if (user.crolename == null) errors.Add("crolename: Field is missing from JSON");
                    if (user.cgradecode == null) errors.Add("cgradecode: Field is missing from JSON");
                    if (user.cgradedesc == null) errors.Add("cgradedesc: Field is missing from JSON");
                    if (user.csubrolecode == null) errors.Add("csubrolecode: Field is missing from JSON");
                    if (user.cdeptcode == null) errors.Add("cdeptcode: Field is missing from JSON");
                    if (user.cdeptdesc == null) errors.Add("cdeptdesc: Field is missing from JSON");
                    if (user.cjobcode == null) errors.Add("cjobcode: Field is missing from JSON");
                    if (user.cjobdesc == null) errors.Add("cjobdesc: Field is missing from JSON");
                    if (user.creportmgrcode == null) errors.Add("creportmgrcode: Field is missing from JSON");
                    if (user.creportmgrname == null) errors.Add("creportmgrname: Field is missing from JSON");
                    if (user.cRoll_id == null) errors.Add("cRoll_id: Field is missing from JSON");
                    if (user.cRoll_name == null) errors.Add("cRoll_name: Field is missing from JSON");
                    if (user.cRoll_Id_mngr == null) errors.Add("cRoll_Id_mngr: Field is missing from JSON");
                    if (user.cRoll_Id_mngr_desc == null) errors.Add("cRoll_Id_mngr_desc: Field is missing from JSON");
                    if (user.cReportManager_empcode == null) errors.Add("cReportManager_empcode: Field is missing from JSON");
                    if (user.cReportManager_Poscode == null) errors.Add("cReportManager_Poscode: Field is missing from JSON");
                    if (user.cReportManager_Posdesc == null) errors.Add("cReportManager_Posdesc: Field is missing from JSON");
                    if (user.LastLoginIP == null) errors.Add("LastLoginIP: Field is missing from JSON");
                    if (user.LastLoginDevice == null) errors.Add("LastLoginDevice: Field is missing from JSON");
                    if (user.ccreatedby == null) errors.Add("ccreatedby: Field is missing from JSON");
                    if (user.cmodifiedby == null) errors.Add("cmodifiedby: Field is missing from JSON");
                    if (user.cDeletedBy == null) errors.Add("cDeletedBy: Field is missing from JSON");

                    if (!user.nIsActive.HasValue) errors.Add("nIsActive: Field is missing from JSON");
                    if (!user.ldob.HasValue) errors.Add("ldob: Field is missing from JSON");
                    if (!user.ldoj.HasValue) errors.Add("ldoj: Field is missing from JSON");
                    if (!user.nnoticePeriodDays.HasValue) errors.Add("nnoticePeriodDays: Field is missing from JSON");
                    if (!user.lresignationDate.HasValue) errors.Add("lresignationDate: Field is missing from JSON");
                    if (!user.llastWorkingDate.HasValue) errors.Add("llastWorkingDate: Field is missing from JSON");
                    if (!user.croleID.HasValue) errors.Add("croleID: Field is missing from JSON");
                    if (!user.nIsWebAccessEnabled.HasValue) errors.Add("nIsWebAccessEnabled: Field is missing from JSON");
                    if (!user.nIsEventRead.HasValue) errors.Add("nIsEventRead: Field is missing from JSON");
                    if (!user.lLastLoginAt.HasValue) errors.Add("lLastLoginAt: Field is missing from JSON");
                    if (!user.nFailedLoginAttempts.HasValue) errors.Add("nFailedLoginAttempts: Field is missing from JSON");
                    if (!user.cPasswordChangedAt.HasValue) errors.Add("cPasswordChangedAt: Field is missing from JSON");
                    if (!user.nIsLocked.HasValue) errors.Add("nIsLocked: Field is missing from JSON");
                    if (!user.ccreateddate.HasValue) errors.Add("ccreateddate: Field is missing from JSON");
                    if (!user.lmodifieddate.HasValue) errors.Add("lmodifieddate: Field is missing from JSON");
                    if (!user.nIsDeleted.HasValue) errors.Add("nIsDeleted: Field is missing from JSON");
                    if (!user.lDeletedDate.HasValue) errors.Add("lDeletedDate: Field is missing from JSON");


                    if (user.cuserid <= 0) errors.Add("cuserid: Must be positive number");
                    else if (user.cuserid < 1000 || user.cuserid > 9999999999999) errors.Add("cuserid: Must be between 1000-9999999999999");

                    if (string.IsNullOrWhiteSpace(user.cusername)) errors.Add("cusername: Is mandatory");
                    else if (user.cusername.ToLower() == "null") errors.Add("cusername: Cannot be 'NULL'");
                    else if (user.cusername.Length > 100) errors.Add("cusername: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.cemail)) errors.Add("cemail: Is mandatory");
                    else if (user.cemail.ToLower() == "null") errors.Add("cemail: Cannot be 'NULL'");
                    else if (user.cemail.Length > 255) errors.Add("cemail: Cannot exceed 255 characters");
                    else
                    {
                        try
                        {
                            var addr = new System.Net.Mail.MailAddress(user.cemail);
                            if (addr.Address != user.cemail) errors.Add("cemail: Invalid email format");
                        }
                        catch { errors.Add("cemail: Invalid email format"); }
                    }
                    if (string.IsNullOrWhiteSpace(user.cpassword)) errors.Add("cpassword: Is mandatory");
                    else if (user.cpassword.ToLower() == "null") errors.Add("cpassword: Cannot be 'NULL'");
                    else if (user.cpassword.Length > 500) errors.Add("cpassword: Cannot exceed 500 characters");
                    else if (user.cpassword.Length < 15) errors.Add("cpassword: Must be at least 8 characters");

                    if (!user.nIsActive.HasValue) errors.Add("nIsActive: Is mandatory");

                    if (string.IsNullOrWhiteSpace(user.cfirstName)) errors.Add("cfirstName: Is mandatory");
                    else if (user.cfirstName.ToLower() == "null") errors.Add("cfirstName: Cannot be 'NULL'");
                    else if (user.cfirstName.Length > 100) errors.Add("cfirstName: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.clastName)) errors.Add("clastName: Is mandatory");
                    else if (user.clastName.ToLower() == "null") errors.Add("clastName: Cannot be 'NULL'");
                    else if (user.clastName.Length > 100) errors.Add("clastName: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.cphoneno)) errors.Add("cphoneno: Is mandatory");
                    else if (user.cphoneno.ToLower() == "null") errors.Add("cphoneno: Cannot be 'NULL'");
                    else if (user.cphoneno.Length > 25) errors.Add("cphoneno: Cannot exceed 25 characters");

                    if (string.IsNullOrWhiteSpace(user.cAlternatePhone)) errors.Add("cAlternatePhone: Is mandatory");
                    else if (user.cAlternatePhone.ToLower() == "null") errors.Add("cAlternatePhone: Cannot be 'NULL'");
                    else if (user.cAlternatePhone.Length > 25) errors.Add("cAlternatePhone: Cannot exceed 25 characters");

                    if (!user.ldob.HasValue) errors.Add("ldob: Is mandatory");

                    if (string.IsNullOrWhiteSpace(user.cMaritalStatus)) errors.Add("cMaritalStatus: Is mandatory");
                    else if (user.cMaritalStatus.ToLower() == "null") errors.Add("cMaritalStatus: Cannot be 'NULL'");
                    else if (user.cMaritalStatus.Length > 10) errors.Add("cMaritalStatus: Cannot exceed 10 characters");

                    if (string.IsNullOrWhiteSpace(user.cnation)) errors.Add("cnation: Is mandatory");
                    else if (user.cnation.ToLower() == "null") errors.Add("cnation: Cannot be 'NULL'");
                    else if (user.cnation.Length > 50) errors.Add("cnation: Cannot exceed 50 characters");

                    if (string.IsNullOrWhiteSpace(user.cgender)) errors.Add("cgender: Is mandatory");
                    else if (user.cgender.ToLower() == "null") errors.Add("cgender: Cannot be 'NULL'");
                    else if (user.cgender.Length > 10) errors.Add("cgender: Cannot exceed 10 characters");

                    if (string.IsNullOrWhiteSpace(user.caddress)) errors.Add("caddress: Is mandatory");
                    else if (user.caddress.ToLower() == "null") errors.Add("caddress: Cannot be 'NULL'");
                    else if (user.caddress.Length > 500) errors.Add("caddress: Cannot exceed 500 characters");

                    if (string.IsNullOrWhiteSpace(user.caddress1)) errors.Add("caddress1: Is mandatory");
                    else if (user.caddress1.ToLower() == "null") errors.Add("caddress1: Cannot be 'NULL'");
                    else if (user.caddress1.Length > 500) errors.Add("caddress1: Cannot exceed 500 characters");

                    if (string.IsNullOrWhiteSpace(user.caddress2)) errors.Add("caddress2: Is mandatory");
                    else if (user.caddress2.ToLower() == "null") errors.Add("caddress2: Cannot be 'NULL'");
                    else if (user.caddress2.Length > 500) errors.Add("caddress2: Cannot exceed 500 characters");

                    if (string.IsNullOrWhiteSpace(user.cpincode)) errors.Add("cpincode: Is mandatory");
                    else if (user.cpincode.ToLower() == "null") errors.Add("cpincode: Cannot be 'NULL'");
                    else if (user.cpincode.Length > 10) errors.Add("cpincode: Cannot exceed 10 characters");

                    if (string.IsNullOrWhiteSpace(user.ccity)) errors.Add("ccity: Is mandatory");
                    else if (user.ccity.ToLower() == "null") errors.Add("ccity: Cannot be 'NULL'");
                    else if (user.ccity.Length > 250) errors.Add("ccity: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.cstatecode)) errors.Add("cstatecode: Is mandatory");
                    else if (user.cstatecode.ToLower() == "null") errors.Add("cstatecode: Cannot be 'NULL'");
                    else if (user.cstatecode.Length > 10) errors.Add("cstatecode: Cannot exceed 10 characters");

                    if (string.IsNullOrWhiteSpace(user.cstatedesc)) errors.Add("cstatedesc: Is mandatory");
                    else if (user.cstatedesc.ToLower() == "null") errors.Add("cstatedesc: Cannot be 'NULL'");
                    else if (user.cstatedesc.Length > 250) errors.Add("cstatedesc: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.ccountrycode)) errors.Add("ccountrycode: Is mandatory");
                    else if (user.ccountrycode.ToLower() == "null") errors.Add("ccountrycode: Cannot be 'NULL'");
                    else if (user.ccountrycode.Length > 10) errors.Add("ccountrycode: Cannot exceed 10 characters");

                    if (string.IsNullOrWhiteSpace(user.ProfileImage)) errors.Add("ProfileImage: Is mandatory");
                    else if (user.ProfileImage.ToLower() == "null") errors.Add("ProfileImage: Cannot be 'NULL'");

                    if (string.IsNullOrWhiteSpace(user.cbankName)) errors.Add("cbankName: Is mandatory");
                    else if (user.cbankName.ToLower() == "null") errors.Add("cbankName: Cannot be 'NULL'");
                    else if (user.cbankName.Length > 200) errors.Add("cbankName: Cannot exceed 200 characters");

                    if (string.IsNullOrWhiteSpace(user.caccountNumber)) errors.Add("caccountNumber: Is mandatory");
                    else if (user.caccountNumber.ToLower() == "null") errors.Add("caccountNumber: Cannot be 'NULL'");
                    else if (user.caccountNumber.Length > 50) errors.Add("caccountNumber: Cannot exceed 50 characters");

                    if (string.IsNullOrWhiteSpace(user.ciFSCCode)) errors.Add("ciFSCCode: Is mandatory");
                    else if (user.ciFSCCode.ToLower() == "null") errors.Add("ciFSCCode: Cannot be 'NULL'");
                    else if (user.ciFSCCode.Length > 20) errors.Add("ciFSCCode: Cannot exceed 20 characters");

                    if (string.IsNullOrWhiteSpace(user.cpAN)) errors.Add("cpAN: Is mandatory");
                    else if (user.cpAN.ToLower() == "null") errors.Add("cpAN: Cannot be 'NULL'");
                    else if (user.cpAN.Length > 20) errors.Add("cpAN: Cannot exceed 20 characters");

                    if (!user.ldoj.HasValue) errors.Add("ldoj: Is mandatory");

                    if (string.IsNullOrWhiteSpace(user.cemploymentStatus)) errors.Add("cemploymentStatus: Is mandatory");
                    else if (user.cemploymentStatus.ToLower() == "null") errors.Add("cemploymentStatus: Cannot be 'NULL'");
                    else if (user.cemploymentStatus.Length > 50) errors.Add("cemploymentStatus: Cannot exceed 50 characters");

                    if (!user.nnoticePeriodDays.HasValue) errors.Add("nnoticePeriodDays: Is mandatory");
                    else if (user.nnoticePeriodDays < 0) errors.Add("nnoticePeriodDays: Cannot be negative");

                    if (!user.lresignationDate.HasValue) errors.Add("lresignationDate: Is mandatory");

                    if (!user.llastWorkingDate.HasValue) errors.Add("llastWorkingDate: Is mandatory");

                    if (string.IsNullOrWhiteSpace(user.cempcategory)) errors.Add("cempcategory: Is mandatory");
                    else if (user.cempcategory.ToLower() == "null") errors.Add("cempcategory: Cannot be 'NULL'");
                    else if (user.cempcategory.Length > 100) errors.Add("cempcategory: Cannot exceed 100 characters");

                    if (user.cworkloccode == null)
                    {
                        errors.Add("cworkloccode: Field is missing from JSON");
                    }
                    else if (string.IsNullOrWhiteSpace(user.cworkloccode))
                    {
                        errors.Add("cworkloccode: Cannot be empty or whitespace");
                    }
                    else if (user.cworkloccode.ToLower() == "null")
                    {
                        errors.Add("cworkloccode: Cannot be 'NULL'");
                    }
                    else if (user.cworkloccode.Length > 100)
                    {
                        errors.Add("cworkloccode: Cannot exceed 100 characters");
                    }

                    if (string.IsNullOrWhiteSpace(user.cworklocname)) errors.Add("cworklocname: Is mandatory");
                    else if (user.cworklocname.ToLower() == "null") errors.Add("cworklocname: Cannot be 'NULL'");
                    else if (user.cworklocname.Length > 250) errors.Add("cworklocname: Cannot exceed 250 characters");

                    if (!user.croleID.HasValue) errors.Add("croleID: Is mandatory");
                    else if (user.croleID <= 0) errors.Add("croleID: Must be positive");


                    if (string.IsNullOrWhiteSpace(user.crolecode))
                    {
                        user.crolecode = "USER"; 
                    }
                    else if (user.crolecode.ToLower() == "null")
                    {
                        errors.Add("crolecode: Cannot be 'NULL'");
                    }
                    else if (user.crolecode.Length > 100)
                    {
                        errors.Add("crolecode: Cannot exceed 100 characters");
                    }

                    if (string.IsNullOrWhiteSpace(user.crolename)) errors.Add("crolename: Is mandatory");
                    else if (user.crolename.ToLower() == "null") errors.Add("crolename: Cannot be 'NULL'");
                    else if (user.crolename.Length > 250) errors.Add("crolename: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.cgradecode)) errors.Add("cgradecode: Is mandatory");
                    else if (user.cgradecode.ToLower() == "null") errors.Add("cgradecode: Cannot be 'NULL'");
                    else if (user.cgradecode.Length > 100) errors.Add("cgradecode: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.cgradedesc)) errors.Add("cgradedesc: Is mandatory");
                    else if (user.cgradedesc.ToLower() == "null") errors.Add("cgradedesc: Cannot be 'NULL'");
                    else if (user.cgradedesc.Length > 500) errors.Add("cgradedesc: Cannot exceed 500 characters");

                    if (string.IsNullOrWhiteSpace(user.csubrolecode)) errors.Add("csubrolecode: Is mandatory");
                    else if (user.csubrolecode.ToLower() == "null") errors.Add("csubrolecode: Cannot be 'NULL'");
                    else if (user.csubrolecode.Length > 100) errors.Add("csubrolecode: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.cdeptcode)) errors.Add("cdeptcode: Is mandatory");
                    else if (user.cdeptcode.ToLower() == "null") errors.Add("cdeptcode: Cannot be 'NULL'");
                    else if (user.cdeptcode.Length > 100) errors.Add("cdeptcode: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.cdeptdesc)) errors.Add("cdeptdesc: Is mandatory");
                    else if (user.cdeptdesc.ToLower() == "null") errors.Add("cdeptdesc: Cannot be 'NULL'");
                    else if (user.cdeptdesc.Length > 250) errors.Add("cdeptdesc: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.cjobcode)) errors.Add("cjobcode: Is mandatory");
                    else if (user.cjobcode.ToLower() == "null") errors.Add("cjobcode: Cannot be 'NULL'");
                    else if (user.cjobcode.Length > 250) errors.Add("cjobcode: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.cjobdesc)) errors.Add("cjobdesc: Is mandatory");
                    else if (user.cjobdesc.ToLower() == "null") errors.Add("cjobdesc: Cannot be 'NULL'");
                    else if (user.cjobdesc.Length > 250) errors.Add("cjobdesc: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.creportmgrcode)) errors.Add("creportmgrcode: Is mandatory");
                    else if (user.creportmgrcode.ToLower() == "null") errors.Add("creportmgrcode: Cannot be 'NULL'");
                    else if (user.creportmgrcode.Length > 500) errors.Add("creportmgrcode: Cannot exceed 500 characters");

                    if (string.IsNullOrWhiteSpace(user.creportmgrname)) errors.Add("creportmgrname: Is mandatory");
                    else if (user.creportmgrname.ToLower() == "null") errors.Add("creportmgrname: Cannot be 'NULL'");
                    else if (user.creportmgrname.Length > 500) errors.Add("creportmgrname: Cannot exceed 500 characters");

                    if (user.cRoll_id == null)
                    {
                        errors.Add("cRoll_id: Field is missing from JSON");
                    }
                    else if (string.IsNullOrWhiteSpace(user.cRoll_id))
                    {
                        errors.Add("cRoll_id: Cannot be empty or whitespace");
                    }
                    else if (user.cRoll_id.ToLower() == "null")
                    {
                        errors.Add("cRoll_id: Cannot be 'NULL'");
                    }
                    else if (user.cRoll_id.Length > 100)
                    {
                        errors.Add("cRoll_id: Cannot exceed 100 characters");
                    }

                    if (string.IsNullOrWhiteSpace(user.cRoll_name)) errors.Add("cRoll_name: Is mandatory");
                    else if (user.cRoll_name.ToLower() == "null") errors.Add("cRoll_name: Cannot be 'NULL'");
                    else if (user.cRoll_name.Length > 250) errors.Add("cRoll_name: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.cRoll_Id_mngr)) errors.Add("cRoll_Id_mngr: Is mandatory");
                    else if (user.cRoll_Id_mngr.ToLower() == "null") errors.Add("cRoll_Id_mngr: Cannot be 'NULL'");
                    else if (user.cRoll_Id_mngr.Length > 100) errors.Add("cRoll_Id_mngr: Cannot exceed 100 characters");

                    if (string.IsNullOrWhiteSpace(user.cRoll_Id_mngr_desc)) errors.Add("cRoll_Id_mngr_desc: Is mandatory");
                    else if (user.cRoll_Id_mngr_desc.ToLower() == "null") errors.Add("cRoll_Id_mngr_desc: Cannot be 'NULL'");
                    else if (user.cRoll_Id_mngr_desc.Length > 250) errors.Add("cRoll_Id_mngr_desc: Cannot exceed 250 characters");

                    if (string.IsNullOrWhiteSpace(user.cReportManager_empcode)) errors.Add("cReportManager_empcode: Is mandatory");
                    else if (user.cReportManager_empcode.ToLower() == "null") errors.Add("cReportManager_empcode: Cannot be 'NULL'");
                    else if (user.cReportManager_empcode.Length > 1000) errors.Add("cReportManager_empcode: Cannot exceed 1000 characters");

                    if (string.IsNullOrWhiteSpace(user.cReportManager_Poscode)) errors.Add("cReportManager_Poscode: Is mandatory");
                    else if (user.cReportManager_Poscode.ToLower() == "null") errors.Add("cReportManager_Poscode: Cannot be 'NULL'");
                    else if (user.cReportManager_Poscode.Length > 1000) errors.Add("cReportManager_Poscode: Cannot exceed 1000 characters");

                    if (string.IsNullOrWhiteSpace(user.cReportManager_Posdesc)) errors.Add("cReportManager_Posdesc: Is mandatory");
                    else if (user.cReportManager_Posdesc.ToLower() == "null") errors.Add("cReportManager_Posdesc: Cannot be 'NULL'");
                    else if (user.cReportManager_Posdesc.Length > 1000) errors.Add("cReportManager_Posdesc: Cannot exceed 1000 characters");

                    if (!user.nIsWebAccessEnabled.HasValue) errors.Add("nIsWebAccessEnabled: Is mandatory");

                    if (!user.nIsEventRead.HasValue) errors.Add("nIsEventRead: Is mandatory");

                    if (!user.lLastLoginAt.HasValue) errors.Add("lLastLoginAt: Is mandatory");

                    if (!user.nFailedLoginAttempts.HasValue) errors.Add("nFailedLoginAttempts: Is mandatory");
                    else if (user.nFailedLoginAttempts < 0) errors.Add("nFailedLoginAttempts: Cannot be negative");

                    if (!user.cPasswordChangedAt.HasValue) errors.Add("cPasswordChangedAt: Is mandatory");

                    if (!user.nIsLocked.HasValue) errors.Add("nIsLocked: Is mandatory");

                    if (string.IsNullOrWhiteSpace(user.LastLoginIP)) errors.Add("LastLoginIP: Is mandatory");
                    else if (user.LastLoginIP.ToLower() == "null") errors.Add("LastLoginIP: Cannot be 'NULL'");
                    else if (user.LastLoginIP.Length > 50) errors.Add("LastLoginIP: Cannot exceed 50 characters");

                    if (string.IsNullOrWhiteSpace(user.LastLoginDevice)) errors.Add("LastLoginDevice: Is mandatory");
                    else if (user.LastLoginDevice.ToLower() == "null") errors.Add("LastLoginDevice: Cannot be 'NULL'");
                    else if (user.LastLoginDevice.Length > 200) errors.Add("LastLoginDevice: Cannot exceed 200 characters");

                     if (string.IsNullOrWhiteSpace(usernameClaim))
                    {
                        errors.Add("ccreatedby: Cannot get creator from token - token username is missing");
                    }
                    else if (usernameClaim.ToLower() == "null")
                    {
                        errors.Add("ccreatedby: Creator from token cannot be 'NULL'");
                    }
                    else if (usernameClaim.Length > 50)
                    {
                        errors.Add("ccreatedby: Creator name from token exceeds 50 characters");
                    }

                    if (string.IsNullOrWhiteSpace(usernameClaim))
                    {
                        errors.Add("cmodifiedby: Cannot get modifier from token");
                    }
                    else if (usernameClaim.Length > 50)
                    {
                        errors.Add("cmodifiedby: Modifier name from token exceeds 50 characters");
                    }
                    if (!user.lmodifieddate.HasValue) errors.Add("lmodifieddate: Is mandatory");

                   
                    if (user.cDeletedBy == null)
                    {
                        errors.Add("cDeletedBy: Field is missing from JSON");
                    }
                    else if (string.IsNullOrWhiteSpace(user.cDeletedBy))
                    {
                        user.cDeletedBy = "";
                    }
                    else if (user.cDeletedBy.ToLower() == "null")
                    {
                        errors.Add("cDeletedBy: Cannot be 'NULL'");
                    }
                    else if (user.cDeletedBy.Length > 50)
                    {
                        errors.Add("cDeletedBy: Cannot exceed 50 characters");
                    }

                    var userValidation = new
                    {
                        Email = user.cemail ?? "Not provided",
                        UserID = user.cuserid,
                        Phone = user.cphoneno ?? "Not provided",
                        IsValid = !errors.Any(),
                        Errors = errors.Take(50).ToList()
                    };

                    validationResults.Add(userValidation);

                    if (errors.Any())
                    {
                        failedUsers.Add(new
                        {
                            user.cemail,
                            user.cuserid,
                            user.cphoneno,
                            reason = string.Join("; ", errors.Take(3))
                        });
                    }
                    else
                    {
                        validUsers.Add(user);
                    }
                }

                var duplicateUserIds = validUsers.GroupBy(u => u.cuserid).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                var duplicateEmails = validUsers.GroupBy(u => u.cemail).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
                var duplicatePhones = validUsers.GroupBy(u => u.cphoneno).Where(g => g.Count() > 1).Select(g => g.Key).ToList();

                var duplicateErrors = new List<object>();
                var finalValidUsers = new List<CreateUserDTO>();

                foreach (var user in validUsers)
                {
                    var errors = new List<string>();

                    if (duplicateUserIds.Contains(user.cuserid)) errors.Add("Duplicate User ID in JSON");
                    if (duplicateEmails.Contains(user.cemail)) errors.Add("Duplicate Email in JSON");
                    if (duplicatePhones.Contains(user.cphoneno)) errors.Add("Duplicate Phone in JSON");

                    if (errors.Any())
                    {
                        duplicateErrors.Add(new
                        {
                            user.cemail,
                            user.cuserid,
                            user.cphoneno,
                            reason = string.Join("; ", errors)
                        });
                    }
                    else
                    {
                        finalValidUsers.Add(user);
                    }
                }
                var response = new
                {
                    status = 400,
                    statusText = "JSON Validation Completed",
                    body = new
                    {
                        validation_type = "JSON_ONLY_VALIDATION",
                        database_operation = "NONE",
                        total_users_received = users.Count,
                        actual_json_data = users.Select(u => new
                        {
                            u.cemail,
                            u.cuserid,
                            u.cphoneno,
                            all_fields = u.GetType().GetProperties()
                                .ToDictionary(p => p.Name, p => p.GetValue(u))
                        }),

                        json_structure_valid = true,
                        validation_results = new
                        {
                            total_valid_users = finalValidUsers.Count,
                            total_failed_users = failedUsers.Count + duplicateErrors.Count,
                            valid_users = finalValidUsers.Select(u => new { u.cemail, u.cuserid, u.cphoneno }),
                            field_validation_failures = failedUsers,
                            duplicate_failures = duplicateErrors
                        }
                    },
                    message = "Pure JSON validation completed successfully. No database operations performed."
                };


                string json = JsonConvert.SerializeObject(response);
                string encrypted = AesEncryption.Encrypt(json);
                return Ok($"\"{encrypted}\"");
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    status = 500,
                    statusText = "JSON Validation Error",
                    error = ex.Message,
                    note = "Validation failed during JSON processing only - No database involved"
                };
                string errorJson = JsonConvert.SerializeObject(errorResponse);
                var encryptedError = AesEncryption.Encrypt(errorJson);
                return StatusCode(500, encryptedError);
            }
        }


    }
}