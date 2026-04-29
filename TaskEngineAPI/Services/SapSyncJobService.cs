using Newtonsoft.Json;
using Serilog;
using System.Data;
using System.Text;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;
using Microsoft.Data.SqlClient;
namespace TaskEngineAPI.Services
{
    public class SapSyncJobService : ISapSyncJobService
    {

        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;

        public SapSyncJobService(HttpClient httpClient, IConfiguration config)
        {
            _httpClient = httpClient;
            _config = config;
        }

        public async Task<bool> SyncEmployeesAsync(int tenantId)
        {
            try
            {
                string apiUrl = "https://misdevapi.sheenlac.com/api/Progovex/GetAllEmployeeDtls";

                var requestBody = new
                {
                    EMPLOYEE_ID = "",
                    COMPANY_CODE = ""
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync(apiUrl, content);

                if (!response.IsSuccessStatusCode)
                    throw new Exception($"API failed: {response.StatusCode}");

                var json = await response.Content.ReadAsStringAsync();

                var employees = JsonConvert.DeserializeObject<List<SapSyncEmployeeResponse>>(json);

                if (employees == null || !employees.Any())
                    return false;


                Guid batchId = Guid.NewGuid();


                DataTable dt = new DataTable();

                // (Your columns remain same)
                dt.Columns.Add("TenantId", typeof(int));
                dt.Columns.Add("cuserid", typeof(long));
                dt.Columns.Add("cuser_name", typeof(string));
                dt.Columns.Add("cemail", typeof(string));
                dt.Columns.Add("cpassword", typeof(string));
                dt.Columns.Add("nIs_active", typeof(bool));
                dt.Columns.Add("cfirst_name", typeof(string));
                dt.Columns.Add("clast_name", typeof(string));
                dt.Columns.Add("cphoneno", typeof(string));
                dt.Columns.Add("calternate_phone", typeof(string));
                dt.Columns.Add("ldob", typeof(DateTime));
                dt.Columns.Add("cmarital_status", typeof(string));
                dt.Columns.Add("cnation", typeof(string));
                dt.Columns.Add("cgender", typeof(string));
                dt.Columns.Add("caddress", typeof(string));
                dt.Columns.Add("caddress1", typeof(string));
                dt.Columns.Add("caddress2", typeof(string));
                dt.Columns.Add("cpincode", typeof(string));
                dt.Columns.Add("ccity", typeof(string));
                dt.Columns.Add("cstate_code", typeof(string));
                dt.Columns.Add("cstate_desc", typeof(string));
                dt.Columns.Add("ccountry_code", typeof(string));
                dt.Columns.Add("cbank_name", typeof(string));
                dt.Columns.Add("caccount_number", typeof(string));
                dt.Columns.Add("ciFSC_code", typeof(string));
                dt.Columns.Add("cpan", typeof(string));
                dt.Columns.Add("ldoj", typeof(DateTime));
                dt.Columns.Add("cemployment_status", typeof(string));
                dt.Columns.Add("nnotice_period_days", typeof(int));
                dt.Columns.Add("cemp_category", typeof(string));
                dt.Columns.Add("cdept_code", typeof(string));
                dt.Columns.Add("cdept_desc", typeof(string));
                dt.Columns.Add("cjob_code", typeof(string));
                dt.Columns.Add("cjob_desc", typeof(string));
                dt.Columns.Add("creport_mgr_code", typeof(string));
                dt.Columns.Add("creport_mgr_name", typeof(string));
                dt.Columns.Add("croll_id", typeof(string));
                dt.Columns.Add("croll_name", typeof(string));
                dt.Columns.Add("croll_id_mngr", typeof(string));
                dt.Columns.Add("croll_id_mngr_desc", typeof(string));
                dt.Columns.Add("creport_manager_empcode", typeof(string));
                dt.Columns.Add("creport_manager_poscode", typeof(string));
                dt.Columns.Add("creport_manager_pos_desc", typeof(string));
                dt.Columns.Add("cposition_code", typeof(string));
                dt.Columns.Add("cposition_name", typeof(string));
                dt.Columns.Add("IsActive", typeof(bool));
                dt.Columns.Add("BatchId", typeof(Guid));
                dt.Columns.Add("SyncDate", typeof(DateTime));


                foreach (var emp in employees)
                {
                    long empId;
                    object empIdValue = long.TryParse(emp.EMPLOYEE_ID, out empId) ? empId : (object)DBNull.Value;

                    dt.Rows.Add(
                        tenantId,
                        empIdValue,
                        emp.EMPLOYEE_NAME ?? (object)DBNull.Value,
                        emp.EMAIL_ADDRESS ?? (object)DBNull.Value,
                        "",
                        emp.EMPLOYEE_STATUS == "Active",
                        emp.EMPLOYEE_FIRST_NAME ?? (object)DBNull.Value,
                        emp.EMPLOYEE_LAST_NAME ?? (object)DBNull.Value,
                        emp.PHONE_NUMBER ?? (object)DBNull.Value,
                        emp.ALTERNATIVE_NUMBER ?? (object)DBNull.Value,
                        ConvertToDate(emp.DATE_OF_BIRTH),
                        emp.MARITAL_STATUS ?? (object)DBNull.Value,
                        emp.NATIONALITY ?? (object)DBNull.Value,
                        emp.GENDER ?? (object)DBNull.Value,
                        emp.ADDRESS ?? (object)DBNull.Value,
                        emp.ADDRESS_LINE1 ?? (object)DBNull.Value,
                        emp.ADDRESS_LINE2 ?? (object)DBNull.Value,
                        emp.PIN_CODE ?? (object)DBNull.Value,
                        emp.CITY ?? (object)DBNull.Value,
                        emp.STATE_CODE ?? (object)DBNull.Value,
                        emp.STATE ?? (object)DBNull.Value,
                        (object)DBNull.Value,
                        emp.BANK_NAME ?? (object)DBNull.Value,
                        emp.ACCOUNT_NUMBER ?? (object)DBNull.Value,
                        emp.IFSC_CODE ?? (object)DBNull.Value,
                        emp.PAN ?? (object)DBNull.Value,
                        ConvertToDate(emp.DATE_OF_JOINING),
                        emp.EMPLOYEE_STATUS ?? (object)DBNull.Value,
                        ExtractDays(emp.NOTICE_PERIOD),
                        emp.EMPLOYEE_CATEGORY ?? (object)DBNull.Value,
                        emp.DEPARTMENT_CODE ?? (object)DBNull.Value,
                        emp.DEPARTMENT ?? (object)DBNull.Value,
                        (object)DBNull.Value,
                        (object)DBNull.Value,
                        emp.REPORTING_MANAGER_CODE ?? (object)DBNull.Value,
                        emp.REPORTING_MANAGER ?? (object)DBNull.Value,
                        (object)DBNull.Value,
                        (object)DBNull.Value,
                        (object)DBNull.Value,
                        (object)DBNull.Value,
                        emp.REPORTING_MANAGER_CODE ?? (object)DBNull.Value,
                        emp.MANAGER_POS_ID ?? (object)DBNull.Value,
                        emp.MANAGER_POS_DES ?? (object)DBNull.Value,
                        emp.EMPLOYEE_POSITION_CODE ?? (object)DBNull.Value,
                        emp.POSITION ?? (object)DBNull.Value,
                        emp.EMPLOYEE_STATUS == "Active",
                        batchId,
                        DateTime.Now
                    );
                }


                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await conn.OpenAsync();

                    // ✅ STEP 1: BULK INSERT + COMMIT
                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {

                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, tran))
                            {
                                bulkCopy.DestinationTableName = "dbo.Users_Staging";



                                foreach (DataColumn col in dt.Columns)
                                {



                                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                }



                                await bulkCopy.WriteToServerAsync(dt);
                            }

                            tran.Commit(); // ✅ FIXED
                        }
                        catch
                        {
                            tran.Rollback();

                            throw;
                        }
                    }

                    // ✅ STEP 2: CALL SP AFTER COMMIT
                    using (SqlCommand cmd = new SqlCommand("SP_Sync_Users", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add("@BatchId", SqlDbType.UniqueIdentifier).Value = batchId;
                        cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                        await cmd.ExecuteNonQueryAsync();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception("SAP Sync Error: " + ex.Message);

            }
        }



        private object ConvertToDate(string date)
        {
            if (DateTime.TryParse(date, out DateTime result))
                return result;

            return DBNull.Value;
        }

        private int ExtractDays(string notice)
        {
            if (string.IsNullOrEmpty(notice)) return 0;

            var num = new string(notice.Where(char.IsDigit).ToArray());

            return int.TryParse(num, out int days) ? days : 0;
        }


    }
}

