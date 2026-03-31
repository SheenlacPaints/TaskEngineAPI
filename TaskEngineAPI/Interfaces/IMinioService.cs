namespace TaskEngineAPI.Interfaces
{
    public interface IMinioService
    {

        Task UploadFileAsync(IFormFile file);
        Task<(MemoryStream stream, string contentType)> GetFileAsync(string fileName);
        Task<(MemoryStream stream, string contentType)> GetuserFileAsync(string fileName, string type, int ctenantid);
        Task FileUploadFileAsync(IFormFile form, string type, int ctenantid);

        Task TaskFileUploadFileAsync(IFormFile form, string type, int ctenantid);
        Task ProjectFileUploadFileAsync(IFormFile form, string type, int ctenantid,  string id,string raiseby);
        Task<(MemoryStream stream, string contentType)> GetprojectFileAsync(string fileName, string type, int ctenantid, int projectid, string raisedby);


    }
}