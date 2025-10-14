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
    [Route("api/[controller]")]
    [ApiController]
    public class InternalController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IAdminService _AccountService;
        private readonly ApplicationDbContext _context;
        public InternalController(IConfiguration configuration, IJwtService jwtService)
        {

            _config = configuration;
            _jwtService = jwtService;

        }

        private static readonly byte[] IV = Encoding.UTF8.GetBytes("ABCDEFGH12345678");  // 16 bytes IV
        private static readonly byte[] ENCKey = Encoding.UTF8.GetBytes("#@WORKFLOW!#%!#%$%^&KEY*&%#(@*!#");
        private static readonly byte[] DECKey = Encoding.UTF8.GetBytes("#@MISPORTAL2025!%^$#$123456789@#");


        [HttpPost]
        [Route("EncryptPassword")]
        public ActionResult<string> Encryptpassword([FromBody] string password)
        {
            var hashed = BCrypt.Net.BCrypt.HashPassword(password);
            return Ok(hashed);
        }

        [HttpPost]
        [Route("EncryptInput")]
        public ActionResult<string> EncryptInput([FromBody] OtpActionRequest user)
        {
            string json = JsonConvert.SerializeObject(user);
            string encrypted = Encrypt(json);
            return Ok(encrypted);

        }       
      
        [HttpPost]
        [Route("EncryptInputint")]
        public ActionResult<string> EncryptInputint(TaskMasterDTO CreateAdminDTO)
        {
            string json = JsonConvert.SerializeObject(CreateAdminDTO);
            string encrypted = Encrypt(json);
            return Ok(encrypted);
        }

        [HttpPost]
        [Route("Decrypt")]
        public ActionResult<string> Decrypt([FromBody] string encryptedInput)
        {
            string json = JsonConvert.SerializeObject(encryptedInput);
            string encrypted = Decrypt1(json);
            return Ok(encrypted);

        }

        [HttpPost]
        [Route("DecryptedInput_API")]
        public ActionResult<string> DecryptedInput_API([FromBody] string encryptedInput)
        {

            string Decrypted = DecryptAPI(encryptedInput);
            return Ok(Decrypted);
        }

      
        public static string DecryptAPI(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = ENCKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }


        public static string Decrypt1(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = DECKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }

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

        public static string DecryptI(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = DECKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }

        public static string DecryptAPII(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = ENCKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }


        

    }
}

