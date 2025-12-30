using System.ComponentModel.DataAnnotations;
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


    public class InputDTO
    {
        [Required]
        public string payload { get; set; }  // Encrypted JSON

        public IFormFile? attachment { get; set; } // Single uploaded file
    }


    public class pays 
    { public PayloadWrapper payload { get; set; } 
    }
    public class PayloadWrapper 
    { 
        public string payload { get; set; } 
        public IFormFile? attachment { get; set; }
    }


    public class FileUploadDTO
    {
        [Required]
        public string type { get; set; }   
        [Required]
        public string id { get; set; }        

        [Required]
        public IFormFile file { get; set; } 
    }


    public class FileUploadRequest
    {
        public IFormFile File { get; set; }
    }


}
