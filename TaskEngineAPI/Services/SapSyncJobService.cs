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
       
        public async Task SyncEmployeesAsync(int tenantId)
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

                var jsonResponse = await response.Content.ReadAsStringAsync();

                var employees = JsonConvert.DeserializeObject<List<SapEmployeeResponse>>(jsonResponse);

                if (employees == null || !employees.Any())
                    throw new Exception("No employees received");

                // ✅ ONE BatchId for entire sync
                Guid batchId = Guid.NewGuid();

                // ✅ Create DataTable
                DataTable dt = new DataTable();
                dt.Columns.Add("TenantId", typeof(int));
                dt.Columns.Add("EmployeeCode", typeof(string));
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Email", typeof(string));
                dt.Columns.Add("ManagerCode", typeof(string));
                dt.Columns.Add("ManagerName", typeof(string));
                dt.Columns.Add("IsActive", typeof(bool));
                dt.Columns.Add("BatchId", typeof(Guid));
                dt.Columns.Add("SyncDate", typeof(DateTime));

                // ✅ Fill DataTable
                foreach (var emp in employees)
                {
                    dt.Rows.Add(
                        tenantId,
                        emp.EMPLOYEE_ID ?? (object)DBNull.Value,
                        emp.EMPLOYEE_NAME ?? (object)DBNull.Value,
                        emp.EMAIL_ADDRESS ?? (object)DBNull.Value,
                        emp.REPORTING_MANAGER_CODE ?? (object)DBNull.Value,
                        emp.REPORTING_MANAGER ?? (object)DBNull.Value,
                        emp.EMPLOYEE_STATUS == "Active",
                        batchId,
                        DateTime.Now
                    );
                }

                // ✅ DB Operations
                using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    await conn.OpenAsync();

                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {
                            // ✅ BULK INSERT
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, tran))
                            {
                                bulkCopy.DestinationTableName = "Users_Staging";
                                bulkCopy.BatchSize = 5000;
                                bulkCopy.BulkCopyTimeout = 120;

                                bulkCopy.ColumnMappings.Add("TenantId", "TenantId");
                                bulkCopy.ColumnMappings.Add("EmployeeCode", "EmployeeCode");
                                bulkCopy.ColumnMappings.Add("Name", "Name");
                                bulkCopy.ColumnMappings.Add("Email", "Email");
                                bulkCopy.ColumnMappings.Add("ManagerCode", "ManagerCode");
                                bulkCopy.ColumnMappings.Add("ManagerName", "ManagerName");
                                bulkCopy.ColumnMappings.Add("IsActive", "IsActive");
                                bulkCopy.ColumnMappings.Add("BatchId", "BatchId");
                                bulkCopy.ColumnMappings.Add("SyncDate", "SyncDate");

                                await bulkCopy.WriteToServerAsync(dt);
                            }

                            // ✅ CALL STORED PROCEDURE
                            using (SqlCommand cmd = new SqlCommand("SP_Sync_Users", conn, tran))
                            {
                                cmd.CommandType = CommandType.StoredProcedure;
                                cmd.CommandTimeout = 120;

                                cmd.Parameters.Add("@BatchId", SqlDbType.UniqueIdentifier).Value = batchId;
                                cmd.Parameters.Add("@TenantId", SqlDbType.Int).Value = tenantId;

                                await cmd.ExecuteNonQueryAsync();
                            }

                            // ✅ COMMIT
                            tran.Commit();

                            Log.Information("Employee sync completed successfully. BatchId: {BatchId}", batchId);
                        }
                        catch (Exception ex)
                        {
                            tran.Rollback();
                            Log.Error(ex, "Transaction failed. BatchId: {BatchId}", batchId);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error inserting SAP employee");
                throw;
            }
        }
    }
}

