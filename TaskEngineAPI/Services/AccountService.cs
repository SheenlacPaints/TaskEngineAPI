using System;
using System.Data.SqlClient;
using System.Net;
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
                ( cTenantID, cfirstName, clastName, cusername, cemail, cphoneno, 
                 cpassword, croleID, nisActive, llastLoginAt, cPasswordChangedAt, 
                 cLastLoginIP, cLastLoginDevice)
                VALUES 
                (@TenantID, @FirstName, @LastName, @Username, @Email, @PhoneNo, 
                 @Password, @RoleID, @IsActive, @LastLoginAt, @PasswordChangedAt, 
                 @LastLoginIP, @LastLoginDevice);
                 SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);

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
                    var newId = await cmd.ExecuteScalarAsync();
                    return newId != null ? Convert.ToInt32(newId) : 0;
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
            SELECT [ID],  [cTenantID], [cfirstName], [clastName], [cusername],
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

                              ID = reader.GetInt32(reader.GetOrdinal("ID")),
                              cTenantID = reader.GetInt32(reader.GetOrdinal("cTenantID")),
                              cfirstName = reader.GetString(reader.GetOrdinal("cfirstName")),
                              clastName = reader.GetString(reader.GetOrdinal("clastName")),
                              cusername = reader.GetString(reader.GetOrdinal("cusername")),
                              cemail = reader.GetString(reader.GetOrdinal("cemail")),
                              cphoneno = reader.IsDBNull(reader.GetOrdinal("cphoneno")) ? null : reader.GetString(reader.GetOrdinal("cphoneno")),
                              cpassword = reader.GetString(reader.GetOrdinal("cpassword")),
                              croleID = reader.GetInt32(reader.GetOrdinal("croleID")),
                              nisActive = reader.GetBoolean(reader.GetOrdinal("nisActive")),
                              llastLoginAt = reader.IsDBNull(reader.GetOrdinal("llastLoginAt")) ? null : reader.GetDateTime(reader.GetOrdinal("llastLoginAt")),
                              lfailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("lfailedLoginAttempts")) ? null : reader.GetInt32(reader.GetOrdinal("lfailedLoginAttempts")),
                              cPasswordChangedAt = reader.IsDBNull(reader.GetOrdinal("cPasswordChangedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("cPasswordChangedAt")),
                              cMustChangePassword = reader.IsDBNull(reader.GetOrdinal("cMustChangePassword")) ? null : reader.GetBoolean(reader.GetOrdinal("cMustChangePassword")),
                              cLastLoginIP = reader.IsDBNull(reader.GetOrdinal("cLastLoginIP")) ? null : reader.GetString(reader.GetOrdinal("cLastLoginIP")),
                              cLastLoginDevice = reader.IsDBNull(reader.GetOrdinal("cLastLoginDevice")) ? null : reader.GetString(reader.GetOrdinal("cLastLoginDevice"))
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
        WHERE ID = @ID AND  cTenantID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", (object?)model.cusername ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.cemail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", (object?)model.cpassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nisActive ?? true);

                    cmd.Parameters.AddWithValue("@ID", model.cid);                 
                    cmd.Parameters.AddWithValue("@TenantID", model.cTenantID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }


        public async Task<bool> DeleteSuperAdminAsync(DeleteAdminDTO model, int cTenantID)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        DELETE FROM AdminUsers
        WHERE ID = @cuserid AND cTenantID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cuserid", model.cid);
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }



        public async Task<int> InsertUserAsync(CreateUserDTO model)
        {
            var connStr = _config.GetConnectionString("Database");
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            string query = @"INSERT INTO Users (
        cuserid, ctenantID, cusername, cemail, cpassword, nIsActive, cfirstName, clastName, cphoneno, cAlternatePhone,
        ldob, cMaritalStatus, cnation, cgender, caddress, caddress1, caddress2, cpincode, ccity, cstatecode, cstatedesc,
        ccountrycode, ProfileImage, cbankName, caccountNumber, ciFSCCode, cpAN, ldoj, cemploymentStatus, nnoticePeriodDays,
        lresignationDate, llastWorkingDate, cempcategory, cworkloccode, cworklocname, croleID, crolecode, crolename,
        cgradecode, cgradedesc, csubrolecode, cdeptcode, cdeptdesc, cjobcode, cjobdesc, creportmgrcode, creportmgrname,
        cRoll_id, cRoll_name, cRoll_Id_mngr, cRoll_Id_mngr_desc, cReportManager_empcode, cReportManager_Poscode,
        cReportManager_Posdesc, nIsWebAccessEnabled, nIsEventRead, lLastLoginAt, nFailedLoginAttempts, cPasswordChangedAt,
        nIsLocked, LastLoginIP, LastLoginDevice, ccreateddate, ccreatedby, cmodifiedby, lmodifieddate, nIsDeleted,
        cDeletedBy, lDeletedDate)
        VALUES (
        @cuserid, @ctenantID, @cusername, @cemail, @cpassword, @nIsActive, @cfirstName, @clastName, @cphoneno, @cAlternatePhone,
        @ldob, @cMaritalStatus, @cnation, @cgender, @caddress, @caddress1, @caddress2, @cpincode, @ccity, @cstatecode, @cstatedesc,
        @ccountrycode, @ProfileImage, @cbankName, @caccountNumber, @ciFSCCode, @cpAN, @ldoj, @cemploymentStatus, @nnoticePeriodDays,
        @lresignationDate, @llastWorkingDate, @cempcategory, @cworkloccode, @cworklocname, @croleID, @crolecode, @crolename,
        @cgradecode, @cgradedesc, @csubrolecode, @cdeptcode, @cdeptdesc, @cjobcode, @cjobdesc, @creportmgrcode, @creportmgrname,
        @cRoll_id, @cRoll_name, @cRoll_Id_mngr, @cRoll_Id_mngr_desc, @cReportManager_empcode, @cReportManager_Poscode,
        @cReportManager_Posdesc, @nIsWebAccessEnabled, @nIsEventRead, @lLastLoginAt, @nFailedLoginAttempts, @cPasswordChangedAt,
        @nIsLocked, @LastLoginIP, @LastLoginDevice, @ccreateddate, @ccreatedby, @cmodifiedby, @lmodifieddate, @nIsDeleted,
        @cDeletedBy, @lDeletedDate)";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@cuserid", model.cuserid);
            cmd.Parameters.AddWithValue("@ctenantID", model.ctenantID);
            cmd.Parameters.AddWithValue("@cusername", model.cusername);
            cmd.Parameters.AddWithValue("@cemail", model.cemail);
            cmd.Parameters.AddWithValue("@cpassword", model.cpassword); // store as plain text
            cmd.Parameters.AddWithValue("@nIsActive", model.nIsActive ?? true);
            cmd.Parameters.AddWithValue("@cfirstName", (object?)model.cfirstName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@clastName", (object?)model.clastName ?? DBNull.Value);                     
            cmd.Parameters.AddWithValue("@cphoneno", (object?)model.cphoneno ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cAlternatePhone", (object?)model.cAlternatePhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ldob", (object?)model.ldob ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cMaritalStatus", (object?)model.cMaritalStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cnation", (object?)model.cnation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cgender", (object?)model.cgender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddress", (object?)model.caddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddress1", (object?)model.caddress1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddress2", (object?)model.caddress2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cpincode", (object?)model.cpincode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ccity", (object?)model.ccity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cstatecode", (object?)model.cstatecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cstatedesc", (object?)model.cstatedesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ccountrycode", (object?)model.ccountrycode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfileImage", (object?)model.ProfileImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cbankName", (object?)model.cbankName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caccountNumber", (object?)model.caccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ciFSCCode", (object?)model.ciFSCCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cpAN", (object?)model.cpAN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ldoj", (object?)model.ldoj ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cemploymentStatus", (object?)model.cemploymentStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nnoticePeriodDays", (object?)model.nnoticePeriodDays ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lresignationDate", (object?)model.lresignationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@llastWorkingDate", (object?)model.llastWorkingDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cempcategory", (object?)model.cempcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cworkloccode", (object?)model.cworkloccode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cworklocname", (object?)model.cworklocname ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@croleID", (object?)model.croleID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@crolecode", (object?)model.crolecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@crolename", (object?)model.crolename ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cgradecode", (object?)model.cgradecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cgradedesc", (object?)model.cgradedesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@csubrolecode", (object?)model.csubrolecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cdeptcode", (object?)model.cdeptcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cdeptdesc", (object?)model.cdeptdesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cjobcode", (object?)model.cjobcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cjobdesc", (object?)model.cjobdesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@creportmgrcode", (object?)model.creportmgrcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@creportmgrname", (object?)model.creportmgrname ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_id", (object?)model.cRoll_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_name", (object?)model.cRoll_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_Id_mngr", (object?)model.cRoll_Id_mngr ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_Id_mngr_desc", (object?)model.cRoll_Id_mngr_desc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cReportManager_empcode", (object?)model.cReportManager_empcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cReportManager_Poscode", (object?)model.cReportManager_Poscode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cReportManager_Posdesc", (object?)model.cReportManager_Posdesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsWebAccessEnabled", (object?)model.nIsWebAccessEnabled ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsEventRead", (object?)model.nIsEventRead ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lLastLoginAt", (object?)model.lLastLoginAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nFailedLoginAttempts", (object?)model.nFailedLoginAttempts ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cPasswordChangedAt", (object?)model.cPasswordChangedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsLocked", (object?)model.nIsLocked ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LastLoginIP", (object?)model.LastLoginIP ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LastLoginDevice", (object?)model.LastLoginDevice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ccreateddate", (object?)model.ccreateddate ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@ccreatedby", (object?)model.ccreatedby ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cmodifiedby", (object?)model.cmodifiedby ??  DBNull.Value);
            cmd.Parameters.AddWithValue("@lmodifieddate", (object?)model.lmodifieddate ?? model.ccreateddate ?? DateTime.UtcNow);
            cmd.Parameters.AddWithValue("@nIsDeleted", (object?)model.nIsDeleted ?? false);
            cmd.Parameters.AddWithValue("@cDeletedBy", (object?)model.cDeletedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lDeletedDate", (object?)model.lDeletedDate ?? DBNull.Value);
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0 ? model.cuserid : 0;
        }

        public async Task<bool> UpdateUserAsync(UpdateUserDTO model)
        {
            var connStr = _config.GetConnectionString("Database");
            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            string query = @"UPDATE Users SET
        cusername = @cusername,
        cemail = @cemail,
        cpassword = @cpassword,
        nIsActive = @nIsActive,
        cfirstName = @cfirstName,
        clastName = @clastName,
        cphoneno = @cphoneno,
        cAlternatePhone = @cAlternatePhone,
        ldob = @ldob,
        cMaritalStatus = @cMaritalStatus,
        cnation = @cnation,
        cgender = @cgender,
        caddress = @caddress,
        caddress1 = @caddress1,
        caddress2 = @caddress2,
        cpincode = @cpincode,
        ccity = @ccity,
        cstatecode = @cstatecode,
        cstatedesc = @cstatedesc,
        ccountrycode = @ccountrycode,
        ProfileImage = @ProfileImage,
        cbankName = @cbankName,
        caccountNumber = @caccountNumber,
        ciFSCCode = @ciFSCCode,
        cpAN = @cpAN,
        ldoj = @ldoj,
        cemploymentStatus = @cemploymentStatus,
        nnoticePeriodDays = @nnoticePeriodDays,
        lresignationDate = @lresignationDate,
        llastWorkingDate = @llastWorkingDate,
        cempcategory = @cempcategory,
        cworkloccode = @cworkloccode,
        cworklocname = @cworklocname,
        croleID = @croleID,
        crolecode = @crolecode,
        crolename = @crolename,
        cgradecode = @cgradecode,
        cgradedesc = @cgradedesc,
        csubrolecode = @csubrolecode,
        cdeptcode = @cdeptcode,
        cdeptdesc = @cdeptdesc,
        cjobcode = @cjobcode,
        cjobdesc = @cjobdesc,
        creportmgrcode = @creportmgrcode,
        creportmgrname = @creportmgrname,
        cRoll_id = @cRoll_id,
        cRoll_name = @cRoll_name,
        cRoll_Id_mngr = @cRoll_Id_mngr,
        cRoll_Id_mngr_desc = @cRoll_Id_mngr_desc,
        cReportManager_empcode = @cReportManager_empcode,
        cReportManager_Poscode = @cReportManager_Poscode,
        cReportManager_Posdesc = @cReportManager_Posdesc,
        nIsWebAccessEnabled = @nIsWebAccessEnabled,
        nIsEventRead = @nIsEventRead,
        lLastLoginAt = @lLastLoginAt,
        nFailedLoginAttempts = @nFailedLoginAttempts,
        cPasswordChangedAt = @cPasswordChangedAt,
        nIsLocked = @nIsLocked,
        LastLoginIP = @LastLoginIP,
        LastLoginDevice = @LastLoginDevice,
        cmodifiedby = @cmodifiedby,
        lmodifieddate = @lmodifieddate,
        nIsDeleted = @nIsDeleted,
        cDeletedBy = @cDeletedBy,
        lDeletedDate = @lDeletedDate
        WHERE cuserid = @cuserid AND ctenantID = @ctenantID";

            using var cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@cuserid", model.cuserid);
            cmd.Parameters.AddWithValue("@ctenantID", model.ctenantID);
            cmd.Parameters.AddWithValue("@cusername", (object?)model.cusername ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cemail", (object?)model.cemail ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cpassword", (object?)model.cpassword ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsActive", (object?)model.nIsActive ?? true);
            cmd.Parameters.AddWithValue("@cfirstName", (object?)model.cfirstName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@clastName", (object?)model.clastName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cphoneno", (object?)model.cphoneno ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cAlternatePhone", (object?)model.cAlternatePhone ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ldob", (object?)model.ldob ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cMaritalStatus", (object?)model.cMaritalStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cnation", (object?)model.cnation ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cgender", (object?)model.cgender ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddress", (object?)model.caddress ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddress1", (object?)model.caddress1 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caddress2", (object?)model.caddress2 ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cpincode", (object?)model.cpincode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ccity", (object?)model.ccity ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cstatecode", (object?)model.cstatecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cstatedesc", (object?)model.cstatedesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ccountrycode", (object?)model.ccountrycode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ProfileImage", (object?)model.ProfileImage ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cbankName", (object?)model.cbankName ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@caccountNumber", (object?)model.caccountNumber ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ciFSCCode", (object?)model.ciFSCCode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cpAN", (object?)model.cpAN ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@ldoj", (object?)model.ldoj ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cemploymentStatus", (object?)model.cemploymentStatus ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nnoticePeriodDays", (object?)model.nnoticePeriodDays ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lresignationDate", (object?)model.lresignationDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@llastWorkingDate", (object?)model.llastWorkingDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cempcategory", (object?)model.cempcategory ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cworkloccode", (object?)model.cworkloccode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cworklocname", (object?)model.cworklocname ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@croleID", (object?)model.croleID ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@crolecode", (object?)model.crolecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@crolename", (object?)model.crolename ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cgradecode", (object?)model.cgradecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cgradedesc", (object?)model.cgradedesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@csubrolecode", (object?)model.csubrolecode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cdeptcode", (object?)model.cdeptcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cdeptdesc", (object?)model.cdeptdesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cjobcode", (object?)model.cjobcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cjobdesc", (object?)model.cjobdesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@creportmgrcode", (object?)model.creportmgrcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@creportmgrname", (object?)model.creportmgrname ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_id", (object?)model.cRoll_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_name", (object?)model.cRoll_name ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_Id_mngr", (object?)model.cRoll_Id_mngr ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cRoll_Id_mngr_desc", (object?)model.cRoll_Id_mngr_desc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cReportManager_empcode", (object?)model.cReportManager_empcode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cReportManager_Poscode", (object?)model.cReportManager_Poscode ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cReportManager_Posdesc", (object?)model.cReportManager_Posdesc ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsWebAccessEnabled", (object?)model.nIsWebAccessEnabled ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsEventRead", (object?)model.nIsEventRead ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lLastLoginAt", (object?)model.lLastLoginAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nFailedLoginAttempts", (object?)model.nFailedLoginAttempts ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cPasswordChangedAt", (object?)model.cPasswordChangedAt ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsLocked", (object?)model.nIsLocked ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LastLoginIP", (object?)model.LastLoginIP ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@LastLoginDevice", (object?)model.LastLoginDevice ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cmodifiedby", (object?)model.cmodifiedby ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lmodifieddate", (object?)model.lmodifieddate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@nIsDeleted", (object?)model.nIsDeleted ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cDeletedBy", (object?)model.cDeletedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lDeletedDate", (object?)model.lDeletedDate ?? DBNull.Value);

            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;

        }
        }
}