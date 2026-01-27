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


        public async Task<string> GetAnalyticalhub(int cTenantID, string username, string? type, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetAnalyticalDTO> tsk = new List<GetAnalyticalDTO>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_analytical_workspace", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@tenantid", cTenantID);
                        cmd.Parameters.AddWithValue("@cuserid", username);
                        cmd.Parameters.AddWithValue("@searchtxt", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@type", type ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
                            {
                                GetAnalyticalDTO p = new GetAnalyticalDTO
                                {
                                    ID=sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                    ctenant_id= sdr.IsDBNull(sdr.GetOrdinal("ctenant_id")) ? 0 : Convert.ToInt32(sdr["ctenant_id"]),
                                    canalyticalname = sdr.IsDBNull(sdr.GetOrdinal("canalyticalname")) ? string.Empty : Convert.ToString(sdr["canalyticalname"]),
                                    canalyticalDescription = sdr.IsDBNull(sdr.GetOrdinal("canalyticalDescription")) ? string.Empty : Convert.ToString(sdr["canalyticalDescription"]),
                                    canalyticalprompt = sdr.IsDBNull(sdr.GetOrdinal("canalyticalprompt")) ? string.Empty : Convert.ToString(sdr["canalyticalprompt"]),
                                    capi_method = sdr.IsDBNull(sdr.GetOrdinal("capi_method")) ? string.Empty : Convert.ToString(sdr["capi_method"]),
                                    capi_url = sdr.IsDBNull(sdr.GetOrdinal("capi_url")) ? string.Empty : Convert.ToString(sdr["capi_url"]),
                                    capi_params = sdr.IsDBNull(sdr.GetOrdinal("capi_params")) ? string.Empty : Convert.ToString(sdr["capi_params"]),
                                    capi_headers = sdr.IsDBNull(sdr.GetOrdinal("capi_headers")) ? string.Empty : Convert.ToString(sdr["capi_headers"]),
                                    cbody = sdr.IsDBNull(sdr.GetOrdinal("cbody")) ? string.Empty : Convert.ToString(sdr["cbody"]),                                 
                                    nis_active = sdr.IsDBNull(sdr.GetOrdinal("nis_active")) ? false : Convert.ToBoolean(sdr["nis_active"]),
                                    ccreated_by = sdr.IsDBNull(sdr.GetOrdinal("ccreated_by")) ? string.Empty : Convert.ToString(sdr["ccreated_by"]),
                                    lcreated_date= sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                    cmodified_by= sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                    lmodified_date= sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date"))                        
                                };
                                tsk.Add(p);
                            }

                            if (await sdr.NextResultAsync())
                            {
                                if (await sdr.ReadAsync())
                                {
                                    totalCount = sdr.IsDBNull(0) ? 0 : Convert.ToInt32(sdr[0]);
                                }
                            }
                        }
                    }
                }

                var response = new
                {
                    totalCount = totalCount,
                    data = tsk
                };

                return JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            catch (Exception ex)
            {

                var errorResponse = new
                {
                    totalCount = 0,
                    data = new List<GetProjectList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }




    }
}
