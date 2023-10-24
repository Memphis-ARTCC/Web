using Amazon.S3;
using Amazon.S3.Model;

namespace Memphis.API.Services;

public class S3Service
{
    private readonly AmazonS3Client _client = new(
        Environment.GetEnvironmentVariable("S3_KEY") ??
        throw new ArgumentNullException("S3_KEY env variable not found"),
        Environment.GetEnvironmentVariable("S3_SECRET") ??
        throw new ArgumentNullException("S3_SECRET env variable not found"),
        new AmazonS3Config
        {
            ServiceURL = Environment.GetEnvironmentVariable("S3_SERVICE_URL") ??
                         throw new ArgumentNullException("S3_SERVICE_URL env variable not found")
        });

    public async Task<string?> UploadFile(HttpRequest request, string type)
    {
        if (!request.Form.Files.Any())
            return null;
        var file = request.Form.Files.First();
        var reader = new StreamReader(file.OpenReadStream());

        var putRequest = new PutObjectRequest
        {
            BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ??
                         throw new ArgumentNullException("S3_BUCKET_NAME env variable not found"),
            Key = $"{type}/{file.FileName.Replace(" ", "-")}",
            ContentBody = await reader.ReadToEndAsync(),
            CannedACL = S3CannedACL.PublicRead
        };
        await _client.PutObjectAsync(putRequest);

        var url = Environment.GetEnvironmentVariable("S3_URL") ??
                  throw new ArgumentNullException("S3_URL env variable not found");
        return $"{url}/{type}/{file.FileName.Replace(" ", "-")}";
    }

    public async Task DeleteFile(string url)
    {
        var bucketUrl = Environment.GetEnvironmentVariable("S3_URL") ??
                        throw new ArgumentNullException("S3_URL env variable not found");
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ??
                         throw new ArgumentNullException("S3_BUCKET_NAME env variable not found"),
            Key = url.Replace(bucketUrl, string.Empty),
        };
        await _client.DeleteObjectAsync(deleteRequest);
    }
}