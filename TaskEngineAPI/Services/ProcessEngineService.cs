using System.Data.SqlClient;
using System.Net.NetworkInformation;
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
            SELECT ctype FROM [dbo].[tbl_process_engine_type] WHERE nis_active = 1 AND ctenant_Id = @TenantID";


                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new ProcessEngineTypeDTO
                            {

                                ctype = reader.GetString(reader.GetOrdinal("ctype"))
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

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    {
                        string queryMaster = @"
                INSERT INTO tbl_process_engine_master (
                    ctenentid, ciseqno, cprocesscode, cprocessname, ctype, cstatus, 
                    cuser_id, cuser_name, crole_code, crole_name, cposition_code, 
                    cposition_title, cdepartment_code, cdepartment_name, 
                    ccreated_date, ccreated_by, cmodified_by, lmodified_date
                ) VALUES (
                    @TenantID, @ciseqno, @cprocesscode, @cprocessname, @ctype, @cstatus, 
                    @cuser_id, @cuser_name, @crole_code, @crole_name, @cposition_code, 
                    @cposition_title, @cdepartment_code, @cdepartment_name, 
                    @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date
                );
                SELECT SCOPE_IDENTITY();";

                        int masterId;
                        using (SqlCommand cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                            cmd.Parameters.AddWithValue("@ciseqno", (object?)model.ciseqno ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocesscode", (object?)model.cprocesscode ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cprocessname", (object?)model.cprocessname ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ctype", (object?)model.ctype ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", (object?)model.cstatus ?? DBNull.Value);   // ✅ fixed
                            cmd.Parameters.AddWithValue("@cuser_id", (object?)model.cuser_id ?? DBNull.Value); // ✅ fixed
                            cmd.Parameters.AddWithValue("@cuser_name", (object?)model.cuser_name ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@crole_code", (object?)model.crole_code ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@crole_name", (object?)model.crole_name ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cposition_code", (object?)model.cposition_code ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cposition_title", (object?)model.cposition_title ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cdepartment_code", (object?)model.cdepartment_code ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cdepartment_name", (object?)model.cdepartment_name ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ccreated_by", username);
                            cmd.Parameters.AddWithValue("@cmodified_by", username);
                            cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }

                     
                        string queryDetail = @"
                INSERT INTO tbl_process_engine_details (
                    ctenentid, ciseqno, cprocesscode, cseq_order, cactivitycode, cactivitydescription, 
                    ctasktype, cprevstep, cactivityname, cnextseqno, 
                    ccreated_date, ccreated_by, cmodified_by, lmodified_date,cassignee,cprocess_type
                ) VALUES (
                    @TenantID, @ciseqno, @cprocesscode, @cseq_order, @cactivitycode, @cactivitydescription, 
                    @ctasktype, @cprevstep, @cactivityname, @cnextseqno, 
                    @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date,@cassignee,@cprocess_type
                );";

                        foreach (var detail in model.ProcessEngineChildItems)
                        {
                            using (SqlCommand cmdDetail = new SqlCommand(queryDetail, conn, transaction))
                            {
                                cmdDetail.Parameters.AddWithValue("@TenantID", cTenantID);
                                cmdDetail.Parameters.AddWithValue("@cprocesscode", detail.cprocesscode);
                                cmdDetail.Parameters.AddWithValue("@ciseqno", masterId);
                                cmdDetail.Parameters.AddWithValue("@cactivitycode", detail.cactivitycode ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactivitydescription", detail.cactivitydescription ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@ctasktype", detail.ctasktype ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cprevstep", detail.cprevstep ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cactivityname", detail.cactivityname ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cnextseqno", detail.cnextseqno ?? (object)DBNull.Value);
                                cmdDetail.Parameters.AddWithValue("@cseq_order", detail.cseq_order);
                                cmdDetail.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                                cmdDetail.Parameters.AddWithValue("@ccreated_by", username);
                                cmdDetail.Parameters.AddWithValue("@cmodified_by", username);
                                cmdDetail.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                                cmdDetail.Parameters.AddWithValue("@cassignee", detail.cassignee);
                                cmdDetail.Parameters.AddWithValue("@cprocess_type", detail.cprocess_type);
                                await cmdDetail.ExecuteNonQueryAsync();
                            }

                         
                            string queryCondition = @"
                    INSERT INTO tbl_process_engine_condition (
                        ctenentid, cprocesscode, ciseqno, cseq_order, icondseqno, ctype, 
                        clabel, cfieldvalue, ccondition, remarks1, remarks2, remarks3, 
                        ccreated_date, ccreated_by, cmodified_by, lmodified_date
                    ) VALUES (
                        @TenantID, @cprocesscode, @ciseqno, @cseq_order, @icondseqno, @ctype, 
                        @clabel, @cfieldvalue, @ccondition, @remarks1, @remarks2, @remarks3,
                        @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date
                    );";

                            foreach (var cond in detail.ProcessEngineConditionDetails)
                            {
                                using (SqlCommand cmdCond = new SqlCommand(queryCondition, conn, transaction))
                                {
                                    cmdCond.Parameters.AddWithValue("@TenantID", cTenantID);
                                    cmdCond.Parameters.AddWithValue("@cprocesscode", cond.cprocesscode);
                                    cmdCond.Parameters.AddWithValue("@ciseqno", masterId);
                                    cmdCond.Parameters.AddWithValue("@cseq_order", cond.cseq_order);
                                    cmdCond.Parameters.AddWithValue("@icondseqno", cond.icondseqno);
                                    cmdCond.Parameters.AddWithValue("@ctype", cond.ctype ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@clabel", cond.clabel ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@cfieldvalue", cond.cfieldvalue ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@ccondition", cond.ccondition ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@remarks1", cond.remarks1 ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@remarks2", cond.remarks2 ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@remarks3", cond.remarks3 ?? (object)DBNull.Value);
                                    cmdCond.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                                    cmdCond.Parameters.AddWithValue("@ccreated_by", username);
                                    cmdCond.Parameters.AddWithValue("@cmodified_by", username);
                                    cmdCond.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                                    await cmdCond.ExecuteNonQueryAsync();
                                }
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
                    m.cseq_id, m.ctenentid, m.ciseqno, m.cprocesscode, m.cprocessname, m.ctype, m.cstatus,
                    m.cuser_id, m.cuser_name, m.crole_code, m.crole_name, m.cposition_code, m.cposition_title,
                    m.cdepartment_code, m.cdepartment_name, m.ccreated_by, m.ccreated_date, m.cmodified_by, m.lmodified_date,
                    d.cactivitycode, d.cactivitydescription, d.ctasktype, d.cprevstep, d.cactivityname, d.cnextseqno, d.cseq_order,
                    c.icondseqno, c.cseq_order AS cond_seq_order, c.ctype AS cond_type, c.clabel, c.cfieldvalue, c.ccondition,
                    c.remarks1, c.remarks2, c.remarks3
                FROM tbl_process_engine_master m
                LEFT JOIN tbl_process_engine_details d 
                    ON m.cprocesscode = d.cprocesscode AND  m.cseq_id = d.ciseqno
                LEFT JOIN tbl_process_engine_condition c 
                    ON  d.ciseqno = c.ciseqno 
                WHERE m.ctenentid = @TenantID
                ORDER BY m.cseq_id, d.cseq_order, c.icondseqno";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int cseq_id = reader.GetInt32(reader.GetOrdinal("cseq_id"));

                    if (!result.TryGetValue(cseq_id, out var engine))
                    {
                        engine = new GetProcessEngineDTO
                        {
                            cseq_id = cseq_id,
                            ciseqno = reader.SafeGetString("ciseqno"),
                            cprocesscode = reader.SafeGetString("cprocesscode"),
                            cprocessname = reader.SafeGetString("cprocessname"),
                            ctype = reader.SafeGetString("ctype"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cuser_id = reader.SafeGetString("cuser_id"),
                            cuser_name = reader.SafeGetString("cuser_name"),
                            crole_code = reader.SafeGetString("crole_code"),
                            crole_name = reader.SafeGetString("crole_name"),
                            cposition_code = reader.SafeGetString("cposition_code"),
                            cposition_title = reader.SafeGetString("cposition_title"),
                            cdepartment_code = reader.SafeGetString("cdepartment_code"),
                            cdepartment_name = reader.SafeGetString("cdepartment_name"),
                            ccreated_by = reader.SafeGetString("ccreated_by"),
                            ccreated_date = reader.SafeGetDateTime("ccreated_date"),
                            cmodified_by = reader.SafeGetString("cmodified_by"),
                            lmodified_date = reader.SafeGetDateTime("lmodified_date"),
                            ProcessEngineChildItems = new List<ProcessEngineChildItems>()
                        };
                        result[cseq_id] = engine;
                    }

                    string activityCode = reader.SafeGetString("cactivitycode");
                    if (!string.IsNullOrEmpty(activityCode))
                    {
                        var child = engine.ProcessEngineChildItems.FirstOrDefault(x => x.cactivitycode == activityCode);
                        if (child == null)
                        {
                            child = new ProcessEngineChildItems
                            {
                                cactivitycode = activityCode,
                                cactivitydescription = reader.SafeGetString("cactivitydescription"),
                                ctasktype = reader.SafeGetString("ctasktype"),
                                cprevstep = reader.SafeGetString("cprevstep"),
                                cactivityname = reader.SafeGetString("cactivityname"),
                                cnextseqno = reader.SafeGetString("cnextseqno"),
                                cseq_order = reader.SafeGetString("cseq_order"),
                                ProcessEngineConditionDetails = new List<ProcessEngineConditionDetails>()
                            };
                            engine.ProcessEngineChildItems.Add(child);
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("icondseqno")))
                        {
                            child.ProcessEngineConditionDetails.Add(new ProcessEngineConditionDetails
                            {
                                cprocesscode = reader.SafeGetString("cprocesscode"),
                                ciseqno = reader.SafeGetInt("ciseqno"),
                                icondseqno = reader.SafeGetInt("icondseqno"),
                                cseq_order = reader.SafeGetInt("cond_seq_order"),
                                ctype = reader.SafeGetString("cond_type"),
                                clabel = reader.SafeGetString("clabel"),
                                cfieldvalue = reader.SafeGetString("cfieldvalue"),
                                ccondition = reader.SafeGetString("ccondition"),
                                remarks1 = reader.SafeGetString("remarks1"),
                                remarks2 = reader.SafeGetString("remarks2"),
                                remarks3 = reader.SafeGetString("remarks3")
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
                    m.cseq_id, m.ctenentid, m.ciseqno, m.cprocesscode, m.cprocessname, m.ctype, m.cstatus,
                    m.cuser_id, m.cuser_name, m.crole_code, m.crole_name, m.cposition_code, m.cposition_title,
                    m.cdepartment_code, m.cdepartment_name, m.ccreated_by, m.ccreated_date, m.cmodified_by, m.lmodified_date,
                    d.cactivitycode, d.cactivitydescription, d.ctasktype, d.cprevstep, d.cactivityname, d.cnextseqno, d.cseq_order,
                    c.icondseqno, c.cseq_order AS cond_seq_order, c.ctype AS cond_type, c.clabel, c.cfieldvalue, c.ccondition,
                    c.remarks1, c.remarks2, c.remarks3
                FROM tbl_process_engine_master m
                LEFT JOIN tbl_process_engine_details d 
                    ON m.cprocesscode = d.cprocesscode AND  m.cseq_id = d.ciseqno
                LEFT JOIN tbl_process_engine_condition c 
                    ON  d.ciseqno = c.ciseqno 
                WHERE m.ctenentid = @TenantID and m.cseq_id=@id
                ORDER BY m.cseq_id, d.cseq_order, c.icondseqno";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                cmd.Parameters.AddWithValue("@id", id);
                using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    int cseq_id = reader.GetInt32(reader.GetOrdinal("cseq_id"));

                    if (!result.TryGetValue(cseq_id, out var engine))
                    {
                        engine = new GetProcessEngineDTO
                        {
                            cseq_id = cseq_id,
                            ciseqno = reader.SafeGetString("ciseqno"),
                            cprocesscode = reader.SafeGetString("cprocesscode"),
                            cprocessname = reader.SafeGetString("cprocessname"),
                            ctype = reader.SafeGetString("ctype"),
                            cstatus = reader.SafeGetString("cstatus"),
                            cuser_id = reader.SafeGetString("cuser_id"),
                            cuser_name = reader.SafeGetString("cuser_name"),
                            crole_code = reader.SafeGetString("crole_code"),
                            crole_name = reader.SafeGetString("crole_name"),
                            cposition_code = reader.SafeGetString("cposition_code"),
                            cposition_title = reader.SafeGetString("cposition_title"),
                            cdepartment_code = reader.SafeGetString("cdepartment_code"),
                            cdepartment_name = reader.SafeGetString("cdepartment_name"),
                            ccreated_by = reader.SafeGetString("ccreated_by"),
                            ccreated_date = reader.SafeGetDateTime("ccreated_date"),
                            cmodified_by = reader.SafeGetString("cmodified_by"),
                            lmodified_date = reader.SafeGetDateTime("lmodified_date"),
                            ProcessEngineChildItems = new List<ProcessEngineChildItems>()
                        };
                        result[cseq_id] = engine;
                    }

                    string activityCode = reader.SafeGetString("cactivitycode");
                    if (!string.IsNullOrEmpty(activityCode))
                    {
                        var child = engine.ProcessEngineChildItems.FirstOrDefault(x => x.cactivitycode == activityCode);
                        if (child == null)
                        {
                            child = new ProcessEngineChildItems
                            {
                                cactivitycode = activityCode,
                                cactivitydescription = reader.SafeGetString("cactivitydescription"),
                                ctasktype = reader.SafeGetString("ctasktype"),
                                cprevstep = reader.SafeGetString("cprevstep"),
                                cactivityname = reader.SafeGetString("cactivityname"),
                                cnextseqno = reader.SafeGetString("cnextseqno"),
                                cseq_order = reader.SafeGetString("cseq_order"),
                                ProcessEngineConditionDetails = new List<ProcessEngineConditionDetails>()
                            };
                            engine.ProcessEngineChildItems.Add(child);
                        }

                        if (!reader.IsDBNull(reader.GetOrdinal("icondseqno")))
                        {
                            child.ProcessEngineConditionDetails.Add(new ProcessEngineConditionDetails
                            {
                                cprocesscode = reader.SafeGetString("cprocesscode"),
                                ciseqno = reader.SafeGetInt("ciseqno"),
                                icondseqno = reader.SafeGetInt("icondseqno"),
                                cseq_order = reader.SafeGetInt("cond_seq_order"),
                                ctype = reader.SafeGetString("cond_type"),
                                clabel = reader.SafeGetString("clabel"),
                                cfieldvalue = reader.SafeGetString("cfieldvalue"),
                                ccondition = reader.SafeGetString("ccondition"),
                                remarks1 = reader.SafeGetString("remarks1"),
                                remarks2 = reader.SafeGetString("remarks2"),
                                remarks3 = reader.SafeGetString("remarks3")
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





    }

}

