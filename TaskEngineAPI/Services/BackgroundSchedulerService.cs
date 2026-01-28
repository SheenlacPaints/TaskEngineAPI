using Hangfire;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Services
{
    public interface ISchedulerService
    {
        string ScheduleMessage(ScheduleRequest request);
        List<ScheduledJob> GetJobs();
        bool CancelJob(string jobId);
        string ScheduleRecurring(ScheduleRequest request, string cronExpression);
    }

    public class ScheduledJob
    {
        public string JobId { get; set; }
        public string Destination { get; set; }
        public string CampaignName { get; set; }
        public string TemplateName { get; set; }
        public DateTime ScheduledTime { get; set; }
        public string Status { get; set; } = "Scheduled";
        public List<string> TemplateParams { get; set; } = new();
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class BackgroundSchedulerService : ISchedulerService
    {
        private readonly IWhatsAppSchedulerService _whatsAppService;
        private readonly ILogger<BackgroundSchedulerService> _logger;
        private readonly ConcurrentDictionary<string, (string hangfireJobId, ScheduledJob job)> _jobs = new();

        public BackgroundSchedulerService(
            IWhatsAppSchedulerService whatsAppService,
            ILogger<BackgroundSchedulerService> logger)
        {
            _whatsAppService = whatsAppService;
            _logger = logger;
        }

        public string ScheduleMessage(ScheduleRequest request)
        {
            try
            {
                if (!request.ScheduleTime.HasValue)
                {
                    throw new ArgumentException("ScheduleTime is required for scheduling");
                }

                if (request.ScheduleTime.Value <= DateTime.UtcNow)
                {
                    throw new ArgumentException("ScheduleTime must be in the future");
                }

                var payload = ConvertToWhatsAppPayload(request);
                var templateName = GenerateTemplateName(request.CampaignName);
                var jobId = Guid.NewGuid().ToString();

                var scheduledJob = new ScheduledJob
                {
                    JobId = jobId,
                    Destination = request.Destination,
                    CampaignName = request.CampaignName,
                    TemplateName = templateName,
                    ScheduledTime = request.ScheduleTime.Value,
                    TemplateParams = request.TemplateParams ?? new List<string>()
                };

                var hangfireJobId = BackgroundJob.Schedule(
                    () => ExecuteScheduledMessage(payload),
                    request.ScheduleTime.Value
                );

                _jobs[jobId] = (hangfireJobId, scheduledJob);

                _logger.LogInformation("Message scheduled. JobId: {JobId}, HangfireId: {HangfireId}, Scheduled for: {Time}",
                    jobId, hangfireJobId, request.ScheduleTime.Value);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule message");
                throw;
            }
        }

        public List<ScheduledJob> GetJobs()
        {
            return _jobs.Values
                .Select(x => x.job)
                .OrderByDescending(j => j.ScheduledTime)
                .ToList();
        }

        public bool CancelJob(string jobId)
        {
            try
            {
                if (_jobs.TryGetValue(jobId, out var jobInfo))
                {
                    BackgroundJob.Delete(jobInfo.hangfireJobId);
                    _jobs.TryRemove(jobId, out _);

                    _logger.LogInformation("Job {JobId} cancelled successfully", jobId);
                    return true;
                }

                _logger.LogWarning("Job {JobId} not found", jobId);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel job {JobId}", jobId);
                return false;
            }
        }

        public string ScheduleRecurring(ScheduleRequest request, string cronExpression)
        {
            try
            {
                var payload = ConvertToWhatsAppPayload(request);
                var templateName = GenerateTemplateName(request.CampaignName);
                var jobId = Guid.NewGuid().ToString();

                var scheduledJob = new ScheduledJob
                {
                    JobId = jobId,
                    Destination = request.Destination,
                    CampaignName = request.CampaignName,
                    TemplateName = templateName,
                    ScheduledTime = DateTime.UtcNow, 
                    Status = "Recurring",
                    TemplateParams = request.TemplateParams ?? new List<string>()
                };

                RecurringJob.AddOrUpdate(
                    jobId,
                    () => ExecuteScheduledMessage(payload),
                    cronExpression
                );

                _jobs[jobId] = (jobId, scheduledJob);

                _logger.LogInformation("Recurring message scheduled. JobId: {JobId}, Cron: {Cron}",
                    jobId, cronExpression);

                return jobId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to schedule recurring message");
                throw;
            }
        }

        public async Task ExecuteScheduledMessage(WhatsAppPayload payload)
        {
            try
            {
                _logger.LogInformation("Executing scheduled message to {Destination}", payload.Destination);
                await _whatsAppService.SendMessage(payload);

                var job = _jobs.Values.FirstOrDefault(x =>
                    x.job.Destination == payload.Destination &&
                    x.job.CampaignName == payload.CampaignName);

                if (!string.IsNullOrEmpty(job.job?.JobId))
                {
                    job.job.Status = "Executed";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute scheduled message");
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
                TemplateParams = request.TemplateParams ?? new List<string>(),
                Source = request.Source ?? "api",
                Media = request.Media ?? new { },
                Buttons = request.Buttons ?? new List<object>(),
                CarouselCards = request.CarouselCards ?? new List<object>(),
                Location = request.Location ?? new { },
                Attributes = request.Attributes ?? new { },
                ParamsFallbackValue = request.ParamsFallbackValue ?? new Dictionary<string, string>()
            };
        }

        private string GenerateTemplateName(string campaignName)
        {
            if (string.IsNullOrEmpty(campaignName))
                return "default_template";

            return campaignName
                .ToLower()
                .Replace(" ", "_")
                .Replace("-", "_")
                .Replace(".", "_")
                .Trim();
        }
    }
}