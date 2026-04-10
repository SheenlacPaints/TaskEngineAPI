namespace TaskEngineAPI.Interfaces
{
  
    public interface ISapSyncJobService
    {
        //Task UserdetailSAPAPIinsertAsync(int cTenantID);
        Task SyncEmployeesAsync(int tenantId);
    }

}
