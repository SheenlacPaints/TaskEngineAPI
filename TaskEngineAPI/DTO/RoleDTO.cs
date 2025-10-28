using System;

namespace TaskEngineAPI.DTO
{
    public class Role
    {
        public int CRole_Id { get; set; }
        public int CTenant_Id { get; set; }
        public string CRole_Code { get; set; }
        public string CRole_Name { get; set; }
        public bool NIs_Active { get; set; }
        public DateTime CCreated_Date { get; set; }
        public string CCreated_By { get; set; }
        public string CModified_By { get; set; }
        public DateTime? LModified_Date { get; set; }
    }
}
