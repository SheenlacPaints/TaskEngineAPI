using System.Data.SqlClient;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using TaskEngineAPI.Data;
using TaskEngineAPI.Services;
using Microsoft.EntityFrameworkCore;
using TaskEngineAPI.Repositories;
using Microsoft.AspNetCore.Authorization;
using System.IdentityModel.Tokens.Jwt;
using System.Reflection.PortableExecutable;
using BCrypt.Net;
using System.Net.Http.Headers;
using System.Drawing;
using System.Net.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Net.Mail;
using Microsoft.IdentityModel.Tokens;
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
                statusText = result > 0 ? "User created successfully" : "User creation failed"
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
          
                bool usernameExists = await _AccountService.CheckuserUsernameExistsputAsync(model.cusername, model.ctenantID,model.id);
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
                        bool deleted = await _AccountService.DeleteSuperAdminAsync(deleteModel.payload, cTenantID,username);
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

        [Authorize]
        [HttpPut("UpdateSuperAdminpassword")]
        public async Task<IActionResult> UpdateSuperAdminpassword([FromBody] pay request)
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
                return StatusCode(401, $"\"{encryptedError}\"");
            }
            string decryptedJson = AesEncryption.Decrypt(request.payload);
            var model = JsonConvert.DeserializeObject<UpdateadminPassword>(decryptedJson);
            bool success = await _AccountService.UpdatePasswordSuperAdminAsync(model, cTenantID, username);

            var response = new APIResponse
            {
                status = success ? 200 : 204,
                statusText = success ? "Update successful" : "SuperAdmin not found or update failed"
            };

            string json = JsonConvert.SerializeObject(response);
            string encrypted = AesEncryption.Encrypt(json);
            return StatusCode(response.status, encrypted);
        }
      
    }
}
            
        


  

