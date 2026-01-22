namespace TaskEngineAPI.Interfaces;

public interface IWorkflowRepository
{
    Task<IEnumerable<dynamic>> GetWorkflowDashboardAsync(string tenantId, string userId);
}