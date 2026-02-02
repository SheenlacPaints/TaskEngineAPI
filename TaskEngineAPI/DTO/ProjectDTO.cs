namespace TaskEngineAPI.DTO
{
    public class CreateProjectDTO
    {
        public int AssignedManagerId { get; set; }
        public string ProjectName { get; set; }
        public string ProjectType { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTime? expecteddate { get; set; }
    }

    public class GetProjectList
    {
        public int ProjectId { get; set; }
        public int ClientTenantId { get; set; }
        public int RaisedByUserId { get; set; }
        public int AssignedManagerId { get; set; }
        public string? ProjectName { get; set; }
        public string? ProjectType { get; set; }
        public string? Description { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? Status { get; set; }
        public string? Attachments { get; set; }
        public DateTime? expecteddate { get; set; }
        public List<ProjectDetailResponse> project_Details { get; set; } = new List<ProjectDetailResponse>();


    }


    public class ProjectDetailResponse
    {
        public int header_id { get; set; }
        public int Detail_id { get; set; }
        public string module { get; set; }
        public string projectDescription { get; set; }
        public string Resources { get; set; }
        public int No_of_Resources { get; set; }
        public int Slavalue { get; set; }
        public string Slaunit { get; set; }
        public string version { get; set; }
        public string Remarks { get; set; }
        public string Resource_Names { get; set; }
        public string status1 { get; set; }
    }

    public class TaskProjectResponse
    {
        //public bool Success { get; set; }
        public int TotalCount { get; set; }
        public List<GetProjectList> Data { get; set; }
    }

    public class ProjectDetailRequest
    {

        public int header_id { get; set; }
        public int Detail_id { get; set; }
        public string module { get; set; }
        public string projectDescription { get; set; }
        public string Resources { get; set; }
        public int No_of_Resources { get; set; }
        public int Slavalue { get; set; }
        public string Slaunit { get; set; } 
        public string Version { get; set; } 
        public string Remarks { get; set; } 
    }
}
