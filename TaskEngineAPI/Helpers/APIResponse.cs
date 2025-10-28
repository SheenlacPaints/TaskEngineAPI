namespace TaskEngineAPI.Helpers
{
    public class APIResponse
    {
        public int status { get; set; }
        public string statusText { get; set; }
        public string error { get; set; }
        public object[] body { get; set; }
    }

    public class BulkInsertResult
    {
        public int Status { get; set; }
        public string StatusText { get; set; }
        public int Total { get; set; }
        public int Success { get; set; }
        public int FailedCount => Failed?.Count ?? 0;
        public List<object> Inserted { get; set; } = new List<object>();
        public List<FailedUser> Failed { get; set; } = new List<FailedUser>();
    }

    public class FailedUser
    {
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Phone { get; set; }
        public string Reason { get; set; }
    }

    public class DuplicateInfo
    {
        public HashSet<string> DuplicateEmails { get; set; } = new HashSet<string>();
        public HashSet<string> DuplicateUserNames { get; set; } = new HashSet<string>();
        public HashSet<string> DuplicatePhones { get; set; } = new HashSet<string>();

        public bool IsDuplicate(string email, string userName, string phone)
        {
            return DuplicateEmails.Contains(email) ||
                   DuplicateUserNames.Contains(userName) ||
                   DuplicatePhones.Contains(phone);
        }

        public string GetReason(string email, string userName, string phone)
        {
            if (DuplicateEmails.Contains(email)) return "Email exists";
            if (DuplicateUserNames.Contains(userName)) return "Username exists";
            if (DuplicatePhones.Contains(phone)) return "Phone number exists";
            return "Unknown duplicate";
        }
    }








}
