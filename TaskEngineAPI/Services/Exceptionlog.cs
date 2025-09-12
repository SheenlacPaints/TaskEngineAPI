using System.Data.SqlClient;

namespace TaskEngineAPI.Services
{
    public class Exceptionlog
    {

        public static IConfiguration Configuration;

        public Exceptionlog(IConfiguration _configuration)
        {
            Configuration = _configuration;
        }
     
        public static void LogException(string message, string docType, Exception ex = null, int? tenantId = null, int? userId = null, Guid? requestId = null)
        {
            try
            {
                int maxLength = 1999;
                if (!string.IsNullOrEmpty(docType) && docType.Length > maxLength)
                {
                    docType = docType.Substring(0, maxLength);
                }

                IConfigurationRoot configuration = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsettings.json")
                    .Build();

                string connectionString = configuration.GetConnectionString("Database");

                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    string query = @"INSERT INTO tbl_exceptionlog
                            (message, stacktrace, createddate, doctype, tenantId, userId, requestId)
                             VALUES (@message, @stacktrace, @createddate, @doctype, @tenantId, @userId, @requestId)";

                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@message", message ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@stacktrace", ex?.StackTrace ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@createddate", DateTime.Now);
                        cmd.Parameters.AddWithValue("@doctype", docType ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@tenantId", tenantId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@userId", userId ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@requestId", requestId?.ToString() ?? (object)DBNull.Value);

                        con.Open();
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch
            {
                // don’t throw again — last chance logging
            }
        }














    }
}
