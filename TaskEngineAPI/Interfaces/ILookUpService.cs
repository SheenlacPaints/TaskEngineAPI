
using TaskEngineAPI.DTO.LookUpDTO;

namespace TaskEngineAPI.Interfaces
{
    public interface ILookUpService
    {

        Task<IEnumerable<NotificationTypeDTO>> GetAllNotificationTypesAsync(int tenantID);
        Task<bool> CreateNotificationTypeAsync(CreateNotificationTypeDTO model);
        Task<bool> UpdateNotificationTypeAsync(UpdateNotificationTypeDTO model);
        Task<bool> DeleteNotificationTypeAsync(DeleteNotificationTypeDTO model, int tenantID, string username);


        Task<IEnumerable<ParticipantTypeDTO>> GetAllParticipantTypesAsync(int tenantID);
        Task<bool> CreateParticipantTypeAsync(CreateParticipantTypeDTO model);
        Task<bool> UpdateParticipantTypeAsync(UpdateParticipantTypeDTO model);
        Task<bool> DeleteParticipantTypeAsync(DeleteParticipantTypeDTO model, int tenantID, string username);


        Task<IEnumerable<ProcessPrivilegeTypeDTO>> GetAllProcessPrivilegeTypesAsync(int tenantID);
        Task<bool> CreateProcessPrivilegeTypeAsync(CreateProcessPrivilegeTypeDTO model);
        Task<bool> UpdateProcessPrivilegeTypeAsync(UpdateProcessPrivilegeTypeDTO model);
        Task<bool> DeleteProcessPrivilegeTypeAsync(DeleteProcessPrivilegeTypeDTO model, int tenantID, string username); 
        


        Task<IEnumerable<PrivilegeItemDTO>> GetPrivilegeTypeByIdAsync(int privilegeType, int tenantID);
        Task<IEnumerable<PrivilegeItemDTO>> GetPrivilegeListAsync(int tenantID);
     

    }
}