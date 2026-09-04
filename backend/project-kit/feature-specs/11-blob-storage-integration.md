# Backend Feature 11 — Blob Storage Integration (Vercel Blob → R2 Migration Path)

> Production-ready attachment storage with Vercel Blob (v1) and migration path to Cloudflare R2 (production scale). Replaces local/disk storage with cloud-native blob store + CDN delivery.

**Status:** Planning  
**Priority:** **CRITICAL** (production blocker — must ship before public launch)  
**Effort:** 2–3 days  
**Depends on:** Features 01 (ERD), 02 (EF Core), 04 (REST APIs)  
**Blocks:** Public launch, mobile file uploads

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
| **Vercel Blob** | $0.023/GB | $0.05/GB | `@vercel/blob` SDK, 1-day setup | ✅ **v1** |
| **Cloudflare R2** | $0.015/GB | **$0 (FREE)** | S3-compatible API, 1-day migration | ✅ **v2** |
| AWS S3 | $0.023/GB | $0.09/GB (6× R2) | Mature ecosystem | ❌ expensive egress |
| Azure Blob | $0.023/GB | $0.087/GB | .NET native SDK | ❌ similar to S3 |

**Decision:**  
1. **Ship v1 with Vercel Blob on free Hobby tier** (1 GB storage + 10 GB transfer/month included with existing Vercel account — no additional cost)
2. **Monitor egress usage** — Vercel dashboard tracks storage + transfer; set alert at 8 GB/month transfer
3. **Migrate to Cloudflare R2 for production scale** when egress consistently exceeds 100 GB/month (cost savings: ~$50/month per TB egress vs Vercel's $0.05/GB)

**Rationale:** Free tier unblocks launch immediately with zero infrastructure cost; v1 usage (1,500 users × 50 MB attachments ≈ 75 GB storage) fits within free storage limit (costs ~$2/month for storage only, transfer stays within 10 GB free). R2 migration is a config swap (S3-compatible API) when scale justifies migration effort (>2TB/month transfer = >$100/month Vercel costs vs $0 R2 egress).

### 2.2 Attachment flow (v1 — Vercel Blob)

```
┌────────────┐  POST /api/tasks/{id}/attachments   ┌────────────────┐
│   Web /    │  (multipart/form-data)             │  ASP.NET API   │
│   Mobile   ├─────────────────────────────────────►│ AttachmentCtrl │
└────────────┘                                      │                │
                                                     │  ↓ validate    │
                                                     │    size/type   │
                                                     │  ↓ upload      │
                                                     │    @vercel/blob│
                ┌────────────────────────────────────┤                │
                │  Vercel Blob (S3-backed)          │  ↓ save meta   │
                │  - public URL returned            │    to SQL      │
                │  - CDN-cached (512 MB max)        └────────────────┘
                │  - auto-expires via TTL (optional)
                └────────────────────────────────────
                       │
                       ▼ public URL
                 ┌───────────────┐
                 │ Browser/Mobile│ (direct download, no API proxy)
                 └───────────────┘
```

**Key changes from local/disk:**
- `StorageUrl` now holds public blob URL (e.g. `https://xyz.public.blob.vercel-storage.com/abc123.jpg`)
- Uploads go through API → Vercel Blob SDK → S3-backed store
- Downloads bypass API (client fetches blob URL directly → CDN-cached)

### 2.3 Migration path (v1 → v2: Vercel Blob → Cloudflare R2)

When egress costs justify migration (>100 GB/month):

1. **Swap SDK:**
   ```diff
   - import { put, del } from '@vercel/blob';
   + import { S3Client, PutObjectCommand, DeleteObjectCommand } from '@aws-sdk/client-s3';
   + const s3 = new S3Client({ endpoint: 'https://<account-id>.r2.cloudflarestorage.com', ... });
   ```

2. **Update StorageUrl semantics:**
   - Vercel Blob: `https://*.public.blob.vercel-storage.com/*`
   - R2: `https://r2.griot.io/*` (custom domain via Cloudflare Workers)

3. **Migrate existing blobs:**
   - One-time script: `SELECT Id, StorageUrl FROM Attachments` → download from Vercel → upload to R2 → `UPDATE StorageUrl`
   - Cutover: update `BLOB_STORAGE_PROVIDER` env var → new uploads go to R2

**Contract:** `AttachmentService` interface stays the same; only provider implementation changes.

---

## 3. Implementation Details

### 3.1 Package dependencies

Add to `backend/Griot.Api/Griot.Api.csproj`:

```xml
<!-- v1: Vercel Blob SDK (Node.js SDK via System.Diagnostics.Process or HTTP client) -->
<!-- Note: @vercel/blob is a Node package; .NET integration via HTTP API -->
<!-- Alternative: Use Vercel Blob REST API directly (no SDK needed) -->
```

**Decision:** Use Vercel Blob **REST API** (no SDK dependency) — simpler for .NET:
- `POST https://blob.vercel-storage.com/{store-id}/put` (upload)
- `DELETE https://blob.vercel-storage.com/{store-id}?url=...` (delete)
- Auth: Bearer token from `BLOB_READ_WRITE_TOKEN` env var

**Future (R2 migration):** Add `AWSSDK.S3` NuGet package for S3-compatible API.

### 3.2 Configuration

**appsettings.json:**
```json
{
  "BlobStorage": {
    "Provider": "VercelBlob",  // or "CloudflareR2" post-migration
    "VercelBlob": {
      "StoreId": "griot_attachments",
      "ApiUrl": "https://blob.vercel-storage.com",
      "TokenEnvVar": "BLOB_READ_WRITE_TOKEN"
    },
    "MaxFileSizeBytes": 26214400,  // 25 MB per file
    "MaxWorkspaceQuotaBytes": 104857600,  // 100 MB per workspace (v1)
    "AllowedMimeTypes": [
      "image/jpeg", "image/png", "image/gif", "image/webp",
      "application/pdf", "text/plain",
      "application/vnd.openxmlformats-officedocument.wordprocessingml.document",  // .docx
      "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"  // .xlsx
    ],
    "BlockedExtensions": [".exe", ".dll", ".bat", ".sh", ".ps1"]
  }
}
```

**Railway/Vercel env vars:**
```env
BLOB_READ_WRITE_TOKEN=vercel_blob_rw_xxxxxxxxxxxx  # from Vercel dashboard → Storage → Blob
```

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

**Implementation (v1 — Vercel Blob REST API):**

```csharp
// backend/Griot.Infrastructure/BlobStorage/VercelBlobStorageService.cs
public class VercelBlobStorageService : IBlobStorageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<VercelBlobStorageService> _logger;

    public VercelBlobStorageService(HttpClient httpClient, IConfiguration config, ILogger<VercelBlobStorageService> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        var token = Environment.GetEnvironmentVariable(_config["BlobStorage:VercelBlob:TokenEnvVar"]);
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType, CancellationToken ct)
    {
        var storeId = _config["BlobStorage:VercelBlob:StoreId"];
        var apiUrl = _config["BlobStorage:VercelBlob:ApiUrl"];

        var content = new MultipartFormDataContent();
        content.Add(new StreamContent(fileStream), "file", fileName);

        var response = await _httpClient.PostAsync($"{apiUrl}/{storeId}/put", content, ct);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<VercelBlobUploadResponse>(ct);
        return result!.Url;  // public URL (e.g. https://xyz.public.blob.vercel-storage.com/abc123.jpg)
    }

    public async Task DeleteAsync(string storageUrl, CancellationToken ct)
    {
        var storeId = _config["BlobStorage:VercelBlob:StoreId"];
        var apiUrl = _config["BlobStorage:VercelBlob:ApiUrl"];

        var response = await _httpClient.DeleteAsync($"{apiUrl}/{storeId}?url={Uri.EscapeDataString(storageUrl)}", ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Failed to delete blob {Url}: {Status}", storageUrl, response.StatusCode);
        }
    }

    public async Task<long> GetWorkspaceUsageAsync(Guid workspaceId, CancellationToken ct)
    {
        // Injected IAttachmentRepository (or direct EF query)
        // SELECT SUM(SizeBytes) FROM Attachments a JOIN TaskItems t ON a.TaskId = t.Id WHERE t.WorkspaceId = @workspaceId
        throw new NotImplementedException("Query from repository");
    }

    private record VercelBlobUploadResponse(string Url);
}
```

**Registration (`Program.cs`):**

```csharp
builder.Services.AddHttpClient<IBlobStorageService, VercelBlobStorageService>();
```

**AttachmentService changes:**

```csharp
// backend/Griot.Application/Services/AttachmentService.cs
public class AttachmentService
{
    private readonly IAttachmentRepository _repo;
    private readonly IBlobStorageService _blobStorage;
    private readonly IConfiguration _config;

    public async Task<Attachment> UploadAsync(Guid taskId, Guid uploaderId, IFormFile file, CancellationToken ct)
    {
        // 1. Validate file size
        var maxSize = _config.GetValue<long>("BlobStorage:MaxFileSizeBytes");
        if (file.Length > maxSize)
            throw new ValidationException($"File size exceeds {maxSize / 1024 / 1024} MB limit.");

        // 2. Validate MIME type
        var allowedTypes = _config.GetSection("BlobStorage:AllowedMimeTypes").Get<string[]>()!;
        if (!allowedTypes.Contains(file.ContentType))
            throw new ValidationException($"File type {file.ContentType} not allowed.");

        // 3. Validate workspace quota
        var task = await _taskRepo.GetByIdAsync(taskId, ct);
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

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var attachment = await _repo.GetByIdAsync(id, ct);
        if (attachment == null) throw new NotFoundException("Attachment not found.");

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
public class AttachmentController : ControllerBase
{
    private readonly AttachmentService _service;

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

## 4. Migration Strategy (local → Vercel Blob)

### 4.1 Pre-launch (no existing production data)
- No migration needed — v1 ships with Vercel Blob from day 1

### 4.2 If local-disk uploads exist (staging/dev)
1. Export attachments:
   ```sql
   SELECT Id, TaskId, FileName, MimeType, SizeBytes, StorageUrl FROM Attachments;
   ```
2. For each row:
   - Read file from `StorageUrl` (local path, e.g. `./uploads/abc123.jpg`)
   - Upload to Vercel Blob via `IBlobStorageService.UploadAsync()`
   - Update `StorageUrl` to new blob URL
   - Delete local file
3. Deploy new code + env vars

---

## 5. Testing Strategy

### 5.1 Unit tests (`Griot.Application.Tests`)

```csharp
public class AttachmentServiceTests
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
            .ReturnsAsync("https://xyz.public.blob.vercel-storage.com/test-abc123.jpg");

        // Act
        var attachment = await _service.UploadAsync(taskId, userId, file, ct);

        // Assert
        Assert.Equal("https://xyz.public.blob.vercel-storage.com/test-abc123.jpg", attachment.StorageUrl);
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

### 6.1 Metrics (tracked via Netdata + Vercel dashboard)
- **Blob storage size:** Vercel dashboard → Storage → Blob → total GB used
- **Blob data transfer:** Vercel dashboard → total GB downloaded (track egress)
- **Upload success rate:** `ApiLogs` → count 201 vs 400/500 for `/api/tasks/{id}/attachments`
- **p95 upload latency:** `ApiLogs.DurationMs` for upload endpoint

### 6.2 Alerts
- **Quota exceeded:** Email/Slack when workspace usage > 95 MB (before hitting 100 MB hard cap)
- **Upload failure spike:** If upload 5xx rate > 5% over 10 min, alert on-call
- **Vercel Blob API errors:** Log + alert if Vercel returns non-2xx (503 = service issue)

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
  - Mitigation: Use signed URLs (Vercel Blob supports TTL expiration) for sensitive attachments (v2 enhancement)

### 7.3 Audit trail
- Every upload/delete → `ActivityLogs` (action = `AttachmentUploaded`/`AttachmentDeleted`, payload = JSON with file metadata)
- Every delete → `AuditLogs` (before = attachment metadata, after = null)

### 7.4 OWASP compliance (Week 6 gate)
- **A03:2021 – Injection:** Validated — file uploads are binary, not executed; MIME type whitelisted
- **A01:2021 – Broken Access Control:** Validated — workspace membership required to upload; blob URLs are public (documented risk)
- **A05:2021 – Security Misconfiguration:** Validated — blocked extensions prevent executable uploads

---

## 8. Acceptance Criteria

- [ ] `POST /api/tasks/{id}/attachments` uploads files to Vercel Blob, returns public URL
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
**Answer:** Defer to v2. Use Vercel Image Optimization (`/_vercel/image?url=...`) for on-the-fly resizing instead of generating thumbnails at upload time.

---

## 10. References

- **Optimization recommendations:** `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` §1
- **ERD (Attachments table):** `backend/project-kit/feature-specs/01-erd-and-schema-design.md`
- **API surface:** `backend/project-kit/context/api-surface.md` (routes: `GET/POST /api/tasks/{id}/attachments`)
- **Vercel Blob docs:** https://vercel.com/docs/storage/vercel-blob/usage-and-pricing
- **Vercel Blob REST API:** https://vercel.com/docs/storage/vercel-blob/using-blob-sdk
- **Cloudflare R2 docs:** https://developers.cloudflare.com/r2/
- **HotChocolate file uploads:** https://chillicream.com/docs/hotchocolate/v13/server/files

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
