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

        public AccountService(IAdminRepository repository,IConfiguration _configuration, IAdminRepository AdminRepository)
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



    }



}
