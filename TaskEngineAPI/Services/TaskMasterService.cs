
using System.Data;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
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
            int masterId = 0;

            using (var conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {                    
                        string taskNoQuery = @"
                    SELECT ISNULL(MAX(TRY_CAST(itaskno AS INT)), 0) + 1 
                    FROM tbl_taskflow_master 
                    WHERE ctenent_id = @TenantID";

                        int newTaskNo;
                        using (var taskNoCmd = new SqlCommand(taskNoQuery, conn, transaction))
                        {
                            taskNoCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            var result = await taskNoCmd.ExecuteScalarAsync();
                            newTaskNo = result != null ? Convert.ToInt32(result) : 1;
                        }

                        string queryMaster = @"
                    INSERT INTO tbl_taskflow_master (
                        itaskno, ctenent_id, ctask_type, ctask_name, ctask_description, cstatus,  
                        lcreated_date, ccreated_by, cmodified_by, lmodified_date
                    ) VALUES (
                        @itaskno, @TenantID, @ctask_type, @ctask_name, @ctask_description, @cstatus,
                        @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date
                    );
                    SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@itaskno", newTaskNo);
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@ctask_type", (object?)model.ctask_type ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctask_name", (object?)model.ctask_name ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctask_description", (object?)model.ctask_description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", "Initiated");
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ccreated_by", username);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }
                     
                        string selectQuery = @"
                    SELECT ctenentid, cprocesscode, ciseqno, cseq_order, cactivitycode, 
                           cactivitydescription, ctasktype, cprevstep, cactivityname, cnextseqno 
                    FROM tbl_process_engine_details 
                    WHERE cprocesscode = @cprocesscode AND ctenentid = @ctenent_id";

                        var detailRows = new List<Dictionary<string, object>>();

                        using (var cmdSelect = new SqlCommand(selectQuery, conn, transaction))
                        {
                            cmdSelect.Parameters.AddWithValue("@cprocesscode", model.ctask_name);
                            cmdSelect.Parameters.AddWithValue("@ctenent_id", cTenantID);

                            using (var reader = await cmdSelect.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var row = new Dictionary<string, object>
                                    {
                                        ["ciseqno"] = reader["ciseqno"],
                                        ["ctenentid"] = reader["ctenentid"],
                                        ["ctasktype"] = reader["ctasktype"],
                                        ["cprocesscode"] = reader["cprocesscode"],
                                        ["cnextseqno"] = reader["cnextseqno"],
                                        ["cprevstep"] = reader["cprevstep"]
                                    };
                                    detailRows.Add(row);
                                }
                            }
                        }                    
                        string queryDetail = @"
                    INSERT INTO tbl_taskflow_detail (
                        itaskno, iseqno, iheader_id, ctenent_id, ctask_type, cmapping_code, 
                        ccurrent_status, lcurrent_status_date, cremarks, inext_seqno, 
                        cnext_seqtype, cprevtype, csla
                    ) VALUES (
                        @itaskno, @iseqno, @iheader_id, @ctenent_id, @ctask_type, @cmapping_code, 
                        @ccurrent_status, @lcurrent_status_date, @cremarks, @inext_seqno, 
                        @cnext_seqtype, @cprevtype, @csla);";

                        foreach (var row in detailRows)
                        {
                            using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdInsert.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdInsert.Parameters.AddWithValue("@iseqno", row["ciseqno"]);
                                cmdInsert.Parameters.AddWithValue("@iheader_id", masterId);
                                cmdInsert.Parameters.AddWithValue("@ctenent_id", row["ctenentid"]);
                                cmdInsert.Parameters.AddWithValue("@ctask_type", row["ctasktype"]);
                                cmdInsert.Parameters.AddWithValue("@cmapping_code", row["cprocesscode"]);
                                cmdInsert.Parameters.AddWithValue("@ccurrent_status", "P");
                                cmdInsert.Parameters.AddWithValue("@lcurrent_status_date", DateTime.Now);
                                cmdInsert.Parameters.AddWithValue("@cremarks", DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@inext_seqno", row["cnextseqno"]);
                                cmdInsert.Parameters.AddWithValue("@cnext_seqtype", DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@cprevtype", row["cprevstep"]);
                                cmdInsert.Parameters.AddWithValue("@csla", DBNull.Value);

                                await cmdInsert.ExecuteNonQueryAsync();
                            }
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



        public async Task<string> GetAllProcessmetaAsync(int cTenantID, string processcode)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_Process_meta", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                    cmd.Parameters.AddWithValue("@processcode", processcode);
                    var ds = new DataSet();
                    var adapter = new SqlDataAdapter(cmd);
                    await Task.Run(() => adapter.Fill(ds)); // async wrapper

                    if (ds.Tables.Count > 0)
                    {
                        return JsonConvert.SerializeObject(ds.Tables[0], Formatting.Indented);
                    }

                    return "[]";
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }


    }
}




   
