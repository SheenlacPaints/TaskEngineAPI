using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
  
        public interface ITaskMasterService
        {
            Task<int> InsertTaskMasterAsync(TaskMasterDTO model, int tenantId, string username);


            Task<string> GetAllProcessmetaAsync(int cTenantID,string processcode);


    }

        
}
