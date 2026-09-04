#!/usr/bin/env bash
set -euo pipefail

# Griot Documentation Export Script (PDF + Word)
# Usage: ./scripts/export-docs.sh [pdf|docx|both]
# Requires: pandoc 3.x+, LaTeX distribution (for PDF)

DOCS_DIR="docs"
EXPORTS_DIR="docs/exports"
FORMAT="${1:-both}"  # pdf, docx, or both (default)

# ANSI colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m' # No Color

# Create exports directory if missing
mkdir -p "$EXPORTS_DIR"

# Document list (key docs for stakeholders)
DOCS=(
  "ARCHITECTURE.md"
  "database/DATABASE-DESIGN.md"
  "planning/CAPACITY-PLAN.md"
  "planning/NFR.md"
  "planning/OPTIMIZATION-RECOMMENDATIONS.md"
  "planning/RISK-REGISTER.md"
  "planning/RUNBOOK-ROLLBACK.md"
  "planning/CHANGE-MANAGEMENT.md"
  "deployment/DEPLOYMENT.md"
  "observability/MONITORING.md"
  "api/REST-API.md"
  "api/GRAPHQL-API.md"
  "seo/SEO-DEPLOYMENT.md"
)

# Pandoc common options for PDF
PANDOC_PDF_OPTS=(
  --toc
  --toc-depth=2
  --number-sections
  --variable geometry:margin=1in
  --variable fontsize=11pt
  --variable colorlinks=true
  --highlight-style=tango
)

# Pandoc common options for Word
PANDOC_DOCX_OPTS=(
  --toc
  --toc-depth=2
  --number-sections
)

# Check dependencies
check_deps() {
  echo -e "${BLUE}Checking dependencies...${NC}"
  
  if ! command -v pandoc &> /dev/null; then
    echo -e "${RED}✗ pandoc not found. Install: https://pandoc.org/installing.html${NC}"
    exit 1
  fi
  
  if [ "$FORMAT" == "pdf" ] || [ "$FORMAT" == "both" ]; then
    if ! command -v pdflatex &> /dev/null; then
      echo -e "${YELLOW}⚠ pdflatex not found. PDF export requires LaTeX distribution.${NC}"
      echo -e "${YELLOW}  Install: texlive-latex-base (Linux), basictex (macOS), miktex (Windows)${NC}"
      exit 1
    fi
  fi
  
  echo -e "${GREEN}✓ Dependencies OK${NC}"
}

# Export function
export_doc() {
  local src="$1"
  local format="$2"
  local basename=$(basename "$src" .md)
  local subdir=$(dirname "$src")
  
  # Flatten output filename (replace / with -)
  local output_name="${subdir//\//-}-${basename}.${format}"
  local output="$EXPORTS_DIR/$output_name"
  
  echo -e "${BLUE}Exporting $src → $output_name...${NC}"
  
  if [ "$format" == "pdf" ]; then
    if pandoc "$DOCS_DIR/$src" \
      --from markdown \
      --to pdf \
      --output "$output" \
      "${PANDOC_PDF_OPTS[@]}" 2>&1; then
      echo -e "${GREEN}  ✓ PDF exported${NC}"
    else
      echo -e "${RED}  ✗ PDF export failed${NC}"
      return 1
    fi
  elif [ "$format" == "docx" ]; then
    if pandoc "$DOCS_DIR/$src" \
      --from markdown \
      --to docx \
      --output "$output" \
      "${PANDOC_DOCX_OPTS[@]}" 2>&1; then
      echo -e "${GREEN}  ✓ Word doc exported${NC}"
    else
      echo -e "${RED}  ✗ Word export failed${NC}"
      return 1
    fi
  fi
}

# Main
main() {
  echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
  echo -e "${BLUE}   Griot Documentation Export (PDF + Word)${NC}"
  echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
  echo ""
  
  check_deps
  
  echo ""
  echo -e "${BLUE}Format: ${FORMAT}${NC}"
  echo -e "${BLUE}Documents: ${#DOCS[@]}${NC}"
  echo ""
  
  local success=0
  local failed=0
  
  for doc in "${DOCS[@]}"; do
    if [ ! -f "$DOCS_DIR/$doc" ]; then
      echo -e "${YELLOW}⚠ Skipping $doc (not found)${NC}"
      continue
    fi
    
    if [ "$FORMAT" == "both" ] || [ "$FORMAT" == "pdf" ]; then
      if export_doc "$doc" "pdf"; then
        : $((success++))
      else
        : $((failed++))
      fi
    fi
    
    if [ "$FORMAT" == "both" ] || [ "$FORMAT" == "docx" ]; then
      if export_doc "$doc" "docx"; then
        : $((success++))
      else
        : $((failed++))
      fi
    fi
  done
  
  echo ""
  echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
  echo -e "${GREEN}✓ Export complete${NC}"
  echo -e "  Success: ${GREEN}${success}${NC}"
  if [ $failed -gt 0 ]; then
    echo -e "  Failed:  ${RED}${failed}${NC}"
  fi
  echo -e "  Output:  ${EXPORTS_DIR}/"
  echo -e "${BLUE}═══════════════════════════════════════════════════════${NC}"
  
  # Return nonzero if any exports failed
  [ "$failed" -eq 0 ]
}

# Run
main
