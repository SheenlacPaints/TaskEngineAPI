using Microsoft.EntityFrameworkCore;
using TaskEngineAPI.Data;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Services
{
    public class RoleService : IRoleService
    {
        private readonly ApplicationDbContext _context;

        public RoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Role>> GetRolesAsync()
        {
            return await _context.Roles.ToListAsync();
        }

        public async Task<List<Role>> GetAllRolesAsync(int tenantId)
        {
            return await _context.Roles
                .Where(r => r.CTenant_Id == tenantId)
                .ToListAsync();
        }

        public async Task<Role> GetRoleByIdAsync(int id)
        {
            return await _context.Roles.FindAsync(id);
        }

        public async Task<Role> CreateRoleAsync(Role roleDto)
        {
            roleDto.CCreated_Date = DateTime.UtcNow;
            _context.Roles.Add(roleDto);
            await _context.SaveChangesAsync();
            return roleDto;
        }

        public async Task<Role> UpdateRoleAsync(int id, Role roleDto)
        {
            var existingRole = await _context.Roles.FindAsync(id);
            if (existingRole == null)
                return null;

            existingRole.CRole_Code = roleDto.CRole_Code;
            existingRole.CRole_Name = roleDto.CRole_Name;
            existingRole.NIs_Active = roleDto.NIs_Active;
            existingRole.CModified_By = roleDto.CModified_By;
            existingRole.LModified_Date = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return existingRole;
        }

        public async Task<bool> DeleteRoleAsync(int id)
        {
            var role = await _context.Roles.FindAsync(id);
            if (role == null)
                return false;

            _context.Roles.Remove(role);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
