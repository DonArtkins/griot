#!/usr/bin/env bash
set -euo pipefail

# Cloudinary Upload Script
# Uploads binary files (PDF, PPTX, PNG, etc.) to Cloudinary and generates download manifest

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
MANIFEST_FILE="$PROJECT_ROOT/.cloudinary-manifest.json"

# Colors
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   Griot Cloudinary Upload (Binary Files)${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""

# Check for Cloudinary credentials
if [ -z "${CLOUDINARY_URL:-}" ]; then
  echo -e "${RED}Error: CLOUDINARY_URL not set${NC}"
  echo ""
  echo "Set your Cloudinary credentials:"
  echo "  export CLOUDINARY_URL=cloudinary://API_KEY:API_SECRET@CLOUD_NAME"
  echo ""
  echo "Get credentials from: https://console.cloudinary.com/"
  exit 1
fi

# Parse Cloudinary URL
CLOUD_NAME=$(echo "$CLOUDINARY_URL" | sed -n 's|.*@\(.*\)|\1|p')
API_KEY=$(echo "$CLOUDINARY_URL" | sed -n 's|.*://\(.*\):.*|\1|p')
API_SECRET=$(echo "$CLOUDINARY_URL" | sed -n 's|.*://.*:\(.*\)@.*|\1|p')

if [ -z "$CLOUD_NAME" ] || [ -z "$API_KEY" ] || [ -z "$API_SECRET" ]; then
  echo -e "${RED}Error: Invalid CLOUDINARY_URL format${NC}"
  exit 1
fi

echo -e "${GREEN}✓ Cloudinary credentials loaded${NC}"
echo -e "  Cloud: ${BLUE}${CLOUD_NAME}${NC}"
echo ""

# Binary files to upload
declare -a FILES=(
  "research/GTP 2026 BOOTCAMP EDITION.pdf"
  "research/Netdata_RD_Presentation.pptx"
  "research/Figma.png"
  "research/screenshots/SERVER.png"
  "research/screenshots/Server2.png"
  "research/screenshots/server3.png"
  "research/screenshots/UI SCREENSHOT.png"
  "research/screenshots/backend.png"
  "research/screenshots/graphql.png"
  "research/screenshots/Screenshot_20260902_202606.png"
  "research/screenshots/Screenshot_20260902_202651.png"
  "research/screenshots/Screenshot_20260902_202722.png"
  "research/screenshots/Screenshot_20260902_202743.png"
  "research/screenshots/Screenshot_20260902_202835.png"
)

# Initialize manifest
echo "{" > "$MANIFEST_FILE"
echo '  "uploaded_at": "'$(date -u +"%Y-%m-%dT%H:%M:%SZ")'",' >> "$MANIFEST_FILE"
echo '  "cloud_name": "'$CLOUD_NAME'",' >> "$MANIFEST_FILE"
echo '  "files": {' >> "$MANIFEST_FILE"

uploaded=0
failed=0
total=${#FILES[@]}

for file_path in "${FILES[@]}"; do
  full_path="$PROJECT_ROOT/$file_path"
  
  if [ ! -f "$full_path" ]; then
    echo -e "${YELLOW}⚠ Skipping $file_path (not found)${NC}"
    continue
  fi
  
  # Generate public_id from file path (replace / with -, remove extension)
  public_id="griot/$(echo "$file_path" | sed 's|/|-|g' | sed 's|\.[^.]*$||')"
  
  echo -e "${BLUE}Uploading $file_path...${NC}"
  
  # Determine resource_type based on extension
  extension="${file_path##*.}"
  case "$extension" in
    pdf|pptx|docx)
      resource_type="raw"
      ;;
    png|jpg|jpeg|gif|webp)
      resource_type="image"
      ;;
    *)
      resource_type="auto"
      ;;
  esac
  
  # Upload to Cloudinary using curl
  timestamp=$(date +%s)
  signature_string="public_id=${public_id}&timestamp=${timestamp}${API_SECRET}"
  signature=$(echo -n "$signature_string" | openssl dgst -sha256 -hex | sed 's/^.* //')
  
  response=$(curl -s -X POST \
    "https://api.cloudinary.com/v1_1/${CLOUD_NAME}/${resource_type}/upload" \
    -F "file=@${full_path}" \
    -F "public_id=${public_id}" \
    -F "timestamp=${timestamp}" \
    -F "api_key=${API_KEY}" \
    -F "signature=${signature}")
  
  # Check if upload succeeded
  if echo "$response" | grep -q '"secure_url"'; then
    secure_url=$(echo "$response" | grep -o '"secure_url":"[^"]*"' | sed 's/"secure_url":"\(.*\)"/\1/')
    echo -e "${GREEN}  ✓ Uploaded: $secure_url${NC}"
    
    # Add to manifest
    if [ $uploaded -gt 0 ]; then
      echo "," >> "$MANIFEST_FILE"
    fi
    echo -n "    \"$file_path\": \"$secure_url\"" >> "$MANIFEST_FILE"
    
    : $((uploaded++))
  else
    echo -e "${RED}  ✗ Upload failed${NC}"
    echo "$response" | head -3
    : $((failed++))
  fi
  
  # Rate limit: sleep between uploads
  sleep 1
done

# Finalize manifest
echo "" >> "$MANIFEST_FILE"
echo "  }" >> "$MANIFEST_FILE"
echo "}" >> "$MANIFEST_FILE"

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✓ Upload complete${NC}"
echo -e "  Success: ${GREEN}${uploaded}${NC} / ${total}"
if [ $failed -gt 0 ]; then
  echo -e "  Failed:  ${RED}${failed}${NC}"
fi
echo -e "  Manifest: ${BLUE}${MANIFEST_FILE}${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""
echo "Next steps:"
echo "  1. Commit .cloudinary-manifest.json to git"
echo "  2. Add binary files to .gitignore"
echo "  3. Run ./scripts/cloudinary-download.sh on new clones"
