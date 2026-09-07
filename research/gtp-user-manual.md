# GTP Environment — User Manual (Read This First)

> **Who this is for:** you, specifically, because this is your first time using Docker, .NET, Node/nvm, or any of the other tools in `gtp-2026-prep.md`. That file is the *reference* — every command, every fix, every "why". This file is the *walkthrough* — what to actually type, in order, and what you should see happen. Keep both open side by side.

---

## 1. How to use these two files together

- **`gtp-2026-prep.md`** is dense on purpose — it's the permanent record of the stack, the architecture, and every gotcha hit on this machine. You'll come back to it for months.
- **`this file`** is disposable in spirit — it's the "hold my hand through setup once" guide. Once your environment is working, you mostly won't need it again except to remember what a term means.

When this manual says "see §6.3", that's pointing at a section number in `gtp-2026-prep.md`.

---

## 2. The five tools, explained like you've never heard of them

You don't need to memorize any of this. It's here so the terminal output stops looking like a foreign language.

| Tool | What it actually is | Why GTP needs it |
|---|---|---|
| **Docker** | A way to run a whole program (a database, for example) in an isolated little box called a **container**, without installing that program directly onto your computer. | The bootcamp's databases (SQL Server, PostgreSQL, Redis) all run as Docker containers instead of being installed the normal way. |
| **Docker Compose** | A tool that starts *several* Docker containers at once from one config file (`docker-compose.yml`), instead of you typing a separate command per container. | One file starts your SQL Server + Postgres + Redis together with `docker compose up -d`. |
| **.NET SDK** | The toolkit (compiler, runtime, CLI) for building C#/.NET applications. | The GTP backend (ASP.NET Core Web API) is written in .NET 8. |
| **nvm / Node** | `nvm` (Node Version Manager) lets you have several versions of **Node.js** (a JavaScript runtime) installed at once and switch between them per project. | The React frontend, Flutter mobile tooling, and the AI/MCP layer all need Node 20 specifically, while your personal projects might use a different version — `nvm` keeps them from colliding. |
| **VSCodium** | A version of VS Code (the code editor) with Microsoft's tracking removed. | Where you'll actually write and read code, using the C# extension for the backend. |

One term that will come up constantly: a **daemon**. It just means "a program running quietly in the background, waiting for instructions." Docker has one (`dockerd`) — it has to be *running* before any `docker` command will work, the same way you can't send a text message if your phone is off.

---

## 3. First-time setup, in the order to actually do it

Do these in order. Each step tells you what success looks like, so you know when to move to the next one.

### Step 1 — Update the system
```bash
sudo apt update && sudo apt full-upgrade -y
```
**What you'll see:** a list of packages being fetched and installed. This can take a few minutes the first time. It finishes back at your normal prompt.

### Step 2 — Install Docker properly
Follow **§6.3** in `gtp-2026-prep.md` exactly, top to bottom, in one sitting. Don't skip the `sudo apt remove -y docker-compose` line even if you don't think it applies to you — it's there because it broke the install on this exact machine before.

**What success looks like**, after the whole block:
```bash
docker --version
# → Docker version 29.x.x, build ...
sudo systemctl status docker
# → Active: active (running)
sudo docker run hello-world
# → ends with "Hello from Docker!"
```
If any of those three don't match, **stop and read §6.1 and §6.3's troubleshooting notes** before continuing — don't push forward and hope it resolves itself.

**Then log out and back in** (or close and reopen your terminal). This is not optional — Linux won't apply your new Docker permissions until you do.

### Step 3 — Start the shared databases
```bash
mkdir -p ~/sababisha/infra ~/sababisha/projects
cd ~/sababisha/infra
```
Then create `docker-compose.yml` exactly as shown in **§6.4**, and run:
```bash
docker compose up -d
```
**What you'll see:** three lines like `[+] Running 3/3` with `sababisha-sqlserver`, `sababisha-postgres`, `sababisha-redis` each showing `Started` or `Healthy`. **The first run downloads the database images and can take several minutes** — SQL Server's image alone is over a gigabyte. Seeing "Pulling" sit for a while is normal, not stuck. Check on it from a second terminal tab with:
```bash
docker compose ps
```
That shows current status without interrupting the pull.

### Step 4 — Install .NET
Follow **§6.5**. The one thing to pay close attention to: after the install script finishes, you **must** add the `export PATH=...` line to `~/.bashrc` yourself and reload it (`source ~/.bashrc`), or open a brand-new terminal. The installer does not do this for you — this exact thing failed silently on this machine before.

**What success looks like:**
```bash
dotnet --version
# → 8.0.x
```

### Step 5 — Install Node via nvm
Follow **§6.6**. This one is usually the smoothest step.

**What success looks like:**
```bash
node -v   # → v20.x.x
npm -v    # → 10.x.x
```

### Step 6 — Everything else
Follow **§6.7** and **§6.8** for the remaining frontend/mobile/AI tooling. Flutter is **known not to be installed yet** on this machine — that's expected right now, not a mistake on your part. It gets installed later, before Week 4.

### Step 7 — Run the full checklist
Once all of the above is done, run every command in **§7 (Verification checklist)** of the prep doc, one at a time, and compare each output to what's described. This is your "am I actually ready" test.

---

## 4. Reading terminal output without panicking

A few patterns you'll see a lot, and what they actually mean:

- **A wall of `Notice: Skipping acquire...` lines during `apt update`** — harmless. It just means some of your software sources don't publish 32-bit (`i386`) packages, which you don't need anyway.
- **`Cannot connect to the Docker daemon at unix:///run/user/1000/podman/podman.sock`** — this specific error means something is pointing Docker commands at Podman (a different, unrelated container tool) instead of real Docker. See §6.1 gotcha 1 and gotcha 12 in the prep doc. Check `echo $DOCKER_HOST` and `type docker` first.
- **`dpkg: error processing archive ... trying to overwrite`** — two packages both want to install the same file. See §6.1 gotcha 11 — usually means an old conflicting package needs removing first.
- **A command just sits there with no new output** — for `docker compose up -d` on the first run, this is very likely a large image downloading. It is *not* the same as an error message. Give it a few minutes before assuming it's broken.
- **`command not found`** — almost always means either (a) the tool genuinely isn't installed yet, or (b) it's installed but not on your `PATH` yet (very common with `dotnet` right after install — see Step 4 above).

**General rule:** an error message that names a specific file, socket, or package is telling you exactly what's wrong — read it literally before searching for anything more exotic. Most of the failures on this machine so far turned out to be exactly what the error said, not something hidden underneath it.

---

## 5. When something goes wrong

1. **Read the actual error text**, not just "it failed." Copy the exact line.
2. **Check §6.1 in `gtp-2026-prep.md` first** — it's a running list of every real problem hit on this exact machine, with the exact fix that worked. Most new errors turn out to be a variation of something already solved there.
3. **Don't reboot as a first response.** Several of the problems already hit (failed installs, a disabled daemon, a missing PATH entry) look like they'd be fixed by a restart but aren't — the prep doc calls this out specifically because it wasted time once already.
4. **Re-run one command at a time**, not the whole block again, once you've identified which line actually failed — re-running a whole multi-line block can repeat work that already succeeded.

---

## 6. Using SQL Server inside Docker (no GUI installed — using `sqlcmd` for now)

There's no database GUI (like Azure Data Studio or DBeaver) installed yet — right now you're talking to SQL Server entirely through the `sqlcmd` command-line tool that lives *inside* the container. That's normal for this stage. Here's the actual daily pattern.

### 6.1 The command you already used, broken down

```bash
docker exec -it infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SababishaDev2026!" -C -Q "CREATE DATABASE Griot"
```

| Piece | What it means |
|---|---|
| `docker exec` | "Run a command inside an already-running container" |
| `-it` | Interactive + attach a terminal, so you can see output / type things |
| `infra-sababisha-sqlserver-1` | Which container to run it inside (your SQL Server one) |
| `/opt/mssql-tools18/bin/sqlcmd` | The actual SQL Server command-line client, installed inside the container |
| `-S localhost` | Server to connect to (from inside the container, SQL Server is always `localhost`) |
| `-U sa` | Username — `sa` is the built-in admin account |
| `-P "..."` | Password — this is `SababishaDev2026!`, the default set in your `docker-compose.yml` |
| `-C` | Trust the server's certificate (needed since it's self-signed, dev-only) |
| `-Q "..."` | Run this one SQL statement and exit |

**Your password is `SababishaDev2026!`** — it's not secret in the sense of hidden from you; it was set by you (or the default) in `docker-compose.yml` under `MSSQL_SA_PASSWORD`. It's only meant to stay out of git.

### 6.2 Opening an interactive session (instead of one-off `-Q` commands)

Drop `-Q "..."` and you get a live prompt where you can type multiple SQL statements:

```bash
docker exec -it infra-sababisha-sqlserver-1 /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "SababishaDev2026!" -C
```

You'll land on a `1>` prompt. SQL Server needs `GO` on its own line to actually *run* what you typed — this trips up almost everyone the first time:

```sql
1> SELECT name FROM sys.databases;
2> GO
```

Common things to run here:
```sql
1> USE Griot;
2> GO
1> SELECT @@VERSION;
2> GO
```

Type `EXIT` (no `GO` needed) or press `Ctrl+D` to leave.

### 6.3 A GUI, when you want one (optional, not required to keep working)

Command-line `sqlcmd` is fine for quick checks, but a GUI is much nicer for browsing tables and writing longer queries. Two solid free options that work on Linux:

- **Azure Data Studio** — Microsoft's own tool, built specifically for SQL Server. Download the `.deb` from Microsoft's site, or `sudo apt install ./azuredatastudio-linux-*.deb` after downloading.
- **DBeaver Community** — already listed in the bootcamp's Week 1 tooling (`DBeaver 24+`), and it also handles your Postgres connection in the same app, so it's the better pick here since you need both databases anyway.

Either way, the connection details are the same:
| Field | Value |
|---|---|
| Host | `localhost` |
| Port | `14333` (not 1433 — see §6.4 in the prep doc on why the port is remapped) |
| Username | `sa` |
| Password | `SababishaDev2026!` |
| Encrypt/Trust certificate | Yes / Trust server certificate (self-signed, dev only) |

For Postgres in the same GUI: host `localhost`, port `5433`, user `postgres`, password `sababisha-pg`.

### 6.4 "Database already exists" is not an error to worry about

```
Msg 1801, Level 16, State 3, Server 926a61a27165, Line 1
Database 'Griot' already exists. Choose a different database name.
```
This means `CREATE DATABASE Griot` already succeeded earlier — you're just trying to create the same thing twice. Nothing is broken; move on. If you ever need to start that database over completely, that's a deliberate `DROP DATABASE Griot;` first — never do that without meaning to, since it deletes everything in it.

---

## 7. Accessing the other tools' GUIs

| Tool | How to open it | Notes |
|---|---|---|
| **VSCodium** | `codium <path>` from a terminal (e.g. `codium ~/sababisha/projects/gtp/griot`), or the KDE menu | This is where you'll spend most of your time |
| **Docker containers** | No GUI installed by default — you're using `docker` CLI commands (`docker ps`, `docker compose ps`, `docker logs <name>`) | Docker Desktop has a GUI but isn't installed here; not required |
| **MCP Inspector** | `npx @modelcontextprotocol/inspector` (bare, no flags — see prep doc gotcha 9), then open the printed `http://127.0.0.1:6274/...` link in a browser | Only needed when working on the `mcp/` folder |
| **Trigger.dev dashboard** | `https://cloud.trigger.dev` in a browser, log in with the account from `npx trigger.dev@latest login` | For monitoring the AI/agent layer's background jobs |
| **Jira** | Wherever Sababisha's Jira workspace URL is — bookmark it | Test/project management, per the bootcamp guide |

Quick reference for checking what's currently running, any time:
```bash
docker compose -f ~/sababisha/infra/docker-compose.yml ps
```

---

## 8. Daily dev workflow — working on Griot's backend

This is the actual sequence for a normal day working in `~/sababisha/projects/gtp/griot/backend`.

### 8.1 Start of day
```bash
# 1. Make sure the shared databases are up (safe to run even if already running)
docker compose -f ~/sababisha/infra/docker-compose.yml up -d

# 2. Go to the project
cd ~/sababisha/projects/gtp/griot

# 3. Pin Node to the right version for this repo (reads .nvmrc → 20)
nvm use

# 4. Open the editor
codium .
```

### 8.2 Working in `backend/`
```bash
cd ~/sababisha/projects/gtp/griot/backend

# Run the API locally (auto-restarts on file changes)
dotnet watch run
```
Leave that running in one terminal tab. It'll print the local URL it's listening on (usually something like `http://localhost:5xxx`) — that's what your React frontend or Postman will call.

**Made a schema change?** (added/changed an EF Core model)
```bash
dotnet ef migrations add <DescriptiveName>
dotnet ef database update
```
The `update` step is what actually applies the change to the `Griot` database running inside `infra-sababisha-sqlserver-1` — you don't touch SQL Server directly for this.

**Checking the data landed correctly** — either drop into `sqlcmd` (§6.2 above) or open your GUI (§6.3) and browse the `Griot` database's tables.

### 8.3 Testing an endpoint
Use Postman (per the bootcamp's Week 2 tooling) against whatever URL `dotnet watch run` printed, e.g. `http://localhost:5001/api/...`.

### 8.4 Committing work
```bash
cd ~/sababisha/projects/gtp/griot
git status
git add <files>
git commit -m "..."
git push
```

### 8.5 End of day
You don't need to stop the Docker containers — they're lightweight and meant to stay running in the background across projects (that's the whole point of the shared `sababisha-infra` setup). Just close your terminals/editor normally. If you ever do want to stop them (e.g. before a resource-intensive task):
```bash
docker compose -f ~/sababisha/infra/docker-compose.yml stop
```
`stop` (not `down`) keeps your data — `down` would remove the containers too, though your data survives in the volumes either way unless you also pass `-v`. When in doubt, `stop` is the safe one.

### 8.6 Switching to a different Sababisha project later
```bash
cd ~/sababisha/projects/<other-project>
nvm use
```
Same running database containers serve it — nothing to restart, per §8 (Isolation Contract) in the prep doc.

---

## 9. Glossary (plain-language)

- **CLI** — Command Line Interface; a tool you control by typing commands instead of clicking.
- **daemon** — a background process that stays running and waits for instructions (e.g. `dockerd`).
- **socket** — a local communication channel a program listens on; when Docker errors mention a `.sock` path, it's saying "I tried to talk to a program at this address and nothing answered correctly there."
- **package/package manager** — `apt` is Parrot/Debian's package manager; it installs and removes software (`.deb` packages) and tracks what depends on what.
- **PATH** — the list of folders your shell searches through when you type a command name; if a tool "isn't found" but is installed, it's often just not on this list yet.
- **repo (apt context)** — a remote server `apt` downloads packages from; Docker's official repo is separate from Parrot's own.
- **container vs. image** — an **image** is the packaged, unstarted version of a program (like a recipe); a **container** is a running instance of that image (like the dish made from the recipe). `docker compose up -d` turns images into running containers.
- **`.nvmrc`** — a tiny file in a project folder that tells `nvm` which Node version that specific project wants, so `nvm use` picks it automatically.
- **`global.json`** — the .NET equivalent: pins which SDK version a specific repo uses.
- **env var (environment variable)** — a named value your shell keeps around (like `DOCKER_HOST`); some tools check these to decide how to behave, which is why a leftover one can cause confusing failures.
- **`sqlcmd`** — SQL Server's own command-line client; how you talk to the database when there's no GUI open. Needs a `GO` on its own line after a query to actually run it.
- **`sa`** — SQL Server's built-in admin username ("system administrator"), not a person's initials.
- **`/etc/profile.d/`** — a folder of scripts Linux runs automatically for every user's login shell; this is where the stray `DOCKER_HOST` export was eventually found (`podman-docker.sh`), which is why it kept coming back even after `unset` in individual terminals.
