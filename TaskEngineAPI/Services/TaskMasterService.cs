
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
                     SELECT ctenent_id, cprocesscode, ciseqno, cseq_order, cactivitycode, 
                    cactivity_description, ctask_type, cprev_step, cactivityname, cnext_seqno 
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
                                        ["cprevstep"] = reader["cprev_step"]
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

                        string meta = @"
                    INSERT INTO tbl_transaction_process_meta_layout (
                        [cmeta_id],[cprocess_id],[cprocess_code],[ctenent_id],[cdata]
                    ) VALUES (
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

        public async Task<string> Gettaskinbox(int cTenantID, string username)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_worflow_inbox", con))
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


        public async Task<string> Gettaskhold(int cTenantID, string username)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_worflow_hold", con))
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

         public async Task<string> Gettaskinitiator(int cTenantID, string username)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_workflow_initiator", con))
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




    }
}




   
