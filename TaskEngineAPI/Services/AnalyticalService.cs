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
using System.Collections.Generic;
using TaskEngineAPI.Controllers;
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
                    INSERT INTO [tbl_analytical_workspace] (ctenant_id,canalyticalname,canalyticalDescription
                    ,canalyticalprompt,capi_method,capi_url,capi_params,capi_headers,capi_response,cbody,cbusiness_function,cstatus
                    ,csource_type,cdataset_id,crefresh_mode,ctime_range,cdescriptive_analytics,cpredictive_analytics,cdiagnostic_analytics
                    ,cprescriptive_analytics,canalysis_depth,cexplanation_tone,cauto_follow_up,nmax_row_limit,nquery_depth_limit
                    ,callowed_join_types,cread_only_mode,caudit_logging,callowed_roles,cmasking_rule,cdefault_chart_type
                    ,ccolor_scheme,cexport_excel,cexport_pdf,cexport_csv,cexport_json,cexport_png,cenable_drill_down
                    ,cshow_data_labels,cenable_animations,ccolumn_mappings,nis_active,ccreated_by,lcreated_date,cmodified_by,lmodified_date)
                      VALUES(@ctenant_id,@canalyticalname,@canalyticalDescription
                    ,@canalyticalprompt,@capi_method,@capi_url,@capi_params,@capi_headers,@capi_response,@cbody,@cbusiness_function,@cstatus
                    ,@csource_type,@cdataset_id,@crefresh_mode,@ctime_range,@cdescriptive_analytics,@cpredictive_analytics,@cdiagnostic_analytics
                    ,@cprescriptive_analytics,@canalysis_depth,@cexplanation_tone,@cauto_follow_up,@nmax_row_limit,@nquery_depth_limit
                    ,@callowed_join_types,@cread_only_mode,@caudit_logging,@callowed_roles,@cmasking_rule,@cdefault_chart_type
                    ,@ccolor_scheme,@cexport_excel,@cexport_pdf,@cexport_csv,@cexport_json,@cexport_png,@cenable_drill_down
                    ,@cshow_data_labels,@cenable_animations,@ccolumn_mappings,@nis_active,@ccreated_by,@lcreated_date,@cmodified_by,@lmodified_date);SELECT SCOPE_IDENTITY();";

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
                            cmd.Parameters.AddWithValue("@capi_response", (object?)model.capi_response ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cbody", (object?)model.cbody ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cbusiness_function", (object?)model.cbusiness_function ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", (object?)model.cstatus ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@csource_type", (object?)model.csource_type ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cdataset_id", (object?)model.cdataset_id ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@crefresh_mode", (object?)model.crefresh_mode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctime_range", (object?)model.ctime_range ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cdescriptive_analytics", (object?)model.cdescriptive_analytics ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cpredictive_analytics", (object?)model.cpredictive_analytics ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cdiagnostic_analytics", (object?)model.cdiagnostic_analytics ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprescriptive_analytics", (object?)model.cprescriptive_analytics ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@canalysis_depth", (object?)model.canalysis_depth ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cexplanation_tone", (object?)model.cexplanation_tone ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cauto_follow_up", (object?)model.cauto_follow_up ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nmax_row_limit", (object?)model.nmax_row_limit ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nquery_depth_limit", (object?)model.nquery_depth_limit ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@callowed_join_types", (object?)model.callowed_join_types ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cread_only_mode", (object?)model.cread_only_mode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@caudit_logging", (object?)model.caudit_logging ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@callowed_roles", (object?)model.callowed_roles ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cmasking_rule", (object?)model.cmasking_rule ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cdefault_chart_type", (object?)model.cdefault_chart_type ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ccolor_scheme", (object?)model.ccolor_scheme ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cexport_excel", (object?)model.cexport_excel ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cexport_pdf", (object?)model.cexport_pdf ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cexport_csv", (object?)model.cexport_csv ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cexport_json", (object?)model.cexport_json ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cexport_png", (object?)model.cexport_png ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cenable_drill_down", (object?)model.cenable_drill_down ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cshow_data_labels", (object?)model.cshow_data_labels ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cenable_animations", (object?)model.cenable_animations ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ccolumn_mappings", (object?)model.ccolumn_mappings ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nis_active", (object?)model.nis_active ?? DBNull.Value);
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
                                    ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                    ctenant_id = sdr.IsDBNull(sdr.GetOrdinal("ctenant_id")) ? 0 : Convert.ToInt32(sdr["ctenant_id"]),
                                    canalyticalname = sdr.IsDBNull(sdr.GetOrdinal("canalyticalname")) ? string.Empty : Convert.ToString(sdr["canalyticalname"]),
                                    canalyticalDescription = sdr.IsDBNull(sdr.GetOrdinal("canalyticalDescription")) ? string.Empty : Convert.ToString(sdr["canalyticalDescription"]),
                                    canalyticalprompt = sdr.IsDBNull(sdr.GetOrdinal("canalyticalprompt")) ? string.Empty : Convert.ToString(sdr["canalyticalprompt"]),
                                    capi_method = sdr.IsDBNull(sdr.GetOrdinal("capi_method")) ? string.Empty : Convert.ToString(sdr["capi_method"]),
                                    capi_url = sdr.IsDBNull(sdr.GetOrdinal("capi_url")) ? string.Empty : Convert.ToString(sdr["capi_url"]),
                                    capi_params = sdr.IsDBNull(sdr.GetOrdinal("capi_params")) ? string.Empty : Convert.ToString(sdr["capi_params"]),
                                    capi_headers = sdr.IsDBNull(sdr.GetOrdinal("capi_headers")) ? string.Empty : Convert.ToString(sdr["capi_headers"]),
                                    cbody = sdr.IsDBNull(sdr.GetOrdinal("cbody")) ? string.Empty : Convert.ToString(sdr["cbody"]),
                                    cstatus = sdr.IsDBNull(sdr.GetOrdinal("cstatus")) ? string.Empty : Convert.ToString(sdr["cstatus"]),
                                    cbusiness_function = sdr.IsDBNull(sdr.GetOrdinal("cbusiness_function")) ? string.Empty : Convert.ToString(sdr["cbusiness_function"]),
                                    nis_active = sdr.IsDBNull(sdr.GetOrdinal("nis_active")) ? false : Convert.ToBoolean(sdr["nis_active"]),
                                    ccreated_by = sdr.IsDBNull(sdr.GetOrdinal("ccreated_by")) ? string.Empty : Convert.ToString(sdr["ccreated_by"]),
                                    lcreated_date = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                    cmodified_by = sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                    lmodified_date = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date"))
                                
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
                    data = new List<GetAnalyticalDTO>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }



        public async Task<List<GetAnalyticalbyidDTO>> GetAnalyticalhubbtid(int cTenantID, int id)
        {
            List<GetAnalyticalbyidDTO> tsk = new List<GetAnalyticalbyidDTO>();
            
            try
            {
                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_analytical_data", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@tenantid", cTenantID);
                        cmd.Parameters.AddWithValue("@id", id);
                     

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
                            {
                                GetAnalyticalbyidDTO p = new GetAnalyticalbyidDTO
                                {
                                    ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                    ctenant_id = sdr.IsDBNull(sdr.GetOrdinal("ctenant_id")) ? 0 : Convert.ToInt32(sdr["ctenant_id"]),
                                    canalyticalname = sdr.IsDBNull(sdr.GetOrdinal("canalyticalname")) ? string.Empty : Convert.ToString(sdr["canalyticalname"]),
                                    canalyticalDescription = sdr.IsDBNull(sdr.GetOrdinal("canalyticalDescription")) ? string.Empty : Convert.ToString(sdr["canalyticalDescription"]),
                                    canalyticalprompt = sdr.IsDBNull(sdr.GetOrdinal("canalyticalprompt")) ? string.Empty : Convert.ToString(sdr["canalyticalprompt"]),
                                    capi_method = sdr.IsDBNull(sdr.GetOrdinal("capi_method")) ? string.Empty : Convert.ToString(sdr["capi_method"]),
                                    capi_url = sdr.IsDBNull(sdr.GetOrdinal("capi_url")) ? string.Empty : Convert.ToString(sdr["capi_url"]),
                                    capi_params = sdr.IsDBNull(sdr.GetOrdinal("capi_params")) ? string.Empty : Convert.ToString(sdr["capi_params"]),
                                    capi_headers = sdr.IsDBNull(sdr.GetOrdinal("capi_headers")) ? string.Empty : Convert.ToString(sdr["capi_headers"]),
                                    capi_response = sdr.IsDBNull(sdr.GetOrdinal("capi_response")) ? string.Empty : Convert.ToString(sdr["capi_response"]),
                                    cbody = sdr.IsDBNull(sdr.GetOrdinal("cbody")) ? string.Empty : Convert.ToString(sdr["cbody"]),
                                    cstatus = sdr.IsDBNull(sdr.GetOrdinal("cstatus")) ? string.Empty : Convert.ToString(sdr["cstatus"]),
                                    cbusiness_function = sdr.IsDBNull(sdr.GetOrdinal("cbusiness_function")) ? string.Empty : Convert.ToString(sdr["cbusiness_function"]),
                                    csource_type = sdr.IsDBNull(sdr.GetOrdinal("csource_type")) ? string.Empty : Convert.ToString(sdr["csource_type"]),
                                    cdataset_id = sdr.IsDBNull(sdr.GetOrdinal("cdataset_id")) ? 0 : Convert.ToInt32(sdr["cdataset_id"]),
                                    crefresh_mode = sdr.IsDBNull(sdr.GetOrdinal("crefresh_mode")) ? string.Empty : Convert.ToString(sdr["crefresh_mode"]),
                                    ctime_range = sdr.IsDBNull(sdr.GetOrdinal("ctime_range")) ? string.Empty : Convert.ToString(sdr["ctime_range"]),
                                    cdescriptive_analytics = sdr.IsDBNull(sdr.GetOrdinal("cdescriptive_analytics")) ? false : Convert.ToBoolean(sdr["cdescriptive_analytics"]),
                                    cpredictive_analytics = sdr.IsDBNull(sdr.GetOrdinal("cpredictive_analytics")) ? false : Convert.ToBoolean(sdr["cpredictive_analytics"]),
                                    cdiagnostic_analytics = sdr.IsDBNull(sdr.GetOrdinal("cdiagnostic_analytics")) ? false : Convert.ToBoolean(sdr["cdiagnostic_analytics"]),
                                    cprescriptive_analytics = sdr.IsDBNull(sdr.GetOrdinal("cprescriptive_analytics")) ? false : Convert.ToBoolean(sdr["cprescriptive_analytics"]),
                                    canalysis_depth = sdr.IsDBNull(sdr.GetOrdinal("canalysis_depth")) ? string.Empty : Convert.ToString(sdr["canalysis_depth"]),
                                    cexplanation_tone = sdr.IsDBNull(sdr.GetOrdinal("cexplanation_tone")) ? string.Empty : Convert.ToString(sdr["cexplanation_tone"]),
                                    cauto_follow_up = sdr.IsDBNull(sdr.GetOrdinal("cauto_follow_up")) ? string.Empty : Convert.ToString(sdr["cauto_follow_up"]),
                                    nmax_row_limit = sdr.IsDBNull(sdr.GetOrdinal("nmax_row_limit")) ? 0 : Convert.ToInt32(sdr["nmax_row_limit"]),
                                    nquery_depth_limit = sdr.IsDBNull(sdr.GetOrdinal("nquery_depth_limit")) ? 0 : Convert.ToInt32(sdr["nquery_depth_limit"]),
                                    callowed_join_types = sdr.IsDBNull(sdr.GetOrdinal("callowed_join_types")) ? string.Empty : Convert.ToString(sdr["callowed_join_types"]),
                                    cread_only_mode = sdr.IsDBNull(sdr.GetOrdinal("cread_only_mode")) ? false : Convert.ToBoolean(sdr["cread_only_mode"]),
                                    caudit_logging = sdr.IsDBNull(sdr.GetOrdinal("ctime_range")) ? string.Empty : Convert.ToString(sdr["ctime_range"]),
                                    callowed_roles = sdr.IsDBNull(sdr.GetOrdinal("callowed_roles")) ? string.Empty : Convert.ToString(sdr["callowed_roles"]),
                                    cmasking_rule = sdr.IsDBNull(sdr.GetOrdinal("cmasking_rule")) ? string.Empty : Convert.ToString(sdr["cmasking_rule"]),
                                    cdefault_chart_type = sdr.IsDBNull(sdr.GetOrdinal("cdefault_chart_type")) ? string.Empty : Convert.ToString(sdr["cdefault_chart_type"]),
                                    ccolor_scheme = sdr.IsDBNull(sdr.GetOrdinal("ccolor_scheme")) ? string.Empty : Convert.ToString(sdr["ccolor_scheme"]),
                                    cexport_excel = sdr.IsDBNull(sdr.GetOrdinal("cexport_excel")) ? false : Convert.ToBoolean(sdr["cexport_excel"]),
                                    cexport_pdf = sdr.IsDBNull(sdr.GetOrdinal("cexport_pdf")) ? false : Convert.ToBoolean(sdr["cexport_pdf"]),
                                    cexport_csv = sdr.IsDBNull(sdr.GetOrdinal("cexport_csv")) ? false : Convert.ToBoolean(sdr["cexport_csv"]),
                                    cexport_json = sdr.IsDBNull(sdr.GetOrdinal("cexport_json")) ? false : Convert.ToBoolean(sdr["cexport_json"]),
                                    cexport_png = sdr.IsDBNull(sdr.GetOrdinal("cexport_png")) ? false : Convert.ToBoolean(sdr["cexport_png"]),
                                    cenable_drill_down = sdr.IsDBNull(sdr.GetOrdinal("cenable_drill_down")) ? false : Convert.ToBoolean(sdr["cenable_drill_down"]),
                                    cshow_data_labels = sdr.IsDBNull(sdr.GetOrdinal("cshow_data_labels")) ? false : Convert.ToBoolean(sdr["cshow_data_labels"]),
                                    cenable_animations = sdr.IsDBNull(sdr.GetOrdinal("cenable_animations")) ? false : Convert.ToBoolean(sdr["cenable_animations"]),
                                    ccolumn_mappings = sdr.IsDBNull(sdr.GetOrdinal("ccolumn_mappings")) ? string.Empty : Convert.ToString(sdr["ccolumn_mappings"]),
                                    nis_active = sdr.IsDBNull(sdr.GetOrdinal("nis_active")) ? false : Convert.ToBoolean(sdr["nis_active"]),
                                    ccreated_by = sdr.IsDBNull(sdr.GetOrdinal("ccreated_by")) ? string.Empty : Convert.ToString(sdr["ccreated_by"]),
                                    lcreated_date = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                    cmodified_by = sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                    lmodified_date = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date"))

                                };
                                tsk.Add(p);
                            }
                        }
                    }
                }
                return tsk;
                

            }
            catch (Exception ex)
            {

                return new List<GetAnalyticalbyidDTO>();
            }
        }



    }
}
