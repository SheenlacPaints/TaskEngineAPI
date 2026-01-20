namespace TaskEngineAPI.Models
{
    
    public class SocketRequest
    {
        public int cTenantID { get; set; }
        public string username { get; set; }
        public string? type { get; set; }
        public string? searchText { get; set; }
    }


}
