
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO.LookUpDTO;
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


        //public async Task<int> InsertTaskMasterAsync(TaskMasterDTO model, int cTenantID, string username)
        //{
        //    int masterId = 0;
        //    int detailId = 0;
        //    using (var conn = new SqlConnection(_config.GetConnectionString("Database")))
        //    {
        //        await conn.OpenAsync();

        //        using (var transaction = conn.BeginTransaction())
        //        {
        //            try
        //            {
        //                string taskNoQuery = @"
        //            SELECT ISNULL(MAX(TRY_CAST(itaskno AS INT)), 0) + 1 
        //            FROM tbl_taskflow_master 
        //            WHERE ctenant_id = @TenantID";

        //                int newTaskNo;
        //                using (var taskNoCmd = new SqlCommand(taskNoQuery, conn, transaction))
        //                {
        //                    taskNoCmd.Parameters.AddWithValue("@TenantID", cTenantID);
        //                    var result = await taskNoCmd.ExecuteScalarAsync();
        //                    newTaskNo = result != null ? Convert.ToInt32(result) : 1;
        //                }

        //                string queryMaster = @"
        //            INSERT INTO tbl_taskflow_master (
        //                itaskno, ctenant_id, ctask_type, ctask_name, ctask_description, cstatus,  
        //                lcreated_date, ccreated_by, cmodified_by, lmodified_date,cprocess_id) VALUES (
        //                @itaskno, @TenantID, @ctask_type, @ctask_name, @ctask_description, @cstatus,
        //                @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cprocess_id );SELECT SCOPE_IDENTITY();";
        //                using (var cmd = new SqlCommand(queryMaster, conn, transaction))
        //                {
        //                    cmd.Parameters.AddWithValue("@itaskno", newTaskNo);
        //                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);
        //                    cmd.Parameters.AddWithValue("@ctask_type", (object?)model.ctask_type ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@ctask_name", (object?)model.ctask_name ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@ctask_description", (object?)model.ctask_description ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cstatus", "Initiated");
        //                    cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@ccreated_by", username);
        //                    cmd.Parameters.AddWithValue("@cmodified_by", username);
        //                    cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocess_id ?? DBNull.Value);
        //                    var newId = await cmd.ExecuteScalarAsync();
        //                    masterId = newId != null ? Convert.ToInt32(newId) : 0;
        //                }

        //                string selectQuery = @"                     
        //                 SELECT ctenant_id, cprocesscode, ciseqno,cactivitycode, 
        //                 cactivity_description, ctask_type, cprev_step, cactivityname, cnext_seqno,nboard_enabled,cmapping_code,
        //                 cparticipant_type,csla_day,csla_Hour,caction_privilege,crejection_privilege,cmapping_type
        //                 FROM tbl_process_engine_details 
        //                 WHERE cheader_id = @cprocesscode AND ctenant_id = @ctenent_id";

        //                var detailRows = new List<Dictionary<string, object>>();

        //                using (var cmdSelect = new SqlCommand(selectQuery, conn, transaction))
        //                {
        //                    cmdSelect.Parameters.AddWithValue("@cprocesscode", model.cprocess_id);
        //                    cmdSelect.Parameters.AddWithValue("@ctenent_id", cTenantID);

        //                    using (var reader = await cmdSelect.ExecuteReaderAsync())
        //                    {
        //                        while (await reader.ReadAsync())
        //                        {
        //                            var row = new Dictionary<string, object>
        //                            {
        //                                ["ciseqno"] = reader["ciseqno"],
        //                                ["ctenantid"] = reader["ctenant_id"],
        //                                ["ctasktype"] = reader["ctask_type"],
        //                                ["cprocesscode"] = reader["cprocesscode"],
        //                                ["cnextseqno"] = reader["cnext_seqno"],
        //                                ["cprevstep"] = reader["cprev_step"],
        //                                ["cmapping_code"] = reader["cmapping_code"],
        //                                ["nboard_enabled"] = reader["nboard_enabled"],
        //                                ["cparticipant_type"] = reader["cparticipant_type"],
        //                                ["csla_day"] = reader["csla_day"],
        //                                ["csla_Hour"] = reader["csla_Hour"],
        //                                ["caction_privilege"] = reader["caction_privilege"],
        //                                ["crejection_privilege"] = reader["crejection_privilege"],
        //                                ["cmapping_type"] = reader["cmapping_type"]

        //                            };
        //                            detailRows.Add(row);
        //                        }
        //                    }
        //                }


        //                string queryDetail = @"INSERT INTO tbl_taskflow_detail (
        //                itaskno, iseqno, iheader_id, ctenant_id, ctask_type, cmapping_code, 
        //                ccurrent_status, lcurrent_status_date, cremarks, inext_seqno, 
        //                cnext_seqtype, cprevtype,nboard_enabled, cprocess_type,csla_day,csla_Hour,caction_privilege,crejection_privilege) VALUES (
        //                @itaskno, @iseqno, @iheader_id, @ctenent_id, @ctask_type, @cmapping_code, 
        //                @ccurrent_status, @lcurrent_status_date, @cremarks, @inext_seqno, 
        //                @cnext_seqtype, @cprevtype,@nboard_enabled,@cparticipant_type,@csla_day,@csla_Hour,@caction_privilege,@crejection_privilege);SELECT SCOPE_IDENTITY();";

        //                string queryStatus = @"INSERT INTO tbl_transaction_taskflow_detail_and_status (
        //                itaskno, ctenant_id, cheader_id, cdetail_id, cstatus, cstatus_with, lstatus_date) VALUES 
        //                (@itaskno, @ctenent_id, @cheader_id, @cdetail_id, @cstatus, @cstatus_with, @lstatus_date);";

        //                string meta = @"INSERT INTO tbl_transaction_process_meta_layout (
        //                [cmeta_id],[cprocess_id],[cprocess_code],[ctenant_id],[cdata],[citaskno],[cdetail_id]) VALUES (
        //                @cmeta_id, @cprocess_id, @cprocess_code, @TenantID, @cdata,@citaskno,@cdetail_id);";

        //                foreach (var row in detailRows)
        //                {
        //                    using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
        //                    {
        //                        cmdInsert.Parameters.AddWithValue("@itaskno", newTaskNo);
        //                        cmdInsert.Parameters.AddWithValue("@iseqno", row["ciseqno"]);
        //                        cmdInsert.Parameters.AddWithValue("@iheader_id", masterId);
        //                        cmdInsert.Parameters.AddWithValue("@ctenent_id", cTenantID);
        //                        cmdInsert.Parameters.AddWithValue("@ctask_type", row["ctasktype"]);
        //                        cmdInsert.Parameters.AddWithValue("@cmapping_code", row["cmapping_code"]);
        //                        cmdInsert.Parameters.AddWithValue("@ccurrent_status", "P");
        //                        cmdInsert.Parameters.AddWithValue("@lcurrent_status_date", DateTime.Now);
        //                        cmdInsert.Parameters.AddWithValue("@cremarks", DBNull.Value);
        //                        cmdInsert.Parameters.AddWithValue("@inext_seqno", row["cnextseqno"]);
        //                        cmdInsert.Parameters.AddWithValue("@cnext_seqtype", DBNull.Value);
        //                        cmdInsert.Parameters.AddWithValue("@cprevtype", row["cprevstep"]);
        //                        cmdInsert.Parameters.AddWithValue("@cparticipant_type", row["cparticipant_type"]);
        //                        cmdInsert.Parameters.AddWithValue("@csla_day", row["csla_day"]);
        //                        cmdInsert.Parameters.AddWithValue("@csla_Hour", row["csla_Hour"]);
        //                        cmdInsert.Parameters.AddWithValue("@caction_privilege", row["caction_privilege"]);
        //                        cmdInsert.Parameters.AddWithValue("@crejection_privilege", row["crejection_privilege"]);
        //                        cmdInsert.Parameters.AddWithValue("@nboard_enabled", row["nboard_enabled"]);

        //                        var newId = await cmdInsert.ExecuteScalarAsync();
        //                        detailId = newId != null ? Convert.ToInt32(newId) : 0;
        //                    }

        //                    using (var cmdStatus = new SqlCommand(queryStatus, conn, transaction))
        //                    {
        //                        cmdStatus.Parameters.AddWithValue("@itaskno", newTaskNo);
        //                        cmdStatus.Parameters.AddWithValue("@ctenent_id", cTenantID);
        //                        cmdStatus.Parameters.AddWithValue("@cheader_id", 1);
        //                        cmdStatus.Parameters.AddWithValue("@cdetail_id", detailId);
        //                        cmdStatus.Parameters.AddWithValue("@cstatus", "P");
        //                        cmdStatus.Parameters.AddWithValue("@cstatus_with", username); // or a value if applicable
        //                        cmdStatus.Parameters.AddWithValue("@lstatus_date", DateTime.Now);
        //                        await cmdStatus.ExecuteNonQueryAsync();
        //                    }
        //                    foreach (var metaData in model.metaData)
        //                    {
        //                        using (SqlCommand cmd = new SqlCommand(meta, conn, transaction))
        //                        {
        //                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);

        //                            cmd.Parameters.AddWithValue("@cmeta_id", (object?)metaData.cmeta_id ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocess_id ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@cprocess_code", (object?)model.ctask_name ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
        //                            cmd.Parameters.AddWithValue("@citaskno", newTaskNo);
        //                            cmd.Parameters.AddWithValue("@cdetail_id", detailId);
        //                            cmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
        //                            await cmd.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                }

        //                transaction.Commit();
        //            }
        //            catch (Exception ex)
        //            {
        //                transaction.Rollback();
        //                throw;
        //            }
        //        }
        //    }

        //    return masterId;
        //}

        // Assuming Dapper is used for cleaner SQL execution
        // using Dapper;
        // using System.Data;

        public async Task<int> InsertTaskMasterAsync(TaskMasterDTO model, int tenantId, string userName)
        {
            int masterId = 0;
            int detailId = 0;
            int primaryDetailId = 0; 

            var connectionString = _config.GetConnectionString("Database");

            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string taskNoQuery = @"
                    SELECT ISNULL(MAX(TRY_CAST(itaskno AS INT)), 0) + 1 
                    FROM tbl_taskflow_master 
                    WHERE ctenant_id = @TenantID";

                        int newTaskNo;
                        using (var taskNoCmd = new SqlCommand(taskNoQuery, conn, transaction))
                        {
                            taskNoCmd.Parameters.AddWithValue("@TenantID", tenantId);

                            var result = await taskNoCmd.ExecuteScalarAsync();
                            newTaskNo = result != null ? Convert.ToInt32(result) : 1;
                        }
                        string queryMaster = @"
                    INSERT INTO tbl_taskflow_master (
                        itaskno, ctenant_id, ctask_type, ctask_name, ctask_description, cstatus,  
                        lcreated_date, ccreated_by, cmodified_by, lmodified_date, cprocess_id
                    ) VALUES (
                        @itaskno, @TenantID, @ctask_type, @ctask_name, @ctask_description, @cstatus,
                        @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cprocess_id
                    );
                    SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@itaskno", newTaskNo);
                            cmd.Parameters.AddWithValue("@TenantID", tenantId);
                            cmd.Parameters.AddWithValue("@ctask_type", (object?)model.ctask_type ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctask_name", (object?)model.ctask_name ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctask_description", (object?)model.ctask_description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", "Initiated");
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ccreated_by", userName);
                            cmd.Parameters.AddWithValue("@cmodified_by", userName);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocess_id ?? DBNull.Value);
                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }
                        string selectQuery = @"                     
                    SELECT ctenant_id, cprocesscode, ciseqno, cactivitycode,
                    cactivity_description, ctask_type, cprev_step, cactivityname, cnext_seqno, nboard_enabled, cmapping_code,
                    cparticipant_type, csla_day, csla_Hour, caction_privilege, crejection_privilege, cmapping_type
                    FROM tbl_process_engine_details 
                    WHERE cheader_id = @cprocesscode AND ctenant_id = @ctenent_id";

                        var detailRows = new List<Dictionary<string, object>>();

                        using (var cmdSelect = new SqlCommand(selectQuery, conn, transaction))
                        {
                            cmdSelect.Parameters.AddWithValue("@cprocesscode", model.cprocess_id);
                            cmdSelect.Parameters.AddWithValue("@ctenent_id", tenantId);

                            using (var reader = await cmdSelect.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var row = new Dictionary<string, object>
                                    {
                                        ["ciseqno"] = reader["ciseqno"],
                                        ["ctenantid"] = reader["ctenant_id"],
                                        ["ctasktype"] = reader["ctask_type"],
                                        ["cprocesscode"] = reader["cprocesscode"],
                                        ["cnextseqno"] = reader["cnext_seqno"],
                                        ["cprevstep"] = reader["cprev_step"],
                                        ["cmapping_code"] = reader["cmapping_code"],
                                        ["nboard_enabled"] = reader["nboard_enabled"],
                                        ["cparticipant_type"] = reader["cparticipant_type"],
                                        ["csla_day"] = reader["csla_day"],
                                        ["csla_Hour"] = reader["csla_Hour"],
                                        ["caction_privilege"] = reader["caction_privilege"],
                                        ["crejection_privilege"] = reader["crejection_privilege"],
                                        ["cmapping_type"] = reader["cmapping_type"]
                                    };
                                    detailRows.Add(row);
                                }
                            }
                        }

                        string queryDetail = @"INSERT INTO tbl_taskflow_detail (
                    itaskno, iseqno, iheader_id, ctenant_id, ctask_type, cmapping_code, 
                    ccurrent_status, lcurrent_status_date, cremarks, inext_seqno, 
                    cnext_seqtype, cprevtype, nboard_enabled, cprocess_type, csla_day, csla_Hour, caction_privilege, crejection_privilege) VALUES (
                    @itaskno, @iseqno, @iheader_id, @ctenent_id, @ctask_type, @cmapping_code, 
                    @ccurrent_status, @lcurrent_status_date, @cremarks, @inext_seqno, 
                    @cnext_seqtype, @cprevtype, @nboard_enabled, @cparticipant_type, @csla_day, @csla_Hour, @caction_privilege, @crejection_privilege);SELECT SCOPE_IDENTITY();";

                        string queryStatus = @"INSERT INTO tbl_transaction_taskflow_detail_and_status (
                    itaskno, ctenant_id, cheader_id, cdetail_id, cstatus, cstatus_with, lstatus_date) VALUES 
                    (@itaskno, @ctenent_id, @cheader_id, @cdetail_id, @cstatus, @cstatus_with, @lstatus_date);";

                        foreach (var row in detailRows)
                        {
                            using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdInsert.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdInsert.Parameters.AddWithValue("@iseqno", row["ciseqno"]);
                                cmdInsert.Parameters.AddWithValue("@iheader_id", masterId);
                                cmdInsert.Parameters.AddWithValue("@ctenent_id", tenantId);
                                cmdInsert.Parameters.AddWithValue("@ctask_type", row["ctasktype"]);
                                cmdInsert.Parameters.AddWithValue("@cmapping_code", row["cmapping_code"]);
                                cmdInsert.Parameters.AddWithValue("@ccurrent_status", "P");
                                cmdInsert.Parameters.AddWithValue("@lcurrent_status_date", DateTime.Now);
                                cmdInsert.Parameters.AddWithValue("@cremarks", DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@inext_seqno", row["cnextseqno"]);
                                cmdInsert.Parameters.AddWithValue("@cnext_seqtype", DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@cprevtype", row["cprevstep"]);
                                cmdInsert.Parameters.AddWithValue("@cparticipant_type", row["cparticipant_type"]);
                                cmdInsert.Parameters.AddWithValue("@csla_day", row["csla_day"]);
                                cmdInsert.Parameters.AddWithValue("@csla_Hour", row["csla_Hour"]);
                                cmdInsert.Parameters.AddWithValue("@caction_privilege", row["caction_privilege"]);
                                cmdInsert.Parameters.AddWithValue("@crejection_privilege", row["crejection_privilege"]);
                                cmdInsert.Parameters.AddWithValue("@nboard_enabled", row["nboard_enabled"]);
                                var newId = await cmdInsert.ExecuteScalarAsync();
                                detailId = newId != null ? Convert.ToInt32(newId) : 0;
                            }
                            if (primaryDetailId == 0)
                            {
                                primaryDetailId = detailId;
                            }
                            using (var cmdStatus = new SqlCommand(queryStatus, conn, transaction))
                            {
                                cmdStatus.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdStatus.Parameters.AddWithValue("@ctenent_id", tenantId);
                                cmdStatus.Parameters.AddWithValue("@cheader_id", 1);
                                cmdStatus.Parameters.AddWithValue("@cdetail_id", detailId);
                                cmdStatus.Parameters.AddWithValue("@cstatus", "P");
                                cmdStatus.Parameters.AddWithValue("@cstatus_with", userName);
                                cmdStatus.Parameters.AddWithValue("@lstatus_date", DateTime.Now);
                                await cmdStatus.ExecuteNonQueryAsync();
                            }
                        } 
                        string metaQuery = @"INSERT INTO tbl_transaction_process_meta_layout (
                    [cmeta_id],[cprocess_id],[cprocess_code],[ctenant_id],[cdata],[citaskno],[cdetail_id]) VALUES (
                    @cmeta_id, @cprocess_id, @cprocess_code, @TenantID, @cdata,@citaskno,@cdetail_id);";

                        if (primaryDetailId > 0)
                        {
                            foreach (var metaData in model.metaData)
                            {
                                using (SqlCommand cmd = new SqlCommand(metaQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@TenantID", tenantId);
                                    cmd.Parameters.AddWithValue("@cmeta_id", (object?)metaData.cmeta_id ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocess_id ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@cprocess_code", (object?)model.ctask_name ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@citaskno", newTaskNo);
                                    cmd.Parameters.AddWithValue("@cdetail_id", primaryDetailId);

                                    await cmd.ExecuteNonQueryAsync();
                                }
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

        public async Task<string> GetAllProcessmetaAsync(int cTenantID, int processid)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_Process_meta", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                    cmd.Parameters.AddWithValue("@processid", processid);
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

        public async Task<string> Getdepartmentroleposition(int cTenantID, string table)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_department_role_position", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenantid", cTenantID);
                    cmd.Parameters.AddWithValue("@table", table);
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

        //public async Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege)
        //{
        //    try
        //    {
        //        using (var con = new SqlConnection(_config.GetConnectionString("Database")))
        //        using (var cmd = new SqlCommand("sp_get_process_engine_privilege", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@tenentid", cTenantID);
        //            cmd.Parameters.AddWithValue("@value", value);
        //            cmd.Parameters.AddWithValue("@cprivilege", cprivilege);
        //            var ds = new DataSet();
        //            var adapter = new SqlDataAdapter(cmd);
        //            await Task.Run(() => adapter.Fill(ds)); // async wrapper

        //            if (ds.Tables.Count > 0)
        //            {
        //                return JsonConvert.SerializeObject(ds.Tables[0], Formatting.Indented);
        //            }

        //            return "[]";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw;
        //    }
        //}



        public async Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await con.OpenAsync();

                    if (!int.TryParse(value, out int privilegeId))
                    {
                        return JsonConvert.SerializeObject(new List<object>
                {
                    new { name = value, workflow = new List<object>() }
                }, Formatting.Indented);
                    }

                    string query = @"
SELECT DISTINCT    
    a.cprocess_id,
    a.[cprocesscode],
    b.[cprocessname],
    b.[cprocessdescription],
    b.cmeta_id,
    b.cvalue,
    -- Get entity name from cvalue using CASE conditions
    CASE  
        WHEN p.cprocess_privilege = 'role' THEN 
            (SELECT TOP 1 crole_name FROM tbl_role_master WHERE crole_code = b.cvalue)
        WHEN p.cprocess_privilege = 'user' THEN 
            (SELECT TOP 1 cuser_name FROM users WHERE CAST(cuserid AS VARCHAR(50)) = b.cvalue)
        WHEN p.cprocess_privilege = 'department' THEN
            (SELECT TOP 1 cdepartment_name FROM tbl_department_master WHERE cdepartment_code = b.cvalue)
        WHEN p.cprocess_privilege = 'position' THEN 
            (SELECT TOP 1 cposition_name FROM tbl_position_master WHERE cposition_code = b.cvalue)
        ELSE b.cvalue
    END AS entity_name
FROM [dbo].[tbl_engine_master_to_process_privilege] a 
INNER JOIN tbl_process_engine_master b ON a.cprocess_id = b.ID
INNER JOIN tbl_process_privilege_type p ON b.cprivilege_type = p.ID AND b.ctenant_id = p.ctenant_id
WHERE a.cis_active = 1 
    AND a.[cprocess_privilege] = @privilegeId";

                    var groupedWorkflows = new Dictionary<string, List<object>>();

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@privilegeId", privilegeId);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string entityName = reader["entity_name"]?.ToString() ?? "";

                                if (string.IsNullOrEmpty(entityName))
                                    continue;

                                if (!groupedWorkflows.ContainsKey(entityName))
                                {
                                    groupedWorkflows[entityName] = new List<object>();
                                }

                                groupedWorkflows[entityName].Add(new
                                {
                                    cprocess_id = reader["cprocess_id"] != DBNull.Value ? Convert.ToInt32(reader["cprocess_id"]) : 0,
                                    cprocesscode = reader["cprocesscode"]?.ToString() ?? "",
                                    cprocessname = reader["cprocessname"]?.ToString() ?? "",
                                    cprocessdescription = reader["cprocessdescription"]?.ToString() ?? "",
                                    cmeta_id = reader["cmeta_id"] != DBNull.Value ? Convert.ToInt32(reader["cmeta_id"]) : 0
                                });
                            }
                        }
                    }

                    var result = new List<object>();
                    foreach (var group in groupedWorkflows)
                    {
                        result.Add(new
                        {
                            name = group.Key,
                            workflow = group.Value
                        });
                    }

                    return JsonConvert.SerializeObject(result, Formatting.Indented);
                }
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new List<object>
        {
            new
            {
                name = value,
                workflow = new List<object>(),
                error = ex.Message
            }
        }, Formatting.Indented);
            }
        }

        public async Task<string> Getdropdown(int cTenantID, string @column)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_dropdown", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenant", cTenantID);
                    cmd.Parameters.AddWithValue("@column", @column);
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

        public async Task<string> Gettaskapprove(int cTenantID, string username)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_worflow_approved", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@userid", username);
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

        public async Task<string> DeptposrolecrudAsync(DeptPostRoleDTO model, int cTenantID, string username)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_tables_crud", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenantid", cTenantID);
                    cmd.Parameters.AddWithValue("@action", model.action);
                    cmd.Parameters.AddWithValue("@userid", model.userid);
                    cmd.Parameters.AddWithValue("@position", model.position);
                    cmd.Parameters.AddWithValue("@role", model.role);
                    cmd.Parameters.AddWithValue("@departmentname", model.departmentname);
                    cmd.Parameters.AddWithValue("@departmentdesc", model.departmentdesc);
                    cmd.Parameters.AddWithValue("@table", model.table);
                    cmd.Parameters.AddWithValue("@cdepartmentmanagerrolecode", model.cdepartmentmanagerrolecode);
                    cmd.Parameters.AddWithValue("@cdepartmentmanagername", model.cdepartmentmanagername);
                    cmd.Parameters.AddWithValue("@cdepartmentemail", model.cdepartmentemail);
                    cmd.Parameters.AddWithValue("@cdepartmentphone", model.cdepartmentphone);
                    cmd.Parameters.AddWithValue("@nisactive", model.nisactive);
                    cmd.Parameters.AddWithValue("@user", model.user);
                    cmd.Parameters.AddWithValue("@cdepartmentcode", model.cdepartmentcode);
                    cmd.Parameters.AddWithValue("@rolename", model.rolename);
                    cmd.Parameters.AddWithValue("@rolelevel", model.rolelevel);
                    cmd.Parameters.AddWithValue("@roledescription", model.roledescription);
                    cmd.Parameters.AddWithValue("@positionname", model.positionname);
                    cmd.Parameters.AddWithValue("@positioncode", model.positioncode);
                    cmd.Parameters.AddWithValue("@positiondescription", model.positiondescription);
                    cmd.Parameters.AddWithValue("@creportingmanagerpositionid", model.creportingmanagerpositionid);
                    cmd.Parameters.AddWithValue("@rolecode", model.rolecode);
                    cmd.Parameters.AddWithValue("@id", model.id);

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
        public async Task<int> Processprivilege_mapping(privilegeMappingDTO model, int cTenantID, string username)
        {

            int masterId = 0;

            try
            {
                using (var conn = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await conn.OpenAsync();
                    using (var transaction = conn.BeginTransaction())
                    {
                        string query = @"
                INSERT INTO tbl_engine_master_to_process_privilege (
                    [ctenant_id],[cprocess_privilege],[cseq_id],[ciseqno],[cprocess_id],[cprocesscode],
                    [cprocessname],[cis_active],[ccreated_by],[lcreated_date],[cmodified_by],[lmodified_date]
                ) VALUES (
                    @ctenent_id, @cprocess_privilege, @cseq_id, @ciseqno, @cprocess_id, @cprocesscode, 
                    @cprocessname, @cis_active, @ccreated_by, @lcreated_date, 
                    @cmodified_by, @lmodified_date); SELECT SCOPE_IDENTITY()";


                        using (var cmdInsert = new SqlCommand(query, conn, transaction))
                        {
                            cmdInsert.Parameters.AddWithValue("@ctenent_id", cTenantID);
                            cmdInsert.Parameters.AddWithValue("@cprocess_privilege", model.privilege);
                            cmdInsert.Parameters.AddWithValue("@cseq_id", "1");
                            cmdInsert.Parameters.AddWithValue("@ciseqno", "1");
                            cmdInsert.Parameters.AddWithValue("@cprocess_id", model.cprocess_id);
                            cmdInsert.Parameters.AddWithValue("@cprocesscode", model.cprocess_code);
                            cmdInsert.Parameters.AddWithValue("@cprocessname", model.cprocess_name);
                            cmdInsert.Parameters.AddWithValue("@cis_active", "1");
                            cmdInsert.Parameters.AddWithValue("@ccreated_by", username);
                            cmdInsert.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                            cmdInsert.Parameters.AddWithValue("@cmodified_by", username);
                            cmdInsert.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            var newId = await cmdInsert.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }

                        string queryDetail = @"
                    INSERT INTO tbl_process_privilege_details (
                        privilege_id, entity_type, entity_id, ctenant_id, cis_active,ccreated_by,lcreated_date, 
                    cmodified_by, lmodified_date,cprocess_id) VALUES (
                        @privilege_id, @entity_type, @entity_id, @ctenent_id, @cis_active, @ccreated_by, 
                        @lcreated_date, @cmodified_by, @lmodified_date, @cprocess_id);";

                        foreach (var row in model.privilegeMapping)
                        {
                            using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdInsert.Parameters.AddWithValue("@privilege_id", masterId);
                                cmdInsert.Parameters.AddWithValue("@entity_type", row.entity_type);
                                cmdInsert.Parameters.AddWithValue("@entity_id", row.entity_id);
                                cmdInsert.Parameters.AddWithValue("@ctenent_id", cTenantID);
                                cmdInsert.Parameters.AddWithValue("@cis_active", true);
                                cmdInsert.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                cmdInsert.Parameters.AddWithValue("@ccreated_by", username);
                                cmdInsert.Parameters.AddWithValue("@cmodified_by", username);
                                cmdInsert.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                cmdInsert.Parameters.AddWithValue("@cprocess_id", model.cprocess_id);
                                await cmdInsert.ExecuteNonQueryAsync();
                            }
                        }
                        transaction.Commit();


                    }
                }

                return masterId;
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        public async Task<string> GetTaskInitiator(int cTenantID, string username)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();

            string query = "sp_get_workflow_initiator";
            using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            List<GetTaskDetails> tskdtl = new List<GetTaskDetails>();
                            GetTaskList p = new GetTaskList                           
                            {
                                ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                itaskno = sdr.IsDBNull(sdr.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                ctasktype = sdr.IsDBNull(sdr.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr["ctask_type"]),
                                ctaskname = sdr.IsDBNull(sdr.GetOrdinal("ctask_name")) ? string.Empty : Convert.ToString(sdr["ctask_name"]),
                                ctaskdescription = sdr.IsDBNull(sdr.GetOrdinal("ctask_description")) ? string.Empty : Convert.ToString(sdr["ctask_description"]),
                                cstatus = sdr.IsDBNull(sdr.GetOrdinal("cstatus")) ? string.Empty : Convert.ToString(sdr["cstatus"]),
                                lcompleteddate = sdr.IsDBNull(sdr.GetOrdinal("lcompleted_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcompleted_date")),
                                ccreatedby = sdr.IsDBNull(sdr.GetOrdinal("ccreated_by")) ? string.Empty : Convert.ToString(sdr["ccreated_by"]),
                                ccreatedbyname = sdr.IsDBNull(sdr.GetOrdinal("ccreated_byname")) ? string.Empty : Convert.ToString(sdr["ccreated_byname"]),
                                lcreateddate = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                cmodifiedby = sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                cmodifiedbyname = sdr.IsDBNull(sdr.GetOrdinal("cmodified_byname")) ? string.Empty : Convert.ToString(sdr["cmodified_byname"]),
                                lmodifieddate = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date")),
                                Employeecode = sdr.IsDBNull(sdr.GetOrdinal("Employeecode")) ? string.Empty : Convert.ToString(sdr["Employeecode"]),
                                Employeename = sdr.IsDBNull(sdr.GetOrdinal("Employeename")) ? string.Empty : Convert.ToString(sdr["Employeename"]),
                                EmpDepartment = sdr.IsDBNull(sdr.GetOrdinal("EmpDepartment")) ? string.Empty : Convert.ToString(sdr["EmpDepartment"]),
                                cprocess_id = sdr.IsDBNull(sdr.GetOrdinal("cprocess_id")) ? 0 : Convert.ToInt32(sdr["cprocess_id"]),
                                cprocesscode = sdr.IsDBNull(sdr.GetOrdinal("cprocesscode")) ? string.Empty : Convert.ToString(sdr["cprocesscode"]),
                                cprocessname = sdr.IsDBNull(sdr.GetOrdinal("cprocessname")) ? string.Empty : Convert.ToString(sdr["cprocessname"]),
                                };

                            using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                            {
                                string query1 = "sp_get_workflow_initiatordeatils";
                                using (SqlCommand cmd1 = new SqlCommand(query1))
                                {
                                    cmd1.Connection = con1;
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    cmd1.Parameters.AddWithValue("@userid", username);
                                    cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                    cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                    con1.Open();
                                    using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                                    {
                                        while (sdr1.Read())
                                        {
                                            GetTaskDetails pd = new GetTaskDetails
                                            {

                                                ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                ctasktype = sdr.IsDBNull(sdr.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr["ctask_type"]),
                                                cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1.GetOrdinal("cremarks")),
                                                inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                nboard_enabled = sdr1.IsDBNull(sdr1.GetOrdinal("nboard_enabled")) ? false : Convert.ToBoolean(sdr1["nboard_enabled"]),
                                                caction_privilege = sdr1.IsDBNull(sdr1.GetOrdinal("caction_privilege")) ? string.Empty : Convert.ToString(sdr1["caction_privilege"]),
                                                crejection_privilege = sdr1.IsDBNull(sdr1.GetOrdinal("crejection_privilege")) ? string.Empty : Convert.ToString(sdr1["crejection_privilege"]),
                                                cisforwarded = sdr1.IsDBNull(sdr1.GetOrdinal("cis_forwarded")) ? string.Empty : Convert.ToString(sdr1["cis_forwarded"]),
                                                lfwd_date = sdr1.IsDBNull(sdr1.GetOrdinal("lfwd_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lfwd_date")),
                                                cfwd_to = sdr1.IsDBNull(sdr1.GetOrdinal("cfwd_to")) ? string.Empty : Convert.ToString(sdr1["cfwd_to"]),
                                                cis_reassigned = sdr1.IsDBNull(sdr1.GetOrdinal("cis_reassigned")) ? string.Empty : Convert.ToString(sdr1["cis_reassigned"]),
                                                creassign_name = sdr1.IsDBNull(sdr1.GetOrdinal("creassign_name")) ? string.Empty : Convert.ToString(sdr1["creassign_name"]),
                                                lreassign_date = sdr1.IsDBNull(sdr1.GetOrdinal("lreassign_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lreassign_date")),
                                                creassign_to = sdr1.IsDBNull(sdr1.GetOrdinal("creassign_to")) ? string.Empty : Convert.ToString(sdr1["creassign_to"]),
                                                cactivityname = sdr1.IsDBNull(sdr1.GetOrdinal("cactivityname")) ? string.Empty : Convert.ToString(sdr1["cactivityname"]),
                                                cactivity_description = sdr1.IsDBNull(sdr1.GetOrdinal("cactivity_description")) ? string.Empty : Convert.ToString(sdr1["cactivity_description"])
                                            };
                                            tskdtl.Add(pd);
                                        }
                                    }
                                    con1.Close();
                                }
                            }

                            p.TaskChildItems = tskdtl;
                            tsk.Add(p);
                        }
                    }
                    con.Close();
                }
            }
            // ✅ Serialize the result to JSON
            return JsonConvert.SerializeObject(tsk, Formatting.Indented);
        }


        public async Task<string> Gettaskinbox(int cTenantID, string username)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();

            string query = "sp_get_worflow_inbox";
            using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    con.Open();

                    using (SqlDataReader sdr = cmd.ExecuteReader())
                    {
                        while (sdr.Read())
                        {
                            List<GetTaskDetails> tskdtl = new List<GetTaskDetails>();
                            GetTaskList p = new GetTaskList
                            {
                                ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                itaskno = sdr.IsDBNull(sdr.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                ctasktype = sdr.IsDBNull(sdr.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr["ctask_type"]),
                                ctaskname = sdr.IsDBNull(sdr.GetOrdinal("ctask_name")) ? string.Empty : Convert.ToString(sdr["ctask_name"]),
                                ctaskdescription = sdr.IsDBNull(sdr.GetOrdinal("ctask_description")) ? string.Empty : Convert.ToString(sdr["ctask_description"]),
                                cstatus = sdr.IsDBNull(sdr.GetOrdinal("cstatus")) ? string.Empty : Convert.ToString(sdr["cstatus"]),
                                lcompleteddate = sdr.IsDBNull(sdr.GetOrdinal("lcompleted_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcompleted_date")),
                                ccreatedby = sdr.IsDBNull(sdr.GetOrdinal("ccreated_by")) ? string.Empty : Convert.ToString(sdr["ccreated_by"]),
                                ccreatedbyname = sdr.IsDBNull(sdr.GetOrdinal("ccreated_byname")) ? string.Empty : Convert.ToString(sdr["ccreated_byname"]),
                                lcreateddate = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                cmodifiedby = sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                cmodifiedbyname = sdr.IsDBNull(sdr.GetOrdinal("cmodified_byname")) ? string.Empty : Convert.ToString(sdr["cmodified_byname"]),
                                lmodifieddate = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date")),
                                Employeecode = sdr.IsDBNull(sdr.GetOrdinal("Employeecode")) ? string.Empty : Convert.ToString(sdr["Employeecode"]),
                                Employeename = sdr.IsDBNull(sdr.GetOrdinal("Employeename")) ? string.Empty : Convert.ToString(sdr["Employeename"]),
                                EmpDepartment = sdr.IsDBNull(sdr.GetOrdinal("EmpDepartment")) ? string.Empty : Convert.ToString(sdr["EmpDepartment"]),
                                cprocess_id = sdr.IsDBNull(sdr.GetOrdinal("cprocess_id")) ? 0 : Convert.ToInt32(sdr["cprocess_id"]),
                                cprocesscode = sdr.IsDBNull(sdr.GetOrdinal("cprocesscode")) ? string.Empty : Convert.ToString(sdr["cprocesscode"]),
                                cprocessname = sdr.IsDBNull(sdr.GetOrdinal("cprocessname")) ? string.Empty : Convert.ToString(sdr["cprocessname"]),
                                cprocessdescription = sdr.IsDBNull(sdr.GetOrdinal("cprocessdescription")) ? string.Empty : Convert.ToString(sdr["cprocessdescription"]),
                                //privilege_name = sdr.IsDBNull(sdr.GetOrdinal("privilege_name")) ? string.Empty : Convert.ToString(sdr["privilege_name"])

                            };

                            using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                            {
                                string query1 = "sp_get_worflow_inbox_details";
                                using (SqlCommand cmd1 = new SqlCommand(query1))
                                {
                                    cmd1.Connection = con1;
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    cmd1.Parameters.AddWithValue("@userid", username);
                                    cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                    cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                    con1.Open();
                                    using (SqlDataReader sdr1 = cmd1.ExecuteReader())
                                    {
                                        while (sdr1.Read())
                                        {
                                            GetTaskDetails pd = new GetTaskDetails
                                            {

                                                ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                ctasktype = sdr.IsDBNull(sdr.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr["ctask_type"]),
                                                cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1.GetOrdinal("cremarks")),
                                                inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                nboard_enabled = sdr1.IsDBNull(sdr1.GetOrdinal("nboard_enabled")) ? false : Convert.ToBoolean(sdr1["nboard_enabled"]),
                                                caction_privilege = sdr1.IsDBNull(sdr1.GetOrdinal("caction_privilege")) ? string.Empty : Convert.ToString(sdr1["caction_privilege"]),
                                                crejection_privilege = sdr1.IsDBNull(sdr1.GetOrdinal("crejection_privilege")) ? string.Empty : Convert.ToString(sdr1["crejection_privilege"]),
                                                cisforwarded = sdr1.IsDBNull(sdr1.GetOrdinal("cis_forwarded")) ? string.Empty : Convert.ToString(sdr1["cis_forwarded"]),
                                                lfwd_date = sdr1.IsDBNull(sdr1.GetOrdinal("lfwd_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lfwd_date")),
                                                cfwd_to = sdr1.IsDBNull(sdr1.GetOrdinal("cfwd_to")) ? string.Empty : Convert.ToString(sdr1["cfwd_to"]),
                                                cis_reassigned = sdr1.IsDBNull(sdr1.GetOrdinal("cis_reassigned")) ? string.Empty : Convert.ToString(sdr1["cis_reassigned"]),
                                                creassign_name = sdr1.IsDBNull(sdr1.GetOrdinal("creassign_name")) ? string.Empty : Convert.ToString(sdr1["creassign_name"]),
                                                lreassign_date = sdr1.IsDBNull(sdr1.GetOrdinal("lreassign_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lreassign_date")),
                                                creassign_to = sdr1.IsDBNull(sdr1.GetOrdinal("creassign_to")) ? string.Empty : Convert.ToString(sdr1["creassign_to"]),
                                                cactivityname = sdr1.IsDBNull(sdr1.GetOrdinal("cactivityname")) ? string.Empty : Convert.ToString(sdr1["cactivityname"]),
                                                cactivity_description = sdr1.IsDBNull(sdr1.GetOrdinal("cactivity_description")) ? string.Empty : Convert.ToString(sdr1["cactivity_description"])
                                            };
                                            tskdtl.Add(pd);
                                        }
                                    }
                                    con1.Close();
                                }
                            }

                            p.TaskChildItems = tskdtl;
                            tsk.Add(p);
                        }
                    }
                    con.Close();
                }
            }
            // ✅ Serialize the result to JSON
            return JsonConvert.SerializeObject(tsk, Formatting.Indented);
        }

        public async Task<List<GetprocessEngineConditionDTO>> GetTaskConditionBoard(int cTenantID, int ID)
        {
            try
            {
                var result = new List<GetprocessEngineConditionDTO>();
                var connStr = _config.GetConnectionString("Database");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"SELECT  ID,cheader_id,cprocesscode,ciseqno,ctenant_id,icond_seqno,ctype,clabel,
                cplaceholder,cis_required,cis_readonly,cis_disabled,cfield_value,ccondition,cdata_source
                FROM [TASKENGINE].[dbo].[tbl_process_engine_condition] 
                WHERE cheader_id = @cheader_id AND ctenant_id = @TenantID  ORDER BY ID DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@cheader_id", ID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var mapping = new GetprocessEngineConditionDTO
                                {
                                    ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
                                    cprocessCode = reader["cprocesscode"]?.ToString() ?? "",
                                    ciseqno = reader["ciseqno"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ciseqno"]),
                                    icondseqno = reader["icond_seqno"] == DBNull.Value ? 0 : Convert.ToInt32(reader["icond_seqno"]),
                                    ctype = reader["ctype"]?.ToString() ?? "",
                                    clabel = reader["clabel"]?.ToString() ?? "",
                                    cplaceholder = reader["cplaceholder"]?.ToString() ?? "",
                                    cisRequired = reader.SafeGetBoolean("cis_required"),
                                    cisReadonly = reader.SafeGetBoolean("cis_readonly"),
                                    cis_disabled = reader.SafeGetBoolean("cis_disabled"),
                                    cfieldValue = reader["cfield_value"]?.ToString() ?? "",
                                    cdatasource = reader["cdata_source"]?.ToString() ?? "",
                                    ccondition = reader["ccondition"]?.ToString() ?? ""
                                };

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving task condition list: {ex.Message}");
            }
        }

               
        public async Task<List<GettaskinboxbyidDTO>> Getinboxdatabyidold(int cTenantID, int ID)
        {
            try
            {
                var result = new List<GettaskinboxbyidDTO>();
                var connStr = _config.GetConnectionString("Database");
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"select a.cprocess_id as processId,c.cprocessname as processName,c.cprocessdescription as processDesc,
	            d.cactivityname as activityName,d.cactivity_description as activityDesc,c.cpriority_label as priorityLabel ,
	            b.ccurrent_status as taskStatus,d.cparticipant_type as participantType,
	            d.caction_privilege as actionPrivilege,d.cmapping_type as assigneeType,d.cmapping_code as assigneeValue,
	            d.csla_day as slaDays,d.csla_Hour as slaHours,d.ctask_type as executionType,
	            c.nshow_timeline as showTimeline,a.lcreated_date as taskInitiatedDate,		 
	            b.lcurrent_status_date as taskAssignedDate ,e.cfirst_name+ ' '+e.clast_name as assigneeName
            from tbl_taskflow_master a 
            inner join tbl_taskflow_detail b on a.id=b.iheader_id
            inner join tbl_process_engine_master c on a.cprocess_id=c.ID 
            inner join tbl_process_engine_details d on c.ID=d.cheader_id and d.ciseqno=b.iseqno 
            inner join Users e on e.cuserid= CONVERT(int,a.ccreated_by) and e.ctenant_id=a.ctenant_id 
            where b.id=@ID "; 

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@ID", ID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var mapping = new GettaskinboxbyidDTO
                                {
                                    processId = reader["processId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>(),
                                };
                                if (mapping.showTimeline == true)
                                {
                                    var child = new TimelineDTO
                                    {
                                        taskName = reader["processName"]?.ToString() ?? "",
                                        assigneeName = reader["assigneeName"]?.ToString() ?? "",
                                        status = reader["taskStatus"]?.ToString() ?? "",
                                        slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                        slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    };
                                    mapping.timeline.Add(child);
                                }
                                string condQuery = @"SELECT c.ID, c.ciseqno, c.icond_seqno, c.ctype, c.clabel, c.cfield_value, c.ccondition,
                                              c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled, c.cdata_source
                                              FROM tbl_process_engine_condition c
                                              WHERE c.cheader_id = @HeaderID;"; 

                                using var condCmd = new SqlCommand(condQuery, conn);
                              
                                condCmd.Parameters.AddWithValue("@HeaderID", mapping.processId);

                                using var condReader = await condCmd.ExecuteReaderAsync();
                                while (await condReader.ReadAsync())
                                {
                                   var childboard = new GetprocessEngineConditionDTO
                                    {
                                        ID = condReader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(condReader["ID"]),                                        
                                        cprocessCode = mapping.processName,
                                        ciseqno = condReader["ciseqno"] == DBNull.Value ? 0 : Convert.ToInt32(condReader["ciseqno"]),
                                        icondseqno = condReader["icond_seqno"] == DBNull.Value ? 0 : Convert.ToInt32(condReader["icond_seqno"]),
                                        ctype = condReader["ctype"]?.ToString() ?? "",
                                        clabel = condReader["clabel"]?.ToString() ?? "",
                                        cplaceholder = condReader["cplaceholder"]?.ToString() ?? "",
                                        cisRequired = condReader.SafeGetBoolean("cis_required"),
                                        cisReadonly = condReader.SafeGetBoolean("cis_readonly"),
                                        cis_disabled = condReader.SafeGetBoolean("cis_disabled"),
                                        cfieldValue = condReader["cfield_value"]?.ToString() ?? "",
                                        cdatasource = condReader["cdata_source"]?.ToString() ?? "",
                                        ccondition = condReader["ccondition"]?.ToString() ?? "",
                                    };
                                    mapping.board.Add(childboard);
                                }
                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {              
                throw new Exception($"Error retrieving task inbox list: {ex.Message}", ex);
            }
        }

        public async Task<List<GetmetalayoutDTO>> GetmetalayoutByid(int cTenantID, int itaskno)
        {
            try
            {
                var result = new List<GetmetalayoutDTO>();
                var connStr = _config.GetConnectionString("Database");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"SELECT a.ID,a.cprocess_id,a.cdata,cinput_type,c.label,c.cplaceholder,
                                    c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
                                    from [tbl_transaction_process_meta_layout] a 
                                    inner join tbl_process_engine_master b on a.cprocess_id=b.ID 
                                    inner join  tbl_process_meta_detail c on c.cheader_id=b.cmeta_id
                                    where a.citaskno=@ID and a.ctenant_id=@TenantID ORDER BY a.ID asc";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@ID", itaskno);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var mapping = new GetmetalayoutDTO
                                {
                                    ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
                                    cprocess_id = reader["cprocess_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cprocess_id"]),
                                    cdata = reader["cdata"]?.ToString() ?? "",
                                    cinput_type = reader["cinput_type"]?.ToString() ?? "",
                                    label = reader["label"]?.ToString() ?? "",
                                    cplaceholder = reader["cplaceholder"]?.ToString() ?? "",                                 
                                    cis_required = reader.SafeGetBoolean("cis_required"),                                
                                    cis_readonly = reader.SafeGetBoolean("cis_readonly"),
                                    cis_disabled = reader.SafeGetBoolean("cis_disabled"),
                                    cfield_value = reader["cfield_value"]?.ToString() ?? "",
                                    cdata_source = reader["cdata_source"]?.ToString() ?? ""                                                             
                                };

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving task condition list: {ex.Message}");
            }
        }


        //public async Task<List<GettaskinboxbyidDTO>> Gettaskinboxdatabyid(int cTenantID, int ID)
        //{
        //    var result = new List<GettaskinboxbyidDTO>();
        //    var connStr = _config.GetConnectionString("Database");

        //    try
        //    {
        //        using (SqlConnection conn = new SqlConnection(connStr))
        //        {
        //            await conn.OpenAsync();
        //            string query = @"select a.cprocess_id as processId,c.cprocessname as processName,c.cprocessdescription as processDesc,
        //                d.cactivityname as activityName,d.cactivity_description as activityDesc,c.cpriority_label as priorityLabel ,
        //                b.ccurrent_status as taskStatus,d.cparticipant_type as participantType,
        //                d.caction_privilege as actionPrivilege,d.cmapping_type as assigneeType,d.cmapping_code as assigneeValue,
        //                d.csla_day as slaDays,d.csla_Hour as slaHours,d.ctask_type as executionType,
        //                c.nshow_timeline as showTimeline,a.lcreated_date as taskInitiatedDate,		 
        //                b.lcurrent_status_date as taskAssignedDate ,e.cfirst_name + ' ' + e.clast_name as assigneeName,d.id as processdetailid,
        //                 c.cmeta_id,a.itaskno
        //            from tbl_taskflow_master a 
        //            inner join tbl_taskflow_detail b on a.id=b.iheader_id
        //            inner join tbl_process_engine_master c on a.cprocess_id=c.ID 
        //            inner join tbl_process_engine_details d on c.ID=d.cheader_id and d.ciseqno=b.iseqno 
        //            inner join Users e on e.cuserid= CONVERT(int,a.ccreated_by) and e.ctenant_id=a.ctenant_id 
        //            where b.id=@ID";

        //            using (SqlCommand cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
        //                cmd.Parameters.AddWithValue("@ID", ID);

        //                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //                {
        //                    while (await reader.ReadAsync())
        //                    {

        //                        int processdetailid = reader["processdetailid"] == DBNull.Value ? 0 : Convert.ToInt32(reader["processdetailid"]);
        //                        int meta_id = reader["cmeta_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cmeta_id"]);
        //                        int itaskno = reader["itaskno"] == DBNull.Value ? 0 : Convert.ToInt32(reader["itaskno"]);


        //                        var mapping = new GettaskinboxbyidDTO
        //                        {
        //                            processId = reader["processId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["processId"]),
        //                            processName = reader["processName"]?.ToString() ?? "",
        //                            processDesc = reader["processDesc"]?.ToString() ?? "",
        //                            activityName = reader["activityName"]?.ToString() ?? "",
        //                            priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
        //                            activityDesc = reader["activityDesc"]?.ToString() ?? "",
        //                            taskStatus = reader["taskStatus"]?.ToString() ?? "",
        //                            participantType = reader["participantType"]?.ToString() ?? "",
        //                            actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
        //                            assigneeType = reader["assigneeType"]?.ToString() ?? "",
        //                            assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
        //                            slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
        //                            slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
        //                            executionType = reader["executionType"]?.ToString() ?? "",
        //                            taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
        //                            taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
        //                            showTimeline = reader.SafeGetBoolean("showTimeline"),
        //                            timeline = new List<TimelineDTO>(),
        //                            board = new List<GetprocessEngineConditionDTO>(),
        //                            meta = new List<processEnginetaskMeta>(),
        //                        };

        //                        if (mapping.showTimeline == true)
        //                        {
        //                            var child = new TimelineDTO
        //                            {
        //                                taskName = reader["processName"]?.ToString() ?? "",
        //                                assigneeName = reader["assigneeName"]?.ToString() ?? "",
        //                                status = reader["taskStatus"]?.ToString() ?? "",
        //                                slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
        //                                slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
        //                            };
        //                            mapping.timeline.Add(child);
        //                        }
        //                        string condQuery = @"SELECT c.ID, c.ciseqno, c.icond_seqno, c.ctype, c.clabel, c.cfield_value, c.ccondition,
        //                                          c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled, c.cdata_source
        //                                          FROM tbl_process_engine_condition c
        //                                          WHERE c.cheader_id = @HeaderID and ciseqno=@ciseqno;";

        //                        using var condCmd = new SqlCommand(condQuery, conn);
        //                        condCmd.Parameters.AddWithValue("@HeaderID", mapping.processId);
        //                        condCmd.Parameters.AddWithValue("@ciseqno", processdetailid);
        //                        using var condReader = await condCmd.ExecuteReaderAsync();
        //                        while (await condReader.ReadAsync())
        //                        {
        //                            var childboard = new GetprocessEngineConditionDTO
        //                            {
        //                                ID = condReader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(condReader["ID"]),
        //                                cprocessCode = mapping.processName,
        //                                ciseqno = condReader["ciseqno"] == DBNull.Value ? 0 : Convert.ToInt32(condReader["ciseqno"]),
        //                                icondseqno = condReader["icond_seqno"] == DBNull.Value ? 0 : Convert.ToInt32(condReader["icond_seqno"]),
        //                                ctype = condReader["ctype"]?.ToString() ?? "",
        //                                clabel = condReader["clabel"]?.ToString() ?? "",
        //                                cplaceholder = condReader["cplaceholder"]?.ToString() ?? "",
        //                                cisRequired = condReader.SafeGetBoolean("cis_required"),
        //                                cisReadonly = condReader.SafeGetBoolean("cis_readonly"),
        //                                cis_disabled = condReader.SafeGetBoolean("cis_disabled"),
        //                                cfieldValue = condReader["cfield_value"]?.ToString() ?? "",
        //                                cdatasource = condReader["cdata_source"]?.ToString() ?? "",
        //                                ccondition = condReader["ccondition"]?.ToString() ?? "",
        //                            };
        //                            mapping.board.Add(childboard);
        //                        }

        //                        string metaQuery = @"SELECT Id,cInput_type,label,cPlaceholder,cis_Required,cis_readonly,cis_disabled,cfield_value,cdata_source
        //                        FROM tbl_process_meta_detail where cheader_id= @HeaderID ;";
        //                        using var metaCmd = new SqlCommand(metaQuery, conn);
        //                        metaCmd.Parameters.AddWithValue("@HeaderID", meta_id);
        //                        using var metaReader = await metaCmd.ExecuteReaderAsync();
        //                        while (await metaReader.ReadAsync())
        //                        {
        //                            var childmeta = new processEnginetaskMeta
        //                            {
        //                                cinputType = metaReader["cInput_type"]?.ToString() ?? "",
        //                                clabel = metaReader["label"]?.ToString() ?? "",
        //                                cplaceholder = metaReader["cPlaceholder"]?.ToString() ?? "",
        //                                cisRequired = metaReader.SafeGetBoolean("cis_Required"),
        //                                cisReadonly = metaReader.SafeGetBoolean("cis_readonly"),
        //                                cisDisabled = metaReader.SafeGetBoolean("cis_disabled"),
        //                                cfieldValue = metaReader["cfield_value"]?.ToString() ?? "",
        //                                cdatasource = metaReader["cdata_source"]?.ToString() ?? "",
        //                            };
        //                            mapping.meta.Add(childmeta);
        //                        }

        //                            string layoutQuery = @"SELECT ID, cprocess_id, cdata
        //                    FROM tbl_transaction_process_meta_layout
        //                    WHERE citaskno = @ID AND ctenant_id = @TenantID 
        //                    ORDER BY ID DESC";

        //                            using var layoutCmd = new SqlCommand(layoutQuery, conn);
        //                            layoutCmd.Parameters.AddWithValue("@ID", itaskno);
        //                            layoutCmd.Parameters.AddWithValue("@TenantID", cTenantID);

        //                            using var layoutReader = await layoutCmd.ExecuteReaderAsync();
        //                            while (await layoutReader.ReadAsync())
        //                            {
        //                                var layoutmeta = new GetmetalayoutDTO
        //                                {
        //                                    ID = layoutReader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(layoutReader["ID"]),
        //                                    cprocess_id = layoutReader["cprocess_id"] == DBNull.Value ? 0 : Convert.ToInt32(layoutReader["cprocess_id"]),
        //                                    cdata = layoutReader.IsDBNull(layoutReader.GetOrdinal("cdata"))
        //                                                ? string.Empty
        //                                                : layoutReader.GetString(layoutReader.GetOrdinal("cdata"))
        //                                };
        //                              mapping.layout.Add(layoutmeta);
        //                            }                               
        //                        result.Add(mapping);
        //                    }





        //                }
        //            }
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving task inbox list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
        //    }
        //}



        public async Task<List<GettaskinboxbyidDTO>> Gettaskinboxdatabyid(int cTenantID, int ID)
        {
            var result = new List<GettaskinboxbyidDTO>();
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    a.cprocess_id AS processId,
                    c.cprocessname AS processName,
                    c.cprocessdescription AS processDesc,
                    d.cactivityname AS activityName,
                    d.cactivity_description AS activityDesc,
                    c.cpriority_label AS priorityLabel,
                    b.ccurrent_status AS taskStatus,
                    d.cparticipant_type AS participantType,
                    d.caction_privilege AS actionPrivilege,
                    d.cmapping_type AS assigneeType,
                    d.cmapping_code AS assigneeValue,
                    d.csla_day AS slaDays,
                    d.csla_Hour AS slaHours,
                    d.ctask_type AS executionType,
                    c.nshow_timeline AS showTimeline,
                    a.lcreated_date AS taskInitiatedDate,
                    b.lcurrent_status_date AS taskAssignedDate,
                    e.cfirst_name + ' ' + e.clast_name AS assigneeName,
                    d.id AS processdetailid,
                    c.cmeta_id,
                    a.itaskno
                FROM tbl_taskflow_master a
                INNER JOIN tbl_taskflow_detail b ON a.id = b.iheader_id
                INNER JOIN tbl_process_engine_master c ON a.cprocess_id = c.ID
                INNER JOIN tbl_process_engine_details d ON c.ID = d.cheader_id AND d.ciseqno = b.iseqno
                INNER JOIN Users e ON e.cuserid = CONVERT(int, a.ccreated_by) 
                                   AND e.ctenant_id = a.ctenant_id
                WHERE b.id = @ID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", ID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                int processdetailid = reader["processdetailid"] == DBNull.Value ? 0 : Convert.ToInt32(reader["processdetailid"]);
                                int meta_id = reader["cmeta_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cmeta_id"]);
                                int itaskno = reader["itaskno"] == DBNull.Value ? 0 : Convert.ToInt32(reader["itaskno"]);

                                var mapping = new GettaskinboxbyidDTO
                                {
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>(),
                                    layout = new List<GetmetalayoutDTO>()
                                };

                                // Add timeline
                           
                                    if (mapping.showTimeline == true)

                                    {
                                    mapping.timeline.Add(new TimelineDTO
                                    {
                                        taskName = mapping.processName,
                                        assigneeName = reader["assigneeName"]?.ToString() ?? "",
                                        status = mapping.taskStatus,
                                        slaDays = mapping.slaDays,
                                        slaHours = mapping.slaHours
                                    });
                                }

                                // Load Conditions
                                await LoadProcessConditions(conn, mapping, processdetailid);

                                // Load Meta
                                await LoadMeta(conn, mapping, meta_id);

                                // Load Layout
                                await LoadLayout(conn, mapping, itaskno, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving task inbox list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }
        private async Task LoadProcessConditions(SqlConnection conn, GettaskinboxbyidDTO mapping, int seqno)
        {
            string sql = @"SELECT * FROM tbl_process_engine_condition 
                   WHERE cheader_id = @HeaderID AND ciseqno = @Seqno";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HeaderID", mapping.processId);
            cmd.Parameters.AddWithValue("@Seqno", seqno);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.board.Add(new GetprocessEngineConditionDTO
                {
                    ID = Convert.ToInt32(dr["ID"]),
                    cprocessCode = mapping.processName,
                    ciseqno = dr["ciseqno"] == DBNull.Value ? 0 : Convert.ToInt32(dr["ciseqno"]),                 
                    icondseqno = dr["icond_seqno"] == DBNull.Value ? 0 : Convert.ToInt32(dr["icond_seqno"]),                   
                    ctype = dr["ctype"]?.ToString() ?? "",
                    clabel = dr["clabel"]?.ToString() ?? "",
                    cplaceholder = dr["cplaceholder"]?.ToString() ?? "",
                    cisRequired = dr.SafeGetBoolean("cis_required"),
                    cisReadonly = dr.SafeGetBoolean("cis_readonly"),
                    cis_disabled = dr.SafeGetBoolean("cis_disabled"),
                    cfieldValue = dr["cfield_value"]?.ToString() ?? "",
                    cdatasource = dr["cdata_source"]?.ToString() ?? "",
                    ccondition = dr["ccondition"]?.ToString() ?? ""
                });
            }
        }

        private async Task LoadMeta(SqlConnection conn, GettaskinboxbyidDTO mapping, int meta_id)
        {
            string sql = @"SELECT * FROM tbl_process_meta_detail WHERE cheader_id = @HeaderID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@HeaderID", meta_id);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cinputType = dr["cInput_type"]?.ToString() ?? "",
                    clabel = dr["label"]?.ToString() ?? "",
                    cplaceholder = dr["cPlaceholder"]?.ToString() ?? "",
                    cisRequired = dr.SafeGetBoolean("cis_Required"),
                    cisReadonly = dr.SafeGetBoolean("cis_readonly"),
                    cisDisabled = dr.SafeGetBoolean("cis_disabled"),
                    cfieldValue = dr["cfield_value"]?.ToString() ?? "",
                    cdatasource = dr["cdata_source"]?.ToString() ?? ""
                });
            }
        }
        private async Task LoadLayout(SqlConnection conn, GettaskinboxbyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT * FROM tbl_transaction_process_meta_layout
                   WHERE citaskno = @TaskNo AND ctenant_id = @TenantID
                   ORDER BY ID DESC";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.layout.Add(new GetmetalayoutDTO
                {
                    ID = Convert.ToInt32(dr["ID"]),
                    cprocess_id = Convert.ToInt32(dr["cprocess_id"]),
                    cdata = dr.IsDBNull(dr.GetOrdinal("cdata")) ? "" : dr.GetString(dr.GetOrdinal("cdata"))
                });
            }
        }


    }
}





