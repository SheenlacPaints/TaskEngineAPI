namespace TaskEngineAPI.DTO
{
    public class CreateProjectDTO
    {
        public int AssignedManagerId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
       
    }
}
