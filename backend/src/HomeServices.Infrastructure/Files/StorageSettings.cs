namespace HomeServices.Infrastructure.Files;

/// <summary>S3-compatible object storage (configuration section "Storage").</summary>
public sealed class StorageSettings
{
    public const string SectionName = "Storage";

    /// <summary>Where the API reaches the storage, e.g. "http://localhost:8333" or "https://s3.eu-central-1.amazonaws.com".</summary>
    public string ServiceUrl { get; set; } = string.Empty;

    /// <summary>
    /// Where browsers reach the storage, if different from <see cref="ServiceUrl"/> (e.g. the API talks
    /// to "http://seaweedfs:8333" inside Docker, browsers use "https://files.example.am"). Signed links use this address.
    /// </summary>
    public string? PublicServiceUrl { get; set; }

    public string Bucket { get; set; } = "home-services";

    public string Region { get; set; } = "us-east-1";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    /// <summary>"host/bucket/key" addresses instead of "bucket.host/key". Needed for SeaweedFS and MinIO.</summary>
    public bool ForcePathStyle { get; set; } = true;

    /// <summary>Create the bucket when the API starts (development).</summary>
    public bool CreateBucket { get; set; }

    /// <summary>
    /// Sites allowed to upload and download straight from the browser (e.g. "https://staging.tnashen.am"). When set,
    /// the bucket's CORS rules are replaced with these when the API starts; empty leaves the bucket's rules alone.
    /// </summary>
    public string[] CorsOrigins { get; set; } = [];
}
