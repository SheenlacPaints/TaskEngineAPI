namespace TaskEngineAPI.DTO
{
    
    public class ProcessEngineTypeDTO
    {
        public int? ID { get; set; }
        public string? privilege { get; set; }
       
    }
    public class PrivilageDTO
    {
        public int? value { get; set; }
        public string? view_value { get; set; }

    }

    public class ProcessEngineDTO
    {
        public string? cprocessCode { get; set; }
        public string? cprocessName { get; set; }

        public int? cprivilegeType  { get; set; }
        public string? cvalue { get; set; }       
        public string? cstatus { get; set; }
        public string? cpriorityLabel { get; set; }
        public bool? nshowTimeline { get; set; }
        public int? cnotificationType { get; set; }
        public string? cmetaType { get; set; }
        public int? cmetaId { get; set; }
        public string? cmetaName { get; set; }
        public List<processEngineChildItems> processEngineChildItems { get; set; }
        public List<processEngineMeta> processEngineMeta { get; set; }   

        
    }
    public class processEngineChildItems
    {      
        public string? cprocessCode { get; set; }
        public int? ciseqno { get; set; }     
        public string? cactivityCode { get; set; }
        public string? cactivityDescription { get; set; }
        public string? ctaskType { get; set; }
        public string? cprevStep { get; set; }
        public string? cactivityName { get; set; }
        public string? cnextSeqno { get; set; }
        public string? cmappingCode { get; set; }
        public string? cmappingType { get; set; }
        public string? cparticipantType { get; set; }
        public int? cslaDay { get; set; }
        public int? cslaHour { get; set; }
        public bool? nboardEnabled { get; set; }
        public string? cactionPrivilege { get; set; }
        public string? crejectionPrivilege { get; set; }        
        public List<processEngineConditionDetails> processEngineConditionDetails { get; set; }
    }
    public class processEngineMeta
    {
        public string? cinputType { get; set; }
        public string? label { get; set; }
        public string? cplaceholder { get; set; }
        public bool? cisRequired { get; set; }
        public bool? cisAutofill { get; set; }
        public bool? cisEditable { get; set; }
        public bool? cisValidate { get; set; }
        public int? cminLen { get; set; }
        public int? cmaxLen { get; set; }
        public string? cdataSourceType { get; set; }
        public string? cfetchType { get; set; }
        public bool? cisReqSearch { get; set; }
        public bool? cisMultiSelect { get; set; }
        public DateTime? cminDate { get; set; }
        public DateTime? cmaxDate { get; set; }
        public string? cdateType { get; set; }
        public int? cminTime { get; set; }
        public int? cmaxTime { get; set; }
        public string? ctimeType { get; set; }
        public bool? cprocessSource { get; set; }
        public string? clocation { get; set; }
        public string? ccolumnValue { get; set; }
        public string? cfieldValue { get; set; }
      
    }
    public class processEngineConditionDetails
    {
        
        public string? cprocessCode { get; set; }
        public int? ciseqno { get; set; }
        public int? icondseqno { get; set; }
        public string? ctype { get; set; }
        public string? clabel { get; set; }
        public string? cplaceholder { get; set; }
        public bool? cisRequired { get; set; }
        public bool? cisReadonly { get; set; }
        public bool? cis_disabled { get; set; }
        public string? cdefaultValue { get; set; }
        public string? cmin { get; set; }
        public string? cmax { get; set; }
        public string? cpattern { get; set; }
        public bool? nallowSpaces { get; set; }
        public bool? nallowNumbers { get; set; }
        public bool? nallowSpecialChars { get; set; }
        public bool? ntrim { get; set; }
        public bool? nautoFocus { get; set; }
        public bool? ncapitalize { get; set; }
        public bool? ntoUpperCase { get; set; }
        public bool? ntoLowerCase { get; set; }
        public bool? nshowCopyButton { get; set; }
        public string? cdependsOn { get; set; }
        public string? cdisabledWhen { get; set; }
        public string? crequiredWhen { get; set; }
        public string? cvisibleWhen { get; set; }
        public string? cfieldValue { get; set; }
        public string? ccondition { get; set; }
        public string? remarks1 { get; set; }
        public string? remarks2 { get; set; }
        public string? remarks3 { get; set; }

    }
    public class GetProcessEngineDTO
    {
        public int? ID { get; set; }
        public int? cprivilege_type { get; set; }    
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cvalue { get; set; }     
        public string? cpriority_label { get; set; }
        public bool? nshow_timeline { get; set; }
        public int? cnotification_type { get; set; }
        public string? cstatus { get; set; }
        public int? cmeta_id { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime? ccreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }

        public List<processEngineChildItems> processEngineChildItems { get; set; }
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


    public class GetTaskList
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
        public List<GetTaskDetails>? TaskChildItems { get; set; }
    }





    public class GetTaskDetails
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
        public int? csla_day { get; set; }
        public int? csla_Hour { get; set; }
        public string? cprocess_type { get; set; }
        public bool? nboard_enabled { get; set; }
        public string? caction_privilege { get; set; }
        public string? crejection_privilege { get; set; }                              
        public string? cisforwarded { get; set; }
        public DateTime? lfwd_date { get; set; }
        public string? cfwd_to { get; set; }
        public string? cis_reassigned { get; set; }
        public DateTime? lreassign_date { get; set; }
        public string? creassign_to { get; set; }

    }
}

