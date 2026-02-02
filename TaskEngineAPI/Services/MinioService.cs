using TaskEngineAPI.Services;
using Microsoft.AspNetCore.Http;
using Minio;
using Minio.DataModel.Args;
using TaskEngineAPI.Interfaces;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
namespace TaskEngineAPI.Services;

public class MinioService : IMinioService
{
    private readonly IMinioClient _minio;
    private readonly string _bucketName;

    public MinioService(IConfiguration config)
    {
        _bucketName = config["Minio:BucketName"];

        _minio = new MinioClient()
            .WithEndpoint(config["Minio:Endpoint"])
 .WithCredentials(
                config["Minio:AccessKey"],
                config["Minio:SecretKey"]
            )
            .WithSSL(bool.Parse(config["Minio:UseSSL"]))
            .Build();
    }

    public async Task UploadFileAsync(IFormFile form)
    {
        try
        {
            // 1️⃣ Validate file
            if (form == null)
                throw new ArgumentException("File not found");

            if (form.Length == 0)
                throw new ArgumentException("Uploaded file is empty");

            if (string.IsNullOrWhiteSpace(form.FileName))
                throw new ArgumentException("Invalid file name");

            // 2️⃣ Ensure bucket exists
            bool bucketExists = await _minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName)
            );

            if (!bucketExists)
            {
                await _minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName)
                );
            }

            // 3️⃣ Get safe file name
            var fileName = Path.GetFileName(form.FileName);

            // 4️⃣ Decide object path (NO default folder)
            string objectName;

            if (fileName.Contains("~"))
            {
                var folderName = fileName.Split('~')[0];
                objectName = $"{folderName}/{fileName}";
            }
            else
            {
                objectName = fileName; // root of bucket
            }

            // 5️⃣ Upload
            using var stream = form.OpenReadStream();

            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(form.Length)
                    .WithContentType(form.ContentType ?? "application/octet-stream")
            );
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            throw new Exception("Upload failed: object not found");
        }
        catch (Minio.Exceptions.MinioException ex)
        {
            // MinIO specific issues
            throw new Exception($"MinIO error: {ex.Message}");
        }
        catch (IOException)
        {
            throw new Exception("File stream error while uploading");
        }
        catch (ArgumentException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            // Catch-all fallback
            throw new Exception($"Unexpected error during upload: {ex.Message}");
        }
    }
    public async Task<(MemoryStream stream, string contentType)> GetFileAsync(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);

        var folderName = safeFileName.Contains("~")
            ? safeFileName.Split('~')[0]
            : null;

        string objectName = folderName != null
            ? $"{folderName}/{safeFileName}"
            : safeFileName;

        var memoryStream = new MemoryStream();

        try
        {
            // 🔹 1st attempt: folder-based path
            await _minio.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(memoryStream);
                    })
            );
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            // 🔹 2nd attempt: root-level file
            memoryStream = new MemoryStream();

            await _minio.GetObjectAsync(
                new GetObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(safeFileName)
                    .WithCallbackStream(stream =>
                    {
                        stream.CopyTo(memoryStream);
                    })
            );
        }

        memoryStream.Position = 0;

        var contentType = Path.GetExtension(safeFileName).ToLower() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };

        return (memoryStream, contentType);
    }
    public async Task FileUploadFileAsync(IFormFile form,string type,int ctenantid)
    {
        try
        {       
            if (form == null)
                throw new ArgumentException("File not found");
            if (form.Length == 0)
                throw new ArgumentException("Uploaded file is empty");
            if (string.IsNullOrWhiteSpace(form.FileName))
                throw new ArgumentException("Invalid file name");
            bool bucketExists = await _minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName)
            );
            if (!bucketExists)
            {
                await _minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName)
                );
            }
            var fileName = Path.GetFileName(form.FileName);
            string objectName;
            var folderName = type;
            objectName = $"{ctenantid}/{folderName}/{fileName}";
            using var stream = form.OpenReadStream();

            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(form.Length)
                    .WithContentType(form.ContentType ?? "application/octet-stream")
            );
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            throw new Exception("Upload failed: object not found");
        }
        catch (Minio.Exceptions.MinioException ex)
        {
            // MinIO specific issues
            throw new Exception($"MinIO error: {ex.Message}");
        }
        catch (IOException)
        {
            throw new Exception("File stream error while uploading");
        }
        catch (ArgumentException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {         
            throw new Exception($"Unexpected error during upload: {ex.Message}");
        }
    }
    public async Task<(MemoryStream stream, string contentType)>
    GetuserFileAsync(string fileName, string type, int ctenantid)
    {
        var safeFileName = Path.GetFileName(fileName);
        var safeType = Path.GetFileName(type);
        string objectName = $"{ctenantid}/{safeType}/{safeFileName}";

        var memoryStream = new MemoryStream();

        await _minio.GetObjectAsync(
            new GetObjectArgs()
                .WithBucket(_bucketName)     
                .WithObject(objectName)           
                .WithCallbackStream(stream =>
                {
                    stream.CopyTo(memoryStream);
                })
        );

        memoryStream.Position = 0;

        var contentType = Path.GetExtension(safeFileName).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".pdf" => "application/pdf",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "application/octet-stream"
        };

        return (memoryStream, contentType);
    }

    public async Task TaskFileUploadFileAsync(IFormFile form, string type, int ctenantid)
    {
        try
        {
            if (form == null)
                throw new ArgumentException("File not found");

            if (form.Length == 0)
                throw new ArgumentException("Uploaded file is empty");

            if (string.IsNullOrWhiteSpace(form.FileName))
                throw new ArgumentException("Invalid file name");

            bool bucketExists = await _minio.BucketExistsAsync(
                new BucketExistsArgs().WithBucket(_bucketName)
            );

            if (!bucketExists)
            {
                await _minio.MakeBucketAsync(
                    new MakeBucketArgs().WithBucket(_bucketName)
                );
            }

            var fileName = Path.GetFileName(form.FileName);
            
            string objectName;
            //if (string.IsNullOrWhiteSpace(type))            
            //var folderName = "Task";
            var folderName = type;
            objectName = $"{ctenantid}/{folderName}/{fileName}";
            using var stream = form.OpenReadStream();

            await _minio.PutObjectAsync(
                new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(objectName)
                    .WithStreamData(stream)
                    .WithObjectSize(form.Length)
                    .WithContentType(form.ContentType ?? "application/octet-stream")
            );
        }
        catch (Minio.Exceptions.ObjectNotFoundException)
        {
            throw new Exception("Upload failed: object not found");
        }
        catch (Minio.Exceptions.MinioException ex)
        {
            throw new Exception($"MinIO error: {ex.Message}");
        }
        catch (IOException)
        {
            throw new Exception("File stream error while uploading");
        }
        catch (ArgumentException ex)
        {
            throw new Exception(ex.Message);
        }
        catch (Exception ex)
        {
            throw new Exception($"Unexpected error during upload: {ex.Message}");
        }
    }


}