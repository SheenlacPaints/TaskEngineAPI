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
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Collections.Generic;

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
            SELECT cprocess_privilege,ID FROM [dbo].[tbl_process_privilege_type] WHERE nis_active = 1 AND ctenant_id = @TenantID";


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

            // Format: tenantId-DDMMHHMMSS (e.g., 1500-2910185301)
            string autoprocessCode = $"{cTenantID}-{now:ddM MHHmmss}".Replace(" ", "");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int masterId;

                        string queryMaster = @"INSERT INTO tbl_process_engine_master (
                    ctenant_id,cprocesscode, cprocessname,cprocessdescription, cprivilege_type, cstatus,cvalue,cpriority_label, nshow_timeline,
                    cnotification_type,lcreated_date,ccreated_by, cmodified_by,lmodified_date, cmeta_id,nIs_deleted) 
                    VALUES (@TenantID, @cprocesscode, @cprocessname,@cprocessdescription,@cprocess_type, @cstatus,  
                    @cvalue,@cpriority_label,@nshow_timeline,@cnotification_type,
                    @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cmeta_id,@nIs_deleted);
                    SELECT SCOPE_IDENTITY();";

                        using (SqlCommand cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                            cmd.Parameters.AddWithValue("@cprocessname", (object?)model.cprocessName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocessdescription", (object?)model.cprocessdescription ?? DBNull.Value);
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
                        string queryDetail = @"INSERT INTO tbl_process_engine_details (
                    ctenant_id, cheader_id, ciseqno, cprocesscode,cactivitycode, cactivity_description,  
                    ctask_type, cprev_step, cactivityname, cnext_seqno, lcreated_date, ccreated_by, cmodified_by, lmodified_date, cmapping_code,
                    cparticipant_type,nboard_enabled,csla_day,csla_Hour,caction_privilege,crejection_privilege,cmapping_type) 
                    VALUES (@TenantID, @cheader_id, @ciseqno, @cprocesscode, @cactivitycode, @cactivitydescription,  
                    @ctasktype, @cprev_step, @cactivityname, @cnext_seqno, @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date, @cassignee, @cparticipantType,
                    @nboardenabled,@csladay,@cslaHour,@cactionprivilege,@crejectionprivilege,@cmapping_type);
                    SELECT SCOPE_IDENTITY();";

                        int seqNo = 1;
                        foreach (var detail in model.processEngineChildItems)
                        {
                            int detailId;
                            using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@cprocesscode", autoprocessCode);
                                cmdDetail.Parameters.AddWithValue("@ciseqno", seqNo);
                                cmdDetail.Parameters.AddWithValue("@cheader_id", masterId);
                                cmdDetail.Parameters.AddWithValue("@cactivitycode", detail.cactivityCode ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactivitydescription", detail.cactivityDescription ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ctasktype", detail.ctaskType ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cprev_step", detail.cprevStep ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactivityname", detail.cactivityName ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cnext_seqno", detail.cnextSeqno ?? (object)DBNull.Value);
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
                            }
                            seqNo++;
                            if (detail.processEngineConditionDetails != null)
                            {
                                string queryCondition = @"INSERT INTO tbl_process_engine_condition (
                            ctenant_id,cheader_id, cprocesscode, ciseqno,icond_seqno, ctype,  
                            clabel, cfield_value, ccondition,  
                            lcreated_date, ccreated_by, cmodified_by, lmodified_date,cplaceholder,cis_required
                            ,cis_readonly,cis_disabled,cdata_source) 
                            VALUES (@TenantID,@cheader_id, @cprocesscode, @ciseqno,@icondseqno, @ctype,  
                            @clabel, @cfieldvalue, @ccondition,
                            @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cplaceholder,@cis_required
                            ,@cis_readonly,@cis_disabled,@cdatasource);";

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
                                        cmdCond.Parameters.AddWithValue("@cdatasource", cond.cdatasource ?? (object)DBNull.Value);
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
                        @lcreated_date, @cmodified_by, @lmodified_date);
                        SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmd = new SqlCommand(metadatamaster, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmd.Parameters.AddWithValue("@meta_Name", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@meta_Description", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@label", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@nis_active", 1);
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
                            ccreated_by, lcreated_date, cmodified_by, lmodified_date,cfield_value,cdata_source) 
                            VALUES (@Header_ID, @TenantID, @cinput_type, @label, @cplaceholder, @cis_required, @cis_readonly,  
                            @cis_disabled,@ccreated_by, @lcreated_date, @cmodified_by, @lmodified_date, @cfield_value,@cdatasource);";

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
                                        cmdMeta.Parameters.AddWithValue("@cdatasource", meta.cdatasource ?? (object)DBNull.Value);
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
                        // Rollback on any error
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
        public async Task<List<GetProcessEngineDTO>> GetAllProcessengineAsyncold(int cTenantID)
        {
            var result = new Dictionary<int, GetProcessEngineDTO>();
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                string query = @"SELECT m.ID, m.ctenant_id, m.cprocessdescription, m.cprocesscode, m.cprocessname,
m.cprivilege_type,p.cprocess_privilege as privilege_name,
    CASE 
        WHEN p.cprocess_privilege ='role' THEN 
            (SELECT TOP 1 crole_name 
             FROM tbl_role_master 
             WHERE crole_code = m.cvalue)
        WHEN p.cprocess_privilege = 'user' THEN 
            (SELECT TOP 1 cuser_name 
             FROM users 
             WHERE cuserid = m.cvalue)
        WHEN p.cprocess_privilege = 'department' THEN
            (SELECT TOP 1 cdepartment_name
             FROM tbl_department_master
             WHERE cdepartment_code = m.cvalue)
        WHEN p.cprocess_privilege = 'position' THEN 
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
LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID and m.ctenant_id=p.ctenant_id
LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID  
LEFT JOIN tbl_status_master s ON m.cstatus = s.id 
LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
WHERE m.ctenant_id = @TenantID and m.nIs_deleted=0 ORDER BY m.ID DESC;";

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
                            privilege_name = reader.SafeGetString("privilege_name"),
                            cprivilege_type = reader.SafeGetInt("cprivilege_type"),
                            cprocessType = reader.SafeGetString("cprocesscode"),
                            cprocessdescription= reader.SafeGetString("cprocessdescription"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cprocessvalueid = reader.SafeGetString("cvalue"),
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

SELECT m.ID, m.ctenant_id, m.cprocessdescription, m.cprocesscode, m.cprocessname, m.cprivilege_type,
p.cprocess_privilege as privilege_name,
    CASE 
        WHEN p.cprocess_privilege ='role' THEN 
            (SELECT TOP 1 crole_name 
             FROM tbl_role_master 
             WHERE crole_code = m.cvalue)
        WHEN p.cprocess_privilege = 'user' THEN 
            (SELECT TOP 1 cuser_name 
             FROM users 
             WHERE cuserid = m.cvalue)
        WHEN p.cprocess_privilege = 'department' THEN
            (SELECT TOP 1 cdepartment_name
             FROM tbl_department_master
             WHERE cdepartment_code = m.cvalue)
        WHEN p.cprocess_privilege = 'position' THEN 
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
LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID and m.ctenant_id=p.ctenant_id
LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID  
LEFT JOIN tbl_status_master s ON m.cstatus = s.id 
LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
WHERE m.ctenant_id = @TenantID and m.nIs_deleted=0  and m.ID=@id ORDER BY m.ID DESC;";

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
                            cprocessdescription = reader.SafeGetString("cprocessdescription"),
                            cprivilege_type = reader.SafeGetInt("cprivilege_type"),
                            privilege_name = reader.SafeGetString("privilege_name"),                                                     
                            cstatus = reader.SafeGetString("cstatus"),
                            cstatus_description = reader.SafeGetString("cstatus_description"),
                            cprocessvalue = reader.SafeGetString("cvalue"),
                            cpriority_label = reader.SafeGetString("cpriority_label"),
                            nshow_timeline = reader.SafeGetBoolean("nshow_timeline"),
                            cnotification_type = reader.SafeGetInt("cnotification_type"),
                            cmeta_id = reader.SafeGetInt("cmeta_id"),
                            cmetaname = reader.SafeGetString("meta_Name"),                      
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
    c.cplaceholder, c.cis_required, c.cis_readonly, c.cis_disabled, d.cprocesscode,c.cdata_source
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
                                cdatasource = condReader.SafeGetString("cdata_source"),
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
    metadetail.cfield_value,metadetail.cdata_source
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
                            cfieldValue = metaReader.SafeGetString("cfield_value"),
                            cdatasource = metaReader.SafeGetString("cdata_source")
                            // ✅ fixed
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
                        string checkDuplicateQuery = @"
                    SELECT COUNT(1) 
                    FROM tbl_engine_master_to_process_privilege 
                    WHERE cprocess_id = @cprocess_id 
                    AND ctenant_id = @ctenant_id";

                        using (SqlCommand checkCmd = new SqlCommand(checkDuplicateQuery, conn, tx))
                        {
                            checkCmd.Parameters.AddWithValue("@cprocess_id", model.cprocessid);
                            checkCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);

                            int existingCount = (int)await checkCmd.ExecuteScalarAsync();

                            if (existingCount > 0)
                            {
                                string getExistingPrivilegeQuery = @"
                            SELECT cprocess_privilege 
                            FROM tbl_engine_master_to_process_privilege 
                            WHERE cprocess_id = @cprocess_id 
                            AND ctenant_id = @ctenant_id";

                                using (SqlCommand getPrivilegeCmd = new SqlCommand(getExistingPrivilegeQuery, conn, tx))
                                {
                                    getPrivilegeCmd.Parameters.AddWithValue("@cprocess_id", model.cprocessid);
                                    getPrivilegeCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);

                                    var existingPrivilege = await getPrivilegeCmd.ExecuteScalarAsync();
                                    throw new InvalidOperationException($"Process ID {model.cprocessid} already has privilege {existingPrivilege}. Only one privilege allowed per process.");
                                }
                            }
                        }

                        int headerId;
                        string query = @"
                    INSERT INTO tbl_engine_master_to_process_privilege 
                        (cprocess_id, cprocesscode, ctenant_id, cprocess_privilege, 
                         ccreated_by, lcreated_date, cmodified_by, lmodified_date,cis_active)
                    VALUES (@cprocess_id, @cprocesscode, @TenantID, @cprocess_privilege, 
                            @ccreated_by, @ccreated_date, @cmodified_by, @lmodified_date,@cis_active);
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
                            cmd.Parameters.AddWithValue("@cis_active", 1);
                            var newId = await cmd.ExecuteScalarAsync();
                            headerId = Convert.ToInt32(newId);
                        }

                        string detailQuery = @"
                    INSERT INTO tbl_process_privilege_details 
                        (cheader_id, cprocess_id, entity_id, entity_value, ctenant_id, cis_active, 
                         ccreated_by, lcreated_date, cmodified_by, lmodified_date)
                    VALUES (@cheader_id, @cprocess_id, @entity_id, @entity_value, @ctenant_id, @cis_active, 
                            @ccreated_by, @lcreated_date, @cmodified_by, @lmodified_date)";

                        foreach (var detail in model.privilegeList)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(detailQuery, conn, tx))
                            {
                                cmdDetail.Parameters.AddWithValue("@cheader_id", headerId);
                                cmdDetail.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocessid ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@entity_id", (object?)detail.value ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@entity_value", (object?)detail.view_value ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ctenant_id", cTenantID);
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
                        throw;
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
                        string checkDuplicateQuery = @"
                    SELECT COUNT(1) 
                    FROM tbl_engine_master_to_process_privilege 
                    WHERE cprocess_id = @cprocess_id                   
                    AND ctenant_id = @ctenant_id
                    AND id != @current_id";

                        using (SqlCommand checkCmd = new SqlCommand(checkDuplicateQuery, conn, tx))
                        {
                            checkCmd.Parameters.AddWithValue("@cprocess_id", model.cprocessid);
                            checkCmd.Parameters.AddWithValue("@cprocess_privilege", model.cprivilegeType);
                            checkCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                            checkCmd.Parameters.AddWithValue("@current_id", model.cmappingid);

                            int duplicateCount = (int)await checkCmd.ExecuteScalarAsync();

                            if (duplicateCount > 0)
                            {
                                throw new InvalidOperationException($"Process privilege '{model.cprivilegeType}' is already assigned to this process. Please choose a different privilege number.");
                            }
                        }

                        string deleteQuery = "DELETE FROM tbl_process_privilege_details WHERE cheader_ID = @cheaderid";
                        using (SqlCommand cmd = new SqlCommand(deleteQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cheaderid", model.cmappingid);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string insertQuery = @"
                    INSERT INTO tbl_process_privilege_details 
                        (cheader_id, cprocess_id, entity_id, entity_value, ctenant_id, cis_active,
                         ccreated_by, lcreated_date, cmodified_by, lmodified_date)
                    VALUES (@cheader_id, @cprocess_id, @entity_id, @entity_value, @ctenant_id, @cis_active,
                            @ccreated_by, @lcreated_date, @cmodified_by, @lmodified_date)";

                        foreach (var item in model.privilegeList)
                        {
                            using (SqlCommand cmd = new SqlCommand(insertQuery, conn, tx))
                            {
                                cmd.Parameters.AddWithValue("@cheader_id", model.cmappingid);
                                cmd.Parameters.AddWithValue("@cprocess_id", (object?)model.cprocessid ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@entity_id", (object?)item.value ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@entity_value", (object?)item.view_value ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@ctenant_id", cTenantID);
                                cmd.Parameters.AddWithValue("@cis_active", 1);
                                cmd.Parameters.AddWithValue("@ccreated_by", username);
                                cmd.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                cmd.Parameters.AddWithValue("@cmodified_by", username);
                                cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        string updateQuery = @"
                    UPDATE tbl_engine_master_to_process_privilege 
                    SET cmodified_by = @cmodified_by,
                        lmodified_date = @lmodified_date,
                        cprocess_privilege = @cprocess_privilege,
                        cis_active=@cisactive
                    WHERE id = @cheaderid AND ctenant_id = @tenantid";

                        using (SqlCommand cmd = new SqlCommand(updateQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cheaderid", model.cmappingid);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@cprocess_privilege", model.cprivilegeType);
                            cmd.Parameters.AddWithValue("@tenantid", cTenantID);                           
                            cmd.Parameters.AddWithValue("@cisactive", model.cis_active ?? (object)DBNull.Value);

                            int rowsAffected = await cmd.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                tx.Rollback();
                                return false;
                            }
                        }

                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
                    }
                }
            }
        }
        public async Task<bool> DeleteprocessmappingAsync(int mappingId, int tenantId, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                using (SqlTransaction tx = conn.BeginTransaction())
                {
                    try
                    {
                        // Delete from child table first
                        string deleteDetailsQuery = "DELETE FROM tbl_process_privilege_details WHERE cheader_ID = @cheaderid";
                        using (SqlCommand cmd = new SqlCommand(deleteDetailsQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cheaderid", mappingId);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // Delete from parent table
                        string deleteMainQuery = "DELETE FROM tbl_engine_master_to_process_privilege WHERE id = @cheaderid AND ctenant_id = @tenantid";
                        using (SqlCommand cmd = new SqlCommand(deleteMainQuery, conn, tx))
                        {
                            cmd.Parameters.AddWithValue("@cheaderid", mappingId);
                            cmd.Parameters.AddWithValue("@tenantid", tenantId);
                            int rowsAffected = await cmd.ExecuteNonQueryAsync();

                            if (rowsAffected == 0)
                            {
                                tx.Rollback();
                                return false;
                            }
                        }

                        tx.Commit();
                        return true;
                    }
                    catch
                    {
                        tx.Rollback();
                        throw;
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
                            h.cprocesscode as processcode,
                            e.cprocessname as cprocessname,
                            e.cprocessdescription as cprocessdescription,
                            h.cprocess_privilege as privilegeType, 
                            p.cprocess_privilege as privilegeTypevalue,
                            d.entity_id as value,
                            d.entity_value as view_value,d.cis_active
                        FROM tbl_engine_master_to_process_privilege h
                        inner join tbl_process_engine_master e on e.id=h.cprocess_id
                        inner join tbl_process_privilege_type p on h.cprocess_privilege=p.ID
                        LEFT JOIN tbl_process_privilege_details d ON h.ID = d.cheader_id
                        WHERE h.ctenant_id =@TenantID  
                        AND (d.ctenant_id = @TenantID OR d.ctenant_id IS NULL)
                        AND (d.cis_active = 1) and
                        (h.cis_active=1)

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
                                        cprocessname = reader.GetValue(reader.GetOrdinal("cprocessname"))?.ToString() ?? string.Empty,
                                        cprocessdescription = reader.GetValue(reader.GetOrdinal("cprocessdescription"))?.ToString() ?? string.Empty,
                                        cprocesscode = reader.GetValue(reader.GetOrdinal("processcode"))?.ToString() ?? string.Empty,
                                        privilegeType = reader.GetValue(reader.GetOrdinal("privilegeType"))?.ToString() ?? string.Empty,
                                        privilegeTypevalue = reader.GetValue(reader.GetOrdinal("privilegeTypevalue"))?.ToString() ?? string.Empty,
                                        cis_active = reader.IsDBNull(reader.GetOrdinal("cis_active"))
    ? null
    : reader.GetBoolean(reader.GetOrdinal("cis_active")),
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
        public async Task<List<GetProcessEngineDTO>> GetAllProcessengineAsync(int cTenantID,string searchText = null,int page = 1,int pageSize = 10,int? created_by = null,string priority = null,int? status = null)
        {
            var result = new List<GetProcessEngineDTO>();
            var connStr = _config.GetConnectionString("Database");

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            int skip = (page - 1) * pageSize;

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                string query = @"SELECT m.ID, m.ctenant_id, m.cprocessdescription, m.cprocesscode, m.cprocessname,
                                m.cprivilege_type, p.cprocess_privilege AS privilege_name, CASE  
                                WHEN p.cprocess_privilege = 'role' THEN (SELECT TOP 1 crole_name FROM tbl_role_master WHERE crole_code = m.cvalue)
                                WHEN p.cprocess_privilege = 'user' THEN (SELECT TOP 1 cuser_name FROM users WHERE CAST(cuserid AS VARCHAR(50)) = m.cvalue)
                                WHEN p.cprocess_privilege = 'department' THEN (SELECT TOP 1 cdepartment_name FROM tbl_department_master WHERE cdepartment_code = m.cvalue)
                                WHEN p.cprocess_privilege = 'position' THEN (SELECT TOP 1 cposition_name FROM tbl_position_master WHERE cposition_code = m.cvalue)
                                ELSE m.cvalue END AS cvalue,m.cpriority_label, m.nshow_timeline, m.cnotification_type, m.cstatus,
                                m.ccreated_by,m.lcreated_date,ISNULL(u2.cfirst_name,'') + ' ' + ISNULL(u2.clast_name,'') AS modified_by,
                                m.lmodified_date, m.cmeta_id,n.notification_type AS Notification_Description,
                                s.cstatus_description,meta.meta_Name, meta.meta_Description,COUNT(d.ID) AS DetailCount,
                                CASE WHEN SUM(ISNULL(d.csla_day, 0)) + SUM(ISNULL(d.csla_Hour, 0)) > 0
                                THEN CAST(SUM(ISNULL(d.csla_day, 0)) + SUM(ISNULL(d.csla_Hour, 0)) / 24 AS VARCHAR(10)) + ' days ' + 
                                CAST(SUM(ISNULL(d.csla_Hour, 0)) % 24 AS VARCHAR(10)) + ' hrs' ELSE '' END AS sla_Sum
                                FROM tbl_process_engine_master m
                                LEFT JOIN AdminUsers u1 ON CAST(m.ccreated_by AS VARCHAR(50)) = CAST(u1.cuserid AS VARCHAR(50))
                                LEFT JOIN AdminUsers u2 ON CAST(m.cmodified_by AS VARCHAR(50)) = CAST(u2.cuserid AS VARCHAR(50))
                                LEFT JOIN tbl_process_engine_details d ON m.ID = d.cheader_id
                                LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID AND m.ctenant_id = p.ctenant_id
                                LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID
                                LEFT JOIN tbl_status_master s ON m.cstatus = CAST(s.id AS VARCHAR(50))
                                LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
                                WHERE m.ctenant_id = @TenantID AND m.nIs_deleted = 0";

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query += " AND (m.cprocesscode LIKE '%' + @SearchText + '%' OR m.cprocessname LIKE '%' + @SearchText + '%' OR m.cprocessdescription LIKE '%' + @SearchText + '%')";
                }
                if (created_by.HasValue)
                {
                    query += " AND m.ccreated_by = @CreatedBy";
                }


                if (!string.IsNullOrWhiteSpace(priority))
                {
                    query += " AND m.cpriority_label = @Priority";
                }

                if (status.HasValue && status.Value > 0)
                {
                    query += " AND m.cstatus = @Status";
                }

                query += @" GROUP BY m.ID, m.ctenant_id, m.cprocessdescription, m.cprocesscode, m.cprocessname,
        m.cprivilege_type, p.cprocess_privilege, m.cvalue, m.cpriority_label,m.ccreated_by, m.nshow_timeline,
        m.cnotification_type, m.cstatus, ISNULL(u1.cfirst_name,'') + ' ' + ISNULL(u1.clast_name,''),
        m.lcreated_date,ISNULL(u2.cfirst_name,'') + ' ' + ISNULL(u2.clast_name,''),m.lmodified_date, m.cmeta_id,
        n.notification_type, s.cstatus_description, meta.meta_Name, meta.meta_Description ORDER BY m.ID DESC";

                query += $@" OFFSET {skip} ROWS FETCH NEXT {pageSize} ROWS ONLY;";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    cmd.Parameters.AddWithValue("@SearchText", searchText);
                }
                if (created_by.HasValue)
                {
                    cmd.Parameters.AddWithValue("@CreatedBy", created_by.Value.ToString());
                }
            
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    cmd.Parameters.AddWithValue("@Priority", priority);
                }
                if (status.HasValue && status.Value > 0)
                {
                    cmd.Parameters.AddWithValue("@Status", status.Value);
                }

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new GetProcessEngineDTO
                    {
                        ID = reader["ID"] == DBNull.Value ? 0 : Convert.ToInt32(reader["ID"]),
                        cprocesscode = reader["cprocesscode"]?.ToString() ?? "",
                        cprocessname = reader["cprocessname"]?.ToString() ?? "",
                        privilege_name = reader["privilege_name"]?.ToString() ?? "",
                        cprivilege_type = reader["cprivilege_type"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cprivilege_type"]),
                        cprocessType = reader["cprocessname"]?.ToString() ?? "",
                        cprocessdescription = reader["cprocessdescription"]?.ToString() ?? "",
                        cstatus = reader["cstatus"]?.ToString() ?? "",
                        cprocessvalueid = reader["cvalue"]?.ToString() ?? "",
                        cpriority_label = reader["cpriority_label"]?.ToString() ?? "",
                        nshow_timeline = reader["nshow_timeline"] != DBNull.Value && Convert.ToBoolean(reader["nshow_timeline"]),
                        cnotification_type = reader["cnotification_type"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cnotification_type"]),
                        cmeta_id = reader["cmeta_id"] == DBNull.Value ? 0 : Convert.ToInt32(reader["cmeta_id"]),
                        cmetaName = reader["meta_Name"]?.ToString() ?? "",
                        created_by =reader["ccreated_by"]?.ToString() ?? "",
                        ccreated_date = reader["lcreated_date"] == DBNull.Value ? null : (DateTime?)reader["lcreated_date"],
                        modified_by = reader["modified_by"]?.ToString() ?? "",
                        lmodified_date = reader["lmodified_date"] == DBNull.Value ? null : (DateTime?)reader["lmodified_date"],
                        cstatus_description = reader["cstatus_description"]?.ToString() ?? "",
                        processEngineChildItems = reader["DetailCount"] == DBNull.Value ? 0 : Convert.ToInt32(reader["DetailCount"]),
                        slasum = reader["sla_Sum"]?.ToString() ?? ""
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching process engine list", ex);
            }

            return result;
        }



        public async Task<bool> UpdateProcessEngineAsync(UpdateProcessEngineDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");        
            if (model.ID == null)
            {
                throw new ArgumentException("Master ID is required for updating the process engine.");
            }

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();              
                string checkDuplicateQuery = @"SELECT COUNT(1) FROM tbl_taskflow_master a LEFT JOIN tbl_taskflow_detail b ON a.itaskno = b.itaskno
                                              WHERE ccurrent_status IN ('P', 'H') AND cprocess_id = @cprocess_id AND a.ctenant_id = @ctenant_id;"; 

                using (SqlCommand checkCmd = new SqlCommand(checkDuplicateQuery, conn))
                {
                   
                    checkCmd.Parameters.AddWithValue("@cprocess_id", model.ID);
                    checkCmd.Parameters.AddWithValue("@ctenant_id", cTenantID);

                    int duplicateCount = (int)await checkCmd.ExecuteScalarAsync();

                    if (duplicateCount > 0)
                    {
                        
                        return false;
                    }
                }

                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        int masterId = model.ID.Value;
                        string deleteConditionsQuery = @"
                    DELETE FROM tbl_process_engine_condition 
                    WHERE cheader_id = @MasterID AND ctenant_id = @TenantID;";
                        using (SqlCommand cmd = new SqlCommand(deleteConditionsQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MasterID", masterId);
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string deleteDetailsQuery = @"
                    DELETE FROM tbl_process_engine_details 
                    WHERE cheader_id = @MasterID AND ctenant_id = @TenantID;";
                        using (SqlCommand cmd = new SqlCommand(deleteDetailsQuery, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@MasterID", masterId);
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        string queryMaster = @"
                    UPDATE tbl_process_engine_master SET  
                        cprocessname=@cprocessname, cprocessdescription=@cprocessdescription, 
                        cprivilege_type=@cprocess_type, cstatus=@cstatus, cvalue=@cvalue, 
                        cpriority_label=@cpriority_label, nshow_timeline=@nshow_timeline,
                        cnotification_type=@cnotification_type, cmodified_by=@cmodified_by,
                        lmodified_date=@lmodified_date, cmeta_id=@cmeta_id, nIs_deleted=@nIs_deleted
                    WHERE ID=@ID;"; 

                        using (SqlCommand cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@ID", masterId);
                            cmd.Parameters.AddWithValue("@cprocessname", (object?)model.cprocessName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocessdescription", (object?)model.cprocessdescription ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocess_type", (object?)model.cprivilegeType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", (object?)model.cstatus ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cvalue", (object?)model.cvalue ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cpriority_label", (object?)model.cpriorityLabel ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nshow_timeline", (object?)model.nshowTimeline ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cnotification_type", (object?)model.cnotificationType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@cmeta_id", (object?)model.cmetaId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@nIs_deleted", 0);
                            await cmd.ExecuteNonQueryAsync();
                        }
                        string queryDetail = @"INSERT INTO tbl_process_engine_details (
                        ctenant_id, cheader_id, ciseqno, cprocesscode, cactivitycode, cactivity_description,  
                        ctask_type, cprev_step, cactivityname, cnext_seqno, lcreated_date, ccreated_by, 
                        cmodified_by, lmodified_date, cmapping_code, cparticipant_type, nboard_enabled, 
                        csla_day, csla_Hour, caction_privilege, crejection_privilege, cmapping_type) 
                        VALUES (@TenantID, @cheader_id, @ciseqno, @cprocesscode, @cactivitycode, @cactivitydescription,  
                        @ctasktype, @cprevstep, @cactivityname, @cnextseqno, @ccreated_date, @ccreated_by, 
                        @cmodified_by, @lmodified_date, @cassignee, @cparticipantType, @nboardenabled, 
                        @csladay, @cslaHour, @cactionprivilege, @crejectionprivilege, @cmapping_type);
                        SELECT SCOPE_IDENTITY();";

                        int seqNo = 1;
                        
                        foreach (var detail in model.processEngineChildItems)
                        {                       
                            int detailId;
                            using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@cprocesscode", model.cprocessCode ?? (object)DBNull.Value);
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
                                cmdDetail.Parameters.AddWithValue("@nboardenabled", (object?)detail.nboardEnabled ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@csladay", (object?)detail.cslaDay ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cslaHour", (object?)detail.cslaHour ?? DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactionprivilege", detail.cactionPrivilege ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@crejectionprivilege", detail.crejectionPrivilege ?? (object)DBNull.Value);

                                var newId = await cmdDetail.ExecuteScalarAsync();
                                detailId = newId != null ? Convert.ToInt32(newId) : 0;
                            }
                            seqNo++;

                            if (detail.processEngineConditionDetails != null && detailId > 0)
                            {
                                string queryCondition = @"INSERT INTO tbl_process_engine_condition (
                                ctenant_id, cheader_id, cprocesscode, ciseqno, icond_seqno, ctype,  
                                clabel, cfield_value, ccondition, lcreated_date, ccreated_by, 
                                cmodified_by, lmodified_date, cplaceholder, cis_required, cis_readonly, cis_disabled) 
                                VALUES (
                                @TenantID, @cheader_id, @cprocesscode, @ciseqno, @icondseqno, @ctype,  
                                @clabel, @cfieldvalue, @ccondition, @lcreated_date, @ccreated_by, 
                                @cmodified_by, @lmodified_date, @cplaceholder, @cis_required, @cis_readonly, @cis_disabled);";

                                foreach (var cond in detail.processEngineConditionDetails)
                                {
                                    using (SqlCommand cmdCond = new SqlCommand(queryCondition, conn, transaction))
                                    {
                                        cmdCond.Parameters.AddWithValue("@TenantID", cTenantID);
                                        cmdCond.Parameters.AddWithValue("@cheader_id", masterId);
                                        cmdCond.Parameters.AddWithValue("@cprocesscode", model.cprocessCode ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@ciseqno", detailId);
                                        cmdCond.Parameters.AddWithValue("@icondseqno", (object?)cond.icondseqno ?? DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@ctype", cond.ctype ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@clabel", cond.clabel ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cfieldvalue", cond.cfieldValue ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@ccondition", cond.ccondition ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                        cmdCond.Parameters.AddWithValue("@ccreated_by", username);
                                        cmdCond.Parameters.AddWithValue("@cmodified_by", username);
                                        cmdCond.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                        cmdCond.Parameters.AddWithValue("@cplaceholder", cond.cplaceholder ?? (object)DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cis_required", (object?)cond.cisRequired ?? DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cis_readonly", (object?)cond.cisReadonly ?? DBNull.Value);
                                        cmdCond.Parameters.AddWithValue("@cis_disabled", (object?)cond.cis_disabled ?? DBNull.Value);

                                        await cmdCond.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }                     
                        if (model.cmetaType == "NEW" && model.cmetaName != null && model.cmetaName.Any())
                        {                          
                            int metaMasterId = 0;
                            string metadatamaster = @"INSERT INTO tbl_process_meta_Master (
                            ctenant_id, meta_Name, meta_Description, label, nis_active, ccreated_by, lcreated_date, cmodified_by, lmodified_date)
                            VALUES (@TenantID, @meta_Name, @meta_Description, @label, @nis_active, @ccreated_by,  
                            @lcreated_date, @cmodified_by, @lmodified_date); SELECT SCOPE_IDENTITY();";

                            using (SqlCommand cmd = new SqlCommand(metadatamaster, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmd.Parameters.AddWithValue("@meta_Name", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@meta_Description", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@label", (object?)model.cmetaName ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@nis_active", 1);
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
                                cheader_id, ctenant_id, cinput_type, label, cplaceholder, cis_required, 
                                cis_readonly, cis_disabled, ccreated_by, lcreated_date, cmodified_by, 
                                lmodified_date, cfield_value,cdata_source) VALUES (
                                @Header_ID, @TenantID, @cinput_type, @label, @cplaceholder, @cis_required,  
                                @cis_readonly, @cis_disabled, @ccreated_by, @lcreated_date, 
                                @cmodified_by, @lmodified_date, @cfield_value,@cdata_source);";

                                foreach (var meta in model.processEngineMeta)
                                {
                                    using (SqlCommand cmdMeta = new SqlCommand(metadata, conn, transaction))
                                    {
                                        cmdMeta.Parameters.AddWithValue("@TenantID", cTenantID);
                                        cmdMeta.Parameters.AddWithValue("@Header_ID", metaMasterId);
                                        cmdMeta.Parameters.AddWithValue("@cinput_type", meta.cinputType ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@label", meta.label ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cplaceholder", meta.cplaceholder ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cis_required", (object?)meta.cisRequired ?? DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cis_readonly", (object?)meta.cisReadonly ?? DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cis_disabled", (object?)meta.cisDisabled ?? DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@ccreated_by", username);
                                        cmdMeta.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                                        cmdMeta.Parameters.AddWithValue("@cmodified_by", username);
                                        cmdMeta.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                        cmdMeta.Parameters.AddWithValue("@cfield_value", meta.cfieldValue ?? (object)DBNull.Value);
                                        cmdMeta.Parameters.AddWithValue("@cdata_source", meta.cdatasource ?? (object)DBNull.Value);
                                        await cmdMeta.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                        else if (model.cmetaType == "old")
                        {
                           
                            string updateMasterQuery = @"
                        UPDATE tbl_process_engine_master
                        SET cmeta_id = @cmeta_id WHERE id = @masterId";
                            using (var cmd = new SqlCommand(updateMasterQuery, conn, transaction))
                            {
                                cmd.Parameters.AddWithValue("@masterId", masterId);
                                cmd.Parameters.AddWithValue("@cmeta_id", model.cmetaId ?? (object)DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        public async Task<List<GetProcessEngineDTO>> GetAllProcessengineAsyncnew(
int cTenantID, string searchText = null, int page = 1, int pageSize = 10, string created_by = null, string priority = null, int? status = null)
        {

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            int skip = (page - 1) * pageSize;

            var result = new List<GetProcessEngineDTO>();
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using var conn = new SqlConnection(connStr);
                await conn.OpenAsync();

                string query = @"SELECT m.ID, m.ctenant_id, m.cprocessdescription, m.cprocesscode, m.cprocessname,
                m.cprivilege_type, p.cprocess_privilege AS privilege_name,CASE  
                WHEN p.cprocess_privilege = 'role' THEN  
                (SELECT TOP 1 crole_name FROM tbl_role_master WHERE crole_code = m.cvalue)
               WHEN p.cprocess_privilege = 'user' THEN  
               (SELECT TOP 1 cuser_name FROM users WHERE CAST(cuserid AS VARCHAR(50)) = m.cvalue)
               WHEN p.cprocess_privilege = 'department' THEN
               (SELECT TOP 1 cdepartment_name FROM tbl_department_master WHERE cdepartment_code = m.cvalue)
                WHEN p.cprocess_privilege = 'position' THEN  
                (SELECT TOP 1 cposition_name FROM tbl_position_master WHERE cposition_code = m.cvalue)
        ELSE m.cvalue END AS cvalue,m.cpriority_label, m.nshow_timeline, m.cnotification_type, m.cstatus,
    ISNULL(u1.cfirst_name,'') + ' ' + ISNULL(u1.clast_name,'') AS created_by,m.lcreated_date,
    ISNULL(u2.cfirst_name,'') + ' ' + ISNULL(u2.clast_name,'') AS modified_by,m.lmodified_date, m.cmeta_id,
    n.notification_type AS Notification_Description,s.cstatus_description,meta.meta_Name, meta.meta_Description,
    COUNT(d.ID) AS DetailCount,CASE WHEN SUM(ISNULL(d.csla_day, 0)) > 0 OR SUM(ISNULL(d.csla_Hour, 0)) > 0
        THEN CASE 
            WHEN SUM(ISNULL(d.csla_day, 0)) + SUM(ISNULL(d.csla_Hour, 0)) / 24 > 0 
            THEN CAST(SUM(ISNULL(d.csla_day, 0)) + SUM(ISNULL(d.csla_Hour, 0)) / 24 AS VARCHAR(10)) + ' days ' + 
                 CAST(SUM(ISNULL(d.csla_Hour, 0)) % 24 AS VARCHAR(10)) + ' hrs'
            ELSE CAST(SUM(ISNULL(d.csla_Hour, 0)) % 24 AS VARCHAR(10)) + ' hrs'
        END
        ELSE ''
    END AS csla_Sum
FROM tbl_process_engine_master m
LEFT JOIN AdminUsers u1 ON CAST(m.ccreated_by AS VARCHAR(50)) = CAST(u1.cuserid AS VARCHAR(50))
LEFT JOIN AdminUsers u2 ON CAST(m.cmodified_by AS VARCHAR(50)) = CAST(u2.cuserid AS VARCHAR(50))
LEFT JOIN tbl_process_engine_details d ON m.ID = d.cheader_id
LEFT JOIN tbl_process_privilege_type p ON m.cprivilege_type = p.ID AND m.ctenant_id = p.ctenant_id
LEFT JOIN tbl_notification_type n ON m.cnotification_type = n.ID  
LEFT JOIN tbl_status_master s ON m.cstatus = CAST(s.id  AS VARCHAR(50))
LEFT JOIN tbl_process_meta_Master meta ON m.cmeta_id = meta.id
        WHERE m.ctenant_id = @TenantID AND m.nIs_deleted = 0";

                // ===================================
                // ADD DYNAMIC FILTERING (WHERE CLAUSES)
                // ===================================
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    query += @"
        AND (
            m.cprocesscode LIKE '%' + @SearchText + '%'  
            OR m.cprocessname LIKE '%' + @SearchText + '%'
            OR m.cprocessdescription LIKE '%' + @SearchText + '%')";
                }

                if (!string.IsNullOrWhiteSpace(created_by))
                {
                    query += " AND m.ccreated_by = @CreatedBy";
                }

                if (!string.IsNullOrWhiteSpace(priority))
                {
                    query += " AND m.cpriority_label = @Priority";
                }

                if (status.HasValue && status.Value > 0)
                {
                    query += " AND m.cstatus = @Status";
                }

                query += @"
        GROUP BY 
            m.ID, m.ctenant_id, m.cprocessdescription, m.cprocesscode, m.cprocessname,
            m.cprivilege_type, p.cprocess_privilege, m.cvalue, m.cpriority_label, m.nshow_timeline,
            m.cnotification_type, m.cstatus, ISNULL(u1.cfirst_name,'') + ' ' + ISNULL(u1.clast_name,''),
            m.lcreated_date,
            ISNULL(u2.cfirst_name,'') + ' ' + ISNULL(u2.clast_name,''),
            m.lmodified_date, m.cmeta_id,
            n.notification_type, s.cstatus_description, meta.meta_Name, meta.meta_Description
        ORDER BY m.ID DESC";
                query += $@"
        OFFSET {skip} ROWS
        FETCH NEXT {pageSize} ROWS ONLY;";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                if (!string.IsNullOrWhiteSpace(searchText))
                {
                    cmd.Parameters.AddWithValue("@SearchText", searchText);
                }
                if (!string.IsNullOrWhiteSpace(created_by))
                {
                    cmd.Parameters.AddWithValue("@CreatedBy", created_by);
                }
                if (!string.IsNullOrWhiteSpace(priority))
                {
                    cmd.Parameters.AddWithValue("@Priority", priority);
                }
                if (status.HasValue && status.Value > 0)
                {
                    cmd.Parameters.AddWithValue("@Status", status.Value);
                }

                using var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new GetProcessEngineDTO
                    {
                        ID = reader.GetInt32(reader.GetOrdinal("ID")),
                        cprocesscode = reader.SafeGetString("cprocesscode"),
                        cprocessname = reader.SafeGetString("cprocessname"),
                        privilege_name = reader.SafeGetString("privilege_name"),
                        cprivilege_type = reader.SafeGetInt("cprivilege_type"),
                        cprocessType = reader.SafeGetString("cprocessname"),
                        cprocessdescription = reader.SafeGetString("cprocessdescription"),
                        cstatus = reader.SafeGetString("cstatus"),
                        cprocessvalueid = reader.SafeGetString("cprocessvalueid"),
                       // cvalue = reader.SafeGetString("cvalue_description"),
                        cpriority_label = reader.SafeGetString("cpriority_label"),
                        nshow_timeline = reader.SafeGetBoolean("nshow_timeline"),
                        cnotification_type = reader.SafeGetInt("cnotification_type"),
                        cmeta_id = reader.SafeGetInt("cmeta_id"),
                        created_by = reader.SafeGetString("created_by_name"),
                        ccreated_date = reader.SafeGetDateTime("lcreated_date"),
                        modified_by = reader.SafeGetString("modified_by_name"),
                        lmodified_date = reader.SafeGetDateTime("lmodified_date"),
                        cstatus_description = reader.SafeGetString("cstatus_description"),
                        processEngineChildItems = reader.SafeGetInt("DetailCount")
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error fetching process engine list", ex);
            }

            return result;
        }





    }
}

