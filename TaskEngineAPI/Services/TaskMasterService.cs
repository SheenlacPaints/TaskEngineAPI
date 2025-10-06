
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Options;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;

namespace TaskEngineAPI.Services

{
    public class TaskMasterService : ITaskMasterService
    {

        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;
        public TaskMasterService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository, IOptions<UploadSettings> uploadSettings)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
            _uploadSettings = uploadSettings.Value;
        }


        public async Task<int> InsertTaskMasterAsync(TaskMasterDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {

                        string taskNoQuery = @"SELECT ISNULL(MAX(CAST(itaskno AS INT)), 0) + 1 
                                               FROM tbl_taskflow_master WHERE ctenent_id = @TenantID";
                        int newTaskNo;
                        using (SqlCommand taskNoCmd = new SqlCommand(taskNoQuery, conn, transaction))
                        {
                            taskNoCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            var result = await taskNoCmd.ExecuteScalarAsync();
                            newTaskNo = result != null ? Convert.ToInt32(result) : 1; // fallback to 1 if null
                        }

                        using (SqlCommand taskNoCmd = new SqlCommand(taskNoQuery, conn, transaction))
                        {
                            taskNoCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            var result = await taskNoCmd.ExecuteScalarAsync();
                            newTaskNo = result != null ? Convert.ToInt32(result) : 1; // fallback to 1 if null
                        }


                        string queryMaster = @"
                INSERT INTO tbl_taskflow_master (
                    itaskno, ctenent_id, ctask_type, ctask_name, ctask_description, cstatus,  
                    ccreated_date, ccreated_by, cmodified_by, lmodified_date
                ) VALUES (
                   @itaskno,@TenantID, @ctask_type, @ctask_name, @ctask_description, @cstatus,
                    @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date
                );
                SELECT SCOPE_IDENTITY();";

                        int masterId;
                        using (SqlCommand cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@itaskno", newTaskNo);
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);                        
                            cmd.Parameters.AddWithValue("@ctask_type", (object?)model.ctask_type ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctask_name", (object?)model.ctask_name ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctask_description", (object?)model.ctask_description ?? DBNull.Value);                         
                            cmd.Parameters.AddWithValue("@cstatus", "Initiated");   // ✅ fixed
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ccreated_by", username);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }


                        string queryDetail = @"
                INSERT INTO tbl_taskflow_detail (
                    itaskno, iseqno, iheader_id, ctenent_id, ctask_type, cmapping_code, 
                    ccurrent_status, lcurrent_status_date, cremarks, inext_seqno, 
                  cnext_seqtype,cprevtype,csla
                ) VALUES (
                    @itaskno, @iseqno, @iheader_id, @ctenent_id, @ctask_type, @cmapping_code, 
                    @ccurrent_status, @lcurrent_status_date, @cremarks, @inext_seqno, 
                    @cnext_seqtype, @cprevtype,@csla);";

                        foreach (var detail in model.TaskDetailDTO)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdDetail.Parameters.AddWithValue("@iseqno", detail.iseqno);
                                cmdDetail.Parameters.AddWithValue("@iheader_id", masterId );
                                cmdDetail.Parameters.AddWithValue("@ctenent_id", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@ctask_type", detail.ctask_type ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cmapping_code", detail.cmapping_code ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ccurrent_status", detail.ccurrent_status ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@lcurrent_status_date", DateTime.Now);
                                cmdDetail.Parameters.AddWithValue("@cremarks", detail.cremarks ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@inext_seqno", detail.inext_seqno ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cnext_seqtype", detail.cnext_seqtype);
                                cmdDetail.Parameters.AddWithValue("@cprevtype", detail.cprevtype);
                                cmdDetail.Parameters.AddWithValue("@csla", detail.csla);
                                await cmdDetail.ExecuteNonQueryAsync();
                            }                                               
                        }

                        transaction.Commit();
                        return masterId;
                    }
                    catch
                    {

                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }




    }
}