using System;
using System.Data;
using System.Data.SqlClient;
using System.Net;
using System.Net.Mail;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
using TaskEngineAPI.Repositories;
using Newtonsoft.Json;

namespace TaskEngineAPI.Services
{


    public class AccountService : IAdminService
    {
        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;
        public AccountService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository, IOptions<UploadSettings> uploadSettings)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
            _uploadSettings = uploadSettings.Value;
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

                string query = @"INSERT INTO AdminUsers (
        ctenant_Id, cfirst_name, clast_name, cuserid, cemail, cphoneno, 
        cpassword, crole_id, nis_active, llast_login_at, cpassword_changed_at, 
        clast_login_ip, clast_login_device, ccreated_date, ccreated_by, cmodified_by,
        lmodified_date) VALUES(
        @TenantID, @FirstName, @LastName, @cuserid, @Email, @PhoneNo, 
        @Password, @RoleID, @IsActive, @LastLoginAt, @PasswordChangedAt, 
        @LastLoginIP, @LastLoginDevice, @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date);
        SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    string hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);

                    cmd.Parameters.AddWithValue("@TenantID", model.ctenant_Id);
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirst_name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clast_name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cuserid", model.cuserid);
                    cmd.Parameters.AddWithValue("@Email", model.cemail);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", hashedPassword); // store as plain text
                    cmd.Parameters.AddWithValue("@RoleID", (object?)model.crole_id ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nis_active ?? true);
                    cmd.Parameters.AddWithValue("@LastLoginAt", (object?)model.llast_login_at ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PasswordChangedAt", (object?)model.cpassword_changed_at ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastLoginIP", (object?)model.clast_login_ip ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastLoginDevice", (object?)model.clast_login_device ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@ccreated_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ccreated_by", (object?)model.ccreated_by ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                    var newId = await cmd.ExecuteScalarAsync();
                    return newId != null ? Convert.ToInt32(newId) : 0;
                }
            }
        }

        public async Task<bool> CheckEmailExistsAsync(string email, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM AdminUsers WHERE cemail = @email AND ctenant_Id = @tenantId ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
                }
            }
        }


        public async Task<bool> CheckUsernameExistsAsync(int cuserid, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM AdminUsers WHERE cuserid = @username AND ctenant_Id = @tenantId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", cuserid);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }

        public async Task<bool> CheckPhenonoExistsAsync(string phoneno, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM AdminUsers WHERE cphoneno = @phoneno AND ctenant_Id = @tenantId ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@phoneno", phoneno);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;
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
            SELECT [ID],  [ctenant_Id], [cfirst_name], [clast_name], [cuserid],
                   [cemail], [cphoneno], [cpassword], [crole_id], [nis_active], [llast_login_at],
                   [lfailed_login_attempts], [cpassword_changed_at], [cmust_change_password],
                   [clast_login_ip], [clast_login_device],[nis_locked],[ccreated_date],[ccreated_by],[cmodified_by],
                   [lmodified_date],[nIs_deleted],[cdeleted_by],[ldeleted_date],[cprofile_image_name],[cprofile_image_path]
            FROM [dbo].[AdminUsers] WHERE crole_id = 2 AND ctenant_Id = @TenantID and nis_deleted=0";


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
                                cTenantID = reader.GetInt32(reader.GetOrdinal("ctenant_Id")),
                                cfirstName = reader.IsDBNull(reader.GetOrdinal("cfirst_name")) ? null : reader.GetString(reader.GetOrdinal("cfirst_name")),
                                clastName = reader.IsDBNull(reader.GetOrdinal("clast_name")) ? null : reader.GetString(reader.GetOrdinal("clast_name")),
                                cuserid = reader.IsDBNull(reader.GetOrdinal("cuserid")) ? 0 : reader.GetInt32(reader.GetOrdinal("cuserid")),
                                cemail = reader.IsDBNull(reader.GetOrdinal("cemail")) ? null : reader.GetString(reader.GetOrdinal("cemail")),
                                cphoneno = reader.IsDBNull(reader.GetOrdinal("cphoneno")) ? null : reader.GetString(reader.GetOrdinal("cphoneno")),
                                cpassword = reader.IsDBNull(reader.GetOrdinal("cpassword")) ? null : reader.GetString(reader.GetOrdinal("cpassword")),
                                croleID = reader.IsDBNull(reader.GetOrdinal("crole_id")) ? 0 : reader.GetInt32(reader.GetOrdinal("crole_id")),
                                nisActive = reader.GetBoolean(reader.GetOrdinal("nis_active")),
                                llastLoginAt = reader.IsDBNull(reader.GetOrdinal("llast_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("llast_login_at")),
                                lfailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("lfailed_login_attempts")) ? null : reader.GetInt32(reader.GetOrdinal("lfailed_login_attempts")),
                                cPasswordChangedAt = reader.IsDBNull(reader.GetOrdinal("cpassword_changed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("cpassword_changed_at")),
                                cMustChangePassword = reader.IsDBNull(reader.GetOrdinal("cmust_change_password")) ? null : reader.GetBoolean(reader.GetOrdinal("cmust_change_password")),
                                cLastLoginIP = reader.IsDBNull(reader.GetOrdinal("clast_login_ip")) ? null : reader.GetString(reader.GetOrdinal("clast_login_ip")),
                                cLastLoginDevice = reader.IsDBNull(reader.GetOrdinal("clast_login_device")) ? null : reader.GetString(reader.GetOrdinal("clast_login_device")),
                                nis_locked = reader.IsDBNull(reader.GetOrdinal("nis_locked")) ? null : reader.GetBoolean(reader.GetOrdinal("nis_locked")),
                                ccreated_date = reader.IsDBNull(reader.GetOrdinal("ccreated_date")) ? null : reader.GetDateTime(reader.GetOrdinal("ccreated_date")),
                                ccreated_by = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                cmodified_by = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodified_date = reader.IsDBNull(reader.GetOrdinal("lmodified_date")) ? null : reader.GetDateTime(reader.GetOrdinal("lmodified_date")),
                                nIs_deleted = reader.IsDBNull(reader.GetOrdinal("nIs_deleted")) ? null : reader.GetBoolean(reader.GetOrdinal("nIs_deleted")),
                                cdeleted_by = reader.IsDBNull(reader.GetOrdinal("cdeleted_by")) ? null : reader.GetString(reader.GetOrdinal("cdeleted_by")),
                                ldeleted_date = reader.IsDBNull(reader.GetOrdinal("ldeleted_date")) ? null : reader.GetDateTime(reader.GetOrdinal("ldeleted_date")),
                                cprofile_image_name = reader.IsDBNull(reader.GetOrdinal("cprofile_image_name")) ? null : reader.GetString(reader.GetOrdinal("cprofile_image_name")),
                                cprofile_image_path = reader.IsDBNull(reader.GetOrdinal("cprofile_image_path")) ? null : reader.GetString(reader.GetOrdinal("cprofile_image_path")),
                            });
                        }
                    }
                }
                return result;
            }
        }

        public async Task<bool> UpdateSuperAdminAsyncold(UpdateAdminDTO model)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();



                string query = @"
             UPDATE AdminUsers SET
            cfirst_name = @FirstName,
            clast_name = @LastName,
            cuserid = @userid,
            cemail = @Email,
            cphoneno = @PhoneNo,
            cpassword = @Password,
            nis_active = @IsActive,
            cmodified_by=cmodified_by,
            lmodified_date=lmodified_date          
            WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    // model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userid", (object?)model.cuserid ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.cemail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", (object?)model.cpassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nisActive ?? true);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ID", model.cid);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }

        public async Task<bool> UpdateSuperAdminAsync(UpdateAdminDTO model)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string hashedPassword = null;
                if (!string.IsNullOrEmpty(model.cpassword))
                {
                    hashedPassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);
                }

                string query = @"
            UPDATE AdminUsers SET
                cfirst_name = @FirstName,
                clast_name = @LastName,
                cuserid = @userid,
                cemail = @Email,
                cphoneno = @PhoneNo,
                nis_active = @IsActive,
                cmodified_by = @cmodified_by,
                lmodified_date = @lmodified_date,
                cpassword = CASE WHEN @Password IS NOT NULL THEN @Password ELSE cpassword END
            WHERE ID = @ID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@userid", (object?)model.cuserid ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.cemail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nisActive ?? true);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ID", model.cid);
                    cmd.Parameters.AddWithValue("@Password", (object?)hashedPassword ?? DBNull.Value);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }



        public async Task<bool> DeleteSuperAdminAsync(DeleteAdminDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        update  AdminUsers set nis_deleted=1,cdeleted_by=@username,ldeleted_Date=@ldeleted_Date
        WHERE ID = @cuserid AND cTenant_ID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cuserid", model.cid);
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@ldeleted_Date", DateTime.Now);
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

            string query = @"
INSERT INTO Users (
    cuserid, [ctenant_id], [cuser_name], [cemail], [cpassword], [nIs_active],
    [cfirst_name], [clast_name], [cphoneno], [calternate_phone], [ldob], [cmarital_status],
    [cnation], [cgender], [caddress], [caddress1], [caddress2], [cpincode], [ccity],
    [cstate_code], [cstate_desc], [ccountry_code], [profile_image], [cbank_name],
    [caccount_number], [ciFSC_code], [cpan], [ldoj], [cemployment_status], [nnotice_period_days],
    [lresignation_date], [llast_working_date], [cemp_category], [cwork_loc_code], [cwork_loc_name],
    [crole_id], [crole_code], [crole_name], [cgrade_code], [cgrade_desc], [csub_role_code],
    [cdept_code], [cdept_desc], [cjob_code], [cjob_desc], [creport_mgr_code], [creport_mgr_name],
    [croll_id], [croll_name], [croll_id_mngr], [croll_id_mngr_desc], [creport_manager_empcode],
    [creport_manager_poscode], [creport_manager_pos_desc], [nis_web_access_enabled],
    [nis_event_read], [llast_login_at], [nfailed_logina_attempts], [cpassword_changed_at],
    [nis_locked], [last_login_ip], [last_login_device], [ccreated_date], [ccreated_by],
    [cmodified_by], [lmodified_date], [nIs_deleted], [cdeleted_by], [ldeleted_date]
)
VALUES (
    @cuserid, @ctenantID, @cusername, @cemail, @cpassword, @nIsActive,
    @cfirstName, @clastName, @cphoneno, @cAlternatePhone, @ldob, @cMaritalStatus,
    @cnation, @cgender, @caddress, @caddress1, @caddress2, @cpincode, @ccity,
    @cstatecode, @cstatedesc, @ccountrycode, @ProfileImage, @cbankName,
    @caccountNumber, @ciFSCCode, @cpAN, @ldoj, @cemploymentStatus, @nnoticePeriodDays,
    @lresignationDate, @llastWorkingDate, @cempcategory, @cworkloccode, @cworklocname,
    @croleID, @crolecode, @crolename, @cgradecode, @cgradedesc, @csubrolecode,
    @cdeptcode, @cdeptdesc, @cjobcode, @cjobdesc, @creportmgrcode, @creportmgrname,
    @cRoll_id, @cRoll_name, @cRoll_Id_mngr, @cRoll_Id_mngr_desc, @cReportManager_empcode,
    @cReportManager_Poscode, @cReportManager_Posdesc, @nIsWebAccessEnabled,
    @nIsEventRead, @lLastLoginAt, @nFailedLoginAttempts, @cPasswordChangedAt,
    @nIsLocked, @LastLoginIP, @LastLoginDevice, @ccreateddate, @ccreatedby,
    @cmodifiedby, @lmodifieddate, @nIsDeleted, @cDeletedBy, @lDeletedDate); SELECT SCOPE_IDENTITY(); ";

            using var cmd = new SqlCommand(query, conn);
            model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);

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
            cmd.Parameters.AddWithValue("@cmodifiedby", (object?)model.cmodifiedby ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lmodifieddate", (DateTime.Now));
            cmd.Parameters.AddWithValue("@nIsDeleted", (object?)model.nIsDeleted ?? false);
            cmd.Parameters.AddWithValue("@cDeletedBy", (object?)model.cDeletedBy ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@lDeletedDate", (object?)model.lDeletedDate ?? DBNull.Value);
            int rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0 ? model.cuserid : 0;
            var newId = await cmd.ExecuteScalarAsync();
            return newId != null ? Convert.ToInt32(newId) : 0;
        }

        public async Task<bool> UpdateUserAsync(UpdateUserDTO model, int cTenantID)
        {
            var connStr = _config.GetConnectionString("Database");


            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();
            string query = @"UPDATE Users SET
            cuser_name = @cusername,
            cemail = @cemail,
            cpassword = @cpassword,
            [nIs_active] = @nIsActive,
            [cfirst_name] = @cfirstName,
            [clast_name] = @clastName,
            cphoneno = @cphoneno,
            calternate_phone = @cAlternatePhone,
            ldob = @ldob,
            cmarital_status = @cMaritalStatus,
            cnation = @cnation,
            cgender = @cgender,
            caddress = @caddress,
            caddress1 = @caddress1,
            caddress2 = @caddress2,
            cpincode = @cpincode,
            ccity = @ccity,
            cstate_code = @cstatecode,
            cstate_desc = @cstatedesc,
            ccountry_code = @ccountrycode,
            profile_image = @ProfileImage,
            cbank_name = @cbankName,
            caccount_number = @caccountNumber,
            ciFSC_code = @ciFSCCode,
            cpan = @cpAN,
            ldoj = @ldoj,
            cemployment_status = @cemploymentStatus,
            nnotice_period_days = @nnoticePeriodDays,
            lresignation_date = @lresignationDate,
            llast_working_date = @llastWorkingDate,
            cemp_category = @cempcategory,
            cwork_loc_code = @cworkloccode,
            cwork_loc_name = @cworklocname,
            crole_id = @croleID,
            crole_code = @crolecode,
            crole_name = @crolename,
            cgrade_code = @cgradecode,
            cgrade_desc = @cgradedesc,
            csub_role_code = @csubrolecode,
            cdept_code = @cdeptcode,
            cdept_desc = @cdeptdesc,
            cjob_code = @cjobcode,
            cjob_desc = @cjobdesc,
            creport_mgr_code = @creportmgrcode,
            creport_mgr_name = @creportmgrname,
            croll_id = @cRoll_id,
            croll_name = @cRoll_name,
            croll_id_mngr = @cRoll_Id_mngr,
            croll_id_mngr_desc = @cRoll_Id_mngr_desc,
            creport_manager_empcode = @cReportManager_empcode,
            creport_manager_poscode = @cReportManager_Poscode,
            creport_manager_pos_desc = @cReportManager_Posdesc,
            nis_web_access_enabled = @nIsWebAccessEnabled,
            nis_event_read = @nIsEventRead,
            llast_login_at = @lLastLoginAt,
            nfailed_logina_attempts = @nFailedLoginAttempts,
            cpassword_changed_at = @cPasswordChangedAt,
            nis_locked = @nIsLocked,
            last_login_ip = @LastLoginIP,
            last_login_device = @LastLoginDevice,
            cmodified_by = @cmodifiedby,
            lmodified_date = @lmodifieddate,
            nIs_deleted = @nIsDeleted,
            cdeleted_by = @cDeletedBy,
            ldeleted_date = @lDeletedDate        
            WHERE ctenant_id = @ctenantID and id=@id";

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@id", model.id);
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
            cmd.Parameters.AddWithValue("@ProfileImageName", (object?)model.ProfileImage ?? DBNull.Value);
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
       SELECT [ID], [cuserid], [ctenant_id], [cuser_name], [cemail], [cpassword], [nIs_active],
       [cfirst_name],[clast_name], [cphoneno], [calternate_phone], [ldob], [cmarital_status],
        [cnation], [cgender], [caddress], [caddress1], [caddress2], [cpincode], [ccity],
       [cstate_code],[cstate_desc],[ccountry_code],[profile_image], [cbank_name],
        [caccount_number],[ciFSC_code],[cpan], [ldoj], [cemployment_status],
        [nnotice_period_days], [lresignation_date], [llast_working_date],[cemp_category],
       [cwork_loc_code],[cwork_loc_name],[crole_id],[crole_code],[crole_name],[cgrade_code],
        [cgrade_desc], [csub_role_code], [cdept_code], [cdept_desc], [cjob_code], [cjob_desc], 
	[creport_mgr_code],[creport_mgr_name],[croll_id],[croll_name],[croll_id_mngr],[croll_id_mngr_desc]
      ,[creport_manager_empcode],[creport_manager_poscode]
      ,[creport_manager_pos_desc],[nis_web_access_enabled]
      ,[nis_event_read],[llast_login_at],[nfailed_logina_attempts],
	  [cpassword_changed_at],[nis_locked],[last_login_ip],[last_login_device],
	  [ccreated_date],[ccreated_by],[cmodified_by],[lmodified_date],[nIs_deleted],[cdeleted_by],
	  [ldeleted_date],[cprofile_image_name],[cprofile_image_path]   FROM [dbo].[Users]
        WHERE crole_id = 3 AND ctenant_id = @TenantID and nis_deleted=0";

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
                                ctenantID = reader.GetInt32(reader.GetOrdinal("ctenant_id")),
                                cusername = reader.GetString(reader.GetOrdinal("cuser_name")),
                                cemail = reader.GetString(reader.GetOrdinal("cemail")),
                                cpassword = reader.GetString(reader.GetOrdinal("cpassword")),
                                nIsActive = reader.GetBoolean(reader.GetOrdinal("nIs_active")),
                                cfirstName = reader.GetString(reader.GetOrdinal("cfirst_name")),
                                clastName = reader.GetString(reader.GetOrdinal("clast_name")),
                                cphoneno = reader.IsDBNull(reader.GetOrdinal("cphoneno")) ? null : reader.GetString(reader.GetOrdinal("cphoneno")),
                                cAlternatePhone = reader.IsDBNull(reader.GetOrdinal("calternate_phone")) ? null : reader.GetString(reader.GetOrdinal("calternate_phone")),
                                ldob = reader.IsDBNull(reader.GetOrdinal("ldob")) ? null : reader.GetDateTime(reader.GetOrdinal("ldob")),
                                cMaritalStatus = reader.IsDBNull(reader.GetOrdinal("cmarital_status")) ? null : reader.GetString(reader.GetOrdinal("cmarital_status")),
                                cnation = reader.IsDBNull(reader.GetOrdinal("cnation")) ? null : reader.GetString(reader.GetOrdinal("cnation")),
                                cgender = reader.IsDBNull(reader.GetOrdinal("cgender")) ? null : reader.GetString(reader.GetOrdinal("cgender")),
                                caddress = reader.IsDBNull(reader.GetOrdinal("caddress")) ? null : reader.GetString(reader.GetOrdinal("caddress")),
                                caddress1 = reader.IsDBNull(reader.GetOrdinal("caddress1")) ? null : reader.GetString(reader.GetOrdinal("caddress1")),
                                caddress2 = reader.IsDBNull(reader.GetOrdinal("caddress2")) ? null : reader.GetString(reader.GetOrdinal("caddress2")),
                                cpincode = reader.GetInt32(reader.GetOrdinal("cpincode")),
                                ccity = reader.IsDBNull(reader.GetOrdinal("ccity")) ? null : reader.GetString(reader.GetOrdinal("ccity")),
                                cstatecode = reader.IsDBNull(reader.GetOrdinal("cstate_code")) ? null : reader.GetString(reader.GetOrdinal("cstate_code")),
                                cstatedesc = reader.IsDBNull(reader.GetOrdinal("cstate_desc")) ? null : reader.GetString(reader.GetOrdinal("cstate_desc")),
                                ccountrycode = reader.IsDBNull(reader.GetOrdinal("ccountry_code")) ? null : reader.GetString(reader.GetOrdinal("ccountry_code")),
                                ProfileImage = reader.IsDBNull(reader.GetOrdinal("profile_image")) ? null : reader.GetString(reader.GetOrdinal("profile_image")),
                                cbankName = reader.IsDBNull(reader.GetOrdinal("cbank_name")) ? null : reader.GetString(reader.GetOrdinal("cbank_name")),
                                caccountNumber = reader.IsDBNull(reader.GetOrdinal("caccount_number")) ? null : reader.GetString(reader.GetOrdinal("caccount_number")),
                                ciFSCCode = reader.IsDBNull(reader.GetOrdinal("ciFSC_code")) ? null : reader.GetString(reader.GetOrdinal("ciFSC_code")),
                                cpAN = reader.IsDBNull(reader.GetOrdinal("cpan")) ? null : reader.GetString(reader.GetOrdinal("cpan")),
                                ldoj = reader.IsDBNull(reader.GetOrdinal("ldoj")) ? null : reader.GetDateTime(reader.GetOrdinal("ldoj")),
                                cemploymentStatus = reader.IsDBNull(reader.GetOrdinal("cemployment_status")) ? null : reader.GetString(reader.GetOrdinal("cemployment_status")),
                                nnoticePeriodDays = reader.IsDBNull(reader.GetOrdinal("nnotice_period_days")) ? null : reader.GetInt32(reader.GetOrdinal("nnotice_period_days")),
                                lresignationDate = reader.IsDBNull(reader.GetOrdinal("lresignation_date")) ? null : reader.GetDateTime(reader.GetOrdinal("lresignation_date")),
                                llastWorkingDate = reader.IsDBNull(reader.GetOrdinal("llast_working_date")) ? null : reader.GetDateTime(reader.GetOrdinal("llast_working_date")),
                                cempcategory = reader.IsDBNull(reader.GetOrdinal("cemp_category")) ? null : reader.GetString(reader.GetOrdinal("cemp_category")),
                                cworkloccode = reader.IsDBNull(reader.GetOrdinal("cwork_loc_code")) ? null : reader.GetString(reader.GetOrdinal("cwork_loc_code")),
                                cworklocname = reader.IsDBNull(reader.GetOrdinal("cwork_loc_name")) ? null : reader.GetString(reader.GetOrdinal("cwork_loc_name")),
                                croleID = reader.GetInt32(reader.GetOrdinal("crole_id")),
                                crolecode = reader.IsDBNull(reader.GetOrdinal("crole_code")) ? null : reader.GetString(reader.GetOrdinal("crole_code")),
                                crolename = reader.IsDBNull(reader.GetOrdinal("crole_name")) ? null : reader.GetString(reader.GetOrdinal("crole_name")),
                                cgradecode = reader.IsDBNull(reader.GetOrdinal("cgrade_code")) ? null : reader.GetString(reader.GetOrdinal("cgrade_code")),
                                cgradedesc = reader.IsDBNull(reader.GetOrdinal("cgrade_desc")) ? null : reader.GetString(reader.GetOrdinal("cgrade_desc")),
                                csubrolecode = reader.IsDBNull(reader.GetOrdinal("csub_role_code")) ? null : reader.GetString(reader.GetOrdinal("csub_role_code")),
                                cdeptcode = reader.IsDBNull(reader.GetOrdinal("cdept_code")) ? null : reader.GetString(reader.GetOrdinal("cdept_code")),
                                cdeptdesc = reader.IsDBNull(reader.GetOrdinal("cdept_desc")) ? null : reader.GetString(reader.GetOrdinal("cdept_desc")),
                                cjobcode = reader.IsDBNull(reader.GetOrdinal("cjob_code")) ? null : reader.GetString(reader.GetOrdinal("cjob_code")),
                                cjobdesc = reader.IsDBNull(reader.GetOrdinal("cjob_desc")) ? null : reader.GetString(reader.GetOrdinal("cjob_desc")),
                                creportmgrcode = reader.IsDBNull(reader.GetOrdinal("creport_mgr_code")) ? null : reader.GetString(reader.GetOrdinal("creport_mgr_code")),
                                creportmgrname = reader.IsDBNull(reader.GetOrdinal("creport_mgr_name")) ? null : reader.GetString(reader.GetOrdinal("creport_mgr_name")),
                                cRoll_id = reader.IsDBNull(reader.GetOrdinal("croll_id")) ? null : reader.GetString(reader.GetOrdinal("croll_id")),
                                cRoll_name = reader.IsDBNull(reader.GetOrdinal("croll_name")) ? null : reader.GetString(reader.GetOrdinal("croll_name")),
                                cRoll_Id_mngr = reader.IsDBNull(reader.GetOrdinal("cRoll_Id_mngr")) ? null : reader.GetString(reader.GetOrdinal("cRoll_Id_mngr")),
                                cRoll_Id_mngr_desc = reader.IsDBNull(reader.GetOrdinal("cRoll_Id_mngr_desc")) ? null : reader.GetString(reader.GetOrdinal("cRoll_Id_mngr_desc")),
                                cReportManager_empcode = reader.IsDBNull(reader.GetOrdinal("creport_manager_empcode")) ? null : reader.GetString(reader.GetOrdinal("creport_manager_empcode")),
                                cReportManager_Poscode = reader.IsDBNull(reader.GetOrdinal("creport_manager_poscode")) ? null : reader.GetString(reader.GetOrdinal("creport_manager_poscode")),
                                cReportManager_Posdesc = reader.IsDBNull(reader.GetOrdinal("creport_manager_pos_desc")) ? null : reader.GetString(reader.GetOrdinal("creport_manager_pos_desc")),
                                nIsWebAccessEnabled = reader.IsDBNull(reader.GetOrdinal("nis_web_access_enabled")) ? null : reader.GetBoolean(reader.GetOrdinal("nis_web_access_enabled")),
                                nIsEventRead = reader.IsDBNull(reader.GetOrdinal("nis_event_read")) ? null : reader.GetBoolean(reader.GetOrdinal("nis_event_read")),
                                lLastLoginAt = reader.IsDBNull(reader.GetOrdinal("llast_login_at")) ? null : reader.GetDateTime(reader.GetOrdinal("llast_login_at")),
                                nFailedLoginAttempts = reader.IsDBNull(reader.GetOrdinal("nfailed_logina_attempts")) ? null : reader.GetInt32(reader.GetOrdinal("nfailed_logina_attempts")),
                                cPasswordChangedAt = reader.IsDBNull(reader.GetOrdinal("cpassword_changed_at")) ? null : reader.GetDateTime(reader.GetOrdinal("cpassword_changed_at")),
                                nIsLocked = reader.IsDBNull(reader.GetOrdinal("nis_locked")) ? null : reader.GetBoolean(reader.GetOrdinal("nis_locked")),
                                LastLoginIP = reader.IsDBNull(reader.GetOrdinal("last_login_ip")) ? null : reader.GetString(reader.GetOrdinal("last_login_ip")),
                                LastLoginDevice = reader.IsDBNull(reader.GetOrdinal("last_login_device")) ? null : reader.GetString(reader.GetOrdinal("last_login_device")),
                                ccreateddate = reader.IsDBNull(reader.GetOrdinal("ccreated_date")) ? null : reader.GetDateTime(reader.GetOrdinal("ccreated_date")),
                                ccreatedby = reader.IsDBNull(reader.GetOrdinal("ccreated_by")) ? null : reader.GetString(reader.GetOrdinal("ccreated_by")),
                                cmodifiedby = reader.IsDBNull(reader.GetOrdinal("cmodified_by")) ? null : reader.GetString(reader.GetOrdinal("cmodified_by")),
                                lmodifieddate = reader.IsDBNull(reader.GetOrdinal("lmodified_date")) ? null : reader.GetDateTime(reader.GetOrdinal("lmodified_date")),
                                nIsDeleted = reader.IsDBNull(reader.GetOrdinal("nIs_deleted")) ? null : reader.GetBoolean(reader.GetOrdinal("nIs_deleted")),
                                cDeletedBy = reader.IsDBNull(reader.GetOrdinal("cdeleted_by")) ? null : reader.GetString(reader.GetOrdinal("cdeleted_by")),
                                lDeletedDate = reader.IsDBNull(reader.GetOrdinal("ldeleted_date")) ? null : reader.GetDateTime(reader.GetOrdinal("ldeleted_date")),
                                cprofile_image_name = reader.IsDBNull(reader.GetOrdinal("cprofile_image_name")) ? null : reader.GetString(reader.GetOrdinal("cprofile_image_name")),
                                cprofile_image_path = reader.IsDBNull(reader.GetOrdinal("cprofile_image_path")) ? null : reader.GetString(reader.GetOrdinal("cprofile_image_path"))


                            });
                        }
                    }
                }
                return result;


            }
        }

        public async Task<List<GetUserDTO>> GetAllUserIdAsync(int cTenantID, int userid)
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
               [nIsDeleted], [cDeletedBy], [lDeletedDate], [cprofile_image_name], [cprofile_image_path]
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
                                lDeletedDate = reader.IsDBNull(reader.GetOrdinal("lDeletedDate")) ? null : reader.GetDateTime(reader.GetOrdinal("lDeletedDate")),
                                cprofile_image_name = reader.IsDBNull(reader.GetOrdinal("cprofile_image_name")) ? null : reader.GetString(reader.GetOrdinal("cprofile_image_name")),
                                cprofile_image_path = reader.IsDBNull(reader.GetOrdinal("cprofile_image_path")) ? null : reader.GetString(reader.GetOrdinal("cprofile_image_path")),

                            });
                        }
                    }
                }
                return result;
            }
        }

        public async Task<bool> CheckuserUsernameExistsAsync(int cuserid, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cuserid = @username AND ctenant_Id = @tenantId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", cuserid);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }
        public async Task<bool> CheckuserEmailExistsAsync(string email, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cemail = @email AND ctenant_Id = @tenantId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }

        public async Task<bool> CheckuserPhonenoExistsAsync(string phoneno, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cphoneno = @phoneno AND ctenant_Id = @tenantId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@phoneno", phoneno);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);

                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }

        public async Task<bool> UpdatePasswordSuperAdminAsync(UpdateadminPassword model, int tenantId, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);
                string query = @"
        UPDATE AdminUsers SET
            cpassword = @Password,
            cpassword_changed_at =@cPasswordChangedAt          
        WHERE cuser_name = @ID AND  ctenant_Id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Password", (object?)model.cpassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@cPasswordChangedAt", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ID", username);
                    cmd.Parameters.AddWithValue("@TenantID", tenantId);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }
        public async Task<bool> CheckuserUsernameExistsputAsync(string username, int tenantId, int cuserid)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cuser_name = @username AND ctenant_Id = @tenantId and ID!= @excludeUserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);
                    cmd.Parameters.AddWithValue("@excludeUserId", cuserid);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }

        public async Task<bool> CheckuserEmailExistsputAsync(string email, int tenantId, int cuserid)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cemail = @email AND ctenant_Id = @tenantId and ID!= @excludeUserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);
                    cmd.Parameters.AddWithValue("@excludeUserId", cuserid);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }

        public async Task<bool> CheckuserPhonenoExistsputAsync(string phoneno, int tenantId, int cuserid)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cphoneno = @phoneno AND ctenant_Id = @tenantId and ID!= @excludeUserId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@phoneno", phoneno);
                    cmd.Parameters.AddWithValue("@tenantId", tenantId);
                    cmd.Parameters.AddWithValue("@excludeUserId", cuserid);
                    int count = (int)await cmd.ExecuteScalarAsync();
                    return count > 0;

                }
            }
        }


        public async Task<bool> DeleteuserAsync(DeleteuserDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string query = @"
        update  Users set nis_deleted=1,cdeleted_by=@username,ldeleted_Date=@ldeleted_Date
        WHERE ID = @cuserid AND cTenant_ID = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@cuserid", model.id);
                    cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@ldeleted_Date", DateTime.Now);
                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }



        //public async Task<int> InsertUsersBulkAsync(List<CreateUserDTO> users)
        //{
        //    if (users == null || !users.Any())
        //        return 0;

        //    var connStr = _config.GetConnectionString("Database");

        //    using var conn = new SqlConnection(connStr);
        //    using var cmd = new SqlCommand("sp_Insert_Users_Bulk", conn);
        //    cmd.CommandType = CommandType.StoredProcedure;

        //    var table = new DataTable();
        //    // Add columns (same as your UserTableType)
        //    table.Columns.Add("cuserid", typeof(int));
        //    table.Columns.Add("ctenant_id", typeof(int));
        //    table.Columns.Add("cuser_name", typeof(string));
        //    table.Columns.Add("cemail", typeof(string));
        //    table.Columns.Add("cpassword", typeof(string));
        //    table.Columns.Add("nIs_active", typeof(bool));
        //    table.Columns.Add("cfirst_name", typeof(string));
        //    table.Columns.Add("clast_name", typeof(string));
        //    table.Columns.Add("cphoneno", typeof(string));
        //    table.Columns.Add("calternate_phone", typeof(string));
        //    table.Columns.Add("ldob", typeof(DateTime));
        //    table.Columns.Add("cmarital_status", typeof(string));
        //    table.Columns.Add("cnation", typeof(string));
        //    table.Columns.Add("cgender", typeof(string));
        //    table.Columns.Add("caddress", typeof(string));
        //    table.Columns.Add("caddress1", typeof(string));
        //    table.Columns.Add("caddress2", typeof(string));
        //    table.Columns.Add("cpincode", typeof(string));
        //    table.Columns.Add("ccity", typeof(string));
        //    table.Columns.Add("cstate_code", typeof(string));
        //    table.Columns.Add("cstate_desc", typeof(string));
        //    table.Columns.Add("ccountry_code", typeof(string));
        //    table.Columns.Add("ProfileImage", typeof(string));
        //    table.Columns.Add("cbank_name", typeof(string));
        //    table.Columns.Add("caccount_number", typeof(string));
        //    table.Columns.Add("ciFSC_code", typeof(string));
        //    table.Columns.Add("cpan", typeof(string));
        //    table.Columns.Add("ldoj", typeof(DateTime));
        //    table.Columns.Add("cemployment_status", typeof(string));
        //    table.Columns.Add("nnotice_period_days", typeof(int));
        //    table.Columns.Add("lresignation_date", typeof(DateTime));
        //    table.Columns.Add("llast_working_date", typeof(DateTime));
        //    table.Columns.Add("cemp_category", typeof(string));
        //    table.Columns.Add("cwork_loc_code", typeof(string));
        //    table.Columns.Add("cwork_loc_name", typeof(string));
        //    table.Columns.Add("crole_id", typeof(int));
        //    table.Columns.Add("crole_code", typeof(string));
        //    table.Columns.Add("crole_name", typeof(string));
        //    table.Columns.Add("cgrade_code", typeof(string));
        //    table.Columns.Add("cgrade_desc", typeof(string));
        //    table.Columns.Add("csub_role_code", typeof(string));
        //    table.Columns.Add("cdept_code", typeof(string));
        //    table.Columns.Add("cdept_desc", typeof(string));
        //    table.Columns.Add("cjob_code", typeof(string));
        //    table.Columns.Add("cjob_desc", typeof(string));
        //    table.Columns.Add("creport_mgr_code", typeof(string));
        //    table.Columns.Add("creport_mgr_name", typeof(string));
        //    table.Columns.Add("cRoll_id", typeof(string));
        //    table.Columns.Add("cRoll_name", typeof(string));
        //    table.Columns.Add("cRoll_Id_mngr", typeof(string));
        //    table.Columns.Add("cRoll_Id_mngr_desc", typeof(string));
        //    table.Columns.Add("creport_manager_empcode", typeof(string));
        //    table.Columns.Add("creport_manager_poscode", typeof(string));
        //    table.Columns.Add("creport_manager_pos_desc", typeof(string));
        //    table.Columns.Add("nis_web_access_enabled", typeof(bool));
        //    table.Columns.Add("nis_event_read", typeof(bool));
        //    table.Columns.Add("llast_login_at", typeof(DateTime));
        //    table.Columns.Add("nfailed_logina_attempts", typeof(int));
        //    table.Columns.Add("cpassword_changed_at", typeof(DateTime));
        //    table.Columns.Add("nis_locked", typeof(bool));
        //    table.Columns.Add("last_login_ip", typeof(string));
        //    table.Columns.Add("last_login_device", typeof(string));
        //    table.Columns.Add("ccreated_date", typeof(DateTime));
        //    table.Columns.Add("ccreated_by", typeof(string));
        //    table.Columns.Add("cmodified_by", typeof(string));
        //    table.Columns.Add("lmodified_date", typeof(DateTime));
        //    table.Columns.Add("nIsDeleted", typeof(bool));
        //    table.Columns.Add("cdeleted_by", typeof(string));
        //    table.Columns.Add("ldeleted_date", typeof(DateTime));

        //    // Fill DataTable safely
        //    foreach (var u in users)
        //    {
        //        var row = table.NewRow();
        //        row["cuserid"] = u.cuserid;
        //        row["ctenant_id"] = u.ctenantID;
        //        row["cuser_name"] = u.cusername ?? (object)DBNull.Value;
        //        row["cemail"] = u.cemail ?? (object)DBNull.Value;
        //        row["cpassword"] = u.cpassword ?? (object)DBNull.Value;
        //        row["nIs_active"] = u.nIsActive ?? true;
        //        row["cfirst_name"] = u.cfirstName ?? (object)DBNull.Value;
        //        row["clast_name"] = u.clastName ?? (object)DBNull.Value;
        //        row["cphoneno"] = u.cphoneno ?? (object)DBNull.Value;
        //        row["calternate_phone"] = u.cAlternatePhone ?? (object)DBNull.Value;
        //        row["ldob"] = u.ldob ?? (object)DBNull.Value;
        //        row["cmarital_status"] = u.cMaritalStatus ?? (object)DBNull.Value;
        //        row["cnation"] = u.cnation ?? (object)DBNull.Value;
        //        row["cgender"] = u.cgender ?? (object)DBNull.Value;
        //        row["caddress"] = u.caddress ?? (object)DBNull.Value;
        //        row["caddress1"] = u.caddress1 ?? (object)DBNull.Value;
        //        row["caddress2"] = u.caddress2 ?? (object)DBNull.Value;
        //        row["cpincode"] = u.cpincode ?? (object)DBNull.Value;
        //        row["ccity"] = u.ccity ?? (object)DBNull.Value;
        //        row["cstate_code"] = u.cstatecode ?? (object)DBNull.Value;
        //        row["cstate_desc"] = u.cstatedesc ?? (object)DBNull.Value;
        //        row["ccountry_code"] = u.ccountrycode ?? (object)DBNull.Value;
        //        row["ProfileImage"] = u.ProfileImage ?? (object)DBNull.Value;
        //        row["cbank_name"] = u.cbankName ?? (object)DBNull.Value;
        //        row["caccount_number"] = u.caccountNumber ?? (object)DBNull.Value;
        //        row["ciFSC_code"] = u.ciFSCCode ?? (object)DBNull.Value;
        //        row["cpan"] = u.cpAN ?? (object)DBNull.Value;
        //        row["ldoj"] = u.ldoj ?? (object)DBNull.Value;
        //        row["cemployment_status"] = u.cemploymentStatus ?? (object)DBNull.Value;
        //        row["nnotice_period_days"] = u.nnoticePeriodDays ?? (object)DBNull.Value;
        //        row["lresignation_date"] = u.lresignationDate ?? (object)DBNull.Value;
        //        row["llast_working_date"] = u.llastWorkingDate ?? (object)DBNull.Value;
        //        row["cemp_category"] = u.cempcategory ?? (object)DBNull.Value;
        //        row["cwork_loc_code"] = u.cworkloccode ?? (object)DBNull.Value;
        //        row["cwork_loc_name"] = u.cworklocname ?? (object)DBNull.Value;
        //        row["crole_id"] = u.croleID ?? (object)DBNull.Value;
        //        row["crole_code"] = u.crolecode ?? (object)DBNull.Value;
        //        row["crole_name"] = u.crolename ?? (object)DBNull.Value;
        //        row["cgrade_code"] = u.cgradecode ?? (object)DBNull.Value;
        //        row["cgrade_desc"] = u.cgradedesc ?? (object)DBNull.Value;
        //        row["csub_role_code"] = u.csubrolecode ?? (object)DBNull.Value;
        //        row["cdept_code"] = u.cdeptcode ?? (object)DBNull.Value;
        //        row["cdept_desc"] = u.cdeptdesc ?? (object)DBNull.Value;
        //        row["cjob_code"] = u.cjobcode ?? (object)DBNull.Value;
        //        row["cjob_desc"] = u.cjobdesc ?? (object)DBNull.Value;
        //        row["creport_mgr_code"] = u.creportmgrcode ?? (object)DBNull.Value;
        //        row["creport_mgr_name"] = u.creportmgrname ?? (object)DBNull.Value;
        //        row["cRoll_id"] = u.cRoll_id ?? (object)DBNull.Value;
        //        row["cRoll_name"] = u.cRoll_name ?? (object)DBNull.Value;
        //        row["cRoll_Id_mngr"] = u.cRoll_Id_mngr ?? (object)DBNull.Value;
        //        row["cRoll_Id_mngr_desc"] = u.cRoll_Id_mngr_desc ?? (object)DBNull.Value;
        //        row["creport_manager_empcode"] = u.cReportManager_empcode ?? (object)DBNull.Value;
        //        row["creport_manager_poscode"] = u.cReportManager_Poscode ?? (object)DBNull.Value;
        //        row["creport_manager_pos_desc"] = u.cReportManager_Posdesc ?? (object)DBNull.Value;
        //        row["nis_web_access_enabled"] = u.nIsWebAccessEnabled ?? false;
        //        row["nis_event_read"] = u.nIsEventRead ?? false;
        //        row["llast_login_at"] = u.lLastLoginAt ?? (object)DBNull.Value;
        //        row["nfailed_logina_attempts"] = u.nFailedLoginAttempts ?? (object)DBNull.Value;
        //        row["cpassword_changed_at"] = u.cPasswordChangedAt ?? (object)DBNull.Value;
        //        row["nis_locked"] = u.nIsLocked ?? false;
        //        row["last_login_ip"] = u.LastLoginIP ?? (object)DBNull.Value;
        //        row["last_login_device"] = u.LastLoginDevice ?? (object)DBNull.Value;
        //        row["ccreated_date"] = u.ccreateddate ?? DateTime.Now;
        //        row["ccreated_by"] = u.ccreatedby ?? (object)DBNull.Value;
        //        row["cmodified_by"] = u.cmodifiedby ?? (object)DBNull.Value;
        //        row["lmodified_date"] = u.lmodifieddate ?? DateTime.Now;
        //        row["nIsDeleted"] = u.nIsDeleted ?? false;
        //        row["cdeleted_by"] = u.cDeletedBy ?? (object)DBNull.Value;
        //        row["ldeleted_date"] = u.lDeletedDate ?? (object)DBNull.Value;

        //        table.Rows.Add(row);
        //    }

        //    // Add TVP parameter
        //    cmd.Parameters.Add("@UserList", SqlDbType.Structured).Value = table;
        //    cmd.Parameters["@UserList"].TypeName = "dbo.UserTableType";

        //    // Add OUTPUT parameter
        //    var outputParam = new SqlParameter("@InsertedCount", SqlDbType.Int) { Direction = ParameterDirection.Output };
        //    cmd.Parameters.Add(outputParam);

        //    await conn.OpenAsync();
        //    await cmd.ExecuteNonQueryAsync();

        //    return outputParam.Value != DBNull.Value ? (int)outputParam.Value : 0;
        //}



        //public async Task<int> InsertUsersBulkAsync(List<CreateUserDTO> users, int cTenantID, string usernameClaim)
        //{
        //    if (users == null || !users.Any())
        //        return 0;

        //    var connStr = _config.GetConnectionString("Database");

        //    var table = new DataTable();
        //    table.Columns.Add("cuserid", typeof(int));
        //    table.Columns.Add("ctenant_id", typeof(int));
        //    table.Columns.Add("cuser_name", typeof(string));
        //    table.Columns.Add("cpassword", typeof(string));
        //    table.Columns.Add("cemail", typeof(string));
        //    table.Columns.Add("nIs_active", typeof(bool));
        //    table.Columns.Add("cfirst_name", typeof(string));
        //    table.Columns.Add("clast_name", typeof(string));
        //    table.Columns.Add("cphoneno", typeof(string));
        //    table.Columns.Add("calternate_phone", typeof(string));
        //    table.Columns.Add("ldob", typeof(DateTime));
        //    table.Columns.Add("cmarital_status", typeof(string));
        //    table.Columns.Add("cnation", typeof(string));
        //    table.Columns.Add("cgender", typeof(string));
        //    table.Columns.Add("caddress", typeof(string));
        //    table.Columns.Add("caddress1", typeof(string));
        //    table.Columns.Add("caddress2", typeof(string));
        //    table.Columns.Add("cpincode", typeof(string));
        //    table.Columns.Add("ccity", typeof(string));
        //    table.Columns.Add("cstate_code", typeof(string));
        //    table.Columns.Add("cstate_desc", typeof(string));
        //    table.Columns.Add("ccountry_code", typeof(string));
        //    table.Columns.Add("profile_image", typeof(string));  // lowercase
        //    table.Columns.Add("cbank_name", typeof(string));
        //    table.Columns.Add("caccount_number", typeof(string));
        //    table.Columns.Add("ciFSC_code", typeof(string));
        //    table.Columns.Add("cpan", typeof(string));
        //    table.Columns.Add("ldoj", typeof(DateTime));
        //    table.Columns.Add("cemployment_status", typeof(string));
        //    table.Columns.Add("nnotice_period_days", typeof(int));
        //    table.Columns.Add("lresignation_date", typeof(DateTime));
        //    table.Columns.Add("llast_working_date", typeof(DateTime));
        //    table.Columns.Add("cemp_category", typeof(string));
        //    table.Columns.Add("cwork_loc_code", typeof(string));
        //    table.Columns.Add("cwork_loc_name", typeof(string));
        //    table.Columns.Add("crole_id", typeof(int));
        //    table.Columns.Add("crole_code", typeof(string));
        //    table.Columns.Add("crole_name", typeof(string));
        //    table.Columns.Add("cgrade_code", typeof(string));
        //    table.Columns.Add("cgrade_desc", typeof(string));
        //    table.Columns.Add("csub_role_code", typeof(string));
        //    table.Columns.Add("cdept_code", typeof(string));
        //    table.Columns.Add("cdept_desc", typeof(string));
        //    table.Columns.Add("cjob_code", typeof(string));
        //    table.Columns.Add("cjob_desc", typeof(string));
        //    table.Columns.Add("creport_mgr_code", typeof(string));
        //    table.Columns.Add("creport_mgr_name", typeof(string));
        //    table.Columns.Add("cRoll_id", typeof(string));
        //    table.Columns.Add("cRoll_name", typeof(string));
        //    table.Columns.Add("cRoll_Id_mngr", typeof(string));
        //    table.Columns.Add("cRoll_Id_mngr_desc", typeof(string));
        //    table.Columns.Add("creport_manager_empcode", typeof(string));
        //    table.Columns.Add("creport_manager_poscode", typeof(string));
        //    table.Columns.Add("creport_manager_pos_desc", typeof(string));
        //    table.Columns.Add("nis_web_access_enabled", typeof(bool));
        //    table.Columns.Add("nis_event_read", typeof(bool));
        //    table.Columns.Add("llast_login_at", typeof(DateTime));
        //    table.Columns.Add("nfailed_logina_attempts", typeof(int));
        //    table.Columns.Add("cpassword_changed_at", typeof(DateTime));
        //    table.Columns.Add("nis_locked", typeof(bool));
        //    table.Columns.Add("last_login_ip", typeof(string));
        //    table.Columns.Add("last_login_device", typeof(string));
        //    table.Columns.Add("ccreated_date", typeof(DateTime));
        //    table.Columns.Add("ccreated_by", typeof(string));
        //    table.Columns.Add("cmodified_by", typeof(string));
        //    table.Columns.Add("lmodified_date", typeof(DateTime));
        //    table.Columns.Add("nIs_deleted", typeof(bool));
        //    table.Columns.Add("cdeleted_by", typeof(string));
        //    table.Columns.Add("ldeleted_date", typeof(DateTime));

        //    foreach (var u in users)
        //    {
        //        var row = table.NewRow();
        //        row["cuserid"] = u.cuserid;
        //        row["ctenant_id"] = cTenantID;
        //        row["cuser_name"] = u.cusername ?? (object)DBNull.Value;
        //        row["cpassword"] = u.cpassword ?? (object)DBNull.Value;
        //        row["cemail"] = u.cemail ?? (object)DBNull.Value;
        //        row["nIs_active"] = u.nIsActive ?? true;
        //        row["cfirst_name"] = u.cfirstName ?? (object)DBNull.Value;
        //        row["clast_name"] = u.clastName ?? (object)DBNull.Value;
        //        row["cphoneno"] = u.cphoneno ?? (object)DBNull.Value;
        //        row["calternate_phone"] = u.cAlternatePhone ?? (object)DBNull.Value;
        //        row["ldob"] = u.ldob ?? (object)DBNull.Value;
        //        row["cmarital_status"] = u.cMaritalStatus ?? (object)DBNull.Value;
        //        row["cnation"] = u.cnation ?? (object)DBNull.Value;
        //        row["cgender"] = u.cgender ?? (object)DBNull.Value;
        //        row["caddress"] = u.caddress ?? (object)DBNull.Value;
        //        row["caddress1"] = u.caddress1 ?? (object)DBNull.Value;
        //        row["caddress2"] = u.caddress2 ?? (object)DBNull.Value;
        //        row["cpincode"] = u.cpincode ?? (object)DBNull.Value;
        //        row["ccity"] = u.ccity ?? (object)DBNull.Value;
        //        row["cstate_code"] = u.cstatecode ?? (object)DBNull.Value;
        //        row["cstate_desc"] = u.cstatedesc ?? (object)DBNull.Value;
        //        row["ccountry_code"] = u.ccountrycode ?? (object)DBNull.Value;
        //        row["profile_image"] = u.ProfileImage ?? (object)DBNull.Value;  // FIXED: lowercase to match DataTable
        //        row["cbank_name"] = u.cbankName ?? (object)DBNull.Value;
        //        row["caccount_number"] = u.caccountNumber ?? (object)DBNull.Value;
        //        row["ciFSC_code"] = u.ciFSCCode ?? (object)DBNull.Value;
        //        row["cpan"] = u.cpAN ?? (object)DBNull.Value;
        //        row["ldoj"] = u.ldoj ?? (object)DBNull.Value;
        //        row["cemployment_status"] = u.cemploymentStatus ?? (object)DBNull.Value;
        //        row["nnotice_period_days"] = u.nnoticePeriodDays ?? (object)DBNull.Value;
        //        row["lresignation_date"] = u.lresignationDate ?? (object)DBNull.Value;
        //        row["llast_working_date"] = u.llastWorkingDate ?? (object)DBNull.Value;
        //        row["cemp_category"] = u.cempcategory ?? (object)DBNull.Value;
        //        row["cwork_loc_code"] = u.cworkloccode ?? (object)DBNull.Value;
        //        row["cwork_loc_name"] = u.cworklocname ?? (object)DBNull.Value;
        //        row["crole_id"] = 3 ;
        //        row["crole_code"] = u.crolecode ?? (object)DBNull.Value;
        //        row["crole_name"] = u.crolename ?? (object)DBNull.Value;
        //        row["cgrade_code"] = u.cgradecode ?? (object)DBNull.Value;
        //        row["cgrade_desc"] = u.cgradedesc ?? (object)DBNull.Value;
        //        row["csub_role_code"] = u.csubrolecode ?? (object)DBNull.Value;
        //        row["cdept_code"] = u.cdeptcode ?? (object)DBNull.Value;
        //        row["cdept_desc"] = u.cdeptdesc ?? (object)DBNull.Value;
        //        row["cjob_code"] = u.cjobcode ?? (object)DBNull.Value;
        //        row["cjob_desc"] = u.cjobdesc ?? (object)DBNull.Value;
        //        row["creport_mgr_code"] = u.creportmgrcode ?? (object)DBNull.Value;
        //        row["creport_mgr_name"] = u.creportmgrname ?? (object)DBNull.Value;
        //        row["cRoll_id"] = u.cRoll_id ?? (object)DBNull.Value;
        //        row["cRoll_name"] = u.cRoll_name ?? (object)DBNull.Value;
        //        row["cRoll_Id_mngr"] = u.cRoll_Id_mngr ?? (object)DBNull.Value;
        //        row["cRoll_Id_mngr_desc"] = u.cRoll_Id_mngr_desc ?? (object)DBNull.Value;
        //        row["creport_manager_empcode"] = u.cReportManager_empcode ?? (object)DBNull.Value;
        //        row["creport_manager_poscode"] = u.cReportManager_Poscode ?? (object)DBNull.Value;
        //        row["creport_manager_pos_desc"] = u.cReportManager_Posdesc ?? (object)DBNull.Value;
        //        row["nis_web_access_enabled"] = u.nIsWebAccessEnabled ?? false;
        //        row["nis_event_read"] = u.nIsEventRead ?? false;
        //        row["llast_login_at"] = u.lLastLoginAt ?? (object)DBNull.Value;
        //        row["nfailed_logina_attempts"] = u.nFailedLoginAttempts ?? (object)DBNull.Value;
        //        row["cpassword_changed_at"] = u.cPasswordChangedAt ?? (object)DBNull.Value;
        //        row["nis_locked"] = u.nIsLocked ?? false;
        //        row["last_login_ip"] = u.LastLoginIP ?? (object)DBNull.Value;
        //        row["last_login_device"] = u.LastLoginDevice ?? (object)DBNull.Value;
        //        row["ccreated_date"] = u.ccreateddate ?? DateTime.Now;
        //        row["ccreated_by"] = usernameClaim;
        //        row["cmodified_by"] = usernameClaim;
        //        row["lmodified_date"] = u.lmodifieddate ?? DateTime.Now;
        //        row["nIs_deleted"] = u.nIsDeleted ?? false;
        //        row["cdeleted_by"] = u.cDeletedBy ?? (object)DBNull.Value;
        //        row["ldeleted_date"] = u.lDeletedDate ?? (object)DBNull.Value;
        //        table.Rows.Add(row);
        //    }

        //    using var conn = new SqlConnection(connStr);
        //    await conn.OpenAsync();

        //    using var bulkCopy = new SqlBulkCopy(conn)
        //    {
        //        DestinationTableName = "Users"
        //    };

        //    // Use consistent casing - all source names should match DataTable column names exactly
        //    bulkCopy.ColumnMappings.Add("cuserid", "cuserid");
        //    bulkCopy.ColumnMappings.Add("ctenant_id", "ctenant_id");
        //    bulkCopy.ColumnMappings.Add("cuser_name", "cuser_name");
        //    bulkCopy.ColumnMappings.Add("cpassword", "cpassword");
        //    bulkCopy.ColumnMappings.Add("cemail", "cemail");
        //    bulkCopy.ColumnMappings.Add("nIs_active", "nIs_active");
        //    bulkCopy.ColumnMappings.Add("cfirst_name", "cfirst_name");
        //    bulkCopy.ColumnMappings.Add("clast_name", "clast_name");
        //    bulkCopy.ColumnMappings.Add("cphoneno", "cphoneno");
        //    bulkCopy.ColumnMappings.Add("calternate_phone", "calternate_phone");
        //    bulkCopy.ColumnMappings.Add("ldob", "ldob");
        //    bulkCopy.ColumnMappings.Add("cmarital_status", "cmarital_status");
        //    bulkCopy.ColumnMappings.Add("cnation", "cnation");
        //    bulkCopy.ColumnMappings.Add("cgender", "cgender");
        //    bulkCopy.ColumnMappings.Add("caddress", "caddress");
        //    bulkCopy.ColumnMappings.Add("caddress1", "caddress1");
        //    bulkCopy.ColumnMappings.Add("caddress2", "caddress2");
        //    bulkCopy.ColumnMappings.Add("cpincode", "cpincode");
        //    bulkCopy.ColumnMappings.Add("ccity", "ccity");
        //    bulkCopy.ColumnMappings.Add("cstate_code", "cstate_code");
        //    bulkCopy.ColumnMappings.Add("cstate_desc", "cstate_desc");
        //    bulkCopy.ColumnMappings.Add("ccountry_code", "ccountry_code");
        //    bulkCopy.ColumnMappings.Add("cpan", "cpan");
        //    bulkCopy.ColumnMappings.Add("ldoj", "ldoj");
        //    bulkCopy.ColumnMappings.Add("crole_id", "crole_id");
        //    bulkCopy.ColumnMappings.Add("crole_name", "crole_name");
        //    bulkCopy.ColumnMappings.Add("crole_code", "crole_code");
        //    bulkCopy.ColumnMappings.Add("cdept_code", "cdept_code");
        //    bulkCopy.ColumnMappings.Add("cdept_desc", "cdept_desc");
        //    bulkCopy.ColumnMappings.Add("creport_mgr_code", "creport_mgr_code");
        //    bulkCopy.ColumnMappings.Add("creport_mgr_name", "creport_mgr_name");
        //    bulkCopy.ColumnMappings.Add("cwork_loc_code", "cwork_loc_code");
        //    bulkCopy.ColumnMappings.Add("cwork_loc_name", "cwork_loc_name");
        //    bulkCopy.ColumnMappings.Add("cbank_name", "cbank_name");
        //    bulkCopy.ColumnMappings.Add("caccount_number", "caccount_number");


        //    bulkCopy.ColumnMappings.Add("profile_image", "Profile_Image");  // DataTable -> Database
        //    bulkCopy.ColumnMappings.Add("cbank_name", "cbank_name");
        //    bulkCopy.ColumnMappings.Add("caccount_number", "caccount_number");
        //    bulkCopy.ColumnMappings.Add("ciFSC_code", "ciFSC_code");
        //    bulkCopy.ColumnMappings.Add("cemployment_status", "cemployment_status");
        //    bulkCopy.ColumnMappings.Add("nnotice_period_days", "nnotice_period_days");
        //    bulkCopy.ColumnMappings.Add("lresignation_date", "lresignation_date");
        //    bulkCopy.ColumnMappings.Add("llast_working_date", "llast_working_date");
        //    bulkCopy.ColumnMappings.Add("cemp_category", "cemp_category");
        //    bulkCopy.ColumnMappings.Add("cgrade_code", "cgrade_code");
        //    bulkCopy.ColumnMappings.Add("cgrade_desc", "cgrade_desc");
        //    bulkCopy.ColumnMappings.Add("csub_role_code", "csub_role_code");
        //    bulkCopy.ColumnMappings.Add("cjob_code", "cjob_code");
        //    bulkCopy.ColumnMappings.Add("cjob_desc", "cjob_desc");
        //    bulkCopy.ColumnMappings.Add("cRoll_id", "cRoll_id");
        //    bulkCopy.ColumnMappings.Add("cRoll_name", "cRoll_name");
        //    bulkCopy.ColumnMappings.Add("cRoll_Id_mngr", "cRoll_Id_mngr");
        //    bulkCopy.ColumnMappings.Add("cRoll_Id_mngr_desc", "cRoll_Id_mngr_desc");
        //    bulkCopy.ColumnMappings.Add("creport_manager_empcode", "creport_manager_empcode");
        //    bulkCopy.ColumnMappings.Add("creport_manager_poscode", "creport_manager_poscode");
        //    bulkCopy.ColumnMappings.Add("creport_manager_pos_desc", "creport_manager_pos_desc");
        //    bulkCopy.ColumnMappings.Add("nis_web_access_enabled", "nis_web_access_enabled");
        //    bulkCopy.ColumnMappings.Add("nis_event_read", "nis_event_read");
        //    bulkCopy.ColumnMappings.Add("llast_login_at", "llast_login_at");
        //    bulkCopy.ColumnMappings.Add("nfailed_logina_attempts", "nfailed_logina_attempts");
        //    bulkCopy.ColumnMappings.Add("cpassword_changed_at", "cpassword_changed_at");
        //    bulkCopy.ColumnMappings.Add("nis_locked", "nis_locked");
        //    bulkCopy.ColumnMappings.Add("last_login_ip", "last_login_ip");
        //    bulkCopy.ColumnMappings.Add("last_login_device", "last_login_device");
        //    bulkCopy.ColumnMappings.Add("ccreated_date", "ccreated_date");
        //    bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
        //    bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
        //    bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
        //    bulkCopy.ColumnMappings.Add("nIs_deleted", "nIs_deleted");
        //    bulkCopy.ColumnMappings.Add("cdeleted_by", "cdeleted_by");
        //    bulkCopy.ColumnMappings.Add("ldeleted_date", "ldeleted_date");

        //    await bulkCopy.WriteToServerAsync(table);
        //    return table.Rows.Count;
        //}



        public async Task<int> InsertUsersBulkAsync(List<BulkUserDTO> users, int cTenantID, string usernameClaim)
        {
            if (users == null || !users.Any())
                return 0;

            var connStr = _config.GetConnectionString("Database");

            var table = new DataTable();
            table.Columns.Add("cuserid", typeof(int));
            table.Columns.Add("ctenant_id", typeof(int));
            table.Columns.Add("cuser_name", typeof(string));
            table.Columns.Add("cpassword", typeof(string));
            table.Columns.Add("cemail", typeof(string));
            table.Columns.Add("nIs_active", typeof(bool));
            table.Columns.Add("cfirst_name", typeof(string));
            table.Columns.Add("clast_name", typeof(string));
            table.Columns.Add("cphoneno", typeof(string));
            table.Columns.Add("calternate_phone", typeof(string));
            table.Columns.Add("ldob", typeof(DateTime));
            table.Columns.Add("cmarital_status", typeof(string));
            table.Columns.Add("cnation", typeof(string));
            table.Columns.Add("cgender", typeof(string));
            table.Columns.Add("caddress", typeof(string));
            table.Columns.Add("caddress1", typeof(string));
            table.Columns.Add("caddress2", typeof(string));
            table.Columns.Add("cpincode", typeof(string));
            table.Columns.Add("ccity", typeof(string));
            table.Columns.Add("cstate_code", typeof(string));
            table.Columns.Add("cstate_desc", typeof(string));
            table.Columns.Add("ccountry_code", typeof(string));
            table.Columns.Add("profile_image", typeof(string));  // lowercase
            table.Columns.Add("cbank_name", typeof(string));
            table.Columns.Add("caccount_number", typeof(string));
            // table.Columns.Add("ciFSC_code", typeof(string));
            table.Columns.Add("ciFSC_code", typeof(string));
            table.Columns.Add("cpan", typeof(string));
            table.Columns.Add("ldoj", typeof(DateTime));
            table.Columns.Add("cemployment_status", typeof(string));
            table.Columns.Add("nnotice_period_days", typeof(int));
            table.Columns.Add("lresignation_date", typeof(DateTime));
            table.Columns.Add("llast_working_date", typeof(DateTime));
            table.Columns.Add("cemp_category", typeof(string));
            table.Columns.Add("cwork_loc_code", typeof(string));
            table.Columns.Add("cwork_loc_name", typeof(string));
            table.Columns.Add("crole_id", typeof(int));
            table.Columns.Add("crole_code", typeof(string));
            table.Columns.Add("crole_name", typeof(string));
            table.Columns.Add("cgrade_code", typeof(string));
            table.Columns.Add("cgrade_desc", typeof(string));
            table.Columns.Add("csub_role_code", typeof(string));
            table.Columns.Add("cdept_code", typeof(string));
            table.Columns.Add("cdept_desc", typeof(string));
            table.Columns.Add("cjob_code", typeof(string));
            table.Columns.Add("cjob_desc", typeof(string));
            table.Columns.Add("creport_mgr_code", typeof(string));
            table.Columns.Add("creport_mgr_name", typeof(string));
            table.Columns.Add("croll_id", typeof(string));
            table.Columns.Add("croll_name", typeof(string));
            table.Columns.Add("croll_id_mngr", typeof(string));
            table.Columns.Add("croll_id_mngr_desc", typeof(string));
            table.Columns.Add("creport_manager_empcode", typeof(string));
            table.Columns.Add("creport_manager_poscode", typeof(string));
            table.Columns.Add("creport_manager_pos_desc", typeof(string));
            table.Columns.Add("nis_web_access_enabled", typeof(bool));
            table.Columns.Add("nis_event_read", typeof(bool));
            table.Columns.Add("llast_login_at", typeof(DateTime));
            table.Columns.Add("nfailed_logina_attempts", typeof(int));
            table.Columns.Add("cpassword_changed_at", typeof(DateTime));
            table.Columns.Add("nis_locked", typeof(bool));
            table.Columns.Add("last_login_ip", typeof(string));
            table.Columns.Add("last_login_device", typeof(string));
            table.Columns.Add("ccreated_date", typeof(DateTime));
            table.Columns.Add("ccreated_by", typeof(string));
            table.Columns.Add("cmodified_by", typeof(string));
            table.Columns.Add("lmodified_date", typeof(DateTime));
            table.Columns.Add("nIs_deleted", typeof(bool));
            table.Columns.Add("cdeleted_by", typeof(string));
            table.Columns.Add("ldeleted_date", typeof(DateTime));

            foreach (var u in users)
            {
                var row = table.NewRow();
                row["cuserid"] = u.cuserid;
                row["ctenant_id"] = cTenantID;
                row["cuser_name"] = u.cusername ?? (object)DBNull.Value;
                row["cpassword"] = u.cpassword ?? (object)DBNull.Value;
                row["cemail"] = u.cemail ?? (object)DBNull.Value;
                row["nIs_active"] = true;// u.nIsActive ?? true;
                row["cfirst_name"] = u.cfirstName ?? (object)DBNull.Value;
                row["clast_name"] = u.clastName ?? (object)DBNull.Value;
                row["cphoneno"] = u.cphoneno ?? (object)DBNull.Value;
                row["calternate_phone"] = u.cAlternatePhone ?? (object)DBNull.Value;
                row["ldob"] = u.ldob ?? (object)DBNull.Value;
                row["cmarital_status"] = u.cMaritalStatus ?? (object)DBNull.Value;
                row["cnation"] = u.cnation ?? (object)DBNull.Value;
                row["cgender"] = u.cgender ?? (object)DBNull.Value;
                row["caddress"] = u.caddress ?? (object)DBNull.Value;
                row["caddress1"] = u.caddress1 ?? (object)DBNull.Value;
                row["caddress2"] = u.caddress2 ?? (object)DBNull.Value;
                row["cpincode"] = u.cpincode ?? (object)DBNull.Value;
                row["ccity"] = u.ccity ?? (object)DBNull.Value;
                row["cstate_code"] = u.cstatecode ?? (object)DBNull.Value;
                row["cstate_desc"] = u.cstatedesc ?? (object)DBNull.Value;
                row["ccountry_code"] = u.ccountrycode ?? (object)DBNull.Value;
                //row["profile_image"] = u.ProfileImage ?? (object)DBNull.Value;  // FIXED: lowercase to match DataTable
                row["cbank_name"] = u.cbankName ?? (object)DBNull.Value;
                row["caccount_number"] = u.caccountNumber ?? (object)DBNull.Value;
                row["ciFSC_code"] = u.ciFSC_code ?? (object)DBNull.Value;
                // row["ciFSC_code"] = u.ciFSC_code ?? (object)DBNull.Value;
                row["cpAN"] = u.cpAN ?? (object)DBNull.Value;
                row["ldoj"] = u.ldoj ?? (object)DBNull.Value;
                row["cemployment_status"] = u.cemploymentStatus ?? (object)DBNull.Value;
                row["nnotice_period_days"] = u.nnoticePeriodDays ?? (object)DBNull.Value;
                //row["lresignation_date"] = u.lresignationDate ?? (object)DBNull.Value;
                //row["llast_working_date"] = u.llastWorkingDate ?? (object)DBNull.Value;
                row["cemp_category"] = u.cempcategory ?? (object)DBNull.Value;
                row["cwork_loc_code"] = u.cworkloccode ?? (object)DBNull.Value;
                row["cwork_loc_name"] = u.cworklocname ?? (object)DBNull.Value;
                row["crole_id"] = 3;
                row["crole_code"] = usernameClaim;//u.crolecode ?? (object)DBNull.Value;
                row["crole_name"] = usernameClaim; //u.crolename ?? (object)DBNull.Value;
                row["cgrade_code"] = u.cgradecode ?? (object)DBNull.Value;
                row["cgrade_desc"] = u.cgradedesc ?? (object)DBNull.Value;
                row["csub_role_code"] = u.csubrolecode ?? (object)DBNull.Value;
                row["cdept_code"] = u.cdeptcode ?? (object)DBNull.Value;
                row["cdept_desc"] = u.cdeptdesc ?? (object)DBNull.Value;
                row["cjob_code"] = u.cjobcode ?? (object)DBNull.Value;
                row["cjob_desc"] = u.cjobdesc ?? (object)DBNull.Value;
                row["creport_mgr_code"] = u.creportmgrcode ?? (object)DBNull.Value;
                row["creport_mgr_name"] = u.creportmgrname ?? (object)DBNull.Value;
                row["croll_id"] = u.croll_id ?? (object)DBNull.Value;
                row["croll_name"] = u.croll_name ?? (object)DBNull.Value;
                row["croll_id_mngr"] = u.croll_id_mngr ?? (object)DBNull.Value;
                row["croll_id_mngr_desc"] = u.croll_id_mngr_desc ?? (object)DBNull.Value;
                row["creport_manager_empcode"] = u.cReportManager_empcode ?? (object)DBNull.Value;
                row["creport_manager_poscode"] = u.cReportManager_Poscode ?? (object)DBNull.Value;
                row["creport_manager_pos_desc"] = u.cReportManager_Posdesc ?? (object)DBNull.Value;
                row["nis_web_access_enabled"] = true;//u.nIsWebAccessEnabled ?? false;
                row["nis_event_read"] = true;//u.nIsEventRead ?? false;
                //row["llast_login_at"] = u.lLastLoginAt ?? (object)DBNull.Value;
                row["nfailed_logina_attempts"] = 0;// u.nFailedLoginAttempts ?? (object)DBNull.Value;
                //row["cpassword_changed_at"] = u.cPasswordChangedAt ?? (object)DBNull.Value;
                row["nis_locked"] = false;//u.nIsLocked ?? false;
                //row["last_login_ip"] = u.LastLoginIP ?? (object)DBNull.Value;
                //row["last_login_device"] = u.LastLoginDevice ?? (object)DBNull.Value;
                //row["ccreated_date"] = u.ccreateddate ?? DateTime.Now;
                row["ccreated_by"] = usernameClaim;
                row["cmodified_by"] = usernameClaim;
                //row["lmodified_date"] = u.lmodifieddate ?? DateTime.Now;
                row["nIs_deleted"] = false; //u.nIsDeleted ?? false;
                //row["cdeleted_by"] = u.cDeletedBy ?? (object)DBNull.Value;
                //row["ldeleted_date"] = u.lDeletedDate ?? (object)DBNull.Value;
                table.Rows.Add(row);
            }

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = "Users"
            };

            // Use consistent casing - all source names should match DataTable column names exactly
            bulkCopy.ColumnMappings.Add("cuserid", "cuserid");
            bulkCopy.ColumnMappings.Add("ctenant_id", "ctenant_id");
            bulkCopy.ColumnMappings.Add("cuser_name", "cuser_name");
            bulkCopy.ColumnMappings.Add("cpassword", "cpassword");
            bulkCopy.ColumnMappings.Add("cemail", "cemail");
            bulkCopy.ColumnMappings.Add("nIs_active", "nIs_active");
            bulkCopy.ColumnMappings.Add("cfirst_name", "cfirst_name");
            bulkCopy.ColumnMappings.Add("clast_name", "clast_name");
            bulkCopy.ColumnMappings.Add("cphoneno", "cphoneno");
            bulkCopy.ColumnMappings.Add("calternate_phone", "calternate_phone");
            bulkCopy.ColumnMappings.Add("ldob", "ldob");
            bulkCopy.ColumnMappings.Add("cmarital_status", "cmarital_status");
            bulkCopy.ColumnMappings.Add("cnation", "cnation");
            bulkCopy.ColumnMappings.Add("cgender", "cgender");
            bulkCopy.ColumnMappings.Add("caddress", "caddress");
            bulkCopy.ColumnMappings.Add("caddress1", "caddress1");
            bulkCopy.ColumnMappings.Add("caddress2", "caddress2");
            bulkCopy.ColumnMappings.Add("cpincode", "cpincode");
            bulkCopy.ColumnMappings.Add("ccity", "ccity");
            bulkCopy.ColumnMappings.Add("cstate_code", "cstate_code");
            bulkCopy.ColumnMappings.Add("cstate_desc", "cstate_desc");
            bulkCopy.ColumnMappings.Add("ccountry_code", "ccountry_code");
            bulkCopy.ColumnMappings.Add("cpan", "cpan");
            bulkCopy.ColumnMappings.Add("ldoj", "ldoj");
            bulkCopy.ColumnMappings.Add("crole_id", "crole_id");
            bulkCopy.ColumnMappings.Add("crole_name", "crole_name");
            bulkCopy.ColumnMappings.Add("crole_code", "crole_code");
            bulkCopy.ColumnMappings.Add("cdept_code", "cdept_code");
            bulkCopy.ColumnMappings.Add("cdept_desc", "cdept_desc");
            bulkCopy.ColumnMappings.Add("creport_mgr_code", "creport_mgr_code");
            bulkCopy.ColumnMappings.Add("creport_mgr_name", "creport_mgr_name");
            bulkCopy.ColumnMappings.Add("cwork_loc_code", "cwork_loc_code");
            bulkCopy.ColumnMappings.Add("cwork_loc_name", "cwork_loc_name");
            bulkCopy.ColumnMappings.Add("cbank_name", "cbank_name");
            bulkCopy.ColumnMappings.Add("caccount_number", "caccount_number");


            //bulkCopy.ColumnMappings.Add("profile_image", "Profile_Image");  // DataTable -> Database
            // bulkCopy.ColumnMappings.Add("cbank_name", "cbank_name");
            // Add this line with the other column mappings
            bulkCopy.ColumnMappings.Add("ciFSC_code", "ciFSC_code");
            //bulkCopy.ColumnMappings.Add("caccount_number", "caccount_number");
            //bulkCopy.ColumnMappings.Add("ciFSC_code", "ciFSC_code");
            bulkCopy.ColumnMappings.Add("cemployment_status", "cemployment_status");
            bulkCopy.ColumnMappings.Add("nnotice_period_days", "nnotice_period_days");
            //bulkCopy.ColumnMappings.Add("lresignation_date", "lresignation_date");
            //bulkCopy.ColumnMappings.Add("llast_working_date", "llast_working_date");
            bulkCopy.ColumnMappings.Add("cemp_category", "cemp_category");
            bulkCopy.ColumnMappings.Add("cgrade_code", "cgrade_code");
            bulkCopy.ColumnMappings.Add("cgrade_desc", "cgrade_desc");
            bulkCopy.ColumnMappings.Add("csub_role_code", "csub_role_code");
            bulkCopy.ColumnMappings.Add("cjob_code", "cjob_code");
            bulkCopy.ColumnMappings.Add("cjob_desc", "cjob_desc");
            bulkCopy.ColumnMappings.Add("croll_id", "croll_id");
            bulkCopy.ColumnMappings.Add("croll_name", "croll_name");
            bulkCopy.ColumnMappings.Add("croll_id_mngr", "croll_id_mngr");
            bulkCopy.ColumnMappings.Add("croll_id_mngr_desc", "croll_id_mngr_desc");
            bulkCopy.ColumnMappings.Add("creport_manager_empcode", "creport_manager_empcode");
            bulkCopy.ColumnMappings.Add("creport_manager_poscode", "creport_manager_poscode");
            bulkCopy.ColumnMappings.Add("creport_manager_pos_desc", "creport_manager_pos_desc");
            bulkCopy.ColumnMappings.Add("nis_web_access_enabled", "nis_web_access_enabled");
            bulkCopy.ColumnMappings.Add("nis_event_read", "nis_event_read");
            //bulkCopy.ColumnMappings.Add("llast_login_at", "llast_login_at");
            //bulkCopy.ColumnMappings.Add("nfailed_logina_attempts", "nfailed_logina_attempts");
            //bulkCopy.ColumnMappings.Add("cpassword_changed_at", "cpassword_changed_at");
            bulkCopy.ColumnMappings.Add("nis_locked", "nis_locked");
            ////bulkCopy.ColumnMappings.Add("last_login_ip", "last_login_ip");
            //bulkCopy.ColumnMappings.Add("last_login_device", "last_login_device");
            // bulkCopy.ColumnMappings.Add("ccreated_date", "ccreated_date");
            bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
            bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
            //bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
            //bulkcopy.Columnmappings.add("nis_deleted", "nis_deleted");
            bulkCopy.ColumnMappings.Add("nIs_deleted", "nIs_deleted");
            //bulkCopy.ColumnMappings.Add("cdeleted_by", "cdeleted_by");
            //bulkCopy.ColumnMappings.Add("ldeleted_date", "ldeleted_date");

            await bulkCopy.WriteToServerAsync(table);
            return table.Rows.Count;
        }




        public async Task<int> InsertUserApiAsync(List<UserApiDTO> users, int cTenantID, string usernameClaim)
        {
            if (users == null || !users.Any())
                return 0;

            var connStr = _config.GetConnectionString("Database");

            var table = new DataTable();
            table.Columns.Add("cuserid", typeof(int));
            table.Columns.Add("ctenant_id", typeof(int));
            table.Columns.Add("cuser_name", typeof(string));
            table.Columns.Add("cpassword", typeof(string));
            table.Columns.Add("cemail", typeof(string));
            table.Columns.Add("nIs_active", typeof(bool));
            table.Columns.Add("cfirst_name", typeof(string));
            table.Columns.Add("clast_name", typeof(string));
            table.Columns.Add("cphoneno", typeof(string));
            table.Columns.Add("calternate_phone", typeof(string));
            table.Columns.Add("ldob", typeof(DateTime));
            table.Columns.Add("cmarital_status", typeof(string));
            table.Columns.Add("cnation", typeof(string));
            table.Columns.Add("cgender", typeof(string));
            table.Columns.Add("caddress", typeof(string));
            table.Columns.Add("caddress1", typeof(string));
            table.Columns.Add("caddress2", typeof(string));
            table.Columns.Add("cpincode", typeof(string));
            table.Columns.Add("ccity", typeof(string));
            table.Columns.Add("cstate_code", typeof(string));
            table.Columns.Add("cstate_desc", typeof(string));
            table.Columns.Add("ccountry_code", typeof(string));
            table.Columns.Add("profile_image", typeof(string));  // lowercase
            table.Columns.Add("cbank_name", typeof(string));
            table.Columns.Add("caccount_number", typeof(string));
            // table.Columns.Add("ciFSC_code", typeof(string));
            table.Columns.Add("ciFSC_code", typeof(string));
            table.Columns.Add("cpan", typeof(string));
            table.Columns.Add("ldoj", typeof(DateTime));
            table.Columns.Add("cemployment_status", typeof(string));
            table.Columns.Add("nnotice_period_days", typeof(int));
            table.Columns.Add("lresignation_date", typeof(DateTime));
            table.Columns.Add("llast_working_date", typeof(DateTime));
            table.Columns.Add("cemp_category", typeof(string));
            table.Columns.Add("cwork_loc_code", typeof(string));
            table.Columns.Add("cwork_loc_name", typeof(string));
            table.Columns.Add("crole_id", typeof(int));
            table.Columns.Add("crole_code", typeof(string));
            table.Columns.Add("crole_name", typeof(string));
            table.Columns.Add("cgrade_code", typeof(string));
            table.Columns.Add("cgrade_desc", typeof(string));
            table.Columns.Add("csub_role_code", typeof(string));
            table.Columns.Add("cdept_code", typeof(string));
            table.Columns.Add("cdept_desc", typeof(string));
            table.Columns.Add("cjob_code", typeof(string));
            table.Columns.Add("cjob_desc", typeof(string));
            table.Columns.Add("creport_mgr_code", typeof(string));
            table.Columns.Add("creport_mgr_name", typeof(string));
            table.Columns.Add("croll_id", typeof(string));
            table.Columns.Add("croll_name", typeof(string));
            table.Columns.Add("croll_id_mngr", typeof(string));
            table.Columns.Add("croll_id_mngr_desc", typeof(string));
            table.Columns.Add("creport_manager_empcode", typeof(string));
            table.Columns.Add("creport_manager_poscode", typeof(string));
            table.Columns.Add("creport_manager_pos_desc", typeof(string));
            table.Columns.Add("nis_web_access_enabled", typeof(bool));
            table.Columns.Add("nis_event_read", typeof(bool));
            table.Columns.Add("llast_login_at", typeof(DateTime));
            table.Columns.Add("nfailed_logina_attempts", typeof(int));
            table.Columns.Add("cpassword_changed_at", typeof(DateTime));
            table.Columns.Add("nis_locked", typeof(bool));
            table.Columns.Add("last_login_ip", typeof(string));
            table.Columns.Add("last_login_device", typeof(string));
            table.Columns.Add("ccreated_date", typeof(DateTime));
            table.Columns.Add("ccreated_by", typeof(string));
            table.Columns.Add("cmodified_by", typeof(string));
            table.Columns.Add("lmodified_date", typeof(DateTime));
            table.Columns.Add("nIs_deleted", typeof(bool));
            table.Columns.Add("cdeleted_by", typeof(string));
            table.Columns.Add("ldeleted_date", typeof(DateTime));

            foreach (var u in users)
            {
                var row = table.NewRow();
                row["cuserid"] = u.cuserid;
                row["ctenant_id"] = cTenantID;
                row["cuser_name"] = u.cusername ?? (object)DBNull.Value;
                row["cpassword"] = u.cpassword ?? (object)DBNull.Value;
                row["cemail"] = u.cemail ?? (object)DBNull.Value;
                row["nIs_active"] = true;// u.nIsActive ?? true;
                row["cfirst_name"] = u.cfirstName ?? (object)DBNull.Value;
                row["clast_name"] = u.clastName ?? (object)DBNull.Value;
                row["cphoneno"] = u.cphoneno ?? (object)DBNull.Value;
                row["calternate_phone"] = u.cAlternatePhone ?? (object)DBNull.Value;
                row["ldob"] = u.ldob ?? (object)DBNull.Value;
                row["cmarital_status"] = u.cMaritalStatus ?? (object)DBNull.Value;
                row["cnation"] = u.cnation ?? (object)DBNull.Value;
                row["cgender"] = u.cgender ?? (object)DBNull.Value;
                row["caddress"] = u.caddress ?? (object)DBNull.Value;
                row["caddress1"] = u.caddress1 ?? (object)DBNull.Value;
                row["caddress2"] = u.caddress2 ?? (object)DBNull.Value;
                row["cpincode"] = u.cpincode ?? (object)DBNull.Value;
                row["ccity"] = u.ccity ?? (object)DBNull.Value;
                row["cstate_code"] = u.cstatecode ?? (object)DBNull.Value;
                row["cstate_desc"] = u.cstatedesc ?? (object)DBNull.Value;
                row["ccountry_code"] = u.ccountrycode ?? (object)DBNull.Value;
                //row["profile_image"] = u.ProfileImage ?? (object)DBNull.Value;  // FIXED: lowercase to match DataTable
                row["cbank_name"] = u.cbankName ?? (object)DBNull.Value;
                row["caccount_number"] = u.caccountNumber ?? (object)DBNull.Value;
                row["ciFSC_code"] = u.ciFSC_code ?? (object)DBNull.Value;
                // row["ciFSC_code"] = u.ciFSC_code ?? (object)DBNull.Value;
                row["cpAN"] = u.cpAN ?? (object)DBNull.Value;
                row["ldoj"] = u.ldoj ?? (object)DBNull.Value;
                row["cemployment_status"] = u.cemploymentStatus ?? (object)DBNull.Value;
                row["nnotice_period_days"] = u.nnoticePeriodDays ?? (object)DBNull.Value;
                //row["lresignation_date"] = u.lresignationDate ?? (object)DBNull.Value;
                //row["llast_working_date"] = u.llastWorkingDate ?? (object)DBNull.Value;
                row["cemp_category"] = u.cempcategory ?? (object)DBNull.Value;
                row["cwork_loc_code"] = u.cworkloccode ?? (object)DBNull.Value;
                row["cwork_loc_name"] = u.cworklocname ?? (object)DBNull.Value;
                row["crole_id"] = 3;
                row["crole_code"] = usernameClaim;//u.crolecode ?? (object)DBNull.Value;
                row["crole_name"] = usernameClaim; //u.crolename ?? (object)DBNull.Value;
                row["cgrade_code"] = u.cgradecode ?? (object)DBNull.Value;
                row["cgrade_desc"] = u.cgradedesc ?? (object)DBNull.Value;
                row["csub_role_code"] = u.csubrolecode ?? (object)DBNull.Value;
                row["cdept_code"] = u.cdeptcode ?? (object)DBNull.Value;
                row["cdept_desc"] = u.cdeptdesc ?? (object)DBNull.Value;
                row["cjob_code"] = u.cjobcode ?? (object)DBNull.Value;
                row["cjob_desc"] = u.cjobdesc ?? (object)DBNull.Value;
                row["creport_mgr_code"] = u.creportmgrcode ?? (object)DBNull.Value;
                row["creport_mgr_name"] = u.creportmgrname ?? (object)DBNull.Value;
                row["croll_id"] = u.croll_id ?? (object)DBNull.Value;
                row["croll_name"] = u.croll_name ?? (object)DBNull.Value;
                row["croll_id_mngr"] = u.croll_id_mngr ?? (object)DBNull.Value;
                row["croll_id_mngr_desc"] = u.croll_id_mngr_desc ?? (object)DBNull.Value;
                row["creport_manager_empcode"] = u.cReportManager_empcode ?? (object)DBNull.Value;
                row["creport_manager_poscode"] = u.cReportManager_Poscode ?? (object)DBNull.Value;
                row["creport_manager_pos_desc"] = u.cReportManager_Posdesc ?? (object)DBNull.Value;
                row["nis_web_access_enabled"] = true;//u.nIsWebAccessEnabled ?? false;
                row["nis_event_read"] = true;//u.nIsEventRead ?? false;
                //row["llast_login_at"] = u.lLastLoginAt ?? (object)DBNull.Value;
                row["nfailed_logina_attempts"] = 0;// u.nFailedLoginAttempts ?? (object)DBNull.Value;
                //row["cpassword_changed_at"] = u.cPasswordChangedAt ?? (object)DBNull.Value;
                row["nis_locked"] = false;//u.nIsLocked ?? false;
                //row["last_login_ip"] = u.LastLoginIP ?? (object)DBNull.Value;
                //row["last_login_device"] = u.LastLoginDevice ?? (object)DBNull.Value;
                //row["ccreated_date"] = u.ccreateddate ?? DateTime.Now;
                row["ccreated_by"] = usernameClaim;
                row["cmodified_by"] = usernameClaim;
                //row["lmodified_date"] = u.lmodifieddate ?? DateTime.Now;
                row["nIs_deleted"] = false; //u.nIsDeleted ?? false;
                //row["cdeleted_by"] = u.cDeletedBy ?? (object)DBNull.Value;
                //row["ldeleted_date"] = u.lDeletedDate ?? (object)DBNull.Value;
                table.Rows.Add(row);
            }

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = "Users"
            };

            // Use consistent casing - all source names should match DataTable column names exactly
            bulkCopy.ColumnMappings.Add("cuserid", "cuserid");
            bulkCopy.ColumnMappings.Add("ctenant_id", "ctenant_id");
            bulkCopy.ColumnMappings.Add("cuser_name", "cuser_name");
            bulkCopy.ColumnMappings.Add("cpassword", "cpassword");
            bulkCopy.ColumnMappings.Add("cemail", "cemail");
            bulkCopy.ColumnMappings.Add("nIs_active", "nIs_active");
            bulkCopy.ColumnMappings.Add("cfirst_name", "cfirst_name");
            bulkCopy.ColumnMappings.Add("clast_name", "clast_name");
            bulkCopy.ColumnMappings.Add("cphoneno", "cphoneno");
            bulkCopy.ColumnMappings.Add("calternate_phone", "calternate_phone");
            bulkCopy.ColumnMappings.Add("ldob", "ldob");
            bulkCopy.ColumnMappings.Add("cmarital_status", "cmarital_status");
            bulkCopy.ColumnMappings.Add("cnation", "cnation");
            bulkCopy.ColumnMappings.Add("cgender", "cgender");
            bulkCopy.ColumnMappings.Add("caddress", "caddress");
            bulkCopy.ColumnMappings.Add("caddress1", "caddress1");
            bulkCopy.ColumnMappings.Add("caddress2", "caddress2");
            bulkCopy.ColumnMappings.Add("cpincode", "cpincode");
            bulkCopy.ColumnMappings.Add("ccity", "ccity");
            bulkCopy.ColumnMappings.Add("cstate_code", "cstate_code");
            bulkCopy.ColumnMappings.Add("cstate_desc", "cstate_desc");
            bulkCopy.ColumnMappings.Add("ccountry_code", "ccountry_code");
            bulkCopy.ColumnMappings.Add("cpan", "cpan");
            bulkCopy.ColumnMappings.Add("ldoj", "ldoj");
            bulkCopy.ColumnMappings.Add("crole_id", "crole_id");
            bulkCopy.ColumnMappings.Add("crole_name", "crole_name");
            bulkCopy.ColumnMappings.Add("crole_code", "crole_code");
            bulkCopy.ColumnMappings.Add("cdept_code", "cdept_code");
            bulkCopy.ColumnMappings.Add("cdept_desc", "cdept_desc");
            bulkCopy.ColumnMappings.Add("creport_mgr_code", "creport_mgr_code");
            bulkCopy.ColumnMappings.Add("creport_mgr_name", "creport_mgr_name");
            bulkCopy.ColumnMappings.Add("cwork_loc_code", "cwork_loc_code");
            bulkCopy.ColumnMappings.Add("cwork_loc_name", "cwork_loc_name");
            bulkCopy.ColumnMappings.Add("cbank_name", "cbank_name");
            bulkCopy.ColumnMappings.Add("caccount_number", "caccount_number");


            //bulkCopy.ColumnMappings.Add("profile_image", "Profile_Image");  // DataTable -> Database
            // bulkCopy.ColumnMappings.Add("cbank_name", "cbank_name");
            // Add this line with the other column mappings
            bulkCopy.ColumnMappings.Add("ciFSC_code", "ciFSC_code");
            //bulkCopy.ColumnMappings.Add("caccount_number", "caccount_number");
            //bulkCopy.ColumnMappings.Add("ciFSC_code", "ciFSC_code");
            bulkCopy.ColumnMappings.Add("cemployment_status", "cemployment_status");
            bulkCopy.ColumnMappings.Add("nnotice_period_days", "nnotice_period_days");
            //bulkCopy.ColumnMappings.Add("lresignation_date", "lresignation_date");
            //bulkCopy.ColumnMappings.Add("llast_working_date", "llast_working_date");
            bulkCopy.ColumnMappings.Add("cemp_category", "cemp_category");
            bulkCopy.ColumnMappings.Add("cgrade_code", "cgrade_code");
            bulkCopy.ColumnMappings.Add("cgrade_desc", "cgrade_desc");
            bulkCopy.ColumnMappings.Add("csub_role_code", "csub_role_code");
            bulkCopy.ColumnMappings.Add("cjob_code", "cjob_code");
            bulkCopy.ColumnMappings.Add("cjob_desc", "cjob_desc");
            bulkCopy.ColumnMappings.Add("croll_id", "croll_id");
            bulkCopy.ColumnMappings.Add("croll_name", "croll_name");
            bulkCopy.ColumnMappings.Add("croll_id_mngr", "croll_id_mngr");
            bulkCopy.ColumnMappings.Add("croll_id_mngr_desc", "croll_id_mngr_desc");
            bulkCopy.ColumnMappings.Add("creport_manager_empcode", "creport_manager_empcode");
            bulkCopy.ColumnMappings.Add("creport_manager_poscode", "creport_manager_poscode");
            bulkCopy.ColumnMappings.Add("creport_manager_pos_desc", "creport_manager_pos_desc");
            bulkCopy.ColumnMappings.Add("nis_web_access_enabled", "nis_web_access_enabled");
            bulkCopy.ColumnMappings.Add("nis_event_read", "nis_event_read");
            //bulkCopy.ColumnMappings.Add("llast_login_at", "llast_login_at");
            //bulkCopy.ColumnMappings.Add("nfailed_logina_attempts", "nfailed_logina_attempts");
            //bulkCopy.ColumnMappings.Add("cpassword_changed_at", "cpassword_changed_at");
            bulkCopy.ColumnMappings.Add("nis_locked", "nis_locked");
            ////bulkCopy.ColumnMappings.Add("last_login_ip", "last_login_ip");
            //bulkCopy.ColumnMappings.Add("last_login_device", "last_login_device");
            // bulkCopy.ColumnMappings.Add("ccreated_date", "ccreated_date");
            bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
            bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
            //bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
            //bulkcopy.Columnmappings.add("nis_deleted", "nis_deleted");
            bulkCopy.ColumnMappings.Add("nIs_deleted", "nIs_deleted");
            //bulkCopy.ColumnMappings.Add("cdeleted_by", "cdeleted_by");
            //bulkCopy.ColumnMappings.Add("ldeleted_date", "ldeleted_date");

            await bulkCopy.WriteToServerAsync(table);
            return table.Rows.Count;
        }

        // In your AccountService class
        //public async Task<BulkInsertResult> InsertUsersBulkAsync(List<CreateUserDTO> users)
        //{
        //    var result = new BulkInsertResult
        //    {
        //        Total = users.Count,
        //        Inserted = new List<object>(),
        //        Failed = new List<FailedUser>()
        //    };

        //    if (users == null || !users.Any())
        //        return result;

        //    using var conn = new SqlConnection(_config.GetConnectionString("Database"));
        //    await conn.OpenAsync();

        //    using var transaction = await conn.BeginTransactionAsync();

        //    try
        //    {
        //        // Step 1: Check all duplicates in one query (more efficient)
        //        var duplicateInfo = await CheckAllDuplicatesAsync(conn, transaction, users);

        //        // Step 2: Insert only non-duplicates using your existing DataTable approach
        //        var usersToInsert = users.Where(u => !duplicateInfo.IsDuplicate(u.cemail, u.cusername, u.cphoneno)).ToList();

        //        if (usersToInsert.Any())
        //        {
        //            var insertedCount = await BulkInsertNonDuplicatesAsync(conn, transaction, usersToInsert);
        //            result.Success = insertedCount;

        //            // Add inserted records to result
        //            result.Inserted = usersToInsert.Select(u => new {
        //                u.cemail,
        //                u.cusername,
        //                u.cfirstName,
        //                u.clastName
        //            }).Cast<object>().ToList();
        //        }

        //        // Step 3: Build failed list with specific reasons
        //        foreach (var user in users)
        //        {
        //            if (duplicateInfo.IsDuplicate(user.cemail, user.cusername, user.cphoneno))
        //            {
        //                result.Failed.Add(new FailedUser
        //                {
        //                    Email = user.cemail,
        //                    UserName = user.cusername,
        //                    Phone = user.cphoneno,
        //                    Reason = duplicateInfo.GetReason(user.cemail, user.cusername, user.cphoneno)
        //                });
        //            }
        //        }

        //        await transaction.CommitAsync();

        //        result.Status = result.Success == result.Total ? 200 : 207;
        //        result.StatusText = result.Success == result.Total ? "Success" : "Partial success";

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        await transaction.RollbackAsync();
        //        result.Status = 500;
        //        result.StatusText = $"Error: {ex.Message}";
        //        return result;
        //    }
        //}

        //private async Task<DuplicateInfo> CheckAllDuplicatesAsync(SqlConnection conn, SqlTransaction transaction, List<CreateUserDTO> users)
        //{
        //    var duplicateInfo = new DuplicateInfo();

        //    // Get existing emails, usernames, and phones in one query (much more efficient)
        //    var sql = @"
        //SELECT cemail, 'email' as Type FROM Users WHERE cemail IN @Emails AND ctenant_id = @TenantId
        //UNION ALL
        //SELECT cuser_name, 'username' as Type FROM Users WHERE cuser_name IN @UserNames AND ctenant_id = @TenantId
        //UNION ALL
        //SELECT cphoneno, 'phone' as Type FROM Users WHERE cphoneno IN @Phones AND ctenant_id = @TenantId";

        //    var existingEmails = users.Select(u => u.cemail).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
        //    var existingUserNames = users.Select(u => u.cusername).Where(u => !string.IsNullOrEmpty(u)).Distinct().ToList();
        //    var existingPhones = users.Select(u => u.cphoneno).Where(p => !string.IsNullOrEmpty(p)).Distinct().ToList();
        //    var tenantId = users.First().ctenantID; // Assuming all users have same tenant

        //    using var command = new SqlCommand(sql, conn, transaction);
        //    command.Parameters.AddWithValue("@Emails", existingEmails.Any() ? existingEmails : new[] { "" });
        //    command.Parameters.AddWithValue("@UserNames", existingUserNames.Any() ? existingUserNames : new[] { "" });
        //    command.Parameters.AddWithValue("@Phones", existingPhones.Any() ? existingPhones : new[] { "" });
        //    command.Parameters.AddWithValue("@TenantId", tenantId);

        //    using var reader = await command.ExecuteReaderAsync();
        //    while (await reader.ReadAsync())
        //    {
        //        var value = reader.GetString(0);
        //        var type = reader.GetString(1);

        //        if (type == "email")
        //            duplicateInfo.DuplicateEmails.Add(value);
        //        else if (type == "username")
        //            duplicateInfo.DuplicateUserNames.Add(value);
        //        else if (type == "phone")
        //            duplicateInfo.DuplicatePhones.Add(value);
        //    }

        //    return duplicateInfo;
        //}

        //private async Task<int> BulkInsertNonDuplicatesAsync(SqlConnection conn, SqlTransaction transaction, List<CreateUserDTO> users)
        //{
        //    var table = new DataTable();

        //    // Add all columns (your existing code)
        //    table.Columns.Add("cuserid", typeof(int));
        //    table.Columns.Add("ctenant_id", typeof(int));
        //    table.Columns.Add("cuser_name", typeof(string));
        //    table.Columns.Add("cpassword", typeof(string));
        //    table.Columns.Add("cemail", typeof(string));
        //    table.Columns.Add("nIs_active", typeof(bool));
        //    table.Columns.Add("cfirst_name", typeof(string));
        //    table.Columns.Add("clast_name", typeof(string));
        //    table.Columns.Add("cphoneno", typeof(string));
        //    table.Columns.Add("calternate_phone", typeof(string));
        //    table.Columns.Add("ldob", typeof(DateTime));
        //    table.Columns.Add("cmarital_status", typeof(string));
        //    table.Columns.Add("cnation", typeof(string));
        //    table.Columns.Add("cgender", typeof(string));
        //    table.Columns.Add("caddress", typeof(string));
        //    table.Columns.Add("caddress1", typeof(string));
        //    table.Columns.Add("caddress2", typeof(string));
        //    table.Columns.Add("cpincode", typeof(string));
        //    table.Columns.Add("ccity", typeof(string));
        //    table.Columns.Add("cstate_code", typeof(string));
        //    table.Columns.Add("cstate_desc", typeof(string));
        //    table.Columns.Add("ccountry_code", typeof(string));
        //    table.Columns.Add("profile_image", typeof(string));
        //    table.Columns.Add("cbank_name", typeof(string));
        //    table.Columns.Add("caccount_number", typeof(string));
        //    table.Columns.Add("ciFSC_code", typeof(string));
        //    table.Columns.Add("cpan", typeof(string));
        //    table.Columns.Add("ldoj", typeof(DateTime));
        //    table.Columns.Add("cemployment_status", typeof(string));
        //    table.Columns.Add("cemp_category", typeof(string));
        //    table.Columns.Add("cwork_loc_code", typeof(string));
        //    table.Columns.Add("cwork_loc_name", typeof(string));
        //    table.Columns.Add("crole_id", typeof(int));
        //    table.Columns.Add("crole_code", typeof(string));
        //    table.Columns.Add("crole_name", typeof(string));
        //    table.Columns.Add("cgrade_code", typeof(string));
        //    table.Columns.Add("cgrade_desc", typeof(string));
        //    table.Columns.Add("csub_role_code", typeof(string));
        //    table.Columns.Add("cdept_code", typeof(string));
        //    table.Columns.Add("cdept_desc", typeof(string));
        //    table.Columns.Add("cjob_code", typeof(string));
        //    table.Columns.Add("cjob_desc", typeof(string));
        //    table.Columns.Add("creport_mgr_code", typeof(string));
        //    table.Columns.Add("creport_mgr_name", typeof(string));
        //    table.Columns.Add("cRoll_id", typeof(string));
        //    table.Columns.Add("cRoll_name", typeof(string));
        //    table.Columns.Add("cRoll_Id_mngr", typeof(string));
        //    table.Columns.Add("cRoll_Id_mngr_desc", typeof(string));
        //    table.Columns.Add("creport_manager_empcode", typeof(string));
        //    table.Columns.Add("creport_manager_poscode", typeof(string));
        //    table.Columns.Add("creport_manager_pos_desc", typeof(string));
        //    table.Columns.Add("ccreated_date", typeof(DateTime));
        //    table.Columns.Add("ccreated_by", typeof(string));
        //    table.Columns.Add("cmodified_by", typeof(string));
        //    table.Columns.Add("lmodified_date", typeof(DateTime));
        //    table.Columns.Add("nIs_deleted", typeof(bool));
        //    table.Columns.Add("cdeleted_by", typeof(string));
        //    table.Columns.Add("ldeleted_date", typeof(DateTime));

        //    foreach (var u in users)
        //    {
        //        var row = table.NewRow();
        //        // Your existing row population code
        //        row["cuserid"] = u.cuserid;
        //        row["ctenant_id"] = u.ctenantID;
        //        row["cuser_name"] = u.cusername ?? (object)DBNull.Value;
        //        row["cpassword"] = u.cpassword ?? (object)DBNull.Value;
        //        row["cemail"] = u.cemail ?? (object)DBNull.Value;
        //        row["nIs_active"] = u.nIsActive ?? true;
        //        row["cfirst_name"] = u.cfirstName ?? (object)DBNull.Value;
        //        row["clast_name"] = u.clastName ?? (object)DBNull.Value;
        //        row["cphoneno"] = u.cphoneno ?? (object)DBNull.Value;
        //        row["calternate_phone"] = u.cAlternatePhone ?? (object)DBNull.Value;
        //        row["ldob"] = u.ldob ?? (object)DBNull.Value;
        //        row["cmarital_status"] = u.cMaritalStatus ?? (object)DBNull.Value;
        //        row["cnation"] = u.cnation ?? (object)DBNull.Value;
        //        row["cgender"] = u.cgender ?? (object)DBNull.Value;
        //        row["caddress"] = u.caddress ?? (object)DBNull.Value;
        //        row["caddress1"] = u.caddress1 ?? (object)DBNull.Value;
        //        row["caddress2"] = u.caddress2 ?? (object)DBNull.Value;
        //        row["cpincode"] = u.cpincode ?? (object)DBNull.Value;
        //        row["ccity"] = u.ccity ?? (object)DBNull.Value;
        //        row["cstate_code"] = u.cstatecode ?? (object)DBNull.Value;
        //        row["cstate_desc"] = u.cstatedesc ?? (object)DBNull.Value;
        //        row["ccountry_code"] = u.ccountrycode ?? (object)DBNull.Value;
        //        row["profile_image"] = u.ProfileImage ?? (object)DBNull.Value;
        //        row["cbank_name"] = u.cbankName ?? (object)DBNull.Value;
        //        row["caccount_number"] = u.caccountNumber ?? (object)DBNull.Value;
        //        row["ciFSC_code"] = u.ciFSCCode ?? (object)DBNull.Value;
        //        row["cpan"] = u.cpAN ?? (object)DBNull.Value;
        //        row["ldoj"] = u.ldoj ?? (object)DBNull.Value;
        //        row["cemployment_status"] = u.cemploymentStatus ?? (object)DBNull.Value;
        //        row["cemp_category"] = u.cempcategory ?? (object)DBNull.Value;
        //        row["cwork_loc_code"] = u.cworkloccode ?? (object)DBNull.Value;
        //        row["cwork_loc_name"] = u.cworklocname ?? (object)DBNull.Value;
        //        row["crole_id"] = u.croleID ?? (object)DBNull.Value;
        //        row["crole_code"] = u.crolecode ?? (object)DBNull.Value;
        //        row["crole_name"] = u.crolename ?? (object)DBNull.Value;
        //        row["cgrade_code"] = u.cgradecode ?? (object)DBNull.Value;
        //        row["cgrade_desc"] = u.cgradedesc ?? (object)DBNull.Value;
        //        row["csub_role_code"] = u.csubrolecode ?? (object)DBNull.Value;
        //        row["cdept_code"] = u.cdeptcode ?? (object)DBNull.Value;
        //        row["cdept_desc"] = u.cdeptdesc ?? (object)DBNull.Value;
        //        row["cjob_code"] = u.cjobcode ?? (object)DBNull.Value;
        //        row["cjob_desc"] = u.cjobdesc ?? (object)DBNull.Value;
        //        row["creport_mgr_code"] = u.creportmgrcode ?? (object)DBNull.Value;
        //        row["creport_mgr_name"] = u.creportmgrname ?? (object)DBNull.Value;
        //        row["cRoll_id"] = u.cRoll_id ?? (object)DBNull.Value;
        //        row["cRoll_name"] = u.cRoll_name ?? (object)DBNull.Value;
        //        row["cRoll_Id_mngr"] = u.cRoll_Id_mngr ?? (object)DBNull.Value;
        //        row["cRoll_Id_mngr_desc"] = u.cRoll_Id_mngr_desc ?? (object)DBNull.Value;
        //        row["creport_manager_empcode"] = u.cReportManager_empcode ?? (object)DBNull.Value;
        //        row["creport_manager_poscode"] = u.cReportManager_Poscode ?? (object)DBNull.Value;
        //        row["creport_manager_pos_desc"] = u.cReportManager_Posdesc ?? (object)DBNull.Value;
        //        row["ccreated_date"] = u.ccreateddate ?? DateTime.Now;
        //        row["ccreated_by"] = u.ccreatedby ?? (object)DBNull.Value;
        //        row["cmodified_by"] = u.cmodifiedby ?? (object)DBNull.Value;
        //        row["lmodified_date"] = u.lmodifieddate ?? DateTime.Now;
        //        row["nIs_deleted"] = u.nIsDeleted ?? false;
        //        row["cdeleted_by"] = u.cDeletedBy ?? (object)DBNull.Value;
        //        row["ldeleted_date"] = u.lDeletedDate ?? (object)DBNull.Value;
        //        table.Rows.Add(row);
        //    }

        //    using var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.Default, transaction)
        //    {
        //        DestinationTableName = "Users",
        //        BatchSize = 1000
        //    };

        //    // Add all column mappings (your existing mappings)
        //    foreach (DataColumn column in table.Columns)
        //    {
        //        bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        //    }

        //    await bulkCopy.WriteToServerAsync(table);
        //    return table.Rows.Count;
        //}

        public async Task<bool> InsertusersapisyncconfigAsync(usersapisyncDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();
                    string query = @"
                INSERT INTO tbl_users_api_sync_config (
                    ctenant_id, capi_method, capi_type, capi_url, 
                    capi_params, capi_headers, capi_config, capi_settings, cbody,cname,
                    nis_active, ccreated_by, lcreated_date, 
                    cmodified_by, lmodified_date
                ) VALUES(
                    @TenantID, @capi_method, @capi_type, @capi_url, 
                    @capi_params, @capi_headers, @capi_config, @capi_settings, @cbody,@cname
                    @nis_active, @ccreated_by, @lcreated_date, 
                    @cmodified_by, @lmodified_date
                )";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@capi_method", (object?)model.capi_method ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_type", (object?)model.capi_type ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_url", (object?)model.capi_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_params", (object?)model.capi_params ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_headers", (object?)model.capi_headers ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_config", (object?)model.capi_config ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_settings", (object?)model.capi_settings ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cbody", (object?)model.cbody ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cname", (object?)model.cname ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@ccreated_by", username);
                        cmd.Parameters.AddWithValue("@lcreated_date", DateTime.Now);
                        cmd.Parameters.AddWithValue("@cmodified_by", username);
                        cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in InsertusersapisyncconfigAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        public async Task<int> InsertDepartmentsBulkAsync(List<BulkDepartmentDTO> departments, int cTenantID, string usernameClaim)
        {
            if (departments == null || !departments.Any())
                return 0;

            var connStr = _config.GetConnectionString("Database");

            var table = new DataTable();
            table.Columns.Add("ctenant_id", typeof(int)); // Fixed: ctenent_id to ctenant_id
            table.Columns.Add("cdepartment_code", typeof(string));
            table.Columns.Add("cdepartment_name", typeof(string));
            table.Columns.Add("cdepartment_desc", typeof(string));
            table.Columns.Add("cdepartment_manager_rolecode", typeof(string));
            table.Columns.Add("cdepartment_manager_position_code", typeof(string));
            table.Columns.Add("cdepartment_manager_name", typeof(string));
            table.Columns.Add("cdepartment_email", typeof(string));
            table.Columns.Add("cdepartment_phone", typeof(string));
            table.Columns.Add("nis_active", typeof(bool));
            table.Columns.Add("ccreated_by", typeof(string));
            table.Columns.Add("lcreated_date", typeof(DateTime));
            table.Columns.Add("cmodified_by", typeof(string));
            table.Columns.Add("lmodified_date", typeof(DateTime));
            table.Columns.Add("nis_deleted", typeof(bool));

            foreach (var dept in departments)
            {
                var row = table.NewRow();
                row["ctenant_id"] = cTenantID; // Fixed: ctenent_id to ctenant_id
                row["cdepartment_code"] = dept.cdepartment_code ?? (object)DBNull.Value;
                row["cdepartment_name"] = dept.cdepartment_name ?? (object)DBNull.Value;
                row["cdepartment_desc"] = dept.cdepartment_desc ?? (object)DBNull.Value;
                row["cdepartment_manager_rolecode"] = dept.cdepartment_manager_rolecode ?? (object)DBNull.Value;
                row["cdepartment_manager_position_code"] = dept.cdepartment_manager_position_code ?? (object)DBNull.Value;
                row["cdepartment_manager_name"] = dept.cdepartment_manager_name ?? (object)DBNull.Value;
                row["cdepartment_email"] = dept.cdepartment_email ?? (object)DBNull.Value;
                row["cdepartment_phone"] = dept.cdepartment_phone ?? (object)DBNull.Value;
                row["nis_active"] = dept.nis_active ?? true;
                row["ccreated_by"] = usernameClaim;
                row["lcreated_date"] = DateTime.Now;
                row["cmodified_by"] = usernameClaim;
                row["lmodified_date"] = DateTime.Now;
                row["nis_deleted"] = false;
                table.Rows.Add(row);
            }

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = "tbl_department_master",
                BatchSize = 1000,
                BulkCopyTimeout = 300
            };

            // Fixed column mappings to match actual table structure
            bulkCopy.ColumnMappings.Add("ctenant_id", "ctenant_id"); // Fixed: ctenent_id to ctenant_id
            bulkCopy.ColumnMappings.Add("cdepartment_code", "cdepartment_code");
            bulkCopy.ColumnMappings.Add("cdepartment_name", "cdepartment_name");
            bulkCopy.ColumnMappings.Add("cdepartment_desc", "cdepartment_desc");
            bulkCopy.ColumnMappings.Add("cdepartment_manager_rolecode", "cdepartment_manager_rolecode");
            bulkCopy.ColumnMappings.Add("cdepartment_manager_position_code", "cdepartment_manager_position_code");
            bulkCopy.ColumnMappings.Add("cdepartment_manager_name", "cdepartment_manager_name");
            bulkCopy.ColumnMappings.Add("cdepartment_email", "cdepartment_email");
            bulkCopy.ColumnMappings.Add("cdepartment_phone", "cdepartment_phone");
            bulkCopy.ColumnMappings.Add("nis_active", "nis_active");
            bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
            bulkCopy.ColumnMappings.Add("lcreated_date", "lcreated_date");
            bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
            bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
            bulkCopy.ColumnMappings.Add("nis_deleted", "nis_deleted");

            try
            {
                await bulkCopy.WriteToServerAsync(table);
                return table.Rows.Count;
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627) // Unique constraint violation
            {
                throw new InvalidOperationException("Duplicate department codes found during insertion", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during bulk insert", ex);
            }
        }


        public async Task<List<string>> CheckExistingDepartmentCodesAsync(List<string> departmentCodes, int tenantId)
        {
            var existingCodes = new List<string>();
            var connStr = _config.GetConnectionString("Database");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            if (!departmentCodes.Any()) return existingCodes;

            // Create parameters for the list of codes
            var parameters = new List<SqlParameter>();
            var inClause = new List<string>();

            for (int i = 0; i < departmentCodes.Count; i++)
            {
                var paramName = $"@Code{i}";
                inClause.Add(paramName);
                parameters.Add(new SqlParameter(paramName, departmentCodes[i]));
            }

            string query = $@"
        SELECT cdepartment_code 
        FROM tbl_department_master 
        WHERE ctenant_id = @TenantID
        AND cdepartment_code IN ({string.Join(",", inClause)})
        AND nIs_deleted = 0";

            parameters.Add(new SqlParameter("@TenantID", tenantId));

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingCodes.Add(reader["cdepartment_code"]?.ToString() ?? "");
            }

            return existingCodes;
        }
        //public async Task<int> InsertDepartmentsBulkAsync(List<BulkDepartmentDTO> departments, int cTenantID, string usernameClaim)
        //{
        //    if (departments == null || !departments.Any())
        //        return 0;

        //    var connStr = _config.GetConnectionString("Database");

        //    var table = new DataTable();
        //    table.Columns.Add("ctenent_id", typeof(int));
        //    table.Columns.Add("cdepartment_code", typeof(string));
        //    table.Columns.Add("cdepartment_name", typeof(string));
        //    table.Columns.Add("cdepartment_desc", typeof(string));
        //    //table.Columns.Add("cdepartmentslug", typeof(string));
        //    table.Columns.Add("cdepartment_manager_rolecode", typeof(string));
        //    table.Columns.Add("cdepartment_manager_position_code", typeof(string));
        //    table.Columns.Add("cdepartment_manager_name", typeof(string));
        //    table.Columns.Add("cdepartment_email", typeof(string));
        //    table.Columns.Add("cdepartment_phone", typeof(string));
        //    table.Columns.Add("nis_active", typeof(bool));
        //    table.Columns.Add("ccreated_by", typeof(string));
        //    table.Columns.Add("lcreated_date", typeof(DateTime));
        //    table.Columns.Add("cmodified_by", typeof(string));
        //    table.Columns.Add("lmodified_date", typeof(DateTime));
        //    table.Columns.Add("nis_deleted", typeof(bool));

        //    foreach (var dept in departments)
        //    {
        //        var row = table.NewRow();
        //        row["ctenent_id"] = cTenantID;
        //        row["cdepartment_code"] = dept.cdepartment_code ?? (object)DBNull.Value;
        //        row["cdepartment_name"] = dept.cdepartment_name ?? (object)DBNull.Value;
        //        row["cdepartment_desc"] = dept.cdepartment_desc ?? (object)DBNull.Value;
        //        //row["cdepartmentslug"] = dept.cdepartmentslug ?? (object)DBNull.Value;
        //        row["cdepartment_manager_rolecode"] = dept.cdepartment_manager_rolecode ?? (object)DBNull.Value;
        //        row["cdepartment_manager_position_code"] = dept.cdepartment_manager_position_code ?? (object)DBNull.Value;
        //        row["cdepartment_manager_name"] = dept.cdepartment_manager_name ?? (object)DBNull.Value;
        //        row["cdepartment_email"] = dept.cdepartment_email ?? (object)DBNull.Value;
        //        row["cdepartment_phone"] = dept.cdepartment_phone ?? (object)DBNull.Value;
        //        row["nis_active"] = dept.nis_active ?? true; 
        //        row["ccreated_by"] = usernameClaim;
        //        row["lcreated_date"] = DateTime.Now;
        //        row["cmodified_by"] = usernameClaim;
        //        row["lmodified_date"] = DateTime.Now;
        //        row["nis_deleted"] = false;
        //        table.Rows.Add(row);
        //    }

        //    using var conn = new SqlConnection(connStr);
        //    await conn.OpenAsync();

        //    using var bulkCopy = new SqlBulkCopy(conn)
        //    {
        //        DestinationTableName = "tbl_department_master",
        //        BatchSize = 1000,
        //        BulkCopyTimeout = 300
        //    };

        //    bulkCopy.ColumnMappings.Add("ctenent_id", "ctenent_id");
        //    bulkCopy.ColumnMappings.Add("cdepartment_code", "cdepartment_code");
        //    bulkCopy.ColumnMappings.Add("cdepartment_name", "cdepartment_name");
        //    bulkCopy.ColumnMappings.Add("cdepartment_desc", "cdepartment_desc");
        //   // bulkCopy.ColumnMappings.Add("cdepartmentslug", "cdepartmentslug");
        //    bulkCopy.ColumnMappings.Add("cdepartment_manager_rolecode", "cdepartment_manager_rolecode");
        //    bulkCopy.ColumnMappings.Add("cdepartment_manager_position_code", "cdepartment_manager_position_code");
        //    bulkCopy.ColumnMappings.Add("cdepartment_manager_name", "cdepartment_manager_name");
        //    bulkCopy.ColumnMappings.Add("cdepartment_email", "cdepartment_email");
        //    bulkCopy.ColumnMappings.Add("cdepartment_phone", "cdepartment_phone");
        //    bulkCopy.ColumnMappings.Add("nis_active", "nis_active");
        //    bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
        //    bulkCopy.ColumnMappings.Add("lcreated_date", "lcreated_date");
        //    bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
        //    bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
        //    bulkCopy.ColumnMappings.Add("nis_deleted", "nis_deleted");

        //    await bulkCopy.WriteToServerAsync(table);
        //    return table.Rows.Count;
        //}

        //public async Task<int> InsertRolesBulkAsync(List<BulkRoleDTO> roles, int cTenantID, string usernameClaim)
        //{
        //    if (roles == null || !roles.Any())
        //        return 0;

        //    var connStr = _config.GetConnectionString("Database");

        //    var table = new DataTable();
        //    table.Columns.Add("ctenent_id", typeof(int));
        //    table.Columns.Add("crole_code", typeof(string));
        //    table.Columns.Add("crole_name", typeof(string));
        //    //table.Columns.Add("cslug", typeof(string));
        //    table.Columns.Add("crole_level", typeof(int));
        //    table.Columns.Add("cdepartment_code", typeof(string));
        //    table.Columns.Add("creporting_manager_code", typeof(string));
        //    table.Columns.Add("creporting_manager_name", typeof(string));
        //    table.Columns.Add("crole_description", typeof(string));
        //    table.Columns.Add("nis_active", typeof(bool));
        //    table.Columns.Add("ccreated_by", typeof(string));
        //    table.Columns.Add("lcreated_date", typeof(DateTime));
        //    table.Columns.Add("cmodified_by", typeof(string));
        //    table.Columns.Add("lmodified_date", typeof(DateTime));
        //    table.Columns.Add("nis_deleted", typeof(bool));

        //    foreach (var role in roles)
        //    {
        //        var row = table.NewRow();
        //        row["ctenent_id"] = cTenantID;
        //        row["crole_code"] = role.crole_code ?? (object)DBNull.Value;
        //        row["crole_name"] = role.crole_name ?? (object)DBNull.Value;
        //        //row["cslug"] = role.cslug ?? (object)DBNull.Value;
        //        row["crole_level"] = role.cslug ?? (object)DBNull.Value;
        //        row["cdepartment_code"] = role.cdepartment_code ?? (object)DBNull.Value;
        //        row["creporting_manager_code"] = role.creporting_manager_code ?? (object)DBNull.Value;
        //        row["creporting_manager_name"] = role.creporting_manager_name ?? (object)DBNull.Value;
        //        row["crole_description"] = role.crole_description ?? (object)DBNull.Value;
        //        row["nis_active"] = true; 
        //        row["ccreated_by"] = usernameClaim;
        //        row["lcreated_date"] = DateTime.Now;
        //        row["cmodified_by"] = usernameClaim;
        //        row["lmodified_date"] = DateTime.Now;
        //        row["nis_deleted"] = false;
        //        table.Rows.Add(row);
        //    }

        //    using var conn = new SqlConnection(connStr);
        //    await conn.OpenAsync();

        //    using var bulkCopy = new SqlBulkCopy(conn)
        //    {
        //        DestinationTableName = "tbl_role_master",
        //        BatchSize = 1000,
        //        BulkCopyTimeout = 300
        //    };

        //    bulkCopy.ColumnMappings.Add("ctenent_id", "ctenent_id");
        //    bulkCopy.ColumnMappings.Add("crole_code", "crole_code");
        //    bulkCopy.ColumnMappings.Add("crole_name", "crole_name");
        //    //bulkCopy.ColumnMappings.Add("cslug", "cslug");
        //    bulkCopy.ColumnMappings.Add("crole_level", "crole_level");
        //    bulkCopy.ColumnMappings.Add("cdepartment_code", "cdepartment_code");
        //    bulkCopy.ColumnMappings.Add("creporting_manager_code", "creporting_manager_code");
        //    bulkCopy.ColumnMappings.Add("creporting_manager_name", "creporting_manager_name");
        //    bulkCopy.ColumnMappings.Add("crole_description", "crole_description");
        //    bulkCopy.ColumnMappings.Add("nis_active", "nis_active");
        //    bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
        //    bulkCopy.ColumnMappings.Add("lcreated_date", "lcreated_date");
        //    bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
        //    bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
        //    bulkCopy.ColumnMappings.Add("nis_deleted", "nis_deleted");

        //    await bulkCopy.WriteToServerAsync(table);
        //    return table.Rows.Count;
        //}

        // In AccountService implementation
        public async Task<List<string>> CheckExistingRoleCodesAsync(List<string> roleCodes, int tenantId)
        {
            var existingCodes = new List<string>();
            var connStr = _config.GetConnectionString("Database");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            if (!roleCodes.Any()) return existingCodes;

            var parameters = new List<SqlParameter>();
            var inClause = new List<string>();

            for (int i = 0; i < roleCodes.Count; i++)
            {
                var paramName = $"@Code{i}";
                inClause.Add(paramName);
                parameters.Add(new SqlParameter(paramName, roleCodes[i]));
            }

            string query = $@"
        SELECT crole_code 
        FROM tbl_role_master 
        WHERE ctenant_id = @TenantID
        AND crole_code IN ({string.Join(",", inClause)})
        AND nIs_deleted = 0";

            parameters.Add(new SqlParameter("@TenantID", tenantId));

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingCodes.Add(reader["crole_code"]?.ToString() ?? "");
            }

            return existingCodes;
        }

        public async Task<int> InsertRolesBulkAsync(List<BulkRoleDTO> roles, int tenantId, string username)
        {
            if (roles == null || !roles.Any())
                return 0;

            var connStr = _config.GetConnectionString("Database");

            var table = new DataTable();
            table.Columns.Add("ctenant_id", typeof(int));
            table.Columns.Add("crole_code", typeof(string));
            table.Columns.Add("crole_name", typeof(string));
            table.Columns.Add("crole_description", typeof(string));
            table.Columns.Add("cslug", typeof(string));
            table.Columns.Add("crole_level", typeof(string));
            table.Columns.Add("cdepartment_code", typeof(string));
            table.Columns.Add("creporting_manager_code", typeof(string));
            table.Columns.Add("creporting_manager_name", typeof(string));
            table.Columns.Add("nis_active", typeof(bool));
            table.Columns.Add("ccreated_by", typeof(string));
            table.Columns.Add("lcreated_date", typeof(DateTime));
            table.Columns.Add("cmodified_by", typeof(string));
            table.Columns.Add("lmodified_date", typeof(DateTime));
            table.Columns.Add("nis_deleted", typeof(bool));

            foreach (var role in roles)
            {
                var row = table.NewRow();
                row["ctenant_id"] = tenantId;
                row["crole_code"] = role.crole_code ?? (object)DBNull.Value;
                row["crole_name"] = role.crole_name ?? (object)DBNull.Value;
                row["crole_description"] = role.crole_description ?? (object)DBNull.Value;

                row["crole_level"] = role.crole_level ?? (object)DBNull.Value;
                row["cdepartment_code"] = role.cdepartment_code ?? (object)DBNull.Value;
                row["creporting_manager_code"] = role.creporting_manager_code ?? (object)DBNull.Value;
                row["creporting_manager_name"] = role.creporting_manager_name ?? (object)DBNull.Value;
                row["nis_active"] = true;
                row["ccreated_by"] = username;
                row["lcreated_date"] = DateTime.Now;
                row["cmodified_by"] = username;
                row["lmodified_date"] = DateTime.Now;
                row["nis_deleted"] = false;
                table.Rows.Add(row);
            }

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = "tbl_role_master",
                BatchSize = 1000,
                BulkCopyTimeout = 300
            };

            // Add column mappings
            bulkCopy.ColumnMappings.Add("ctenant_id", "ctenant_id");
            bulkCopy.ColumnMappings.Add("crole_code", "crole_code");
            bulkCopy.ColumnMappings.Add("crole_name", "crole_name");
            bulkCopy.ColumnMappings.Add("crole_description", "crole_description");
            bulkCopy.ColumnMappings.Add("cslug", "cslug");
            bulkCopy.ColumnMappings.Add("crole_level", "crole_level");
            bulkCopy.ColumnMappings.Add("cdepartment_code", "cdepartment_code");
            bulkCopy.ColumnMappings.Add("creporting_manager_code", "creporting_manager_code");
            bulkCopy.ColumnMappings.Add("creporting_manager_name", "creporting_manager_name");
            bulkCopy.ColumnMappings.Add("nis_active", "nis_active");
            bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
            bulkCopy.ColumnMappings.Add("lcreated_date", "lcreated_date");
            bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
            bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
            bulkCopy.ColumnMappings.Add("nis_deleted", "nis_deleted");

            try
            {
                await bulkCopy.WriteToServerAsync(table);
                return table.Rows.Count;
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627)
            {
                throw new InvalidOperationException("Duplicate role codes found during insertion", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during bulk insert of roles", ex);
            }
        }

        //public async Task<int> InsertPositionsBulkAsync(List<BulkPositionDTO> positions, int cTenantID, string usernameClaim)
        //{
        //    if (positions == null || !positions.Any())
        //        return 0;

        //    var connStr = _config.GetConnectionString("Database");

        //    var table = new DataTable();
        //    table.Columns.Add("ctenent_id", typeof(int));
        //    table.Columns.Add("cposition_code", typeof(string));
        //    table.Columns.Add("cposition_name", typeof(string));
        //    table.Columns.Add("cposition_decsription", typeof(string));
        //    //table.Columns.Add("cposition_slug", typeof(string));
        //    table.Columns.Add("cdepartment_code", typeof(string));
        //    table.Columns.Add("creporting_manager_positionid", typeof(string));
        //    table.Columns.Add("creporting_manager_name", typeof(string));
        //    table.Columns.Add("nis_active", typeof(bool));
        //    table.Columns.Add("ccreated_by", typeof(string));
        //    table.Columns.Add("lcreated_date", typeof(DateTime));
        //    table.Columns.Add("cmodified_by", typeof(string));
        //    table.Columns.Add("lmodified_date", typeof(DateTime));
        //    table.Columns.Add("nis_deleted", typeof(bool));

        //    foreach (var position in positions)
        //    {
        //        var row = table.NewRow();
        //        row["ctenent_id"] = cTenantID;
        //        row["cposition_code"] = position.cposition_code ?? (object)DBNull.Value;
        //        row["cposition_name"] = position.cposition_name ?? (object)DBNull.Value;
        //        row["cposition_decsription"] = position.cposition_decsription ?? (object)DBNull.Value;
        //        //row["cposition_slug"] = position.cposition_slug ?? (object)DBNull.Value;
        //        row["cdepartment_code"] = position.cdepartment_code ?? (object)DBNull.Value;
        //        row["creporting_manager_positionid"] = position.creporting_manager_positionid ?? (object)DBNull.Value;
        //        row["creporting_manager_name"] = position.creporting_manager_name ?? (object)DBNull.Value;
        //        row["nis_active"] = true; 
        //        row["ccreated_by"] = usernameClaim;
        //        row["lcreated_date"] = DateTime.Now;
        //        row["cmodified_by"] = usernameClaim;
        //        row["lmodified_date"] = DateTime.Now;
        //        row["nis_deleted"] = false;
        //        table.Rows.Add(row);
        //    }

        //    using var conn = new SqlConnection(connStr);
        //    await conn.OpenAsync();

        //    using var bulkCopy = new SqlBulkCopy(conn)
        //    {
        //        DestinationTableName = "tbl_position_master",
        //        BatchSize = 1000,
        //        BulkCopyTimeout = 300
        //    };

        //    bulkCopy.ColumnMappings.Add("ctenent_id", "ctenent_id");
        //    bulkCopy.ColumnMappings.Add("cposition_code", "cposition_code");
        //    bulkCopy.ColumnMappings.Add("cposition_name", "cposition_name");
        //    bulkCopy.ColumnMappings.Add("cposition_decsription", "cposition_decsription");
        //    //bulkCopy.ColumnMappings.Add("cposition_slug", "cposition_slug");
        //    bulkCopy.ColumnMappings.Add("cdepartment_code", "cdepartment_code");
        //    bulkCopy.ColumnMappings.Add("creporting_manager_positionid", "creporting_manager_positionid");
        //    bulkCopy.ColumnMappings.Add("creporting_manager_name", "creporting_manager_name");
        //    bulkCopy.ColumnMappings.Add("nis_active", "nis_active");
        //    bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
        //    bulkCopy.ColumnMappings.Add("lcreated_date", "lcreated_date");
        //    bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
        //    bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
        //    bulkCopy.ColumnMappings.Add("nis_deleted", "nis_deleted");

        //    await bulkCopy.WriteToServerAsync(table);
        //    return table.Rows.Count;
        //}

        public async Task<int> InsertPositionsBulkAsync(List<BulkPositionDTO> positions, int cTenantID, string usernameClaim)
        {
            if (positions == null || !positions.Any())
                return 0;

            var connStr = _config.GetConnectionString("Database");

            var table = new DataTable();
            table.Columns.Add("ctenant_id", typeof(int)); // Fixed: ctenent_id to ctenant_id
            table.Columns.Add("cposition_code", typeof(string));
            table.Columns.Add("cposition_name", typeof(string));
            table.Columns.Add("cposition_decsription", typeof(string));
            //table.Columns.Add("cposition_slug", typeof(string));
            table.Columns.Add("cdepartment_code", typeof(string));
            table.Columns.Add("creporting_manager_positionid", typeof(string));
            table.Columns.Add("creporting_manager_name", typeof(string));
            table.Columns.Add("nis_active", typeof(bool));
            table.Columns.Add("ccreated_by", typeof(string));
            table.Columns.Add("lcreated_date", typeof(DateTime));
            table.Columns.Add("cmodified_by", typeof(string));
            table.Columns.Add("lmodified_date", typeof(DateTime));
            table.Columns.Add("nis_deleted", typeof(bool));

            foreach (var position in positions)
            {
                var row = table.NewRow();
                row["ctenant_id"] = cTenantID; // Fixed: ctenent_id to ctenant_id
                row["cposition_code"] = position.cposition_code ?? (object)DBNull.Value;
                row["cposition_name"] = position.cposition_name ?? (object)DBNull.Value;
                row["cposition_decsription"] = position.cposition_decsription ?? (object)DBNull.Value;
                //row["cposition_slug"] = position.cposition_slug ?? (object)DBNull.Value;
                row["cdepartment_code"] = position.cdepartment_code ?? (object)DBNull.Value;
                row["creporting_manager_positionid"] = position.creporting_manager_positionid ?? (object)DBNull.Value;
                row["creporting_manager_name"] = position.creporting_manager_name ?? (object)DBNull.Value;
                row["nis_active"] = true;
                row["ccreated_by"] = usernameClaim;
                row["lcreated_date"] = DateTime.Now;
                row["cmodified_by"] = usernameClaim;
                row["lmodified_date"] = DateTime.Now;
                row["nis_deleted"] = false;
                table.Rows.Add(row);
            }

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            using var bulkCopy = new SqlBulkCopy(conn)
            {
                DestinationTableName = "tbl_position_master",
                BatchSize = 1000,
                BulkCopyTimeout = 300
            };

            // Fixed column mappings
            bulkCopy.ColumnMappings.Add("ctenant_id", "ctenant_id"); // Fixed: ctenent_id to ctenant_id
            bulkCopy.ColumnMappings.Add("cposition_code", "cposition_code");
            bulkCopy.ColumnMappings.Add("cposition_name", "cposition_name");
            bulkCopy.ColumnMappings.Add("cposition_decsription", "cposition_decsription");
            //bulkCopy.ColumnMappings.Add("cposition_slug", "cposition_slug");
            bulkCopy.ColumnMappings.Add("cdepartment_code", "cdepartment_code");
            bulkCopy.ColumnMappings.Add("creporting_manager_positionid", "creporting_manager_positionid");
            bulkCopy.ColumnMappings.Add("creporting_manager_name", "creporting_manager_name");
            bulkCopy.ColumnMappings.Add("nis_active", "nis_active");
            bulkCopy.ColumnMappings.Add("ccreated_by", "ccreated_by");
            bulkCopy.ColumnMappings.Add("lcreated_date", "lcreated_date");
            bulkCopy.ColumnMappings.Add("cmodified_by", "cmodified_by");
            bulkCopy.ColumnMappings.Add("lmodified_date", "lmodified_date");
            bulkCopy.ColumnMappings.Add("nis_deleted", "nis_deleted");

            try
            {
                await bulkCopy.WriteToServerAsync(table);
                return table.Rows.Count;
            }
            catch (SqlException ex) when (ex.Number == 2601 || ex.Number == 2627) // Unique constraint violation
            {
                throw new InvalidOperationException("Duplicate position codes found during insertion", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error during bulk insert of positions", ex);
            }
        }

        public async Task<List<string>> CheckExistingPositionCodesAsync(List<string> positionCodes, int tenantId)
        {
            var existingCodes = new List<string>();
            var connStr = _config.GetConnectionString("Database");

            using var conn = new SqlConnection(connStr);
            await conn.OpenAsync();

            if (!positionCodes.Any()) return existingCodes;

            var parameters = new List<SqlParameter>();
            var inClause = new List<string>();

            for (int i = 0; i < positionCodes.Count; i++)
            {
                var paramName = $"@Code{i}";
                inClause.Add(paramName);
                parameters.Add(new SqlParameter(paramName, positionCodes[i]));
            }

            string query = $@"
        SELECT cposition_code 
        FROM tbl_position_master 
        WHERE ctenant_id = @TenantID
        AND cposition_code IN ({string.Join(",", inClause)})
        AND nIs_deleted = 0";

            parameters.Add(new SqlParameter("@TenantID", tenantId));

            using var cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddRange(parameters.ToArray());

            using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                existingCodes.Add(reader["cposition_code"]?.ToString() ?? "");
            }

            return existingCodes;
        }
        public async Task<List<GetusersapisyncDTO>> GetAllAPISyncConfigAsync(int cTenantID)
        {
            var connStr = _config.GetConnectionString("Database");
            var results = new List<GetusersapisyncDTO>();

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    ID, ctenant_id, capi_method, capi_type, capi_url, 
                    capi_params, capi_headers, capi_config, capi_settings, cbody,
                    nis_active, ccreated_by, lcreated_date,
                    cmodified_by, lmodified_date
                FROM tbl_users_api_sync_config 
                WHERE ctenant_id = @TenantID 
                AND nis_active = 1
                ORDER BY lcreated_date DESC";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var config = new GetusersapisyncDTO
                                {
                                    ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                                    ctenant_id = reader["ctenant_id"] != DBNull.Value ? Convert.ToInt32(reader["ctenant_id"]) : 0,
                                    capi_method = reader["capi_method"] != DBNull.Value ? reader["capi_method"].ToString() : null,
                                    capi_type = reader["capi_type"] != DBNull.Value ? reader["capi_type"].ToString() : null,
                                    capi_url = reader["capi_url"] != DBNull.Value ? reader["capi_url"].ToString() : null,
                                    capi_params = reader["capi_params"] != DBNull.Value ? reader["capi_params"].ToString() : null,
                                    capi_headers = reader["capi_headers"] != DBNull.Value ? reader["capi_headers"].ToString() : null,
                                    capi_config = reader["capi_config"] != DBNull.Value ? reader["capi_config"].ToString() : null,
                                    capi_settings = reader["capi_settings"] != DBNull.Value ? reader["capi_settings"].ToString() : null,
                                    cbody = reader["cbody"] != DBNull.Value ? reader["cbody"].ToString() : null,
                                    nis_active = reader["nis_active"] != DBNull.Value ? Convert.ToBoolean(reader["nis_active"]) : null,
                                    ccreated_by = reader["ccreated_by"] != DBNull.Value ? reader["ccreated_by"].ToString() : null,
                                    lcreated_date = reader["lcreated_date"] != DBNull.Value ? Convert.ToDateTime(reader["lcreated_date"]) : null,
                                    cmodified_by = reader["cmodified_by"] != DBNull.Value ? reader["cmodified_by"].ToString() : null,
                                    lmodified_date = reader["lmodified_date"] != DBNull.Value ? Convert.ToDateTime(reader["lmodified_date"]) : null
                                };
                                results.Add(config);
                            }
                        }
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllAPISyncConfigAsync: {ex.Message}");
                return new List<GetusersapisyncDTO>();
            }
        }
        public async Task<bool> DeleteAPISyncConfigAsync(DeleteAPISyncConfigDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                DELETE FROM tbl_users_api_sync_config 
                WHERE ID = @ID 
                AND ctenant_id = @TenantID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public async Task<bool> UpdateAPISyncConfigAsync(UpdateAPISyncConfigDTO model, int cTenantID, string username)
        {
            var connStr = _config.GetConnectionString("Database");
            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                UPDATE tbl_users_api_sync_config 
                SET 
                    capi_method = @capi_method,
                    capi_type = @capi_type,
                    capi_url = @capi_url,
                    capi_params = @capi_params,
                    capi_headers = @capi_headers,
                    capi_config = @capi_config,
                    capi_settings = @capi_settings,
                    cbody = @cbody,
                    nis_active = @nis_active,
                    cmodified_by = @username,
                    lmodified_date = GETDATE()
                WHERE ID = @ID 
                AND ctenant_id = @TenantID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", model.ID);
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);
                        cmd.Parameters.AddWithValue("@capi_method", (object?)model.capi_method ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_type", (object?)model.capi_type ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_url", (object?)model.capi_url ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_params", (object?)model.capi_params ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_headers", (object?)model.capi_headers ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_config", (object?)model.capi_config ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@capi_settings", (object?)model.capi_settings ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@cbody", (object?)model.cbody ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@nis_active", model.nis_active ?? true);
                        cmd.Parameters.AddWithValue("@username", username);

                        int rowsAffected = await cmd.ExecuteNonQueryAsync();
                        return rowsAffected > 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAPISyncConfigAsync: {ex.Message}");
                return false;
            }
        }
        public async Task<GetusersapisyncDTO> GetAPISyncConfigByIDAsync(int id, int cTenantID)
        {
            var connStr = _config.GetConnectionString("Database");

            try
            {
                using (SqlConnection conn = new SqlConnection(connStr))
                {
                    await conn.OpenAsync();

                    string query = @"
                SELECT 
                    ID, ctenant_id, capi_method, capi_type, capi_url, 
                    capi_params, capi_headers, capi_config, capi_settings, cbody,
                    nis_active, ccreated_by, lcreated_date,
                    cmodified_by, lmodified_date
                FROM tbl_users_api_sync_config 
                WHERE ID = @ID 
                AND ctenant_id = @TenantID";

                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@ID", id);
                        cmd.Parameters.AddWithValue("@TenantID", cTenantID);

                        using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new GetusersapisyncDTO
                                {
                                    ID = reader["ID"] != DBNull.Value ? Convert.ToInt32(reader["ID"]) : 0,
                                    ctenant_id = reader["ctenant_id"] != DBNull.Value ? Convert.ToInt32(reader["ctenant_id"]) : 0,
                                    capi_method = reader["capi_method"] != DBNull.Value ? reader["capi_method"].ToString() : null,
                                    capi_type = reader["capi_type"] != DBNull.Value ? reader["capi_type"].ToString() : null,
                                    capi_url = reader["capi_url"] != DBNull.Value ? reader["capi_url"].ToString() : null,
                                    capi_params = reader["capi_params"] != DBNull.Value ? reader["capi_params"].ToString() : null,
                                    capi_headers = reader["capi_headers"] != DBNull.Value ? reader["capi_headers"].ToString() : null,
                                    capi_config = reader["capi_config"] != DBNull.Value ? reader["capi_config"].ToString() : null,
                                    capi_settings = reader["capi_settings"] != DBNull.Value ? reader["capi_settings"].ToString() : null,
                                    cbody = reader["cbody"] != DBNull.Value ? reader["cbody"].ToString() : null,
                                    nis_active = reader["nis_active"] != DBNull.Value ? Convert.ToBoolean(reader["nis_active"]) : null,
                                    ccreated_by = reader["ccreated_by"] != DBNull.Value ? reader["ccreated_by"].ToString() : null,
                                    lcreated_date = reader["lcreated_date"] != DBNull.Value ? Convert.ToDateTime(reader["lcreated_date"]) : null,
                                    cmodified_by = reader["cmodified_by"] != DBNull.Value ? reader["cmodified_by"].ToString() : null,
                                    lmodified_date = reader["lmodified_date"] != DBNull.Value ? Convert.ToDateTime(reader["lmodified_date"]) : null
                                };
                            }
                        }
                    }
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAPISyncConfigByIDAsync: {ex.Message}");
                return null;
            }
        }
    }
}
