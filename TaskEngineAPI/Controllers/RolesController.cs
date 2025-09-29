using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetRoles()
        {
            var roles = await _roleService.GetRolesAsync();
            return Ok(roles);
        }


        [HttpGet("tenant/{tenantId}")]
        public async Task<ActionResult<IEnumerable<Role>>> GetRolesByTenant(int tenantId)
        {
            var roles = await _roleService.GetAllRolesAsync(tenantId);
            return Ok(roles);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<Role>> GetRole(int id)
        {
            var role = await _roleService.GetRoleByIdAsync(id);
            if (role == null)
                return NotFound();

            return Ok(role);
        }

        [HttpPost]
        public async Task<ActionResult<Role>> PostRole(Role role)
        {
            var newRole = await _roleService.CreateRoleAsync(role);
            return CreatedAtAction(nameof(GetRole), new { id = newRole.CRole_Id }, newRole);
        }


        [HttpPut("{id}")]
        public async Task<IActionResult> PutRole(int id, Role role)
        {
            var updatedRole = await _roleService.UpdateRoleAsync(id, role);
            if (updatedRole == null)
                return NotFound();

            return Ok(updatedRole);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteRole(int id)
        {
            var deleted = await _roleService.DeleteRoleAsync(id);
            if (!deleted)
                return NotFound();

            return NoContent();
        }
    }
}
