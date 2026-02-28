namespace TaskEngineAPI.Interfaces
{
   
    public interface IApiProxyService
    {
       
        Task<string> ExecuteIntegrationApi(APIFetchDTO model, int tenantId, string username);
    }

}
