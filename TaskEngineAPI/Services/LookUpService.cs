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

        public async Task<IEnumerable<NotificationTypeDTO>> GetAllNotificationTypesAsync(int tenantID)
        {
            var connStr = _config.GetConnectionString("Database");
            var result = new List<NotificationTypeDTO>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        ID, ctenent_id, notification_type, nis_active,
                        ccreated_by, lcreated_date, cmodified_by, lmodified_date
                    FROM tbl_notification_type 
                    WHERE ctenent_id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", tenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new NotificationTypeDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ctenent_id = reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                notification_type = reader.GetString(reader.GetOrdinal("notification_type")),
                                nis_active = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                lcreated_date = reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                            });
                        }
                    }
                }
            }
            return result;
        }

        public async Task<NotificationTypeDTO> GetNotificationTypeByIdAsync(int id, int tenantID)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        ID, ctenent_id, notification_type, nis_active,
                        ccreated_by, lcreated_date, cmodified_by, lmodified_date
                    FROM tbl_notification_type 
                    WHERE ID = @Id AND ctenent_id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantID", tenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new NotificationTypeDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ctenent_id = reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                notification_type = reader.GetString(reader.GetOrdinal("notification_type")),
                                nis_active = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                lcreated_date = reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> CreateNotificationTypeAsync(CreateNotificationTypeDTO model)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
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
                    cmd.Parameters.AddWithValue("@notification_type", model.notification_type);
                    cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                    cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);

                    var newId = await cmd.ExecuteScalarAsync();
                    return newId != null ? Convert.ToInt32(newId) > 0 : false;
                }
            }
        }

        public async Task<bool> UpdateNotificationTypeAsync(UpdateNotificationTypeDTO model)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    UPDATE tbl_notification_type 
                    SET 
                        notification_type = @notification_type,
                        nis_active = @nis_active,
                        cmodified_by = @cmodified_by,
                        lmodified_date = GETDATE()
                    WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", model.ID);
                    cmd.Parameters.AddWithValue("@notification_type", model.notification_type);
                    cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<bool> DeleteNotificationTypeAsync(DeleteNotificationTypeDTO model, int tenantID, string username)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
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

        public async Task<IEnumerable<ProcessPriorityLabelDTO>> GetAllProcessPriorityLabelsAsync(int tenantID)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            var result = new List<ProcessPriorityLabelDTO>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        ID, ctenent_id, priority_type, nis_active,
                        ccreated_by, lcreated_date, cmodified_by, lmodified_date
                    FROM tbl_process_priority_label 
                    WHERE ctenent_id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", tenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new ProcessPriorityLabelDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ctenent_id = reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                priority_type = reader.GetString(reader.GetOrdinal("priority_type")),
                                nis_active = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                lcreated_date = reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                            });
                        }
                    }
                }
            }
            return result;
        }

        public async Task<ProcessPriorityLabelDTO> GetProcessPriorityLabelByIdAsync(int id, int tenantID)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        ID, ctenent_id, priority_type, nis_active,
                        ccreated_by, lcreated_date, cmodified_by, lmodified_date
                    FROM tbl_process_priority_label 
                    WHERE ID = @Id AND ctenent_id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantID", tenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new ProcessPriorityLabelDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ctenent_id = reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                priority_type = reader.GetString(reader.GetOrdinal("priority_type")),
                                nis_active = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                lcreated_date = reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> CreateProcessPriorityLabelAsync(CreateProcessPriorityLabelDTO model)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
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
                    cmd.Parameters.AddWithValue("@priority_type", model.priority_type);
                    cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                    cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);

                    var newId = await cmd.ExecuteScalarAsync();
                    return newId != null ? Convert.ToInt32(newId) > 0 : false;
                }
            }
        }

        public async Task<bool> UpdateProcessPriorityLabelAsync(UpdateProcessPriorityLabelDTO model)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    UPDATE tbl_process_priority_label 
                    SET 
                        priority_type = @priority_type,
                        nis_active = @nis_active,
                        cmodified_by = @cmodified_by,
                        lmodified_date = GETDATE()
                    WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", model.ID);
                    cmd.Parameters.AddWithValue("@priority_type", model.priority_type);
                    cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<bool> DeleteProcessPriorityLabelAsync(DeleteProcessPriorityLabelDTO model, int tenantID, string username)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
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

        public async Task<IEnumerable<ParticipantTypeDTO>> GetAllParticipantTypesAsync(int tenantID)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");
            var result = new List<ParticipantTypeDTO>();

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        ID, ctenent_id, participant_type, nis_active,
                        ccreated_by, lcreated_date, cmodified_by, lmodified_date
                    FROM tbl_participant_type 
                    WHERE ctenent_id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", tenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new ParticipantTypeDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ctenent_id = reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                participant_type = reader.GetString(reader.GetOrdinal("participant_type")),
                                nis_active = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                lcreated_date = reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                            });
                        }
                    }
                }
            }
            return result;
        }

        public async Task<ParticipantTypeDTO> GetParticipantTypeByIdAsync(int id, int tenantID)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    SELECT 
                        ID, ctenent_id, participant_type, nis_active,
                        ccreated_by, lcreated_date, cmodified_by, lmodified_date
                    FROM tbl_participant_type 
                    WHERE ID = @Id AND ctenent_id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    cmd.Parameters.AddWithValue("@TenantID", tenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new ParticipantTypeDTO
                            {
                                ID = reader.GetInt32(reader.GetOrdinal("ID")),
                                ctenent_id = reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                participant_type = reader.GetString(reader.GetOrdinal("participant_type")),
                                nis_active = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                lcreated_date = reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.GetDateTime(reader.GetOrdinal("lmodified_date"))
                            };
                        }
                    }
                }
            }
            return null;
        }

        public async Task<bool> CreateParticipantTypeAsync(CreateParticipantTypeDTO model)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
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
                    cmd.Parameters.AddWithValue("@participant_type", model.participant_type);
                    cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                    cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);

                    var newId = await cmd.ExecuteScalarAsync();
                    return newId != null ? Convert.ToInt32(newId) > 0 : false;
                }
            }
        }

        public async Task<bool> UpdateParticipantTypeAsync(UpdateParticipantTypeDTO model)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"
                    UPDATE tbl_participant_type 
                    SET 
                        participant_type = @participant_type,
                        nis_active = @nis_active,
                        cmodified_by = @cmodified_by,
                        lmodified_date = GETDATE()
                    WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID", model.ID);
                    cmd.Parameters.AddWithValue("@participant_type", model.participant_type);
                    cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                    var affectedRows = await cmd.ExecuteNonQueryAsync();
                    return affectedRows > 0;
                }
            }
        }

        public async Task<bool> DeleteParticipantTypeAsync(DeleteParticipantTypeDTO model, int tenantID, string username)
        {
            var connStr = _config.GetConnectionString("DefaultConnection");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
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
    }
}