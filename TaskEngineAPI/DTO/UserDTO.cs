using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace TaskEngineAPI.DTO
{

    public class CreateUserDTO
    {
        public int cuserid { get; set; }
        public int ctenantID { get; set; }
        public string cusername { get; set; }
        public string cemail { get; set; }
        public string cpassword { get; set; }
        public bool? nIsActive { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string? cphoneno { get; set; }
        public string? cAlternatePhone { get; set; }
        public DateTime? ldob { get; set; }
        public string? cMaritalStatus { get; set; }
        public string? cnation { get; set; }
        public string? cgender { get; set; }
        public string? caddress { get; set; }
        public string? caddress1 { get; set; }
        public string? caddress2 { get; set; }
        public string? cpincode { get; set; }
        public string? ccity { get; set; }
        public string? cstatecode { get; set; }
        public string? cstatedesc { get; set; }
        public string? ccountrycode { get; set; }
        public string? ProfileImage { get; set; }
        public string? cbankName { get; set; }
        public string? caccountNumber { get; set; }
        public string? ciFSCCode { get; set; }
        public string? cpAN { get; set; }
        public DateTime? ldoj { get; set; }
        public string? cemploymentStatus { get; set; }
        public int? nnoticePeriodDays { get; set; }
        public DateTime? lresignationDate { get; set; }
        public DateTime? llastWorkingDate { get; set; }
        public string? cempcategory { get; set; }
        public string? cworkloccode { get; set; }
        public string? cworklocname { get; set; }
        public int? croleID { get; set; }
        public string? crolecode { get; set; }
        public string? crolename { get; set; }
        public string? cgradecode { get; set; }
        public string? cgradedesc { get; set; }
        public string? csubrolecode { get; set; }
        public string? cdeptcode { get; set; }
        public string? cdeptdesc { get; set; }
        public string? cjobcode { get; set; }
        public string? cjobdesc { get; set; }
        public string? creportmgrcode { get; set; }
        public string? creportmgrname { get; set; }
        public string? cRoll_id { get; set; }
        public string? cRoll_name { get; set; }
        public string? cRoll_Id_mngr { get; set; }
        public string? cRoll_Id_mngr_desc { get; set; }
        public string? cReportManager_empcode { get; set; }
        public string? cReportManager_Poscode { get; set; }
        public string? cReportManager_Posdesc { get; set; }
        public bool? nIsWebAccessEnabled { get; set; }
        public bool? nIsEventRead { get; set; }
        public DateTime? lLastLoginAt { get; set; }
        public int? nFailedLoginAttempts { get; set; }
        public DateTime? cPasswordChangedAt { get; set; }
        public bool? nIsLocked { get; set; }
        public string? LastLoginIP { get; set; }
        public string? LastLoginDevice { get; set; }
        public DateTime? ccreateddate { get; set; }
        public string? ccreatedby { get; set; }
        public string? cmodifiedby { get; set; }
        public DateTime? lmodifieddate { get; set; }
        public bool? nIsDeleted { get; set; }
        public string? cDeletedBy { get; set; }
        public DateTime? lDeletedDate { get; set; }
        public string? cposition_code { get; set; }
        public string? cposition_name { get; set; }

        //public string? cprofile_image_name { get; set; }
        //public string? cprofile_image_path { get; set; }
    }



    public class BulkUserDTO
    {
        public int cuserid { get; set; }
        public string cusername { get; set; }
        public string cemail { get; set; }
        public string cpassword { get; set; }
        public bool? nIsActive { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string cphoneno { get; set; }
        public string? cAlternatePhone { get; set; }
        public string? ldob { get; set; }
        public string? cMaritalStatus { get; set; }
        public string? cnation { get; set; }
        public string? cgender { get; set; }
        public string? caddress { get; set; }
        public string? caddress1 { get; set; }
        public string? caddress2 { get; set; }
        public int? cpincode { get; set; }
        public string? ccity { get; set; }
        public string? cstatecode { get; set; }
        public string? cstatedesc { get; set; }
        public string? ccountrycode { get; set; }
        public string? cbankName { get; set; }
        public string? caccountNumber { get; set; }
        public string? ciFSCCode { get; set; } 
        public string? cpAN { get; set; }
        public string? ldoj { get; set; }
        public string? cemploymentStatus { get; set; }
        public string? cempcategory { get; set; }
        public int? croleID { get; set; }
        public string? crolecode { get; set; }
        public string? crolename { get; set; }
        public string? cdeptcode { get; set; }
        public string? cdeptdesc { get; set; }
        public string? creportmgrcode { get; set; }
        public string? creportmgrname { get; set; }
        public string? cRoll_id { get; set; }
        public string? cRoll_name { get; set; }
        public string? cposition_code { get; set; }
        public string? cposition_name { get; set; }
        public int? nnoticePeriodDays { get; set; }
        public string? cworkloccode { get; set; }
        public string? cworklocname { get; set; }
        public string? cgradecode { get; set; }
        public string? cgradedesc { get; set; }
        public string? csubrolecode { get; set; }
        public string? cjobcode { get; set; }
        public string? cjobdesc { get; set; }
        public string? croll_id_mngr { get; set; }
        public string? croll_id_mngr_desc { get; set; }
        public string? cReportManager_empcode { get; set; }
        public string? cReportManager_Poscode { get; set; }
        public string? cReportManager_Posdesc { get; set; }
    }


    public class UserApiDTO
    {
        [JsonProperty("ciFSCCode")]
        public string ciFSC_code { get; set; }

        [JsonProperty("cpAN")]
        public string cpAN { get; set; }
        public int cuserid { get; set; }
        public string cusername { get; set; }
        public string cemail { get; set; }
        public string cpassword { get; set; }

        public string? crolecode { get; set; }
        public string? crolename { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string? cphoneno { get; set; }
        public string? cAlternatePhone { get; set; }
        public DateTime? ldob { get; set; }
        public string? cMaritalStatus { get; set; }
        public string? cnation { get; set; }
        public string? cgender { get; set; }
        public string? caddress { get; set; }
        public string? caddress1 { get; set; }
        public string? caddress2 { get; set; }
        public string? cpincode { get; set; }
        public string? ccity { get; set; }
        public string? cstatecode { get; set; }
        public string? cstatedesc { get; set; }
        public string? ccountrycode { get; set; }
        public string? cbankName { get; set; }
        public string? caccountNumber { get; set; }
        //public string? ciFSC_code { get; set; }
        //public string? cpan { get; set; }
        public DateTime? ldoj { get; set; }
        public string? cemploymentStatus { get; set; }
        public int? nnoticePeriodDays { get; set; }
        public string? cempcategory { get; set; }
        public string? cworkloccode { get; set; }
        public string? cworklocname { get; set; }
        public string? cgradecode { get; set; }
        public string? cgradedesc { get; set; }
        public string? csubrolecode { get; set; }
        public string? cdeptcode { get; set; }

        public string? cdeptdesc { get; set; }
        public string? cjobcode { get; set; }
        public string? cjobdesc { get; set; }
        public string? creportmgrcode { get; set; }
        public string? creportmgrname { get; set; }
        public string? croll_id { get; set; }
        public string? croll_name { get; set; }
        public string? croll_id_mngr { get; set; }
        public string? croll_id_mngr_desc { get; set; }
        public string? cReportManager_empcode { get; set; }
        public string? cReportManager_Poscode { get; set; }
        public string? cReportManager_Posdesc { get; set; }
    }


    public class GetUserDTO
    {

        public int? id { get; set; }
        public int? cuserid { get; set; }
        public int? ctenantID { get; set; }
        public string? cusername { get; set; }
        public string? cemail { get; set; }
        public string cpassword { get; set; }
        public bool? nIsActive { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string? cphoneno { get; set; }
        public string? cAlternatePhone { get; set; }
        public DateTime? ldob { get; set; }
        public string? cMaritalStatus { get; set; }
        public string? cnation { get; set; }
        public string? cgender { get; set; }
        public string? caddress { get; set; }
        public string? caddress1 { get; set; }
        public string? caddress2 { get; set; }
        public int? cpincode { get; set; }
        public string? ccity { get; set; }
        public string? cstatecode { get; set; }
        public string? cstatedesc { get; set; }
        public string? ccountrycode { get; set; }
        public string? ProfileImage { get; set; }
        public string? cbankName { get; set; }
        public string? caccountNumber { get; set; }
        public string? ciFSCCode { get; set; }
        public string? cpAN { get; set; }
        public DateTime? ldoj { get; set; }
        public string? cemploymentStatus { get; set; }
        public int? nnoticePeriodDays { get; set; }
        public DateTime? lresignationDate { get; set; }
        public DateTime? llastWorkingDate { get; set; }
        public string? cempcategory { get; set; }
        public string? cworkloccode { get; set; }
        public string? cworklocname { get; set; }
        public int? croleID { get; set; }
        public string? crolecode { get; set; }
        public string? crolename { get; set; }
        public string? cgradecode { get; set; }
        public string? cgradedesc { get; set; }
        public string? csubrolecode { get; set; }
        public string? cdeptcode { get; set; }
        public string? cdeptdesc { get; set; }
        public string? cjobcode { get; set; }
        public string? cjobdesc { get; set; }
        public string? creportmgrcode { get; set; }
        public string? creportmgrname { get; set; }
        public string? cRoll_id { get; set; }
        public string? cRoll_name { get; set; }
        public string? cRoll_Id_mngr { get; set; }
        public string? cRoll_Id_mngr_desc { get; set; }
        public string? cReportManager_empcode { get; set; }
        public string? cReportManager_Poscode { get; set; }
        public string? cReportManager_Posdesc { get; set; }
        public bool? nIsWebAccessEnabled { get; set; }
        public bool? nIsEventRead { get; set; }
        public DateTime? lLastLoginAt { get; set; }
        public int? nFailedLoginAttempts { get; set; }
        public DateTime? cPasswordChangedAt { get; set; }
        public bool? nIsLocked { get; set; }
        public string? LastLoginIP { get; set; }
        public string? LastLoginDevice { get; set; }
        public DateTime? ccreateddate { get; set; }
        public string? ccreatedby { get; set; }
        public string? cmodifiedby { get; set; }
        public DateTime? lmodifieddate { get; set; }
        public bool? nIsDeleted { get; set; }
        public string? cDeletedBy { get; set; }
        public DateTime? lDeletedDate { get; set; }
        public string? cprofile_image_name { get; set; }
        public string? cprofile_image_path { get; set; }
        public string? cposition_code { get; set; }
        public string? cposition_name { get; set; }

    }
    public class UpdateUserDTO
    {

        [Required]
        public int id { get; set; }

        [Required]
        public int cuserid { get; set; }

        [Required]
        public int ctenantID { get; set; }
        public string? cusername { get; set; }
        public string? cemail { get; set; }

        public string? cpassword { get; set; }
        public bool? nIsActive { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string? cphoneno { get; set; }
        public string? cAlternatePhone { get; set; }
        public DateTime? ldob { get; set; }
        public string? cMaritalStatus { get; set; }
        public string? cnation { get; set; }
        public string? cgender { get; set; }
        public string? caddress { get; set; }
        public string? caddress1 { get; set; }
        public string? caddress2 { get; set; }
        public string? cpincode { get; set; }
        public string? ccity { get; set; }
        public string? cstatecode { get; set; }
        public string? cstatedesc { get; set; }
        public string? ccountrycode { get; set; }
        public string? ProfileImage { get; set; }
        public string? cbankName { get; set; }
        public string? caccountNumber { get; set; }
        public string? ciFSCCode { get; set; }
        public string? cpAN { get; set; }
        public DateTime? ldoj { get; set; }
        public string? cemploymentStatus { get; set; }
        public int? nnoticePeriodDays { get; set; }
        public DateTime? lresignationDate { get; set; }
        public DateTime? llastWorkingDate { get; set; }
        public string? cempcategory { get; set; }
        public string? cworkloccode { get; set; }
        public string? cworklocname { get; set; }
        public int? croleID { get; set; }
        public string? crolecode { get; set; }
        public string? crolename { get; set; }
        public string? cgradecode { get; set; }
        public string? cgradedesc { get; set; }
        public string? csubrolecode { get; set; }
        public string? cdeptcode { get; set; }
        public string? cdeptdesc { get; set; }
        public string? cjobcode { get; set; }
        public string? cjobdesc { get; set; }
        public string? creportmgrcode { get; set; }
        public string? creportmgrname { get; set; }
        public string? cRoll_id { get; set; }
        public string? cRoll_name { get; set; }
        public string? cRoll_Id_mngr { get; set; }
        public string? cRoll_Id_mngr_desc { get; set; }
        public string? cReportManager_empcode { get; set; }
        public string? cReportManager_Poscode { get; set; }
        public string? cReportManager_Posdesc { get; set; }
        public bool? nIsWebAccessEnabled { get; set; }
        public bool? nIsEventRead { get; set; }
        public DateTime? lLastLoginAt { get; set; }
        public int? nFailedLoginAttempts { get; set; }
        public DateTime? cPasswordChangedAt { get; set; }
        public bool? nIsLocked { get; set; }
        public string? LastLoginIP { get; set; }
        public string? LastLoginDevice { get; set; }
        public string? cmodifiedby { get; set; }
        public DateTime? lmodifieddate { get; set; }
        public bool? nIsDeleted { get; set; }
        public string? cDeletedBy { get; set; }
        public DateTime? lDeletedDate { get; set; }
        public string? cposition_code { get; set; }
        public string? cposition_name { get; set; }
    }
    public class DeleteuserDTO
    {
        public int id { get; set; }
    }




    public class GetusersapisyncDTO
    {
        public int ID { get; set; }
        public int ctenant_id { get; set; }
        public string? capi_method { get; set; }
        public string? capi_type { get; set; }
        public string? capi_url { get; set; }
        public string? capi_params { get; set; }
        public string? capi_headers { get; set; }
        public string? capi_config { get; set; }
        public string? capi_settings { get; set; }
        public string? capi_response { get; set; }
        public string? sync_type { get; set; }
        public string? cbody { get; set; }
        public string? cname { get; set; }
        public bool? nis_active { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime? lcreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }
    }

    public class usersapisyncDTO
    {
        public string? capi_method { get; set; }
        public string? capi_type { get; set; }
        public string? capi_url { get; set; }
        public string? capi_params { get; set; }
        public string? capi_headers { get; set; }
        public string? capi_config { get; set; }
        public string? capi_settings { get; set; }
        public string? cbody { get; set; }
        public string? cname { get; set; }
        public string? capi_response { get; set; }
        public bool? nis_active { get; set; }


    }

    public class GetAPISyncConfigByIDDTO
    {
        public int ID { get; set; }
        public int ctenant_id { get; set; }
        public string? capi_method { get; set; }
        public string? capi_type { get; set; }
        public string? capi_url { get; set; }
        public string? capi_params { get; set; }
        public string? capi_headers { get; set; }
        public string? capi_config { get; set; }
        public string? capi_settings { get; set; }
        public string? capi_response { get; set; }
        public string? cbody { get; set; }
        public string? cname { get; set; }
        public bool? nis_active { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime? lcreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }
    }

    public class UpdateAPISyncConfigDTO
    {
        public int ID { get; set; }
        public string? capi_method { get; set; }
        public string? capi_type { get; set; }
        public string? capi_url { get; set; }
        public string? capi_params { get; set; }
        public string? capi_headers { get; set; }
        public string? capi_config { get; set; }
        public string? capi_settings { get; set; }
        public string? capi_response { get; set; }
        public string? cbody { get; set; }
        public bool? nis_active { get; set; }
    }

    public class UpdateAPISyncConfigActiveStatusAsyncDTO
    {
        public int ID { get; set; }
        public bool nis_active { get; set; }
    }
    public class DeleteAPISyncConfigDTO
    {
        public int ID { get; set; }
    }
    public class BulkRoleDTO
    {
        public string crole_code { get; set; }
        public string crole_name { get; set; }
        public string crole_level { get; set; }
        public string? cdepartment_code { get; set; }
        public string? creporting_manager_code { get; set; }
        public string? creporting_manager_name { get; set; }
        public string? crole_description { get; set; }
        public bool? nis_active { get; set; }
    }


    public class RoleDTO
    {
        public string crole_code { get; set; }
        public string? crole_name { get; set; }
        public string crole_level { get; set; }
        public string? cdepartment_code { get; set; }
        public string? creporting_manager_code { get; set; }
        public string? creporting_manager_name { get; set; }
        public string? crole_description { get; set; }
        public bool? nis_active { get; set; }
    }


    public class BulkDepartmentDTO
    {
        public string cdepartment_code { get; set; }
        public string? cdepartment_name { get; set; }
        public string? cdepartment_desc { get; set; }
        public string? cdepartment_manager_rolecode { get; set; }
        public string? cdepartment_manager_position_code { get; set; }
        public string? cdepartment_manager_name { get; set; }
        public string? cdepartment_email { get; set; }
        public string? cdepartment_phone { get; set; }
        public bool? nis_active { get; set; }
    }

    public class DepartmentDTO
    {
        public string cdepartment_code { get; set; }
        public string? cdepartment_name { get; set; }
        public string? cdepartment_desc { get; set; }
        public string? cdepartment_manager_rolecode { get; set; }
        public string? cdepartment_manager_position_code { get; set; }
        public string? cdepartment_manager_name { get; set; }
        public string? cdepartment_email { get; set; }
        public bool? nis_active { get; set; }
        public string? cdepartment_phone { get; set; }
    }

    public class PositionDTO
    {
        public string cposition_code { get; set; }
        public string? cposition_name { get; set; }
        public string? cposition_decsription { get; set; }
        public string? cdepartment_code { get; set; }
        public string? creporting_manager_positionid { get; set; }
        public string? creporting_manager_name { get; set; }
        public bool? nis_active { get; set; }
    }



    public class BulkPositionDTO
    {
        public string cposition_code { get; set; }
        public string? cposition_name { get; set; }
        public string? cposition_decsription { get; set; } 
        public string? cdepartment_code { get; set; }
        public string? creporting_manager_positionid { get; set; } 
        public string? creporting_manager_name { get; set; }
        public bool? nis_active { get; set; }
    }

    public class SapEmployeeResponse
    {
        public string EMPLOYEE_ID { get; set; }
        public string EMPLOYEE_NAME { get; set; }
        public string DATE_OF_JOINING { get; set; }
        public string DEPARTMENT { get; set; }
        public string POSITION { get; set; }
        public string REPORTING_MANAGER_CODE { get; set; }
        public string REPORTING_MANAGER { get; set; }
        public string EMPLOYEE_STATUS { get; set; }
        public string EMAIL_ADDRESS { get; set; }
        public string PHONE_NUMBER { get; set; }
    }

    public class SapSyncEmployeeResponse
    {
        public string EMPLOYEE_ID { get; set; }
        public string EMPLOYEE_NAME { get; set; }
        public string EMPLOYEE_FIRST_NAME { get; set; }
        public string EMPLOYEE_LAST_NAME { get; set; }
        public string GENDER { get; set; }
        public string DATE_OF_BIRTH { get; set; }
        public string MARITAL_STATUS { get; set; }
        public string DATE_OF_JOINING { get; set; }
        public string ROLE { get; set; }
        public string DEPARTMENT_CODE { get; set; }
        public string DEPARTMENT { get; set; }
        public string POSITION { get; set; }
        public string EMPLOYEE_POSITION_CODE { get; set; }
        public string EMPLOYEE_STATUS_CODE { get; set; }
        public string EMPLOYEE_STATUS { get; set; }
        public string EMPLOYEE_CATEGORY { get; set; }
        public string REPORTING_MANAGER_CODE { get; set; }
        public string REPORTING_MANAGER { get; set; }
        public string MANAGER_POS_ID { get; set; }
        public string MANAGER_POS_DES { get; set; }
        public string BANK_NAME { get; set; }
        public string ACCOUNT_NUMBER { get; set; }
        public string IFSC_CODE { get; set; }
        public string PAN { get; set; }
        public string ALTERNATIVE_NUMBER { get; set; }
        public string ADDRESS_LINE1 { get; set; }
        public string ADDRESS_LINE2 { get; set; }
        public string ADDRESS { get; set; }
        public string CITY { get; set; }
        public string STATE_CODE { get; set; }
        public string STATE { get; set; }
        public string PIN_CODE { get; set; }
        public string NATIONALITY_CODE { get; set; }
        public string NATIONALITY { get; set; }
        public string PHONE_NUMBER { get; set; }
        public string NOTICE_PERIOD { get; set; }
        public string EMAIL_ADDRESS { get; set; }
        public string COMPANY_CODE { get; set; }
    }

    public class InBoundSyncRequestDTO
    {
        public bool SyncOrgUnit { get; set; } = true;
        public bool SyncJobCode { get; set; } = true;
        public bool SyncPositionDetails { get; set; } = true;
        public bool SyncAttendanceTimesheet { get; set; } = true;
        public string TriggeredBy { get; set; } = "Manual";
    }

    public class InBoundSyncResponseDTO
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
        public InBoundSyncSummaryDTO Summary { get; set; }
        public List<string> Errors { get; set; }
    }

    public class InBoundSyncSummaryDTO
    {
        public bool OrgUnitHadData { get; set; }
        public int OrgUnitRecordsDeleted { get; set; }
        public int OrgUnitRecordsInserted { get; set; }
        public bool JobCodeHadData { get; set; }
        public int JobCodeRecordsDeleted { get; set; }
        public int JobCodeRecordsInserted { get; set; }
        public bool PositionHadData { get; set; }
        public int PositionRecordsDeleted { get; set; }
        public int PositionRecordsInserted { get; set; }
        public bool AttendanceTimesheetHadData { get; set; }
        public int AttendanceTimesheetRecordsDeleted { get; set; }
        public int AttendanceTimesheetRecordsInserted { get; set; }
        public int TotalRecordsAffected { get; set; }
        public TimeSpan Duration { get; set; }
        public List<string> SkippedTables { get; set; }
    }
}
