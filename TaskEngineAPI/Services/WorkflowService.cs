using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Repositories;

namespace TaskEngineAPI.Services;

public class WorkflowService : IWorkflowService
{
    private readonly IWorkflowRepository _repository;
    private readonly ILogger<WorkflowService> _logger;

    public WorkflowService(IWorkflowRepository repository, ILogger<WorkflowService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task<IEnumerable<dynamic>> GetWorkflowDashboardAsync(string tenantId, string userId)
    {
        _logger.LogInformation("Getting workflow dashboard for tenant {TenantId}, user {UserId}", tenantId, userId);
        return await _repository.GetWorkflowDashboardAsync(tenantId, userId);
    }
}