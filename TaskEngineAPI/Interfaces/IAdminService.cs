using System.Net.Mail;
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
        Task<bool> DeleteSuperAdminAsync(DeleteAdminDTO model, int cTenantID,string username);
        Task<int> InsertUserAsync(CreateUserDTO model);
        Task<bool> UpdateUserAsync(UpdateUserDTO model, int cTenantID);    
        Task<List<GetUserDTO>> GetAllUserAsync(int cTenantID);
        Task<List<GetUserDTO>> GetAllUserIdAsync(int cTenantID,int userid);
        Task<bool> CheckEmailExistsAsync(string email, int tenantId);
        Task<bool> CheckUsernameExistsAsync(int cuserid, int tenantId);
        Task<bool> CheckPhenonoExistsAsync(string phoneno, int tenantId);
        Task<bool> CheckuserUsernameExistsAsync(int cuserid, int tenantId);
        Task<bool> CheckuserEmailExistsAsync(string email, int tenantId);
        Task<bool> CheckuserPhonenoExistsAsync(string phoneno, int tenantId);
        Task<bool> UpdatePasswordSuperAdminAsync(UpdateadminPassword model, int tenantId, string username);
        Task<bool> CheckuserUsernameExistsputAsync(string username, int tenantId,int cuserid);
        Task<bool> CheckuserEmailExistsputAsync(string email, int tenantId,int cuserid);
        Task<bool> CheckuserPhonenoExistsputAsync(string phoneno, int tenantId, int cuserid);
        Task<bool> DeleteuserAsync(DeleteuserDTO model, int cTenantID, string username);
        Task<int> InsertUsersBulkAsync(List<BulkUserDTO> model,int cTenantID,string usernameClaim);

        Task<bool> InsertusersapisyncconfigAsync(usersapisyncDTO model, int cTenantID, string username);



        //public async Task<int> InsertUsersBulkAsync(List<CreateUserDTO> users)
        //{
        //    // your bulk insert logic here
        //    return insertedCount; // make sure you return an int
        //}

    }

}
