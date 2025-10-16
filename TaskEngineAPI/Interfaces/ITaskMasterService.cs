﻿using System.Data.Common;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Helpers;
namespace TaskEngineAPI.Interfaces
{
  
        public interface ITaskMasterService
        {
            Task<int> InsertTaskMasterAsync(TaskMasterDTO model, int tenantId, string username);
            Task<string> GetAllProcessmetaAsync(int cTenantID,int processid);
            Task<string> Getdepartmentroleposition(int cTenantID, string table);
            Task<string> Getprocessengineprivilege(int cTenantID, string value, string cprivilege);
            Task<string> Getdropdown(int cTenantID, string column);
            Task<string> Gettaskinbox(int cTenantID, string username);
            Task<string> Gettaskapprove(int cTenantID, string username);
            Task<string> Gettaskhold(int cTenantID, string username);
            Task<string> DeptposrolecrudAsync(DeptPostRoleDTO model, int cTenantID, string username);
            Task<int> Processprivilege_mapping(privilegeMappingDTO model, int tenantId, string username);
            Task<string> Gettaskinitiator(int cTenantID, string username);
    }
        
}
