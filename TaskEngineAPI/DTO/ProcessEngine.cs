using TaskEngineAPI.DTO.LookUpDTO;

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
        public string? cprocessdescription { get; set; }
        public int? cprivilegeType  { get; set; }
        public string? cvalue { get; set; }       
        public string? cstatus { get; set; }
        public string? cpriorityLabel { get; set; }
        public bool? nshowTimeline { get; set; }
        public int? cnotificationType { get; set; }
        public string? cmetaType { get; set; }
        public int? cmetaId { get; set; }
        public string? cmetaName { get; set; }

        public bool? nshow_table { get; set; }
        public bool? nis_metaapi_integration { get; set; }
        public int? cmetaapi_id { get; set; }

        public string? cmetaapi_response { get; set; }
        public List<processEngineChildItems> processEngineChildItems { get; set; }
        public List<processEngineMeta> processEngineMeta { get; set; }   

        
    }


    public class UpdateProcessEngineDTO
    {
        public int? ID { get; set; }
        public string? cprocessCode { get; set; }
        public string? cprocessName { get; set; }
        public string? cprocessdescription { get; set; }
        public int? cprivilegeType { get; set; }
        public string? cvalue { get; set; }
        public string? cstatus { get; set; }
        public string? cpriorityLabel { get; set; }
        public bool? nshowTimeline { get; set; }
        public int? cnotificationType { get; set; }
        public string? cmetaType { get; set; }
        public int? cmetaId { get; set; }
        public string? cmetaName { get; set; }
        public bool? nshow_table { get; set; }
        public bool? nis_metaapi_integration { get; set; }
        public int? cmetaapi_id { get; set; }

        public string? cmetaapi_response { get; set; }
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
        public string? cboard_visablity { get; set; }

        public bool? nsla_overdue_action { get; set; }
        public List<processEngineConditionDetails> processEngineConditionDetails { get; set; }
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
        public string? cfieldValue { get; set; }
        public string? cdatasource { get; set; }
        public string? ccondition { get; set; }
        //public string? remarks1 { get; set; }
        //public string? remarks2 { get; set; }
        //public string? remarks3 { get; set; }

    }


    public class processEngineMeta
    {
        public string? cinputType { get; set; }
        public string? label { get; set; }
        public string? cplaceholder { get; set; }
        public bool? cisRequired { get; set; }
        public bool? cisReadonly { get; set; }
        public bool? cisDisabled { get; set; }   
        public string? cfieldValue { get; set; }

        public string? cdatasource { get; set; }

        public string? capi_mapping { get; set; }
    }

    public class GetProcessEngineCountDTO
    {
        public int? totalCount { get; set; }
        public List<GetProcessEngineDTO> data { get; set; }



    }

    public class GettaskreassignCountDTO
    {
        public int? totalCount { get; set; }
        public List<GetTaskList> data { get; set; }



    }

    public class GetProcessEngineDTO
    {
        public int? ID { get; set; }
        public string? cprocessType { get; set; }    
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cprocessvalueid { get; set; }
        public string? cpriority_label { get; set; }
        public string? privilege_name { get; set; }
        public int? cprivilege_type { get; set; }
        public bool? nshow_timeline { get; set; }

        public bool? nshow_table { get; set; }
        
        public int? cnotification_type { get; set; }
        public string? cprocessdescription { get; set; }
        public string? cprocessvalue { get; set; }     
        public string? cstatus { get; set; }
        public int? cmeta_id { get; set; }

        public string? cmetaName { get; set; }
        public string? created_by { get; set; }
        public DateTime? ccreated_date { get; set; }
        public string? modified_by { get; set; }
        public DateTime? lmodified_date { get; set; }
        public string? cstatus_description { get; set; }
        public string? Notification_Description { get; set; }
        public int processEngineChildItems { get; set; }
        public string? slasum { get; set; }
        public int? Usedcount { get; set; }
        public int? Activecount { get; set; }
        public bool? nis_metaapi_integration { get; set; }
        public int? cmetaapi_id { get; set; }
        
    }


    public class GetprocessEngineChildItems
    {
        public int? cheader_id { get; set; }
       
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
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }
        public DateTime? lcompleteddate { get; set; }
        public string? ccreatedby { get; set; }
        public string? ccreatedbyname { get; set; }

        public DateTime? lcreateddate { get; set; }
        public string? cmodifiedby { get; set; }
        public string? cmodifiedbyname { get; set; }
        
        public DateTime? lmodifieddate { get; set; }
        public string? Employeecode { get; set; }
        public string? Employeename { get; set; }
        
        public string? EmpDepartment { get; set; }
        public int? cprocess_id { get; set; }

        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cprocessdescription { get; set; }

        public string? cremarks { get; set; }
        public string? cmeta_response { get; set; }

        //public string? privilege_name { get; set; }
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
        public string? cprofile_image_name { get; set; }
        public string? creassign_name { get; set; }
        public string? cactivityname { get; set; }
        public string? cactivity_description { get; set; }
        public string? cmappingcode_name { get; set; }
    }



   
    public class GetTaskinitiateList
    {
        public int ID { get; set; }
        public int cprocessID { get; set; }
        public int itaskno { get; set; }
        public string? ctasktype { get; set; }
        public string? ctaskname { get; set; }
        public string? ctaskdescription { get; set; }
        public string? cstatus { get; set; }
        public DateTime? lcompleteddate { get; set; }
        public string? ccreatedby { get; set; }
        public string? ccreatedbyname { get; set; }
        public string? createdbyavatar { get; set; }
        public DateTime? lcreateddate { get; set; }
        public string? cmodifiedby { get; set; }
        public string? cmodifiedbyname { get; set; }

        public DateTime? lmodifieddate { get; set; }
        public string? Employeecode { get; set; }
        public string? Employeename { get; set; }

        public string? EmpDepartment { get; set; }
        public int? cprocess_id { get; set; }

        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cprocessdescription { get; set; }

        public string? cremarks { get; set; }

        public string? cmeta_response { get; set; }

        //public string? privilege_name { get; set; }
        public List<GetTaskinitiateDetails>? TaskChildItems { get; set; }

    }
    public class GetTaskinitiateDetails
    {
        public int? ID { get; set; }
        public int? iheader_id { get; set; }
        public int? itaskno { get; set; }
        public int? iseqno { get; set; }
        public string? ctasktype { get; set; }
        public string? cmappingcode { get; set; }
        public string? cmappingcodename { get; set; }
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

        public string? creassign_name { get; set; }
        public string? cactivityname { get; set; }
        public string? cactivity_description { get; set; }
        public string? cprofile_image_name { get; set; }
        public string? cmappingcode_name { get; set; }
    }


    public class GetIDProcessEngineDTO
    {
        public int? ID { get; set; }
        public string? cprocesscode { get; set; }
        public string? cprocessname { get; set; }
        public string? cprocessdescription { get; set; }
        
        public string? cprocessvalue { get; set; }   
        public string? cstatus { get; set; }
        public string? cstatus_description { get; set; }
        public string? cpriority_label { get; set; }
        public bool? nshow_timeline { get; set; }
        public int? cnotification_type { get; set; }
        public string? Notification_Description { get; set; }
        public int? cmeta_id { get; set; }
        public string? cmetaname { get; set; }
        public string? privilege_name { get;set; }
        public int? cprivilege_type { get; set; }

        public bool? nshow_table { get; set; }

        public string? cattachment { get; set; }

        public bool? nis_metaapi_integration { get; set; }
        public int? cmetaapi_id { get; set; }

        public string? cmetaapi_response { get; set; }
        public List<GetIDprocessEngineChildItems> processEngineChildItems { get; set; }
       
       public List<processEngineMeta> processEngineMeta { get; set; }

    }


    public class GetIDprocessEngineChildItems
    {
        public int? id{ get; set; }
        public int? cheader_id { get; set; }
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
        public string? cboard_visablity { get; set; }
        public bool? nsla_overdue_action { get; set; }
        public List<processEngineConditionDetails> processEngineConditionDetails { get; set; }
    }


    public class updatestatusdeleteDTO
    {
        public int? ID { get; set; }
        public int? status { get; set; }
        public bool? isDeleted { get; set; }

    }

    public class createprocessmappingDTO
    {
        public int? cprocessid { get; set; }
        public string? cprocesscode { get; set; }
        public int? cprivilegeType { get; set; }
        

        public List<privilegeList>? privilegeList { get; set; }
    }

    public class privilegeList
    {
        public int? value { get; set; }
        public string? view_value { get; set; }
   
    }
    public class MappingListDTO
    {
        public int processID { get; set; }
        public int mappingID { get; set; }
        public string cprocessname { get; set; } = string.Empty;
        public string cprocessdescription { get; set; } = string.Empty;
        public string cprocesscode { get; set; }      
        public string privilegeType { get; set; }      
        public string privilegeTypevalue { get; set; }  
        public bool? cis_active { get; set; }
        public List<PrivilegeItemDTO> privilegeList { get; set; } = new List<PrivilegeItemDTO>();
    }

    public class updateprocessmappingDTO
    {
        public int? cmappingid { get; set; }
        public int? cprocessid { get; set; }      
        public int? cprivilegeType { get; set; }
        public bool? cis_active { get; set; }
        public List<privilegeList>? privilegeList { get; set; }
    }


    public class DeleteProcessMappingDTO
    {
        public int MappingId { get; set; }
    }

  
}

