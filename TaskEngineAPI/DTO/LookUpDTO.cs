using System;

namespace TaskEngineAPI.DTO.LookUpDTO
{
    public class NotificationTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string notification_type { get; set; } = string.Empty;
        public bool nis_active { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime lcreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime lmodified_date { get; set; }
    }

    public class CreateNotificationTypeDTO
    {
        public int ctenent_id { get; set; }
        public string notification_type { get; set; } = string.Empty;
        public bool nis_active { get; set; } = true;
        public string? ccreated_by { get; set; }
    }

    public class UpdateNotificationTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string notification_type { get; set; } = string.Empty;
        public bool nis_active { get; set; }
        public string? cmodified_by { get; set; }
    }

    public class DeleteNotificationTypeDTO
    {
        public int ID { get; set; }
    }

    public class ParticipantTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string participant_type { get; set; } = string.Empty;
        public bool nis_active { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime lcreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime lmodified_date { get; set; }
    }

    public class CreateParticipantTypeDTO
    {
        public int ctenent_id { get; set; }
        public string participant_type { get; set; } = string.Empty;
        public bool nis_active { get; set; } = true;
        public string? ccreated_by { get; set; }
    }

    public class UpdateParticipantTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string participant_type { get; set; } = string.Empty;
        public bool nis_active { get; set; }
        public string? cmodified_by { get; set; }
    }

    public class DeleteParticipantTypeDTO
    {
        public int ID { get; set; }
    }

    public class ProcessPrivilegeTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string cprocess_privilege { get; set; } = string.Empty;
        public bool nis_active { get; set; }
        public string? ccreated_by { get; set; }
        public DateTime lcreated_date { get; set; }
        public string? cmodified_by { get; set; }
        public DateTime? lmodified_date { get; set; }
        public string slug { get; set; } = string.Empty;
    }

    public class CreateProcessPrivilegeTypeDTO
    {
        public int ctenent_id { get; set; }
        public string cprocess_privilege { get; set; } = string.Empty;
        public bool nis_active { get; set; } = true;
        public string? ccreated_by { get; set; }
    }

    public class UpdateProcessPrivilegeTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string cprocess_privilege { get; set; } = string.Empty;
        public bool nis_active { get; set; }
        public string? cmodified_by { get; set; }
    }

    public class DeleteProcessPrivilegeTypeDTO
    {
        public int ID { get; set; }
    }
    public class PrivilegeItemDTO
    {
        public string value { get; set; } = string.Empty;
        public string view_value { get; set; } = string.Empty;
    }

    public class ProcessMappingDTO
    {
        public int processID { get; set; }
        public int? mappingID { get; set; }
        public string privilegeType { get; set; } = string.Empty;
        public List<PrivilegeItemDTO> privilegeList { get; set; } = new List<PrivilegeItemDTO>();
    }

    public class ProcessMappingResponseDTO
    {
        public int processID { get; set; }
        public int mappingID { get; set; }
        public string processName { get; set; } = string.Empty;
        public string privilegeType { get; set; } = string.Empty;
        public List<string> privilegeList { get; set; } = new List<string>();
    }
}