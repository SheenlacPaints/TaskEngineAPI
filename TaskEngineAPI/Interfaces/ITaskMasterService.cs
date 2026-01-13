using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Data.Common;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{

    public interface ITaskMasterService
    {
        Task<int> InsertTaskMasterAsync(TaskMasterDTO model, int tenantId, string username);
        Task<string> GetAllProcessmetaAsync(int cTenantID, int processid);
        Task<string> GetAllProcessmetadetailAsync(int cTenantID, int metaid);
        Task<string> Getdepartmentroleposition(int cTenantID, string table);
        Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege);
        Task<string> Getdropdown(int cTenantID, string column);
        //Task<string> Gettaskinbox(int cTenantID, string username);
        Task<string> Gettaskinbox(int cTenantID, string username, string searchText, int page,int pageSize);
        Task<string> Gettaskapprove(int cTenantID, string username, string? searchText, int page, int pageSize);
        //  Task<string> Gettaskhold(int cTenantID, string username);
        Task<string> DeptposrolecrudAsync(DeptPostRoleDTO model, int cTenantID, string username);
        Task<int> Processprivilege_mapping(privilegeMappingDTO model, int tenantId, string username);
        Task<string> GetTaskInitiator(int cTenantID, string username, string? searchText, int page, int pageSize);
        Task<List<GetprocessEngineConditionDTO>> GetTaskConditionBoard(int cTenantID, int ID);
        Task<List<GettaskinboxbyidDTO>> Gettaskinboxdatabyid(int cTenantID, int ID, string username);
     
        Task<List<GettaskApprovedatabyidDTO>> Gettaskapprovedatabyid(int cTenantID, int ID);
        Task<List<GetmetalayoutDTO>> GetmetalayoutByid(int cTenantID, int itaskno);
        Task<bool> UpdatetaskapproveAsync(updatetaskDTO model, int cTenantID, string username);
        Task<string> GetDropDownFilterAsync(int cTenantID, GetDropDownFilterDTO filterDto);
        Task<string> GettaskHold(int cTenantID, string username, string? searchText, int page, int pageSize);
        Task<List<GettaskHolddatabyidDTO>> GettaskHolddatabyid(int cTenantID, int id,string username);
        Task<bool> UpdatetaskHoldAsync(updatetaskDTO model, int cTenantID, string username);
        Task<string> GettaskReject(int cTenantID, string username, string? searchText, int page, int pageSize);
        Task <List<GettaskRejectdatabyidDTO>>GettaskRejectdatabyid(int cTenantID, int id);
        Task<bool> UpdatetaskRejectAsync(updatetaskDTO model, int cTenantID, string username);
        Task<string> Getopentasklist(int cTenantID, string username, string? searchText);
        Task <List<GetopentasklistdatabyidDTO>> Getopentasklistdatabyid(int cTenantID, int id);

        Task<List<GetmetaviewdataDTO>> Getmetaviewdatabyid(int cTenantID, int id);


        Task<GettaskreassignCountDTO> GettaskReassign(int cTenantID, string username, string? searchText = null, int page = 1, int pageSize = 50);
        Task<List<GettaskInitiatordatabyidDTO>> GettaskInitiatordatabyid(int cTenantID, int id);
        Task<List<GettaskReassigndatabyidDTO>> GettaskReassigndatabyid(int cTenantID, int id);

        Task<string> Gettasktimeline(int cTenantID, string username, string? searchText = null, int pageNo = 1, int pageSize = 50);

        Task<List<GetTaskDetails>> GettasktimelinedetailAsync(int itaskno, string userid, int tenantid);

        Task<string> Getworkflowdashboard(int cTenantID, string username, string searchtext);
    }

}
