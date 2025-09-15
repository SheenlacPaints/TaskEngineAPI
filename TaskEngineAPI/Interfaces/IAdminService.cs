using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;

namespace TaskEngineAPI.Interfaces
{  
        public interface IAdminService
        {
            Task<APIResponse> CreateSuperAdminAsync(CreateAdminDTO model);
       
        Task<int> InsertSuperAdminAsync(CreateAdminDTO model);

   
        Task<List<AdminUserDTO>> GetAllSuperAdminsAsync(int cTenantID);


    }

}
