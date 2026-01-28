using System;
using System.Threading.Tasks;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Services; 

namespace TaskEngineAPI.Jobs
{
    public class WhatsAppJob
    {
        private readonly IWhatsAppSchedulerService _whatsAppService;

        public WhatsAppJob(IWhatsAppSchedulerService whatsAppService)
        {
            _whatsAppService = whatsAppService;
        }

        public async Task SendAsync(WhatsAppPayload payload)
        {
            await _whatsAppService.SendMessage(payload);
        }
    }
}