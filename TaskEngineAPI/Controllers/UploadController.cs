using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.DTO;
namespace TaskEngineAPI.Controllers

{
    [Route("api/[controller]")]
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly IConfiguration Configuration;

        private readonly IMinioService _minioService;
        public UploadController(IConfiguration _configuration, IMinioService MinioService)
        {
            Configuration = _configuration;
            _minioService = MinioService;
        }


        [HttpPost("Fileupload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadToMinio([FromForm] FileUploadRequest request)
        {
            if (request.File == null || request.File.Length == 0)
                return BadRequest("File is empty");

            await _minioService.UploadFileAsync(request.File);

            return Ok("File uploaded successfully");
        }
     

        [HttpGet("downloadS3/{fileName}")]
        public async Task<IActionResult> Download(string fileName)
        {
            var (stream, contentType) = await _minioService.GetFileAsync(fileName);

            return File(stream, contentType, fileName);
        }

        [HttpGet("viewS3/{fileName}")]
        public async Task<IActionResult> View(string fileName)
        {
            var (stream, contentType) = await _minioService.GetFileAsync(fileName);

            Response.Headers.Add("Content-Disposition", "inline");

            return File(stream, contentType);
        }

        [HttpGet("Getfile")]
        public async Task<IActionResult> Getfile(string fileName,string type)
        {
            var (stream, contentType) = await _minioService.GetFileAsync(fileName);

            Response.Headers.Add("Content-Disposition", "inline");

            return File(stream, contentType);
        }





    }




}



