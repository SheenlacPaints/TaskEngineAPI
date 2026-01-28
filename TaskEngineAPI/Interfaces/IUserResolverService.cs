namespace TaskEngineAPI.Interfaces
{
    public interface IUserResolverService
    {
        (string Phone, string UserName) GetUser(
            string employeeCode, string positionCode, string roleCode,
            string deptCode, string username, string userId);
    }

}
