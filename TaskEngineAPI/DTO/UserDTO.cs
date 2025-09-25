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
    }


    public class DeleteuserDTO
    {
        public int id { get; set; }

    }


}
