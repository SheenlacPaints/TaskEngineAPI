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



        //public async Task<string> ExecuteIntegrationApi(APIFetchDTO model, int tenantId, string username)
        //{
        //    string url = null;
        //    string method = "POST"; // Default
        //    var connectionString = _config.GetConnectionString("Database");

        //    using (var conn = new SqlConnection(connectionString))
        //    {
        //        var sql = "SELECT capi_url, capi_method FROM tbl_users_api_sync_config WHERE id = @apiId";

        //        using (var cmd = new SqlCommand(sql, conn))
        //        {
        //            cmd.Parameters.AddWithValue("@apiId", model.APIID);
        //            await conn.OpenAsync();

        //            using (var reader = await cmd.ExecuteReaderAsync())
        //            {
        //                if (await reader.ReadAsync())
        //                {
        //                    url = reader["capi_url"]?.ToString();
        //                    method = reader["capi_method"]?.ToString() ?? "POST";
        //                }
        //            }
        //        }
        //    }

        //    if (string.IsNullOrEmpty(url))
        //        return "{\"error\": \"API Config not found\"}";

        //    var client = _httpClientFactory.CreateClient();

        //    var requestMethod = new HttpMethod(method.ToUpper().Trim());
        //    var request = new HttpRequestMessage(requestMethod, url);

        //    if (requestMethod != HttpMethod.Get)
        //    {
        //        request.Content = new StringContent(model.Payload, Encoding.UTF8, "application/json");
        //    }
        //    try
        //    {
        //        var response = await client.SendAsync(request);
        //        var apiResponse = await response.Content.ReadAsStringAsync();

        //        string mappingJson = "";
        //        using (var conn = new SqlConnection(connectionString))
        //        {
        //            var mapSql = @"SELECT cmetaapi_response 
        //           FROM tbl_process_engine_master 
        //           WHERE id = @id";

        //            using (var cmd = new SqlCommand(mapSql, conn))
        //            {
        //                cmd.Parameters.AddWithValue("@id", model.processid);
        //                await conn.OpenAsync();

        //                var result = await cmd.ExecuteScalarAsync();
        //                mappingJson = result?.ToString();
        //            }
        //        }

        //        if (string.IsNullOrEmpty(mappingJson))
        //            return apiResponse; // fallback

        //        var mappings = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(mappingJson)
        //                        .OrderBy(x => Convert.ToInt32(x["sequence"]))
        //                        .ToList();

        //        var apiObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(apiResponse);

        //        if (!apiObj.ContainsKey("body"))
        //            return apiResponse;

        //        var bodyStr = apiObj["body"].ToString();

        //        List<Dictionary<string, object>> dataList;

        //        if (bodyStr.Trim().StartsWith("["))
        //        {
        //            dataList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(bodyStr);
        //        }
        //        else
        //        {
        //            var single = JsonConvert.DeserializeObject<Dictionary<string, object>>(bodyStr);
        //            dataList = new List<Dictionary<string, object>> { single };
        //        }

        //        // STEP 4: Apply mapping
        //        var finalList = new List<Dictionary<string, object>>();

        //        foreach (var item in dataList)
        //        {
        //            var newObj = new Dictionary<string, object>();

        //            foreach (var map in mappings)
        //            {
        //                var sourceCol = map["responseColumn"].ToString();
        //                var displayCol = map["displayColumn"].ToString();

        //                if (item.ContainsKey(sourceCol))
        //                {
        //                    newObj[displayCol] = item[sourceCol];
        //                }
        //                else
        //                {
        //                    newObj[displayCol] = null;
        //                }
        //            }

        //            finalList.Add(newObj);
        //        }

        //        // STEP 5: Return final JSON
        //        return JsonConvert.SerializeObject(finalList);


        //    }
        //    catch (Exception ex)
        //    {
        //        return $"{{\"error\": \"HTTP Request Failed\", \"details\": \"{ex.Message}\"}}";
        //    }
        //}
        public async Task<string> ExecuteIntegrationApi(APIFetchDTO model, int tenantId, string username, string bearerToken)
        {
            string url = null;
            string method = "POST";
            var connectionString = _config.GetConnectionString("Database");

            // 🔹 STEP 1: Get API Config
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
            var request = new HttpRequestMessage(new HttpMethod(method.ToUpper().Trim()), url);

            // ✅ ADD BEARER TOKEN HERE
            if (!string.IsNullOrEmpty(bearerToken))
            {
                if (!bearerToken.StartsWith("Bearer "))
                    bearerToken = "Bearer " + bearerToken;

                request.Headers.TryAddWithoutValidation("Authorization", bearerToken);
            }

            if (method.ToUpper() != "GET")
            {
                request.Content = new StringContent(model.Payload ?? "", Encoding.UTF8, "application/json");
            }

            try
            {
                // 🔹 STEP 2: Call API
                var response = await client.SendAsync(request);
                var apiResponse = await response.Content.ReadAsStringAsync();

                // ❗ If HTTP failed → return raw response
                if (!response.IsSuccessStatusCode)
                    return apiResponse;

                // 🔹 STEP 3: Get Mapping JSON
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
                    return apiResponse;

                var mappings = JsonConvert
                    .DeserializeObject<List<Dictionary<string, object>>>(mappingJson)
                    .OrderBy(x => Convert.ToInt32(x["sequence"]))
                    .ToList();

                // 🔹 STEP 4: Parse API Response SAFELY
                var apiObj = Newtonsoft.Json.Linq.JObject.Parse(apiResponse);

                if (apiObj["error"] != null && !string.IsNullOrEmpty(apiObj["error"].ToString()))
                {
                    return apiResponse;
                }

                if (apiObj["body"] == null)
                {
                    return apiResponse;
                }

                var bodyToken = apiObj["body"];

                List<Dictionary<string, object>> dataList;

                if (bodyToken.Type == Newtonsoft.Json.Linq.JTokenType.Array)
                {
                    dataList = bodyToken.ToObject<List<Dictionary<string, object>>>();
                }
                else if (bodyToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                {
                    var single = bodyToken.ToObject<Dictionary<string, object>>();
                    dataList = new List<Dictionary<string, object>> { single };
                }
                else
                {
                    return apiResponse;
                }

                // 🔹 STEP 5: Apply Mapping
                var finalList = new List<Dictionary<string, object>>();

                foreach (var item in dataList)
                {
                    var newObj = new Dictionary<string, object>();

                    foreach (var map in mappings)
                    {
                        var sourceCol = map["responseColumn"]?.ToString();
                        var displayCol = map["displayColumn"]?.ToString();

                        if (!string.IsNullOrEmpty(sourceCol) && item.ContainsKey(sourceCol))
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

                // 🔹 STEP 6: Return Final JSON
                return JsonConvert.SerializeObject(finalList);
            }
            catch (Exception ex)
            {
                return $"{{\"error\": \"HTTP Request Failed\", \"details\": \"{ex.Message}\"}}";
            }
        }

        public async Task<string> BoardExecuteIntegrationApi(BoardAPIFetchDTO model, int tenantId, string username)
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
                    var mapSql = @"select top 1 cboard_metaapi_response from tbl_process_engine_details a 
                inner join  tbl_taskflow_detail b on a.ciseqno=b.iseqno where a.cheader_id=@processid and b.id=@detailid";

                    using (var cmd = new SqlCommand(mapSql, conn))
                    {
                        cmd.Parameters.AddWithValue("@processid", model.processid);
                        cmd.Parameters.AddWithValue("@detailid", model.Detailid);
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
                        var sourceCol = map["col"].ToString();
                        var displayCol = map["displayName"].ToString();

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