namespace TaskEngineAPI.Helpers
{
    public class APIResponse
    {
        public int status { get; set; }
        public string statusText { get; set; }
        public string error { get; set; }
        public object[] body { get; set; }
    }
}
