namespace TaskEngineAPI.DTO
{
  
    public class User
    {
       
        public string? userName { get; set; }
        public string? password { get; set; }
    }


    public class EncryptedLoginRequest
    {
        public string EncryptedData { get; set; }
    }
}
