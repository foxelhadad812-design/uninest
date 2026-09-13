using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniNest.Domain;
using UniNest.Infrastructure;

namespace UniNest.Api.Controllers;

[ApiController]
[Route("api/v1/media")]
public sealed class MediaController(IWebHostEnvironment environment, UniNestDbContext db) : ControllerBase
{
    // Allowed image types with their magic byte signatures
    private static readonly Dictionary<string, byte[]> AllowedMagicBytes = new()
    {
        { ".jpg",  new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".jpeg", new byte[] { 0xFF, 0xD8, 0xFF } },
        { ".png",  new byte[] { 0x89, 0x50, 0x4E, 0x47 } },
        { ".webp", new byte[] { 0x52, 0x49, 0x46, 0x46 } },  // "RIFF" header
    };

    private static readonly Dictionary<string, string> ExtensionToMimeType = new()
    {
        { ".jpg",  "image/jpeg" },
        { ".jpeg", "image/jpeg" },
        { ".png",  "image/png" },
        { ".webp", "image/webp" },
    };

    private const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary>
    /// Upload an image file. Requires authentication.
    /// Accepts JPG, PNG, and WebP only. Maximum 5 MB.
    /// File content is verified via magic bytes — renaming a non-image to .jpg is rejected.
    /// Creates a MediaAsset record in the database.
    /// </summary>
    [Authorize]
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadImage([FromForm] UploadImageRequest request, CancellationToken cancellationToken)
    {
        var file = request.File;
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded or file is empty." });

        // 1. Enforce size limit
        if (file.Length > MaxFileSizeBytes)
            return BadRequest(new { message = $"File size must not exceed {MaxFileSizeBytes / 1024 / 1024} MB." });

        // 2. Check file extension
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedMagicBytes.TryGetValue(extension, out var expectedMagic))
            return BadRequest(new { message = "Invalid file extension. Only JPG, PNG, and WebP images are allowed." });

        // 3. Verify actual file content via magic bytes
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, cancellationToken);
        ms.Seek(0, SeekOrigin.Begin);

        var header = new byte[8];
        _ = await ms.ReadAsync(header, cancellationToken);
        if (!header.Take(expectedMagic.Length).SequenceEqual(expectedMagic))
            return BadRequest(new { message = "File content does not match its declared extension. Upload rejected." });

        // Calculate SHA256 checksum
        ms.Seek(0, SeekOrigin.Begin);
        var checksumBytes = await SHA256.HashDataAsync(ms, cancellationToken);
        var checksumHex = Convert.ToHexString(checksumBytes).ToLowerInvariant();

        // 4. Save file to disk
        var uploadsFolder = Path.Combine(
            environment.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot"),
            "uploads");

        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var fileName = $"{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsFolder, fileName);

        ms.Seek(0, SeekOrigin.Begin);
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await ms.CopyToAsync(stream, cancellationToken);
        }

        var requestHost = $"{Request.Scheme}://{Request.Host}";
        var fileUrl = $"{requestHost}/uploads/{fileName}";
        var storageKey = $"uploads/{fileName}";

        // Extract current user ID
        Guid? userId = GetUserId();

        // Save MediaAsset entity to database
        var mediaAsset = new MediaAsset
        {
            Id = Guid.NewGuid(),
            UploadedByUserId = userId,
            StorageKey = storageKey,
            OriginalFileName = file.FileName,
            ContentType = ExtensionToMimeType[extension],
            ByteSize = file.Length,
            ChecksumSha256 = checksumHex,
            ScanStatus = DocumentScanStatus.Clean, // Enforced default: Clean (2)
            CreatedAt = DateTimeOffset.UtcNow
        };

        db.MediaAssets.Add(mediaAsset);
        await db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            id = mediaAsset.Id,
            url = fileUrl,
            fileName = fileName,
            size = file.Length,
            contentType = ExtensionToMimeType[extension]
        });
    }

    private Guid? GetUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}

public class UploadImageRequest
{
    public required IFormFile File { get; set; }
}
