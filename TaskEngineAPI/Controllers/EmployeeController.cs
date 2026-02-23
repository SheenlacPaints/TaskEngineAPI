using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Net.Http;
using System.Text;
using TaskEngineAPI.DTO;
using static System.Net.WebRequestMethods;

namespace TaskEngineAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmployeeController : ControllerBase
    {
        private readonly IConfiguration Configuration;
        private readonly HttpClient _httpClient;

        public EmployeeController(IConfiguration _configuration, HttpClient httpClient)
        {
            Configuration = _configuration;
            _httpClient = httpClient;
        }

        [HttpGet("GetEmployeesALLData")]
        public ActionResult<IEnumerable<EmployeeDTO>> GetEmployeesALLData()
        {
            try
            {
                List<EmployeeDTO> empObj = new List<EmployeeDTO>();

                var connectionString = Configuration.GetConnectionString("DDatabase");

                if (string.IsNullOrEmpty(connectionString))
                {
                    return StatusCode(500, "Database connection string is not configured");
                }

                string query = @"
SELECT 
    cempno, cempname, ldoj, cempcategory, cworkloccode, cworklocname, 
    crolecode, crolename, cgradecode, cgradedesc, csubrolecode, 
    cdeptcode, cdeptdesc, ldob, cnation, cgender, caddress, caddress1, 
    caddress2, cpincode, cstatecode, cstatedesc, ccountrycode, 
    cmailid, cphoneno, cjobcode, cjobdesc, creportmgrcode, 
    creportmgrname, Roll_id, Roll_name, Roll_Id_mngr, Roll_Id_mngr_desc, 
    ReportManager_empcode, ReportManager_Poscode, ReportManager_Posdesc 
FROM Hrm_cempmas";

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        con.Open();

                        using (SqlDataReader sdr = cmd.ExecuteReader())
                        {
                            while (sdr.Read())
                            {
                                string fullName = Convert.ToString(sdr["cempname"]);
                                string firstName = "";
                                string lastName = "";

                                if (!string.IsNullOrWhiteSpace(fullName))
                                {
                                    fullName = fullName.Trim();
                                    var nameParts = fullName.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                                    if (nameParts.Length == 1)
                                    {
                                        firstName = nameParts[0];
                                    }
                                    else if (nameParts.Length >= 2)
                                    {
                                        firstName = nameParts[0];
                                        lastName = string.Join(" ", nameParts.Skip(1));
                                    }
                                }

                                empObj.Add(new EmployeeDTO
                                {

                                    id = "",
                                    cuserid = sdr["cempno"] != DBNull.Value ? Convert.ToString(sdr["cempno"]) : "",
                                    ctenantID = "",
                                    cusername = sdr["cempno"] != DBNull.Value ? Convert.ToString(sdr["cempno"]) : "",
                                    cemail = sdr["cmailid"] != DBNull.Value ? Convert.ToString(sdr["cmailid"]) : "",
                                    cpassword = "",
                                    nIsActive = true,


                                    cfirstName = firstName,
                                    clastName = lastName,
                                    cphoneno = sdr["cphoneno"] != DBNull.Value ? Convert.ToString(sdr["cphoneno"]) : "",
                                    cAlternatePhone = "",
                                    ldob = sdr["ldob"] != DBNull.Value ? Convert.ToDateTime(sdr["ldob"]).ToString("yyyy-MM-dd") : "",
                                    cMaritalStatus = "",
                                    cnation = sdr["cnation"] != DBNull.Value ? Convert.ToString(sdr["cnation"]) : "",
                                    cgender = sdr["cgender"] != DBNull.Value ? Convert.ToString(sdr["cgender"]) : "",


                                    caddress = sdr["caddress"] != DBNull.Value ? Convert.ToString(sdr["caddress"]) : "",
                                    caddress1 = sdr["caddress1"] != DBNull.Value ? Convert.ToString(sdr["caddress1"]) : "",
                                    caddress2 = sdr["caddress2"] != DBNull.Value ? Convert.ToString(sdr["caddress2"]) : "",
                                    cpincode = sdr["cpincode"] != DBNull.Value ? Convert.ToString(sdr["cpincode"]) : "",
                                    ccity = "",
                                    cstatecode = sdr["cstatecode"] != DBNull.Value ? Convert.ToString(sdr["cstatecode"]) : "",
                                    cstatedesc = sdr["cstatedesc"] != DBNull.Value ? Convert.ToString(sdr["cstatedesc"]) : "",
                                    ccountrycode = sdr["ccountrycode"] != DBNull.Value ? Convert.ToString(sdr["ccountrycode"]) : "IN",
                                    ProfileImage = "",


                                    cbankName = "",
                                    caccountNumber = "",
                                    ciFSCCode = "",
                                    cpAN = "",


                                    ldoj = sdr["ldoj"] != DBNull.Value ? Convert.ToDateTime(sdr["ldoj"]).ToString("yyyy-MM-dd") : "",
                                    cemploymentStatus = "Active",
                                    nnoticePeriodDays = 30,
                                    lresignationDate = "",
                                    llastWorkingDate = "",
                                    cempcategory = sdr["cempcategory"] != DBNull.Value ? Convert.ToString(sdr["cempcategory"]) : "Full-Time",
                                    cworkloccode = sdr["cworkloccode"] != DBNull.Value ? Convert.ToString(sdr["cworkloccode"]) : "",
                                    cworklocname = sdr["cworklocname"] != DBNull.Value ? Convert.ToString(sdr["cworklocname"]) : "",


                                    croleID = sdr["Roll_id"] != DBNull.Value ? Convert.ToString(sdr["Roll_id"]) : "",
                                    crolecode = sdr["crolecode"] != DBNull.Value ? Convert.ToString(sdr["crolecode"]) : "",
                                    crolename = sdr["crolename"] != DBNull.Value ? Convert.ToString(sdr["crolename"]) : "",
                                    cgradecode = sdr["cgradecode"] != DBNull.Value ? Convert.ToString(sdr["cgradecode"]) : "",
                                    cgradedesc = sdr["cgradedesc"] != DBNull.Value ? Convert.ToString(sdr["cgradedesc"]) : "",
                                    csubrolecode = sdr["csubrolecode"] != DBNull.Value ? Convert.ToString(sdr["csubrolecode"]) : "",


                                    cdeptcode = sdr["cdeptcode"] != DBNull.Value ? Convert.ToString(sdr["cdeptcode"]) : "",
                                    cdeptdesc = sdr["cdeptdesc"] != DBNull.Value ? Convert.ToString(sdr["cdeptdesc"]) : "",


                                    cjobcode = sdr["cjobcode"] != DBNull.Value ? Convert.ToString(sdr["cjobcode"]) : "",
                                    cjobdesc = sdr["cjobdesc"] != DBNull.Value ? Convert.ToString(sdr["cjobdesc"]) : "",


                                    creportmgrcode = sdr["creportmgrcode"] != DBNull.Value ? Convert.ToString(sdr["creportmgrcode"]) : "",
                                    creportmgrname = sdr["creportmgrname"] != DBNull.Value ? Convert.ToString(sdr["creportmgrname"]) : "",

                                    cRoll_id = sdr["Roll_id"] != DBNull.Value ? Convert.ToString(sdr["Roll_id"]) : "",
                                    cRoll_name = sdr["Roll_name"] != DBNull.Value ? Convert.ToString(sdr["Roll_name"]) : "",
                                    cRoll_Id_mngr = sdr["Roll_Id_mngr"] != DBNull.Value ? Convert.ToString(sdr["Roll_Id_mngr"]) : "",
                                    cRoll_Id_mngr_desc = sdr["Roll_Id_mngr_desc"] != DBNull.Value ? Convert.ToString(sdr["Roll_Id_mngr_desc"]) : "",


                                    cReportManager_empcode = sdr["ReportManager_empcode"] != DBNull.Value ? Convert.ToString(sdr["ReportManager_empcode"]) : "",
                                    cReportManager_Poscode = sdr["ReportManager_Poscode"] != DBNull.Value ? Convert.ToString(sdr["ReportManager_Poscode"]) : "",
                                    cReportManager_Posdesc = sdr["ReportManager_Posdesc"] != DBNull.Value ? Convert.ToString(sdr["ReportManager_Posdesc"]) : "",


                                    nIsWebAccessEnabled = false,
                                    nIsEventRead = false,
                                    lLastLoginAt = "",
                                    nFailedLoginAttempts = 0,
                                    cPasswordChangedAt = "",
                                    nIsLocked = false,
                                    LastLoginIP = "",
                                    LastLoginDevice = "",


                                    ccreateddate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                                    ccreatedby = "System",
                                    cmodifiedby = "",
                                    lmodifieddate = "",


                                    nIsDeleted = false,
                                    cDeletedBy = "",
                                    lDeletedDate = ""
                                });
                            }
                        }
                    }
                }

                if (empObj == null || !empObj.Any())
                {
                    return NotFound(new { message = "No employees found in the database" });
                }

                return Ok(empObj);
            }
            catch (SqlException sqlEx)
            {
                return StatusCode(500, new { error = "Database error", details = sqlEx.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }

        [HttpPost("InsertEmployeesData")]
        public async Task<ActionResult> InsertEmployeesData()
        {
            void AddUserParameters(SqlCommand cmd, ExternalEmployeeDTO e)
            {
                cmd.Parameters.AddWithValue("@cuserid", e.cuserid ?? "");
                cmd.Parameters.AddWithValue("@ctenant_id", 1500);
                cmd.Parameters.AddWithValue("@cuser_name", e.cusername ?? "");
                cmd.Parameters.AddWithValue("@cemail", e.cemail ?? "");
                cmd.Parameters.AddWithValue("@cpassword", "DefaultPassword123!");
                cmd.Parameters.AddWithValue("@nIs_active", e.nIsActive ?? true);

                cmd.Parameters.AddWithValue("@cfirst_name", e.cfirstName ?? "");
                cmd.Parameters.AddWithValue("@clast_name", e.clastName ?? "");
                cmd.Parameters.AddWithValue("@cphoneno", e.cphoneno ?? "");
                cmd.Parameters.AddWithValue("@calternate_phone", e.calternate_phone ?? "");
                cmd.Parameters.AddWithValue("@ldob", DateTime.TryParse(e.ldob, out var dob) ? dob : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cmarital_status", e.cmarital_status ?? "Single");
                cmd.Parameters.AddWithValue("@cnation", e.cnation ?? "IN");
                cmd.Parameters.AddWithValue("@cgender", e.cgender ?? "");

                cmd.Parameters.AddWithValue("@caddress", e.caddress ?? "");
                cmd.Parameters.AddWithValue("@caddress1", e.caddress1 ?? "");
                cmd.Parameters.AddWithValue("@caddress2", e.caddress2 ?? "");
                cmd.Parameters.AddWithValue("@cpincode", e.cpincode ?? "");
                cmd.Parameters.AddWithValue("@ccity", e.ccity ?? "");
                cmd.Parameters.AddWithValue("@cstate_code", e.cstatecode ?? "");
                cmd.Parameters.AddWithValue("@cstate_desc", e.cstatedesc ?? "");
                cmd.Parameters.AddWithValue("@ccountry_code", e.ccountrycode ?? "IN");

                cmd.Parameters.AddWithValue("@cbank_name", e.cbank_name ?? "");
                cmd.Parameters.AddWithValue("@caccount_number", e.caccount_number ?? "");
                cmd.Parameters.AddWithValue("@ciFSC_code", e.ciFSC_code ?? "");
                cmd.Parameters.AddWithValue("@cpan", e.cpan ?? "");

                cmd.Parameters.AddWithValue("@ldoj", DateTime.TryParse(e.ldoj, out var doj) ? doj : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cemployment_status", e.cemployment_status ?? "");
                cmd.Parameters.AddWithValue("@nnotice_period_days", e.nnotice_period_days ?? 0);
                cmd.Parameters.AddWithValue("@lresignation_date", DateTime.TryParse(e.lresignation_date, out var resign) ? resign : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@llast_working_date", DateTime.TryParse(e.llast_working_date, out var lastWork) ? lastWork : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@cemp_category", e.cempcategory ?? "Full-Time");
                cmd.Parameters.AddWithValue("@cwork_loc_code", e.cworkloccode ?? "");
                cmd.Parameters.AddWithValue("@cwork_loc_name", e.cworklocname ?? "");

                cmd.Parameters.AddWithValue("@crole_id", e.croleID ?? "");
                cmd.Parameters.AddWithValue("@crole_code", e.crolecode ?? "");
                cmd.Parameters.AddWithValue("@crole_name", e.crolename ?? "");
                cmd.Parameters.AddWithValue("@cgrade_code", e.cgradecode ?? "");
                cmd.Parameters.AddWithValue("@cgrade_desc", e.cgradedesc ?? "");
                cmd.Parameters.AddWithValue("@csub_role_code", e.csubrolecode ?? "");
                cmd.Parameters.AddWithValue("@cdept_code", e.cdeptcode ?? "");
                cmd.Parameters.AddWithValue("@cdept_desc", e.cdeptdesc ?? "");
                cmd.Parameters.AddWithValue("@cjob_code", e.cjobcode ?? "");
                cmd.Parameters.AddWithValue("@cjob_desc", e.cjobdesc ?? "");

                cmd.Parameters.AddWithValue("@creport_mgr_code", e.creportmgrcode ?? "");
                cmd.Parameters.AddWithValue("@creport_mgr_name", e.creportmgrname ?? "");
                cmd.Parameters.AddWithValue("@croll_id", e.cRoll_id ?? "");
                cmd.Parameters.AddWithValue("@croll_name", e.cRoll_name ?? "");
                cmd.Parameters.AddWithValue("@croll_id_mngr", e.cRoll_Id_mngr ?? "");
                cmd.Parameters.AddWithValue("@croll_id_mngr_desc", e.cRoll_Id_mngr_desc ?? "");
                cmd.Parameters.AddWithValue("@creport_manager_empcode", e.cReportManager_empcode ?? "");
                cmd.Parameters.AddWithValue("@creport_manager_poscode", e.cReportManager_Poscode ?? "");
                cmd.Parameters.AddWithValue("@creport_manager_pos_desc", e.cReportManager_Posdesc ?? "");

                cmd.Parameters.AddWithValue("@nis_web_access_enabled", e.nIsWebAccessEnabled ?? true);
                cmd.Parameters.AddWithValue("@nis_event_read", e.nIsEventRead ?? true);
                cmd.Parameters.AddWithValue("@llast_login_at", DateTime.TryParse(e.llast_login_at, out var lastLogin) ? lastLogin : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@nfailed_logina_attempts", e.nfailed_logina_attempts ?? 0);
                cmd.Parameters.AddWithValue("@cpassword_changed_at", DateTime.TryParse(e.cpassword_changed_at, out var pwdChange) ? pwdChange : (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@nis_locked", e.nIsLocked ?? false);

                cmd.Parameters.AddWithValue("@ccreated_by", "System");
                cmd.Parameters.AddWithValue("@cposition_code", e.cposition_code ?? "");
                cmd.Parameters.AddWithValue("@cposition_name", e.cposition_name ?? "");
            }
            try
            {
                var externalApiUrl = "https://taskengineapi.sheenlac.com/Employee/GetEmployeesALLData";  /* https://misdevapi.sheenlac.com/api/Employee/GetEmployeesALLData */
                var response = await _httpClient.GetAsync(externalApiUrl);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, new { error = "Failed to fetch data from external API" });

                var content = await response.Content.ReadAsStringAsync();
                var externalEmployees = JsonConvert.DeserializeObject<List<ExternalEmployeeDTO>>(content);

                if (externalEmployees == null || !externalEmployees.Any())
                    return NotFound(new { message = "No employees found from external API" });

                var connectionString = Configuration.GetConnectionString("Database");
                if (string.IsNullOrEmpty(connectionString))
                    return StatusCode(500, "TaskEngine database connection string is not configured");

                int insertedCount = 0, errorCount = 0, skippedCount = 0;
                var errors = new List<string>();

                using (var con = new SqlConnection(connectionString))
                {
                    await con.OpenAsync();

                    var existingUserIds = new HashSet<string>();
                    string getExistingQuery = "SELECT cuserid FROM users WHERE cuserid IN ({0})";

                    var userIds = externalEmployees.Where(e => !string.IsNullOrEmpty(e.cuserid))
                                                 .Select(e => e.cuserid)
                                                 .Distinct()
                                                 .ToList();

                    if (userIds.Any())
                    {
                        var parameters = string.Join(",", userIds.Select((_, i) => $"@id{i}"));
                        using (var checkCmd = new SqlCommand(string.Format(getExistingQuery, parameters), con))
                        {
                            for (int i = 0; i < userIds.Count; i++)
                            {
                                checkCmd.Parameters.AddWithValue($"@id{i}", userIds[i]);
                            }

                            using (var reader = await checkCmd.ExecuteReaderAsync())
                            {
                                while (await reader.ReadAsync())
                                {
                                    existingUserIds.Add(reader["cuserid"].ToString());
                                }
                            }
                        }
                    }

                    var validEmployees = externalEmployees
                        .Where(e => !string.IsNullOrWhiteSpace(e.cuserid) &&
                                   !string.IsNullOrWhiteSpace(e.cphoneno) &&
                                   !string.IsNullOrWhiteSpace(e.cemail) &&
                                   !existingUserIds.Contains(e.cuserid))
                        .ToList();

                    skippedCount = externalEmployees.Count - validEmployees.Count;

                    for (int i = 0; i < validEmployees.Count; i += 100)
                    {
                        var batch = validEmployees.Skip(i).Take(100).ToList();

                        using (var transaction = con.BeginTransaction())
                        {
                            try
                            {
                                foreach (var employee in batch)
                                {
                                    try
                                    {
                                        string insertQuery = @"
                                    INSERT INTO users (
                                        cuserid, ctenant_id, cuser_name, cemail, cpassword, nIs_active,
                                        cfirst_name, clast_name, cphoneno, calternate_phone, ldob, 
                                        cmarital_status, cnation, cgender, caddress, caddress1, caddress2, 
                                        cpincode, ccity, cstate_code, cstate_desc, ccountry_code, 
                                        cbank_name, caccount_number, ciFSC_code, cpan, ldoj, 
                                        cemployment_status, nnotice_period_days, lresignation_date, 
                                        llast_working_date, cemp_category, cwork_loc_code, cwork_loc_name, 
                                        crole_id, crole_code, crole_name, cgrade_code, cgrade_desc, 
                                        csub_role_code, cdept_code, cdept_desc, cjob_code, cjob_desc, 
                                        creport_mgr_code, creport_mgr_name, croll_id, croll_name, 
                                        croll_id_mngr, croll_id_mngr_desc, creport_manager_empcode, 
                                        creport_manager_poscode, creport_manager_pos_desc,
                                        nis_web_access_enabled, nis_event_read, llast_login_at,
                                        nfailed_logina_attempts, cpassword_changed_at, nis_locked,
                                        ccreated_by, cposition_code, cposition_name
                                    ) VALUES (
                                        @cuserid, @ctenant_id, @cuser_name, @cemail, @cpassword, @nIs_active,
                                        @cfirst_name, @clast_name, @cphoneno, @calternate_phone, @ldob, 
                                        @cmarital_status, @cnation, @cgender, @caddress, @caddress1, @caddress2, 
                                        @cpincode, @ccity, @cstate_code, @cstate_desc, @ccountry_code, 
                                        @cbank_name, @caccount_number, @ciFSC_code, @cpan, @ldoj, 
                                        @cemployment_status, @nnotice_period_days, @lresignation_date, 
                                        @llast_working_date, @cemp_category, @cwork_loc_code, @cwork_loc_name, 
                                        @crole_id, @crole_code, @crole_name, @cgrade_code, @cgrade_desc, 
                                        @csub_role_code, @cdept_code, @cdept_desc, @cjob_code, @cjob_desc, 
                                        @creport_mgr_code, @creport_mgr_name, @croll_id, @croll_name, 
                                        @croll_id_mngr, @croll_id_mngr_desc, @creport_manager_empcode, 
                                        @creport_manager_poscode, @creport_manager_pos_desc,
                                        @nis_web_access_enabled, @nis_event_read, @llast_login_at,
                                        @nfailed_logina_attempts, @cpassword_changed_at, @nis_locked,
                                        @ccreated_by, @cposition_code, @cposition_name
                                    )";

                                        using (var cmd = new SqlCommand(insertQuery, con, transaction))
                                        {
                                            AddUserParameters(cmd, employee);
                                            await cmd.ExecuteNonQueryAsync();
                                            insertedCount++;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        errorCount++;
                                        errors.Add($"Error processing {employee.cuserid}: {ex.Message}");
                                    }
                                }

                                transaction.Commit();
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                errors.Add($"Batch transaction failed: {ex.Message}");
                            }
                        }
                    }
                }

                return Ok(new
                {
                    message = "Employee data processing completed",
                    totalRecords = externalEmployees.Count,
                    inserted = insertedCount,
                    skipped = skippedCount,
                    errors = errorCount,
                    errorDetails = errors.Take(10).ToList()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred", details = ex.Message });
            }
        }


       

     


    }
}
