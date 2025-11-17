using System.Data.SqlClient;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{

    public interface IProcessEngineService
    {

        Task<List<ProcessEngineTypeDTO>> GetAllProcessenginetypeAsync(int cTenantID);

        Task<int> InsertProcessEngineAsync(ProcessEngineDTO model, int cTenantID, string username);

        Task<List<GetProcessEngineDTO>> GetAllProcessengineAsync(int cTenantID);

        Task<List<GetIDProcessEngineDTO>> GetProcessengineAsync(int cTenantID, int id);

        Task<bool> UpdateProcessenginestatusdeleteAsync(updatestatusdeleteDTO model, int cTenantID, string username);

        Task<int> InsertprocessmappingAsync(createprocessmappingDTO model, int cTenantID, string username);

        Task<bool> UpdateprocessmappingAsync(updateprocessmappingDTO model, int cTenantID, string username);



        Task<List<MappingListDTO>> GetMappingListAsync(int cTenantID);
    }

}
