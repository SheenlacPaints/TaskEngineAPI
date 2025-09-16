using Newtonsoft.Json;

namespace TaskEngineAPI.DTO
{
  
    public class User
    {
       
        public string? userName { get; set; }
        public string? password { get; set; }
    }




    public class pay
    {
        [JsonProperty("payload")]
        public string payload { get; set; }
    }

    public class CreateAdminDTO
    {
      
        public int cTenantID { get; set; }
        public string? cfirstName { get; set; }
        public string? clastName { get; set; }
        public string cusername { get; set; }
        public string cemail { get; set; }
        public string? cphoneno { get; set; }
        public string cpassword { get; set; }
        public int? croleID { get; set; }
        public bool? nisActive { get; set; }
        public DateTime? llastLoginAt { get; set; }
        public DateTime? cPasswordChangedAt { get; set; }
        public string cLastLoginIP { get; set; }
        public string cLastLoginDevice { get; set; }
       
    }



}



