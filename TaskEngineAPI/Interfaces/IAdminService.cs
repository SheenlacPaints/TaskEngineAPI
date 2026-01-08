using System.Net.Mail;
using System.Threading.Tasks;
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
        Task<int> InsertDepartmentsBulkAsync(List<BulkDepartmentDTO> departments, int cTenantID, string usernameClaim);
        Task<List<string>> CheckExistingRoleCodesAsync(List<string> roleCodes, int tenantId);
        Task<int> InsertRolesBulkAsync(List<BulkRoleDTO> roles, int tenantId, string username);
        Task<List<string>> CheckExistingPositionCodesAsync(List<string> roleCodes, int tenantId);
        Task<int> InsertPositionsBulkAsync(List<BulkPositionDTO> positions, int cTenantID, string usernameClaim);
        Task<List<string>> CheckExistingDepartmentCodesAsync(List<string> roleCodes, int tenantId);
        Task<bool> InsertUserApiAsync(UserApiDTO users, int cTenantID, string usernameClaim);
        Task<List<GetusersapisyncDTO>> GetAllAPISyncConfigAsync(int cTenantID, string searchText = null,string syncType = null,string apiMethod = null,bool? isActive = null);
        Task<bool> DeleteAPISyncConfigAsync(DeleteAPISyncConfigDTO model, int cTenantID, string username);
        Task<bool> UpdateAPISyncConfigAsync(UpdateAPISyncConfigDTO model, int cTenantID, string username);
        Task<GetAPISyncConfigByIDDTO> GetAPISyncConfigByIDAsync(int id, int cTenantID);
        Task<bool> UpdateAPISyncConfigActiveStatusAsync(int id, bool isActive, int tenantId, string username);
        Task<int> InsertDepartmentsAsync(List<DepartmentDTO> departments, int cTenantID, string usernameClaim);
        Task<int> InsertRolesAsync(List<RoleDTO> roles, int tenantId, string username);
        Task<int> InsertPositionsAsync(List<PositionDTO> positions, int cTenantID, string usernameClaim);
        Task<bool> CheckUserIdInUsersAsync(int cuserid, int ctenantID);
        Task<bool> CheckUserIdInAdminUsersAsync(int cuserid, int ctenantID);
        Task<int> CreateAsync(string name);
        Task<bool> UpdateAsync(int id, string name);
        Task<object> GetByIdAsync(int id);
        Task<bool> UpdatePasswordUserAsync(UpdateUserPasswordDTO model, int cTenantID, string usernameClaim);


        //public async Task<int> InsertUsersBulkAsync(List<CreateUserDTO> users)
        //{
        //    // your bulk insert logic here
        //    return insertedCount; // make sure you return an int
        //}

    }

}
