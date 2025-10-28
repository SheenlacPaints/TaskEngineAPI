namespace TaskEngineAPI.DTO
{
    
    public class ProcessEngineTypeDTO
    {
        public int? ID { get; set; }
        public string? privilege { get; set; }
       
    }
    public class ProcessEngineDTO
    {
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? ctype { get; set; }
        public string? cvalue { get; set; }
        public string? cvaluebyid { get; set; }
        public string? ciseqno { get; set; }
        public string? cstatus { get; set; }
        public string? cpriority_label { get; set; }
        public bool? nshow_timeline { get; set; }
        public int? cnotification_type { get; set; }
        public string? cmetatype { get; set; }
        public int? cmeta_id { get; set; }

        public List<ProcessEngineChildItems> ProcessEngineChildItems { get; set; }
        public List<ProcessEngineMetaMaster> ProcessEngineMetaMaster { get; set; }
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
        public int? csla_day { get; set; }
        public int? csla_Hour { get; set; }
        public bool? nboard_enabled { get; set; }
        public string? caction_privilege { get; set; }
        public string? crejection_privilege { get; set; }

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
        public string? ctype { get; set; }
        public string? clabel { get; set; }
        public string? cplaceholder { get; set; }
        public bool? cis_required { get; set; }
        public bool? cis_readonly { get; set; }
        public bool? cis_disabled { get; set; }
        public string? cdefault_value { get; set; }
        public string? cmin { get; set; }
        public string? cmax { get; set; }
        public string? cpattern { get; set; }
        public bool? nallow_spaces { get; set; }
        public bool? nallow_numbers { get; set; }
        public bool? nallow_special_chars { get; set; }
        public bool? ntrim { get; set; }
        public bool? nauto_focus { get; set; }
        public bool? ncapitalize { get; set; }
        public bool? nto_upper_case { get; set; }
        public bool? nto_lower_case { get; set; }
        public bool? nshow_copy_button { get; set; }
        public string? cdepends_on { get; set; }
        public string? cdisabled_when { get; set; }
        public string? crequired_when { get; set; }
        public string? cvisible_when { get; set; }
        public string? cfieldvalue { get; set; }
        public string? ccondition { get; set; }
        public string? remarks1 { get; set; }
        public string? remarks2 { get; set; }
        public string? remarks3 { get; set; }



    }
    public class GetProcessEngineDTO
    {
        public int? ID { get; set; }
        public string? ctype { get; set; }
        public int? ciseqno { get; set; }
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cvalue { get; set; }
        public string? cvaluebyid { get; set; }
        public string? cpriority_label { get; set; }
        public bool? nshow_timeline { get; set; }
        public int? cnotification_type { get; set; }
        public string? cstatus { get; set; }
        public int? cmeta_id { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime? ccreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }

        public List<ProcessEngineChildItems> ProcessEngineChildItems { get; set; }
    }
    public class ProcessEngineMetaMaster
    {
        public string? meta_Name { get; set; }
        public string? meta_Description { get; set; }
        public string?label { get; set; }
    }

    public class TaskList
    {
        public int ID { get; set; }
        public int itaskno { get; set; }
        public string? ctasktype { get; set; }
        public string? ctaskname { get; set; }
        public string? ctaskdescription { get; set; }
        public string? cstatus { get; set; }  
        public DateTime? lcompleteddate { get; set; }
        public string? ccreatedby { get; set; }
        public DateTime? lcreateddate { get; set; }
        public string? cmodifiedby { get; set; }
        public DateTime? lmodifieddate { get; set; }
        public string? Employeecode { get; set; }
        public string? EmpDepartment { get; set; }
        public List<TaskDetails>? TaskChildItems { get; set; }
    }

    public class TaskDetails
    {
        public int? ID { get; set; }
        public int? iheader_id { get; set; }
        public int? itaskno { get; set; }
        public int? iseqno { get; set; }
        public string? ctasktype { get; set; }
        public string? cmappingcode { get; set; }
        public string? ccurrentstatus { get; set; }
        public DateTime? lcurrentstatusdate { get; set; }
        public string? cremarks { get; set; }
        public int? inextseqno { get; set; }
        public string? cnextseqtype { get; set; }
        public string? cprevtype { get; set; }
       
        public string? SLA { get; set; }
        public string? cisforwarded { get; set; }
        public DateTime? lfwddate { get; set; }
        public string? cfwdto { get; set; }
        public string? cisreassigned { get; set; }
        public DateTime? lreassigndt { get; set; }
        public string? creassignto { get; set; }
       
    }

}

