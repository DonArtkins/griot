#!/usr/bin/env python3
"""Check contract drift and branch documentation coverage before commit/push.

Static checks supplement the semantic review required by CONTRACT-SYNC.md.
No files, Git state, services or databases are changed.
"""

import argparse
from collections import defaultdict
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
SYSTEMS = ("backend", "web", "mobile", "infra", "qa", "ai", "mcp")


def git(*args):
    return subprocess.check_output(["git", *args], cwd=ROOT, text=True).strip()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--base", help="Compare against this commit (default: merge base with main).")
    args = parser.parse_args()
    base = args.base or git("merge-base", "HEAD", "main")
    changed = set(git("diff", "--name-only", base).splitlines())
    changed.update(git("ls-files", "--others", "--exclude-standard").splitlines())
    files = set(git("ls-files").splitlines()) | changed
    errors = []

    def require(condition, message):
        if not condition:
            errors.append(message)

    branch = git("branch", "--show-current")
    require(branch not in ("main", "master", ""), "Implementation review must run on a named feature branch.")
    for system in SYSTEMS:
        numbers = defaultdict(list)
        for path in (ROOT / system / "project-kit/feature-specs").glob("[0-9][0-9]-*.md"):
            numbers[path.name[:2]].append(path.name)
        for number, names in numbers.items():
            require(len(names) == 1, f"{system}: duplicate feature {number}: {', '.join(names)}")

    stale = {
        r"\bJWT__SigningKey\b": "Use the implemented JWT__Key setting.",
        r"`sub`/`wid`|sub/wid|`sub` \+ `wid`|`sub`, `wid`": "JWT workspace claim is stale; use sub/email/jti and request membership checks.",
        r"RevokeAllActiveTokensForUserAsync": "Refresh reuse must revoke the user/family, not all user sessions.",
    }
    for name in sorted(files):
        path = ROOT / name
        if not path.is_file() or path.suffix not in (".md", ".cs"):
            continue
        # Generated migrations document historical schema, not current runtime behavior.
        if "/Migrations/" in name:
            continue
        for pattern, message in stale.items():
            for match in re.finditer(pattern, path.read_text()):
                line = path.read_text()[:match.start()].count("\n") + 1
                errors.append(f"{name}:{line}: {message}")

    checks = {
        "backend/src/Griot.Api/Program.cs": [r'"localhost:6380"', r"AddSingleton<IConnectionMultiplexer>"],
        "backend/src/Griot.Api/Controllers/AuthController.cs": [r"Status201Created", r"RegisterAsync", r"LoginAsync", r"RefreshAsync", r"LogoutAsync"],
        "backend/src/Griot.Application/Services/AuthService.cs": [r"FamilyId = storedToken.FamilyId", r"RefreshTokenBytes \* 2", r"DummyPasswordHash", r"rotated is null", r"FromDays\(30\)"],
        "backend/src/Griot.Infrastructure/Repositories/AuthRepository.cs": [r"BeginTransactionAsync", r"ExecuteUpdateAsync", r"affected != 1", r"r.FamilyId == familyId", r"IX_Users_Email"],
        "backend/src/Griot.Infrastructure/Migrations/GriotDbContextModelSnapshot.cs": [r'Property<Guid>\("FamilyId"\)', r'HasIndex\("UserId", "FamilyId"\)'],
        "docs/api/auth-contract.md": [r"201", r"401", r"204", r"FamilyId", r"JWT__Key", r"30 days", r"pending backend prerequisite"],
        "backend/project-kit/feature-specs/07-auth-jwt-argon2-redis.md": [r"FamilyId", r"JWT__Key", r"auth-contract.md"],
        "PROMPTS/week-02/02-erd-figma-make-master-prompt.md": [r"FamilyId GUID"],
    }
    for name, patterns in checks.items():
        path = ROOT / name
        content = path.read_text() if path.exists() else ""
        for pattern in patterns:
            require(re.search(pattern, content), f"{name}: expected contract marker {pattern}")
    auth = (ROOT / "backend/src/Griot.Api/Controllers/AuthController.cs").read_text()
    require("Not implemented yet" not in auth, "AuthController still returns scaffold success responses.")

    production = {".cs", ".csproj", ".ts", ".tsx", ".js", ".jsx", ".dart", ".sql"}
    for system in SYSTEMS:
        code = [name for name in changed if name.startswith(system + "/") and
                (Path(name).suffix in production or Path(name).name.startswith("Dockerfile")) and
                ("/src/" in name or "/lib/" in name or "Dockerfile" in name)]
        if not code:
            continue
        for prefix in (f"{system}/project-kit/feature-specs/", f"{system}/project-kit/context/", "docs/", "research/"):
            require(any(name.startswith(prefix) and name.endswith(".md") for name in changed),
                    f"{system}: implementation lacks synchronized changes under {prefix}")
        for required in ("AGENTS.md", f"{system}/AGENTS.md", "project-kit/context/integration-contracts.md",
                         "project-kit/context/progress-tracker.md", f"{system}/project-kit/context/progress-tracker.md", "CHANGELOG.md"):
            require(required in changed, f"{system}: missing branch contract/update evidence in {required}")

    require((ROOT / "docs/decisions/ADR-003-auth-architecture-otp-security.md").exists() and
            (ROOT / "docs/security/AUTHENTICATION-GUIDE.md").exists() and
            (ROOT / "docs/api/auth-contract.md").exists(),
            "Missing auth architecture/contract documentation (ADR-003, guide, or auth-contract).")
    for error in errors:
        print(f"FAIL: {error}", file=sys.stderr)
    if errors:
        print(f"Contract sync failed: {len(errors)} issue(s).", file=sys.stderr)
        return 1
    print(f"Contract sync passed: {len(files)} files inspected; {len(changed)} branch changes; base {base[:12]}.")
    print("Review the semantic contract matrix and run system tests before pushing.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
