using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace TaskEngineAPI.Helpers
{
    public class EncryptionHelper
    {


        //private ActionResult EncryptedError(int status, string message)
        //    {
        //        var response = new APIResponse { status = status, statusText = message };
        //        string json = JsonConvert.SerializeObject(response);
        //        string encrypted = AesEncryption.Encrypt(json);
        //        return Ok(encrypted);
        //    }
        //}

        // private ActionResult EncryptedSuccess(string message)
        //    {
        //        var response = new APIResponse { status = 200, statusText = message };
        //        string json = JsonConvert.SerializeObject(response);
        //        string encrypted = AesEncryption.Encrypt(json);
        //        return Ok(encrypted);
        //    }
    }
}