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

            public List<TaskDetailDTO> TaskDetailDTO { get; set; }
        }

        public class TaskDetailDTO
    {
        public string? cprocesscode { get; set; }
        public int? ciseqno { get; set; }
        public string? cseq_order { get; set; }
        public string cactivitycode { get; set; }
        public string cactivitydescription { get; set; }
        public string ctasktype { get; set; }
        public string cprevstep { get; set; }
        public string cactivityname { get; set; }
        public string cnextseqno { get; set; }
      

    }


}



