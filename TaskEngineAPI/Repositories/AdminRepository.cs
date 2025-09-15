using System.Collections.Generic;
using System.Data.SqlClient;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;

namespace TaskEngineAPI.Repositories
{

    public class AdminRepository : IAdminRepository
    {
        private readonly IConfiguration _config;

        public AdminRepository(IConfiguration config)
        {
            _config = config;
        }


     


    }

}
