
using System.Data;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Reflection.PortableExecutable;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
            int detailId = 0;
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
                        lcreated_date, ccreated_by, cmodified_by, lmodified_date,cprocess_id) VALUES (
                        @itaskno, @TenantID, @ctask_type, @ctask_name, @ctask_description, @cstatus,
                        @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cprocess_id );SELECT SCOPE_IDENTITY();";
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
                            cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocess_id ?? DBNull.Value);
                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }
                     
                        string selectQuery = @"                     
                         SELECT ctenent_id, cprocesscode, ciseqno, cseq_order, cactivitycode, 
                         cactivity_description, ctask_type, cprev_step, cactivityname, cnext_seqno,nboard_enabled,cassignee,
                        cprocess_type,csla_day,csla_Hour,caction_privilege,crejection_privilege
                        FROM tbl_process_engine_details 
                        WHERE cheader_id = @cprocesscode AND ctenent_id = @ctenent_id";

                        var detailRows = new List<Dictionary<string, object>>();

                        using (var cmdSelect = new SqlCommand(selectQuery, conn, transaction))
                        {
                            cmdSelect.Parameters.AddWithValue("@cprocesscode", model.cprocess_id);
                            cmdSelect.Parameters.AddWithValue("@ctenent_id", cTenantID);

                            using (var reader = await cmdSelect.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    var row = new Dictionary<string, object>
                                    {
                                        ["ciseqno"] = reader["ciseqno"],
                                        ["ctenentid"] = reader["ctenent_id"],
                                        ["ctasktype"] = reader["ctask_type"],
                                        ["cprocesscode"] = reader["cprocesscode"],
                                        ["cnextseqno"] = reader["cnext_seqno"],
                                        ["cprevstep"] = reader["cprev_step"],
                                        ["cassignee"] = reader["cassignee"],
                                        ["nboard_enabled"]= reader["nboard_enabled"],
                                        ["cprocess_type"] = reader["cprocess_type"],
                                        ["csla_day"] = reader["csla_day"],
                                        ["csla_Hour"] = reader["csla_Hour"],
                                        ["caction_privilege"] = reader["caction_privilege"],
                                        ["crejection_privilege"] = reader["crejection_privilege"],
                                        
                                        
                                    };
                                    detailRows.Add(row);
                                }
                            }
                        }                    
                  

                        string queryDetail = @"INSERT INTO tbl_taskflow_detail (
                        itaskno, iseqno, iheader_id, ctenent_id, ctask_type, cmapping_code, 
                        ccurrent_status, lcurrent_status_date, cremarks, inext_seqno, 
                        cnext_seqtype, cprevtype,nboard_enabled cprocess_type,csla_day,csla_Hour,caction_privilege,crejection_privilege,nboard_enabled) VALUES (
                        @itaskno, @iseqno, @iheader_id, @ctenent_id, @ctask_type, @cmapping_code, 
                        @ccurrent_status, @lcurrent_status_date, @cremarks, @inext_seqno, 
                        @cnext_seqtype, @cprevtype,@nboard_enabled,@cprocess_type,@csla_day,@csla_Hour,@caction_privilege,@crejection_privilege,@nboard_enabled);SELECT SCOPE_IDENTITY();";

                        string queryStatus = @"INSERT INTO tbl_transaction_taskflow_detail_and_status (
                        itaskno, ctenent_id, cheader_id, cdetail_id, cstatus, cstatus_with, lstatus_date) VALUES 
                        (@itaskno, @ctenent_id, @cheader_id, @cdetail_id, @cstatus, @cstatus_with, @lstatus_date);";

                        foreach (var row in detailRows)
                        {
                            using (var cmdInsert = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdInsert.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdInsert.Parameters.AddWithValue("@iseqno", row["ciseqno"]);
                                cmdInsert.Parameters.AddWithValue("@iheader_id", masterId);
                                cmdInsert.Parameters.AddWithValue("@ctenent_id", cTenantID);
                                cmdInsert.Parameters.AddWithValue("@ctask_type", row["ctasktype"]);
                                cmdInsert.Parameters.AddWithValue("@cmapping_code", row["cassignee"]);
                                cmdInsert.Parameters.AddWithValue("@ccurrent_status", "P");
                                cmdInsert.Parameters.AddWithValue("@lcurrent_status_date", DateTime.Now);
                                cmdInsert.Parameters.AddWithValue("@cremarks", DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@inext_seqno", row["cnextseqno"]);
                                cmdInsert.Parameters.AddWithValue("@cnext_seqtype", DBNull.Value);
                                cmdInsert.Parameters.AddWithValue("@cprevtype", row["cprevstep"]);
                                cmdInsert.Parameters.AddWithValue("@cprocess_type", row["cprocess_type"]);
                                cmdInsert.Parameters.AddWithValue("@csla_day", row["csla_day"]);
                                cmdInsert.Parameters.AddWithValue("@csla_Hour", row["csla_Hour"]);
                                cmdInsert.Parameters.AddWithValue("@caction_privilege", row["caction_privilege"]);
                                cmdInsert.Parameters.AddWithValue("@crejection_privilege", row["crejection_privilege"]);
                                cmdInsert.Parameters.AddWithValue("@nboard_enabled", row["nboard_enabled"]);

                                var newId = await cmdInsert.ExecuteScalarAsync();
                                detailId = newId != null ? Convert.ToInt32(newId) : 0;
                            }

                            using (var cmdStatus = new SqlCommand(queryStatus, conn, transaction))
                            {
                                cmdStatus.Parameters.AddWithValue("@itaskno", newTaskNo);
                                cmdStatus.Parameters.AddWithValue("@ctenent_id", cTenantID);
                                cmdStatus.Parameters.AddWithValue("@cheader_id", 1);
                                cmdStatus.Parameters.AddWithValue("@cdetail_id", detailId);
                                cmdStatus.Parameters.AddWithValue("@cstatus", "P");
                                cmdStatus.Parameters.AddWithValue("@cstatus_with", username); // or a value if applicable
                                cmdStatus.Parameters.AddWithValue("@lstatus_date", DateTime.Now);
                                await cmdStatus.ExecuteNonQueryAsync();
                            }
                        }
                        string meta = @"INSERT INTO tbl_transaction_process_meta_layout (
                        [cmeta_id],[cprocess_id],[cprocess_code],[ctenent_id],[cdata]) VALUES (
                        @cmeta_id, @cprocess_id, @cprocess_code, @TenantID, @cdata);";

                        foreach (var metaData in model.metaData)
                        {
                            using (SqlCommand cmd = new SqlCommand(meta, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                                cmd.Parameters.AddWithValue("@cmeta_id", (object?)metaData.cmeta_id ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocess_id ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@cprocess_code", (object?)model.ctask_name ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@cdata", (object?)metaData.cdata ?? DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
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
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
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

        public async Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_process_engine_privilege", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.Parameters.AddWithValue("@cprivilege", cprivilege);
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

        public async Task<string> Getdropdown(int cTenantID, string @column)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_dropdown", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenent", cTenantID);
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
                    cmd.Parameters.AddWithValue("@tenentid", cTenantID);
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
                    [ctenent_id],[cprocess_privilege],[cseq_id],[ciseqno],[cprocess_id],[cprocesscode],
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
                        privilege_id, entity_type, entity_id, ctenent_id, cis_active,ccreated_by,lcreated_date, 
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
                                cmdInsert.Parameters.AddWithValue("@cis_active",true);                  
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
            List<TaskList> tsk = new List<TaskList>();

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
                            List<TaskDetails> tskdtl = new List<TaskDetails>();
                            TaskList p = new TaskList
                            {                        
                                ID = sdr.IsDBNull(sdr.GetOrdinal("ID"))? 0: Convert.ToInt32(sdr["ID"]),
                                itaskno = sdr.IsDBNull(sdr.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                ctasktype = sdr.IsDBNull(sdr.GetOrdinal("ctask_type"))? string.Empty: Convert.ToString(sdr["ctask_type"]),
                                ctaskname = sdr.IsDBNull(sdr.GetOrdinal("ctask_name")) ? string.Empty : Convert.ToString(sdr["ctask_name"]),
                                ctaskdescription = sdr.IsDBNull(sdr.GetOrdinal("ctask_description")) ? string.Empty : Convert.ToString(sdr["ctask_description"]),
                                cstatus = sdr.IsDBNull(sdr.GetOrdinal("cstatus")) ? string.Empty : Convert.ToString(sdr["cstatus"]),
                                lcompleteddate = sdr.IsDBNull(sdr.GetOrdinal("lcompleted_date"))? (DateTime?)null: sdr.GetDateTime(sdr.GetOrdinal("lcompleted_date")),
                                ccreatedby = sdr.IsDBNull(sdr.GetOrdinal("ccreated_by")) ? string.Empty : Convert.ToString(sdr["ccreated_by"]),
                                lcreateddate = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                cmodifiedby = sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                lmodifieddate = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date")),
                                Employeecode = sdr.IsDBNull(sdr.GetOrdinal("Employeecode")) ? string.Empty : Convert.ToString(sdr["Employeecode"]),
                                EmpDepartment = sdr.IsDBNull(sdr.GetOrdinal("EmpDepartment")) ? string.Empty : Convert.ToString(sdr["EmpDepartment"])
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
                                            TaskDetails pd = new TaskDetails
                                            {
                                               
                                                ID = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                                iheader_id = sdr1.IsDBNull(sdr1.GetOrdinal("iheader_id"))? 0: Convert.ToInt32(sdr1["iheader_id"]),                                   
                                                itaskno = sdr.IsDBNull(sdr.GetOrdinal("itaskno")) ? 0 : Convert.ToInt32(sdr["itaskno"]),
                                                iseqno = sdr1.IsDBNull(sdr1.GetOrdinal("iseqno")) ? 0 : Convert.ToInt32(sdr1["iseqno"]),
                                                ctasktype = sdr.IsDBNull(sdr.GetOrdinal("ctask_type")) ? string.Empty : Convert.ToString(sdr["ctask_type"]),
                                                cmappingcode = sdr1.IsDBNull(sdr1.GetOrdinal("cmapping_code")) ? string.Empty : Convert.ToString(sdr1["cmapping_code"]),
                                                ccurrentstatus = sdr1.IsDBNull(sdr1.GetOrdinal("ccurrent_status")) ? string.Empty : Convert.ToString(sdr1["ccurrent_status"]),
                                                lcurrentstatusdate = sdr1.IsDBNull(sdr1.GetOrdinal("lcurrent_status_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lcurrent_status_date")),
                                                cremarks = sdr1.IsDBNull(sdr1.GetOrdinal("cremarks")) ? string.Empty : Convert.ToString(sdr1.GetOrdinal("cremarks")),
                                                inextseqno = sdr1.IsDBNull(sdr1.GetOrdinal("inext_seqno")) ? 0 : Convert.ToInt32(sdr1["inext_seqno"]),
                                                cnextseqtype = sdr1.IsDBNull(sdr1.GetOrdinal("cnext_seqtype")) ? string.Empty : Convert.ToString(sdr1["cnext_seqtype"]),
                                                cprevtype = sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                SLA = sdr1.IsDBNull(sdr1.GetOrdinal("csla")) ? string.Empty : Convert.ToString(sdr1["csla"]),                                             
                                                cisforwarded = sdr1.IsDBNull(sdr1.GetOrdinal("cis_forwarded")) ? string.Empty : Convert.ToString(sdr1["cis_forwarded"]),
                                                lfwddate = sdr1.IsDBNull(sdr1.GetOrdinal("lfwddate")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lfwddate")),
                                                cfwdto = sdr1.IsDBNull(sdr1.GetOrdinal("cfwd_to")) ? string.Empty : Convert.ToString(sdr1["cfwd_to"]),
                                                cisreassigned = sdr1.IsDBNull(sdr1.GetOrdinal("cis_reassigned")) ? string.Empty : Convert.ToString(sdr1["cis_reassigned"]),
                                                lreassigndt = sdr1.IsDBNull(sdr1.GetOrdinal("lreassigndt")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lreassigndt")),
                                                creassignto = sdr1.IsDBNull(sdr1.GetOrdinal("creassign_to")) ? string.Empty : Convert.ToString(sdr1["creassign_to"]),                                                                                          
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
                                lcreateddate = sdr.IsDBNull(sdr.GetOrdinal("lcreated_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lcreated_date")),
                                cmodifiedby = sdr.IsDBNull(sdr.GetOrdinal("cmodified_by")) ? string.Empty : Convert.ToString(sdr["cmodified_by"]),
                                lmodifieddate = sdr.IsDBNull(sdr.GetOrdinal("lmodified_date")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("lmodified_date")),
                                Employeecode = sdr.IsDBNull(sdr.GetOrdinal("Employeecode")) ? string.Empty : Convert.ToString(sdr["Employeecode"]),
                                EmpDepartment = sdr.IsDBNull(sdr.GetOrdinal("EmpDepartment")) ? string.Empty : Convert.ToString(sdr["EmpDepartment"]),
                                cprocess_id= sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
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
                                                csla_day = sdr1.IsDBNull(sdr1.GetOrdinal("csla_day")) ? 0: Convert.ToInt32(sdr1["csla_day"]),
                                                csla_Hour = sdr1.IsDBNull(sdr1.GetOrdinal("csla_Hour")) ? 0 : Convert.ToInt32(sdr1["csla_Hour"]),
                                                cprocess_type=sdr1.IsDBNull(sdr1.GetOrdinal("cprevtype")) ? string.Empty : Convert.ToString(sdr1["cprevtype"]),
                                                nboard_enabled = sdr1.IsDBNull(sdr1.GetOrdinal("nboard_enabled"))? false : Convert.ToBoolean(sdr1["nboard_enabled"]),
                                                caction_privilege = sdr1.IsDBNull(sdr1.GetOrdinal("caction_privilege")) ? string.Empty : Convert.ToString(sdr1["caction_privilege"]),
                                                crejection_privilege= sdr1.IsDBNull(sdr1.GetOrdinal("crejection_privilege")) ? string.Empty : Convert.ToString(sdr1["crejection_privilege"]),
                                                cisforwarded = sdr1.IsDBNull(sdr1.GetOrdinal("cis_forwarded")) ? string.Empty : Convert.ToString(sdr1["cis_forwarded"]),
                                                lfwd_date = sdr1.IsDBNull(sdr1.GetOrdinal("lfwd_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lfwd_date")),
                                                cfwd_to = sdr1.IsDBNull(sdr1.GetOrdinal("cfwd_to")) ? string.Empty : Convert.ToString(sdr1["cfwd_to"]),
                                                cis_reassigned = sdr1.IsDBNull(sdr1.GetOrdinal("cis_reassigned")) ? string.Empty : Convert.ToString(sdr1["cis_reassigned"]),
                                                lreassign_date = sdr1.IsDBNull(sdr1.GetOrdinal("lreassign_date")) ? (DateTime?)null : sdr1.GetDateTime(sdr1.GetOrdinal("lreassign_date")),
                                                creassign_to = sdr1.IsDBNull(sdr1.GetOrdinal("creassign_to")) ? string.Empty : Convert.ToString(sdr1["creassign_to"]),
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



    }
}




   
