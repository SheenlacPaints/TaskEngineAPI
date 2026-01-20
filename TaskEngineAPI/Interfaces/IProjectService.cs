using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
    public interface IProjectService
    {       
        Task<int> InsertProjectMasterAsync(CreateProjectDTO model, int tenantId, string userName);
    }
}
