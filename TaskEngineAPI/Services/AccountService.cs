using System;
using System.Data.SqlClient;
using System.Net;
using System.Reflection.PortableExecutable;
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
            cmd.Parameters.AddWithValue("@ccreateddate", (DateTime.Now));
            cmd.Parameters.AddWithValue("@ccreatedby", (object?)model.ccreatedby ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@cmodifiedby", (object?)model.cmodifiedby ??  DBNull.Value);
            cmd.Parameters.AddWithValue("@lmodifieddate", (DateTime.Now));
            cmd.Parameters.AddWithValue("@nIsDeleted", (object?)model.nIsDeleted ?? false);
            cmd.Parameters.AddWithValue("@cDeletedBy", (object?)model.cDeletedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lDeletedDate", (object?)model.lDeletedDate ?? DBNull.Value);


            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0 ? model.cuserid : 0;
        }

        public async Task<bool> UpdateUserAsync(UpdateUserDTO model, int cTenantID)
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
            cmd.Parameters.AddWithValue("@ctenantID", cTenantID);
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


        public async Task<List<GetUserDTO>> GetAllUserAsync(int cTenantID)
        {
            var result = new List<GetUserDTO>();
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        SELECT [ID], [cuserid], [ctenantID], [cusername], [cemail], [cpassword], [nIsActive],
               [cfirstName], [clastName], [cphoneno], [cAlternatePhone], [ldob], [cMaritalStatus],
               [cnation], [cgender], [caddress], [caddress1], [caddress2], [cpincode], [ccity],
               [cstatecode], [cstatedesc], [ccountrycode], [ProfileImage], [cbankName],
               [caccountNumber], [ciFSCCode], [cpAN], [ldoj], [cemploymentStatus],
               [nnoticePeriodDays], [lresignationDate], [llastWorkingDate], [cempcategory],
               [cworkloccode], [cworklocname], [croleID], [crolecode], [crolename], [cgradecode],
               [cgradedesc], [csubrolecode], [cdeptcode], [cdeptdesc], [cjobcode], [cjobdesc],
               [creportmgrcode], [creportmgrname], [cRoll_id], [cRoll_name], [cRoll_Id_mngr],
               [cRoll_Id_mngr_desc], [cReportManager_empcode], [cReportManager_Poscode],
               [cReportManager_Posdesc], [nIsWebAccessEnabled], [nIsEventRead], [lLastLoginAt],
               [nFailedLoginAttempts], [cPasswordChangedAt], [nIsLocked], [LastLoginIP],
               [LastLoginDevice], [ccreateddate], [ccreatedby], [cmodifiedby], [lmodifieddate],
               [nIsDeleted], [cDeletedBy], [lDeletedDate]
        FROM [dbo].[Users]
        WHERE croleID = 3 AND cTenantID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new GetUserDTO
                            {
                                id = reader.GetInt32(reader.GetOrdinal("ID")),
                                cuserid = reader.GetInt32(reader.GetOrdinal("cuserid")),
                                ctenantID = reader.GetInt32(reader.GetOrdinal("ctenantID")),
                                cusername = reader.GetString(reader.GetOrdinal("cusername")),
                                cemail = reader.GetString(reader.GetOrdinal("cemail")),
                                cpassword = reader.GetString(reader.GetOrdinal("cpassword")),
                                nIsActive = reader.GetBoolean(reader.GetOrdinal("nIsActive")),
                                cfirstName = reader.GetString(reader.GetOrdinal("cfirstName")),
                                clastName = reader.GetString(reader.GetOrdinal("clastName")),
                                cphoneno = reader.IsDBNull(reader.GetOrdinal("cphoneno")) ? null : reader.GetString(reader.GetOrdinal("cphoneno")),
                                cAlternatePhone = reader.IsDBNull(reader.GetOrdinal("cAlternatePhone")) ? null : reader.GetString(reader.GetOrdinal("cAlternatePhone")),
                                ldob = reader.IsDBNull(reader.GetOrdinal("ldob")) ? null : reader.GetDateTime(reader.GetOrdinal("ldob")),
                                cMaritalStatus = reader.IsDBNull(reader.GetOrdinal("cMaritalStatus")) ? null : reader.GetString(reader.GetOrdinal("cMaritalStatus")),
                                cnation = reader.IsDBNull(reader.GetOrdinal("cnation")) ? null : reader.GetString(reader.GetOrdinal("cnation")),
                                cgender = reader.IsDBNull(reader.GetOrdinal("cgender")) ? null : reader.GetString(reader.GetOrdinal("cgender")),
                                caddress = reader.IsDBNull(reader.GetOrdinal("caddress")) ? null : reader.GetString(reader.GetOrdinal("caddress")),
                                caddress1 = reader.IsDBNull(reader.GetOrdinal("caddress1")) ? null : reader.GetString(reader.GetOrdinal("caddress1")),
                                caddress2 = reader.IsDBNull(reader.GetOrdinal("caddress2")) ? null : reader.GetString(reader.GetOrdinal("caddress2")),
                                cpincode = reader.GetInt32(reader.GetOrdinal("cpincode")),
                                ccity = reader.IsDBNull(reader.GetOrdinal("ccity")) ? null : reader.GetString(reader.GetOrdinal("ccity")),
                                cstatecode = reader.IsDBNull(reader.GetOrdinal("cstatecode")) ? null : reader.GetString(reader.GetOrdinal("cstatecode")),
                                cstatedesc = reader.IsDBNull(reader.GetOrdinal("cstatedesc")) ? null : reader.GetString(reader.GetOrdinal("cstatedesc")),
                                ccountrycode = reader.IsDBNull(reader.GetOrdinal("ccountrycode")) ? null : reader.GetString(reader.GetOrdinal("ccountrycode")),
                                ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage")) ? null : reader.GetString(reader.GetOrdinal("ProfileImage")),
                                cbankName = reader.IsDBNull(reader.GetOrdinal("cbankName")) ? null : reader.GetString(reader.GetOrdinal("cbankName")),
                                caccountNumber = reader.IsDBNull(reader.GetOrdinal("caccountNumber")) ? null : reader.GetString(reader.GetOrdinal("caccountNumber")),
                                ciFSCCode = reader.IsDBNull(reader.GetOrdinal("ciFSCCode")) ? null : reader.GetString(reader.GetOrdinal("ciFSCCode")),
                                cpAN = reader.IsDBNull(reader.GetOrdinal("cpAN")) ? null : reader.GetString(reader.GetOrdinal("cpAN")),
                                ldoj = reader.IsDBNull(reader.GetOrdinal("ldoj")) ? null : reader.GetDateTime(reader.GetOrdinal("ldoj")),
                                cemploymentStatus = reader.IsDBNull(reader.GetOrdinal("cemploymentStatus")) ? null : reader.GetString(reader.GetOrdinal("cemploymentStatus")),
                                nnoticePeriodDays = reader.IsDBNull(reader.GetOrdinal("nnoticePeriodDays")) ? null : reader.GetInt32(reader.GetOrdinal("nnoticePeriodDays")),
                                lresignationDate = reader.IsDBNull(reader.GetOrdinal("lresignationDate")) ? null : reader.GetDateTime(reader.GetOrdinal("lresignationDate")),
                                llastWorkingDate = reader.IsDBNull(reader.GetOrdinal("llastWorkingDate")) ? null : reader.GetDateTime(reader.GetOrdinal("llastWorkingDate")),
                                cempcategory = reader.IsDBNull(reader.GetOrdinal("cempcategory")) ? null : reader.GetString(reader.GetOrdinal("cempcategory")),
                                cworkloccode = reader.IsDBNull(reader.GetOrdinal("cworkloccode")) ? null : reader.GetString(reader.GetOrdinal("cworkloccode")),
                                cworklocname = reader.IsDBNull(reader.GetOrdinal("cworklocname")) ? null : reader.GetString(reader.GetOrdinal("cworklocname")),
                                croleID = reader.GetInt32(reader.GetOrdinal("croleID")),
                                crolecode = reader.IsDBNull(reader.GetOrdinal("crolecode")) ? null : reader.GetString(reader.GetOrdinal("crolecode")),
                                crolename = reader.IsDBNull(reader.GetOrdinal("crolename")) ? null : reader.GetString(reader.GetOrdinal("crolename")),
                                cgradecode = reader.IsDBNull(reader.GetOrdinal("cgradecode")) ? null : reader.GetString(reader.GetOrdinal("cgradecode")),
                                cgradedesc = reader.IsDBNull(reader.GetOrdinal("cgradedesc")) ? null : reader.GetString(reader.GetOrdinal("cgradedesc")),
                                csubrolecode = reader.IsDBNull(reader.GetOrdinal("csubrolecode")) ? null : reader.GetString(reader.GetOrdinal("csubrolecode")),
                                cdeptcode = reader.IsDBNull(reader.GetOrdinal("cdeptcode")) ? null : reader.GetString(reader.GetOrdinal("cdeptcode")),
                                cdeptdesc = reader.IsDBNull(reader.GetOrdinal("cdeptdesc")) ? null : reader.GetString(reader.GetOrdinal("cdeptdesc")),
                                cjobcode = reader.IsDBNull(reader.GetOrdinal("cjobcode")) ? null : reader.GetString(reader.GetOrdinal("cjobcode")),
                                cjobdesc = reader.IsDBNull(reader.GetOrdinal("cjobdesc")) ? null : reader.GetString(reader.GetOrdinal("cjobdesc")),
                                creportmgrcode = reader.IsDBNull(reader.GetOrdinal("creportmgrcode")) ? null : reader.GetString(reader.GetOrdinal("creportmgrcode")),
                                creportmgrname = reader.IsDBNull(reader.GetOrdinal("creportmgrname")) ? null : reader.GetString(reader.GetOrdinal("creportmgrname")),
                                cRoll_id = reader.IsDBNull(reader.GetOrdinal("cRoll_id")) ? null : reader.GetString(reader.GetOrdinal("cRoll_id")),
                                cRoll_name = reader.IsDBNull(reader.GetOrdinal("cRoll_name")) ? null : reader.GetString(reader.GetOrdinal("cRoll_name")),
                                cRoll_Id_mngr = reader.IsDBNull(reader.GetOrdinal("cRoll_Id_mngr")) ? null : reader.GetString(reader.GetOrdinal("cRoll_Id_mngr")),
                                cRoll_Id_mngr_desc = reader.IsDBNull(reader.GetOrdinal("cRoll_Id_mngr_desc")) ? null : reader.GetString(reader.GetOrdinal("cRoll_Id_mngr_desc")),
                                cReportManager_empcode = reader.IsDBNull(reader.GetOrdinal("cReportManager_empcode")) ? null : reader.GetString(reader.GetOrdinal("cReportManager_empcode")),
                                cReportManager_Poscode = reader.IsDBNull(reader.GetOrdinal("cReportManager_Poscode")) ? null : reader.GetString(reader.GetOrdinal("cReportManager_Poscode")),
                                cReportManager_Posdesc = reader.IsDBNull(reader.GetOrdinal("cReportManager_Posdesc")) ? null : reader.GetString(reader.GetOrdinal("cReportManager_Posdesc")),
                                nIsWebAccessEnabled = reader.IsDBNull(reader.GetOrdinal("nIsWebAccessEnabled")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsWebAccessEnabled")),
                                nIsEventRead = reader.IsDBNull(reader.GetOrdinal("nIsEventRead")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsEventRead")),
                                lLastLoginAt = reader.IsDBNull(reader.GetOrdinal("lLastLoginAt")) ? null : reader.GetDateTime(reader.GetOrdinal("lLastLoginAt")),
                                nFailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("nFailedLoginAttempts")) ? null : reader.GetInt32(reader.GetOrdinal("nFailedLoginAttempts")),
                                cPasswordChangedAt = reader.IsDBNull(reader.GetOrdinal("cPasswordChangedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("cPasswordChangedAt")),
                                nIsLocked = reader.IsDBNull(reader.GetOrdinal("nIsLocked")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsLocked")),
                                LastLoginIP = reader.IsDBNull(reader.GetOrdinal("LastLoginIP")) ? null : reader.GetString(reader.GetOrdinal("LastLoginIP")),
                                LastLoginDevice = reader.IsDBNull(reader.GetOrdinal("LastLoginDevice")) ? null : reader.GetString(reader.GetOrdinal("LastLoginDevice")),
                                ccreateddate = reader.IsDBNull(reader.GetOrdinal("ccreateddate")) ? null : reader.GetDateTime(reader.GetOrdinal("ccreateddate")),
                                ccreatedby = reader.IsDBNull(reader.GetOrdinal("ccreatedby")) ? null : reader.GetString(reader.GetOrdinal("ccreatedby")),
                                cmodifiedby = reader.IsDBNull(reader.GetOrdinal("cmodifiedby")) ? null : reader.GetString(reader.GetOrdinal("cmodifiedby")),
                                lmodifieddate = reader.IsDBNull(reader.GetOrdinal("lmodifieddate")) ? null : reader.GetDateTime(reader.GetOrdinal("lmodifieddate")),
                                nIsDeleted = reader.IsDBNull(reader.GetOrdinal("nIsDeleted")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsDeleted")),
                                cDeletedBy = reader.IsDBNull(reader.GetOrdinal("cDeletedBy")) ? null : reader.GetString(reader.GetOrdinal("cDeletedBy")),
                                lDeletedDate=reader.IsDBNull(reader.GetOrdinal("lDeletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("lDeletedDate"))
                            });                  
                        }
                    }
                }
                return result;
            }
        }


        public async Task<List<GetUserDTO>> GetAllUserIdAsync(int cTenantID,int userid)
        {
            var result = new List<GetUserDTO>();
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        SELECT [ID], [cuserid], [ctenantID], [cusername], [cemail], [cpassword], [nIsActive],
               [cfirstName], [clastName], [cphoneno], [cAlternatePhone], [ldob], [cMaritalStatus],
               [cnation], [cgender], [caddress], [caddress1], [caddress2], [cpincode], [ccity],
               [cstatecode], [cstatedesc], [ccountrycode], [ProfileImage], [cbankName],
               [caccountNumber], [ciFSCCode], [cpAN], [ldoj], [cemploymentStatus],
               [nnoticePeriodDays], [lresignationDate], [llastWorkingDate], [cempcategory],
               [cworkloccode], [cworklocname], [croleID], [crolecode], [crolename], [cgradecode],
               [cgradedesc], [csubrolecode], [cdeptcode], [cdeptdesc], [cjobcode], [cjobdesc],
               [creportmgrcode], [creportmgrname], [cRoll_id], [cRoll_name], [cRoll_Id_mngr],
               [cRoll_Id_mngr_desc], [cReportManager_empcode], [cReportManager_Poscode],
               [cReportManager_Posdesc], [nIsWebAccessEnabled], [nIsEventRead], [lLastLoginAt],
               [nFailedLoginAttempts], [cPasswordChangedAt], [nIsLocked], [LastLoginIP],
               [LastLoginDevice], [ccreateddate], [ccreatedby], [cmodifiedby], [lmodifieddate],
               [nIsDeleted], [cDeletedBy], [lDeletedDate]
        FROM [dbo].[Users]
        WHERE croleID = 3 AND cTenantID = @TenantID  and cuserid=@userid";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                    cmd.Parameters.AddWithValue("@userid", userid);

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            result.Add(new GetUserDTO
                            {
                                id = reader.GetInt32(reader.GetOrdinal("ID")),
                                cuserid = reader.GetInt32(reader.GetOrdinal("cuserid")),
                                ctenantID = reader.GetInt32(reader.GetOrdinal("ctenantID")),
                                cusername = reader.GetString(reader.GetOrdinal("cusername")),
                                cemail = reader.GetString(reader.GetOrdinal("cemail")),
                                cpassword = reader.GetString(reader.GetOrdinal("cpassword")),
                                nIsActive = reader.GetBoolean(reader.GetOrdinal("nIsActive")),
                                cfirstName = reader.GetString(reader.GetOrdinal("cfirstName")),
                                clastName = reader.GetString(reader.GetOrdinal("clastName")),
                                cphoneno = reader.IsDBNull(reader.GetOrdinal("cphoneno")) ? null : reader.GetString(reader.GetOrdinal("cphoneno")),
                                cAlternatePhone = reader.IsDBNull(reader.GetOrdinal("cAlternatePhone")) ? null : reader.GetString(reader.GetOrdinal("cAlternatePhone")),
                                ldob = reader.IsDBNull(reader.GetOrdinal("ldob")) ? null : reader.GetDateTime(reader.GetOrdinal("ldob")),
                                cMaritalStatus = reader.IsDBNull(reader.GetOrdinal("cMaritalStatus")) ? null : reader.GetString(reader.GetOrdinal("cMaritalStatus")),
                                cnation = reader.IsDBNull(reader.GetOrdinal("cnation")) ? null : reader.GetString(reader.GetOrdinal("cnation")),
                                cgender = reader.IsDBNull(reader.GetOrdinal("cgender")) ? null : reader.GetString(reader.GetOrdinal("cgender")),
                                caddress = reader.IsDBNull(reader.GetOrdinal("caddress")) ? null : reader.GetString(reader.GetOrdinal("caddress")),
                                caddress1 = reader.IsDBNull(reader.GetOrdinal("caddress1")) ? null : reader.GetString(reader.GetOrdinal("caddress1")),
                                caddress2 = reader.IsDBNull(reader.GetOrdinal("caddress2")) ? null : reader.GetString(reader.GetOrdinal("caddress2")),
                                cpincode = reader.GetInt32(reader.GetOrdinal("cpincode")),
                                ccity = reader.IsDBNull(reader.GetOrdinal("ccity")) ? null : reader.GetString(reader.GetOrdinal("ccity")),
                                cstatecode = reader.IsDBNull(reader.GetOrdinal("cstatecode")) ? null : reader.GetString(reader.GetOrdinal("cstatecode")),
                                cstatedesc = reader.IsDBNull(reader.GetOrdinal("cstatedesc")) ? null : reader.GetString(reader.GetOrdinal("cstatedesc")),
                                ccountrycode = reader.IsDBNull(reader.GetOrdinal("ccountrycode")) ? null : reader.GetString(reader.GetOrdinal("ccountrycode")),
                                ProfileImage = reader.IsDBNull(reader.GetOrdinal("ProfileImage")) ? null : reader.GetString(reader.GetOrdinal("ProfileImage")),
                                cbankName = reader.IsDBNull(reader.GetOrdinal("cbankName")) ? null : reader.GetString(reader.GetOrdinal("cbankName")),
                                caccountNumber = reader.IsDBNull(reader.GetOrdinal("caccountNumber")) ? null : reader.GetString(reader.GetOrdinal("caccountNumber")),
                                ciFSCCode = reader.IsDBNull(reader.GetOrdinal("ciFSCCode")) ? null : reader.GetString(reader.GetOrdinal("ciFSCCode")),
                                cpAN = reader.IsDBNull(reader.GetOrdinal("cpAN")) ? null : reader.GetString(reader.GetOrdinal("cpAN")),
                                ldoj = reader.IsDBNull(reader.GetOrdinal("ldoj")) ? null : reader.GetDateTime(reader.GetOrdinal("ldoj")),
                                cemploymentStatus = reader.IsDBNull(reader.GetOrdinal("cemploymentStatus")) ? null : reader.GetString(reader.GetOrdinal("cemploymentStatus")),
                                nnoticePeriodDays = reader.IsDBNull(reader.GetOrdinal("nnoticePeriodDays")) ? null : reader.GetInt32(reader.GetOrdinal("nnoticePeriodDays")),
                                lresignationDate = reader.IsDBNull(reader.GetOrdinal("lresignationDate")) ? null : reader.GetDateTime(reader.GetOrdinal("lresignationDate")),
                                llastWorkingDate = reader.IsDBNull(reader.GetOrdinal("llastWorkingDate")) ? null : reader.GetDateTime(reader.GetOrdinal("llastWorkingDate")),
                                cempcategory = reader.IsDBNull(reader.GetOrdinal("cempcategory")) ? null : reader.GetString(reader.GetOrdinal("cempcategory")),
                                cworkloccode = reader.IsDBNull(reader.GetOrdinal("cworkloccode")) ? null : reader.GetString(reader.GetOrdinal("cworkloccode")),
                                cworklocname = reader.IsDBNull(reader.GetOrdinal("cworklocname")) ? null : reader.GetString(reader.GetOrdinal("cworklocname")),
                                croleID = reader.GetInt32(reader.GetOrdinal("croleID")),
                                crolecode = reader.IsDBNull(reader.GetOrdinal("crolecode")) ? null : reader.GetString(reader.GetOrdinal("crolecode")),
                                crolename = reader.IsDBNull(reader.GetOrdinal("crolename")) ? null : reader.GetString(reader.GetOrdinal("crolename")),
                                cgradecode = reader.IsDBNull(reader.GetOrdinal("cgradecode")) ? null : reader.GetString(reader.GetOrdinal("cgradecode")),
                                cgradedesc = reader.IsDBNull(reader.GetOrdinal("cgradedesc")) ? null : reader.GetString(reader.GetOrdinal("cgradedesc")),
                                csubrolecode = reader.IsDBNull(reader.GetOrdinal("csubrolecode")) ? null : reader.GetString(reader.GetOrdinal("csubrolecode")),
                                cdeptcode = reader.IsDBNull(reader.GetOrdinal("cdeptcode")) ? null : reader.GetString(reader.GetOrdinal("cdeptcode")),
                                cdeptdesc = reader.IsDBNull(reader.GetOrdinal("cdeptdesc")) ? null : reader.GetString(reader.GetOrdinal("cdeptdesc")),
                                cjobcode = reader.IsDBNull(reader.GetOrdinal("cjobcode")) ? null : reader.GetString(reader.GetOrdinal("cjobcode")),
                                cjobdesc = reader.IsDBNull(reader.GetOrdinal("cjobdesc")) ? null : reader.GetString(reader.GetOrdinal("cjobdesc")),
                                creportmgrcode = reader.IsDBNull(reader.GetOrdinal("creportmgrcode")) ? null : reader.GetString(reader.GetOrdinal("creportmgrcode")),
                                creportmgrname = reader.IsDBNull(reader.GetOrdinal("creportmgrname")) ? null : reader.GetString(reader.GetOrdinal("creportmgrname")),
                                cRoll_id = reader.IsDBNull(reader.GetOrdinal("cRoll_id")) ? null : reader.GetString(reader.GetOrdinal("cRoll_id")),
                                cRoll_name = reader.IsDBNull(reader.GetOrdinal("cRoll_name")) ? null : reader.GetString(reader.GetOrdinal("cRoll_name")),
                                cRoll_Id_mngr = reader.IsDBNull(reader.GetOrdinal("cRoll_Id_mngr")) ? null : reader.GetString(reader.GetOrdinal("cRoll_Id_mngr")),
                                cRoll_Id_mngr_desc = reader.IsDBNull(reader.GetOrdinal("cRoll_Id_mngr_desc")) ? null : reader.GetString(reader.GetOrdinal("cRoll_Id_mngr_desc")),
                                cReportManager_empcode = reader.IsDBNull(reader.GetOrdinal("cReportManager_empcode")) ? null : reader.GetString(reader.GetOrdinal("cReportManager_empcode")),
                                cReportManager_Poscode = reader.IsDBNull(reader.GetOrdinal("cReportManager_Poscode")) ? null : reader.GetString(reader.GetOrdinal("cReportManager_Poscode")),
                                cReportManager_Posdesc = reader.IsDBNull(reader.GetOrdinal("cReportManager_Posdesc")) ? null : reader.GetString(reader.GetOrdinal("cReportManager_Posdesc")),
                                nIsWebAccessEnabled = reader.IsDBNull(reader.GetOrdinal("nIsWebAccessEnabled")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsWebAccessEnabled")),
                                nIsEventRead = reader.IsDBNull(reader.GetOrdinal("nIsEventRead")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsEventRead")),
                                lLastLoginAt = reader.IsDBNull(reader.GetOrdinal("lLastLoginAt")) ? null : reader.GetDateTime(reader.GetOrdinal("lLastLoginAt")),
                                nFailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("nFailedLoginAttempts")) ? null : reader.GetInt32(reader.GetOrdinal("nFailedLoginAttempts")),
                                cPasswordChangedAt = reader.IsDBNull(reader.GetOrdinal("cPasswordChangedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("cPasswordChangedAt")),
                                nIsLocked = reader.IsDBNull(reader.GetOrdinal("nIsLocked")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsLocked")),
                                LastLoginIP = reader.IsDBNull(reader.GetOrdinal("LastLoginIP")) ? null : reader.GetString(reader.GetOrdinal("LastLoginIP")),
                                LastLoginDevice = reader.IsDBNull(reader.GetOrdinal("LastLoginDevice")) ? null : reader.GetString(reader.GetOrdinal("LastLoginDevice")),
                                ccreateddate = reader.IsDBNull(reader.GetOrdinal("ccreateddate")) ? null : reader.GetDateTime(reader.GetOrdinal("ccreateddate")),
                                ccreatedby = reader.IsDBNull(reader.GetOrdinal("ccreatedby")) ? null : reader.GetString(reader.GetOrdinal("ccreatedby")),
                                cmodifiedby = reader.IsDBNull(reader.GetOrdinal("cmodifiedby")) ? null : reader.GetString(reader.GetOrdinal("cmodifiedby")),
                                lmodifieddate = reader.IsDBNull(reader.GetOrdinal("lmodifieddate")) ? null : reader.GetDateTime(reader.GetOrdinal("lmodifieddate")),
                                nIsDeleted = reader.IsDBNull(reader.GetOrdinal("nIsDeleted")) ? null : reader.GetBoolean(reader.GetOrdinal("nIsDeleted")),
                                cDeletedBy = reader.IsDBNull(reader.GetOrdinal("cDeletedBy")) ? null : reader.GetString(reader.GetOrdinal("cDeletedBy")),
                                lDeletedDate = reader.IsDBNull(reader.GetOrdinal("lDeletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("lDeletedDate"))
                            });
                        }
                    }
                }
                return result;
            }
        }

   
    
    
    
    }
}