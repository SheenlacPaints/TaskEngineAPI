namespace TaskEngineAPI.DTO
{
   
    public class ApiTestRequestDTO
    {
        public string? Method { get; set; }
        public string? Url { get; set; }
        public string? Body { get; set; }
        public Dictionary<string, string>? Headers { get; set; }
    }

}
