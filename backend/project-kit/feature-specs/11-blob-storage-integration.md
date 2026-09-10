# Backend Feature 11 — Blob Storage Integration (Cloudinary → R2 Migration Path)

> Production-ready attachment storage with Cloudinary via `CloudinaryDotNet` (v1) and migration path to Cloudflare R2 (production scale). Replaces local/disk storage with cloud-native blob store + CDN delivery.

**Status:** Planning  
**Priority:** **CRITICAL** (production blocker — must ship before public launch)  
**Effort:** 2–3 days  
**Depends on:** Features 01 (ERD), 02 (EF Core), 04 (REST APIs)  
**Blocks:** Public launch, mobile file uploads

---

## Dependencies

- Feature 01 (approved ERD: `Attachments` entity + `StorageUrl`).
- Feature 02 (EF Core schema: `Attachments` table in the migration).
- Feature 04 (REST routes + service layer to attach upload/download to).
- **Not gated by Features 07–10** — blob storage needs only 01/02/04 and may be implemented in parallel with 08/07/09/10. Phase-1 production blocker per `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` (ships before public launch).

## Context To Read First

- `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` (blob storage = Phase 1)
- `docs/planning/RISK-REGISTER.md` (attachment/Railway-ephemeral-storage risk)

---

## 1. Problem Statement

### Current state (v1 local/disk — NOT production-ready)
- `Attachments` table stores metadata (`Id`, `TaskId`, `UploaderId`, `FileName`, `MimeType`, `SizeBytes`, `StorageUrl`)
- `StorageUrl` field exists but **v1 implementation is local/disk only** (Railway ephemeral filesystem)
- **Critical gaps:**
  1. No file size limits documented or enforced
  2. No blob storage integration
  3. Breaks at scale: Railway restarts wipe uploaded files
  4. No CDN support (slow downloads, no caching, no edge delivery)
  5. No per-workspace quota tracking

### Why this blocks production
- Railway containers are ephemeral — file uploads don't survive restarts
- No size caps = abuse vector (users upload 5 GB files → API OOM)
- No CDN = every download hits API → p99 latency spikes + bandwidth costs
- Attachments are a **core feature** (design screens include file previews) — can't defer

---

## 2. Solution Architecture

### 2.1 Blob storage decision matrix

| Solution | Storage | Egress | Integration | v1 fit? |
|---|---|---|---|---|
| **Cloudinary** | ~25 free credits (≈25 GB storage + 25 GB bandwidth/mo), then ~$0.25/credit-class overage | CDN bandwidth included in credits | `CloudinaryDotNet` NuGet (.NET-native), 1-day setup | ✅ **v1** |
| **Cloudflare R2** | $0.015/GB | **$0 (FREE)** | S3-compatible API, 1-day migration | ✅ **v2** |
| AWS S3 | $0.023/GB | $0.09/GB (6× R2) | Mature ecosystem | ❌ expensive egress |
| Azure Blob | $0.023/GB | $0.087/GB | .NET native SDK | ❌ similar to S3 |

**Decision:**  
1. **Ship v1 with Cloudinary on the free tier** (~25 credits/month: 25 GB storage + 25 GB bandwidth — no additional cost; .NET-native `CloudinaryDotNet` SDK, no HTTP hand-rolling)
2. **Monitor credit/egress usage** — Cloudinary dashboard tracks storage + bandwidth; set alert at 20 GB/month bandwidth
3. **Migrate to Cloudflare R2 for production scale** when egress consistently exceeds 100 GB/month (cost savings: ~$50/month per TB egress vs Cloudinary's credit-class overage pricing)

**Rationale:** Free tier unblocks launch immediately with zero infrastructure cost and a first-class .NET SDK (`CloudinaryDotNet` — no REST-replication layer). R2 migration is a config swap (S3-compatible API) when scale justifies migration effort (>2 TB/month transfer = >$100/month egress costs vs $0 R2 egress).

### 2.2 Attachment flow (v1 — Cloudinary)

```
┌────────────┐  POST /api/tasks/{id}/attachments   ┌────────────────┐
│   Web /    │  (multipart/form-data)             │  ASP.NET API   │
│   Mobile   ├─────────────────────────────────────►│ AttachmentCtrl │
└────────────┘                                      │                │
                                                     │  ↓ validate    │
                                                     │    size/type   │
                                                     │  ↓ upload      │
                                                     │ CloudinaryDotNet│
                ┌────────────────────────────────────┤                │
                │  Cloudinary (CDN-backed)          │  ↓ save meta   │
                │  - public URL returned            │    to SQL      │
                │  - CDN-cached (res.cloudinary.com)└────────────────┘
                │  - signed delivery optional (Phase 3)
                └────────────────────────────────────
                       │
                       ▼ public URL
                 ┌───────────────┐
                 │ Browser/Mobile│ (direct download, no API proxy)
                 └───────────────┘
```

**Key changes from local/disk:**
- `StorageUrl` now holds public Cloudinary URL (e.g. `https://res.cloudinary.com/<cloud>/image/upload/v<ver>/griot/attachments/abc123.jpg`)
- Uploads go through API → `CloudinaryDotNet` SDK → Cloudinary media library
- Downloads bypass API (client fetches the Cloudinary URL directly → CDN-cached)

### 2.3 Migration path (v1 → v2: Cloudinary → Cloudflare R2)

When egress costs justify migration (>100 GB/month):

1. **Swap SDK:**
   ```diff
   - var cloudinary = new Cloudinary(account);           // CloudinaryDotNet
   - var uploadResult = await cloudinary.UploadAsync(new ImageUploadParams { ... });
   + using var s3 = new AmazonS3Client(new AmazonS3Config
   +     { ServiceURL = "https://<account-id>.r2.cloudflarestorage.com" });   // AWSSDK.S3
   + await s3.PutObjectAsync(new PutObjectRequest { BucketName = "griot-attachments", ... });
   ```

2. **Update StorageUrl semantics:**
   - Cloudinary: `https://res.cloudinary.com/<cloud-name>/*`
   - R2: `https://r2.griot.io/*` (custom domain via Cloudflare Workers)

3. **Migrate existing blobs:**
   - One-time script: `SELECT Id, StorageUrl FROM Attachments` → download from Cloudinary → upload to R2 → `UPDATE StorageUrl`
   - Cutover: update `BLOB_STORAGE_PROVIDER` env var → new uploads go to R2

**Contract:** the attachment path stays the same (`AttachmentController` -> `IDomainService.CreateAttachmentAsync`, metadata @ `Attachments`); only the provider implementation changes (spec 17 ships metadata-only).

---

## 3. Implementation Details

### 3.1 Package dependencies

Add to `backend/Griot.Infrastructure/Griot.Infrastructure.csproj` (or `Griot.Api.csproj` if `Infrastructure` does not reference it):

```xml
<PackageReference Include="CloudinaryDotNet" Version="1.26.2" />
```

**Decision:** Use the **`CloudinaryDotNet`** SDK — first-class .NET library (account-based auth via `Account`, typed `UploadAsync`/`DestroyAsync` params/results, automatic request signing). No hand-rolled REST replication.

**Core SDK behavior (documented):**
- **Upload:** `cloudinary.UploadAsync(new RawUploadParams { File = new FileDescription(fileName, stream) })` → `RawUploadResult.SecureUrl` (public CDN URL)
- **Delete:** `cloudinary.DestroyAsync(new DeletionParams(publicId))` → `Result == "ok"`; treat "not found" as success (idempotent deletes)
- **Auth:** `Account(cloudName, apiKey, apiSecret)` built from `CLOUDINARY_URL` or the discrete env vars in §3.2 — request signing (SHA-1 over params + secret) is handled inside the SDK

**Future (R2 migration):** Add `AWSSDK.S3` NuGet package for S3-compatible API.

### 3.2 Configuration

**appsettings.json:**
```json
{
  "BlobStorage": {
    "Provider": "Cloudinary",  // or "CloudflareR2" post-migration
    "Cloudinary": {
      "CloudName": "",          // resolved from CLOUDINARY_CLOUD_NAME (or CLOUDINARY_URL)
      "ApiKey": "",             // resolved from CLOUDINARY_API_KEY
      "ApiSecret": "",          // resolved from CLOUDINARY_API_SECRET (server-only)
      "Folder": "griot/attachments"
    },
    "MaxFileSizeBytes": 26214400,  // 25 MB per file
    "MaxWorkspaceQuotaBytes": 104857600,  // 100 MB per workspace (v1)
    "AllowedMimeTypes": [
      "image/jpeg", "image/png", "image/gif", "image/webp",
      "application/pdf",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document",  // .docx
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"  // .xlsx
    ],
    "BlockedExtensions": [".exe", ".dll", ".bat", ".sh", ".ps1"]
  }
}
```

**Railway/env vars:**
```env
# Option A — single URL (SDK-native format):
CLOUDINARY_URL=cloudinary://<api_key>:<api_secret>@<cloud_name>
# Option B — discrete vars (preferred for Railway; never commit the secret):
CLOUDINARY_CLOUD_NAME=griot
CLOUDINARY_API_KEY=123456789012345
CLOUDINARY_API_SECRET=xxxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

Secrets rule: `CLOUDINARY_API_SECRET` (or the embedded secret inside `CLOUDINARY_URL`) is **server-to-server only** — never exposed to web/mobile, never logged.

### 3.3 Domain layer (`Griot.Domain.Entities`)

**No schema changes** — `Attachments` table already has `StorageUrl`:

```csharp
// backend/Griot.Domain/Entities/Attachment.cs (unchanged from Feature 01)
public class Attachment
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid UploaderId { get; set; }
    public string FileName { get; set; } = string.Empty;  // max 255
    public string MimeType { get; set; } = string.Empty;  // max 100
    public long SizeBytes { get; set; }
    public string StorageUrl { get; set; } = string.Empty;  // v1: local path → v2: blob URL
    public DateTime CreatedAt { get; set; }

    // Navigation
    public TaskItem Task { get; set; } = null!;
    public User Uploader { get; set; } = null!;
}
```

**EF Core configuration** (unchanged from Feature 02).

### 3.4 Application layer (`Griot.Application.Services`)

**New interface:**

```csharp
// backend/Griot.Application/Interfaces/IBlobStorageService.cs
public interface IBlobStorageService
{
    /// <summary>Upload a file to blob storage. Returns public URL.</summary>
    Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct = default);

    /// <summary>Delete a blob by its public URL.</summary>
    Task DeleteAsync(string storageUrl, CancellationToken ct = default);

    /// <summary>Get current workspace storage usage (sum of SizeBytes for all attachments).</summary>
    Task<long> GetWorkspaceUsageAsync(Guid workspaceId, CancellationToken ct = default);
}
```

**Implementation (v1 — Cloudinary via `CloudinaryDotNet`):**

```csharp
// backend/Griot.Infrastructure/BlobStorage/CloudinaryBlobStorageService.cs
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

public class CloudinaryBlobStorageService : IBlobStorageService
{
    private readonly Cloudinary _cloudinary;
    private readonly string _folder;
    private readonly ILogger<CloudinaryBlobStorageService> _logger;

    public CloudinaryBlobStorageService(IConfiguration config, ILogger<CloudinaryBlobStorageService> logger)
    {
        _logger = logger;

        // Option A: single URL (CLOUDINARY_URL=cloudinary://key:secret@cloud_name)
        var url = Environment.GetEnvironmentVariable("CLOUDINARY_URL");
        Account account = url is not null
            ? new Account(url)
            : new Account(
                Environment.GetEnvironmentVariable("CLOUDINARY_CLOUD_NAME")!,
                Environment.GetEnvironmentVariable("CLOUDINARY_API_KEY")!,
                Environment.GetEnvironmentVariable("CLOUDINARY_API_SECRET")!);

        _cloudinary = new Cloudinary(account) { Api = { Secure = true } };
        _folder = config["BlobStorage:Cloudinary:Folder"] ?? "griot/attachments";
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        fileStream.Position = 0;
        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(fileName, fileStream),
            Folder = _folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
            // Preserve original bytes; Cloudinary must not transform non-image files
            Type = "upload"
        };

        var result = await _cloudinary.UploadAsync(uploadParams, ct);
        if (result.Error is not null)
        {
            _logger.LogError("Cloudinary upload failed for {File}: {Error}", fileName, result.Error.Message);
            throw new DomainError(DomainErrorKind.External, $"Cloudinary upload failed: {result.Error.Message}");
        }

        return result.SecureUrl!.ToString();  // public CDN URL (e.g. https://res.cloudinary.com/<cloud>/raw/upload/...)
    }

    public async Task DeleteAsync(string storageUrl, CancellationToken ct)
    {
        // Derive the public ID from the stored URL (Cloudinary asset identifier)
        var publicId = ExtractPublicId(storageUrl);
        if (publicId is null)
        {
            _logger.LogWarning("Could not derive Cloudinary public id from {Url}; skipping destroy", storageUrl);
            return;
        }

        var result = await _cloudinary.DestroyAsync(new DeletionParams(publicId), ct);
        // "ok" = deleted; "not found" = already gone (treat as success — idempotent deletes)
        if (result.Result is not ("ok" or "not found"))
        {
            _logger.LogWarning("Cloudinary destroy for {PublicId} returned {Result}", publicId, result.Result);
        }
    }

    public async Task<long> GetWorkspaceUsageAsync(Guid workspaceId, CancellationToken ct)
    {
        // Implementation: Query workspace storage usage from Attachments table
        // In actual code, inject IAttachmentRepository and call:
        //   return await _attachmentRepo.GetWorkspaceUsageBytesAsync(workspaceId, ct);
        // SQL contract:
        //   SELECT COALESCE(SUM(a.SizeBytes), 0) FROM Attachments a
        //   JOIN TaskItems t ON a.TaskId = t.Id
        //   WHERE t.WorkspaceId = @workspaceId
        return 0; // TODO: Replace with repository call when AttachmentRepository is implemented
    }

    private static string? ExtractPublicId(string storageUrl)
    {
        // res.cloudinary.com/<cloud>/<type>/upload/v<ver>/<publicId>.<ext>  →  <publicId>
        var marker = "/upload/";
        var idx = storageUrl.IndexOf(marker, StringComparison.Ordinal);
        if (idx < 0) return null;
        var tail = storageUrl[(idx + marker.Length)..];
        var slash = tail.IndexOf('/');
        if (slash >= 0 && tail.StartsWith("v", StringComparison.Ordinal)) tail = tail[(slash + 1)..];
        var dot = tail.LastIndexOf('.');
        return dot > 0 ? tail[..dot] : tail;
    }
}
```

**Registration (`Program.cs`):**

```csharp
builder.Services.AddSingleton<IBlobStorageService, CloudinaryBlobStorageService>();
```

**Attachment path changes (spec 17 metadata-only today; this spec adds real upload):**

```csharp
// backend/Griot.Application/Services/DomainService.cs (CreateAttachmentAsync) gains real blob wiring
public class DomainService
{
    private readonly IAttachmentRepository _repo;
    private readonly ITaskRepository _taskRepo;
    private readonly IBlobStorageService _blobStorage;
    private readonly IConfiguration _config;

    public AttachmentService(
        IAttachmentRepository repo,
        ITaskRepository taskRepo,
        IBlobStorageService blobStorage,
        IConfiguration config)
    {
        _repo = repo;
        _taskRepo = taskRepo;
        _blobStorage = blobStorage;
        _config = config;
    }

    public async Task<AttachmentDto?> CreateAttachmentAsync(Guid taskId, CreateAttachmentRequest request, Guid userId)  // + IBlobStorageService upload
    {
        // SECURITY NOTE: This specification phase implementation has known issues that MUST be fixed before production:
        // 1. File validation: ContentType is client-controlled; add extension and magic-byte validation (see §7.1)
        // 2. Quota atomicity: Concurrent uploads can bypass quota; implement atomic reservation (see FIXME below)
        // 3. Workspace authorization: Add explicit workspace membership check before upload (see FIXME below)
        
        // 1. Validate file size
        var maxSize = _config.GetValue<long>("BlobStorage:MaxFileSizeBytes");
        if (file.Length > maxSize)
            throw new ValidationException($"File size exceeds {maxSize / 1024 / 1024} MB limit.");

        // 2. Validate MIME type
        // FIXME (Security): Add extension validation against BlockedExtensions from §7.1
        // FIXME (Security): Add magic-byte validation - do not trust client-controlled ContentType alone
        var allowedTypes = _config.GetSection("BlobStorage:AllowedMimeTypes").Get<string[]>()!;
        if (!allowedTypes.Contains(file.ContentType))
            throw new ValidationException($"File type {file.ContentType} not allowed.");

        // 3. Validate workspace quota
        // FIXME (Security): Add workspace membership check - verify uploaderId is member of task's workspace
        var task = await _taskRepo.GetByIdAsync(taskId, ct);
        // FIXME (Security): Make quota enforcement atomic - use database-level reservation to prevent race conditions
        // Current read-then-check pattern allows concurrent uploads to bypass quota
        // Suggested fix: Add WorkspaceStorageReservations table with atomic INSERT/UPDATE
        var usage = await _blobStorage.GetWorkspaceUsageAsync(task.Board.Project.WorkspaceId, ct);
        var quota = _config.GetValue<long>("BlobStorage:MaxWorkspaceQuotaBytes");
        if (usage + file.Length > quota)
            throw new ValidationException("Workspace storage quota exceeded.");

        // 4. Upload to blob storage
        using var stream = file.OpenReadStream();
        var storageUrl = await _blobStorage.UploadAsync(stream, file.FileName, file.ContentType, ct);

        // 5. Save metadata to DB
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UploaderId = uploaderId,
            FileName = file.FileName,
            MimeType = file.ContentType,
            SizeBytes = file.Length,
            StorageUrl = storageUrl,
            CreatedAt = DateTime.UtcNow
        };

        await _repo.AddAsync(attachment, ct);
        return attachment;
    }

    public async Task<IEnumerable<Attachment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct)
    {
        // FIXME (Security): Add workspace membership check before listing attachments
        // Verify caller has access to the task's workspace
        // Retrieve all attachments for a given task
        return await _repo.GetByTaskIdAsync(taskId, ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var attachment = await _repo.GetByIdAsync(id, ct);
        if (attachment == null) throw new NotFoundException("Attachment not found.");

        // FIXME (Security): Add workspace membership/role check before deletion
        // Verify the attachment's task belongs to a workspace the caller can access
        // Verify caller has permission to delete (owner, workspace admin, or uploader)

        // Delete from blob storage first (idempotent — 404 is OK)
        await _blobStorage.DeleteAsync(attachment.StorageUrl, ct);

        // Then remove DB record
        await _repo.DeleteAsync(id, ct);
    }
}
```

### 3.5 API layer (`Controllers.AttachmentController`)

```csharp
// backend/Griot.Api/Controllers/AttachmentController.cs
[ApiController]
[Route("api/tasks/{taskId}/attachments")]
[Authorize]
public class AttachmentController : DomainControllerBase
{
    private readonly IDomainService _domain;

    [HttpGet]
    public async Task<IActionResult> List(Guid taskId, CancellationToken ct)
    {
        var attachments = await _service.GetByTaskIdAsync(taskId, ct);
        return Ok(attachments.Select(a => new AttachmentDto
        {
            Id = a.Id,
            FileName = a.FileName,
            MimeType = a.MimeType,
            SizeBytes = a.SizeBytes,
            StorageUrl = a.StorageUrl,  // public URL — client fetches directly
            CreatedAt = a.CreatedAt,
            Uploader = new UserSummaryDto { Id = a.UploaderId, DisplayName = a.Uploader.DisplayName }
        }));
    }

    [HttpPost]
    [RequestSizeLimit(26_214_400)]  // 25 MB limit (matches appsettings)
    public async Task<IActionResult> Upload(Guid taskId, IFormFile file, CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var attachment = await _service.UploadAsync(taskId, userId, file, ct);

        return CreatedAtAction(nameof(List), new { taskId }, new AttachmentDto { ... });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _service.DeleteAsync(id, ct);
        return NoContent();
    }
}
```

**Rate limiting** (apply to upload endpoint):
```csharp
[RateLimit("uploads", 10, 60)]  // 10 uploads per 60 seconds per user
```

### 3.6 GraphQL (optional — defer to Feature 05 extension)

```graphql
type Attachment {
  id: ID!
  fileName: String!
  mimeType: String!
  sizeBytes: Long!
  storageUrl: String!  # public URL
  createdAt: DateTime!
  uploader: User!
}

extend type Query {
  attachments(taskId: ID!): [Attachment!]!
}

extend type Mutation {
  # Note: GraphQL file uploads use `Upload` scalar (graphql-upload middleware)
  uploadAttachment(taskId: ID!, file: Upload!): Attachment!
  deleteAttachment(id: ID!): Boolean!
}
```

---

## 4. Migration Strategy (local → Cloudinary)

### 4.1 Pre-launch (no existing production data)
- No migration needed — v1 ships with Cloudinary from day 1

### 4.2 If local-disk uploads exist (staging/dev)
1. Export attachments:
   ```sql
   SELECT Id, TaskId, FileName, MimeType, SizeBytes, StorageUrl FROM Attachments;
   ```
2. For each row:
   - Read file from `StorageUrl` (local path, e.g. `./uploads/abc123.jpg`)
   - Upload to Cloudinary via `IBlobStorageService.UploadAsync()`
   - Update `StorageUrl` to new blob URL
   - Delete local file
3. Deploy new code + env vars

---

## 5. Testing Strategy

### 5.1 Unit tests (`Griot.Application.Tests`)

```csharp
public class DomainServiceAttachmentTests
{
    [Fact]
    public async Task UploadAsync_ExceedsFileSizeLimit_ThrowsValidationException()
    {
        // Arrange: IFormFile with Length = 30 MB (exceeds 25 MB limit)
        var file = Mock.Of<IFormFile>(f => f.Length == 31_457_280);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UploadAsync(taskId, userId, file, ct));
    }

    [Fact]
    public async Task UploadAsync_ExceedsWorkspaceQuota_ThrowsValidationException()
    {
        // Arrange: workspace already has 95 MB used, user uploads 10 MB file (quota = 100 MB)
        _blobStorageMock.Setup(b => b.GetWorkspaceUsageAsync(workspaceId, ct)).ReturnsAsync(99_614_720);
        var file = Mock.Of<IFormFile>(f => f.Length == 10_485_760);

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _service.UploadAsync(taskId, userId, file, ct));
    }

    [Fact]
    public async Task UploadAsync_ValidFile_SavesMetadataAndReturnsPublicUrl()
    {
        // Arrange: 5 MB JPEG file
        var file = CreateMockFormFile("test.jpg", "image/jpeg", 5_242_880);
        _blobStorageMock.Setup(b => b.UploadAsync(It.IsAny<Stream>(), "test.jpg", "image/jpeg", ct))
            .ReturnsAsync("https://res.cloudinary.com/griot/raw/upload/griot/attachments/test-abc123.jpg");

        // Act
        var attachment = await _service.UploadAsync(taskId, userId, file, ct);

        // Assert
        Assert.Equal("https://res.cloudinary.com/griot/raw/upload/griot/attachments/test-abc123.jpg", attachment.StorageUrl);
        _repoMock.Verify(r => r.AddAsync(It.Is<Attachment>(a => a.SizeBytes == 5_242_880), ct), Times.Once);
    }
}
```

### 5.2 Integration tests (`Griot.Api.Tests`)

```csharp
[Collection("ApiIntegration")]
public class AttachmentControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task POST_UploadAttachment_Returns201AndPublicUrl()
    {
        // Arrange: authenticated user, valid task, 2 MB JPEG
        var client = _factory.CreateAuthenticatedClient(userId);
        var content = new MultipartFormDataContent();
        var fileBytes = new byte[2_097_152];  // 2 MB
        new Random().NextBytes(fileBytes);
        content.Add(new ByteArrayContent(fileBytes), "file", "test.jpg");

        // Act
        var response = await client.PostAsync($"/api/tasks/{taskId}/attachments", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var attachment = await response.Content.ReadFromJsonAsync<AttachmentDto>();
        Assert.StartsWith("https://", attachment!.StorageUrl);  // public blob URL
    }

    [Fact]
    public async Task POST_UploadAttachment_ExceedsFileSize_Returns400()
    {
        // Arrange: 30 MB file (exceeds 25 MB limit)
        var client = _factory.CreateAuthenticatedClient(userId);
        var content = new MultipartFormDataContent();
        var fileBytes = new byte[31_457_280];  // 30 MB
        content.Add(new ByteArrayContent(fileBytes), "file", "large.jpg");

        // Act
        var response = await client.PostAsync($"/api/tasks/{taskId}/attachments", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        Assert.Contains("exceeds", error!.Message, StringComparison.OrdinalIgnoreCase);
    }
}
```

### 5.3 Postman tests (Newman CI)

Add to `Postman/Griot.postman_collection.json`:

```json
{
  "name": "Upload Attachment",
  "request": {
    "method": "POST",
    "url": "{{baseUrl}}/api/tasks/{{taskId}}/attachments",
    "header": [{ "key": "Authorization", "value": "Bearer {{accessToken}}" }],
    "body": {
      "mode": "formdata",
      "formdata": [
        { "key": "file", "type": "file", "src": "/path/to/test-image.jpg" }
      ]
    }
  },
  "tests": [
    "pm.test('Status 201', () => pm.response.to.have.status(201));",
    "pm.test('Returns public URL', () => pm.expect(pm.response.json().storageUrl).to.match(/^https:\\/\\//));"
  ]
}
```

---

## 6. Observability & Monitoring

### 6.1 Metrics (tracked via Netdata + Cloudinary dashboard)
- **Blob storage size:** Cloudinary dashboard → total GB used (credits consumed)
- **Blob data transfer:** Cloudinary dashboard → total GB delivered (track bandwidth/egress)
- **Upload success rate:** `ApiLogs` → count 201 vs 400/500 for `/api/tasks/{id}/attachments`
- **p95 upload latency:** `ApiLogs.DurationMs` for upload endpoint

### 6.2 Alerts
- **Quota exceeded:** Email/Slack when workspace usage > 95 MB (before hitting 100 MB hard cap)
- **Upload failure spike:** If upload 5xx rate > 5% over 10 min, alert on-call
- **Cloudinary API errors:** Log + alert if Cloudinary returns non-2xx (503 = service issue) or `result.Error` is set

### 6.3 Logs (`ApiLogs`, `ErrorLogs`)
- **Upload success:** `INFO: Uploaded {FileName} ({SizeBytes} bytes) for Task {TaskId}, StorageUrl: {Url}`
- **Upload failure:** `ERROR: Upload failed for {FileName}: {ErrorMessage}`
- **Quota warning:** `WARN: Workspace {WorkspaceId} at 95% storage quota ({UsageBytes}/{QuotaBytes})`

---

## 7. Security & Compliance

### 7.1 File validation (defense in depth)
1. **MIME type whitelist:** Only allow images + docs (no `.exe`, `.sh`, `.bat`, etc.)
2. **Extension blacklist:** Reject blocked extensions even if MIME type passes
3. **Content-Type header check:** Validate `Content-Type` header matches file magic bytes (use `FileSignatureValidator` NuGet package)
4. **File size cap:** 25 MB per file (prevents DoS via giant uploads)
5. **Workspace quota:** 100 MB total per workspace (prevents storage abuse)

### 7.2 Access control
- **Uploads:** Only authenticated workspace members can upload attachments to their workspace's tasks
- **Downloads:** Public blob URLs are **accessible to anyone with the URL** (no auth required) — this is intentional for CDN caching
  - Risk: If URL leaks, anyone can download the file
  - Mitigation: Use signed-delivery URLs (Cloudinary supports signed URLs / TTL expiration) for sensitive attachments (v2 enhancement)

### 7.3 Audit trail
- Every upload/delete → `ActivityLogs` (action = `AttachmentUploaded`/`AttachmentDeleted`, payload = JSON with file metadata)
- Every upload/delete → `AuditLogs` (upload: before = null, after = attachment metadata; delete: before = attachment metadata, after = null)
  - **Rationale:** Attachments are state-changing writes (Attachment table INSERT/DELETE). Per ARCHITECTURE.md §3.2, all state-changing writes require AuditLogs for compliance traceability.

### 7.4 OWASP compliance (Week 6 gate)
- **A03:2021 – Injection:** Validated — file uploads are binary, not executed; MIME type whitelisted
- **A01:2021 – Broken Access Control:** Validated — workspace membership required to upload; blob URLs are public (documented risk)
- **A05:2021 – Security Misconfiguration:** Validated — blocked extensions prevent executable uploads

---

## 8. Acceptance Criteria

- [ ] `POST /api/tasks/{id}/attachments` uploads files to Cloudinary, returns public CDN URL
- [ ] `DELETE /api/tasks/{id}/attachments/{id}` deletes blob from storage + DB
- [ ] `GET /api/tasks/{id}/attachments` returns list with public `StorageUrl` for each attachment
- [ ] File size validation: 400 error if file >25 MB
- [ ] MIME type validation: 400 error if file type not in whitelist
- [ ] Workspace quota validation: 400 error if workspace exceeds 100 MB total storage
- [ ] Blocked extensions: 400 error if file extension is `.exe`, `.dll`, `.bat`, `.sh`, `.ps1`
- [ ] Public URLs are CDN-cached (verify `Cache-Control` header in blob response)
- [ ] Uploads/deletes are logged to `ActivityLogs` + `AuditLogs`
- [ ] Postman collection includes upload/delete tests (Newman green in CI)
- [ ] Unit tests cover validation edge cases (file size, quota, MIME type)
- [ ] Integration tests verify end-to-end upload + download flow
- [ ] API documentation (`docs/api/ATTACHMENTS.md`) includes limits + example responses

---

## 9. Open Questions & Future Enhancements

### Q1: Should we support client-side direct uploads (presigned URLs)?
**Answer:** Not for v1. Server-proxied uploads are simpler for auth + quota tracking. Add presigned URLs in v2 if upload latency >2s on large files.

### Q2: Should blob URLs be signed (TTL expiration) for privacy?
**Answer:** Not for v1 (public URLs are fine for project-management attachments). Add signed URLs in v2 for sensitive workspaces (enterprise feature).

### Q3: When do we migrate to Cloudflare R2?
**Answer:** When egress costs >$100/month (roughly >2 TB/month transfer at 1,500 users). Migration is a 1-day config swap (S3-compatible API).

### Q4: Should we support image thumbnails (resize on upload)?
**Answer:** Defer to v2. Use Cloudinary transformations (`/upload/w_300,c_fill/...`) for on-the-fly resizing instead of generating thumbnails at upload time.

---

## 10. References

- **Optimization recommendations:** `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` §1
- **ERD (Attachments table):** `backend/project-kit/feature-specs/01-erd-and-schema-design.md`
- **API surface:** `backend/project-kit/context/api-surface.md` (routes: `GET/POST /api/tasks/{id}/attachments`)
- **CloudinaryDotNet SDK:** https://github.com/cloudinary/CloudinaryDotNet
- **Cloudinary docs / pricing:** https://cloudinary.com/documentation / https://cloudinary.com/pricing
- **Cloudflare R2 docs:** https://developers.cloudflare.com/r2/
- **HotChocolate file uploads:** https://chillicream.com/docs/hotchocolate/v13/server/files

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**


---
**HARD RULE:** One feature spec at a time, one feature branch = one PR. Never batch specs, never commit progress-tracker updates directly to main, never commit code to main directly. AND WAIT FOR MY APPROVAL AFTER COMMITTING TO GITHUB AND UPDATE PROGRESS TRACKER BEFORE PUSHING TO GITHUB AND WHEN STARTING THE NEXT SPEC SWITCH TO ITS FEATURE BRANCH SO EACH FEATURE WITH ITS OWN BRANCH, ANY UPDATE BEING DONE TO A FEATURE MUST BE PUSHED TO THAT FEATURE BRANCH AND CONTRACT SYNC RUN, PUSH ONLY WHEN ALL HARD GATES PASS.
