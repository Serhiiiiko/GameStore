using GameStore.Interfaces;
using Minio;
using Minio.DataModel.Args;
using Microsoft.AspNetCore.Http;

namespace GameStore.Services
{
    public class MinioStorageService : IFileService
    {
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MinioStorageService> _logger;
        private readonly ITelegramNotificationService _telegramService;
        private readonly string _bucketName;
        private readonly string _publicUrl;

        public MinioStorageService(
            IConfiguration configuration,
            ILogger<MinioStorageService> logger,
            ITelegramNotificationService telegramService)
        {
            _configuration = configuration;
            _logger = logger;
            _telegramService = telegramService;
            _bucketName = _configuration["MinIO:BucketName"] ?? "gamestore-images";
            _publicUrl = _configuration["MinIO:PublicUrl"] ?? "http://localhost:9000";

            var endpoint = _configuration["MinIO:Endpoint"] ?? "localhost:9000";
            var accessKey = _configuration["MinIO:AccessKey"] ?? "minioadmin";
            var secretKey = _configuration["MinIO:SecretKey"] ?? "minioadmin";
            var useSSL = bool.Parse(_configuration["MinIO:UseSSL"] ?? "false");

            var client = new MinioClient()
                .WithEndpoint(endpoint)
                .WithCredentials(accessKey, secretKey);

            if (useSSL)
            {
                client = client.WithSSL();
            }

            _minioClient = client.Build();

            // Ensure bucket exists
            Task.Run(() => EnsureBucketExistsAsync());
        }

        private async Task EnsureBucketExistsAsync()
        {
            try
            {
                var bucketExistsArgs = new BucketExistsArgs()
                    .WithBucket(_bucketName);

                bool found = await _minioClient.BucketExistsAsync(bucketExistsArgs);

                if (!found)
                {
                    var makeBucketArgs = new MakeBucketArgs()
                        .WithBucket(_bucketName);
                    await _minioClient.MakeBucketAsync(makeBucketArgs);

                    // Set bucket policy to allow public read
                    var policyJson = $@"{{
                        ""Version"": ""2012-10-17"",
                        ""Statement"": [
                            {{
                                ""Effect"": ""Allow"",
                                ""Principal"": {{""AWS"": [""*""]}},
                                ""Action"": [""s3:GetObject""],
                                ""Resource"": [""arn:aws:s3:::{_bucketName}/*""]
                            }}
                        ]
                    }}";

                    var setPolicyArgs = new SetPolicyArgs()
                        .WithBucket(_bucketName)
                        .WithPolicy(policyJson);
                    await _minioClient.SetPolicyAsync(setPolicyArgs);

                    _logger.LogInformation($"Created MinIO bucket: {_bucketName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure MinIO bucket exists");
                await _telegramService.SendStorageAlertAsync(
                    $"Failed to create/verify MinIO bucket: {ex.Message}");
            }
        }

        public async Task<string> SaveImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return null;

            // Validate file type
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension) || !IsImageExtension(extension))
                throw new ArgumentException("Uploaded file is not an image");

            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{extension}";
            var contentType = GetContentType(extension);

            try
            {
                using var stream = file.OpenReadStream();

                var putObjectArgs = new PutObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName)
                    .WithStreamData(stream)
                    .WithObjectSize(file.Length)
                    .WithContentType(contentType);

                await _minioClient.PutObjectAsync(putObjectArgs);

                // Return the public URL
                var imageUrl = $"{_publicUrl}/{_bucketName}/{fileName}";
                _logger.LogInformation($"Image uploaded successfully: {imageUrl}");

                return imageUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to upload image: {fileName}");
                await _telegramService.SendStorageAlertAsync(
                    $"Failed to upload image: {fileName}\nError: {ex.Message}");
                throw;
            }
        }

        public void DeleteImage(string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
                return;

            Task.Run(async () => await DeleteImageAsync(fileUrl));
        }

        private async Task DeleteImageAsync(string fileUrl)
        {
            try
            {
                // Extract filename from URL
                var uri = new Uri(fileUrl);
                var segments = uri.Segments;
                if (segments.Length < 2)
                    return;

                var fileName = segments[segments.Length - 1];

                var removeObjectArgs = new RemoveObjectArgs()
                    .WithBucket(_bucketName)
                    .WithObject(fileName);

                await _minioClient.RemoveObjectAsync(removeObjectArgs);
                _logger.LogInformation($"Image deleted successfully: {fileName}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to delete image: {fileUrl}");
                // Don't send telegram notification for delete failures
            }
        }

        private bool IsImageExtension(string extension)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".bmp" };
            return allowedExtensions.Contains(extension.ToLowerInvariant());
        }

        private string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".bmp" => "image/bmp",
                _ => "application/octet-stream"
            };
        }
    }
}