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
namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase

    {
        private IConfiguration _config;
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
        public async Task<ActionResult<APIResponse>> Login([FromBody] pay request)
        {
            string urlSafe1 = AesEncryption.Decrypt(request.payload);
            var User = JsonConvert.DeserializeObject<User>(urlSafe1);

            APIResponse Objresponse = new APIResponse();

            if (User == null || string.IsNullOrEmpty(User.userName) || string.IsNullOrEmpty(User.password))
                return BadRequest("Username and password must be provided.");

            var connStr = _config.GetConnectionString("Database");
            string status = string.Empty;
            string email = "", TenantID = "", UserID = "", roleid = "",  username = "";
            Console.WriteLine("DB Connection String: " + connStr);

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_validate_Admin_login", conn))
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
                                    UserID = reader["cuserid"]?.ToString();
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

                    var accessToken = _jwtService.GenerateJwtToken(User.userName, out var tokenExpiry);
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
                        UserID = UserID,
                        username = username,
                        RoleID = roleid,
                        TenantID = TenantID,
                        TenantName = TenantID,
                        email = email,
                        Token = accessToken,
                        RefreshToken = refreshToken
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
        [Route("Loginwithoutencrypt")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<APIResponse>> Loginwithoutencrypt(User User)
        {

            APIResponse Objresponse = new APIResponse();

            if (User == null || string.IsNullOrEmpty(User.userName) || string.IsNullOrEmpty(User.password))
                return BadRequest("Username and password must be provided.");

            var connStr = _config.GetConnectionString("Database");
            string status = string.Empty;
            string email = "", TenantID = "", UserID = "", roleid = "", roll_name = "", username = "";
            Console.WriteLine("DB Connection String: " + connStr);

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_validate_Admin_login", conn))
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
                                    UserID = reader["cuserid"]?.ToString();
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

                    var accessToken = _jwtService.GenerateJwtToken(User.userName, out var tokenExpiry);
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
                        UserID = UserID,
                        username = username,
                        RoleID = roleid,
                        TenantID = TenantID,
                        TenantName = TenantID,
                        email = email,
                        Token = accessToken,
                        RefreshToken = refreshToken
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

                    return StatusCode(200, json);
                }

                Objresponse.statusText = "Unexpected login status";
                Objresponse.status = 500;
                return StatusCode(500, (Objresponse));
            }
            catch (Exception ex)
            {
                Objresponse.statusText = "An error occurred during login: " + ex.Message;
                Objresponse.status = 500;
                return StatusCode(500, (Objresponse));
            }

        }

        private static readonly byte[] IV = Encoding.UTF8.GetBytes("ABCDEFGH12345678");  // 16 bytes IV
        private static readonly byte[] ENCKey = Encoding.UTF8.GetBytes("#@WORKFLOW!#%!#%$%^&KEY*&%#(@*!#");
        private static readonly byte[] DECKey = Encoding.UTF8.GetBytes("#@MISPORTAL2025!%^$#$123456789@#");
        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = DECKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        [HttpPost]
        [Route("EncryptInput")]
        public ActionResult<string> EncryptInput([FromBody] CreateAdminDTO user)
        {
            string json = JsonConvert.SerializeObject(user);
            string encrypted = Encrypt(json);
            return Ok(encrypted);

        }

        [HttpPost]
        [Route("DecryptedInput")]
        public ActionResult<string> DecryptInput([FromBody] string encryptedInput)
        {

            string Decrypted = AesEncryption.Decrypt(encryptedInput);
            return Ok(Decrypted);
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

            [HttpGet]
            [Route("GetAllSuperAdmin")]
            public async Task<ActionResult> GetAllSuperAdmin()
            {
                var superAdmins = await _AccountService.GetAllSuperAdminsAsync();

                APIResponse response;

                if (superAdmins == null || !superAdmins.Any())
                {
                    response = new APIResponse
                    {
                        body = Array.Empty<object>(),
                        statusText = "No SuperAdmins found",
                        status = 204
                    };
                }
                else
                {
                    response = new APIResponse
                    {
                        body = superAdmins.ToArray(),
                        statusText = "Successful",
                        status = 200
                    };
                }

                string jsoner = JsonConvert.SerializeObject(response);
                var encrypted = AesEncryption.Encrypt(jsoner);
                return StatusCode(200, encrypted);
            }


        }


    }



  

