namespace TaskEngineAPI.Interfaces
{
  
    public interface ISapSyncJobService
    {
        Task<bool> SyncEmployeesAsync(int tenantId);
    }

}
