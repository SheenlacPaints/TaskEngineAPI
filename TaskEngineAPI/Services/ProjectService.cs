using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using TaskEngineAPI.Controllers;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
using TaskEngineAPI.Models;
namespace TaskEngineAPI.Services
{
    public class ProjectService: IProjectService
    {
        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;
        public ProjectService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository, IOptions<UploadSettings> uploadSettings)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
            _uploadSettings = uploadSettings.Value;
        }


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
                    INSERT INTO Tbl_Project_Master (ClientTenantId,RaisedByUserId,AssignedManagerId,ProjectName,ProjectType,Description,
                     CreatedDate,Status,expecteddate) VALUES (@ClientTenantId, @RaisedByUserId, @AssignedManagerId, @ProjectName,@ProjectType, @Description, 
                     @CreatedDate, @Status,@expecteddate);SELECT SCOPE_IDENTITY();";
            
                        using (var cmd = new SqlCommand(queryMaster, conn, transaction))
                        {
                            
                            cmd.Parameters.AddWithValue("@ClientTenantId", tenantId);
                            cmd.Parameters.AddWithValue("@RaisedByUserId", userName);
                            cmd.Parameters.AddWithValue("@AssignedManagerId", (object?)model.AssignedManagerId ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@ProjectName", (object?)model.ProjectName ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Description", (object?)model.Description ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", "Pending");
                            cmd.Parameters.AddWithValue("@CreatedDate", DateTime.Now);
                            cmd.Parameters.AddWithValue("@ProjectType", (object?)model.ProjectType ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@expecteddate", (object?)model.expecteddate ?? DBNull.Value);
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


        public async Task<string> Getprojectmaster(int cTenantID, string username,string? type, string? searchText = null, int page = 1, int pageSize = 50)
        {
            List<GetProjectList> tsk = new List<GetProjectList>();
            int totalCount = 0;

            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 50;

            try
            {
                using (SqlConnection con = new SqlConnection(_config.GetConnectionString("Database")))
                {
                    using (SqlCommand cmd = new SqlCommand("sp_get_project_details", con))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@tenantid", cTenantID);
                        cmd.Parameters.AddWithValue("@cuserid", username);                   
                        cmd.Parameters.AddWithValue("@searchtxt", searchText ?? (object)DBNull.Value);
                        cmd.Parameters.AddWithValue("@type", type ?? (object)DBNull.Value);                      
                        cmd.Parameters.AddWithValue("@PageNo", page);
                        cmd.Parameters.AddWithValue("@PageSize", pageSize);

                        await con.OpenAsync();

                        using (SqlDataReader sdr = await cmd.ExecuteReaderAsync())
                        {
                            while (await sdr.ReadAsync())
                            {
                                GetProjectList p = new GetProjectList
                                {                              
                                    ProjectId = sdr.IsDBNull(sdr.GetOrdinal("ID")) ? 0 : Convert.ToInt32(sdr["ID"]),
                                    ClientTenantId = sdr.IsDBNull(sdr.GetOrdinal("ClientTenantId")) ? 0 : Convert.ToInt32(sdr["ClientTenantId"]),
                                    RaisedByUserId = sdr.IsDBNull(sdr.GetOrdinal("RaisedByUserId")) ? 0 : Convert.ToInt32(sdr["RaisedByUserId"]),
                                    AssignedManagerId = sdr.IsDBNull(sdr.GetOrdinal("AssignedManagerId")) ? 0 : Convert.ToInt32(sdr["AssignedManagerId"]),
                                    ProjectName = sdr.IsDBNull(sdr.GetOrdinal("ProjectName")) ? string.Empty : Convert.ToString(sdr["ProjectName"]),
                                    Description = sdr.IsDBNull(sdr.GetOrdinal("Description")) ? string.Empty : Convert.ToString(sdr["Description"]),
                                    CreatedDate = sdr.IsDBNull(sdr.GetOrdinal("CreatedDate")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("CreatedDate")),
                                    Status = sdr.IsDBNull(sdr.GetOrdinal("Status")) ? string.Empty : Convert.ToString(sdr["Status"]),
                                    Attachments = sdr.IsDBNull(sdr.GetOrdinal("cattachment")) ? string.Empty : Convert.ToString(sdr["cattachment"]),
                                    ProjectType = sdr.IsDBNull(sdr.GetOrdinal("ProjectType")) ? string.Empty : Convert.ToString(sdr["ProjectType"]),
                                    expecteddate = sdr.IsDBNull(sdr.GetOrdinal("expecteddate")) ? (DateTime?)null : sdr.GetDateTime(sdr.GetOrdinal("expecteddate"))
                                };
                                tsk.Add(p);
                            }

                            if (await sdr.NextResultAsync())
                            {
                                if (await sdr.ReadAsync())
                                {
                                    totalCount = sdr.IsDBNull(0) ? 0 : Convert.ToInt32(sdr[0]);
                                }
                            }
                        }
                    }
                }

                var response = new
                {
                    totalCount = totalCount,
                    data = tsk
                };

                return JsonConvert.SerializeObject(response, Formatting.Indented);
            }
            catch (Exception ex)
            {

                var errorResponse = new
                {
                    totalCount = 0,
                    data = new List<GetProjectList>(),
                    error = ex.Message
                };
                return JsonConvert.SerializeObject(errorResponse, Formatting.Indented);
            }
        }


        public async Task<string> Getprojectdropdown(int cTenantID, string username, string? type, string? searchText = null)
        {
            try
            {
                using (var con = new SqlConnection(_config.GetConnectionString("Database")))
                using (var cmd = new SqlCommand("sp_get_project_dropdown", con))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@tenantid", cTenantID);
                    cmd.Parameters.AddWithValue("@cuserid", username);                  
                    cmd.Parameters.AddWithValue("@searchtext", searchText ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@type", type ?? (object)DBNull.Value);
                    var ds = new DataSet();
                    var adapter = new SqlDataAdapter(cmd);
                    await Task.Run(() => adapter.Fill(ds)); // async wrapper

                    if (ds.Tables.Count > 0)
                    {
                        return JsonConvert.SerializeObject(ds.Tables[0], Formatting.Indented);
                    }

                    return "[]";
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public async Task<bool> InsertProjectDetails(List<ProjectDetailRequest> requests, int tenantId, string username)
        {
            if (requests == null || !requests.Any())
                throw new ArgumentException("Request list cannot be empty");

            using var conn = new SqlConnection(_config.GetConnectionString("Database"));
            await conn.OpenAsync();

            using var transaction = conn.BeginTransaction();
            try
            {
                string query = @"
INSERT INTO Tbl_Project_detail
(header_id, Detail_id, module, projectDescription, Resources, No_of_Resources, Slavalue, Slaunit, version, Remarks)
VALUES (@HeaderId, @DetailId, @Module, @ProjectDescription, @Resources, @NoOfResources, @Slavalue, @Slaunit, @Version, @Remarks);";

                foreach (var r in requests)
                {
                    using var cmd = new SqlCommand(query, conn, transaction);
                    cmd.Parameters.AddWithValue("@HeaderId", r.HeaderId);
                    cmd.Parameters.AddWithValue("@DetailId", r.DetailId);
                    cmd.Parameters.AddWithValue("@Module", r.Module ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@ProjectDescription", r.ProjectDescription ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Resources", r.Resources ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@NoOfResources", r.NoOfResources);
                    cmd.Parameters.AddWithValue("@Slavalue", r.Slavalue);
                    cmd.Parameters.AddWithValue("@Slaunit", r.Slaunit ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Version", r.Version ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Remarks", r.Remarks ?? (object)DBNull.Value);

                    await cmd.ExecuteNonQueryAsync();
                }

                transaction.Commit();
                return true;
            }
            catch (SqlException ex) when (ex.Number == 547) 
            {
                transaction.Rollback();
                throw new InvalidOperationException("Invalid HeaderId. Project Master record not found.", ex);
            }
            catch (SqlException ex) when (ex.Number == 2627) 
            {
                transaction.Rollback();
                throw new InvalidOperationException("Duplicate DetailId. Record already exists.", ex);
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                throw new Exception("Failed to insert project details.", ex);
            }
        }

        public async Task<bool> UpdateProjectDetails(ProjectDetailRequest request, int tenantId, string username)
        {
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            using var conn = new SqlConnection(_config.GetConnectionString("Database"));
            await conn.OpenAsync();

            try
            {
                string query = @"
                    UPDATE Tbl_Project_detail
                    SET
                        module = @Module,
                        projectDescription = @ProjectDescription,
                        Resources = @Resources,
                        No_of_Resources = @NoOfResources,
                        Slavalue = @Slavalue,
                        Slaunit = @Slaunit,
                        version = @Version,
                    Remarks=@Remarks
                    WHERE Detail_id = @DetailId
                      AND header_id = @HeaderId;";
                    
                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@DetailId", request.DetailId);
                cmd.Parameters.AddWithValue("@HeaderId", request.HeaderId);
                cmd.Parameters.AddWithValue("@Module", request.Module);
                cmd.Parameters.AddWithValue("@ProjectDescription", request.ProjectDescription ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Resources", request.Resources ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@NoOfResources", request.NoOfResources);
                cmd.Parameters.AddWithValue("@Slavalue", request.Slavalue);
                cmd.Parameters.AddWithValue("@Slaunit", request.Slaunit);
                cmd.Parameters.AddWithValue("@Version", request.Version);
                cmd.Parameters.AddWithValue("@Remarks", request.Remarks ?? (object)DBNull.Value);


                int rowsAffected = await cmd.ExecuteNonQueryAsync();

                if (rowsAffected == 0)
                {
                    throw new Exception($"No record found for DetailId={request.DetailId}, HeaderId={request.HeaderId}");
                }

                return true;
            }
            catch (SqlException ex) when (ex.Number == 547)
            {
                throw new Exception("Invalid HeaderId. Project Master record not found.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to update project detail.", ex);
            }
        }


    }
}
