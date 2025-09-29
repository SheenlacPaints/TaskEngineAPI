using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Interfaces
{
    public interface IRoleService
    {
        Task<List<Role>> GetRolesAsync();             
        Task<List<Role>> GetAllRolesAsync(int tenantId); 
        Task<Role> GetRoleByIdAsync(int id);
        Task<Role> CreateRoleAsync(Role roleDto);
        Task<Role> UpdateRoleAsync(int id, Role roleDto);
        Task<bool> DeleteRoleAsync(int id);
    }
}
