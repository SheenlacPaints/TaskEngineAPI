using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Repositories;

namespace TaskEngineAPI.Services
{


    public class AccountService : IAdminService
    {
        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;

        public AccountService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
        }

        public Task<APIResponse> CreateSuperAdminAsync(CreateAdminDTO model)
        {
            throw new NotImplementedException();
        }

        public async Task<int> InsertSuperAdminAsync(CreateAdminDTO model)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
                INSERT INTO AdminUsers 
                (cuserid, cTenantID, cfirstName, clastName, cusername, cemail, cphoneno, 
                 cpassword, croleID, nisActive, llastLoginAt, cPasswordChangedAt, 
                 cLastLoginIP, cLastLoginDevice)
                VALUES 
                (@cuserid, @TenantID, @FirstName, @LastName, @Username, @Email, @PhoneNo, 
                 @Password, @RoleID, @IsActive, @LastLoginAt, @PasswordChangedAt, 
                 @LastLoginIP, @LastLoginDevice)";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cuserid", model.cuserid);
                    cmd.Parameters.AddWithValue("@TenantID", model.cTenantID);
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", model.cusername);
                    cmd.Parameters.AddWithValue("@Email", model.cemail);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", model.cpassword); // store as plain text
                    cmd.Parameters.AddWithValue("@RoleID", (object?)model.croleID ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nisActive ?? true);
                    cmd.Parameters.AddWithValue("@LastLoginAt", (object?)model.llastLoginAt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PasswordChangedAt", (object?)model.cPasswordChangedAt ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastLoginIP", (object?)model.cLastLoginIP ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastLoginDevice", (object?)model.cLastLoginDevice ?? DBNull.Value);

                    int rows = await cmd.ExecuteNonQueryAsync();

                    return rows > 0 ? model.cuserid : 0;
                }
            }
        }


        public async Task<List<AdminUserDTO>> GetAllSuperAdminsAsync(int cTenantID)

        {
            var result = new List<AdminUserDTO>();
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
            SELECT [ID], [cuserid], [cTenantID], [cfirstName], [clastName], [cusername],
                   [cemail], [cphoneno], [cpassword], [croleID], [nisActive], [llastLoginAt],
                   [lfailedLoginAttempts], [cPasswordChangedAt], [cMustChangePassword],
                   [cLastLoginIP], [cLastLoginDevice]
            FROM [dbo].[AdminUsers] WHERE croleID = 2 AND cTenantID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new AdminUserDTO
                            {
                                ID = reader.GetInt32(0),
                                cuserid = reader.GetInt32(1),
                                cTenantID = reader.GetInt32(2),
                                cfirstName = reader.GetString(3),
                                clastName = reader.GetString(4),
                                cusername = reader.GetString(5),
                                cemail = reader.GetString(6),
                                cphoneno = reader.IsDBNull(7) ? null : reader.GetString(7),
                                cpassword = reader.GetString(8),
                                croleID = reader.GetInt32(9),
                                nisActive = reader.GetBoolean(10),
                                llastLoginAt = reader.IsDBNull(11) ? null : reader.GetDateTime(11),
                                lfailedLoginAttempts = reader.IsDBNull(12) ? null : reader.GetInt32(12),
                                cPasswordChangedAt = reader.IsDBNull(13) ? null : reader.GetDateTime(13),
                                cMustChangePassword = reader.IsDBNull(14) ? null : reader.GetBoolean(14),
                                cLastLoginIP = reader.IsDBNull(15) ? null : reader.GetString(15),
                                cLastLoginDevice = reader.IsDBNull(16) ? null : reader.GetString(16)
                            });
                        }
                    }
                }


                return result;
            }





        }



        public async Task<bool> UpdateSuperAdminAsync(UpdateAdminDTO model)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        UPDATE AdminUsers SET
            cfirstName = @FirstName,
            clastName = @LastName,
            cusername = @Username,
            cemail = @Email,
            cphoneno = @PhoneNo,
            cpassword = @Password,
            nisActive = @IsActive
        WHERE ID = @ID AND cuserid = @cuserid AND cTenantID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", (object?)model.cusername ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.cemail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", (object?)model.cpassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nisActive ?? true);

                    cmd.Parameters.AddWithValue("@ID", model.ID);
                    cmd.Parameters.AddWithValue("@cuserid", model.cuserid);
                    cmd.Parameters.AddWithValue("@TenantID", model.cTenantID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }


        public async Task<bool> DeleteSuperAdminAsync(DeleteAdminDTO model)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        DELETE FROM AdminUsers
        WHERE cuserid = @cuserid AND cTenantID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cuserid", model.cuserid);
                    cmd.Parameters.AddWithValue("@TenantID", model.cTenantID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }



    }
}