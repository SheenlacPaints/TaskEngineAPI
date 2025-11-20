using System.Data.SqlClient;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{

    public interface IProcessEngineService
    {

        Task<List<ProcessEngineTypeDTO>> GetAllProcessenginetypeAsync(int cTenantID);

        Task<int> InsertProcessEngineAsync(ProcessEngineDTO model, int cTenantID, string username);

        //Task<List<GetProcessEngineDTO>> GetAllProcessengineAsync(int cTenantID);
        Task<List<GetProcessEngineDTO>> GetAllProcessengineAsync(int cTenantID, string searchText = null, int page = 1, int pageSize = 10, string created_by = null, string priority = null, int? status = null);


        Task<List<GetIDProcessEngineDTO>> GetProcessengineAsync(int cTenantID, int id);

        Task<bool> UpdateProcessenginestatusdeleteAsync(updatestatusdeleteDTO model, int cTenantID, string username);

        Task<int> InsertprocessmappingAsync(createprocessmappingDTO model, int cTenantID, string username);

        Task<bool> UpdateprocessmappingAsync(updateprocessmappingDTO model, int cTenantID, string username);

        Task<bool> DeleteprocessmappingAsync(int mappingId, int tenantId, string username);

        Task<List<MappingListDTO>> GetMappingListAsync(int cTenantID);

        Task<bool> UpdateProcessEngineAsync(UpdateProcessEngineDTO model, int cTenantID, string username);


        Task<List<GetProcessEngineDTO>> GetAllProcessengineAsyncnew(
 int cTenantID, string searchText = null, int page = 1, int pageSize = 10, string created_by = null, string priority = null, int? status = null);





    }

}
