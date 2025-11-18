using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Net.NetworkInformation;
using System.Reflection.Emit;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
using System;
using System.Diagnostics;

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
    cnotification_type,lcreated_date,ccreated_by, cmodified_by,lmodified_date, cmeta_id,nIs_deleted) VALUES (@TenantID, @cprocesscode, @cprocessname,@cprocess_type, @cstatus, 
     @cvalue,@cpriority_label,@nshow_timeline,@cnotification_type,
    @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cmeta_id,@nIs_deleted);SELECT SCOPE_IDENTITY();";
                        int masterId;
                        int detailId;
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
                            cmd.Parameters.AddWithValue("@nIs_deleted", 0);
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
       @nboardenabled,@csladay,@cslaHour,@cactionprivilege,@crejectionprivilege,@cmapping_type);SELECT SCOPE_IDENTITY();";

                        int seqNo = 1;
                        foreach (var detail in model.processEngineChildItems)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                                cmdDetail.Parameters.AddWithValue("@ciseqno", seqNo);
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
                                var newId = await cmdDetail.ExecuteScalarAsync();
                                detailId = newId != null ? Convert.ToInt32(newId) : 0;
                                await cmdDetail.ExecuteNonQueryAsync();
                            }
                            seqNo++;
                            if (detail.processEngineConditionDetails != null)
                            {
                                string queryCondition = @"INSERT INTO tbl_process_engine_condition (
        ctenent_id,cheader_id, cprocesscode, ciseqno,icond_seqno, ctype, 
         clabel, cfield_value, ccondition, 
         lcreated_date, ccreated_by, cmodified_by, lmodified_date,cplaceholder,cis_required
      ,cis_readonly,cis_disabled) VALUES (     
         @TenantID,@cheader_id, @cprocesscode, @ciseqno,@icondseqno, @ctype, 
         @clabel, @cfieldvalue, @ccondition,
         @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cplaceholder,@cis_required
      ,@cis_readonly,@cis_disabled);";

                                foreach (var cond in detail.processEngineConditionDetails)
                                {
                                    using (SqlCommand cmdCond = new SqlCommand(queryCondition, conn, transaction))
                                    {
                                        cmdCond.Parameters.AddWithValue("@TenantID", cTenantID);
                                        cmdCond.Parameters.AddWithValue("@cheader_id", masterId);
                                        cmdCond.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                                        cmdCond.Parameters.AddWithValue("@ciseqno", detailId);
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
SELECT m.ID, m.ctenant_id, m.cprocesscode, m.cprocessname, m.cprivilege_type,
    CASE 
        WHEN m.cprivilege_type = 700 THEN 'department'
        WHEN m.cprivilege_type = 701 THEN 'position'
        WHEN m.cprivilege_type = 702 THEN 'role'
        WHEN m.cprivilege_type = 703 THEN 'user'
        WHEN m.cprivilege_type = 704 THEN 'all'
        ELSE 'unknown'
    END AS privilege_name,

    CASE 
        WHEN m.cprivilege_type = 702 THEN 
            (SELECT TOP 1 crole_name 
             FROM tbl_role_master 
             WHERE crole_code = m.cvalue)

        WHEN m.cprivilege_type = 703 THEN 
            (SELECT TOP 1 cuser_name 
             FROM users 
             WHERE cuserid = m.cvalue)

        WHEN m.cprivilege_type = 700 THEN
            (SELECT TOP 1 cdepartment_name
             FROM tbl_department_master
             WHERE cdepartment_code = m.cvalue)

        WHEN m.cprivilege_type = 701 THEN 
            (SELECT TOP 1 cposition_name
             FROM tbl_position_master
             WHERE cposition_code = m.cvalue)

        ELSE m.cvalue
    END AS cvalue,

    m.cpriority_label, m.nshow_timeline, m.cnotification_type, m.cstatus,ISNULL(u1.cfirst_name,'') + ' ' + ISNULL(u1.clast_name,'') AS created_by, 
    m.lcreated_date,ISNULL(u2.cfirst_name,'') + ' ' + ISNULL(u2.clast_name,'') AS modified_by, 
    m.lmodified_date, m.cmeta_id,d.cactivitycode,d.cactivity_description,  d.ctask_type, d.cprev_step, d.cactivityname,  d.cnext_seqno, d.nboard_enabled, 
    d.cmapping_code, d.cmapping_type,  d.cparticipant_type,   d.csla_day,  d.csla_Hour, d.caction_privilege,  d.crejection_privilege,n.notification_type AS Notification_Description,
    s.cstatus_description, d.ciseqno,  d.cheader_id, meta.meta_Name, meta.meta_Description, d.ID AS DetailID  
FROM tbl_process_engine_master m
LEFT JOIN AdminUsers u1 ON CAST(m.ccreated_by AS VARCHAR(50)) = u1.cuserid
LEFT JOIN AdminUsers u2 ON CAST(m.cmodified_by AS VARCHAR(50)) = u2.cuserid
LEFT JOIN tbl_process_engine_details d ON m.cprocesscode = d.cprocesscode AND m.ID = d.cheader_id
LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID 
LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID  
LEFT JOIN tbl_status_master s ON m.cstatus = s.id 
LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
WHERE m.ctenant_id = @TenantID and m.nIs_deleted=0 ORDER BY m.ID DESC;

";

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
                            cprocessType = reader.SafeGetString("cprocess_privilege"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cprocessvalue = reader.SafeGetString("cvalue"),
                            cpriority_label = reader.SafeGetString("cpriority_label"),
                            nshow_timeline = reader.SafeGetBoolean("nshow_timeline"),
                            cnotification_type = reader.SafeGetInt("cnotification_type"),
                            cmeta_id = reader.SafeGetInt("cmeta_id"),
                            created_by = reader.SafeGetString("created_by"),
                            ccreated_date = reader.SafeGetDateTime("lcreated_date"),
                            modified_by = reader.SafeGetString("modified_by"),
                            lmodified_date = reader.SafeGetDateTime("lmodified_date"),
                            cstatus_description = reader.SafeGetString("cstatus_description"),
                            processEngineChildItems = 0
                        };
                        result[headerID] = engine;
                    }

                    int childHeaderId = reader.SafeGetInt("cheader_id");
                    if (childHeaderId != 0)
                    {
                        engine.processEngineChildItems++;
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



        public async Task<List<GetIDProcessEngineDTO>> GetProcessengineAsync(int cTenantID, int id)
        {
            var result = new Dictionary<int, GetIDProcessEngineDTO>();
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                // 🔹 First query: Header + Child (no condition join)
                string mainQuery = @"
SELECT
    m.ID, m.ctenant_id, m.cprocesscode, m.cprocessname, p.cprocess_privilege, 
    m.cvalue, m.cpriority_label, m.nshow_timeline, m.cnotification_type, m.cstatus,
    ISNULL(u1.cfirst_name,'') + ' ' + ISNULL(u1.clast_name,'') AS created_by, 
    m.lcreated_date,     
    ISNULL(u2.cfirst_name,'') + ' ' + ISNULL(u2.clast_name,'') AS modified_by, 
    m.lmodified_date,
    m.cmeta_id,
    d.cactivitycode, d.cactivity_description, d.ctask_type, d.cprev_step, d.cactivityname, d.cnext_seqno,
    d.nboard_enabled, d.cmapping_code, d.cmapping_type, d.cparticipant_type, d.csla_day, d.csla_Hour, d.caction_privilege, d.crejection_privilege,
    n.notification_type As Notification_Description,
    s.cstatus_description, d.ciseqno, d.cheader_id, meta.meta_Name, meta.meta_Description, d.ID as DetailID  
FROM tbl_process_engine_master m
LEFT JOIN AdminUsers u1 ON CAST(m.ccreated_by AS VARCHAR(50)) = u1.cuserid
LEFT JOIN AdminUsers u2 ON CAST(m.cmodified_by AS VARCHAR(50)) = u2.cuserid
LEFT JOIN tbl_process_engine_details d ON m.cprocesscode = d.cprocesscode AND m.ID = d.cheader_id
LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID 
LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID  
LEFT JOIN tbl_status_master s ON m.cstatus = s.id 
LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
WHERE m.ctenant_id = @TenantID AND m.id = @id
ORDER BY m.ID, d.ID ASC;";

                using var cmd = new SqlCommand(mainQuery, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                cmd.Parameters.AddWithValue("@id", id);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int masterId = reader.GetInt32(reader.GetOrdinal("ID"));

                    if (!result.TryGetValue(masterId, out var engine))
                    {
                        engine = new GetIDProcessEngineDTO
                        {
                            ID = masterId,
                            cprocesscode = reader.SafeGetString("cprocesscode"),
                            cprocessname = reader.SafeGetString("cprocessname"),
                            cprocessType = reader.SafeGetString("cprocess_privilege"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cprocessvalue = reader.SafeGetString("cvalue"),
                            cpriority_label = reader.SafeGetString("cpriority_label"),
                            nshow_timeline = reader.SafeGetBoolean("nshow_timeline"),
                            cnotification_type = reader.SafeGetInt("cnotification_type"),
                            cmeta_id = reader.SafeGetInt("cmeta_id"),
                            cmetaname = reader.SafeGetString("meta_Name"),
                            cstatus_description = reader.SafeGetString("cstatus_description"),
                            Notification_Description = reader.SafeGetString("Notification_Description"),
                            processEngineChildItems = new List<GetIDprocessEngineChildItems>(),
                            processEngineMeta = new List<processEngineMeta>()
                        };
                        result[masterId] = engine;
                    }
                    int childHeaderId = reader.SafeGetInt("cheader_id");
                    if (childHeaderId != 0)
                    {
                        var child = new GetIDprocessEngineChildItems
                        {
                            id = reader.SafeGetInt("DetailID"),
                            cheader_id = childHeaderId,
                            cprocessCode = reader.SafeGetString("cprocesscode"),
                            ciseqno = reader.SafeGetInt("ciseqno"),
                            cactivityCode = reader.SafeGetString("cactivitycode"),
                            cactivityDescription = reader.SafeGetString("cactivity_description"),
                            ctaskType = reader.SafeGetString("ctask_type"),
                            cprevStep = reader.SafeGetString("cprev_step"),
                            cactivityName = reader.SafeGetString("cactivityname"),
                            cnextSeqno = reader.SafeGetString("cnext_seqno"),
                            cmappingCode = reader.SafeGetString("cmapping_code"),
                            cmappingType = reader.SafeGetString("cmapping_type"),
                            cparticipantType = reader.SafeGetString("cparticipant_type"),
                            cslaDay = reader.SafeGetInt("csla_day"),
                            cslaHour = reader.SafeGetInt("csla_Hour"),
                            nboardEnabled = reader.SafeGetBoolean("nboard_enabled"),
                            cactionPrivilege = reader.SafeGetString("caction_privilege"),
                            crejectionPrivilege = reader.SafeGetString("crejection_privilege"),
                            processEngineConditionDetails = new List<processEngineConditionDetails>()
                        };
                        engine.processEngineChildItems.Add(child);
                    }
                }
                reader.Close();

                // 🔹 Second query: Condition details
                string condQuery = @"
SELECT 
    m.ID as MasterID,
    c.ciseqno, c.icond_seqno, c.ctype, c.clabel, c.cfield_value, c.ccondition,
    c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled, d.cprocesscode
FROM tbl_process_engine_condition c
INNER JOIN tbl_process_engine_details d ON c.ciseqno = d.id
INNER JOIN tbl_process_engine_master m ON d.cheader_id = m.ID
WHERE m.ID = @HeaderID
ORDER BY c.ciseqno, c.icond_seqno;";

                using var condCmd = new SqlCommand(condQuery, conn);
                condCmd.Parameters.AddWithValue("@HeaderID", id);

                using var condReader = await condCmd.ExecuteReaderAsync();
                while (await condReader.ReadAsync())
                {
                    int masterId = condReader.SafeGetInt("MasterID");
                    int ciseqno = condReader.SafeGetInt("ciseqno");

                    if (result.TryGetValue(masterId, out var engine))
                    {
                        var child = engine.processEngineChildItems.FirstOrDefault(x => x.id == ciseqno);
                        if (child != null)
                        {
                            child.processEngineConditionDetails.Add(new processEngineConditionDetails
                            {
                                cprocessCode = condReader.SafeGetString("cprocesscode"),
                                ciseqno = ciseqno,
                                icondseqno = condReader.SafeGetInt("icond_seqno"),
                                ctype = condReader.SafeGetString("ctype"),
                                clabel = condReader.SafeGetString("clabel"),
                                cfieldValue = condReader.SafeGetString("cfield_value"),
                                ccondition = condReader.SafeGetString("ccondition"),
                                cplaceholder = condReader.SafeGetString("cplaceholder"),
                                cisRequired = condReader.SafeGetBoolean("cis_required"),
                                cisReadonly = condReader.SafeGetBoolean("cis_readonly"),
                                cis_disabled = condReader.SafeGetBoolean("cis_disabled"),
                            });
                        }
                    }
                }
                condReader.Close();
                string metaQuery = @"
SELECT
    m.ID as MasterID,
    m.cmeta_id, meta.meta_Name, meta.meta_Description,
    metadetail.cinput_type, metadetail.label, metadetail.cplaceholder,
    metadetail.cis_required, metadetail.cis_readonly, metadetail.cis_disabled,
    metadetail.cfield_value
FROM tbl_process_engine_master m
LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
LEFT JOIN tbl_process_meta_detail metadetail ON meta.id = metadetail.cheader_id
WHERE m.ctenant_id = @TenantID AND m.id = @id;";
                using var metaCmd = new SqlCommand(metaQuery, conn);
                metaCmd.Parameters.AddWithValue("@TenantID", cTenantID);
                metaCmd.Parameters.AddWithValue("@id", id);          
                using var metaReader = await metaCmd.ExecuteReaderAsync();
                while (await metaReader.ReadAsync())
                {
                    int masterId = metaReader.SafeGetInt("MasterID");

                    if (result.TryGetValue(masterId, out var engine))
                    {
                        engine.processEngineMeta.Add(new processEngineMeta
                        {
                            cinputType = metaReader.SafeGetString("cinput_type"),
                            label = metaReader.SafeGetString("label"),
                            cplaceholder = metaReader.SafeGetString("cplaceholder"),
                            cisRequired = metaReader.SafeGetBoolean("cis_required"),
                            cisReadonly = metaReader.SafeGetBoolean("cis_readonly"),   // ✅ fixed
                            cisDisabled = metaReader.SafeGetBoolean("cis_disabled"),   // ✅ fixed
                            cfieldValue = metaReader.SafeGetString("cfield_value")     // ✅ fixed
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw; // log if needed
            }

            return result.Values.ToList();
        }

        public async Task<bool> UpdateProcessenginestatusdeleteAsync(updatestatusdeleteDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query;
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;

                    if (model.status == null && model.isDeleted == true)
                    {
                        // Case 1: Delete update
                        query = @"
                    UPDATE tbl_process_engine_master SET
                        nIs_deleted = @nIs_deleted,
                        cdeleted_by = @cdeleted_by,
                        ldeleted_date = @ldeleted_date
                    WHERE ID = @ID";

                        cmd.CommandText = query;
                        cmd.Parameters.AddWithValue("@nIs_deleted", true);
                        cmd.Parameters.AddWithValue("@cdeleted_by", username);
                        cmd.Parameters.AddWithValue("@ldeleted_date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                    }
                    else if (model.isDeleted == null && model.status != null)
                    {
                        // Case 2: Status update
                        query = @"
                    UPDATE tbl_process_engine_master SET
                        cstatus = @cstatus,
                        cmodified_by = @cmodified_by,
                        lmodified_date = @lmodified_date
                    WHERE ID = @ID";

                        cmd.CommandText = query;
                        cmd.Parameters.AddWithValue("@cstatus", model.status);
                        cmd.Parameters.AddWithValue("@cmodified_by", username);
                        cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                    }
                    else
                    {
                        // No valid update condition
                        return false;
                    }

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }



        public async Task<int> InsertprocessmappingAsync(createprocessmappingDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        int headerId;
                        // Insert header
                        string query = @"
                    INSERT INTO tbl_engine_master_to_process_privilege 
                        (cprocess_id, cprocesscode, ctenent_id, cprocess_privilege, 
                         ccreated_by, lcreated_date, cmodified_by, lmodified_date)
                    VALUES (@cprocess_id, @cprocesscode, @TenantID, @cprocess_privilege, 
                            @ccreated_by, @ccreated_date, @cmodified_by, @lmodified_date);
                    SELECT SCOPE_IDENTITY();";

                        using (SqlCommand cmd = new SqlCommand(query, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocessid ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocesscode", (object?)model.cprocesscode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocess_privilege", (object?)model.cprivilegeType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ccreated_by", username);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            var newId = await cmd.ExecuteScalarAsync();
                            headerId = Convert.ToInt32(newId);
                        }

                        // Insert details
                        string detailQuery = @"
                    INSERT INTO tbl_process_privilege_details 
                        (cheader_id, cprocess_id, entity_id, entity_value, ctenent_id, cis_active, 
                         ccreated_by, lcreated_date, cmodified_by, lmodified_date)
                    VALUES (@cheader_id, @cprocess_id, @entity_id, @entity_value, @ctenent_id, @cis_active, 
                            @ccreated_by, @lcreated_date, @cmodified_by, @lmodified_date)";

                        foreach (var detail in model.privilegeList)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(detailQuery, conn, tx))
                            {
                                cmdDetail.Parameters.AddWithValue("@cheader_id", headerId);
                                cmdDetail.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocessid ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@entity_id", (object?)detail.value ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@entity_value", (object?)detail.view_value ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ctenent_id", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@cis_active", 1);
                                cmdDetail.Parameters.AddWithValue("@ccreated_by", username);
                                cmdDetail.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                cmdDetail.Parameters.AddWithValue("@cmodified_by", username);
                                cmdDetail.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                                await cmdDetail.ExecuteNonQueryAsync();
                            }
                        }

                        tx.Commit();
                        return headerId;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw; // bubble up the error
                    }
                }
            }
        }


        public async Task<bool> UpdateprocessmappingAsync(updateprocessmappingDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Delete old details
                        string deleteQuery = "DELETE FROM tbl_process_privilege_details WHERE cheader_ID = @cheaderid";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cheaderid", model.cmappingid);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // 2. Insert new details
                        string insertQuery = @"
                    INSERT INTO tbl_process_privilege_details 
                        (cheader_id, cprocess_id, entity_id, entity_value, ctenent_id, cis_active,
                         ccreated_by, lcreated_date, cmodified_by, lmodified_date)
                    VALUES (@cheader_id, @cprocess_id, @entity_id, @entity_value, @ctenent_id, @cis_active,
                            @ccreated_by, @lcreated_date, @cmodified_by, @lmodified_date)";

                        foreach (var item in model.privilegeList)
                        {
                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@cheader_id", model.cmappingid);
                                cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocessid ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@entity_id", (object?)item.value ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@entity_value", (object?)item.view_value ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ctenent_id", cTenantID);
                                cmd.Parameters.AddWithValue("@cis_active", 1);
                                cmd.Parameters.AddWithValue("@ccreated_by", username);
                                cmd.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@cmodified_by", username);
                                cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        string updateQuery = "update tbl_engine_master_to_process_privilege set  cmodified_by=@cmodified_by,lmodified_date=@lmodified_date where id=@cheaderid"; 
                          
                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cheaderid", model.cmappingid);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        tx.Commit();
                        return true; // success
                    }
                    catch
                    {
                        tx.Rollback();
                        throw; // bubble up the error
                    }
                }
            }
        }

        public async Task<List<MappingListDTO>> GetMappingListAsync(int cTenantID)
        {
            var result = new List<MappingListDTO>();

            try
            {
                var connStr = _config.GetConnectionString("Database");

                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    h.ID as mappingID,
                    h.cprocess_id as processID,
                    h.cprocesscode as processName,
                    h.cprocess_privilege as privilegeType,  
                    d.entity_id as value,
                    d.entity_value as view_value
                FROM tbl_engine_master_to_process_privilege h
                LEFT JOIN tbl_process_privilege_details d ON h.ID = d.cheader_id
                WHERE h.ctenent_id = @TenantID  
                AND (d.ctenent_id = @TenantID OR d.ctenent_id IS NULL)
                AND (d.cis_active = 1 OR d.cis_active IS NULL)
                ORDER BY h.ID, d.entity_value";

                    var mappingDict = new Dictionary<int, MappingListDTO>();

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var mappingId = reader.GetInt32(reader.GetOrdinal("mappingID"));

                                if (!mappingDict.ContainsKey(mappingId))
                                {
                                    var mapping = new MappingListDTO
                                    {
                                        mappingID = mappingId,
                                        processID = reader.GetInt32(reader.GetOrdinal("processID")),
                                        processName = reader.GetValue(reader.GetOrdinal("processName"))?.ToString() ?? string.Empty,
                                        privilegeType = reader.GetValue(reader.GetOrdinal("privilegeType"))?.ToString() ?? string.Empty, 
                                        privilegeList = new List<PrivilegeItemDTO>()
                                    };
                                    mappingDict[mappingId] = mapping;
                                }

                                if (!reader.IsDBNull(reader.GetOrdinal("value")) && !reader.IsDBNull(reader.GetOrdinal("view_value")))
                                {
                                    string value = reader.GetValue(reader.GetOrdinal("value"))?.ToString() ?? string.Empty;
                                    string view_value = reader.GetValue(reader.GetOrdinal("view_value"))?.ToString() ?? string.Empty;

                                    if (!string.IsNullOrEmpty(value) && !string.IsNullOrEmpty(view_value))
                                    {
                                        var privilegeItem = new PrivilegeItemDTO
                                        {
                                            value = value,
                                            view_value = view_value
                                        };
                                        mappingDict[mappingId].privilegeList.Add(privilegeItem);
                                    }
                                }
                            }
                        }
                    }

                    result = mappingDict.Values.ToList();
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving mapping list: {ex.Message}");
            }
        }
    }
}

