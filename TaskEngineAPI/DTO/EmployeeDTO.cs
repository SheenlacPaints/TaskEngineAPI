namespace TaskEngineAPI.DTO
{
    public class EmployeeDTO
    {
        public string id { get; set; }
        public string cuserid { get; set; }
        public string ctenantID { get; set; }
        public string cusername { get; set; }
        public string cemail { get; set; }
        public string cpassword { get; set; }
        public bool nIsActive { get; set; }

        public string cfirstName { get; set; }
        public string clastName { get; set; }
        public string cphoneno { get; set; }
        public string cAlternatePhone { get; set; }
        public string ldob { get; set; }
        public string cMaritalStatus { get; set; }
        public string cnation { get; set; }
        public string cgender { get; set; }

        public string caddress { get; set; }
        public string caddress1 { get; set; }
        public string caddress2 { get; set; }
        public string cpincode { get; set; }
        public string ccity { get; set; }
        public string cstatecode { get; set; }
        public string cstatedesc { get; set; }
        public string ccountrycode { get; set; }
        public string ProfileImage { get; set; }

        public string cbankName { get; set; }
        public string caccountNumber { get; set; }
        public string ciFSCCode { get; set; }
        public string cpAN { get; set; }

        public string ldoj { get; set; }
        public string cemploymentStatus { get; set; }
        public int nnoticePeriodDays { get; set; }
        public string lresignationDate { get; set; }
        public string llastWorkingDate { get; set; }
        public string cempcategory { get; set; }
        public string cworkloccode { get; set; }
        public string cworklocname { get; set; }

        public string croleID { get; set; }
        public string crolecode { get; set; }
        public string crolename { get; set; }
        public string cgradecode { get; set; }
        public string cgradedesc { get; set; }
        public string csubrolecode { get; set; }

        public string cdeptcode { get; set; }
        public string cdeptdesc { get; set; }

        public string cjobcode { get; set; }
        public string cjobdesc { get; set; }


        public string creportmgrcode { get; set; }
        public string creportmgrname { get; set; }

        public string cRoll_id { get; set; }
        public string cRoll_name { get; set; }
        public string cRoll_Id_mngr { get; set; }
        public string cRoll_Id_mngr_desc { get; set; }

        public string cReportManager_empcode { get; set; }
        public string cReportManager_Poscode { get; set; }
        public string cReportManager_Posdesc { get; set; }

        public bool nIsWebAccessEnabled { get; set; }
        public bool nIsEventRead { get; set; }
        public string lLastLoginAt { get; set; }
        public int nFailedLoginAttempts { get; set; }
        public string cPasswordChangedAt { get; set; }
        public bool nIsLocked { get; set; }
        public string LastLoginIP { get; set; }
        public string LastLoginDevice { get; set; }

        public string ccreateddate { get; set; }
        public string ccreatedby { get; set; }
        public string cmodifiedby { get; set; }
        public string lmodifieddate { get; set; }

        public bool nIsDeleted { get; set; }
        public string cDeletedBy { get; set; }
        public string lDeletedDate { get; set; }
    }
}
