using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Interfaces
{
  
    public interface ISapSyncJobService
    {
        //Task UserdetailSAPAPIinsertAsync(int cTenantID);

        Task<bool> SyncEmployeesAsync(int tenantId);
        Task<bool> CheckIfSourceHasDataAsync(string tableName);
        Task<InBoundSyncResponseDTO> SyncTablesFromMISPORTALAsync(InBoundSyncRequestDTO request);



    }

}
