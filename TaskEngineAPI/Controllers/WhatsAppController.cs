using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Services;

namespace TaskEngineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class WhatsAppController : ControllerBase
    {
        private readonly IWhatsAppSchedulerService _whatsAppService;
        private readonly ISchedulerService _schedulerService;

        public WhatsAppController(
            IWhatsAppSchedulerService whatsAppService,
            ISchedulerService schedulerService)
        {
            _whatsAppService = whatsAppService;
            _schedulerService = schedulerService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] ScheduleRequest request)
        {
            try
            {
                var payload = ConvertToWhatsAppPayload(request);
                await _whatsAppService.SendMessage(payload);

                return Ok(new
                {
                    success = true,
                    message = "Message sent successfully",
                    destination = request.Destination,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpPost("schedule")]
        public IActionResult ScheduleMessage([FromBody] ScheduleRequest request)
        {
            try
            {
                var jobId = _schedulerService.ScheduleMessage(request);

                return Ok(new
                {
                    success = true,
                    message = "Message scheduled successfully",
                    jobId = jobId,
                    scheduledTime = request.ScheduleTime,
                    destination = request.Destination
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        [HttpGet("jobs")]
        public IActionResult GetJobs()
        {
            var jobs = _schedulerService.GetJobs();
            return Ok(jobs);
        }

        [HttpDelete("jobs/{jobId}")]
        public IActionResult CancelJob(string jobId)
        {
            var result = _schedulerService.CancelJob(jobId);

            if (result)
            {
                return Ok(new { success = true, message = "Job cancelled successfully" });
            }

            return NotFound(new { success = false, message = "Job not found" });
        }

        [HttpPost("schedule-recurring")]
        public IActionResult ScheduleRecurring(
            [FromBody] ScheduleRequest request,
            [FromQuery] string cronExpression = "0 9 * * *") 
        {
            try
            {
                var jobId = _schedulerService.ScheduleRecurring(request, cronExpression);

                return Ok(new
                {
                    success = true,
                    message = "Recurring message scheduled successfully",
                    jobId = jobId,
                    cronExpression = cronExpression,
                    destination = request.Destination
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        private WhatsAppPayload ConvertToWhatsAppPayload(ScheduleRequest request)
        {
            return new WhatsAppPayload
            {
                ApiKey = request.ApiKey,
                CampaignName = request.CampaignName,
                Destination = request.Destination,
                UserName = request.UserName,
                TemplateParams = request.TemplateParams ?? new System.Collections.Generic.List<string>(),
                Source = request.Source ?? "api",
                Media = request.Media ?? new { },
                Buttons = request.Buttons ?? new System.Collections.Generic.List<object>(),
                CarouselCards = request.CarouselCards ?? new System.Collections.Generic.List<object>(),
                Location = request.Location ?? new { },
                Attributes = request.Attributes ?? new { },
                ParamsFallbackValue = request.ParamsFallbackValue ?? new System.Collections.Generic.Dictionary<string, string>()
            };
        }
    }
}