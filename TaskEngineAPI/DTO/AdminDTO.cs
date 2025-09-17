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


}
