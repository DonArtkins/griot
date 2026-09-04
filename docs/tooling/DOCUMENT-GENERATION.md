# Document Generation — PDF & Word Export from Markdown

> Tooling strategy for generating PDF and Word documents from Griot's markdown documentation. Uses Pandoc for batch conversion with consistent formatting and professional output.

**Status:** Documented  
**Priority:** Medium (nice-to-have for stakeholder sharing)  
**Dependencies:** Pandoc 3.x+

---

## 1. Why Export to PDF/Word?

**Use cases:**
- **Stakeholder sharing:** Send architecture/design docs to non-technical stakeholders who prefer PDF
- **Code reviews:** Attach spec PDFs to pull requests for offline review
- **Documentation archives:** Generate timestamped PDF snapshots for regulatory compliance
- **Printing:** Professional printed docs for bootcamp presentations/demos
- **Offline access:** Word docs editable offline without markdown tooling

**What NOT to use this for:**
- Don't replace markdown as source of truth (markdown stays in git)
- Don't manually edit generated PDFs/Word docs (regenerate from markdown instead)
- Don't use for real-time collaboration (markdown + PR workflow is better)

---

## 2. Tooling Strategy: Pandoc

### 2.1 Why Pandoc?

**Pandoc** is the universal document converter (markdown → PDF/Word/HTML/LaTeX/etc.).

**Advantages:**
- ✅ Converts markdown → PDF with proper formatting (headings, code blocks, tables)
- ✅ Converts markdown → Word (.docx) preserving styles
- ✅ Supports custom templates (company branding, header/footer)
- ✅ CLI-based → scriptable batch conversion
- ✅ Open-source, actively maintained, industry-standard

**Alternatives considered:**
| Tool | Pros | Cons | Verdict |
|---|---|---|---|
| **Pandoc** | Universal, scriptable, templates | Requires LaTeX for PDF | ✅ **Best fit** |
| VSCode MD PDF | Easy, VSCode integrated | No custom templates, manual only | ❌ Not scriptable |
| md-to-pdf (npm) | Simple, no LaTeX needed | Basic styling only | ❌ Limited customization |
| Asciidoctor | Great for books, PDFs | Asciidoc syntax, not markdown | ❌ Wrong source format |

**Decision:** Use Pandoc for batch export; document LaTeX dependency for PDF generation.

### 2.2 Installation

**Linux (Ubuntu/Debian):**
```bash
sudo apt update
sudo apt install pandoc texlive-latex-base texlive-fonts-recommended texlive-latex-extra
```

**macOS (Homebrew):**
```bash
brew install pandoc basictex
# After installing BasicTeX, update PATH:
eval "$(/usr/libexec/path_helper)"
sudo tlmgr update --self
sudo tlmgr install collection-fontsrecommended
```

**Windows (Chocolatey):**
```powershell
choco install pandoc miktex
# MiKTeX will auto-install missing LaTeX packages
```

**Docker (no local install):**
```bash
docker run --rm -v $(pwd):/workspace pandoc/latex:latest input.md -o output.pdf
```

**Verify installation:**
```bash
pandoc --version  # Should show 3.x+
pdflatex --version  # Should show TeX distribution
```

---

## 3. Export Scripts

### 3.1 Single-file export

**PDF:**
```bash
pandoc docs/ARCHITECTURE.md \
  --from markdown \
  --to pdf \
  --output docs/exports/ARCHITECTURE.pdf \
  --variable geometry:margin=1in \
  --variable fontsize=11pt \
  --variable colorlinks=true \
  --toc \
  --toc-depth=2 \
  --number-sections \
  --highlight-style=tango
```

**Word (.docx):**
```bash
pandoc docs/ARCHITECTURE.md \
  --from markdown \
  --to docx \
  --output docs/exports/ARCHITECTURE.docx \
  --toc \
  --toc-depth=2 \
  --number-sections \
  --reference-doc=docs/tooling/templates/griot-template.docx  # optional custom template
```

**HTML (bonus — for web hosting):**
```bash
pandoc docs/ARCHITECTURE.md \
  --from markdown \
  --to html5 \
  --output docs/exports/ARCHITECTURE.html \
  --standalone \
  --toc \
  --css=docs/tooling/templates/griot-styles.css  # optional custom CSS
```

### 3.2 Batch export script

**Script:** `scripts/export-docs.sh`

```bash
#!/usr/bin/env bash
set -euo pipefail

# Griot documentation export script (PDF + Word)
# Usage: ./scripts/export-docs.sh [pdf|docx|both]

DOCS_DIR="docs"
EXPORTS_DIR="docs/exports"
FORMAT="${1:-both}"  # pdf, docx, or both (default)

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
  "deployment/DEPLOYMENT.md"
  "observability/MONITORING.md"
)

# Pandoc common options
PANDOC_OPTS=(
  --toc
  --toc-depth=2
  --number-sections
  --variable geometry:margin=1in
  --variable fontsize=11pt
  --variable colorlinks=true
)

# Export function
export_doc() {
  local src="$1"
  local format="$2"
  local basename=$(basename "$src" .md)
  local subdir=$(dirname "$src")
  
  # Flatten output filename (replace / with -)
  local output_name="${subdir//\//-}-${basename}.${format}"
  local output="$EXPORTS_DIR/$output_name"
  
  echo "Exporting $src → $output..."
  
  if [ "$format" == "pdf" ]; then
    pandoc "$DOCS_DIR/$src" \
      --from markdown \
      --to pdf \
      --output "$output" \
      "${PANDOC_OPTS[@]}" \
      --highlight-style=tango
  elif [ "$format" == "docx" ]; then
    pandoc "$DOCS_DIR/$src" \
      --from markdown \
      --to docx \
      --output "$output" \
      --toc \
      --toc-depth=2 \
      --number-sections
  fi
}

# Main export loop
for doc in "${DOCS[@]}"; do
  if [ "$FORMAT" == "both" ] || [ "$FORMAT" == "pdf" ]; then
    export_doc "$doc" "pdf"
  fi
  if [ "$FORMAT" == "both" ] || [ "$FORMAT" == "docx" ]; then
    export_doc "$doc" "docx"
  fi
done

echo "✓ Export complete. Files saved to $EXPORTS_DIR/"
```

**Make executable:**
```bash
chmod +x scripts/export-docs.sh
```

**Usage:**
```bash
./scripts/export-docs.sh        # Export all docs to PDF + Word
./scripts/export-docs.sh pdf    # Export all docs to PDF only
./scripts/export-docs.sh docx   # Export all docs to Word only
```

**Output:**
```
docs/exports/
├── ARCHITECTURE.pdf
├── ARCHITECTURE.docx
├── database-DATABASE-DESIGN.pdf
├── database-DATABASE-DESIGN.docx
├── planning-CAPACITY-PLAN.pdf
├── planning-CAPACITY-PLAN.docx
...
```

---

## 4. Custom Templates (Optional)

### 4.1 Word template (`griot-template.docx`)

**Create a reference document:**
1. Export any markdown file to Word without custom template:
   ```bash
   pandoc docs/ARCHITECTURE.md -o reference.docx
   ```
2. Open `reference.docx` in Microsoft Word
3. Customize styles (Heading 1, Heading 2, Body Text, Code, etc.)
4. Add header/footer with "Griot — Project Management" branding
5. Save as `docs/tooling/templates/griot-template.docx`
6. Use with `--reference-doc=docs/tooling/templates/griot-template.docx`

**Example custom styles:**
- Heading 1: Calibri 18pt, bold, accent color #4472C4 (blue)
- Heading 2: Calibri 14pt, bold
- Body: Calibri 11pt
- Code: Consolas 10pt, gray background
- Header: "Griot — Architecture Documentation" + page number
- Footer: "Confidential — GTP 2026 Bootcamp"

### 4.2 PDF template (LaTeX)

**Create custom LaTeX template:**

`docs/tooling/templates/griot-latex.tex`:
```latex
\documentclass[$if(fontsize)$$fontsize$,$endif$$if(papersize)$$papersize$paper,$endif$]{article}

% Packages
\usepackage{fancyhdr}
\usepackage{geometry}
\usepackage{hyperref}
\usepackage{listings}

% Geometry
\geometry{margin=1in}

% Header/Footer
\pagestyle{fancy}
\fancyhead[L]{Griot — $title$}
\fancyhead[R]{\thepage}
\fancyfoot[C]{Confidential — GTP 2026 Bootcamp}

% Title
\title{$title$}
\author{Sababisha Solutions}
\date{$date$}

% Code blocks
\lstset{
  basicstyle=\ttfamily\small,
  backgroundcolor=\color{gray!10},
  breaklines=true,
  frame=single
}

\begin{document}
\maketitle
\tableofcontents
\newpage

$body$

\end{document}
```

**Use custom template:**
```bash
pandoc docs/ARCHITECTURE.md \
  --from markdown \
  --to pdf \
  --output docs/exports/ARCHITECTURE.pdf \
  --template=docs/tooling/templates/griot-latex.tex \
  --variable title="Griot Architecture Documentation" \
  --variable date="$(date +%Y-%m-%d)"
```

---

## 5. CI/CD Integration (Optional)

### 5.1 GitHub Actions workflow

**`.github/workflows/export-docs.yml`:**

```yaml
name: Export Documentation

on:
  push:
    branches: [main]
    paths:
      - 'docs/**/*.md'
  workflow_dispatch:  # Manual trigger

jobs:
  export:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      
      - name: Install Pandoc + LaTeX
        run: |
          sudo apt update
          sudo apt install -y pandoc texlive-latex-base texlive-fonts-recommended texlive-latex-extra
      
      - name: Export docs
        run: ./scripts/export-docs.sh
      
      - name: Upload artifacts
        uses: actions/upload-artifact@v4
        with:
          name: griot-docs-${{ github.sha }}
          path: docs/exports/
          retention-days: 90
      
      - name: Create release (on tag)
        if: startsWith(github.ref, 'refs/tags/')
        uses: softprops/action-gh-release@v1
        with:
          files: docs/exports/*
        env:
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
```

**Result:**
- Every commit to `main` that touches `docs/` triggers export
- PDF/Word files uploaded as GitHub Actions artifacts (90-day retention)
- On version tags (e.g. `v1.0.0`), docs attached to GitHub Release

### 5.2 Pre-commit hook (Local)

**`.git/hooks/pre-commit`:**
```bash
#!/bin/bash
# Auto-export docs before committing (optional)

if git diff --cached --name-only | grep -q 'docs/'; then
  echo "Detected documentation changes, exporting PDFs..."
  ./scripts/export-docs.sh pdf
  git add docs/exports/*.pdf
fi
```

**Note:** This adds exported PDFs to git (not recommended for large docs). Better: export on CI only.

---

## 6. Best Practices

### 6.1 Source control
- ✅ **DO:** Keep markdown files in git
- ✅ **DO:** Gitignore exported PDFs/Word docs (`docs/exports/` in `.gitignore`)
- ✅ **DO:** Regenerate exports on-demand or in CI
- ❌ **DON'T:** Commit binary PDFs/Word docs to git (bloats repo)

### 6.2 Versioning
- Export with timestamp/version in filename:
  ```bash
  pandoc docs/ARCHITECTURE.md -o "ARCHITECTURE-$(date +%Y%m%d).pdf"
  ```
- For releases, include version tag:
  ```bash
  pandoc docs/ARCHITECTURE.md \
    --variable title="Griot Architecture v1.0.0" \
    -o ARCHITECTURE-v1.0.0.pdf
  ```

### 6.3 Maintenance
- Update `export-docs.sh` when adding new key docs
- Test exports after upgrading Pandoc or LaTeX distribution
- Document custom templates in this file if added

---

## 7. Troubleshooting

### 7.1 Common issues

**Error: `pandoc: pdflatex not found`**
- **Cause:** LaTeX distribution not installed
- **Fix:** Install `texlive-latex-base` (Linux) or `basictex` (macOS) or `miktex` (Windows)

**Error: `! LaTeX Error: File 'pgf.sty' not found`**
- **Cause:** Missing LaTeX package for graphics/tables
- **Fix:** Install `texlive-latex-extra` or use MiKTeX auto-install

**Error: Tables overflow page in PDF**
- **Cause:** Markdown tables too wide for A4/Letter page
- **Fix:** Simplify table or use landscape orientation:
  ```bash
  --variable geometry:landscape
  ```

**Word doc formatting is ugly**
- **Cause:** No custom template
- **Fix:** Create `griot-template.docx` reference doc (see §4.1)

### 7.2 Debugging

**Test Pandoc installation:**
```bash
pandoc --version
echo "# Test" | pandoc -f markdown -t html  # Should output HTML
```

**Test LaTeX installation:**
```bash
pdflatex --version
echo "\documentclass{article}\begin{document}Hello\end{document}" > test.tex
pdflatex test.tex  # Should generate test.pdf
```

**Verbose Pandoc output:**
```bash
pandoc docs/ARCHITECTURE.md -o test.pdf --verbose
```

---

## 8. Future Enhancements

### 8.1 Phase 2 (post-bootcamp)
- **Automated release notes:** Generate PDF changelog from `CHANGELOG.md` on release
- **Multi-language:** Export docs in multiple languages if internationalization added
- **Interactive PDFs:** Add form fields for stakeholder feedback
- **Diagrams:** Auto-embed Figma Make exports (ERD, C4 diagrams) into PDF

### 8.2 Phase 3 (if needed)
- **Custom branding:** Full LaTeX template with logo, colors, fonts
- **Document approval workflow:** Generate PDF → upload to DocuSign for e-signature
- **Archive automation:** Timestamped PDF snapshots stored in S3/R2 for compliance

---

## 9. References

- **Pandoc docs:** https://pandoc.org/MANUAL.html
- **Pandoc templates:** https://github.com/jgm/pandoc-templates
- **LaTeX packages:** https://ctan.org/
- **GitHub Actions Pandoc:** https://github.com/pandoc/pandoc-action-example
- **Markdown best practices:** https://www.markdownguide.org/basic-syntax/

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
