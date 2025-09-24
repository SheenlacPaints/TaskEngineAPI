using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TaskEngineAPI.DTO
{
 
        public class AdminUserDTO
        {
            public int ID { get; set; }
            public int cuserid { get; set; }
            public int cTenantID { get; set; }
            public string cfirstName { get; set; }
            public string clastName { get; set; }
            public string cusername { get; set; }
            public string cemail { get; set; }
            public string cphoneno { get; set; }
            public string cpassword { get; set; }
            public int croleID { get; set; }
            public bool nisActive { get; set; }
            public DateTime? llastLoginAt { get; set; }
            public int? lfailedLoginAttempts { get; set; }
            public DateTime? cPasswordChangedAt { get; set; }
            public bool? cMustChangePassword { get; set; }
            public string cLastLoginIP { get; set; }
            public string cLastLoginDevice { get; set; }
            public bool? nis_locked { get; set; }
            public DateTime? ccreated_date { get; set; }
            public string? ccreated_by { get; set; }
            public string? cmodified_by { get; set; }
            public DateTime? lmodified_date { get; set; }
            public bool? nIs_deleted { get; set; }
            public string? cdeleted_by { get; set; }
            public string? ldeleted_date { get; set; }
    }

    public class UpdateAdminDTO
    {
        public int cid { get; set; }     
        public int cTenantID { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string? cusername { get; set; }
        public string? cemail { get; set; }
        public string? cphoneno { get; set; }
        public string? cpassword { get; set; }
        public bool? nisActive { get; set; }
        public string? cmodified_by { get; set; }
       
    }

    public class DeleteAdminDTO
    {
        public int cid { get; set; }
       
    }


    public class adminget
    {
        public int cTenantID { get; set; }
    }


    public class createCustomerMadel
    {
        public string? cphoneno { get; set; }

        public string? cusername { get; set; }
    }
  

    public class OtpActionRequest
    {
        public string otp { get; set; }
        public string action { get; set; }
        public pay payload { get; set; }

    }


  
    public class OtpActionRequest<T>
    {
        public string otp { get; set; }
        public string action { get; set; }
        public T payload { get; set; }
    }


    public class forgototp
    {
        public string? cphoneno { get; set; }
    }

    public class forgototpModel
    {
        public string? cphoneno { get; set; }

        public string? cusername { get; set; }

        public string? ctenantid { get; set; }

    }

}


