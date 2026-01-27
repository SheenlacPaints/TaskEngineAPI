using System.Data.SqlClient;
using System.Data;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using Newtonsoft.Json;
using System.Net.Mail;
using Microsoft.Extensions.Options;
namespace TaskEngineAPI.Services
{
    public class AnalyticalService: IAnalyticalService
    {
        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;
        public AnalyticalService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository, IOptions<UploadSettings> uploadSettings)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
            _uploadSettings = uploadSettings.Value;
        }

        public async Task<int> InsertAnalyticalhubAsync(AnalyticalDTO model, int tenantId, string userName)
        {
            int masterId = 0;
            var connectionString = _config.GetConnectionString("Database");
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string queryMaster = @"
                    INSERT INTO [tbl_analytical_workspace] (ctenant_id,canalyticalname,canalyticalDescription,canalyticalprompt,capi_method,
                    capi_url,capi_params,capi_headers,cbody,nis_active,ccreated_by,lcreated_date,cmodified_by,lmodified_date) 
                     VALUES (@ctenant_id,@canalyticalname,@canalyticalDescription,@canalyticalprompt,@capi_method,@capi_url,@capi_params 
                     @capi_headers,@cbody,@nis_active,@ccreated_by,@lcreated_date,@cmodified_by,@lmodified_date );SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@ctenant_id", tenantId);
                            cmd.Parameters.AddWithValue("@canalyticalname", (object?)model.canalyticalname ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@canalyticalDescription", (object?)model.canalyticalDescription ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@canalyticalprompt", (object?)model.canalyticalprompt ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@capi_method", (object?)model.capi_method ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@capi_url", (object?)model.capi_url ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@capi_params", (object?)model.capi_params ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@capi_headers", (object?)model.capi_headers ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cbody", (object?)model.@cbody ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nis_active", (object?)model.@nis_active ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ccreated_by", userName);
                            cmd.Parameters.AddWithValue("@lcreated_date", DateTime.Now);                     
                            cmd.Parameters.AddWithValue("@cmodified_by", userName);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);  
                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            return masterId;
        }






    }
}
