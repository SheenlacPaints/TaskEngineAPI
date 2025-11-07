using System.Data.SqlClient;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Services
{
    public class LookUpService : ILookUpService
    {
        private readonly IConfiguration _config;

        public LookUpService(IConfiguration configuration)
        {
            _config = configuration;
        }

        private string GetConnectionString()
        {
            return _config.GetConnectionString("DefaultConnection") ??
                   _config.GetConnectionString("Database") ??
                   throw new Exception("No connection string found");
        }

        public async Task<IEnumerable<NotificationTypeDTO>> GetAllNotificationTypesAsync(int tenantID)
        {
            var result = new List<NotificationTypeDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    // FIX: Handle NULL nis_active values
                    string query = @"
                        SELECT 
                            ID, ctenent_id, notification_type, nis_active,
                            ccreated_by, lcreated_date, cmodified_by, lmodified_date
                        FROM tbl_notification_type 
                        WHERE ctenent_id = @TenantID 
                        AND (nis_active = 1 OR nis_active IS NULL)
                        ORDER BY notification_type";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new NotificationTypeDTO
                                {
                                    ID = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("ID")),
                                    ctenent_id = reader.IsDBNull(reader.GetOrdinal("ctenent_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                    notification_type = reader.IsDBNull(reader.GetOrdinal("notification_type")) ? string.Empty : reader.GetString(reader.GetOrdinal("notification_type")),
                                    nis_active = reader.IsDBNull(reader.GetOrdinal("nis_active")) ||
                                                (!reader.IsDBNull(reader.GetOrdinal("nis_active")) && reader.GetBoolean(reader.GetOrdinal("nis_active"))),
                                    ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                    lcreated_date = reader.IsDBNull(reader.GetOrdinal("lcreated_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                    cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                    lmodified_date = reader.IsDBNull(reader.GetOrdinal("lmodified_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                                });
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving notification types: {ex.Message}");
            }
        }

        public async Task<bool> CreateNotificationTypeAsync(CreateNotificationTypeDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                        INSERT INTO tbl_notification_type 
                            (ctenent_id, notification_type, nis_active, ccreated_by, lcreated_date)
                        VALUES 
                            (@ctenent_id, @notification_type, @nis_active, @ccreated_by, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id);
                        cmd.Parameters.AddWithValue("@notification_type", model.notification_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);

                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null && Convert.ToInt32(newId) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating notification type: {ex.Message}");
            }
        }

        public async Task<bool> UpdateNotificationTypeAsync(UpdateNotificationTypeDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                        UPDATE tbl_notification_type 
                        SET 
                            notification_type = @notification_type,
                            nis_active = @nis_active,
                            cmodified_by = @cmodified_by,
                            lmodified_date = GETDATE()
                        WHERE ID = @ID AND ctenent_id = @ctenent_id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id); // FIX: Added this missing parameter
                        cmd.Parameters.AddWithValue("@notification_type", model.notification_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating notification type: {ex.Message}");
            }
        }

        public async Task<bool> DeleteNotificationTypeAsync(DeleteNotificationTypeDTO model, int tenantID, string username)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                   
                    string checkQuery = "SELECT COUNT(*) FROM tbl_notification_type WHERE ID = @ID AND ctenent_id = @TenantID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ID", model.ID);
                        checkCmd.Parameters.AddWithValue("@TenantID", tenantID);

                        var recordCount = (int)await checkCmd.ExecuteScalarAsync();
                        if (recordCount == 0)
                        {
                            return false; 
                        }
                    }

                    
                    string query = "DELETE FROM tbl_notification_type WHERE ID = @ID AND ctenent_id = @TenantID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting notification type: {ex.Message}");
            }
        }
        public async Task<IEnumerable<ProcessPriorityLabelDTO>> GetAllProcessPriorityLabelsAsync(int tenantID)
        {
            var result = new List<ProcessPriorityLabelDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    // FIX: Handle NULL nis_active values
                    string query = @"
                        SELECT 
                            ID, ctenent_id, priority_type, nis_active,
                            ccreated_by, lcreated_date, cmodified_by, lmodified_date
                        FROM tbl_process_priority_label 
                        WHERE ctenent_id = @TenantID 
                        AND (nis_active = 1 OR nis_active IS NULL)
                        ORDER BY priority_type";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new ProcessPriorityLabelDTO
                                {
                                    ID = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("ID")),
                                    ctenent_id = reader.IsDBNull(reader.GetOrdinal("ctenent_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                    priority_type = reader.IsDBNull(reader.GetOrdinal("priority_type")) ? string.Empty : reader.GetString(reader.GetOrdinal("priority_type")),
                                    nis_active = reader.IsDBNull(reader.GetOrdinal("nis_active")) ||
                                                (!reader.IsDBNull(reader.GetOrdinal("nis_active")) && reader.GetBoolean(reader.GetOrdinal("nis_active"))),
                                    ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                    lcreated_date = reader.IsDBNull(reader.GetOrdinal("lcreated_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                    cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                    lmodified_date = reader.IsDBNull(reader.GetOrdinal("lmodified_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                                });
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving process priority labels: {ex.Message}");
            }
        }

      
        public async Task<bool> CreateProcessPriorityLabelAsync(CreateProcessPriorityLabelDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                        INSERT INTO tbl_process_priority_label 
                            (ctenent_id, priority_type, nis_active, ccreated_by, lcreated_date)
                        VALUES 
                            (@ctenent_id, @priority_type, @nis_active, @ccreated_by, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id);
                        cmd.Parameters.AddWithValue("@priority_type", model.priority_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);

                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null && Convert.ToInt32(newId) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating process priority label: {ex.Message}");
            }
        }

        public async Task<bool> UpdateProcessPriorityLabelAsync(UpdateProcessPriorityLabelDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                        UPDATE tbl_process_priority_label 
                        SET 
                            priority_type = @priority_type,
                            nis_active = @nis_active,
                            cmodified_by = @cmodified_by,
                            lmodified_date = GETDATE()
                        WHERE ID = @ID AND ctenent_id = @ctenent_id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id); // FIX: Added this missing parameter
                        cmd.Parameters.AddWithValue("@priority_type", model.priority_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating process priority label: {ex.Message}");
            }
        }


        public async Task<bool> DeleteProcessPriorityLabelAsync(DeleteProcessPriorityLabelDTO model, int tenantID, string username)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    
                    string checkQuery = "SELECT COUNT(*) FROM tbl_process_priority_label WHERE ID = @ID AND ctenent_id = @TenantID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ID", model.ID);
                        checkCmd.Parameters.AddWithValue("@TenantID", tenantID);

                        var recordCount = (int)await checkCmd.ExecuteScalarAsync();
                        if (recordCount == 0)
                        {
                            return false;
                        }
                    }

                    
                    string query = "DELETE FROM tbl_process_priority_label WHERE ID = @ID AND ctenent_id = @TenantID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting process priority label: {ex.Message}");
            }
        }


        public async Task<IEnumerable<ParticipantTypeDTO>> GetAllParticipantTypesAsync(int tenantID)
        {
            var result = new List<ParticipantTypeDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    string query = @"
                        SELECT 
                            ID, ctenent_id, participant_type, nis_active,
                            ccreated_by, lcreated_date, cmodified_by, lmodified_date
                        FROM tbl_participant_type 
                        WHERE ctenent_id = @TenantID 
                        AND (nis_active = 1 OR nis_active IS NULL)
                        ORDER BY participant_type";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new ParticipantTypeDTO
                                {
                                    ID = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("ID")),
                                    ctenent_id = reader.IsDBNull(reader.GetOrdinal("ctenent_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                    participant_type = reader.IsDBNull(reader.GetOrdinal("participant_type")) ? string.Empty : reader.GetString(reader.GetOrdinal("participant_type")),
                                    nis_active = reader.IsDBNull(reader.GetOrdinal("nis_active")) ||
                                                (!reader.IsDBNull(reader.GetOrdinal("nis_active")) && reader.GetBoolean(reader.GetOrdinal("nis_active"))),
                                    ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                    lcreated_date = reader.IsDBNull(reader.GetOrdinal("lcreated_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                    cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                    lmodified_date = reader.IsDBNull(reader.GetOrdinal("lmodified_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                                });
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving participant types: {ex.Message}");
            }
        }

      
        public async Task<bool> CreateParticipantTypeAsync(CreateParticipantTypeDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                        INSERT INTO tbl_participant_type 
                            (ctenent_id, participant_type, nis_active, ccreated_by, lcreated_date)
                        VALUES 
                            (@ctenent_id, @participant_type, @nis_active, @ccreated_by, GETDATE());
                        SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id);
                        cmd.Parameters.AddWithValue("@participant_type", model.participant_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);

                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null && Convert.ToInt32(newId) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating participant type: {ex.Message}");
            }
        }

        public async Task<bool> UpdateParticipantTypeAsync(UpdateParticipantTypeDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                        UPDATE tbl_participant_type 
                        SET 
                            participant_type = @participant_type,
                            nis_active = @nis_active,
                            cmodified_by = @cmodified_by,
                            lmodified_date = GETDATE()
                        WHERE ID = @ID AND ctenent_id = @ctenent_id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id); 
                        cmd.Parameters.AddWithValue("@participant_type", model.participant_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating participant type: {ex.Message}");
            }
        }


        public async Task<bool> DeleteParticipantTypeAsync(DeleteParticipantTypeDTO model, int tenantID, string username)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                   
                    string checkQuery = "SELECT COUNT(*) FROM tbl_participant_type WHERE ID = @ID AND ctenent_id = @TenantID";
                    using (SqlCommand checkCmd = new SqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@ID", model.ID);
                        checkCmd.Parameters.AddWithValue("@TenantID", tenantID);

                        var recordCount = (int)await checkCmd.ExecuteScalarAsync();
                        if (recordCount == 0)
                        {
                            return false; 
                        }
                    }

                   
                    string query = "DELETE FROM tbl_participant_type WHERE ID = @ID AND ctenent_id = @TenantID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting participant type: {ex.Message}");
            }
        }


    }
}