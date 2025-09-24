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
           
                string query = @"   INSERT INTO AdminUsers (
        ctenant_Id, cfirst_name, clast_name, cuser_name, cemail, cphoneno, 
        cpassword, crole_id, nis_active, llast_login_at, cpassword_changed_at, 
        clast_login_ip, clast_login_device, ccreated_date, ccreated_by, cmodified_by,
        lmodified_date) VALUES(
        @TenantID, @FirstName, @LastName, @Username, @Email, @PhoneNo, 
        @Password, @RoleID, @IsActive, @LastLoginAt, @PasswordChangedAt, 
        @LastLoginIP, @LastLoginDevice, @ccreated_date, @ccreated_by, @cmodified_by, @lmodified_date);
        SELECT SCOPE_IDENTITY();";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);

                    cmd.Parameters.AddWithValue("@TenantID", model.ctenant_Id);
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirst_name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clast_name ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", model.cuser_name);
                    cmd.Parameters.AddWithValue("@Email", model.cemail);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", model.cpassword); // store as plain text
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


        public async Task<bool> CheckUsernameExistsAsync(string username, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM AdminUsers WHERE cuser_name = @username AND ctenant_Id = @tenantId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
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
            SELECT [ID],  [ctenant_Id], [cfirst_name], [clast_name], [cuser_name],
                   [cemail], [cphoneno], [cpassword], [crole_id], [nis_active], [llast_login_at],
                   [lfailed_login_attempts], [cpassword_changed_at], [cmust_change_password],
                   [clast_login_ip], [clast_login_device],[nis_locked],[ccreated_date],[ccreated_by],[cmodified_by],
                   [lmodified_date],[nIs_deleted],[cdeleted_by],[ldeleted_date]
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
                              cfirstName = reader.GetString(reader.GetOrdinal("cfirst_name")),
                              clastName = reader.GetString(reader.GetOrdinal("clast_name")),
                              cusername = reader.GetString(reader.GetOrdinal("cuser_name")),
                              cemail = reader.GetString(reader.GetOrdinal("cemail")),
                              cphoneno = reader.IsDBNull(reader.GetOrdinal("cphoneno")) ? null : reader.GetString(reader.GetOrdinal("cphoneno")),
                              cpassword = reader.GetString(reader.GetOrdinal("cpassword")),
                              croleID = reader.GetInt32(reader.GetOrdinal("crole_id")),
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
                              ldeleted_date = reader.IsDBNull(reader.GetOrdinal("ldeleted_date")) ? null : reader.GetString(reader.GetOrdinal("ldeleted_date")),


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
            cfirst_name = @FirstName,
            clast_name = @LastName,
            cuser_name = @Username,
            cemail = @Email,
            cphoneno = @PhoneNo,
            cpassword = @Password,
            nis_active = @IsActive,
            cmodified_by=cmodified_by,
            lmodified_date=lmodified_date
        WHERE ID = @ID AND  ctenant_Id = @TenantID";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@FirstName", (object?)model.cfirstName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@LastName", (object?)model.clastName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Username", (object?)model.cusername ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Email", (object?)model.cemail ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@PhoneNo", (object?)model.cphoneno ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Password", (object?)model.cpassword ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@IsActive", model.nisActive ?? true);
                    cmd.Parameters.AddWithValue("@cmodified_by", (object?)model.cmodified_by ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@lmodified_date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@ID", model.cid);                 
                    cmd.Parameters.AddWithValue("@TenantID", model.cTenantID);

                    int rowsAffected = await cmd.ExecuteNonQueryAsync();
                    return rowsAffected > 0;
                }
            }
        }


        public async Task<bool> DeleteSuperAdminAsync(DeleteAdminDTO model, int cTenantID,string username)
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
    @cmodifiedby, @lmodifieddate, @nIsDeleted, @cDeletedBy, @lDeletedDate
)";

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
        WHERE cuserid = @cuserid AND ctenant_id = @ctenantID";

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
	  [ldeleted_date] FROM [dbo].[Users]
        WHERE crole_id = 3 AND ctenant_id = @TenantID";

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
                                lDeletedDate=reader.IsDBNull(reader.GetOrdinal("ldeleted_date")) ? null : reader.GetDateTime(reader.GetOrdinal("ldeleted_date"))
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



        public async Task<bool> CheckuserUsernameExistsAsync(string username, int tenantId)
        {
            using (SqlConnection conn = new SqlConnection(_config.GetConnectionString("Database")))
            {
                await conn.OpenAsync();
                string query = "SELECT COUNT(1) FROM Users WHERE cuser_name = @username AND ctenant_Id = @tenantId";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
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


        public async Task<bool> UpdatePasswordSuperAdminAsync(UpdateadminPassword model,int tenantId, string username)
        {
            var connStr = _config.GetConnectionString("Database");

            using (SqlConnection conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                model.cpassword = BCrypt.Net.BCrypt.HashPassword(model.cpassword);
                string query = @"
        UPDATE AdminUsers SET
            cpassword = @Password,
            cPasswordChangedAt =cPasswordChangedAt          
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


    }
}