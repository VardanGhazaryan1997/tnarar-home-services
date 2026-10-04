using System.Net;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using HomeServices.Application.Files;
using Microsoft.Extensions.Options;

namespace HomeServices.Infrastructure.Files;

/// <summary><see cref="IFileStorage"/> on any S3-compatible service (SeaweedFS in development).</summary>
public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly StorageSettings _settings;
    private readonly AmazonS3Client _client;

    // Signed links are computed locally (no request is sent); they must name the address browsers use.
    private readonly AmazonS3Client _signer;
    private readonly Protocol _publicProtocol;

    public S3FileStorage(IOptions<StorageSettings> options)
    {
        _settings = options.Value;
        var credentials = new BasicAWSCredentials(_settings.AccessKey, _settings.SecretKey);
        var publicUrl = string.IsNullOrWhiteSpace(_settings.PublicServiceUrl) ? _settings.ServiceUrl : _settings.PublicServiceUrl;

        _client = new AmazonS3Client(credentials, Config(_settings.ServiceUrl));
        _signer = new AmazonS3Client(credentials, Config(publicUrl));
        _publicProtocol = new Uri(publicUrl).Scheme == Uri.UriSchemeHttp ? Protocol.HTTP : Protocol.HTTPS;
    }

    public async Task<Uri> CreateUploadUrlAsync(string key, string contentType, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var url = await _signer.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _settings.Bucket,
            Key = key,
            Verb = HttpVerb.PUT,
            ContentType = contentType,
            Expires = expiresAt.UtcDateTime,
            Protocol = _publicProtocol,
        });
        return new Uri(url);
    }

    public async Task<Uri> CreateDownloadUrlAsync(string key, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var url = await _signer.GetPreSignedURLAsync(new GetPreSignedUrlRequest
        {
            BucketName = _settings.Bucket,
            Key = key,
            Verb = HttpVerb.GET,
            Expires = expiresAt.UtcDateTime,
            Protocol = _publicProtocol,
        });
        return new Uri(url);
    }

    public async Task<StoredObjectInfo?> GetInfoAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _client.GetObjectMetadataAsync(_settings.Bucket, key, cancellationToken);
            return new StoredObjectInfo(metadata.Headers.ContentLength, metadata.Headers.ContentType);
        }
        catch (AmazonS3Exception e) when (e.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }
    }

    public async Task<byte[]> ReadStartAsync(string key, int count, CancellationToken cancellationToken)
    {
        using var response = await _client.GetObjectAsync(
            new GetObjectRequest { BucketName = _settings.Bucket, Key = key, ByteRange = new ByteRange(0, count - 1) },
            cancellationToken);
        using var buffer = new MemoryStream();
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);

        // A server that ignores the range sends the whole object.
        var bytes = buffer.ToArray();
        return bytes.Length > count ? bytes[..count] : bytes;
    }

    // Only images (at most a few MB) are read whole, so a memory copy is fine and frees the connection at once.
    public async Task<Stream> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        using var response = await _client.GetObjectAsync(_settings.Bucket, key, cancellationToken);
        var copy = new MemoryStream();
        await response.ResponseStream.CopyToAsync(copy, cancellationToken);
        copy.Position = 0;
        return copy;
    }

    public Task PutAsync(string key, Stream content, string contentType, CancellationToken cancellationToken) =>
        _client.PutObjectAsync(
            new PutObjectRequest
            {
                BucketName = _settings.Bucket,
                Key = key,
                InputStream = content,
                ContentType = contentType,
                AutoCloseStream = false,
                // Plain signed body instead of "aws-chunked" uploads, which not every S3-compatible service accepts.
                UseChunkEncoding = false,
            },
            cancellationToken);

    public Task DeleteAsync(string key, CancellationToken cancellationToken) =>
        _client.DeleteObjectAsync(_settings.Bucket, key, cancellationToken);

    public async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        if (_settings.CreateBucket)
        {
            try
            {
                await _client.PutBucketAsync(new PutBucketRequest { BucketName = _settings.Bucket }, cancellationToken);
            }
            catch (AmazonS3Exception e) when (e.ErrorCode is "BucketAlreadyOwnedByYou" or "BucketAlreadyExists")
            {
                // Already there.
            }
        }

        if (_settings.CorsOrigins.Length > 0)
        {
            await _client.PutCORSConfigurationAsync(
                new PutCORSConfigurationRequest { BucketName = _settings.Bucket, Configuration = BrowserCors(_settings.CorsOrigins) },
                cancellationToken);
        }
    }

    /// <summary>
    /// Lets these sites PUT uploads to signed links and GET downloads from the browser (the Portal and Back Office
    /// talk to the storage directly, on another domain).
    /// </summary>
    public static CORSConfiguration BrowserCors(IEnumerable<string> origins) => new()
    {
        Rules =
        [
            new CORSRule
            {
                AllowedOrigins = [.. origins.Select(o => o.Trim().TrimEnd('/')).Where(o => o.Length > 0)],
                AllowedMethods = ["GET", "PUT", "HEAD"],
                AllowedHeaders = ["*"],
                ExposeHeaders = ["ETag"],
                MaxAgeSeconds = 3600,
            },
        ],
    };

    public void Dispose()
    {
        _client.Dispose();
        _signer.Dispose();
    }

    private AmazonS3Config Config(string serviceUrl) => new()
    {
        ServiceURL = serviceUrl,
        AuthenticationRegion = _settings.Region,
        ForcePathStyle = _settings.ForcePathStyle,
        // Newer SDKs add checksums that some S3-compatible services don't support; send them only when required.
        RequestChecksumCalculation = RequestChecksumCalculation.WHEN_REQUIRED,
        ResponseChecksumValidation = ResponseChecksumValidation.WHEN_REQUIRED,
        MaxErrorRetry = 2,
    };
}
