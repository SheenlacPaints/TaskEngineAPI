
namespace TaskEngineAPI.DTO.LookUpDTO
{
    public class ApiResponse
    {
        public object body { get; set; }
        public string statusText { get; set; }
        public int status { get; set; }
    }

    public class InputDTO
    {
        public string payload { get; set; }
    }

    public class pay
    {
        public string payload { get; set; }
    }

    // Notification Type DTOs
    public class NotificationTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string notification_type { get; set; }
        public bool nis_active { get; set; }
        public string ccreated_by { get; set; }
        public DateTime lcreated_date { get; set; }
        public string cmodified_by { get; set; }
        public DateTime lmodified_date { get; set; }
    }

    public class CreateNotificationTypeDTO
    {
        public int ctenent_id { get; set; }
        public string notification_type { get; set; }
        public bool nis_active { get; set; }
        public string ccreated_by { get; set; }
    }

    public class UpdateNotificationTypeDTO
    {
        public int ID { get; set; }
        public string notification_type { get; set; }
        public bool nis_active { get; set; }
        public string cmodified_by { get; set; }
    }

    public class DeleteNotificationTypeDTO
    {
        public int ID { get; set; }
    }

    public class ProcessPriorityLabelDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string priority_type { get; set; }
        public bool nis_active { get; set; }
        public string ccreated_by { get; set; }
        public DateTime lcreated_date { get; set; }
        public string cmodified_by { get; set; }
        public DateTime lmodified_date { get; set; }
    }

    public class CreateProcessPriorityLabelDTO
    {
        public int ctenent_id { get; set; }
        public string priority_type { get; set; }
        public bool nis_active { get; set; }
        public string ccreated_by { get; set; }
    }

    public class UpdateProcessPriorityLabelDTO
    {
        public int ID { get; set; }
        public string priority_type { get; set; }
        public bool nis_active { get; set; }
        public string cmodified_by { get; set; }
    }

    public class DeleteProcessPriorityLabelDTO
    {
        public int ID { get; set; }
    }

    public class ParticipantTypeDTO
    {
        public int ID { get; set; }
        public int ctenent_id { get; set; }
        public string participant_type { get; set; }
        public bool nis_active { get; set; }
        public string ccreated_by { get; set; }
        public DateTime lcreated_date { get; set; }
        public string cmodified_by { get; set; }
        public DateTime lmodified_date { get; set; }
    }

    public class CreateParticipantTypeDTO
    {
        public int ctenent_id { get; set; }
        public string participant_type { get; set; }
        public bool nis_active { get; set; }
        public string ccreated_by { get; set; }
    }

    public class UpdateParticipantTypeDTO
    {
        public int ID { get; set; }
        public string participant_type { get; set; }
        public bool nis_active { get; set; }
        public string cmodified_by { get; set; }
    }

    public class DeleteParticipantTypeDTO
    {
        public int ID { get; set; }
    }
}

