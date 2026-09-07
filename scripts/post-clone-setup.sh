#!/usr/bin/env bash
set -euo pipefail

# Post-Clone Setup Script
# Automatically downloads binary files from Cloudinary after git clone

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m'

echo ""
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo -e "${BLUE}   Griot Post-Clone Setup${NC}"
echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
echo ""
echo "This script will download binary files from Cloudinary."
echo ""
echo -e "${GREEN}Step 1: Downloading research files (PDF, PPTX, screenshots)...${NC}"
echo ""

# Run cloudinary download script
if [ -x "$SCRIPT_DIR/cloudinary-download.sh" ]; then
  "$SCRIPT_DIR/cloudinary-download.sh"
else
  echo "Error: cloudinary-download.sh not found or not executable"
  exit 1
fi

echo ""
echo -e "${GREEN}✓ Setup complete!${NC}"
echo ""
echo "You can now:"
echo "  1. Read research docs: ${BLUE}research/*.pdf${NC}"
echo "  2. View screenshots: ${BLUE}research/screenshots/*.png${NC}"
echo "  3. Start development"
echo ""
