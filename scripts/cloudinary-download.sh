#!/usr/bin/env bash
set -euo pipefail

# Cloudinary Download Script
# Downloads binary files from Cloudinary using the manifest

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
echo -e "${BLUE}   Griot Cloudinary Download (Binary Files)${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""

# Check for manifest
if [ ! -f "$MANIFEST_FILE" ]; then
  echo -e "${RED}Error: Manifest file not found${NC}"
  echo -e "  Expected: ${BLUE}${MANIFEST_FILE}${NC}"
  echo ""
  echo "This file should be committed to git."
  echo "If you're a project maintainer, run ./scripts/cloudinary-upload.sh first."
  exit 1
fi

# Check for jq (JSON parser)
if ! command -v jq &> /dev/null; then
  echo -e "${YELLOW}Warning: jq not found, using basic parsing${NC}"
  echo -e "  For better JSON parsing, install jq: ${BLUE}sudo apt-get install jq${NC}"
  echo ""
  USE_JQ=false
else
  USE_JQ=true
fi

echo -e "${GREEN}✓ Manifest loaded${NC}"
if [ "$USE_JQ" = true ]; then
  uploaded_at=$(jq -r '.uploaded_at' "$MANIFEST_FILE")
  cloud_name=$(jq -r '.cloud_name' "$MANIFEST_FILE")
  file_count=$(jq '.files | length' "$MANIFEST_FILE")
  echo -e "  Uploaded: ${BLUE}${uploaded_at}${NC}"
  echo -e "  Cloud: ${BLUE}${cloud_name}${NC}"
  echo -e "  Files: ${BLUE}${file_count}${NC}"
else
  echo -e "  Using basic parsing"
fi
echo ""

downloaded=0
skipped=0
failed=0

# Parse manifest and download files
if [ "$USE_JQ" = true ]; then
  # Use jq for clean parsing
  jq -r '.files | to_entries[] | "\(.key)|\(.value)"' "$MANIFEST_FILE" | while IFS='|' read -r file_path url; do
    full_path="$PROJECT_ROOT/$file_path"
    
    # Create directory if needed
    mkdir -p "$(dirname "$full_path")"
    
    # Check if file already exists
    if [ -f "$full_path" ]; then
      echo -e "${YELLOW}⊘ Skipping $file_path (already exists)${NC}"
      : $((skipped++))
      continue
    fi
    
    echo -e "${BLUE}Downloading $file_path...${NC}"
    
    if curl -sS -o "$full_path" "$url"; then
      echo -e "${GREEN}  ✓ Downloaded to $file_path${NC}"
      : $((downloaded++))
    else
      echo -e "${RED}  ✗ Download failed${NC}"
      : $((failed++))
    fi
    
    # Rate limit
    sleep 0.5
  done
else
  # Basic parsing without jq
  grep -o '"[^"]*": "https://[^"]*"' "$MANIFEST_FILE" | while read -r line; do
    file_path=$(echo "$line" | sed 's/"\([^"]*\)".*/\1/')
    url=$(echo "$line" | grep -o 'https://[^"]*')
    
    full_path="$PROJECT_ROOT/$file_path"
    mkdir -p "$(dirname "$full_path")"
    
    if [ -f "$full_path" ]; then
      echo -e "${YELLOW}⊘ Skipping $file_path (already exists)${NC}"
      : $((skipped++))
      continue
    fi
    
    echo -e "${BLUE}Downloading $file_path...${NC}"
    
    if curl -sS -o "$full_path" "$url"; then
      echo -e "${GREEN}  ✓ Downloaded${NC}"
      : $((downloaded++))
    else
      echo -e "${RED}  ✗ Download failed${NC}"
      : $((failed++))
    fi
    
    sleep 0.5
  done
fi

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${GREEN}✓ Download complete${NC}"
echo -e "  Downloaded: ${GREEN}${downloaded}${NC}"
if [ $skipped -gt 0 ]; then
  echo -e "  Skipped: ${YELLOW}${skipped}${NC} (already exist)"
fi
if [ $failed -gt 0 ]; then
  echo -e "  Failed: ${RED}${failed}${NC}"
fi
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
