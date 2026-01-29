using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
    public interface IProjectService
    {       
        Task<int> InsertProjectMasterAsync(CreateProjectDTO model, int tenantId, string userName);

        Task<string> Getprojectmaster(int cTenantID, string username, string? type, string? searchText = null, int page = 1, int pageSize = 50);
        Task<string> Getprojectdropdown(int cTenantID, string username, string? type, string? searchText = null);
        Task<bool> InsertProjectDetails(List<ProjectDetailRequest> requests, int tenantId, string username);
        Task<bool> UpdateProjectDetails(ProjectDetailRequest request, int tenantId, string username);

    }
}
