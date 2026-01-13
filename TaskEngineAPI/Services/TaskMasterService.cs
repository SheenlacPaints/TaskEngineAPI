
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using System.Reflection.PortableExecutable;
using System.Xml.Linq;
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
                        lcreated_date, ccreated_by, cmodified_by, lmodified_date, cprocess_id,cremarks
                    ) VALUES (
                        @itaskno, @TenantID, @ctask_type, @ctask_name, @ctask_description, @cstatus,
                        @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cprocess_id,@cremarks
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
                            cmd.Parameters.AddWithValue("@cremarks", (object?)model.cremarks ?? DBNull.Value);
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
                        bool isFirstRow = true;
                        foreach (var row in detailRows)
                        {
                            string currentStatus = isFirstRow ? "P" : "N";
                            using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdInsert.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdInsert.Parameters.AddWithValue("@iseqno", row["ciseqno"]);
                                cmdInsert.Parameters.AddWithValue("@iheader_id", masterId);
                                cmdInsert.Parameters.AddWithValue("@ctenent_id", tenantId);
                                cmdInsert.Parameters.AddWithValue("@ctask_type", row["ctasktype"]);
                                cmdInsert.Parameters.AddWithValue("@cmapping_code", row["cmapping_code"]);
                                cmdInsert.Parameters.AddWithValue("@ccurrent_status", currentStatus);
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
                            //if (primaryDetailId == 0)
                            //{
                            //    primaryDetailId = detailId;
                            //}
                            if (isFirstRow)
                            {
                                primaryDetailId = detailId;
                                isFirstRow = false; // IMPORTANT: Update the flag after first row
                            }
                            using (var cmdStatus = new SqlCommand(queryStatus, conn, transaction))
                            {
                                cmdStatus.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdStatus.Parameters.AddWithValue("@ctenent_id", tenantId);
                                cmdStatus.Parameters.AddWithValue("@cheader_id", 1);
                                cmdStatus.Parameters.AddWithValue("@cdetail_id", detailId);
                                cmdStatus.Parameters.AddWithValue("@cstatus", currentStatus);
                                cmdStatus.Parameters.AddWithValue("@cstatus_with", row["cmapping_code"]);
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
                                    cmd.Parameters.AddWithValue("@cdetail_id", userName);

                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        using (SqlCommand cmd = new SqlCommand("sp_task_updatereportingflow", conn, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@ID", masterId);
                            cmd.Parameters.AddWithValue("@itaskno", newTaskNo);
                            cmd.Parameters.AddWithValue("@ctenantID", tenantId);
                            cmd.ExecuteNonQuery();
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

        public async Task<string> GetAllProcessmetadetailAsync(int cTenantID, int metaid)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_Process_meta_detail", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                    cmd.Parameters.AddWithValue("@metaid", metaid);
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

        //        public async Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege)
        //        {
        //            try
        //            {
        //                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
        //                {
        //                    await con.OpenAsync();

        //                    if (!int.TryParse(value, out int privilegeId))
        //                    {
        //                        return JsonConvert.SerializeObject(new List<object>
        //                {
        //                    new { name = value, workflow = new List<object>() }
        //                }, Formatting.Indented);
        //                    }

        //                    string query = @"
        //SELECT DISTINCT    
        //    a.cprocess_id,
        //    a.[cprocesscode],
        //    b.[cprocessname],
        //    b.[cprocessdescription],
        //    b.cmeta_id,
        //    b.cvalue,
        //    -- Get entity name from cvalue using CASE conditions
        //    CASE  
        //        WHEN p.cprocess_privilege = 'role' THEN 
        //            (SELECT TOP 1 crole_name FROM tbl_role_master WHERE crole_code = b.cvalue)
        //        WHEN p.cprocess_privilege = 'user' THEN 
        //            (SELECT TOP 1 cuser_name FROM users WHERE CAST(cuserid AS VARCHAR(50)) = b.cvalue)
        //        WHEN p.cprocess_privilege = 'department' THEN
        //            (SELECT TOP 1 cdepartment_name FROM tbl_department_master WHERE cdepartment_code = b.cvalue)
        //        WHEN p.cprocess_privilege = 'position' THEN 
        //            (SELECT TOP 1 cposition_name FROM tbl_position_master WHERE cposition_code = b.cvalue)
        //        ELSE b.cvalue
        //    END AS entity_name
        //FROM [dbo].[tbl_engine_master_to_process_privilege] a 
        //INNER JOIN tbl_process_engine_master b ON a.cprocess_id = b.ID
        //INNER JOIN tbl_process_privilege_type p ON b.cprivilege_type = p.ID AND b.ctenant_id = p.ctenant_id
        //WHERE a.cis_active = 1 
        //    AND a.[cprocess_privilege] = @privilegeId";

        //                    var groupedWorkflows = new Dictionary<string, List<object>>();

        //                    using (var cmd = new SqlCommand(query, con))
        //                    {
        //                        cmd.Parameters.AddWithValue("@privilegeId", privilegeId);

        //                        using (var reader = await cmd.ExecuteReaderAsync())
        //                        {
        //                            while (await reader.ReadAsync())
        //                            {
        //                                string entityName = reader["entity_name"]?.ToString() ?? "";

        //                                if (string.IsNullOrEmpty(entityName))
        //                                    continue;

        //                                if (!groupedWorkflows.ContainsKey(entityName))
        //                                {
        //                                    groupedWorkflows[entityName] = new List<object>();
        //                                }

        //                                groupedWorkflows[entityName].Add(new
        //                                {
        //                                    cprocess_id = reader["cprocess_id"] != DBNull.Value ? Convert.ToInt32(reader["cprocess_id"]) : 0,
        //                                    cprocesscode = reader["cprocesscode"]?.ToString() ?? "",
        //                                    cprocessname = reader["cprocessname"]?.ToString() ?? "",
        //                                    cprocessdescription = reader["cprocessdescription"]?.ToString() ?? "",
        //                                    cmeta_id = reader["cmeta_id"] != DBNull.Value ? Convert.ToInt32(reader["cmeta_id"]) : 0
        //                                });
        //                            }
        //                        }
        //                    }

        //                    var result = new List<object>();
        //                    foreach (var group in groupedWorkflows)
        //                    {
        //                        result.Add(new
        //                        {
        //                            name = group.Key,
        //                            workflow = group.Value
        //                        });
        //                    }

        //                    return JsonConvert.SerializeObject(result, Formatting.Indented);
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                return JsonConvert.SerializeObject(new List<object>
        //        {
        //            new
        //            {
        //                name = value,
        //                workflow = new List<object>(),
        //                error = ex.Message
        //            }
        //        }, Formatting.Indented);
        //            }
        //        }
        public async Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await con.OpenAsync();

                    if (!int.TryParse(cprivilege, out int privilegeId))
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
         AND a.[cprocess_privilege] = @privilegeId 
         AND b.cvalue = @cvalue 
         AND b.ctenant_id = @tenantId";

                    // Dictionary to group workflows by entity_name
                    var groupedWorkflows = new Dictionary<string, List<object>>();

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@privilegeId", privilegeId);
                        cmd.Parameters.AddWithValue("@cvalue", value);
                        cmd.Parameters.AddWithValue("@tenantId", cTenantID);

                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string entityName = reader["entity_name"]?.ToString() ?? value;

                                var workflowItem = new
                                {
                                    cprocess_id = reader.IsDBNull(reader.GetOrdinal("cprocess_id")) ? 0 : Convert.ToInt32(reader["cprocess_id"]),
                                    cprocesscode = reader["cprocesscode"]?.ToString() ?? "",
                                    cprocessname = reader["cprocessname"]?.ToString() ?? "",
                                    cprocessdescription = reader["cprocessdescription"]?.ToString() ?? "",
                                    cmeta_id = reader.IsDBNull(reader.GetOrdinal("cmeta_id")) ? 0 : Convert.ToInt32(reader["cmeta_id"])
                                };

                                if (!groupedWorkflows.ContainsKey(entityName))
                                {
                                    groupedWorkflows[entityName] = new List<object>();
                                }
                                groupedWorkflows[entityName].Add(workflowItem);
                            }
                        }
                    }

                    var result = groupedWorkflows.Select(g => new
                    {
                        name = g.Key,
                        workflow = g.Value
                    }).ToList();
                    if (result.Count == 0)
                    {
                        result.Add(new { name = value, workflow = new List<object>() });
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

        public async Task<string> GetDropDownFilterAsync(int cTenantID, GetDropDownFilterDTO filterDto)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_dropdown_filter", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@FilterValue1", (object)filterDto.filtervalue1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FilterValue2", (object)filterDto.filtervalue2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FilterValue3", (object)filterDto.filtervalue3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FilterValue4", (object)filterDto.filtervalue4 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@FilterValue5", (object)filterDto.filtervalue5 ?? DBNull.Value);


                    var ds = new DataSet();
                    var adapter = new SqlDataAdapter(cmd);
                    await Task.Run(() => adapter.Fill(ds));

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

        //public async Task<string> Gettaskapprove(int cTenantID, string username)
        //{
        //    try
        //    {
        //        using (var con = new SqlConnection(_config.GetConnectionString("Database")))
        //        using (var cmd = new SqlCommand("sp_get_worflow_approved", con))
        //        {
        //            cmd.CommandType = CommandType.StoredProcedure;
        //            cmd.Parameters.AddWithValue("@tenentid", cTenantID);
        //            cmd.Parameters.AddWithValue("@userid", username);
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

        public async Task<string> DeptposrolecrudAsync(DeptPostRoleDTO model, int cTenantID, string username)
        {
            try
            {
                using (var con = new SqlConnection(this._config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_tables_crud", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenantid", cTenantID.ToString());
                    cmd.Parameters.AddWithValue("@action", model.action ?? "");
                    cmd.Parameters.AddWithValue("@user", username ?? "");
                    cmd.Parameters.AddWithValue("@table", model.table ?? "");
                    cmd.Parameters.AddWithValue("@userid", model.userid ?? "");
                    cmd.Parameters.AddWithValue("@position", model.position ?? "");
                    cmd.Parameters.AddWithValue("@role", model.role ?? "");
                    cmd.Parameters.AddWithValue("@departmentname", model.departmentname ?? "");
                    cmd.Parameters.AddWithValue("@departmentdesc", model.departmentdesc ?? "");
                    cmd.Parameters.AddWithValue("@cdepartmentmanagerrolecode", model.cdepartmentmanagerrolecode ?? "");
                    cmd.Parameters.AddWithValue("@cdepartmentmanagername", model.cdepartmentmanagername ?? "");
                    cmd.Parameters.AddWithValue("@cdepartmentemail", model.cdepartmentemail ?? "");
                    cmd.Parameters.AddWithValue("@cdepartmentphone", model.cdepartmentphone ?? "");
                    cmd.Parameters.AddWithValue("@nisactive", (model.nisactive ?? true) ? "1" : "0");
                    cmd.Parameters.AddWithValue("@cdepartmentcode", model.cdepartmentcode ?? "");
                    cmd.Parameters.AddWithValue("@rolename", model.rolename ?? "");
                    cmd.Parameters.AddWithValue("@rolelevel", model.rolelevel?.ToString() ?? "0");
                    cmd.Parameters.AddWithValue("@roledescription", model.roledescription ?? "");
                    cmd.Parameters.AddWithValue("@positionname", model.positionname ?? "");
                    cmd.Parameters.AddWithValue("@positioncode", model.positioncode ?? "");
                    cmd.Parameters.AddWithValue("@positiondescription", model.positiondescription ?? "");
                    cmd.Parameters.AddWithValue("@creportingmanagerpositionid", model.creportingmanagerpositionid?.ToString() ?? "0");
                    cmd.Parameters.AddWithValue("@creportingmanagername", model.creportingmanagername ?? "");
                    cmd.Parameters.AddWithValue("@rolecode", model.rolecode ?? "");
                    cmd.Parameters.AddWithValue("@id", model.id.ToString() ?? "0");

                    var ds = new DataSet();
                    var adapter = new SqlDataAdapter(cmd);
                    await Task.Run(() => adapter.Fill(ds));

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
                    [ctenant_id],[cprocess_privilege],[cprocess_id],[cprocesscode],
                    [cis_active],[ccreated_by],[lcreated_date],[cmodified_by],[lmodified_date]
                ) VALUES (
                    @ctenant_id, @cprocess_privilege, @cprocess_id, @cprocesscode, 
                     @cis_active, @ccreated_by, @lcreated_date, 
                    @cmodified_by, @lmodified_date); SELECT SCOPE_IDENTITY()";


                        using (var cmdInsert = new SqlCommand(query, conn, transaction))
                        {
                            cmdInsert.Parameters.AddWithValue("@ctenant_id", cTenantID);
                            cmdInsert.Parameters.AddWithValue("@cprocess_privilege", model.privilege);
                            //cmdInsert.Parameters.AddWithValue("@cseq_id", "1");
                            //cmdInsert.Parameters.AddWithValue("@ciseqno", "1");
                            cmdInsert.Parameters.AddWithValue("@cprocess_id", model.cprocess_id);
                            cmdInsert.Parameters.AddWithValue("@cprocesscode", model.cprocess_code);
                            //cmdInsert.Parameters.AddWithValue("@cprocessname", model.cprocess_name);
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
                         [cheader_id],entity_id, ctenant_id, cis_active,ccreated_by,lcreated_date, 
                    cmodified_by, lmodified_date,cprocess_id) VALUES (
                        @cheader_id,@entity_id, @ctenent_id, @cis_active, @ccreated_by, 
                        @lcreated_date, @cmodified_by, @lmodified_date, @cprocess_id);";

                        foreach (var row in model.privilegeMapping)
                        {
                            using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
                            {
                                //cmdInsert.Parameters.AddWithValue("@privilege_id", masterId);
                                //cmdInsert.Parameters.AddWithValue("@entity_type", row.entity_type);
                                cmdInsert.Parameters.AddWithValue("@cheader_id", model.cheader_id);
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

        public async Task<string> GetTaskInitiator(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetTaskinitiateList> tsk = new List<GetTaskinitiateList>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                string query = "sp_get_workflow_initiator";
                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@userid", username);
                        cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                        cmd.Parameters.AddWithValue("@searchtext", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
                            {
                                List<GetTaskinitiateDetails> tskdtl = new List<GetTaskinitiateDetails>();
                                GetTaskinitiateList p = new GetTaskinitiateList
                                {
                                    ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                    cprocessID = sdr.IsDBNull(sdr.GetOrdinal("cprocess_id")) ? 0 : Convert.ToInt32(sdr["cprocess_id"]),
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
                                    cprocessdescription = sdr.IsDBNull(sdr.GetOrdinal("cprocessdescription")) ? string.Empty : Convert.ToString(sdr["cprocessdescription"])
                                };

                                using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                                {
                                    string query1 = "sp_get_workflow_initiatordeatils";
                                    using (SqlCommand cmd1 = new SqlCommand(query1, con1))
                                    {
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        cmd1.Parameters.AddWithValue("@userid", username);
                                        cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                        cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                        await con1.OpenAsync();
                                        using (SqlDataReader sdr1 = await cmd1.ExecuteReaderAsync())
                                        {
                                            while (await sdr1.ReadAsync())
                                            {
                                                GetTaskinitiateDetails pd = new GetTaskinitiateDetails
                                                {
                                                    ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                    iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                    itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                    iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                    ctasktype = sdr1.IsDBNull(sdr1.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr1["ctask_type"]),
                                                    cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                    cmappingcodename = sdr1.IsDBNull(sdr1.GetOrdinal("cmappingcodename")) ? string.Empty : Convert.ToString(sdr1["cmappingcodename"]),
                                                    ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                    lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                    cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1["cremarks"]),
                                                    inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                    cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                    cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                    csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                    csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                    cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprocess_type")) ? string.Empty : Convert.ToString(sdr1["cprocess_type"]),
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
                                                    cactivity_description = sdr1.IsDBNull(sdr1.GetOrdinal("cactivity_description")) ? string.Empty : Convert.ToString(sdr1["cactivity_description"]),
                                                    cmappingcode_name = sdr1.IsDBNull(sdr1.GetOrdinal("cmappingcodename")) ? string.Empty : Convert.ToString(sdr1["cmappingcodename"]),
                                                };
                                                tskdtl.Add(pd);
                                            }
                                        }
                                    }
                                }

                                p.TaskChildItems = tskdtl;
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
                    data = new List<GetTaskinitiateList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }

        public async Task<string> Gettaskapprove(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_worflow_approve", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@userid", username);
                        cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                        cmd.Parameters.AddWithValue("@searchtext", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
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
                                    cprocessdescription = sdr.IsDBNull(sdr.GetOrdinal("cprocessdescription")) ? string.Empty : Convert.ToString(sdr["cprocessdescription"])
                                };

                                using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                                {
                                    string query1 = "sp_get_worflow_approve_details";
                                    using (SqlCommand cmd1 = new SqlCommand(query1))
                                    {
                                        cmd1.Connection = con1;
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        cmd1.Parameters.AddWithValue("@userid", username);
                                        cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                        cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                        await con1.OpenAsync();
                                        using (SqlDataReader sdr1 = await cmd1.ExecuteReaderAsync())
                                        {
                                            while (await sdr1.ReadAsync())
                                            {
                                                GetTaskDetails pd = new GetTaskDetails
                                                {
                                                    ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                    iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                    itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                    iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                    ctasktype = sdr1.IsDBNull(sdr1.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr1["ctask_type"]),
                                                    cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                    ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                    lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                    cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1["cremarks"]),
                                                    inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                    cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                    cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                    csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                    csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                    cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprocess_type")) ? string.Empty : Convert.ToString(sdr1["cprocess_type"]),
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
                                    }
                                }

                                p.TaskChildItems = tskdtl;
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
                    data = new List<GetTaskList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }

        public async Task<string> Gettaskinbox(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_worflow_inbox", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@userid", username);
                        cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                        cmd.Parameters.AddWithValue("@searchtext", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
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
                                    createdbyavatar = sdr.IsDBNull(sdr.GetOrdinal("cprofile_image_name")) ? string.Empty : Convert.ToString(sdr["cprofile_image_name"]),
                                    modifiedbyavatar = sdr.IsDBNull(sdr.GetOrdinal("cprofile_image_name")) ? string.Empty : Convert.ToString(sdr["cprofile_image_name"]),
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
                                    cprocessdescription = sdr.IsDBNull(sdr.GetOrdinal("cprocessdescription")) ? string.Empty : Convert.ToString(sdr["cprocessdescription"])
                                };

                                using (SqlConnection con1 = new SqlConnection(_config.GetConnectionString("Database")))
                                {
                                    using (SqlCommand cmd1 = new SqlCommand("sp_get_worflow_inbox_details", con1))
                                    {
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        cmd1.Parameters.AddWithValue("@userid", username);
                                        cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                        cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                        await con1.OpenAsync();
                                        using (SqlDataReader sdr1 = await cmd1.ExecuteReaderAsync())
                                        {
                                            while (await sdr1.ReadAsync())
                                            {
                                                GetTaskDetails pd = new GetTaskDetails
                                                {
                                                    ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                    iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                    itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                    iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                    ctasktype = sdr1.IsDBNull(sdr1.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr1["ctask_type"]),
                                                    cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                    ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                    lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                    cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1["cremarks"]),
                                                    inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                    cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                    cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                    csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                    csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                    cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprocess_type")) ? string.Empty : Convert.ToString(sdr1["cprocess_type"]),
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
                                    }
                                }

                                p.TaskChildItems = tskdtl;
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
                    data = new List<GetTaskList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
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

        //public async Task<List<GettaskinboxbyidDTO>> Getinboxdatabyidold(int cTenantID, int ID)
        //{
        //    try
        //    {
        //        var result = new List<GettaskinboxbyidDTO>();
        //        var connStr = _config.GetConnectionString("Database");
        //        using (SqlConnection conn = new SqlConnection(connStr))
        //        {
        //            await conn.OpenAsync();

        //            string query = @"select a.cprocess_id as processId,c.cprocessname as processName,c.cprocessdescription as processDesc,
        //     d.cactivityname as activityName,d.cactivity_description as activityDesc,c.cpriority_label as priorityLabel ,
        //     b.ccurrent_status as taskStatus,d.cparticipant_type as participantType,
        //     d.caction_privilege as actionPrivilege,d.cmapping_type as assigneeType,d.cmapping_code as assigneeValue,
        //     d.csla_day as slaDays,d.csla_Hour as slaHours,d.ctask_type as executionType,
        //     c.nshow_timeline as showTimeline,a.lcreated_date as taskInitiatedDate,		 
        //     b.lcurrent_status_date as taskAssignedDate ,e.cfirst_name+ ' '+e.clast_name as assigneeName
        //    from tbl_taskflow_master a 
        //    inner join tbl_taskflow_detail b on a.id=b.iheader_id
        //    inner join tbl_process_engine_master c on a.cprocess_id=c.ID 
        //    inner join tbl_process_engine_details d on c.ID=d.cheader_id and d.ciseqno=b.iseqno 
        //    inner join Users e on e.cuserid= CONVERT(int,a.ccreated_by) and e.ctenant_id=a.ctenant_id 
        //    where b.id=@ID ";

        //            using (SqlCommand cmd = new SqlCommand(query, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
        //                cmd.Parameters.AddWithValue("@ID", ID);

        //                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //                {
        //                    while (await reader.ReadAsync())
        //                    {
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
        //                                      c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled, c.cdata_source
        //                                      FROM tbl_process_engine_condition c
        //                                      WHERE c.cheader_id = @HeaderID;";

        //                        using var condCmd = new SqlCommand(condQuery, conn);

        //                        condCmd.Parameters.AddWithValue("@HeaderID", mapping.processId);

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
        //                        result.Add(mapping);
        //                    }
        //                }
        //            }
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        throw new Exception($"Error retrieving task inbox list: {ex.Message}", ex);
        //    }
        //}

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

        public async Task<List<GettaskinboxbyidDTO>> Gettaskinboxdatabyid(int cTenantID, int ID, string username)
  
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
                    d.crejection_privilege AS crejection_privilege,
                    d.cmapping_type AS assigneeType,
                    d.cmapping_code AS assigneeValue,
                    d.csla_day AS slaDays,
                    d.csla_Hour AS slaHours,
                    d.ctask_type AS executionType,
                    c.nshow_timeline AS showTimeline,
                    a.lcreated_date AS taskInitiatedDate,
                    b.lcurrent_status_date AS taskAssignedDate,
                    e.cfirst_name + ' ' + e.clast_name AS assigneeName,
                    d.id AS processdetailid,e.cprofile_image_name,
                    c.cmeta_id,
                    a.itaskno,
                    a.cremarks
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
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    createdbyavatar =reader["cprofile_image_name"]?.ToString() ?? "",
                                    modifiedbyavatar = reader["cprofile_image_name"]?.ToString() ?? "",
                                    cremarks = reader["cremarks"]?.ToString() ?? "",
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>(),
                                    approvers = new List<PreviousapproverDTO>()
                                };

                                // Add timeline

                                //if (mapping.showTimeline == true)

                                //{
                                //    mapping.timeline.Add(new TimelineDTO
                                //    {
                                //        taskName = mapping.processName,                                      
                                //        status = mapping.taskStatus,
                                //        userName = mapping.taskStatus,
                                //        userAvatar= mapping.taskStatus,
                                //    });
                                //}
                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }
                                mapping.approvers = await GetPreviousapproverAsync(conn, ID, username, cTenantID);

                                // Load Conditions
                                await LoadProcessConditions(conn, mapping, processdetailid);

                                // Load Meta
                                await LoadMeta(conn, mapping, itaskno, cTenantID);
                                await GetPreviousapproverAsync(conn, ID, username, cTenantID);

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

        private async Task LoadMeta(SqlConnection conn, GettaskinboxbyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
                  c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
                   from [tbl_transaction_process_meta_layout] a 
                   inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
                 inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
                 where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
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
        private async Task<List<TimelineDTO>> GetTimelineAsync(SqlConnection conn, int ID)
        {
            var timelineList = new List<TimelineDTO>();

            string timelineQuery = @"SELECT t.ccurrent_status AS status,t.cremarks,t.ID,
            t.lcurrent_status_date AS statusDate,t.cmapping_code,t.cprocess_id,t.cactivityname,
            u.cuserid,u.cfirst_name + ' ' + u.clast_name AS userName,u.cprofile_image_path AS userAvatar
            FROM (SELECT b.ccurrent_status,b.lcurrent_status_date,b.cremarks,b.cmapping_code,a.cprocess_id,b.ID,
            ped.cactivityname FROM tbl_taskflow_master a LEFT JOIN tbl_taskflow_detail b ON a.ID = b.iheader_id
            LEFT JOIN tbl_process_engine_details ped ON ped.cheader_id = a.cprocess_id AND ped.ciseqno = b.iseqno
            WHERE a.ID IN ( SELECT iheader_id FROM tbl_taskflow_detail WHERE id = @ID)) t
            INNER JOIN Users u ON t.cmapping_code = u.cdept_code OR t.cmapping_code = u.cposition_code
            OR t.cmapping_code = u.croll_id OR t.cmapping_code = CONVERT(VARCHAR(250), u.cuserid) and u.nIs_deleted=0
            ORDER BY t.id asc";

            using (SqlCommand cmd = new SqlCommand(timelineQuery, conn))
            {
                cmd.Parameters.AddWithValue("@ID", ID);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        timelineList.Add(new TimelineDTO
                        {
                            status = reader["status"]?.ToString() ?? "",
                            remarks = reader["cremarks"]?.ToString() ?? "",
                            taskName = reader["cactivityname"]?.ToString() ?? "",
                            userName = reader["userName"]?.ToString() ?? "",
                            userAvatar = reader["userAvatar"]?.ToString() ?? ""

                        });
                    }
                }
            }

            return timelineList;
        }
        //private async Task<List<PreviousapproverDTO>> GetPreviousapproverAsync(SqlConnection conn, int ID)
        //{
        //    var timelineList = new List<PreviousapproverDTO>();
        //    string timelineQuery = @"
        //SELECT 
        //    t.ccurrent_status AS status,
        //    t.cremarks,
        //    t.ID,
        //    t.lcurrent_status_date AS statusDate,
        //    t.cmapping_code,
        //    t.cprocess_id,
        //    t.cactivityname,
        //    u.cuserid,
        //    u.cfirst_name + ' ' + u.clast_name AS userName,
        //    u.cprofile_image_path AS userAvatar
        //FROM (
        //    SELECT 
        //        b.ccurrent_status,
        //        b.lcurrent_status_date,
        //        b.cremarks,
        //        b.cmapping_code,
        //        a.cprocess_id,
        //        b.ID,
        //        b.iseqno,
        //        ped.cactivityname 
        //    FROM tbl_taskflow_master a 
        //    LEFT JOIN tbl_taskflow_detail b ON a.ID = b.iheader_id
        //    LEFT JOIN tbl_process_engine_details ped ON ped.cheader_id = a.cprocess_id AND ped.ciseqno = b.iseqno
        //    WHERE a.ID = (SELECT iheader_id FROM tbl_taskflow_detail WHERE id = @ID)
        //      AND b.iseqno < (SELECT iseqno FROM tbl_taskflow_detail WHERE id = @ID) 
        //) t
        //INNER JOIN Users u ON (
        //    t.cmapping_code = u.cdept_code OR 
        //    t.cmapping_code = u.cposition_code OR 
        //    t.cmapping_code = u.croll_id OR 
        //    t.cmapping_code = CONVERT(VARCHAR(250), u.cuserid)
        //) 
        //WHERE u.nIs_deleted = 0
        //ORDER BY t.iseqno ASC"; // Ordered by sequence to show progress chronologically

        //    using (SqlCommand cmd = new SqlCommand(timelineQuery, conn))
        //    {
        //        cmd.Parameters.AddWithValue("@ID", ID);

        //        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
        //        {
        //            while (await reader.ReadAsync())
        //            {
        //                timelineList.Add(new PreviousapproverDTO
        //                {

        //                    ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
        //                    activity = reader["cactivityname"]?.ToString() ?? "",
        //                    status = reader["status"]?.ToString() ?? "",
        //                    cremarks = reader["cremarks"]?.ToString() ?? "",
        //                    datatime = reader.IsDBNull(reader.GetOrdinal("statusDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("statusDate")),
        //                    pendingwith = reader["userName"]?.ToString() ?? "",
        //                    pendingwithavatar = reader["userAvatar"]?.ToString() ?? ""
        //                });
        //            }
        //        }
        //    }

        //    return timelineList;
        //}

        private async Task<List<PreviousapproverDTO>> GetPreviousapproverAsync(SqlConnection conn, int ID, string userid, int tenantid)
        {
            var timelineList = new List<PreviousapproverDTO>();

            using (SqlCommand cmd = new SqlCommand("sp_get_worflow_previous_approver", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@userid", userid);
                cmd.Parameters.AddWithValue("@tenentid", tenantid);
                cmd.Parameters.AddWithValue("@ID", ID);

                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        timelineList.Add(new PreviousapproverDTO
                        {
                            ID = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : Convert.ToInt32(reader["ID"]),
                            activity = reader["cactivityname"]?.ToString() ?? "",
                            status = reader["status"]?.ToString() ?? "",
                            cremarks = reader["cremarks"]?.ToString() ?? "",
                            datatime = reader.IsDBNull(reader.GetOrdinal("statusDate")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("statusDate")),
                            pendingwith = reader["userName"]?.ToString() ?? "",
                            pendingwithavatar = reader["userAvatar"]?.ToString() ?? "",
                            cboard_visablity_flag = reader["cboard_visablity_flag"]?.ToString() ?? "",
                            cboard_visablity = reader["cboard_visablity"]?.ToString() ?? ""
                        });
                    }
                }
            }

            return timelineList;
        }

        public async Task<bool> UpdatetaskapproveAsync(updatetaskDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                SqlTransaction transaction = conn.BeginTransaction();
                int? processId = null;
                int? taskNo = null;

                try
                {
                    string checkQuery = @"
                SELECT a.itaskno, b.cprocess_id, a.cis_reassigned 
                FROM tbl_taskflow_detail a
                INNER JOIN tbl_taskflow_master b ON a.iheader_id = b.ID 
                WHERE a.ID = @ID";

                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn, transaction))
                    {
                        checkCmd.Parameters.AddWithValue("@ID", model.ID);
                        using (var reader = await checkCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                taskNo = reader["itaskno"] as int? ?? model.itaskno;
                                processId = reader["cprocess_id"] as int?;
                                string alreadyReassigned = reader["cis_reassigned"]?.ToString() ?? "";

                                if (!string.IsNullOrEmpty(model.reassignto) && alreadyReassigned == "Y")
                                {
                                    reader.Close();
                                    transaction.Rollback();
                                    throw new InvalidOperationException("This task has already been reassigned and cannot be reassigned again.");
                                }
                            }
                            else
                            {
                                reader.Close();
                                transaction.Rollback();
                                return false;
                            }
                            reader.Close();
                        }
                    }

                    string updateQuery;
                    bool isReassigning = !string.IsNullOrEmpty(model.reassignto);

                    if (isReassigning)
                    {
                        updateQuery = @"UPDATE tbl_taskflow_detail SET 
                                creassign_to = @creassign_to, lreassign_Date = @lreassign_Date, 
                                cis_reassigned = @cis_reassigned, cremarks = @remarks WHERE ID = @ID";
                    }
                    else
                    {
                        updateQuery = @"UPDATE tbl_taskflow_detail SET 
                                ccurrent_status = @status, lcurrent_status_date = @status_date, cremarks = @remarks WHERE ID = @ID";
                    }

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@ID", model.ID);
                        if (isReassigning)
                        {
                            updateCmd.Parameters.AddWithValue("@creassign_to", model.reassignto);
                            updateCmd.Parameters.AddWithValue("@lreassign_Date", DateTime.Now);
                            updateCmd.Parameters.AddWithValue("@cis_reassigned", "Y");
                            updateCmd.Parameters.AddWithValue("@remarks", (object?)model.remarks ?? DBNull.Value);
                        }
                        else
                        {
                            updateCmd.Parameters.AddWithValue("@status", (object?)model.status ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@status_date", (object?)model.status_date ?? DBNull.Value);
                            updateCmd.Parameters.AddWithValue("@remarks", (object?)model.remarks ?? DBNull.Value);
                        }

                        await updateCmd.ExecuteNonQueryAsync();
                    }

                    string statusQuery = @"
                INSERT INTO tbl_transaction_taskflow_detail_and_status
                (itaskno, ctenant_id, cheader_id, cdetail_id, cstatus, cstatus_with, lstatus_date, cremarks, crejected_reason)
                VALUES(@itaskno, @ctenant_id, @cheader_id, @cdetail_id, @cstatus, @cstatus_with, @lstatus_date, @cremarks, @crejected_reason);";

                    using (SqlCommand statusCmd = new SqlCommand(statusQuery, conn, transaction))
                    {
                        statusCmd.Parameters.AddWithValue("@itaskno", taskNo ?? (object)DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                        statusCmd.Parameters.AddWithValue("@cheader_id", 2);
                        statusCmd.Parameters.AddWithValue("@cdetail_id", model.ID);
                        statusCmd.Parameters.AddWithValue("@cstatus", isReassigning ? "Reassign" : (object?)model.status ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@cstatus_with", username);
                        statusCmd.Parameters.AddWithValue("@lstatus_date", (object?)model.status_date ?? DateTime.Now);
                        statusCmd.Parameters.AddWithValue("@cremarks", (object?)model.remarks ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@crejected_reason", (object?)model.rejectedreason ?? DBNull.Value);
                        await statusCmd.ExecuteNonQueryAsync();
                    }

                    if (model.metaData != null && model.metaData.Any())
                    {
                        string metaQuery = @"
                    INSERT INTO tbl_transaction_process_meta_layout 
                    ([cmeta_id],[cprocess_id],[cprocess_code],[ctenant_id],[cdata],[citaskno],[cdetail_id]) 
                    VALUES (@cmeta_id, @cprocess_id, @cprocess_code, @TenantID, @cdata, @citaskno, @cdetail_id);";

                        foreach (var metaData in model.metaData)
                        {
                            using (SqlCommand metaInsertCmd = new SqlCommand(metaQuery, conn, transaction))
                            {
                                metaInsertCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                                metaInsertCmd.Parameters.AddWithValue("@cmeta_id", (object?)metaData.cmeta_id ?? DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@cprocess_id", processId ?? (object)DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@cprocess_code", "");
                                metaInsertCmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@citaskno", taskNo ?? (object)DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@cdetail_id", model.ID);
                                await metaInsertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }

                    if (model.status == "A")
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_update_pendingtasks_V1", conn, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@itasknoo", model.itaskno);
                            cmd.Parameters.AddWithValue("@ID", model.ID);
                            cmd.Parameters.AddWithValue("@ctenantid", cTenantID);
                            await cmd.ExecuteNonQueryAsync();
                        }
                    }

                    transaction.Commit();
                    return true;
                }
                catch (InvalidOperationException ex)
                {
                    throw new Exception(ex.Message);
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        public async Task<List<GettaskApprovedatabyidDTO>> Gettaskapprovedatabyid(int cTenantID, int ID)
        {
            var result = new List<GettaskApprovedatabyidDTO>();
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
                    d.crejection_privilege AS crejection_privilege,
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
                    c.cmeta_id,a.cremarks,
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

                                var mapping = new GettaskApprovedatabyidDTO
                                {
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    cremarks= reader["cremarks"]?.ToString() ?? "",
                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>()
                                };

                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }

                                await LoadProcessConditionsForApproved(conn, mapping, processdetailid);

                                await LoadMetaForApproved(conn, mapping, itaskno, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved task list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }

        private async Task LoadProcessConditionsForApproved(SqlConnection conn, GettaskApprovedatabyidDTO mapping, int seqno)
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

        private async Task LoadMetaForApproved(SqlConnection conn, GettaskApprovedatabyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
          c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
           from [tbl_transaction_process_meta_layout] a 
           inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
         inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
         where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
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

        public async Task<List<GettaskHolddatabyidDTO>> GettaskHolddatabyid(int cTenantID, int ID, string username)
        {
            var result = new List<GettaskHolddatabyidDTO>();
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
                    d.crejection_privilege AS crejection_privilege,
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
                    a.itaskno,a.cremarks
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

                                var mapping = new GettaskHolddatabyidDTO
                                {
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",
                                    cremarks = reader["cremarks"]?.ToString() ?? "",
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>(),
                                    approvers = new List<PreviousapproverDTO>()
                                };

                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }
                                mapping.approvers = await GetPreviousapproverAsync(conn, ID, username, cTenantID);
                                await LoadProcessConditionsForHold(conn, mapping, processdetailid);

                                await LoadMetaForHold(conn, mapping, itaskno, cTenantID);
                                await GetPreviousapproverAsync(conn, ID, username, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved task list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }

        private async Task LoadProcessConditionsForHold(SqlConnection conn, GettaskHolddatabyidDTO mapping, int seqno)
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

        private async Task LoadMetaForHold(SqlConnection conn, GettaskHolddatabyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
          c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
           from [tbl_transaction_process_meta_layout] a 
           inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
         inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
         where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
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


        public async Task<string> GettaskHold(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_worflow_Hold_New", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@userid", username);
                        cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                        cmd.Parameters.AddWithValue("@searchtext", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
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
                                    cprocessdescription = sdr.IsDBNull(sdr.GetOrdinal("cprocessdescription")) ? string.Empty : Convert.ToString(sdr["cprocessdescription"])
                                };

                                using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                                {
                                    string query1 = "sp_get_worflow_Hold_details";
                                    using (SqlCommand cmd1 = new SqlCommand(query1))
                                    {
                                        cmd1.Connection = con1;
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        cmd1.Parameters.AddWithValue("@userid", username);
                                        cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                        cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                        await con1.OpenAsync();
                                        using (SqlDataReader sdr1 = await cmd1.ExecuteReaderAsync())
                                        {
                                            while (await sdr1.ReadAsync())
                                            {
                                                GetTaskDetails pd = new GetTaskDetails
                                                {
                                                    ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                    iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                    itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                    iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                    ctasktype = sdr1.IsDBNull(sdr1.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr1["ctask_type"]),
                                                    cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                    ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                    lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                    cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1["cremarks"]),
                                                    inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                    cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                    cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                    csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                    csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                    cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprocess_type")) ? string.Empty : Convert.ToString(sdr1["cprocess_type"]),
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
                                    }
                                }

                                p.TaskChildItems = tskdtl;
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
                    data = new List<GetTaskList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }

        public async Task<bool> UpdatetaskHoldAsync(updatetaskDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                SqlTransaction transaction = conn.BeginTransaction();
                int? processId = null;
                int? taskNo = null;
                try
                {
                    string updateQuery = @"UPDATE tbl_taskflow_detail  SET ccurrent_status = @status, 
                 lcurrent_status_date = @status_date ,cremarks=@remarks,creassign_to=@creassign_to WHERE ID = @ID";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@status", (object?)model.status ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@status_date", (object?)model.status_date ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@remarks", (object?)model.remarks ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@ID", model.ID);
                        updateCmd.Parameters.AddWithValue("@creassign_to", (object?)model.reassignto ?? DBNull.Value);
                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    string selectQuery = @"
                SELECT a.itaskno, b.cprocess_id FROM tbl_taskflow_detail a
                INNER JOIN tbl_taskflow_master b ON a.iheader_id = b.ID WHERE a.ID = @ID";

                    using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn, transaction))
                    {
                        selectCmd.Parameters.AddWithValue("@ID", model.ID);

                        using (var reader = await selectCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                taskNo = reader["itaskno"] as int? ?? model.itaskno;
                                processId = reader["cprocess_id"] as int?;
                            }

                            reader.Close();
                        }
                    }

                    string statusQuery = @"
                INSERT INTO tbl_transaction_taskflow_detail_and_status
                (itaskno, ctenant_id, cheader_id, cdetail_id, cstatus, cstatus_with, lstatus_date,cremarks,crejected_reason)
                VALUES(@itaskno, @ctenant_id, @cheader_id, @cdetail_id, @cstatus, @cstatus_with, @lstatus_date,@cremarks,@crejected_reason);";

                    using (SqlCommand statusCmd = new SqlCommand(statusQuery, conn, transaction))
                    {
                        statusCmd.Parameters.AddWithValue("@itaskno", taskNo);
                        statusCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                        statusCmd.Parameters.AddWithValue("@cheader_id", 2);
                        statusCmd.Parameters.AddWithValue("@cdetail_id", model.ID);
                        statusCmd.Parameters.AddWithValue("@cstatus", (object?)model.status ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@cstatus_with", username);
                        statusCmd.Parameters.AddWithValue("@lstatus_date", (object?)model.status_date ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@cremarks", (object?)model.remarks ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@crejected_reason", (object?)model.rejectedreason ?? DBNull.Value);
                        await statusCmd.ExecuteNonQueryAsync();
                    }

                    string metaQuery = @"
                INSERT INTO tbl_transaction_process_meta_layout (
                [cmeta_id],[cprocess_id],[cprocess_code],[ctenant_id],[cdata],[citaskno],[cdetail_id]) VALUES (
                @cmeta_id, @cprocess_id, @cprocess_code, @TenantID, @cdata, @citaskno, @cdetail_id);";
                    if (model.metaData != null)
                    {
                        foreach (var metaData in model.metaData)
                        {
                            using (SqlCommand metaInsertCmd = new SqlCommand(metaQuery, conn, transaction))
                            {
                                metaInsertCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                                metaInsertCmd.Parameters.AddWithValue("@cmeta_id", (object?)metaData.cmeta_id ?? DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@cprocess_id", processId ?? (object)DBNull.Value); // Using retrieved value
                                metaInsertCmd.Parameters.AddWithValue("@cprocess_code", "");
                                metaInsertCmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@citaskno", taskNo);
                                metaInsertCmd.Parameters.AddWithValue("@cdetail_id", model.ID);
                                await metaInsertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }


                    if (model.status == "H")
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_update_pendingtasks_V1", conn, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@itasknoo", model.itaskno);
                            cmd.Parameters.AddWithValue("@ID", model.ID);
                            cmd.Parameters.AddWithValue("@ctenantid", cTenantID);
                            cmd.ExecuteNonQuery();
                        }
                    }


                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<List<GettaskRejectdatabyidDTO>> GettaskRejectdatabyid(int cTenantID, int ID)
        {
            var result = new List<GettaskRejectdatabyidDTO>();
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
                    d.crejection_privilege AS crejection_privilege,
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
                    a.itaskno,a.cremarks
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

                                var mapping = new GettaskRejectdatabyidDTO
                                {
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",
                                    cremarks = reader["cremarks"]?.ToString() ?? "",
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>()
                                };

                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }

                                await LoadProcessConditionsForReject(conn, mapping, processdetailid);

                                await LoadMetaForReject(conn, mapping, itaskno, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved task list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }

        private async Task LoadProcessConditionsForReject(SqlConnection conn, GettaskRejectdatabyidDTO mapping, int seqno)
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

        private async Task LoadMetaForReject(SqlConnection conn, GettaskRejectdatabyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
          c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
           from [tbl_transaction_process_meta_layout] a 
           inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
         inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
         where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
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

        public async Task<string> GettaskReject(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_worflow_reject", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@userid", username);
                        cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                        cmd.Parameters.AddWithValue("@searchtext", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
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
                                    cprocessdescription = sdr.IsDBNull(sdr.GetOrdinal("cprocessdescription")) ? string.Empty : Convert.ToString(sdr["cprocessdescription"])
                                };

                                using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                                {
                                    string query1 = "sp_get_worflow_reject_details";
                                    using (SqlCommand cmd1 = new SqlCommand(query1))
                                    {
                                        cmd1.Connection = con1;
                                        cmd1.CommandType = CommandType.StoredProcedure;
                                        cmd1.Parameters.AddWithValue("@userid", username);
                                        cmd1.Parameters.AddWithValue("@tenentid", cTenantID);
                                        cmd1.Parameters.AddWithValue("@itaskno", p.itaskno);

                                        await con1.OpenAsync();
                                        using (SqlDataReader sdr1 = await cmd1.ExecuteReaderAsync())
                                        {
                                            while (await sdr1.ReadAsync())
                                            {
                                                GetTaskDetails pd = new GetTaskDetails
                                                {
                                                    ID = sdr1.IsDBNull(sdr1.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr1["ID"]),
                                                    iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr1["iheader_id"]),
                                                    itaskno = sdr1.IsDBNull(sdr1.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr1["itaskno"]),
                                                    iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                    ctasktype = sdr1.IsDBNull(sdr1.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr1["ctask_type"]),
                                                    cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                    ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                    lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                    cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1["cremarks"]),
                                                    inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                    cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                    cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                    csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(sdr1["csla_day"]),
                                                    csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                    cprocess_type = sdr1.IsDBNull(sdr1.GetOrdinal("cprocess_type")) ? string.Empty : Convert.ToString(sdr1["cprocess_type"]),
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
                                    }
                                }

                                p.TaskChildItems = tskdtl;
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
                    data = new List<GetTaskList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }

        public async Task<bool> UpdatetaskRejectAsync(updatetaskDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                SqlTransaction transaction = conn.BeginTransaction();
                int? processId = null;
                int? taskNo = null;
                try
                {
                    string updateQuery = @"UPDATE tbl_taskflow_detail  SET ccurrent_status = @status, 
                 lcurrent_status_date = @status_date ,cremarks=@remarks,creassign_to=@creassign_to WHERE ID = @ID";

                    using (SqlCommand updateCmd = new SqlCommand(updateQuery, conn, transaction))
                    {
                        updateCmd.Parameters.AddWithValue("@status", (object?)model.status ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@status_date", (object?)model.status_date ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@remarks", (object?)model.remarks ?? DBNull.Value);
                        updateCmd.Parameters.AddWithValue("@ID", model.ID);
                        updateCmd.Parameters.AddWithValue("@creassign_to", (object?)model.reassignto ?? DBNull.Value);
                        int rowsAffected = await updateCmd.ExecuteNonQueryAsync();

                        if (rowsAffected == 0)
                        {
                            transaction.Rollback();
                            return false;
                        }
                    }

                    string selectQuery = @"
                SELECT a.itaskno, b.cprocess_id FROM tbl_taskflow_detail a
                INNER JOIN tbl_taskflow_master b ON a.iheader_id = b.ID WHERE a.ID = @ID";

                    using (SqlCommand selectCmd = new SqlCommand(selectQuery, conn, transaction))
                    {
                        selectCmd.Parameters.AddWithValue("@ID", model.ID);

                        using (var reader = await selectCmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                taskNo = reader["itaskno"] as int? ?? model.itaskno;
                                processId = reader["cprocess_id"] as int?;
                            }

                            reader.Close();
                        }
                    }

                    string statusQuery = @"
                INSERT INTO tbl_transaction_taskflow_detail_and_status
                (itaskno, ctenant_id, cheader_id, cdetail_id, cstatus, cstatus_with, lstatus_date,cremarks,crejected_reason)
                VALUES(@itaskno, @ctenant_id, @cheader_id, @cdetail_id, @cstatus, @cstatus_with, @lstatus_date,@cremarks,@crejected_reason);";

                    using (SqlCommand statusCmd = new SqlCommand(statusQuery, conn, transaction))
                    {
                        statusCmd.Parameters.AddWithValue("@itaskno", taskNo);
                        statusCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                        statusCmd.Parameters.AddWithValue("@cheader_id", 2);
                        statusCmd.Parameters.AddWithValue("@cdetail_id", model.ID);
                        statusCmd.Parameters.AddWithValue("@cstatus", (object?)model.status ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@cstatus_with", username);
                        statusCmd.Parameters.AddWithValue("@lstatus_date", (object?)model.status_date ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@cremarks", (object?)model.remarks ?? DBNull.Value);
                        statusCmd.Parameters.AddWithValue("@crejected_reason", (object?)model.rejectedreason ?? DBNull.Value);
                        await statusCmd.ExecuteNonQueryAsync();
                    }

                    string metaQuery = @"
                INSERT INTO tbl_transaction_process_meta_layout (
                [cmeta_id],[cprocess_id],[cprocess_code],[ctenant_id],[cdata],[citaskno],[cdetail_id]) VALUES (
                @cmeta_id, @cprocess_id, @cprocess_code, @TenantID, @cdata, @citaskno, @cdetail_id);";
                    if (model.metaData != null)
                    {
                        foreach (var metaData in model.metaData)
                        {
                            using (SqlCommand metaInsertCmd = new SqlCommand(metaQuery, conn, transaction))
                            {
                                metaInsertCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                                metaInsertCmd.Parameters.AddWithValue("@cmeta_id", (object?)metaData.cmeta_id ?? DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@cprocess_id", processId ?? (object)DBNull.Value); // Using retrieved value
                                metaInsertCmd.Parameters.AddWithValue("@cprocess_code", "");
                                metaInsertCmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
                                metaInsertCmd.Parameters.AddWithValue("@citaskno", taskNo);
                                metaInsertCmd.Parameters.AddWithValue("@cdetail_id", model.ID);
                                await metaInsertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }


                    if (model.status == "R")
                    {
                        using (SqlCommand cmd = new SqlCommand("sp_update_pendingtasks_V1", conn, transaction))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@itasknoo", model.itaskno);
                            cmd.Parameters.AddWithValue("@ID", model.ID);
                            cmd.Parameters.AddWithValue("@ctenantid", cTenantID);
                            cmd.ExecuteNonQuery();
                        }
                    }


                    transaction.Commit();
                    return true;
                }
                catch (Exception)
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        public async Task<List<GetopentasklistdatabyidDTO>> Getopentasklistdatabyid(int cTenantID, int ID)
        {
            var result = new List<GetopentasklistdatabyidDTO>();
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
                    d.crejection_privilege AS crejection_privilege,
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

                                var mapping = new GetopentasklistdatabyidDTO
                                {
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",

                                    taskInitiatedDate = reader["taskInitiatedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["taskInitiatedDate"]),
                                    taskAssignedDate = reader["taskAssignedDate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(reader["taskAssignedDate"]),

                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",

                                    showTimeline = reader["showTimeline"] == DBNull.Value ? false : Convert.ToBoolean(reader["showTimeline"]),

                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>()
                                };

                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }

                                await LoadProcessConditionsForopentasklist(conn, mapping, processdetailid);
                                await LoadMetaForopentasklist(conn, mapping, itaskno, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved task list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }

        private async Task LoadProcessConditionsForopentasklist(SqlConnection conn, GetopentasklistdatabyidDTO mapping, int seqno)
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

                    cisRequired = dr["cis_required"] == DBNull.Value ? false : Convert.ToBoolean(dr["cis_required"]),
                    cisReadonly = dr["cis_readonly"] == DBNull.Value ? false : Convert.ToBoolean(dr["cis_readonly"]),
                    cis_disabled = dr["cis_disabled"] == DBNull.Value ? false : Convert.ToBoolean(dr["cis_disabled"]),

                    cfieldValue = dr["cfield_value"]?.ToString() ?? "",
                    cdatasource = dr["cdata_source"]?.ToString() ?? "",
                    ccondition = dr["ccondition"]?.ToString() ?? ""
                });
            }
        }

        private async Task LoadMetaForopentasklist(SqlConnection conn, GetopentasklistdatabyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
      c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
       from [tbl_transaction_process_meta_layout] a 
       inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
     inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
     where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
                    cinputType = dr["cinput_type"]?.ToString() ?? "",
                    clabel = dr["label"]?.ToString() ?? "",
                    cplaceholder = dr["cplaceholder"]?.ToString() ?? "",

                    cisRequired = dr["cis_required"] == DBNull.Value ? false : Convert.ToBoolean(dr["cis_required"]),
                    cisReadonly = dr["cis_readonly"] == DBNull.Value ? false : Convert.ToBoolean(dr["cis_readonly"]),
                    cisDisabled = dr["cis_disabled"] == DBNull.Value ? false : Convert.ToBoolean(dr["cis_disabled"]),

                    cfieldValue = dr["cfield_value"]?.ToString() ?? "",
                    cdatasource = dr["cdata_source"]?.ToString() ?? ""
                });
            }
        }

        public async Task<string> Getopentasklist(int cTenantID, string username, string? searchText = null)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();

            string query = "sp_get_worflow_opentasklist";
            using (SqlConnection con = new SqlConnection(this._config.GetConnectionString("Database")))
            {
                using (SqlCommand cmd = new SqlCommand(query))
                {
                    cmd.Connection = con;
                    cmd.CommandType = CommandType.StoredProcedure;
                    //cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@searchtext", searchText);
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
                                cremarks = sdr.IsDBNull(sdr.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr["cremarks"]),

                                //privilege_name = sdr.IsDBNull(sdr.GetOrdinal("privilege_name")) ? string.Empty : Convert.ToString(sdr["privilege_name"])

                            };

                            using (SqlConnection con1 = new SqlConnection(this._config.GetConnectionString("Database")))
                            {
                                string query1 = "sp_get_worflow_opentasklist_details";
                                using (SqlCommand cmd1 = new SqlCommand(query1))
                                {
                                    cmd1.Connection = con1;
                                    cmd1.CommandType = CommandType.StoredProcedure;
                                    //cmd1.Parameters.AddWithValue("@userid", username);
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
                                                ctasktype = sdr1.IsDBNull(sdr.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr1["ctask_type"]),
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


        public async Task<List<GetmetaviewdataDTO>> Getmetaviewdatabyid(int cTenantID, int id)
        {
            try
            {
                var result = new List<GetmetaviewdataDTO>();
                var connStr = _config.GetConnectionString("Database");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"select d.citaskno,b.icond_seqno,b.ctype,b.clabel,b.cplaceholder,b.cfield_value,b.ccondition,
                                     b.cdata_source,d.cdata,cdetail_id
                                     from tbl_process_engine_details  a
                                     inner join  tbl_process_engine_condition b on a.cheader_id=b.cheader_id
                                     inner join tbl_taskflow_detail c on c.iseqno=a.ciseqno and a.ID=b.ciseqno
                                     inner join tbl_transaction_process_meta_layout d on d.citaskno=c.itaskno and d.cdetail_id=c.id
                                     and d.cmeta_id=b.id
                                     inner join tbl_taskflow_master e on e.itaskno=c.itaskno and e.ID=c.iheader_id
                                     where a.cheader_id=e.cprocess_id and c.itaskno=d.citaskno and c.id=@ID order by b.icond_seqno asc";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var mapping = new GetmetaviewdataDTO
                                {
                                    ID = reader["cdetail_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cdetail_id"]),
                                    itaskno = reader["citaskno"] == DBNull.Value ? 0 : Convert.ToInt32(reader["citaskno"]),
                                    icond_seqno = reader["icond_seqno"] == DBNull.Value ? 0 : Convert.ToInt32(reader["icond_seqno"]),
                                    ctype = reader["ctype"]?.ToString() ?? "",
                                    clabel = reader["clabel"]?.ToString() ?? "",
                                    cplaceholder = reader["cplaceholder"]?.ToString() ?? "",
                                    cfield_value = reader["cfield_value"]?.ToString() ?? "",
                                    ccondition = reader["ccondition"]?.ToString() ?? "",
                                    cdata_source = reader["cdata_source"]?.ToString() ?? "",
                                    cdata = reader["cdata"]?.ToString() ?? "",

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

        public async Task<GettaskreassignCountDTO> GettaskReassign(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();
            int totalCount = 0;

            string connectionString = this._config.GetConnectionString("Database");
            string query = "sp_get_worflow_Reassign";

            using (SqlConnection con = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@searchtext", (object)searchText ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNo", page);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    await con.OpenAsync();

                    using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                    {

                        while (await sdr.ReadAsync())
                        {
                            GetTaskList p = new GetTaskList
                            {
                                ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                itaskno = sdr.IsDBNull(sdr.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                ctasktype = sdr["ctask_type"]?.ToString() ?? string.Empty,
                                ctaskname = sdr["ctask_name"]?.ToString() ?? string.Empty,
                                ctaskdescription = sdr["ctask_description"]?.ToString() ?? string.Empty,
                                cstatus = sdr["cstatus"]?.ToString() ?? string.Empty,
                                lcompleteddate = sdr.IsDBNull(sdr.GetOrdinal("lcompleted_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcompleted_date")),
                                ccreatedby = sdr["ccreated_by"]?.ToString() ?? string.Empty,
                                ccreatedbyname = sdr["ccreated_byname"]?.ToString() ?? string.Empty,
                                lcreateddate = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                cmodifiedby = sdr["cmodified_by"]?.ToString() ?? string.Empty,
                                cmodifiedbyname = sdr["cmodified_byname"]?.ToString() ?? string.Empty,
                                lmodifieddate = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date")),
                                Employeecode = sdr["Employeecode"]?.ToString() ?? string.Empty,
                                Employeename = sdr["Employeename"]?.ToString() ?? string.Empty,
                                EmpDepartment = sdr["EmpDepartment"]?.ToString() ?? string.Empty,
                                cprocess_id = sdr.IsDBNull(sdr.GetOrdinal("cprocess_id")) ? 0 : Convert.ToInt32(sdr["cprocess_id"]),
                                cprocesscode = sdr["cprocesscode"]?.ToString() ?? string.Empty,
                                cprocessname = sdr["cprocessname"]?.ToString() ?? string.Empty,
                                cprocessdescription = sdr["cprocessdescription"]?.ToString() ?? string.Empty
                            };
                            tsk.Add(p);
                        }

                        if (await sdr.NextResultAsync())
                        {
                            if (await sdr.ReadAsync())
                            {
                                totalCount = sdr.IsDBNull(sdr.GetOrdinal("TotalCount")) ? 0 : Convert.ToInt32(sdr["TotalCount"]);
                            }
                        }
                    }
                }
            }


            foreach (var item in tsk)
            {
                item.TaskChildItems = await GetTaskDetailsInternalAsync(connectionString, username, cTenantID, item.itaskno);
            }

            return new GettaskreassignCountDTO
            {
                totalCount = totalCount,
                data = tsk
            };
        }

        private async Task<List<GetTaskDetails>> GetTaskDetailsInternalAsync(string connString, string user, int tenant, int taskNo)
        {
            List<GetTaskDetails> details = new List<GetTaskDetails>();
            using (SqlConnection con = new SqlConnection(connString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_get_worflow_Reassign_details", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userid", user);
                    cmd.Parameters.AddWithValue("@tenentid", tenant);
                    cmd.Parameters.AddWithValue("@itaskno", taskNo);

                    await con.OpenAsync();
                    using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await sdr.ReadAsync())
                        {
                            details.Add(new GetTaskDetails
                            {
                                ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                iheader_id = sdr.IsDBNull(sdr.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(sdr["iheader_id"]),
                                itaskno = sdr.IsDBNull(sdr.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                iseqno = sdr.IsDBNull(sdr.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr["iseqno"]),
                                cmappingcode = sdr["cmapping_code"]?.ToString() ?? string.Empty,
                                ccurrentstatus = sdr["ccurrent_status"]?.ToString() ?? string.Empty,
                                lcurrentstatusdate = sdr.IsDBNull(sdr.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcurrent_status_date")),
                                cremarks = sdr["cremarks"]?.ToString() ?? string.Empty,
                                cactivityname = sdr["cactivityname"]?.ToString() ?? string.Empty,
                                cactivity_description = sdr["cactivity_description"]?.ToString() ?? string.Empty
                            });
                        }
                    }
                }
            }
            return details;
        }


        public async Task<List<GettaskInitiatordatabyidDTO>> GettaskInitiatordatabyid(int cTenantID, int ID)
        {
            var result = new List<GettaskInitiatordatabyidDTO>();
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
                    d.crejection_privilege AS crejection_privilege,
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

                                var mapping = new GettaskInitiatordatabyidDTO
                                {
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>()
                                };

                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }

                                await LoadProcessConditionsForInitiator(conn, mapping, processdetailid);

                                await LoadMetaForInitiator(conn, mapping, itaskno, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved task list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }

        private async Task LoadProcessConditionsForInitiator(SqlConnection conn, GettaskInitiatordatabyidDTO mapping, int seqno)
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

        private async Task LoadMetaForInitiator(SqlConnection conn, GettaskInitiatordatabyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
          c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
           from [tbl_transaction_process_meta_layout] a 
           inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
         inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
         where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
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

        public async Task<List<GettaskReassigndatabyidDTO>> GettaskReassigndatabyid(int cTenantID, int ID)
        {
            var result = new List<GettaskReassigndatabyidDTO>();
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
                    d.crejection_privilege AS crejection_privilege,
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
                    a.itaskno,b.cremarks as Remarks,b.creassign_to as ReassignedTo ,b.lreassign_date as ReassignedDate,
					    ru.cfirst_name + ' ' + ru.clast_name AS ReassignedUsername
                FROM tbl_taskflow_master a
                INNER JOIN tbl_taskflow_detail b ON a.id = b.iheader_id
                INNER JOIN tbl_process_engine_master c ON a.cprocess_id = c.ID
                INNER JOIN tbl_process_engine_details d ON c.ID = d.cheader_id AND d.ciseqno = b.iseqno
                INNER JOIN Users e ON e.cuserid = CONVERT(int, a.ccreated_by) 
                                   AND e.ctenant_id = a.ctenant_id 
                LEFT JOIN Users ru ON ru.cuserid = CONVERT(int, b.creassign_to) 
                   AND ru.ctenant_id = a.ctenant_id
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

                                var mapping = new GettaskReassigndatabyidDTO
                                {
                                    itaskno = itaskno,
                                    processId = Convert.ToInt32(reader["processId"]),
                                    processName = reader["processName"]?.ToString() ?? "",
                                    processDesc = reader["processDesc"]?.ToString() ?? "",
                                    activityName = reader["activityName"]?.ToString() ?? "",
                                    priorityLabel = reader["priorityLabel"]?.ToString() ?? "",
                                    activityDesc = reader["activityDesc"]?.ToString() ?? "",
                                    taskStatus = reader["taskStatus"]?.ToString() ?? "",
                                    participantType = reader["participantType"]?.ToString() ?? "",
                                    actionPrivilege = reader["actionPrivilege"]?.ToString() ?? "",
                                    crejection_privilege = reader["crejection_privilege"]?.ToString() ?? "",
                                    assigneeType = reader["assigneeType"]?.ToString() ?? "",
                                    assigneeValue = reader["assigneeValue"]?.ToString() ?? "",
                                    slaDays = reader["slaDays"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaDays"]),
                                    slaHours = reader["slaHours"] == DBNull.Value ? 0 : Convert.ToInt32(reader["slaHours"]),
                                    executionType = reader["executionType"]?.ToString() ?? "",
                                    taskInitiatedDate = reader.SafeGetDateTime("taskInitiatedDate"),
                                    taskAssignedDate = reader.SafeGetDateTime("taskAssignedDate"),
                                    taskinitiatedbyname = reader["assigneeName"]?.ToString() ?? "",
                                    showTimeline = reader.SafeGetBoolean("showTimeline"),
                                    Remarks = reader["Remarks"]?.ToString() ?? "",
                                    ReassignedTo = reader["ReassignedTo"]?.ToString() ?? "",
                                    ReassignedDate = reader.SafeGetDateTime("ReassignedDate"),
                                    ReassignedUsername = reader["ReassignedUsername"]?.ToString() ?? "",
                                    timeline = new List<TimelineDTO>(),
                                    board = new List<GetprocessEngineConditionDTO>(),
                                    meta = new List<processEnginetaskMeta>()
                                };

                                if (mapping.showTimeline == true)
                                {
                                    mapping.timeline = await GetTimelineAsync(conn, ID);
                                }

                                await LoadProcessConditionsForReassign(conn, mapping, processdetailid);

                                await LoadMetaForReassign(conn, mapping, itaskno, cTenantID);

                                result.Add(mapping);
                            }
                        }
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving approved task list for TenantID {cTenantID} and ID {ID}: {ex.Message}", ex);
            }
        }

        private async Task LoadProcessConditionsForReassign(SqlConnection conn, GettaskReassigndatabyidDTO mapping, int seqno)
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

        private async Task LoadMetaForReassign(SqlConnection conn, GettaskReassigndatabyidDTO mapping, int itaskno, int tenantID)
        {
            string sql = @"SELECT a.cprocess_id,a.cdata,c.cinput_type,c.label,c.cplaceholder,
          c.cis_required,c.cis_readonly,c.cis_disabled,c.cfield_value,c.cdata_source
           from [tbl_transaction_process_meta_layout] a 
           inner join  tbl_process_engine_master b on a.cprocess_id=b.ID
         inner join tbl_process_meta_detail c on c.cheader_id=b.cmeta_id and c.Id=a.cmeta_id
         where a.citaskno=@TaskNo and a.ctenant_id=@TenantID";

            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@TaskNo", itaskno);
            cmd.Parameters.AddWithValue("@TenantID", tenantID);

            using var dr = await cmd.ExecuteReaderAsync();

            while (await dr.ReadAsync())
            {
                mapping.meta.Add(new processEnginetaskMeta
                {
                    cdata = dr["cdata"]?.ToString() ?? "",
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


        public async Task<string> Gettasktimeline(
        int cTenantID,
        string username,
        string? searchText = null,
        int pageNo = 1,
        int pageSize = 50)
        {
            List<GetTaskList> tsk = new List<GetTaskList>();
            int totalCount = 0;

            try
            {
                string query = "sp_get_worflow_timeline";

                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@searchtext", (object?)searchText ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PageNo", pageNo);
                    cmd.Parameters.AddWithValue("@PageSize", pageSize);

                    await con.OpenAsync();

                    using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                    {
                        while (await sdr.ReadAsync())
                        {
                            GetTaskList p = new GetTaskList
                            {
                                ID = sdr.IsDBNull("ID") ? 0 : Convert.ToInt32(sdr["ID"]),
                                itaskno = sdr.IsDBNull("itaskno") ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                ctasktype = sdr["ctask_type"]?.ToString() ?? "",
                                ctaskname = sdr["ctask_name"]?.ToString() ?? "",
                                ctaskdescription = sdr["ctask_description"]?.ToString() ?? "",
                                cstatus = sdr["cstatus"]?.ToString() ?? "",
                                lcompleteddate = sdr.IsDBNull("lcompleted_date") ? null : sdr.GetDateTime(sdr.GetOrdinal("lcompleted_date")),
                                ccreatedby = sdr["ccreated_by"]?.ToString() ?? "",
                                ccreatedbyname = sdr["ccreated_byname"]?.ToString() ?? "",
                                lcreateddate = sdr.IsDBNull("lcreated_date") ? null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                cmodifiedby = sdr["cmodified_by"]?.ToString() ?? "",
                                cmodifiedbyname = sdr["cmodified_byname"]?.ToString() ?? "",
                                lmodifieddate = sdr.IsDBNull("lmodified_date") ? null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date")),
                                Employeecode = sdr["Employeecode"]?.ToString() ?? "",
                                Employeename = sdr["Employeename"]?.ToString() ?? "",
                                EmpDepartment = sdr["EmpDepartment"]?.ToString() ?? "",
                                cprocess_id = sdr.IsDBNull("cprocess_id") ? 0 : Convert.ToInt32(sdr["cprocess_id"]),
                                cprocesscode = sdr["cprocesscode"]?.ToString() ?? "",
                                cprocessname = sdr["cprocessname"]?.ToString() ?? "",
                                cprocessdescription = sdr["cprocessdescription"]?.ToString() ?? ""
                            };

                            tsk.Add(p);
                        }
                        if (await sdr.NextResultAsync() && await sdr.ReadAsync())
                        {
                            totalCount = sdr.IsDBNull(0) ? 0 : Convert.ToInt32(sdr[0]);
                        }
                    }
                }

                var response = new
                {
                    data = tsk,
                    totalCount = totalCount,
                    pageNo = pageNo,
                    pageSize = pageSize
                };

                return JsonConvert.SerializeObject(response);
            }
            catch (Exception ex)
            {              
                return JsonConvert.SerializeObject(new
                {
                    data = new List<GetTaskList>(),
                    totalCount = 0,
                    error = ex.Message
                });
            }
        }

        public async Task<List<GetTaskDetails>> GettasktimelinedetailAsync(int itaskno, string username, int cTenantID)
        {
            var timelineList = new List<GetTaskDetails>();
            string connectionString = this._config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand("sp_get_worflow_timeline_Details", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@itaskno", itaskno);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            timelineList.Add(new GetTaskDetails
                            {
                                ID = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : Convert.ToInt32(reader["ID"]),
                                iheader_id = reader.IsDBNull(reader.GetOrdinal("iheader_id")) ? 0 : Convert.ToInt32(reader["iheader_id"]),
                                itaskno = reader.IsDBNull(reader.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(reader["itaskno"]),
                                iseqno = reader.IsDBNull(reader.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(reader["iseqno"]),
                                ctasktype = reader.IsDBNull(reader.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(reader["ctask_type"]),
                                cmappingcode = reader.IsDBNull(reader.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(reader["cmapping_code"]),
                                ccurrentstatus = reader.IsDBNull(reader.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(reader["ccurrent_status"]),
                                lcurrentstatusdate = reader.IsDBNull(reader.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lcurrent_status_date")),
                                cremarks = reader.IsDBNull(reader.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(reader["cremarks"]),
                                inextseqno = reader.IsDBNull(reader.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(reader["inext_seqno"]),
                                cnextseqtype = reader.IsDBNull(reader.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(reader["cnext_seqtype"]),
                                cprevtype = reader.IsDBNull(reader.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(reader["cprevtype"]),
                                csla_day = reader.IsDBNull(reader.GetOrdinal("csla_day")) ? 0 : Convert.ToInt32(reader["csla_day"]),
                                csla_Hour = reader.IsDBNull(reader.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(reader["csla_Hour"]),
                                cprocess_type = reader.IsDBNull(reader.GetOrdinal("cprocess_type")) ? string.Empty : Convert.ToString(reader["cprocess_type"]),
                                nboard_enabled = reader.IsDBNull(reader.GetOrdinal("nboard_enabled")) ? false : Convert.ToBoolean(reader["nboard_enabled"]),
                                caction_privilege = reader.IsDBNull(reader.GetOrdinal("caction_privilege")) ? string.Empty : Convert.ToString(reader["caction_privilege"]),
                                crejection_privilege = reader.IsDBNull(reader.GetOrdinal("crejection_privilege")) ? string.Empty : Convert.ToString(reader["crejection_privilege"]),
                                cisforwarded = reader.IsDBNull(reader.GetOrdinal("cis_forwarded")) ? string.Empty : Convert.ToString(reader["cis_forwarded"]),
                                lfwd_date = reader.IsDBNull(reader.GetOrdinal("lfwd_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lfwd_date")),
                                cfwd_to = reader.IsDBNull(reader.GetOrdinal("cfwd_to")) ? string.Empty : Convert.ToString(reader["cfwd_to"]),
                                cis_reassigned = reader.IsDBNull(reader.GetOrdinal("cis_reassigned")) ? string.Empty : Convert.ToString(reader["cis_reassigned"]),
                                creassign_name = reader.IsDBNull(reader.GetOrdinal("creassign_name")) ? string.Empty : Convert.ToString(reader["creassign_name"]),
                                lreassign_date = reader.IsDBNull(reader.GetOrdinal("lreassign_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lreassign_date")),
                                creassign_to = reader.IsDBNull(reader.GetOrdinal("creassign_to")) ? string.Empty : Convert.ToString(reader["creassign_to"]),
                                cactivityname = reader.IsDBNull(reader.GetOrdinal("cactivityname")) ? string.Empty : Convert.ToString(reader["cactivityname"]),
                                cactivity_description = reader.IsDBNull(reader.GetOrdinal("cactivity_description")) ? string.Empty : Convert.ToString(reader["cactivity_description"])
                            });
                        }
                    }
                }
            }

            return timelineList;
        }


        public async Task<string> Getworkflowdashboard(int cTenantID, string username, string searchtext)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_workflow_dashboard", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@userid", username);
                    cmd.Parameters.AddWithValue("@searchtext", searchtext);
                    var ds = new DataSet();
                    var adapter = new SqlDataAdapter(cmd);
                    await Task.Run(() => adapter.Fill(ds)); 

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


        public async Task<string> GetProcessmetadetailsbyid(int itaskno, int cTenantID, int processid)
        {
            var metaDetails = new List<object>();
            string connString = _config.GetConnectionString("Database");

            try
            {
                using (SqlConnection con = new SqlConnection(connString))
                {
                    await con.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_get_Process_meta_details_common", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                        cmd.Parameters.AddWithValue("@ctaskno", itaskno);
                        cmd.Parameters.AddWithValue("@processid", processid);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                metaDetails.Add(new
                                {
                                    id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                    citaskno = reader["citaskno"] != DBNull.Value ? Convert.ToInt32(reader["citaskno"]) : 0,
                                    cmeta_id = reader["cmeta_id"] != DBNull.Value ? Convert.ToInt32(reader["cmeta_id"]) : 0,
                                    cinput_type = reader["cinput_type"]?.ToString() ?? "",
                                    label = reader["label"]?.ToString() ?? "",
                                    cdata = reader["cdata"]?.ToString() ?? "",
                                    cdetail_id = reader["cdetail_id"] != DBNull.Value ? Convert.ToInt32(reader["cdetail_id"]) : 0
                                });
                            }
                        }
                    }
                }
                return JsonConvert.SerializeObject(metaDetails, Formatting.Indented);
            }
            catch (Exception ex)
            {
                throw new Exception($"Database error: {ex.Message}");
            }
        }


    }
}


