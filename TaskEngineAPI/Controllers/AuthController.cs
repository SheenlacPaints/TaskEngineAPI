using System.Data;
using System.Data.SqlClient;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Xml;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using APIResponse = TaskEngineAPI.Helpers.APIResponse;
namespace TaskEngineAPI.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private IConfiguration _config;
        private readonly IConfiguration _configuration;
        public AuthController(IConfiguration configuration)
        {
        
            _config = configuration;
           
        }

        [HttpGet]
        [Route("user")]
    
        public ActionResult<string> GetUserGreeting(string name)
        {
            return Ok($"Hello, {name}! Welcome to the API.");
        }

    }
}