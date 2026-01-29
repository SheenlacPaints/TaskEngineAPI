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
       
    }

    public class TaskProjectResponse
    {
        //public bool Success { get; set; }
        public int TotalCount { get; set; }
        public List<GetProjectList> Data { get; set; }
    }

    public class ProjectDetailRequest
    {
        public int HeaderId { get; set; }
        public int DetailId { get; set; }
        public string Module { get; set; }
        public int EmployeeId { get; set; }
        public int NoOfEmp { get; set; }
        public string Remarks { get; set; }
    }

}
