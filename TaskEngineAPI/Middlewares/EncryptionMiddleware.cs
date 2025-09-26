using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using System.Threading.Tasks;

public class EncryptionMiddleware
{
    private readonly RequestDelegate _next;

    public EncryptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
       
        context.Request.EnableBuffering(); // allows reading multiple times
        string requestBody = string.Empty;
        using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, leaveOpen: true))
        {
            requestBody = await reader.ReadToEndAsync();
            context.Request.Body.Position = 0;
        }

        Console.WriteLine($"➡️ Request Path: {context.Request.Path}");
        Console.WriteLine($"➡️ Request Body: {requestBody}");

     
        var originalBodyStream = context.Response.Body;
        using var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        await _next(context); // continue down the pipeline

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        string responseText = await new StreamReader(context.Response.Body).ReadToEndAsync();
        context.Response.Body.Seek(0, SeekOrigin.Begin);

        Console.WriteLine($"⬅️ Response Status: {context.Response.StatusCode}");
        Console.WriteLine($"⬅️ Response Body: {responseText}");
    
        await responseBody.CopyToAsync(originalBodyStream);
    }
}
