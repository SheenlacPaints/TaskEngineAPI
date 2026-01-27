using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
    public interface IAnalyticalService
    {
        Task<int> InsertAnalyticalhubAsync(AnalyticalDTO model, int tenantId, string userName);
    }
}
