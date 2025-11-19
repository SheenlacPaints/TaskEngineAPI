using System.Data;
using System.Data.SqlClient;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Services
{
    public class LookUpService : ILookUpService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<LookUpService> _logger;

        public LookUpService(IConfiguration configuration, ILogger<LookUpService> logger)
        {
            _config = configuration;
            _logger = logger;
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

                    string query = @"
                        SELECT 
                            ID, ctenent_id, notification_type, nis_active,
                            ccreated_by, lcreated_date, cmodified_by, lmodified_date
                        FROM tbl_notification_type 
                        WHERE ctenent_id = @TenantID 
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
                                    nis_active = !reader.IsDBNull(reader.GetOrdinal("nis_active")) && reader.GetBoolean(reader.GetOrdinal("nis_active")),
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
                        SET notification_type = @notification_type,
                            nis_active = @nis_active,
                            cmodified_by = @cmodified_by,
                            lmodified_date = GETDATE()
                        WHERE ID = @ID AND ctenent_id = @ctenent_id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id);
                        cmd.Parameters.AddWithValue("@notification_type", model.notification_type ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@cmodified_by", model.cmodified_by ?? string.Empty);

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

                    string query = "DELETE FROM tbl_notification_type WHERE ID = @ID AND ctenent_id = @tenantId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@tenantId", tenantID);

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
                                    nis_active = !reader.IsDBNull(reader.GetOrdinal("nis_active")) && reader.GetBoolean(reader.GetOrdinal("nis_active")),
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


        public async Task<IEnumerable<ProcessPrivilegeTypeDTO>> GetAllProcessPrivilegeTypesAsync(int tenantID)
        {
            var result = new List<ProcessPrivilegeTypeDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    ID, ctenent_id, cprocess_privilege, nis_active,
                    ccreated_by, lcreated_date, cmodified_by, lmodified_date, slug
                FROM tbl_process_privilege_type 
                WHERE ctenent_id = @TenantID 
                ORDER BY cprocess_privilege";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new ProcessPrivilegeTypeDTO
                                {
                                    ID = reader.IsDBNull(reader.GetOrdinal("ID")) ? 0 : reader.GetInt32(reader.GetOrdinal("ID")),
                                    ctenent_id = reader.IsDBNull(reader.GetOrdinal("ctenent_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("ctenent_id")),
                                    cprocess_privilege = reader.IsDBNull(reader.GetOrdinal("cprocess_privilege")) ? string.Empty : reader.GetString(reader.GetOrdinal("cprocess_privilege")),
                                    nis_active = !reader.IsDBNull(reader.GetOrdinal("nis_active")) && reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                    ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                    lcreated_date = reader.IsDBNull(reader.GetOrdinal("lcreated_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("lcreated_date")),
                                    cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                    lmodified_date = reader.IsDBNull(reader.GetOrdinal("lmodified_date")) ? (DateTime?)null : reader.GetDateTime(reader.GetOrdinal("lmodified_date")),
                                    slug = reader.IsDBNull(reader.GetOrdinal("slug")) ? string.Empty : reader.GetString(reader.GetOrdinal("slug"))
                                });
                            }
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving process privilege types: {ex.Message}");
            }
        }

        public async Task<bool> CreateProcessPrivilegeTypeAsync(CreateProcessPrivilegeTypeDTO model, string usernameclaim)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                INSERT INTO tbl_process_privilege_type 
                    (ctenent_id, cprocess_privilege, nis_active, ccreated_by, lcreated_date)
                VALUES 
                    (@ctenent_id, @cprocess_privilege, @nis_active, @ccreated_by, GETDATE());
                SELECT SCOPE_IDENTITY();";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id);
                        cmd.Parameters.AddWithValue("@cprocess_privilege", model.cprocess_privilege ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@ccreated_by", usernameclaim);

                        var newId = await cmd.ExecuteScalarAsync();
                        return newId != null && Convert.ToInt32(newId) > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating process privilege type: {ex.Message}");
            }
        }

        public async Task<bool> UpdateProcessPrivilegeTypeAsync(UpdateProcessPrivilegeTypeDTO model)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    string query = @"
                UPDATE tbl_process_privilege_type 
                SET cprocess_privilege = @cprocess_privilege,
                    nis_active = @nis_active,
                    cmodified_by = @cmodified_by,
                    lmodified_date = GETDATE()
                WHERE ID = @ID AND ctenent_id = @ctenent_id";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@ctenent_id", model.ctenent_id);
                        cmd.Parameters.AddWithValue("@cprocess_privilege", model.cprocess_privilege ?? string.Empty);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active);
                        cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating process privilege type: {ex.Message}");
            }
        }

        public async Task<bool> DeleteProcessPrivilegeTypeAsync(DeleteProcessPrivilegeTypeDTO model, int tenantID, string username)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();


                    string query = "DELETE FROM tbl_process_privilege_type WHERE ID = @ID AND ctenent_id = @tenantId";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@tenantId", tenantID);

                        var affectedRows = await cmd.ExecuteNonQueryAsync();
                        return affectedRows > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error deleting process privilege type: {ex.Message}");
            }
        }


        public async Task<IEnumerable<PrivilegeItemDTO>> GetPrivilegeListAsync(int tenantID)
        {
            var result = new List<PrivilegeItemDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand("sp_get_dropdown", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@column", "getPrivilegeList");
                        cmd.Parameters.AddWithValue("@tenent", tenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new PrivilegeItemDTO
                                {
                                    value = reader.IsDBNull(reader.GetOrdinal("value")) ? string.Empty : reader.GetString(reader.GetOrdinal("value")),
                                    view_value = reader.IsDBNull(reader.GetOrdinal("view_value")) ? string.Empty : reader.GetString(reader.GetOrdinal("view_value"))
                                });
                            }
                        }
                    }
                }

                result.Add(new PrivilegeItemDTO { value = "0", view_value = "All" });

                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving privilege list: {ex.Message}");
            }
        }

        public async Task<IEnumerable<PrivilegeItemDTO>> GetPrivilegeTypeByIdAsync(int privilegeType, int tenantID)
        {
            var result = new List<PrivilegeItemDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(GetConnectionString()))
                {
                    await conn.OpenAsync();

                    string query = @"           
                        SELECT 
                        
                            ppt.ID as privilege_id,
                            ppt.ctenent_id as privilege_tenant_id,
                            ppt.cprocess_privilege,
                            ppt.nis_active as privilege_is_active,
                           
                        
                            CASE 
                                WHEN ppt.cprocess_privilege = 'department' THEN CAST(dm.cdepartment_code as varchar(50))
                                WHEN ppt.cprocess_privilege = 'position' THEN CAST(pm.cposition_code as varchar(50))
                                WHEN ppt.cprocess_privilege = 'role' THEN CAST(rm.crole_code as varchar(50))
                                WHEN ppt.cprocess_privilege = 'user' THEN CAST(u.cuserid as varchar(50))
                                ELSE CAST(ppt.ID as varchar(10))
                            END as value,
                            
                            CASE 
                                WHEN ppt.cprocess_privilege = 'department' THEN dm.cdepartment_name
                                WHEN ppt.cprocess_privilege = 'position' THEN pm.cposition_name
                                WHEN ppt.cprocess_privilege = 'role' THEN rm.crole_name
                                WHEN ppt.cprocess_privilege = 'user' THEN u.cuser_name
                                ELSE ppt.cprocess_privilege
                            END as view_value
                            
                        FROM tbl_process_privilege_type ppt
                        LEFT JOIN tbl_department_master dm ON ppt.cprocess_privilege = 'department' AND dm.ctenent_id = ppt.ctenent_id AND dm.nis_active = 1
                        LEFT JOIN tbl_position_master pm ON ppt.cprocess_privilege = 'position' AND pm.ctenent_id = ppt.ctenent_id AND pm.nis_active = 1
                        LEFT JOIN tbl_role_master rm ON ppt.cprocess_privilege = 'role' AND rm.ctenent_id = ppt.ctenent_id AND rm.nis_active = 1
                        LEFT JOIN users u ON ppt.cprocess_privilege = 'user' AND u.ctenant_id = ppt.ctenent_id AND u.nis_active = 1 and u.nIs_deleted=0
                        WHERE ppt.ctenent_id = @tenantId 
                        AND ppt.nis_active = 1
                        AND (@PrivilegeType  = '0' OR ppt.ID = @PrivilegeType )
                        ORDER BY view_value;";
                        
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", tenantID);
                        cmd.Parameters.AddWithValue("@PrivilegeType", privilegeType);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                result.Add(new PrivilegeItemDTO
                                {
                                    value = reader.IsDBNull(reader.GetOrdinal("value")) ? string.Empty : reader.GetString(reader.GetOrdinal("value")),
                                    view_value = reader.IsDBNull(reader.GetOrdinal("view_value")) ? string.Empty : reader.GetString(reader.GetOrdinal("view_value"))
                                });
                            }
                        }
                    }
                }
          
                return result;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error retrieving privileges: {ex.Message}");
            }
        }

    }
}