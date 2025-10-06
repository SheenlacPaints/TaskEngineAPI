using Microsoft.AspNetCore.Mvc;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using TaskEngineAPI.Data;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskMasterController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IConfiguration _configuration;
        private readonly IJwtService _jwtService;
        private readonly IAdminService _AccountService;
        private readonly ApplicationDbContext _context;
        public TaskMasterController(IConfiguration configuration, IJwtService jwtService, IAdminService AccountService)
        {

            _config = configuration;
            _jwtService = jwtService;
            _AccountService = AccountService;
        }


    }
}
