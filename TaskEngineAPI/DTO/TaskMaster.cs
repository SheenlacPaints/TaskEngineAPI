using System.Net.NetworkInformation;
using System;
using System.ComponentModel.DataAnnotations;
using TaskEngineAPI.DTO;
using System.Threading.Tasks;

namespace TaskEngineAPI.DTO
{
    public class TaskMasterDTO
    {

        [StringLength(100)]
        public string? ctask_type { get; set; }
        [StringLength(255)]
        public string? ctask_name { get; set; }
        public string? ctask_description { get; set; }
        public int? cprocess_id { get; set; }
        public string? cremarks { get; set; }
        public string? cmeta_response { get; set; }
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
        public string? creportingmanagername { get; set; }
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
        public int cheader_id { get; internal set; }
    }

    public class privilegeMapping
    {
        public int? entity_type { get; set; }
        public string? entity_id { get; set; }

    }


    public class GetprocessEngineConditionDTO
    {
        public int? ID { get; set; }
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
    }

    public class GettaskinboxbyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }
        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }

        public string? taskinitiatedbyname { get; set; }
        public string? cattachment { get; set; }
        public bool? showTimeline { get; set; }
        public string? cremarks { get; set; }

        public string? cmeta_response { get; set; }
        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }
        public List<PreviousapproverDTO> approvers { get; set; }
    }



    public class GettaskApprovedatabyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }
        public string? cremarks { get; set; }
        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public string? HoldRemarks { get; set; }
        public string? RejectReason { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }

        public string? taskinitiatedbyname { get; set; }

        public bool? showTimeline { get; set; }
        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }

    }

    public class GettaskHolddatabyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }
        public string ? cremarks { get; set; }
        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }

        public string? taskinitiatedbyname { get; set; }
        public string? HoldRemarks { get; set; }
        public string? Remarks { get; set; }
        public bool? showTimeline { get; set; }
        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }
        public List<PreviousapproverDTO> approvers { get; set; }

    }

    public class GettaskInitiatordatabyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }
        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }

        public string? taskinitiatedbyname { get; set; }

        public bool? showTimeline { get; set; }
        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }

    }


    public class GettaskReassigndatabyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }

        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }
        public string? TaskRemarks { get; set; }
        public string? taskinitiatedbyname { get; set; }

        public bool? showTimeline { get; set; }
        public DateTime? ReassignedDate { get; set; }
        public string? ReassignedTo { get; set; }
        public string? Remarks { get; set; }
        public string? ReassignedUsername { get; set; }

        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }

    }


    public class GettaskRejectdatabyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? cremarks { get; set; }
        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }

        public string? taskinitiatedbyname { get; set; }

        public bool? showTimeline { get; set; }
        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }

    }

    public class GetopentasklistdatabyidDTO
    {
        public int? itaskno { get; set; }
        public int? processId { get; set; }
        public string? processName { get; set; }
        public string? processDesc { get; set; }
        public string? activityName { get; set; }
        public string? priorityLabel { get; set; }
        public string? activityDesc { get; set; }
        public string? taskStatus { get; set; }
        public string? participantType { get; set; }
        public string? actionPrivilege { get; set; }
        public string? createdbyavatar { get; set; }
        public string? modifiedbyavatar { get; set; }
        public string? crejection_privilege { get; set; }
        public string? assigneeType { get; set; }
        public string? assigneeValue { get; set; }
        public int? slaDays { get; set; }
        public int? slaHours { get; set; }
        public string? executionType { get; set; }
        public DateTime? taskAssignedDate { get; set; }
        public DateTime? taskInitiatedDate { get; set; }

        public string? taskinitiatedbyname { get; set; }

        public bool? showTimeline { get; set; }
        public List<TimelineDTO>? timeline { get; set; }

        public List<processEnginetaskMeta> meta { get; set; }

        public List<GetprocessEngineConditionDTO> board { get; set; }

    }
}

public class processEnginetaskMeta
{
    public string? cdata { get; set; }
    public string? cinputType { get; set; }
    public string? clabel { get; set; }
    public string? cplaceholder { get; set; }
    public bool? cisRequired { get; set; }
    public bool? cisReadonly { get; set; }
    public bool? cisDisabled { get; set; }
    public string? cfieldValue { get; set; }

    public string? cdatasource { get; set; }


}

public class TimelineDTO
{
    public string? status { get; set; }
    public string? remarks { get; set; }

    public string? taskName { get; set; }
    public string? userName { get; set; }
    public string? userAvatar { get; set; }


}

public class GetmetalayoutDTO
{
    public int? ID { get; set; }
    public int? cprocess_id { get; set; }
    public string? cdata { get; set; }
    public string? cinput_type { get; set; }
    public string? label { get; set; }
    public string? cplaceholder { get; set; }
    public bool? cis_required { get; set; }
    public bool? cis_readonly { get; set; }
    public bool? cis_disabled { get; set; }
    public string? cfield_value { get; set; }
    public string? cdata_source { get; set; }

}

public class updatetaskDTO
{

    public int? ID { get; set; }
    public int? itaskno { get; set; }
    public string? status { get; set; }
    public DateTime? status_date { get; set; }

    public string? remarks { get; set; }
    public string? rejectedreason { get; set; }
    public string? reassignto { get; set; }

    public List<metaData> metaData { get; set; }
}

public class GetDropDownFilterDTO
{
    public string? filtervalue1 { get; set; }
    public string? filtervalue2 { get; set; }
    public string? filtervalue3 { get; set; }
    public string? filtervalue4 { get; set; }
    public string? filtervalue5 { get; set; }

}

public class PreviousapproverDTO
{
    public int? ID { get; set; }
    public string? activity { get; set; }
    public string? description { get; set; }
    public string? status { get; set; }
    public DateTime? datatime { get; set; }
    public string? cremarks { get; set; }
    public string? pendingwith { get; set; }
    public string? pendingwithavatar { get; set; }

    public string? cboard_visablity_flag { get; set; }
    public string? cboard_visablity { get;set; }

    


}


public class GetmetaviewdataDTO
{
    public int? ID { get; set; }
    public int? itaskno { get; set; }
    public int? icond_seqno { get; set; }
    public string? ctype { get; set; }
    public string? clabel { get; set; }
    public string? cplaceholder { get; set; }
    public string? cfield_value { get; set; }
    public string? ccondition { get; set; }
    public string? cdata_source { get; set; }
    public string? cdata { get; set; }
    public string? cattachment { get; set; }

}
public class TaskInboxResponse
{
    //public bool Success { get; set; }
    public int TotalCount { get; set; }
    public List<GetTaskList> Data { get; set; }
}

public class APIFetchDTO
{
    public int? APIID { get; set; }
    public string? Payload { get; set; }
    public string? apimethod { get; set; }
}








