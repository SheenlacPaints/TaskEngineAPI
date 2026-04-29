namespace TaskEngineAPI.Interfaces
{
  
    public interface ISapSyncJobService
    {
        //Task UserdetailSAPAPIinsertAsync(int cTenantID);

        Task<bool> SyncEmployeesAsync(int tenantId);
    }

}
