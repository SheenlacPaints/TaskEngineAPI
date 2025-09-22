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
      
        public int ctenant_Id { get; set; }
        public string? cfirst_name { get; set; }
        public string? clast_name { get; set; }
        public string cuser_name { get; set; }
        public string cemail { get; set; }
        public string? cphoneno { get; set; }
        public string cpassword { get; set; }
        public int? crole_id { get; set; }
        public bool? nis_active { get; set; }
        public DateTime? llast_login_at { get; set; }
        public DateTime? cpassword_changed_at { get; set; }
        public string? clast_login_ip { get; set; }
        public string? clast_login_device { get; set; }
        public string? ccreated_by { get; set; }
        public string? cmodified_by { get; set; }

    }

}
