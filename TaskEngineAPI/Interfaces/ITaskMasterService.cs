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
        Task<string> Gettaskinbox(int cTenantID, string username);
        Task<string> Gettaskapprove(int cTenantID, string username);
        //  Task<string> Gettaskhold(int cTenantID, string username);
        Task<string> DeptposrolecrudAsync(DeptPostRoleDTO model, int cTenantID, string username);
        Task<int> Processprivilege_mapping(privilegeMappingDTO model, int tenantId, string username);
        Task<string> GetTaskInitiator(int cTenantID, string username);
        Task<List<GetprocessEngineConditionDTO>> GetTaskConditionBoard(int cTenantID, int ID);
        Task<List<GettaskinboxbyidDTO>> Gettaskinboxdatabyid(int cTenantID, int ID);
        Task<List<GettaskApprovedatabyidDTO>> Gettaskapprovedatabyid(int cTenantID, int ID);
        Task<List<GetmetalayoutDTO>> GetmetalayoutByid(int cTenantID, int itaskno);
        Task<bool> UpdatetaskapproveAsync(updatetaskDTO model, int cTenantID, string username);
        Task<string> GetDropDownFilterAsync(int cTenantID, GetDropDownFilterDTO filterDto);
        Task<string> GettaskHold(int cTenantID, string username);
        Task<List<GettaskHolddatabyidDTO>> GettaskHolddatabyid(int cTenantID, int id);
        Task<bool> UpdatetaskHoldAsync(updatetaskDTO model, int cTenantID, string username);
        Task<string> GettaskReject(int cTenantID, string username);
        Task <List<GettaskRejectdatabyidDTO>>GettaskRejectdatabyid(int cTenantID, int id);
        Task<bool> UpdatetaskRejectAsync(updatetaskDTO model, int cTenantID, string username);
    }

}
