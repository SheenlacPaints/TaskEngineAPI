using TaskEngineAPI.DTO.LookUpDTO;

namespace TaskEngineAPI.Interfaces
{
    public interface ILookUpService
    {
        Task<IEnumerable<NotificationTypeDTO>> GetAllNotificationTypesAsync(int tenantID);
        Task<NotificationTypeDTO> GetNotificationTypeByIdAsync(int id, int tenantID);
        Task<bool> CreateNotificationTypeAsync(CreateNotificationTypeDTO notificationType);
        Task<bool> UpdateNotificationTypeAsync(UpdateNotificationTypeDTO notificationType);
        Task<bool> DeleteNotificationTypeAsync(DeleteNotificationTypeDTO model, int tenantID, string username);

        Task<IEnumerable<ProcessPriorityLabelDTO>> GetAllProcessPriorityLabelsAsync(int tenantID);
        Task<ProcessPriorityLabelDTO> GetProcessPriorityLabelByIdAsync(int id, int tenantID);
        Task<bool> CreateProcessPriorityLabelAsync(CreateProcessPriorityLabelDTO priorityLabel);
        Task<bool> UpdateProcessPriorityLabelAsync(UpdateProcessPriorityLabelDTO priorityLabel);
        Task<bool> DeleteProcessPriorityLabelAsync(DeleteProcessPriorityLabelDTO model, int tenantID, string username);

        Task<IEnumerable<ParticipantTypeDTO>> GetAllParticipantTypesAsync(int tenantID);
        Task<ParticipantTypeDTO> GetParticipantTypeByIdAsync(int id, int tenantID);
        Task<bool> CreateParticipantTypeAsync(CreateParticipantTypeDTO participantType);
        Task<bool> UpdateParticipantTypeAsync(UpdateParticipantTypeDTO participantType);
        Task<bool> DeleteParticipantTypeAsync(DeleteParticipantTypeDTO model, int tenantID, string username);
    }
}
