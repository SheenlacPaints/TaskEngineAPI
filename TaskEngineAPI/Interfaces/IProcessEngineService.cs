using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
  
    public interface IProcessEngineService
    {

        Task<List<ProcessEngineTypeDTO>> GetAllProcessenginetypeAsync(int cTenantID);

        Task<int> InsertProcessEngineAsync(ProcessEngineDTO model, int cTenantID,string username);


    }
}
