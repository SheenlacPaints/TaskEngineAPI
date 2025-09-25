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

}
