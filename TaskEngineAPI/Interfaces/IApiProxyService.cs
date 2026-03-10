using TaskEngineAPI.DTO;

namespace TaskEngineAPI.Interfaces
{
   
    public interface IApiProxyService
    {
       
        Task<string> ExecuteIntegrationApi(APIFetchDTO model, int tenantId, string username);
        Task<string> BoardExecuteIntegrationApi(BoardAPIFetchDTO model, int tenantId, string username);
    }

}
