using System;
using System.Collections.Generic;

namespace TaskEngineAPI.DTO
{
    public class ScheduleRequest
    {
        public string ApiKey { get; set; }
        public string CampaignName { get; set; }
        public string Destination { get; set; }
        public string UserName { get; set; }
        public List<string> TemplateParams { get; set; } = new();
        public string Source { get; set; } = "api";
        public object Media { get; set; } = new { };
        public List<object> Buttons { get; set; } = new();
        public List<object> CarouselCards { get; set; } = new();
        public object Location { get; set; } = new { };
        public object Attributes { get; set; } = new { };
        public Dictionary<string, string> ParamsFallbackValue { get; set; } = new();

        public DateTime? ScheduleTime { get; set; }
    }

    public class WhatsAppPayload
    {
        public string ApiKey { get; set; }
        public string CampaignName { get; set; }
        public string Destination { get; set; }
        public string UserName { get; set; }
        public List<string> TemplateParams { get; set; } = new();
        public string Source { get; set; } = "api";
        public object Media { get; set; } = new { };
        public List<object> Buttons { get; set; } = new();
        public List<object> CarouselCards { get; set; } = new();
        public object Location { get; set; } = new { };
        public object Attributes { get; set; } = new { };
        public Dictionary<string, string> ParamsFallbackValue { get; set; } = new();

        public string TemplateName { get; set; }
    }
}