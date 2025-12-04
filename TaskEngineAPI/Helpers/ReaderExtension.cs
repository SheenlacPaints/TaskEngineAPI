using System.Data.SqlClient;

namespace TaskEngineAPI.Helpers
{
    

        public  static class ReaderExtension
        {
            public static string GetSafeString(this SqlDataReader r, string col) =>
                r[col] == DBNull.Value ? "" : r[col].ToString();

            public static int GetSafeInt(this SqlDataReader r, string col) =>
                r[col] == DBNull.Value ? 0 : Convert.ToInt32(r[col]);

            public static bool GetSafeBool(this SqlDataReader r, string col) =>
                r[col] != DBNull.Value && Convert.ToBoolean(r[col]);

            public static DateTime? GetSafeDate(this SqlDataReader r, string col) =>
                r[col] == DBNull.Value ? null : Convert.ToDateTime(r[col]);
        }

  



}
