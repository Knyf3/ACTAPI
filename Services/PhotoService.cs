using ACTApi.Infrastructure;
using ACTServiceReference;

namespace ACTApi.Services;

/// <summary>Domain service for ACT user photo chunked read/write.</summary>
public class PhotoService : IPhotoService
{
    private readonly ILogger<PhotoService> _logger;

    /// <summary>Initializes a new instance of <see cref="PhotoService"/>.</summary>
    public PhotoService(ILogger<PhotoService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<byte[]?> GetUserPhotoAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        var chunkSize = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetChunkSizeAsync(),
            "GetChunkSize",
            _logger);

        if (chunkSize <= 0)
            chunkSize = 64512; // default ACT chunk size

        using var ms = new MemoryStream();
        int chunk = 0;
        bool isLastChunk;

        do
        {
            var request = new GetUserPhotoChunkRequest(userNumber, chunk, false);
            var response = await WcfCallLogger.ExecuteAsync(
                () => proxy.GetUserPhotoChunkAsync(request),
                "GetUserPhotoChunk",
                _logger);

            if (response.GetUserPhotoChunkResult is { Length: > 0 })
                ms.Write(response.GetUserPhotoChunkResult, 0, response.GetUserPhotoChunkResult.Length);

            chunk = response.chunk;
            isLastChunk = response.isLastChunk;
        } while (!isLastChunk);

        var photoData = ms.ToArray();
        _logger.LogDebug(
            "Retrieved photo for user {UserNumber}: {Size} bytes",
            userNumber, photoData.Length);

        return photoData.Length > 0 ? photoData : null;
    }

    /// <inheritdoc />
    public async Task<bool> SetUserPhotoAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber, byte[] photoData)
    {
        if (photoData == null || photoData.Length == 0)
            throw new ArgumentException("Photo data is required.", nameof(photoData));

        var chunkSize = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetChunkSizeAsync(),
            "GetChunkSize",
            _logger);

        if (chunkSize <= 0)
            chunkSize = 64512;

        int offset = 0;
        int totalChunks = (int)Math.Ceiling((double)photoData.Length / chunkSize);

        for (int i = 0; i < totalChunks; i++)
        {
            int bytesToWrite = Math.Min(chunkSize, photoData.Length - offset);
            var chunk = new byte[bytesToWrite];
            Array.Copy(photoData, offset, chunk, 0, bytesToWrite);
            bool lastChunk = i == totalChunks - 1;

            var result = await WcfCallLogger.ExecuteAsync(
                () => proxy.InsertUserPhotoChunkAsync(userNumber, chunk, lastChunk),
                "InsertUserPhotoChunk",
                _logger);

            if (!result)
            {
                _logger.LogWarning(
                    "Failed to write photo chunk {Chunk}/{TotalChunks} for user {UserNumber}",
                    i + 1, totalChunks, userNumber);
                return false;
            }

            offset += bytesToWrite;
        }

        _logger.LogInformation(
            "Uploaded photo for user {UserNumber}: {Size} bytes in {Chunks} chunks",
            userNumber, photoData.Length, totalChunks);

        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteUserPhotoAsync(ActEnterprisePublicAPI_ExtClient proxy, int userNumber)
    {
        // Overwrite with a single empty chunk to clear the photo
        var chunkSize = await WcfCallLogger.ExecuteAsync(
            () => proxy.GetChunkSizeAsync(),
            "GetChunkSize",
            _logger);

        if (chunkSize <= 0)
            chunkSize = 64512;

        var result = await WcfCallLogger.ExecuteAsync(
            () => proxy.InsertUserPhotoChunkAsync(userNumber, Array.Empty<byte>(), true),
            "InsertUserPhotoChunk",
            _logger);

        _logger.LogInformation(
            "Deleted photo for user {UserNumber} — result: {Result}",
            userNumber, result);

        return result;
    }
}
