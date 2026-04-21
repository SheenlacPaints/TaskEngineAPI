using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Interfaces
{
  
    public interface ISapSyncJobService
    {
        Task<bool> SyncEmployeesAsync(int tenantId);
        Task<bool> CheckIfSourceHasDataAsync(string tableName);
        Task<InBoundSyncResponseDTO> SyncTablesFromMISPORTALAsync(InBoundSyncRequestDTO request);
    }

}
