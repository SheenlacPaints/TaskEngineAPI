using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Serilog;
using System.Data;
using System.Diagnostics;
using System.Text;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;
namespace TaskEngineAPI.Services
{
    public class SapSyncJobService : ISapSyncJobService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly string _targetConnectionString;  // ProcessEngineCustomDB
        private readonly string _sourceConnectionString;  // MISPORTAL
        private readonly string _sourceProcessEngineConnectionString;  // ProcessEngineProductionDB
        private readonly string _targetTaskEngineConnectionString;    // TASKENGINE
        private readonly ILogger<SapSyncJobService> _logger;

        public SapSyncJobService(HttpClient httpClient, IConfiguration config, ILogger<SapSyncJobService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _targetConnectionString = config.GetConnectionString("PDatabase");
            _sourceConnectionString = config.GetConnectionString("DDatabase");
            _sourceProcessEngineConnectionString = config.GetConnectionString("PPDatabase");
            _targetTaskEngineConnectionString = config.GetConnectionString("Database");
        }

        public async Task<bool> CheckIfSourceHasDataAsync(string tableName)
        {
            try
            {
                _logger.LogInformation($"🔍 Checking if {tableName} has data in MISPORTAL...");

                using var sourceConnection = new SqlConnection(_sourceConnectionString);
                await sourceConnection.OpenAsync();

                var query = $"SELECT COUNT(1) FROM {tableName}";
                using var command = new SqlCommand(query, sourceConnection);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());

                _logger.LogInformation($"📊 Table {tableName} has {count} records in MISPORTAL");

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error checking data in {tableName}: {ex.Message}");
                return false;
            }
        }

        public async Task<InBoundSyncResponseDTO> SyncTablesFromMISPORTALAsync(InBoundSyncRequestDTO request)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new InBoundSyncResponseDTO
            {
                StatusCode = 200,
                Timestamp = DateTime.Now,
                Errors = new List<string>(),
                Summary = new InBoundSyncSummaryDTO
                {
                    SkippedTables = new List<string>()
                }
            };

            _logger.LogInformation($"========== Starting InBound Sync from MISPORTAL at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            _logger.LogInformation($"Triggered by: {request.TriggeredBy}");

            using var targetConnection = new SqlConnection(_targetConnectionString);
            await targetConnection.OpenAsync();

            var transaction = targetConnection.BeginTransaction();

            try
            {
                if (request.SyncOrgUnit)
                {
                    _logger.LogInformation("📋 Processing tbl_mis_orgunit...");
                    var hasData = await CheckIfSourceHasDataAsync("tbl_mis_orgunit");
                    if (hasData)
                    {
                        var result = await SyncOrgUnitTableAsync(targetConnection, transaction);
                        response.Summary.OrgUnitRecordsDeleted = result.Deleted;
                        response.Summary.OrgUnitRecordsInserted = result.Inserted;
                        response.Summary.OrgUnitHadData = true;
                        _logger.LogInformation($"✅ OrgUnit - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ MISPORTAL.tbl_mis_orgunit has NO data. Skipping.");
                        response.Summary.OrgUnitHadData = false;
                        response.Summary.SkippedTables.Add("tbl_mis_orgunit");
                    }
                }

                if (request.SyncJobCode)
                {
                    _logger.LogInformation("📋 Processing tbl_mis_jobcode...");
                    var hasData = await CheckIfSourceHasDataAsync("tbl_mis_jobcode");
                    if (hasData)
                    {
                        var result = await SyncJobCodeTableAsync(targetConnection, transaction);
                        response.Summary.JobCodeRecordsDeleted = result.Deleted;
                        response.Summary.JobCodeRecordsInserted = result.Inserted;
                        response.Summary.JobCodeHadData = true;
                        _logger.LogInformation($"✅ JobCode - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ MISPORTAL.tbl_mis_jobcode has NO data. Skipping.");
                        response.Summary.JobCodeHadData = false;
                        response.Summary.SkippedTables.Add("tbl_mis_jobcode");
                    }
                }

                if (request.SyncPositionDetails)
                {
                    _logger.LogInformation("📋 Processing tbl_position_details...");
                    var hasData = await CheckIfSourceHasDataAsync("tbl_position_details");
                    if (hasData)
                    {
                        var result = await SyncPositionDetailsTableAsync(targetConnection, transaction);
                        response.Summary.PositionRecordsDeleted = result.Deleted;
                        response.Summary.PositionRecordsInserted = result.Inserted;
                        response.Summary.PositionHadData = true;
                        _logger.LogInformation($"✅ PositionDetails - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ MISPORTAL.tbl_position_details has NO data. Skipping.");
                        response.Summary.PositionHadData = false;
                        response.Summary.SkippedTables.Add("tbl_position_details");
                    }
                }

                if (request.SyncAttendanceTimesheet)
                {
                    _logger.LogInformation("📋 Processing tbl_attendance_timesheet...");
                    var hasData = await CheckIfSourceHasDataAsync("tbl_attendance_timesheet");
                    if (hasData)
                    {
                        var result = await SyncAttendanceTimesheetTableAsync(targetConnection, transaction);
                        response.Summary.AttendanceTimesheetRecordsDeleted = result.Deleted;
                        response.Summary.AttendanceTimesheetRecordsInserted = result.Inserted;
                        response.Summary.AttendanceTimesheetHadData = true;
                        _logger.LogInformation($"✅ AttendanceTimesheet - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ MISPORTAL.tbl_attendance_timesheet has NO data. Skipping.");
                        response.Summary.AttendanceTimesheetHadData = false;
                        response.Summary.SkippedTables.Add("tbl_attendance_timesheet");
                    }
                }

                response.Summary.TotalRecordsAffected =
                    response.Summary.OrgUnitRecordsInserted +
                    response.Summary.JobCodeRecordsInserted +
                    response.Summary.PositionRecordsInserted +
                    response.Summary.AttendanceTimesheetRecordsInserted;

                transaction.Commit();

                stopwatch.Stop();
                response.Summary.Duration = stopwatch.Elapsed;
                response.Success = true;
                response.Message = $"Sync completed. Total records: {response.Summary.TotalRecordsAffected}";

                _logger.LogInformation($"========== InBound Sync completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds ==========");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                stopwatch.Stop();

                response.StatusCode = 500;
                response.Success = false;
                response.Message = $"Sync failed: {ex.Message}";
                response.Errors.Add(ex.Message);

                _logger.LogError(ex, $"❌ InBound Sync failed at {DateTime.Now}");
            }
            finally
            {
                transaction.Dispose();
            }

            return response;
        }

        private async Task<(int Deleted, int Inserted)> SyncOrgUnitTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM tbl_mis_orgunit";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target tbl_mis_orgunit");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("orgcode", typeof(string));
            dataTable.Columns.Add("orgdescription", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = "SELECT orgcode, orgdescription FROM tbl_mis_orgunit WHERE orgdescription IS NOT NULL";
                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source tbl_mis_orgunit");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "tbl_mis_orgunit";
                    bulkCopy.ColumnMappings.Add("orgcode", "orgcode");
                    bulkCopy.ColumnMappings.Add("orgdescription", "orgdescription");
                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target tbl_mis_orgunit");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncJobCodeTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM tbl_mis_jobcode";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target tbl_mis_jobcode");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("jobcode", typeof(string));
            dataTable.Columns.Add("jobdescription", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"SELECT 
                    CAST(jobcode AS NVARCHAR(255)) as jobcode, 
                    [job description] as jobdescription 
                    FROM tbl_mis_jobcode 
                    WHERE [job description] IS NOT NULL";
                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source tbl_mis_jobcode");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "tbl_mis_jobcode";
                    bulkCopy.ColumnMappings.Add("jobcode", "jobcode");
                    bulkCopy.ColumnMappings.Add("jobdescription", "job description");
                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target tbl_mis_jobcode");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncPositionDetailsTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM tbl_position_details";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target tbl_position_details");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("OBJID", typeof(string));
            dataTable.Columns.Add("STEXT", typeof(string));
            dataTable.Columns.Add("EMPID", typeof(string));
            dataTable.Columns.Add("MGR_POS", typeof(string));
            dataTable.Columns.Add("MGR_POS_TXT", typeof(string));
            dataTable.Columns.Add("MGR_EMP", typeof(string));
            dataTable.Columns.Add("KOSTL", typeof(string));
            dataTable.Columns.Add("EMP_TYPE", typeof(string));
            dataTable.Columns.Add("EMP_CATEGORY", typeof(string));
            dataTable.Columns.Add("EMP_TRAVEL_ALL", typeof(string));
            dataTable.Columns.Add("LATITUDE", typeof(string));
            dataTable.Columns.Add("LONGITUDE", typeof(string));
            dataTable.Columns.Add("ORGUNIT", typeof(string));
            dataTable.Columns.Add("ORGUNIT_TXT", typeof(string));
            dataTable.Columns.Add("JOBCODE", typeof(string));
            dataTable.Columns.Add("JOBCODE_TXT", typeof(string));
            dataTable.Columns.Add("POSBPID", typeof(string));
            dataTable.Columns.Add("BUKRS", typeof(string));
            dataTable.Columns.Add("BUTXT", typeof(string));
            dataTable.Columns.Add("PRIMARY", typeof(string));
            dataTable.Columns.Add("START_DATE", typeof(string));
            dataTable.Columns.Add("END_DATE", typeof(string));
            dataTable.Columns.Add("SHORT_DES", typeof(string));
            dataTable.Columns.Add("LAST_RCNT_EMPNO", typeof(string));
            dataTable.Columns.Add("EMPLYEE_ENDDATE", typeof(string));
            dataTable.Columns.Add("POS_CRET_DATE", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        ISNULL(OBJID, '') as OBJID,
                        ISNULL(STEXT, '') as STEXT,
                        ISNULL(EMPID, '') as EMPID,
                        ISNULL(MGR_POS, '') as MGR_POS,
                        ISNULL(MGR_POS_TXT, '') as MGR_POS_TXT,
                        ISNULL(MGR_EMP, '') as MGR_EMP,
                        ISNULL(KOSTL, '') as KOSTL,
                        ISNULL(EMP_TYPE, '') as EMP_TYPE,
                        ISNULL(EMP_CATEGORY, '') as EMP_CATEGORY,
                        ISNULL(EMP_TRAVEL_ALL, '') as EMP_TRAVEL_ALL,
                        ISNULL(LATITUDE, '') as LATITUDE,
                        ISNULL(LONGITUDE, '') as LONGITUDE,
                        ISNULL(ORGUNIT, '') as ORGUNIT,
                        ISNULL(ORGUNIT_TXT, '') as ORGUNIT_TXT,
                        ISNULL(JOBCODE, '') as JOBCODE,
                        ISNULL(JOBCODE_TXT, '') as JOBCODE_TXT,
                        ISNULL(POSBPID, '') as POSBPID,
                        ISNULL(BUKRS, '') as BUKRS,
                        ISNULL(BUTXT, '') as BUTXT,
                        ISNULL([PRIMARY], '') as [PRIMARY],
                        ISNULL(START_DATE, '') as START_DATE,
                        ISNULL(END_DATE, '') as END_DATE,
                        ISNULL(SHORT_DES, '') as SHORT_DES,
                        ISNULL(LAST_RCNT_EMPNO, '') as LAST_RCNT_EMPNO,
                        ISNULL(EMPLYEE_ENDDATE, '') as EMPLYEE_ENDDATE,
                        ISNULL(POS_CRET_DATE, '') as POS_CRET_DATE
                    FROM tbl_position_details";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source tbl_position_details");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "tbl_position_details";
                    bulkCopy.BulkCopyTimeout = 300;

                    bulkCopy.ColumnMappings.Add("OBJID", "OBJID");
                    bulkCopy.ColumnMappings.Add("STEXT", "STEXT");
                    bulkCopy.ColumnMappings.Add("EMPID", "EMPID");
                    bulkCopy.ColumnMappings.Add("MGR_POS", "MGR_POS");
                    bulkCopy.ColumnMappings.Add("MGR_POS_TXT", "MGR_POS_TXT");
                    bulkCopy.ColumnMappings.Add("MGR_EMP", "MGR_EMP");
                    bulkCopy.ColumnMappings.Add("KOSTL", "KOSTL");
                    bulkCopy.ColumnMappings.Add("EMP_TYPE", "EMP_TYPE");
                    bulkCopy.ColumnMappings.Add("EMP_CATEGORY", "EMP_CATEGORY");
                    bulkCopy.ColumnMappings.Add("EMP_TRAVEL_ALL", "EMP_TRAVEL_ALL");
                    bulkCopy.ColumnMappings.Add("LATITUDE", "LATITUDE");
                    bulkCopy.ColumnMappings.Add("LONGITUDE", "LONGITUDE");
                    bulkCopy.ColumnMappings.Add("ORGUNIT", "ORGUNIT");
                    bulkCopy.ColumnMappings.Add("ORGUNIT_TXT", "ORGUNIT_TXT");
                    bulkCopy.ColumnMappings.Add("JOBCODE", "JOBCODE");
                    bulkCopy.ColumnMappings.Add("JOBCODE_TXT", "JOBCODE_TXT");
                    bulkCopy.ColumnMappings.Add("POSBPID", "POSBPID");
                    bulkCopy.ColumnMappings.Add("BUKRS", "BUKRS");
                    bulkCopy.ColumnMappings.Add("BUTXT", "BUTXT");
                    bulkCopy.ColumnMappings.Add("PRIMARY", "PRIMARY");
                    bulkCopy.ColumnMappings.Add("START_DATE", "START_DATE");
                    bulkCopy.ColumnMappings.Add("END_DATE", "END_DATE");
                    bulkCopy.ColumnMappings.Add("SHORT_DES", "SHORT_DES");
                    bulkCopy.ColumnMappings.Add("LAST_RCNT_EMPNO", "LAST_RCNT_EMPNO");
                    bulkCopy.ColumnMappings.Add("EMPLYEE_ENDDATE", "EMPLYEE_ENDDATE");
                    bulkCopy.ColumnMappings.Add("POS_CRET_DATE", "POS_CRET_DATE");

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target tbl_position_details");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncAttendanceTimesheetTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM tbl_attendance_timesheet";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target tbl_attendance_timesheet");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("ccomcode", typeof(string));
            dataTable.Columns.Add("corgcode", typeof(string));
            dataTable.Columns.Add("cloccode", typeof(string));
            dataTable.Columns.Add("cfincode", typeof(string));
            dataTable.Columns.Add("cdoctype", typeof(string));
            dataTable.Columns.Add("ndocno", typeof(int));
            dataTable.Columns.Add("cempcode", typeof(string));
            dataTable.Columns.Add("cempname", typeof(string));
            dataTable.Columns.Add("creportmgrid", typeof(string));
            dataTable.Columns.Add("creportmgrname", typeof(string));
            dataTable.Columns.Add("niseqno", typeof(int));
            dataTable.Columns.Add("nitaskno", typeof(int));
            dataTable.Columns.Add("ctaskdesc", typeof(string));
            dataTable.Columns.Add("chrstaken", typeof(string));
            dataTable.Columns.Add("cremarks", typeof(string));
            dataTable.Columns.Add("cmanagerremarks", typeof(string));
            dataTable.Columns.Add("ccreatedby", typeof(string));
            dataTable.Columns.Add("lcreateddate", typeof(DateTime));
            dataTable.Columns.Add("cmodifiedby", typeof(string));
            dataTable.Columns.Add("lmodifieddate", typeof(DateTime));
            dataTable.Columns.Add("ctaskstatus", typeof(string));
            dataTable.Columns.Add("lstartdttime", typeof(DateTime));
            dataTable.Columns.Add("lenddttime", typeof(DateTime));
            dataTable.Columns.Add("cmanagerstatus", typeof(string));
            dataTable.Columns.Add("itaskno", typeof(int));
            dataTable.Columns.Add("fileattachements", typeof(string));
            dataTable.Columns.Add("uniqueid", typeof(string));
            dataTable.Columns.Add("updatestarttime", typeof(string));
            dataTable.Columns.Add("updateendtime", typeof(string));
            dataTable.Columns.Add("postponedremarks", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        ISNULL(ccomcode, '') as ccomcode,
                        ISNULL(corgcode, '') as corgcode,
                        ISNULL(cloccode, '') as cloccode,
                        ISNULL(cfincode, '') as cfincode,
                        ISNULL(cdoctype, '') as cdoctype,
                        ISNULL(ndocno, 0) as ndocno,
                        ISNULL(cempcode, '') as cempcode,
                        ISNULL(cempname, '') as cempname,
                        ISNULL(creportmgrid, '') as creportmgrid,
                        ISNULL(creportmgrname, '') as creportmgrname,
                        ISNULL(niseqno, 0) as niseqno,
                        ISNULL(nitaskno, 0) as nitaskno,
                        ISNULL(ctaskdesc, '') as ctaskdesc,
                        ISNULL(chrstaken, '') as chrstaken,
                        ISNULL(cremarks, '') as cremarks,
                        ISNULL(cmanagerremarks, '') as cmanagerremarks,
                        ISNULL(ccreatedby, '') as ccreatedby,
                        CASE WHEN ISDATE(lcreateddate) = 1 THEN lcreateddate ELSE NULL END as lcreateddate,
                        ISNULL(cmodifiedby, '') as cmodifiedby,
                        CASE WHEN ISDATE(lmodifieddate) = 1 THEN lmodifieddate ELSE NULL END as lmodifieddate,
                        ISNULL(ctaskstatus, '') as ctaskstatus,
                        CASE WHEN ISDATE(lstartdttime) = 1 THEN lstartdttime ELSE NULL END as lstartdttime,
                        CASE WHEN ISDATE(lenddttime) = 1 THEN lenddttime ELSE NULL END as lenddttime,
                        ISNULL(cmanagerstatus, '') as cmanagerstatus,
                        ISNULL(itaskno, 0) as itaskno,
                        ISNULL(fileattachements, '') as fileattachements,
                        ISNULL(uniqueid, '') as uniqueid,
                        ISNULL(updatestarttime, '') as updatestarttime,
                        ISNULL(updateendtime, '') as updateendtime,
                        ISNULL(postponedremarks, '') as postponedremarks
                    FROM tbl_attendance_timesheet";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source tbl_attendance_timesheet");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "tbl_attendance_timesheet";
                    bulkCopy.BulkCopyTimeout = 300;

                    bulkCopy.ColumnMappings.Add("ccomcode", "ccomcode");
                    bulkCopy.ColumnMappings.Add("corgcode", "corgcode");
                    bulkCopy.ColumnMappings.Add("cloccode", "cloccode");
                    bulkCopy.ColumnMappings.Add("cfincode", "cfincode");
                    bulkCopy.ColumnMappings.Add("cdoctype", "cdoctype");
                    bulkCopy.ColumnMappings.Add("ndocno", "ndocno");
                    bulkCopy.ColumnMappings.Add("cempcode", "cempcode");
                    bulkCopy.ColumnMappings.Add("cempname", "cempname");
                    bulkCopy.ColumnMappings.Add("creportmgrid", "creportmgrid");
                    bulkCopy.ColumnMappings.Add("creportmgrname", "creportmgrname");
                    bulkCopy.ColumnMappings.Add("niseqno", "niseqno");
                    bulkCopy.ColumnMappings.Add("nitaskno", "nitaskno");
                    bulkCopy.ColumnMappings.Add("ctaskdesc", "ctaskdesc");
                    bulkCopy.ColumnMappings.Add("chrstaken", "chrstaken");
                    bulkCopy.ColumnMappings.Add("cremarks", "cremarks");
                    bulkCopy.ColumnMappings.Add("cmanagerremarks", "cmanagerremarks");
                    bulkCopy.ColumnMappings.Add("ccreatedby", "ccreatedby");
                    bulkCopy.ColumnMappings.Add("lcreateddate", "lcreateddate");
                    bulkCopy.ColumnMappings.Add("cmodifiedby", "cmodifiedby");
                    bulkCopy.ColumnMappings.Add("lmodifieddate", "lmodifieddate");
                    bulkCopy.ColumnMappings.Add("ctaskstatus", "ctaskstatus");
                    bulkCopy.ColumnMappings.Add("lstartdttime", "lstartdttime");
                    bulkCopy.ColumnMappings.Add("lenddttime", "lenddttime");
                    bulkCopy.ColumnMappings.Add("cmanagerstatus", "cmanagerstatus");
                    bulkCopy.ColumnMappings.Add("itaskno", "itaskno");
                    bulkCopy.ColumnMappings.Add("fileattachements", "fileattachements");
                    bulkCopy.ColumnMappings.Add("uniqueid", "uniqueid");
                    bulkCopy.ColumnMappings.Add("updatestarttime", "updatestarttime");
                    bulkCopy.ColumnMappings.Add("updateendtime", "updateendtime");
                    bulkCopy.ColumnMappings.Add("postponedremarks", "postponedremarks");

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target tbl_attendance_timesheet");
                }
            }

            return (deletedCount, insertedCount);
        }

        public async Task<bool> CheckIfProcessEngineSourceHasDataAsync(string tableName)
        {
            try
            {
                _logger.LogInformation($"🔍 Checking if {tableName} has data in ProcessEngineProductionDB...");

                using var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString);
                await sourceConnection.OpenAsync();

                var query = $"SELECT COUNT(1) FROM {tableName}";
                using var command = new SqlCommand(query, sourceConnection);
                var count = Convert.ToInt32(await command.ExecuteScalarAsync());

                _logger.LogInformation($"📊 Table {tableName} has {count} records in ProcessEngineProductionDB");

                return count > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Error checking data in {tableName}: {ex.Message}");
                return false;
            }
        }

        public async Task<ProcessEngineSyncResponseDTO> SyncProcessEngineToTaskEngineAsync(ProcessEngineSyncRequestDTO request)
        {
            var stopwatch = Stopwatch.StartNew();
            var response = new ProcessEngineSyncResponseDTO
            {
                StatusCode = 200,
                Timestamp = DateTime.Now,
                Errors = new List<string>(),
                Summary = new ProcessEngineSyncSummaryDTO
                {
                    SkippedTables = new List<string>()
                }
            };

            _logger.LogInformation($"========== Starting ProcessEngine to TaskEngine Sync at {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==========");
            _logger.LogInformation($"Triggered by: {request.TriggeredBy}");

            using var targetConnection = new SqlConnection(_targetTaskEngineConnectionString);
            await targetConnection.OpenAsync();

            var transaction = targetConnection.BeginTransaction();

            try
            {
                if (request.SyncProjectDetail)
                {
                    _logger.LogInformation("📋 Processing Tbl_Project_Detail...");
                    var hasData = await CheckIfProcessEngineSourceHasDataAsync("Tbl_Project_Detail");
                    if (hasData)
                    {
                        var result = await SyncProjectDetailTableAsync(targetConnection, transaction);
                        response.Summary.ProjectDetailRecordsDeleted = result.Deleted;
                        response.Summary.ProjectDetailRecordsInserted = result.Inserted;
                        response.Summary.ProjectDetailHadData = true;
                        _logger.LogInformation($"✅ ProjectDetail - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ProcessEngine.Tbl_Project_Detail has NO data. Skipping.");
                        response.Summary.ProjectDetailHadData = false;
                        response.Summary.SkippedTables.Add("Tbl_Project_Detail");
                    }
                }

                if (request.SyncProjectMaster)
                {
                    _logger.LogInformation("📋 Processing Tbl_Project_Master...");
                    var hasData = await CheckIfProcessEngineSourceHasDataAsync("Tbl_Project_Master");
                    if (hasData)
                    {
                        var result = await SyncProjectMasterTableAsync(targetConnection, transaction);
                        response.Summary.ProjectMasterRecordsDeleted = result.Deleted;
                        response.Summary.ProjectMasterRecordsInserted = result.Inserted;
                        response.Summary.ProjectMasterHadData = true;
                        _logger.LogInformation($"✅ ProjectMaster - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ProcessEngine.Tbl_Project_Master has NO data. Skipping.");
                        response.Summary.ProjectMasterHadData = false;
                        response.Summary.SkippedTables.Add("Tbl_Project_Master");
                    }
                }

                if (request.SyncProjectVersionDetails)
                {
                    _logger.LogInformation("📋 Processing Tbl_Project_Version_Details...");
                    var hasData = await CheckIfProcessEngineSourceHasDataAsync("Tbl_Project_Version_Details");
                    if (hasData)
                    {
                        var result = await SyncProjectVersionDetailsTableAsync(targetConnection, transaction);
                        response.Summary.ProjectVersionDetailsRecordsDeleted = result.Deleted;
                        response.Summary.ProjectVersionDetailsRecordsInserted = result.Inserted;
                        response.Summary.ProjectVersionDetailsHadData = true;
                        _logger.LogInformation($"✅ ProjectVersionDetails - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ProcessEngine.Tbl_Project_Version_Details has NO data. Skipping.");
                        response.Summary.ProjectVersionDetailsHadData = false;
                        response.Summary.SkippedTables.Add("Tbl_Project_Version_Details");
                    }
                }

                if (request.SyncTaskFlowDetail)
                {
                    _logger.LogInformation("📋 Processing Tbl_TaskFlow_Detail...");
                    var hasData = await CheckIfProcessEngineSourceHasDataAsync("Tbl_TaskFlow_Detail");
                    if (hasData)
                    {
                        var result = await SyncTaskFlowDetailTableAsync(targetConnection, transaction);
                        response.Summary.TaskFlowDetailRecordsDeleted = result.Deleted;
                        response.Summary.TaskFlowDetailRecordsInserted = result.Inserted;
                        response.Summary.TaskFlowDetailHadData = true;
                        _logger.LogInformation($"✅ TaskFlowDetail - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ProcessEngine.Tbl_TaskFlow_Detail has NO data. Skipping.");
                        response.Summary.TaskFlowDetailHadData = false;
                        response.Summary.SkippedTables.Add("Tbl_TaskFlow_Detail");
                    }
                }

                if (request.SyncTaskFlowMaster)
                {
                    _logger.LogInformation("📋 Processing Tbl_TaskFlow_Master...");
                    var hasData = await CheckIfProcessEngineSourceHasDataAsync("Tbl_TaskFlow_Master");
                    if (hasData)
                    {
                        var result = await SyncTaskFlowMasterTableAsync(targetConnection, transaction);
                        response.Summary.TaskFlowMasterRecordsDeleted = result.Deleted;
                        response.Summary.TaskFlowMasterRecordsInserted = result.Inserted;
                        response.Summary.TaskFlowMasterHadData = true;
                        _logger.LogInformation($"✅ TaskFlowMaster - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ProcessEngine.Tbl_TaskFlow_Master has NO data. Skipping.");
                        response.Summary.TaskFlowMasterHadData = false;
                        response.Summary.SkippedTables.Add("Tbl_TaskFlow_Master");
                    }
                }

                if (request.SyncTransactionTaskFlowDetail)
                {
                    _logger.LogInformation("📋 Processing Tbl_Transaction_TaskFlow_Detail_And_Status...");
                    var hasData = await CheckIfProcessEngineSourceHasDataAsync("Tbl_Transaction_TaskFlow_Detail_And_Status");
                    if (hasData)
                    {
                        var result = await SyncTransactionTaskFlowDetailTableAsync(targetConnection, transaction);
                        response.Summary.TransactionTaskFlowDetailRecordsDeleted = result.Deleted;
                        response.Summary.TransactionTaskFlowDetailRecordsInserted = result.Inserted;
                        response.Summary.TransactionTaskFlowDetailHadData = true;
                        _logger.LogInformation($"✅ TransactionTaskFlowDetail - Deleted: {result.Deleted}, Inserted: {result.Inserted}");
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ ProcessEngine.Tbl_Transaction_TaskFlow_Detail_And_Status has NO data. Skipping.");
                        response.Summary.TransactionTaskFlowDetailHadData = false;
                        response.Summary.SkippedTables.Add("Tbl_Transaction_TaskFlow_Detail_And_Status");
                    }
                }

                response.Summary.TotalRecordsAffected =
                    response.Summary.ProjectDetailRecordsInserted +
                    response.Summary.ProjectMasterRecordsInserted +
                    response.Summary.ProjectVersionDetailsRecordsInserted +
                    response.Summary.TaskFlowDetailRecordsInserted +
                    response.Summary.TaskFlowMasterRecordsInserted +
                    response.Summary.TransactionTaskFlowDetailRecordsInserted;

                transaction.Commit();

                stopwatch.Stop();
                response.Summary.Duration = stopwatch.Elapsed;
                response.Success = true;
                response.Message = $"Sync completed. Total records: {response.Summary.TotalRecordsAffected}";

                _logger.LogInformation($"========== ProcessEngine to TaskEngine Sync completed in {stopwatch.Elapsed.TotalSeconds:F2} seconds ==========");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                stopwatch.Stop();

                response.StatusCode = 500;
                response.Success = false;
                response.Message = $"Sync failed: {ex.Message}";
                response.Errors.Add(ex.Message);

                _logger.LogError(ex, $"❌ ProcessEngine to TaskEngine Sync failed at {DateTime.Now}");
            }
            finally
            {
                transaction.Dispose();
            }

            return response;
        }

        private async Task<(int Deleted, int Inserted)> SyncProjectDetailTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM Tbl_Project_Detail";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target Tbl_Project_Detail");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("header_id", typeof(int));
            dataTable.Columns.Add("Detail_id", typeof(int));
            dataTable.Columns.Add("module", typeof(string));
            dataTable.Columns.Add("projectDescription", typeof(string));
            dataTable.Columns.Add("Resources", typeof(string));
            dataTable.Columns.Add("No_of_Resources", typeof(int));
            dataTable.Columns.Add("Slavalue", typeof(int));
            dataTable.Columns.Add("Slaunit", typeof(string));
            dataTable.Columns.Add("version", typeof(int));
            dataTable.Columns.Add("Remarks", typeof(string));
            dataTable.Columns.Add("Status1", typeof(string));
            dataTable.Columns.Add("created_date", typeof(DateTime));
            dataTable.Columns.Add("modified_date", typeof(DateTime));
            dataTable.Columns.Add("Capproved_by", typeof(string));
            dataTable.Columns.Add("Capproved_date", typeof(DateTime));

            using (var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        header_id, Detail_id, module, projectDescription, Resources, 
                        No_of_Resources, Slavalue, Slaunit, version, Remarks, 
                        Status1, created_date, modified_date, Capproved_by, Capproved_date
                    FROM Tbl_Project_Detail";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source Tbl_Project_Detail");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "Tbl_Project_Detail";
                    bulkCopy.BulkCopyTimeout = 300;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target Tbl_Project_Detail");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncProjectMasterTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM Tbl_Project_Master";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target Tbl_Project_Master");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("ClientTenantId", typeof(int));
            dataTable.Columns.Add("RaisedByUserId", typeof(string));
            dataTable.Columns.Add("AssignedManagerId", typeof(string));
            dataTable.Columns.Add("ProjectName", typeof(string));
            dataTable.Columns.Add("ProjectType", typeof(string));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("CreatedDate", typeof(DateTime));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("cattachment", typeof(string));
            dataTable.Columns.Add("expecteddate", typeof(DateTime));
            dataTable.Columns.Add("Version", typeof(int));
            dataTable.Columns.Add("ModifiedDate", typeof(DateTime));
            dataTable.Columns.Add("Remarks1", typeof(string));
            dataTable.Columns.Add("Remarks2", typeof(string));
            dataTable.Columns.Add("Remarks3", typeof(string));
            dataTable.Columns.Add("RaisedByAttachment", typeof(string));
            dataTable.Columns.Add("AssignedManagerAttachment", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        ID, ClientTenantId, RaisedByUserId, AssignedManagerId, ProjectName, 
                        ProjectType, Description, CreatedDate, Status, cattachment, 
                        expecteddate, Version, ModifiedDate, Remarks1, Remarks2, 
                        Remarks3, RaisedByAttachment, AssignedManagerAttachment
                    FROM Tbl_Project_Master";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source Tbl_Project_Master");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "Tbl_Project_Master";
                    bulkCopy.BulkCopyTimeout = 300;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target Tbl_Project_Master");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncProjectVersionDetailsTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM Tbl_Project_Version_Details";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target Tbl_Project_Version_Details");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("Id", typeof(int));
            dataTable.Columns.Add("HeaderId", typeof(int));
            dataTable.Columns.Add("Description", typeof(string));
            dataTable.Columns.Add("createdBy", typeof(string));
            dataTable.Columns.Add("createdDate", typeof(DateTime));
            dataTable.Columns.Add("Status", typeof(string));
            dataTable.Columns.Add("expecteddate", typeof(DateTime));
            dataTable.Columns.Add("Version", typeof(int));

            using (var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        Id, HeaderId, Description, createdBy, createdDate, 
                        Status, expecteddate, Version
                    FROM Tbl_Project_Version_Details";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source Tbl_Project_Version_Details");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "Tbl_Project_Version_Details";
                    bulkCopy.BulkCopyTimeout = 300;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target Tbl_Project_Version_Details");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncTaskFlowDetailTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM Tbl_TaskFlow_Detail";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target Tbl_TaskFlow_Detail");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("itaskno", typeof(int));
            dataTable.Columns.Add("iseqno", typeof(int));
            dataTable.Columns.Add("iheader_id", typeof(int));
            dataTable.Columns.Add("ctenant_id", typeof(int));
            dataTable.Columns.Add("ctask_type", typeof(string));
            dataTable.Columns.Add("cmapping_code", typeof(string));
            dataTable.Columns.Add("ccurrent_status", typeof(string));
            dataTable.Columns.Add("lcurrent_status_date", typeof(DateTime));
            dataTable.Columns.Add("cremarks", typeof(string));
            dataTable.Columns.Add("inext_seqno", typeof(int));
            dataTable.Columns.Add("cnext_seqtype", typeof(string));
            dataTable.Columns.Add("cprevtype", typeof(int));
            dataTable.Columns.Add("cis_forwarded", typeof(int));
            dataTable.Columns.Add("lfwd_date", typeof(DateTime));
            dataTable.Columns.Add("cfwd_to", typeof(string));
            dataTable.Columns.Add("cis_reassigned", typeof(int));
            dataTable.Columns.Add("lreassign_date", typeof(DateTime));
            dataTable.Columns.Add("creassign_to", typeof(string));
            dataTable.Columns.Add("nboard_enabled", typeof(int));
            dataTable.Columns.Add("cprocess_type", typeof(int));
            dataTable.Columns.Add("csla_day", typeof(int));
            dataTable.Columns.Add("csla_Hour", typeof(int));
            dataTable.Columns.Add("caction_privilege", typeof(string));
            dataTable.Columns.Add("crejection_privilege", typeof(string));
            //dataTable.Columns.Add("islug", typeof(string));
            dataTable.Columns.Add("cattachment", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        ID, itaskno, iseqno, iheader_id, ctenant_id, ctask_type, 
                        cmapping_code, ccurrent_status, lcurrent_status_date, cremarks, 
                        inext_seqno, cnext_seqtype, cprevtype, cis_forwarded, lfwd_date, 
                        cfwd_to, cis_reassigned, lreassign_date, creassign_to, nboard_enabled, 
                        cprocess_type, csla_day, csla_Hour, caction_privilege, crejection_privilege, 
                        cattachment
                    FROM Tbl_TaskFlow_Detail";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source Tbl_TaskFlow_Detail");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "Tbl_TaskFlow_Detail";
                    bulkCopy.BulkCopyTimeout = 300;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target Tbl_TaskFlow_Detail");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncTaskFlowMasterTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM Tbl_TaskFlow_Master";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target Tbl_TaskFlow_Master");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("itaskno", typeof(int));
            dataTable.Columns.Add("ctenant_id", typeof(int));
            dataTable.Columns.Add("ctask_type", typeof(string));
            dataTable.Columns.Add("ctask_name", typeof(string));
            dataTable.Columns.Add("ctask_description", typeof(string));
            dataTable.Columns.Add("cstatus", typeof(string));
            dataTable.Columns.Add("ccurrent_status_with", typeof(string));
            dataTable.Columns.Add("lcompleted_date", typeof(DateTime));
            dataTable.Columns.Add("ccreated_by", typeof(string));
            dataTable.Columns.Add("lcreated_date", typeof(DateTime));
            dataTable.Columns.Add("cmodified_by", typeof(string));
            dataTable.Columns.Add("lmodified_date", typeof(DateTime));
            dataTable.Columns.Add("cprocess_id", typeof(int));
            dataTable.Columns.Add("cremarks", typeof(string));
            dataTable.Columns.Add("cattachment", typeof(string));
            dataTable.Columns.Add("cmeta_response", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        ID, itaskno, ctenant_id, ctask_type, ctask_name, ctask_description, 
                        cstatus, ccurrent_status_with, lcompleted_date, ccreated_by, 
                        lcreated_date, cmodified_by, lmodified_date, cprocess_id, 
                        cremarks, cattachment, cmeta_response
                    FROM Tbl_TaskFlow_Master";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source Tbl_TaskFlow_Master");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "Tbl_TaskFlow_Master";
                    bulkCopy.BulkCopyTimeout = 300;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target Tbl_TaskFlow_Master");
                }
            }

            return (deletedCount, insertedCount);
        }

        private async Task<(int Deleted, int Inserted)> SyncTransactionTaskFlowDetailTableAsync(SqlConnection targetConnection, SqlTransaction transaction)
        {
            int deletedCount = 0;
            int insertedCount = 0;

            var deleteQuery = "DELETE FROM Tbl_Transaction_TaskFlow_Detail_And_Status";
            using (var deleteCommand = new SqlCommand(deleteQuery, targetConnection, transaction))
            {
                deletedCount = await deleteCommand.ExecuteNonQueryAsync();
                _logger.LogInformation($"🗑️ Deleted {deletedCount} records from target Tbl_Transaction_TaskFlow_Detail_And_Status");
            }

            var dataTable = new DataTable();
            dataTable.Columns.Add("ID", typeof(int));
            dataTable.Columns.Add("itaskno", typeof(int));
            dataTable.Columns.Add("ctenant_id", typeof(int));
            dataTable.Columns.Add("cheader_id", typeof(int));
            dataTable.Columns.Add("cdetail_id", typeof(int));
            dataTable.Columns.Add("cstatus", typeof(string));
            dataTable.Columns.Add("cstatus_with", typeof(string));
            dataTable.Columns.Add("lstatus_date", typeof(DateTime));
            dataTable.Columns.Add("cremarks", typeof(string));
            dataTable.Columns.Add("crejected_reason", typeof(string));

            using (var sourceConnection = new SqlConnection(_sourceProcessEngineConnectionString))
            {
                await sourceConnection.OpenAsync();
                var selectQuery = @"
                    SELECT 
                        ID, itaskno, ctenant_id, cheader_id, cdetail_id, cstatus, 
                        cstatus_with, lstatus_date, cremarks, crejected_reason
                    FROM Tbl_Transaction_TaskFlow_Detail_And_Status";

                using (var adapter = new SqlDataAdapter(selectQuery, sourceConnection))
                {
                    adapter.Fill(dataTable);
                }
            }

            _logger.LogInformation($"📥 Retrieved {dataTable.Rows.Count} records from source Tbl_Transaction_TaskFlow_Detail_And_Status");

            if (dataTable.Rows.Count > 0)
            {
                using (var bulkCopy = new SqlBulkCopy(targetConnection, SqlBulkCopyOptions.Default, transaction))
                {
                    bulkCopy.DestinationTableName = "Tbl_Transaction_TaskFlow_Detail_And_Status";
                    bulkCopy.BulkCopyTimeout = 300;

                    foreach (DataColumn col in dataTable.Columns)
                    {
                        bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                    }

                    await bulkCopy.WriteToServerAsync(dataTable);
                    insertedCount = dataTable.Rows.Count;
                    _logger.LogInformation($"📝 Inserted {insertedCount} records into target Tbl_Transaction_TaskFlow_Detail_And_Status");
                }
            }

            return (deletedCount, insertedCount);
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
                    dt.Rows.Add(
                        tenantId,
                        string.IsNullOrEmpty(emp.EMPLOYEE_ID) ? (object)DBNull.Value : Convert.ToInt64(emp.EMPLOYEE_ID),
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

                    using (SqlTransaction tran = conn.BeginTransaction())
                    {
                        try
                        {
                            using (SqlBulkCopy bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.TableLock, tran))
                            {
                                bulkCopy.DestinationTableName = "dbo.Users_Staging";
                                bulkCopy.BatchSize = 5000;
                                bulkCopy.BulkCopyTimeout = 120;

                                foreach (DataColumn col in dt.Columns)
                                {
                                    bulkCopy.ColumnMappings.Add(col.ColumnName, col.ColumnName);
                                }

                                await bulkCopy.WriteToServerAsync(dt);
                            }

                            tran.Commit();
                        }
                        catch
                        {
                            tran.Rollback();
                            throw;
                        }
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