using System.Data.SqlClient;
using System.Data;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
namespace TaskEngineAPI.Services
{
    public class ProjectService: IProjectService
    {
        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;

        public async Task<int> InsertProjectMasterAsync(CreateProjectDTO model, int tenantId, string userName)
        {
            int masterId = 0;  
            var connectionString = _config.GetConnectionString("Database");
            using (var conn = new SqlConnection(connectionString))
            {
                await conn.OpenAsync();
                using (var transaction = conn.BeginTransaction())
                {
                    try
                    { 
                        string queryMaster = @"
                    INSERT INTO Tbl_Project_Master (ClientTenantId,RaisedByUserId,AssignedManagerId,ProjectName,Description,
                     CreatedDate,Status) VALUES (@ClientTenantId, @RaisedByUserId, @AssignedManagerId, @ProjectName, @Description, 
                     @CreatedDate, @Status);SELECT SCOPE_IDENTITY();";

                        using (var cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            
                            cmd.Parameters.AddWithValue("@ClientTenantId", tenantId);
                            cmd.Parameters.AddWithValue("@RaisedByUserId", userName);
                            cmd.Parameters.AddWithValue("@AssignedManagerId", (object?)model.AssignedManagerId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjectName", (object?)model.ProjectName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Description", (object?)model.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@cstatus", "Initiated");
                            cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                            var newId = await cmd.ExecuteScalarAsync();
                            masterId = newId != null ? Convert.ToInt32(newId) : 0;
                        }                                         
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            return masterId;
        }


    }
}
