using System.Data.SqlClient;
using System.Data;
using TaskEngineAPI.DTO;
using TaskEngineAPI.Interfaces;
using TaskEngineAPI.Models;
using TaskEngineAPI.DTO.LookUpDTO;
using TaskEngineAPI.Helpers;
using Newtonsoft.Json;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using System.Collections.Generic;
using TaskEngineAPI.Controllers;
using System.Text;
using Azure;

namespace TaskEngineAPI.Services
{


    public class APIIntegrationService : IApiProxyService
    {
      
        private readonly IAdminRepository _repository;
        private readonly IConfiguration _config;
        private readonly IAdminRepository _AdminRepository;
        private readonly UploadSettings _uploadSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        public APIIntegrationService(IAdminRepository repository, IConfiguration _configuration, IAdminRepository AdminRepository, IOptions<UploadSettings> uploadSettings, IHttpClientFactory httpClientFactory)
        {
            _repository = repository;
            _config = _configuration;
            _AdminRepository = AdminRepository;
            _uploadSettings = uploadSettings.Value;
            _httpClientFactory = httpClientFactory;
        }



        public async Task<string> ExecuteIntegrationApi(APIFetchDTO  model, int tenantId, string username)
        {
            string url = null;
            string method = "POST"; // Default
            var connectionString = _config.GetConnectionString("Database");

            using (var conn = new SqlConnection(connectionString))
            {
                var sql = "SELECT capi_url, capi_method FROM tbl_users_api_sync_config WHERE id = @apiId";

                using (var cmd = new SqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@apiId", model.APIID);
                    await conn.OpenAsync();

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            url = reader["capi_url"]?.ToString();
                            method = reader["capi_method"]?.ToString() ?? "POST";
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(url))
                return "{\"error\": \"API Config not found\"}";

            var client = _httpClientFactory.CreateClient();

            var requestMethod = new HttpMethod(method.ToUpper().Trim());
            var request = new HttpRequestMessage(requestMethod, url);

            if (requestMethod != HttpMethod.Get)
            {
                request.Content = new StringContent(model.Payload, Encoding.UTF8, "application/json");
            }
            try
            {
                var response = await client.SendAsync(request);             
                var apiResponse = await response.Content.ReadAsStringAsync();

                string mappingJson = "";
                using (var conn = new SqlConnection(connectionString))
                {
                    var mapSql = @"SELECT cmetaapi_response 
                   FROM tbl_process_engine_master 
                   WHERE id = @id";

                    using (var cmd = new SqlCommand(mapSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@id", model.processid);
                        await conn.OpenAsync();

                        var result = await cmd.ExecuteScalarAsync();
                        mappingJson = result?.ToString();
                    }
                }

                if (string.IsNullOrEmpty(mappingJson))
                    return apiResponse; // fallback

                var mappings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(mappingJson)
                                .OrderBy(x => Convert.ToInt32(x["sequence"]))
                                .ToList();

                var apiObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiResponse);

                if (!apiObj.ContainsKey("body"))
                    return apiResponse;

                var bodyStr = apiObj["body"].ToString();

                List<Dictionary<string, object>> dataList;

                if (bodyStr.Trim().StartsWith("["))
                {
                    dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(bodyStr);
                }
                else
                {
                    var single = JsonConvert.DeserializeObject<Dictionary<string, object>>(bodyStr);
                    dataList = new List<Dictionary<string, object>> { single };
                }

                // STEP 4: Apply mapping
                var finalList = new List<Dictionary<string, object>>();

                foreach (var item in dataList)
                {
                    var newObj = new Dictionary<string, object>();

                    foreach (var map in mappings)
                    {
                        var sourceCol = map["responseColumn"].ToString();
                        var displayCol = map["displayColumn"].ToString();

                        if (item.ContainsKey(sourceCol))
                        {
                            newObj[displayCol] = item[sourceCol];
                        }
                        else
                        {
                            newObj[displayCol] = null;
                        }
                    }

                    finalList.Add(newObj);
                }

                // STEP 5: Return final JSON
                return JsonConvert.SerializeObject(finalList);


            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"HTTP Request Failed\", \"details\": \"{ex.Message}\"}}";
            }
        }

    }
}