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


}


   
  

