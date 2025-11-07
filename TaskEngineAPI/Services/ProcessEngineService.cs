using System.Data;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Options;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;

namespace TaskEngineAPI.Services
{

    public class ProcessEngineService : IProcessEngineService
    {




        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;
        public ProcessEngineService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository, IOptions<UploadSettings> uploadSettings)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
            _uploadSettings = uploadSettings.Value;
        }

        public async Task<List<ProcessEngineTypeDTO>> GetAllProcessenginetypeAsync(int cTenantID)

        {
            var result = new List<ProcessEngineTypeDTO>();
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
            SELECT cprocess_privilege,ID FROM [dbo].[tbl_process_privilege_type] WHERE nis_active = 1 AND ctenent_id = @TenantID";


                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new ProcessEngineTypeDTO
                            {

                                privilege = reader.GetString(reader.GetOrdinal("cprocess_privilege")),
                                ID = reader.GetInt32(reader.GetOrdinal("ID"))
                            });
                        }
                    }
                }
                return result;
            }
        }
        //public async Task<int> InsertProcessEngineAsync(ProcessEngineDTO model, int cTenantID, string username)
        //{
        //    var connStr = _config.GetConnectionString("Database");

        //    using (SqlConnection conn = new SqlConnection(connStr))
        //    {
        //        await conn.OpenAsync();
        //        using (var transaction = conn.BeginTransaction())
        //        {
        //            try
        //            {
        //                string queryMaster = @"
        //        INSERT INTO tbl_process_engine_master (
        //            ctenent_id, ciseqno, cprocesscode, cprocessname, ctype, cstatus, 
        //            cuser_id, cuser_name, crole_code, crole_name, cposition_code, 
        //            cposition_title, cdepartment_code, cdepartment_name, 
        //            lcreated_date, ccreated_by, cmodified_by, lmodified_date,cmeta_id
        //        ) VALUES (
        //            @TenantID, @ciseqno, @cprocesscode, @cprocessname, @ctype, @cstatus, 
        //            @cuser_id, @cuser_name, @crole_code, @crole_name, @cposition_code, 
        //            @cposition_title, @cdepartment_code, @cdepartment_name, 
        //            @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cmeta_id
        //        );
        //        SELECT SCOPE_IDENTITY();";

        //                int masterId;
        //                using (SqlCommand cmd = new SqlCommand(queryMaster, conn, transaction))
        //                {
        //                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);
        //                    cmd.Parameters.AddWithValue("@ciseqno", (object?)model.ciseqno ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cprocesscode", (object?)model.cprocesscode ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cprocessname", (object?)model.cprocessname ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@ctype", (object?)model.ctype ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cstatus", (object?)model.cstatus ?? DBNull.Value);   // ✅ fixed
        //                    cmd.Parameters.AddWithValue("@cuser_id", (object?)model.cuser_id ?? DBNull.Value); // ✅ fixed
        //                    cmd.Parameters.AddWithValue("@cuser_name", (object?)model.cuser_name ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@crole_code", (object?)model.crole_code ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@crole_name", (object?)model.crole_name ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cposition_code", (object?)model.cposition_code ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cposition_title", (object?)model.cposition_title ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cdepartment_code", (object?)model.cdepartment_code ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@cdepartment_name", (object?)model.cdepartment_name ?? DBNull.Value);
        //                    cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@ccreated_by", username);
        //                    cmd.Parameters.AddWithValue("@cmodified_by", username);
        //                    cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
        //                    cmd.Parameters.AddWithValue("@cmeta_id", (object?)model.cmeta_id ?? DBNull.Value);
        //                    var newId = await cmd.ExecuteScalarAsync();
        //                    masterId = newId != null ? Convert.ToInt32(newId) : 0;
        //                }


        //                string queryDetail = @"
        //        INSERT INTO tbl_process_engine_details (
        //            ctenent_id, cheader_id,ciseqno, cprocesscode, cseq_order, cactivitycode, cactivity_description, 
        //            ctask_type, cprev_step, cactivityname, cnext_seqno, 
        //            lcreated_date, ccreated_by, cmodified_by, lmodified_date,cassignee,cprocess_type
        //        ) VALUES (
        //            @TenantID, @cheader_id,@ciseqno, @cprocesscode, @cseq_order, @cactivitycode, @cactivitydescription, 
        //            @ctasktype, @cprevstep, @cactivityname, @cnextseqno, 
        //            @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cassignee,@cprocess_type
        //        );";

        //                foreach (var detail in model.ProcessEngineChildItems)
        //                {
        //                    using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
        //                    {
        //                        cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
        //                        cmdDetail.Parameters.AddWithValue("@cprocesscode", detail.cprocesscode);
        //                        cmdDetail.Parameters.AddWithValue("@ciseqno", masterId);
        //                        cmdDetail.Parameters.AddWithValue("@cheader_id", masterId);
        //                        cmdDetail.Parameters.AddWithValue("@cactivitycode", detail.cactivitycode ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@cactivitydescription", detail.cactivitydescription ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@ctasktype", detail.ctasktype ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@cprevstep", detail.cprevstep ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@cactivityname", detail.cactivityname ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@cnextseqno", detail.cnextseqno ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@cseq_order", detail.cseq_order ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
        //                        cmdDetail.Parameters.AddWithValue("@ccreated_by", username);
        //                        cmdDetail.Parameters.AddWithValue("@cmodified_by", username);
        //                        cmdDetail.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
        //                        cmdDetail.Parameters.AddWithValue("@cassignee", detail.cassignee ?? (object)DBNull.Value);
        //                        cmdDetail.Parameters.AddWithValue("@cprocess_type", detail.cprocess_type ?? (object)DBNull.Value);
        //                        await cmdDetail.ExecuteNonQueryAsync();
        //                    }




        //                    string metadata = @"
        //            INSERT INTO tbl_process_meta (process_code,ctenant_id,cinput_type,label,cplaceholder,cis_required,cis_autofill,cis_editable,
        //            cis_validate,cmin_len,cmax_len,cdata_source_type,cfetch_type,cis_req_search,cis_multi_select,cmin_date,cmax_date,cdate_type,cmin_time,
        //            cmax_time,ctime_type,cprocess_source,clocation,ccreated_by,lcreated_date,cmodified_by,lmodified_date,ccolumn_value)
        //            VALUES ( @cprocesscode,@TenantID,@cinput_type,@label,@cplaceholder,@cis_required,@cis_autofill,@cis_editable,@cis_validate,
        //            @cmin_len,@cmax_len,@cdata_source_type,@cfetch_type,@cis_req_search,@cis_multi_select,@cmin_date,@cmax_date,
        //            @cdate_type,@cmin_time,@cmax_time,@ctime_type,@cprocess_source,@clocation,
        //            @ccreated_by,@ccreated_date,@cmodified_by,@lmodified_date,@ccolumn_value);";

        //                    foreach (var meta in model.ProcessEngineMeta)
        //                    {
        //                        using (SqlCommand cmdDetail = new SqlCommand(metadata, conn, transaction))
        //                        {
        //                            cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
        //                            cmdDetail.Parameters.AddWithValue("@cprocesscode", detail.cprocesscode);
        //                            cmdDetail.Parameters.AddWithValue("@ciseqno", masterId);
        //                            cmdDetail.Parameters.AddWithValue("@cinput_type", meta.cinput_type ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@label", meta.label ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cplaceholder", meta.cplaceholder ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cis_required", meta.cis_required ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cis_autofill", meta.cis_autofill ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cis_editable", meta.cis_editable ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cis_validate", meta.cis_validate ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cmin_len", meta.cmin_len ?? (object)DBNull.Value);
        //                           cmdDetail.Parameters.AddWithValue("@cmax_len", meta.cmax_len ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cdata_source_type", meta.cdata_source_type ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cfetch_type", meta.cfetch_type ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cis_req_search", meta.cis_req_search ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cis_multi_select", meta.cis_multi_select ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cmin_date", meta.cmin_date ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cmax_date", meta.cmax_date ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cdate_type", meta.cdate_type ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cmin_time", meta.cmin_time ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cmax_time", meta.cmax_time ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@ctime_type", meta.ctime_type ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@cprocess_source", meta.cprocess_source ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@clocation", meta.clocation ?? (object)DBNull.Value);
        //                            cmdDetail.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
        //                            cmdDetail.Parameters.AddWithValue("@ccreated_by", username);
        //                            cmdDetail.Parameters.AddWithValue("@cmodified_by", username);
        //                            cmdDetail.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
        //                            cmdDetail.Parameters.AddWithValue("@ccolumn_value", meta.ccolumn_value ?? (object)DBNull.Value);
        //                            await cmdDetail.ExecuteNonQueryAsync();
        //                        }
        //                    }

        //                        string queryCondition = @"
        //            INSERT INTO tbl_process_engine_condition (
        //                ctenent_id, cprocesscode, ciseqno, cseq_order, icond_seqno, ctype, 
        //                clabel, cfield_value, ccondition, remarks1, remarks2, remarks3, 
        //                lcreated_date, ccreated_by, cmodified_by, lmodified_date
        //            ) VALUES (
        //                @TenantID, @cprocesscode, @ciseqno, @cseq_order, @icondseqno, @ctype, 
        //                @clabel, @cfieldvalue, @ccondition, @remarks1, @remarks2, @remarks3,
        //                @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date
        //            );";

        //                    foreach (var cond in detail.ProcessEngineConditionDetails)
        //                    {
        //                        using (SqlCommand cmdCond = new SqlCommand(queryCondition, conn, transaction))
        //                        {
        //                            cmdCond.Parameters.AddWithValue("@TenantID", cTenantID);
        //                            cmdCond.Parameters.AddWithValue("@cprocesscode", cond.cprocesscode);
        //                            cmdCond.Parameters.AddWithValue("@ciseqno", masterId);
        //                            cmdCond.Parameters.AddWithValue("@cseq_order", cond.cseq_order ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@icondseqno", cond.icondseqno ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@ctype", cond.ctype ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@clabel", cond.clabel ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@cfieldvalue", cond.cfieldvalue ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@ccondition", cond.ccondition ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@remarks1", cond.remarks1 ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@remarks2", cond.remarks2 ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@remarks3", cond.remarks3 ?? (object)DBNull.Value);
        //                            cmdCond.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
        //                            cmdCond.Parameters.AddWithValue("@ccreated_by", username);
        //                            cmdCond.Parameters.AddWithValue("@cmodified_by", username);
        //                            cmdCond.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

        //                            await cmdCond.ExecuteNonQueryAsync();
        //                        }
        //                    }
        //                }

        //                transaction.Commit();
        //                return masterId;
        //            }
        //            catch
        //            {

        //                transaction.Rollback();
        //                throw;
        //            }
        //        }
        //    }
        //}   
        //        public async Task<List<GetProcessEngineDTO>> GetAllProcessengineAsync(int cTenantID)
        //        {
        //            var result = new Dictionary<int, GetProcessEngineDTO>();
        //            var connStr = _config.GetConnectionString("Database");

        //            try
        //            {
        //                using var conn = new SqlConnection(connStr);
        //                await conn.OpenAsync();

        //                string query = @"
        //                 SELECT
        //    m.ID, m.ctenant_id, m.cprocesscode, m.cprocessname, m.cprivilege_type, 
        //	m.cvalue,m.cpriority_label,m.nshow_timeline,m.cnotification_type,m.cstatus,
        //    m.ccreated_by, m.lcreated_date, m.cmodified_by, m.lmodified_date,m.cmeta_id,
        //    d.cactivitycode, d.cactivity_description, d.cheader_id,d.ctask_type, d.cprev_step, d.cactivityname, d.cnext_seqno,
        //	d.nboard_enabled,d.cmapping_code,d.cmapping_type,d.cparticipant_type,d.csla_day,d.csla_Hour,d.caction_privilege,d.crejection_privilege,
        //    c.icond_seqno,c.ctype AS cond_type, c.clabel, c.cfield_value, c.ccondition,
        //    c.remarks1, c.remarks2, c.remarks3,c.cplaceholder,c.cis_required,c.cis_readonly,c.cis_disabled,  
        //   c.cfield_value,c.ccondition,c.ciseqno
        //FROM tbl_process_engine_master m
        //LEFT JOIN tbl_process_engine_details d
        //    ON m.cprocesscode = d.cprocesscode AND m.ID = d.cheader_id
        //LEFT JOIN tbl_process_engine_condition c
        //    ON d.ciseqno = c.ciseqno
        //WHERE m.ctenant_id = @TenantID
        //ORDER BY m.ID desc";

        //                using var cmd = new SqlCommand(query, conn);
        //                cmd.Parameters.AddWithValue("@TenantID", cTenantID);

        //                using var reader = await cmd.ExecuteReaderAsync();
        //                while (await reader.ReadAsync())
        //                {
        //                    int cseq_id = reader.GetInt32(reader.GetOrdinal("ID"));

        //                    if (!result.TryGetValue(cseq_id, out var engine))
        //                    {
        //                        engine = new GetProcessEngineDTO
        //                        {
        //                            ID = cseq_id,
        //                            cprocesscode = reader.SafeGetString("cprocesscode"),
        //                            cprocessname = reader.SafeGetString("cprocessname"),
        //                            cprivilege_type = reader.SafeGetInt("cprivilege_type"),
        //                            cstatus = reader.SafeGetString("cstatus"),
        //                            cvalue = reader.SafeGetString("cvalue"),
        //                            cpriority_label = reader.SafeGetString("cpriority_label"),
        //                            nshow_timeline = reader.GetBoolean("nshow_timeline"),
        //                            cnotification_type = reader.SafeGetInt("cnotification_type"),
        //                            cmeta_id = reader.SafeGetInt("cmeta_id"),
        //                            ccreated_by = reader.SafeGetString("ccreated_by"),
        //                            ccreated_date = reader.SafeGetDateTime("lcreated_date"),
        //                            cmodified_by = reader.SafeGetString("cmodified_by"),
        //                            lmodified_date = reader.SafeGetDateTime("lmodified_date"),
        //                            processEngineChildItems = new List<GetprocessEngineChildItems>()
        //                        };
        //                        result[cseq_id] = engine;
        //                    }
        //                    int childHeaderID = reader.SafeGetInt("cheader_id");
        //                    int childSeqNo = reader.SafeGetInt("ciseqno");
        //                    {
        //                        var child = engine.processEngineChildItems.FirstOrDefault(x => x.ciseqno == childSeqNo);
        //                        if (child == null)
        //                        {

        //                            {
        //                                child = new GetprocessEngineChildItems
        //                                {
        //                                    cheader_id = childHeaderID,                                
        //                                    cactivityCode = reader.IsDBNull(reader.GetOrdinal("cactivitycode"))? "0": reader.GetString(reader.GetOrdinal("cactivitycode")),
        //                                    cactivityDescription = reader.SafeGetString("cactivity_description"),
        //                                    ctaskType = reader.SafeGetString("ctask_type"),
        //                                    cprevStep = reader.SafeGetString("cprev_step"),
        //                                    cactivityName = reader.SafeGetString("cactivityname"),
        //                                    cnextSeqno = reader.SafeGetString("cnext_seqno"),
        //                                    cmappingCode = reader.SafeGetString("cmapping_code"),
        //                                    cmappingType = reader.SafeGetString("cmapping_type"),
        //                                    cslaDay = reader.IsDBNull(reader.GetOrdinal("csla_day")) ? 0 : reader.GetInt32(reader.GetOrdinal("csla_day")),
        //                                    cslaHour = reader.IsDBNull(reader.GetOrdinal("csla_Hour")) ? 0 : reader.GetInt32(reader.GetOrdinal("csla_Hour")),
        //                                    ciseqno = reader.IsDBNull(reader.GetOrdinal("ciseqno")) ? 0 : reader.GetInt32(reader.GetOrdinal("ciseqno")),                                 
        //                                    nboardEnabled = reader.GetBoolean("nboard_enabled"),
        //                                    cactionPrivilege = reader.SafeGetString("caction_privilege"),
        //                                    crejectionPrivilege = reader.SafeGetString("crejection_privilege"),
        //                                    cparticipantType = reader.SafeGetString("cparticipant_type"),
        //                                    processEngineConditionDetails = new List<processEngineConditionDetails>()

        //                                };
        //                                engine.processEngineChildItems.Add(child);
        //                            }

        //                            if (!reader.IsDBNull(reader.GetOrdinal("icond_seqno")))
        //                            {
        //                                child.processEngineConditionDetails.Add(new processEngineConditionDetails
        //                                {
        //                                    cprocessCode = reader.SafeGetString("cprocesscode"),
        //                                    ciseqno = reader.SafeGetInt("ciseqno"),
        //                                    icondseqno = reader.SafeGetInt("icond_seqno"),
        //                                    ctype = reader.SafeGetString("cond_type"),
        //                                    clabel = reader.SafeGetString("clabel"),
        //                                    cfieldValue = reader.SafeGetString("cfield_value"),
        //                                    ccondition = reader.SafeGetString("ccondition"),
        //                                    cplaceholder = reader.SafeGetString("cplaceholder"),
        //                                    cisRequired = reader.GetBoolean("cis_required"),
        //                                    cisReadonly = reader.GetBoolean("cis_readonly"),
        //                                    cis_disabled = reader.GetBoolean("cis_disabled")
        //                                });
        //                            }
        //                        }
        //                    }
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                // _logger.LogError(ex, "Error fetching process engine data for tenant {TenantID}", cTenantID);
        //                throw;
        //            }

        //            return result.Values.ToList();
        //        }


        public async Task<List<GetProcessEngineDTO>> GetProcessengineAsync(int cTenantID, int id)
        {
            var result = new Dictionary<int, GetProcessEngineDTO>();
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                string query = @"

SELECT
    m.ID, m.ctenant_id, m.cprocesscode, m.cprocessname, m.cprivilege_type, 
    m.cvalue, m.cpriority_label, m.nshow_timeline, m.cnotification_type, m.cstatus,
    u1.cfirst_name + ' ' + u1.clast_name AS created_by, 
    m.lcreated_date,     
    u2.cfirst_name + ' ' + u2.clast_name AS modified_by, 
    m.lmodified_date,
    m.cmeta_id,
    d.cactivitycode, d.cactivity_description, d.ctask_type, d.cprev_step, d.cactivityname, d.cnext_seqno,
    d.nboard_enabled, d.cmapping_code, d.cmapping_type, d.cparticipant_type, d.csla_day, d.csla_Hour, d.caction_privilege, d.crejection_privilege,
    n.notification_type As Notification_Description,
    c.icond_seqno, c.ctype AS cond_type, c.clabel, c.cfield_value, c.ccondition,
    c.remarks1, c.remarks2, c.remarks3, c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled,  
    s.cstatus_description
FROM tbl_process_engine_master m
LEFT JOIN AdminUsers u1 ON CAST(m.ccreated_by AS VARCHAR(50)) = u1.cuserid
LEFT JOIN AdminUsers u2 ON CAST(m.cmodified_by AS VARCHAR(50)) = u2.cuserid
LEFT JOIN tbl_process_engine_details d ON m.cprocesscode = d.cprocesscode AND m.ID = d.cheader_id
LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID 
LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID  
LEFT JOIN tbl_status_master s ON m.cstatus = s.cstatus_id 
LEFT JOIN tbl_process_engine_condition c ON d.ciseqno = c.ciseqno 
WHERE m.ctenant_id = 1500 and m.id=@id
ORDER BY m.ID, d.ciseqno, c.icond_seqno
";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int ID = reader.GetInt32(reader.GetOrdinal("ID"));

                    if (!result.TryGetValue(ID, out var engine))
                    {
                        engine = new GetProcessEngineDTO
                        {
                            ID = ID,
                            cprocesscode = reader.SafeGetString("cprocesscode"),
                            cprocessname = reader.SafeGetString("cprocessname"),
                            cprivilege_type = reader.SafeGetInt("cprivilege_type"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cvalue = reader.SafeGetString("cvalue"),
                            cpriority_label = reader.SafeGetString("cpriority_label"),
                            nshow_timeline = reader.GetBoolean("nshow_timeline"),
                            cnotification_type = reader.SafeGetInt("cnotification_type"),
                            cmeta_id = reader.SafeGetInt("cmeta_id"),
                            created_by = reader.SafeGetString("created_by"),        // Changed from "ccreated_by"
                            ccreated_date = reader.SafeGetDateTime("lcreated_date"),
                            modified_by = reader.SafeGetString("modified_by"),
                            //ccreated_by = reader.SafeGetString("ccreated_by"),
                            //ccreated_date = reader.SafeGetDateTime("lcreated_date"),
                            //cmodified_by = reader.SafeGetString("cmodified_by"),
                            lmodified_date = reader.SafeGetDateTime("lmodified_date"),
                            cstatus_description = reader.SafeGetString("cstatus_description"),
                            Notification_Description = reader.SafeGetString("Notification_Description"),
                            processEngineChildItems = new List<GetprocessEngineChildItems>()
                        };
                        result[ID] = engine;
                    }

                    string activityCode = reader.SafeGetString("cactivitycode");
                    if (!string.IsNullOrEmpty(activityCode))
                    {
                        var child = engine.processEngineChildItems.FirstOrDefault(x => x.cactivityCode == activityCode);
                        if (child == null)
                        {
                            child = new GetprocessEngineChildItems
                            {
                                cactivityCode = activityCode,
                                cactivityDescription = reader.SafeGetString("cactivity_description"),
                                ctaskType = reader.SafeGetString("ctask_type"),
                                cprevStep = reader.SafeGetString("cprev_step"),
                                cactivityName = reader.SafeGetString("cactivityname"),
                                cnextSeqno = reader.SafeGetString("cnext_seqno"),
                                cmappingCode = reader.SafeGetString("cmapping_code"),
                                cmappingType = reader.SafeGetString("cmapping_type"),
                                cslaDay = reader.SafeGetInt("csla_day"),
                                cslaHour = reader.SafeGetInt("csla_Hour"),
                                ciseqno = reader.SafeGetInt("ciseqno"),
                                nboardEnabled = reader.GetBoolean("nboard_enabled"),
                                cactionPrivilege = reader.SafeGetString("caction_privilege"),
                                crejectionPrivilege = reader.SafeGetString("crejection_privilege"),
                                cparticipantType = reader.SafeGetString("cparticipant_type"),
                                processEngineConditionDetails = new List<processEngineConditionDetails>()
                            };
                            engine.processEngineChildItems.Add(child);
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("icondseqno")))
                        {
                            child.processEngineConditionDetails.Add(new processEngineConditionDetails
                            {
                                cprocessCode = reader.SafeGetString("cprocesscode"),
                                ciseqno = reader.SafeGetInt("ciseqno"),
                                icondseqno = reader.SafeGetInt("icond_seqno"),
                                ctype = reader.SafeGetString("cond_type"),
                                clabel = reader.SafeGetString("clabel"),
                                cfieldValue = reader.SafeGetString("cfield_value"),
                                ccondition = reader.SafeGetString("ccondition"),
                                cplaceholder = reader.SafeGetString("cplaceholder"),
                                cisRequired = reader.GetBoolean("cis_required"),
                                cisReadonly = reader.GetBoolean("cis_readonly"),
                                cis_disabled = reader.GetBoolean("cis_disabled"),

                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error fetching process engine data for tenant {TenantID}", cTenantID);
                throw;
            }

            return result.Values.ToList();
        }

        public async Task<int> InsertProcessEngineAsync(ProcessEngineDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            DateTime now = DateTime.Now;

            // Format: 1500-2910185301  => tenantId-DDMMHHMMSS
            string autoprocessCode = $"{cTenantID}-{now:ddM MHHmmss}".Replace(" ", "");
            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string queryMaster = @"INSERT INTO tbl_process_engine_master (
    ctenant_id,cprocesscode, cprocessname, cprivilege_type, cstatus,cvalue,cpriority_label, nshow_timeline,
    cnotification_type,lcreated_date,ccreated_by, cmodified_by,lmodified_date, cmeta_id) VALUES (@TenantID, @cprocesscode, @cprocessname,@cprocess_type, @cstatus, 
     @cvalue,@cpriority_label,@nshow_timeline,@cnotification_type,
    @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cmeta_id);SELECT SCOPE_IDENTITY();";
                        int masterId;
                        using (SqlCommand cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                            cmd.Parameters.AddWithValue("@cprocessname", (object?)model.cprocessName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocess_type", (object?)model.cprivilegeType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", (object?)model.cstatus ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cvalue", (object?)model.cvalue ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cpriority_label", (object?)model.cpriorityLabel ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nshow_timeline", (object?)model.nshowTimeline ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cnotification_type", (object?)model.cnotificationType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ccreated_by", username);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@cmeta_id", (object?)model.cmetaId ?? DBNull.Value);
                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }
                        // Insert Process Engine Details
                        string queryDetail = @"INSERT INTO tbl_process_engine_details (
       ctenent_id, cheader_id, ciseqno, cprocesscode,cactivitycode, cactivity_description, 
         ctask_type, cprev_step, cactivityname, cnext_seqno, lcreated_date, ccreated_by, cmodified_by, lmodified_date, cmapping_code,
		 cparticipant_type,nboard_enabled,csla_day,csla_Hour,caction_privilege,crejection_privilege,cmapping_type) VALUES (
         @TenantID, @cheader_id, @ciseqno, @cprocesscode, @cactivitycode, @cactivitydescription, 
         @ctasktype, @cprevstep, @cactivityname, @cnextseqno, @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cassignee, @cparticipantType,
       @nboardenabled,@csladay,@cslaHour,@cactionprivilege,@crejectionprivilege,@cmapping_type);";

                        foreach (var detail in model.processEngineChildItems)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                                cmdDetail.Parameters.AddWithValue("@ciseqno", masterId);
                                cmdDetail.Parameters.AddWithValue("@cheader_id", masterId);
                                cmdDetail.Parameters.AddWithValue("@cactivitycode", detail.cactivityCode ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactivitydescription", detail.cactivityDescription ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ctasktype", detail.ctaskType ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cprevstep", detail.cprevStep ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactivityname", detail.cactivityName ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cnextseqno", detail.cnextSeqno ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                                cmdDetail.Parameters.AddWithValue("@ccreated_by", username);
                                cmdDetail.Parameters.AddWithValue("@cmodified_by", username);
                                cmdDetail.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                cmdDetail.Parameters.AddWithValue("@cassignee", detail.cmappingCode ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cmapping_type", detail.cmappingType ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cparticipantType", detail.cparticipantType ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@nboardenabled", detail.nboardEnabled ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@csladay", detail.cslaDay ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cslaHour", detail.cslaHour ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactionprivilege", detail.cactionPrivilege ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@crejectionprivilege", detail.crejectionPrivilege ?? (object)DBNull.Value);
                                await cmdDetail.ExecuteNonQueryAsync();
                            }
                            if (detail.processEngineConditionDetails != null)
                            {
                                string queryCondition = @"INSERT INTO tbl_process_engine_condition (
        ctenent_id, cprocesscode, ciseqno,icond_seqno, ctype, 
         clabel, cfield_value, ccondition, 
         lcreated_date, ccreated_by, cmodified_by, lmodified_date,cplaceholder,cis_required
      ,cis_readonly,cis_disabled) VALUES (     
         @TenantID, @cprocesscode, @ciseqno,@icondseqno, @ctype, 
         @clabel, @cfieldvalue, @ccondition,
         @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cplaceholder,@cis_required
      ,@cis_readonly,@cis_disabled);";

                                foreach (var cond in detail.processEngineConditionDetails)
                                {
                                    using (SqlCommand cmdCond = new SqlCommand(queryCondition, conn, transaction))
                                    {
                                        cmdCond.Parameters.AddWithValue("@TenantID", cTenantID);
                                        cmdCond.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                                        cmdCond.Parameters.AddWithValue("@ciseqno", masterId);
                                        cmdCond.Parameters.AddWithValue("@icondseqno", cond.icondseqno ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@ctype", cond.ctype ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@clabel", cond.clabel ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cfieldvalue", cond.cfieldValue ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@ccondition", cond.ccondition ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                                        cmdCond.Parameters.AddWithValue("@ccreated_by", username);
                                        cmdCond.Parameters.AddWithValue("@cmodified_by", username);
                                        cmdCond.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                        cmdCond.Parameters.AddWithValue("@cplaceholder", cond.cplaceholder ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cis_required", cond.cisRequired ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cis_readonly", cond.cisReadonly ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cis_disabled", cond.cis_disabled ?? (object)DBNull.Value);

                                        await cmdCond.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                        if (model.cmetaType == "NEW" && model.cmetaName != null && model.cmetaName.Any())
                        {
                            int metaMasterId = 0;

                            string metadatamaster = @"INSERT INTO tbl_process_meta_Master (
    ctenant_id, meta_Name, meta_Description, label, nis_active, ccreated_by,lcreated_date, cmodified_by, lmodified_date)
    VALUES (@TenantID, @meta_Name, @meta_Description, @label, @nis_active, @ccreated_by, 
    @lcreated_date, @cmodified_by, @lmodified_date);SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmd = new SqlCommand(metadatamaster, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmd.Parameters.AddWithValue("@meta_Name", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@meta_Description", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@label", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@nis_active", 1); // Assuming active by default
                                cmd.Parameters.AddWithValue("@ccreated_by", username);
                                cmd.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@cmodified_by", username);
                                cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                var metaId = await cmd.ExecuteScalarAsync();
                                metaMasterId = metaId != null ? Convert.ToInt32(metaId) : 0;
                            }

                            if (metaMasterId > 0)
                            {
                                string updateMasterQuery = @"UPDATE tbl_process_engine_master SET cmeta_id = @cmeta_id
                                                            WHERE id = @masterId";
                                using (var cmd = new SqlCommand(updateMasterQuery, conn, transaction))
                                {
                                    cmd.Parameters.AddWithValue("@masterId", masterId);
                                    cmd.Parameters.AddWithValue("@cmeta_id", metaMasterId);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }

                            if (model.processEngineMeta != null && model.processEngineMeta.Any())
                            {
                                string metadata = @"INSERT INTO tbl_process_meta_detail (
    cheader_id, ctenant_id, cinput_type, label, cplaceholder, cis_required, cis_readonly, cis_disabled, 
    ccreated_by, lcreated_date, cmodified_by, lmodified_date,cfield_value) VALUES (
    @Header_ID, @TenantID, @cinput_type, @label, @cplaceholder, @cis_required, @cis_readonly, 
    @cis_disabled,@ccreated_by, @lcreated_date, 
    @cmodified_by, @lmodified_date, @cfield_value);";
                                foreach (var meta in model.processEngineMeta)
                                {
                                    using (SqlCommand cmdMeta = new SqlCommand(metadata, conn, transaction))
                                    {
                                        cmdMeta.Parameters.AddWithValue("@TenantID", cTenantID);
                                        cmdMeta.Parameters.AddWithValue("@Header_ID", metaMasterId);
                                        cmdMeta.Parameters.AddWithValue("@cinput_type", meta.cinputType ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@label", meta.label ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cplaceholder", meta.cplaceholder ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cis_required", meta.cisRequired ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cis_readonly", meta.cisReadonly ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cis_disabled", meta.cisDisabled ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@ccreated_by", username);
                                        cmdMeta.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                        cmdMeta.Parameters.AddWithValue("@cmodified_by", username);
                                        cmdMeta.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                        cmdMeta.Parameters.AddWithValue("@cfield_value", meta.cfieldValue ?? (object)DBNull.Value);
                                        await cmdMeta.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                        else if (model.cmetaType == "old")
                        {
                            string updateMasterQuery = @"UPDATE tbl_process_engine_master
                                            SET cmeta_id = @cmeta_id WHERE id = @masterId";
                            using (var cmd = new SqlCommand(updateMasterQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@masterId", masterId);
                                cmd.Parameters.AddWithValue("@cmeta_id", model.cmetaId ?? (object)DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
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

        public async Task<List<GetProcessEngineDTO>> GetAllProcessengineAsync(int cTenantID)
        {
            var result = new Dictionary<int, GetProcessEngineDTO>();
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                string query = @"
        SELECT
            m.ID, m.ctenant_id, m.cprocesscode, m.cprocessname, m.cprivilege_type, 
            m.cvalue, m.cpriority_label, m.nshow_timeline, m.cnotification_type, m.cstatus,
            u1.cfirst_name + ' ' + u1.clast_name AS created_by, 
            m.lcreated_date,     
            u2.cfirst_name + ' ' + u2.clast_name AS modified_by,  
            m.lmodified_date, m.cmeta_id,
            d.cheader_id, d.cactivitycode, d.cactivity_description, d.ctask_type, d.cprev_step, d.cactivityname, d.cnext_seqno,
            d.nboard_enabled, d.cmapping_code, d.cmapping_type, d.cparticipant_type, d.csla_day, d.csla_Hour, d.caction_privilege, d.crejection_privilege,
            d.ciseqno,
            c.icond_seqno, c.ctype AS cond_type, c.clabel, c.cfield_value, c.ccondition,
            c.remarks1, c.remarks2, c.remarks3, c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled
        FROM tbl_process_engine_master m
        LEFT JOIN tbl_process_engine_details d ON m.cprocesscode = d.cprocesscode AND m.ID = d.cheader_id
        LEFT JOIN AdminUsers u1 ON CAST(m.ccreated_by AS VARCHAR(50)) = u1.cuserid
        LEFT JOIN AdminUsers u2 ON CAST(m.cmodified_by AS VARCHAR(50)) = u2.cuserid
        LEFT JOIN tbl_process_engine_condition c ON d.ciseqno = c.ciseqno
        WHERE m.ctenant_id = @TenantID
        ORDER BY m.ID DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int headerID = reader.GetInt32(reader.GetOrdinal("ID"));

                    if (!result.TryGetValue(headerID, out var engine))
                    {
                        engine = new GetProcessEngineDTO
                        {
                            ID = headerID,
                            cprocesscode = reader.SafeGetString("cprocesscode"),
                            cprocessname = reader.SafeGetString("cprocessname"),
                            cprivilege_type = reader.SafeGetInt("cprivilege_type"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cvalue = reader.SafeGetString("cvalue"),
                            cpriority_label = reader.SafeGetString("cpriority_label"),
                            nshow_timeline = reader.SafeGetBoolean("nshow_timeline"),
                            cnotification_type = reader.SafeGetInt("cnotification_type"),
                            cmeta_id = reader.SafeGetInt("cmeta_id"),
                            created_by = reader.SafeGetString("created_by"),        
                            ccreated_date = reader.SafeGetDateTime("lcreated_date"),
                            modified_by = reader.SafeGetString("modified_by"),
                            lmodified_date = reader.SafeGetDateTime("lmodified_date"),
                            processEngineChildItems = new List<GetprocessEngineChildItems>()
                        };
                        result[headerID] = engine;
                    }

                    int childHeaderID = reader.SafeGetInt("cheader_id");
                    int childSeqNo = reader.SafeGetInt("ciseqno");

                    var child = engine.processEngineChildItems.FirstOrDefault(x => x.ciseqno == childSeqNo);
                    if (child == null)
                    {
                        child = new GetprocessEngineChildItems
                        {
                            cheader_id = childHeaderID,
                            cactivityCode = reader.SafeGetString("cactivitycode"),
                            cactivityDescription = reader.SafeGetString("cactivity_description"),
                            ctaskType = reader.SafeGetString("ctask_type"),
                            cprevStep = reader.SafeGetString("cprev_step"),
                            cactivityName = reader.SafeGetString("cactivityname"),
                            cnextSeqno = reader.SafeGetString("cnext_seqno"),
                            cmappingCode = reader.SafeGetString("cmapping_code"),
                            cmappingType = reader.SafeGetString("cmapping_type"),
                            cslaDay = reader.SafeGetInt("csla_day"),
                            cslaHour = reader.SafeGetInt("csla_Hour"),
                            ciseqno = childSeqNo,
                            nboardEnabled = reader.SafeGetBoolean("nboard_enabled"),
                            cactionPrivilege = reader.SafeGetString("caction_privilege"),
                            crejectionPrivilege = reader.SafeGetString("crejection_privilege"),
                            cparticipantType = reader.SafeGetString("cparticipant_type"),
                            processEngineConditionDetails = new List<processEngineConditionDetails>()
                        };
                        engine.processEngineChildItems.Add(child);
                    }

                    if (!reader.IsDBNull(reader.GetOrdinal("icond_seqno")))
                    {
                        child.processEngineConditionDetails.Add(new processEngineConditionDetails
                        {
                            cprocessCode = reader.SafeGetString("cprocesscode"),
                            ciseqno = childSeqNo,
                            icondseqno = reader.SafeGetInt("icond_seqno"),
                            ctype = reader.SafeGetString("cond_type"),
                            clabel = reader.SafeGetString("clabel"),
                            cfieldValue = reader.SafeGetString("cfield_value"),
                            ccondition = reader.SafeGetString("ccondition"),
                            cplaceholder = reader.SafeGetString("cplaceholder"),
                            cisRequired = reader.SafeGetBoolean("cis_required"),
                            cisReadonly = reader.SafeGetBoolean("cis_readonly"),
                            cis_disabled = reader.SafeGetBoolean("cis_disabled")
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                // _logger.LogError(ex, "Error fetching process engine data for tenant {TenantID}", cTenantID);
                throw;
            }

            return result.Values.ToList();
        }



    }
}

