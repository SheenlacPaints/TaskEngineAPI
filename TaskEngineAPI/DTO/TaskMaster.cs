using System.Net.NetworkInformation;
using System;
using System.ComponentModel.DataAnnotations;
using TaskEngineAPI.DTO;

namespace TaskEngineAPI.DTO
{
        public class TaskMasterDTO
        {
        
           [StringLength(100)]
           public string? ctask_type { get; set; }
            [StringLength(255)]
           public string? ctask_name { get; set; }
           public string? ctask_description { get; set; }              
           public int?  cprocess_id { get; set; }
           public List<metaData> metaData { get; set; }
        }
       public class metaData
       {
         public int? cmeta_id { get; set; }
         public string? cdata { get; set; }
   
       }
        public class TaskDetailDTO
       {
        public int? iseqno { get; set; }
        public string? ctask_type { get; set; }
        public string? cmapping_code { get; set; }
        public string? ccurrent_status { get; set; }
        public string? cremarks { get; set; }
        public int? inext_seqno { get; set; }
        public string? cnext_seqtype { get; set; }
        public string? cprevtype { get; set; }
        public string? csla { get; set; }    
    }

        public class DeptPostRoleDTO
        {
        public string? table { get; set; }
        public string? action { get; set; }
        public string? userid { get; set; }
        public string? position { get; set; }
        public string? role { get; set; }
        public string? departmentname { get; set; }
        public string? departmentdesc { get; set; }  
        public string? cdepartmentmanagerrolecode { get; set; }
        public string? cdepartmentmanagername { get; set; }
        public string? cdepartmentemail { get; set; }
        public string? cdepartmentphone { get; set; }
        public bool? nisactive { get; set; }
        public string? user { get; set; }
        public string? cdepartmentcode { get; set; }
        public string? rolename { get; set; }
        public string? rolelevel { get; set; }
        public string? roledescription { get; set; }
        public string? positionname { get; set; }
        public string? positioncode { get; set; }
        public string? positiondescription { get; set; }
        public string? creportingmanagerpositionid { get; set; }
        public string? rolecode { get; set; }
        public int id { get; set; }
        public string? new_cdepartmentcode { get; set; }
        public string? new_rolecode { get; set; }
        public string? new_positioncode { get; set; }
    }

    public class privilegeMappingDTO
    {
        public int? privilege { get; set; }
        public int? cprocess_id { get; set; }
        public string? cprocess_code { get; set; }
        public string? cprocess_name { get; set; }
        public List<privilegeMapping> privilegeMapping { get; set; }
    }

    public class privilegeMapping
    {
        public int? entity_type { get; set; }
        public string? entity_id { get; set; }

    }

    
    

}





