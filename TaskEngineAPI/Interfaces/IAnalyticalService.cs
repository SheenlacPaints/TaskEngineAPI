using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
    public interface IAnalyticalService
    {
        Task<int> InsertAnalyticalhubAsync(AnalyticalDTO model, int tenantId, string userName);
        Task<string> GetAnalyticalhub(int cTenantID, string username, string? type, string? searchText = null, int page = 1, int pageSize = 50);
    }
}
