namespace TaskEngineAPI.DTO
{
    public class ExternalEmployeeDTO
    {
        public string cuserid { get; set; }
        public string cusername { get; set; }
        public string cemail { get; set; }
        public bool? nIsActive { get; set; }
        public string cfirstName { get; set; }
        public string clastName { get; set; }
        public string cphoneno { get; set; }
        public string calternate_phone { get; set; }

        public string ldob { get; set; }
        public string cmarital_status { get; set; }
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

        public string cbank_name { get; set; }
        public string caccount_number { get; set; }
        public string ciFSC_code { get; set; }
        public string cpan { get; set; }

        public string ldoj { get; set; }
        public string cemployment_status { get; set; }
        public int? nnotice_period_days { get; set; }
        public string lresignation_date { get; set; }
        public string llast_working_date { get; set; }
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

        public bool? nIsWebAccessEnabled { get; set; }
        public bool? nIsEventRead { get; set; }
        public string llast_login_at { get; set; }
        public int? nfailed_logina_attempts { get; set; }
        public string cpassword_changed_at { get; set; }
        public bool? nIsLocked { get; set; }

        public string cposition_code { get; set; }
        public string cposition_name { get; set; }
    }
}
