using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
    public interface IProjectService
    {       
        Task<int> InsertProjectMasterAsync(CreateProjectDTO model, int tenantId, string userName);

        Task<string> Getprojectmaster(int cTenantID, string username, string? type, string? searchText = null, int page = 1, int pageSize = 50, int? projectid =0,string? versionid = null);
        Task<string> Getprojectdropdown(int cTenantID, string username, string? type, string? searchText = null);
        Task<bool> InsertProjectDetails(List<ProjectDetailRequest> requests,int tenantId,string username);
        Task<bool> UpdateProjectDetails(ProjectDetailRequest request, int tenantId, string username);
        Task<string> GetProjectById(int tenantId, string username, int projectId);
        Task<int> CreateProjectVersionAsync(int projectId, string description, DateTime? expectedDate, string username);
        Task<string> GetProjectList(int tenantId, string username);

    }
}
