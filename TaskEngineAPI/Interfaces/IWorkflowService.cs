
namespace TaskEngineAPI.Interfaces;

public interface IWorkflowService
{
    Task<IEnumerable<dynamic>> GetWorkflowDashboardAsync(string tenantId, string userId);
}