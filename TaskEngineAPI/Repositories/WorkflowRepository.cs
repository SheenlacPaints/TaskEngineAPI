using System.Data;
using System.Data.SqlClient;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;

namespace TaskEngineAPI.Repositories;

public class WorkflowRepository : IWorkflowRepository
{
    private readonly string _connectionString;
    private readonly ILogger<WorkflowRepository> _logger;

    public WorkflowRepository(IConfiguration configuration, ILogger<WorkflowRepository> logger)
    {
        _connectionString = configuration.GetConnectionString("Database");
        _logger = logger;
    }

    public async Task<IEnumerable<dynamic>> GetWorkflowDashboardAsync(string tenantId, string userId)
    {
        var results = new List<dynamic>();

        try
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("sp_get_workflow_socket", connection);

            command.CommandType = CommandType.StoredProcedure;
            command.CommandTimeout = 30;

            // Add parameters
            command.Parameters.Add(new SqlParameter("@userid", SqlDbType.VarChar, 100) { Value = userId });
            command.Parameters.Add(new SqlParameter("@tenentid", SqlDbType.VarChar, 100) { Value = tenantId });

            _logger.LogInformation("Executing sp_get_workflow_socket for TenantId: {TenantId}, UserId: {UserId}",
                tenantId, userId);

            await connection.OpenAsync();

            using var reader = await command.ExecuteReaderAsync();

            // Get column names
            var columnNames = new List<string>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                columnNames.Add(reader.GetName(i));
            }

            // Read data dynamically
            while (await reader.ReadAsync())
            {
                var row = new System.Dynamic.ExpandoObject() as IDictionary<string, object>;

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[columnNames[i]] = value;
                }

                results.Add(row);
            }

            _logger.LogInformation("Retrieved {Count} records from sp_get_workflow_socket", results.Count);

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing SP for tenant {TenantId}, user {UserId}", tenantId, userId);
            throw;
        }
    }
}