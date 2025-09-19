using System.Security.Cryptography;
using System.Text;

namespace TaskEngineAPI.Helpers
{

    public static class AesEncryption
    {
        private static readonly byte[] IV = Encoding.UTF8.GetBytes("ABCDEFGH12345678");  // 16 bytes IV
        private static readonly byte[] ENCKey = Encoding.UTF8.GetBytes("#@WORKFLOW!#%!#%$%^&KEY*&%#(@*!#");
        private static readonly byte[] DECKey = Encoding.UTF8.GetBytes("#@MISPORTAL2025!%^$#$123456789@#");

        public static string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = ENCKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream();
                using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                using (var sw = new StreamWriter(cs))
                {
                    sw.Write(plainText);
                }
                return Convert.ToBase64String(ms.ToArray());
            }
        }

        public static string Decrypt(string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);
            using (Aes aes = Aes.Create())
            {
                aes.Key = DECKey;
                aes.IV = IV;
                aes.Padding = PaddingMode.PKCS7;

                using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
                using var ms = new MemoryStream(buffer);
                using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
                using var sr = new StreamReader(cs);
                return sr.ReadToEnd();
            }
        }
    }

 

}


