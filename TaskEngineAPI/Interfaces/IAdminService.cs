using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;

namespace TaskEngineAPI.Interfaces
{  
        public interface IAdminService
        {
            Task<APIResponse> CreateSuperAdminAsync(CreateAdminDTO model);
       
        Task<int> InsertSuperAdminAsync(CreateAdminDTO model);

   
        Task<List<AdminUserDTO>> GetAllSuperAdminsAsync(int cTenantID);

        Task<bool> UpdateSuperAdminAsync(UpdateAdminDTO model);
        Task<bool> DeleteSuperAdminAsync(DeleteAdminDTO model, int cTenantID);

        Task<int> InsertUserAsync(CreateUserDTO model);

        Task<bool> UpdateUserAsync(UpdateUserDTO model, int cTenantID);

        Task<List<GetUserDTO>> GetAllUserAsync(int cTenantID);

        Task<List<GetUserDTO>> GetAllUserIdAsync(int cTenantID,int userid);

        Task<bool> CheckEmailExistsAsync(string email, int tenantId);

        Task<bool> CheckUsernameExistsAsync(string username, int tenantId);

        Task<bool> CheckPhenonoExistsAsync(string phoneno, int tenantId);


        Task<bool> CheckuserUsernameExistsAsync(string username, int tenantId);


        Task<bool> CheckuserEmailExistsAsync(string email, int tenantId);

        Task<bool> CheckuserPhonenoExistsAsync(string phoneno, int tenantId);
    }

}
