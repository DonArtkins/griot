# Binary Files Management (Cloudinary)

> **Why?** Binary files (PDFs, PowerPoints, screenshots) bloat Git repositories. Griot stores these files in Cloudinary (free tier: 25 GB storage + 25 GB bandwidth/month) and tracks them via a lightweight manifest file.

## Quick Start (New Clone)

```bash
# 1. Download all binary files from Cloudinary
./scripts/cloudinary-download.sh

# Done! All research files restored.
```

## File Inventory

**Total:** 14 binary files (~15 MB)

### Research Documents
- `research/GTP 2026 BOOTCAMP EDITION.pdf` — Official bootcamp specification
- `research/Netdata_RD_Presentation.pptx` — Monitoring strategy deck
- `research/Figma.png` — Figma Make reference

### Screenshots (deployment evidence, UI reference)
- `research/screenshots/SERVER.png`, `Server2.png`, `server3.png` — Railway deployment
- `research/screenshots/backend.png`, `graphql.png` — API/GraphQL playground
- `research/screenshots/UI SCREENSHOT.png` — Design mockup
- `research/screenshots/Screenshot_20260902_*.png` (5 files) — Implementation progress

## Manifest File

`.cloudinary-manifest.json` (committed to Git) maps each file to its Cloudinary URL:

```json
{
  "uploaded_at": "2026-09-04T10:48:15Z",
  "cloud_name": "your-cloud-name",
  "files": {
    "research/GTP 2026 BOOTCAMP EDITION.pdf": "https://res.cloudinary.com/...",
    "research/Netdata_RD_Presentation.pptx": "https://res.cloudinary.com/...",
    ...
  }
}
```

## For Maintainers (Upload/Update)

### Initial Setup

1. **Create Cloudinary account** (free tier):
   ```
   https://cloudinary.com/users/register_free
   ```

2. **Get credentials** from dashboard:
   ```
   https://console.cloudinary.com/
   ```

3. **Set environment variable**:
   ```bash
   export CLOUDINARY_URL=cloudinary://API_KEY:API_SECRET@CLOUD_NAME
   
   # Or add to ~/.bashrc / ~/.zshrc for persistence
   echo 'export CLOUDINARY_URL=cloudinary://API_KEY:API_SECRET@CLOUD_NAME' >> ~/.bashrc
   ```

### Upload Binary Files

```bash
# Upload all binary files and generate manifest
./scripts/cloudinary-upload.sh

# Expected output:
#   ✓ Uploaded: https://res.cloudinary.com/.../research-GTP-2026-BOOTCAMP-EDITION.pdf
#   ✓ Uploaded: https://res.cloudinary.com/.../research-Netdata_RD_Presentation.pptx
#   ...
#   Success: 14/14
#   Manifest: .cloudinary-manifest.json
```

### Commit Manifest

```bash
git add .cloudinary-manifest.json
git commit -m "chore: update Cloudinary manifest"
git push origin main
```

### Add New Binary Files

1. Place file in appropriate directory (e.g., `research/new-doc.pdf`)
2. Edit `scripts/cloudinary-upload.sh` → add to `FILES` array
3. Run `./scripts/cloudinary-upload.sh` → generates updated manifest
4. Commit updated manifest

## .gitignore Strategy

Binary files are **ignored by Git** but **tracked in manifest**:

```gitignore
# Binary research files (tracked in .cloudinary-manifest.json)
research/*.pdf
research/*.pptx
research/*.docx
research/screenshots/*.png
research/screenshots/*.jpg

# Except extracted text (tracked in Git)
!research/_bootcamp_2026.txt

# Cloudinary manifest (committed)
!.cloudinary-manifest.json
```

## Cloudinary Free Tier Limits

- **Storage:** 25 GB
- **Bandwidth:** 25 GB/month
- **Transformations:** 25,000 credits/month

**Current usage:** ~15 MB (14 files) — well within free tier.

## Troubleshooting

### Download fails with "Manifest not found"

```bash
# Ensure you're in project root
cd /path/to/griot

# Check manifest exists
ls -la .cloudinary-manifest.json

# If missing, pull latest from Git
git pull origin main
```

### Upload fails with "Invalid CLOUDINARY_URL"

```bash
# Verify format (note the cloudinary:// protocol)
echo $CLOUDINARY_URL
# Should output: cloudinary://123456:SECRET@cloud-name

# Test credentials
curl -u "123456:SECRET" https://api.cloudinary.com/v1_1/cloud-name/resources/image
```

### Files already exist (skip download)

```bash
# Download script skips existing files by default
# To force re-download, delete local files first
rm research/*.pdf research/*.pptx research/screenshots/*.png

# Then re-run
./scripts/cloudinary-download.sh
```

## Direct Access URLs

All files are publicly accessible via Cloudinary CDN. URLs are in `.cloudinary-manifest.json`.

**Example:**
```
research/GTP 2026 BOOTCAMP EDITION.pdf
→ https://res.cloudinary.com/YOUR_CLOUD/raw/upload/griot/research-GTP-2026-BOOTCAMP-EDITION.pdf
```

You can share these URLs directly without cloning the repo.

## Alternative: Manual Download

If scripts fail, download files manually from manifest URLs:

```bash
# Read manifest
cat .cloudinary-manifest.json | grep "GTP 2026"

# Copy URL and download with curl/wget
curl -o "research/GTP 2026 BOOTCAMP EDITION.pdf" "https://res.cloudinary.com/..."
```

## CI/CD Integration

To auto-download files in GitHub Actions / CI:

```yaml
- name: Download binary files
  run: |
    chmod +x scripts/cloudinary-download.sh
    ./scripts/cloudinary-download.sh
```

No credentials needed for download (manifest URLs are public).

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
