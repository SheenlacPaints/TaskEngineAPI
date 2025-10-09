namespace TaskEngineAPI.DTO
{
    
    public class ProcessEngineTypeDTO
    {
        public string? ctype { get; set; } 
    }
    public class ProcessEngineDTO
    {
        public string? ctype { get; set; }
        public string? ciseqno { get; set; }
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cuser_id { get; set; }
        public string? cuser_name { get; set; }
        public string? crole_code { get; set; }
        public string? crole_name { get; set; }
        public string? cposition_code { get; set; }
        public string? cposition_title { get; set; }
        public string? cdepartment_code { get; set; }
        public string? cdepartment_name { get; set; }     
        public string cstatus { get; set; }
        public List<ProcessEngineChildItems> ProcessEngineChildItems { get; set; }

        public List<ProcessEngineMeta> ProcessEngineMeta { get; set; }
    }
    public class ProcessEngineChildItems
    {
        public string? cprocesscode { get; set; }
        public int? ciseqno { get; set; }
        public string? cseq_order { get; set; }
        public string? cactivitycode { get; set; }
        public string? cactivitydescription { get; set; }
        public string? ctasktype { get; set; }
        public string? cprevstep { get; set; }
        public string? cactivityname { get; set; }
        public string? cnextseqno { get; set; }
        public string? cassignee { get; set; }
        public string? cprocess_type { get; set; }

        public List<ProcessEngineConditionDetails> ProcessEngineConditionDetails { get; set; }


  
    }

     public class ProcessEngineMeta
    {
        public string? cinput_type { get; set; }
        public string? label { get; set; }
        public string? cplaceholder { get; set; }
        public bool? cis_required { get; set; }
        public bool? cis_autofill { get; set; }
        public bool? cis_editable { get; set; }
        public bool? cis_validate { get; set; }
        public int? cmin_len { get; set; }
        public int? cmax_len { get; set; }
        public string? cdata_source_type { get; set; }
        public string? cfetch_type { get; set; }
        public bool? cis_req_search { get; set; }
        public bool? cis_multi_select { get; set; }
        public DateTime? cmin_date { get; set; }
        public DateTime? cmax_date { get; set; }
        public string? cdate_type { get; set; }
        public int? cmin_time { get; set; }
        public int? cmax_time { get; set; }
        public string? ctime_type { get; set; }
        public bool? cprocess_source { get; set; }
        public string? clocation { get; set; }
        public string? ccolumn_value { get; set; }

    }

    public class ProcessEngineConditionDetails
    {
        public string? cprocesscode { get; set; }
        public int? ciseqno { get; set; }
        public int? icondseqno { get; set; }
        public int? cseq_order { get; set; }
        public string ctype { get; set; }
        public string clabel { get; set; }
        public string cfieldvalue { get; set; }
        public string ccondition { get; set; }
        public string remarks1 { get; set; }
        public string remarks2 { get; set; }
        public string remarks3 { get; set; }

    }



    public class GetProcessEngineDTO
    {

        public int? cseq_id { get; set; }
        public string? ctype { get; set; }
        public string? ciseqno { get; set; }
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cuser_id { get; set; }
        public string? cuser_name { get; set; }
        public string? crole_code { get; set; }
        public string? crole_name { get; set; }
        public string? cposition_code { get; set; }
        public string? cposition_title { get; set; }
        public string? cdepartment_code { get; set; }
        public string? cdepartment_name { get; set; }
        public string? cstatus { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime? ccreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }
        public List<ProcessEngineChildItems> ProcessEngineChildItems { get; set; }
    }






}

     