using System.Net.Mail;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using TaskEngineAPI.Data;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Services;
using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IConfiguration _config;      
        private readonly ApplicationDbContext _context;

        public EmailController(IConfiguration configuration)
        {
            _config = configuration;   
        }


        [HttpPost]
        public IActionResult SendMail(Email model)
        {

            MailMessage mail = new MailMessage();
            mail.To.Add(model.To);
            mail.From = new MailAddress("misportal@sheenlac.in");
            mail.Subject = model.Subject;
            mail.Body = model.Body;
            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient("smtp.gmail.com", 587);
            smtp.EnableSsl = true;
            smtp.UseDefaultCredentials = false;
            smtp.Credentials = new System.Net.NetworkCredential("misportal@sheenlac.in", "pealwllkghoszomz");
            smtp.Send(mail);
            return NoContent();
        }

       

    }
}
