# Infra Feature 07 — Netdata Monitoring Integration

> Real-time, per-second infrastructure monitoring with anomaly detection. Deploys Netdata agents across Railway nodes with tuned retention to prevent disk-usage issues.

**Status:** Planning  
**Priority:** **HIGH** (critical for observability, must ship before public launch)  
**Effort:** 1 day (install + config + documentation)  
**Depends on:** Features 02 (backend Docker), 06 (Railway deploy)  
**Blocks:** Production incident response, capacity planning validation

---

## 1. Problem Statement

### Current state (minimal monitoring — NOT production-ready)
- `/health` endpoint (liveness + readiness) on API
- Uptime ping (Better Uptime/UptimeRobot) checks `/health` every 60s
- Railway logs (API stdout/stderr) + Vercel logs (web)
- Structured logging to SQL Server (`ApiLogs`, `ErrorLogs`, `AuditLogs`)

**Critical gaps:**
1. **No per-second metrics** — can't catch short CPU/memory spikes that 5-min averages hide
2. **No per-process visibility** — when CPU spikes, we don't know which process is responsible
3. **No anomaly detection** — manual chart review required to spot issues
4. **No SQL Server query performance tracking** — slow queries invisible until users complain
5. **No Redis operations/sec monitoring** — rate-limit failures show up as 429 errors, not metrics
6. **No proactive alerting** — we find out about issues from users, not monitoring

### Why this blocks production
- **Mean time to detect (MTTD)** incidents is unknown (likely >30 min without monitoring)
- No visibility into SQL Server/Redis performance under load (k6 baseline in Week 6 requires this)
- Can't validate capacity planning claims (~1,500 concurrent users) without real metrics

---

## 2. Solution: Netdata with Tuned Retention

### 2.1 What is Netdata?

From R&D presentation (`research/Netdata_RD_Presentation.pptx`):

**Features:**
- **Per-second granularity** — metrics collected at 1s resolution (not 5-min averages)
- **800+ auto-detected collectors** — databases, containers, web servers, hardware sensors, no config needed
- **Zero-configuration anomaly detection** — unsupervised ML flags abnormal metrics before manual review
- **Local dashboard** — full web UI at `:19999` on each node, no external dependencies
- **Netdata Cloud** — centralized view across all nodes (free tier: 5 nodes, 90-day alert history)

**Known drawback (already hit in production):**
- **Disk growth** — per-second history for every metric grows unbounded by default (single biggest complaint)
- **Fix (5 min config):** Tune retention + disk caps to prevent runaway growth

### 2.2 Deployment architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                      Netdata Cloud (free tier)                  │
│   Centralized view, alert routing, metric correlations         │
│   (5 nodes max on free tier — covers all Railway services)     │
└───────────┬────────────────────────────────┬───────────────────┘
            │                                │
    ┌───────▼────────┐              ┌───────▼────────┐
    │ Railway Node 1 │              │ Railway Node 2 │
    │   netdata      │              │   netdata      │
    │   (API + Redis)│              │   (SQL Server) │
    │   :19999       │              │   :19999       │
    └────────────────┘              └────────────────┘
            │                                │
    Auto-detects:                   Auto-detects:
    - ASP.NET process                - SQL Server
    - Redis ops/sec                  - Disk I/O
    - CPU/RAM/disk                   - CPU/RAM
    - Docker containers              - Query performance
```

**Deployment targets (Railway services):**
1. **griot-api** (ASP.NET Core + Redis client)
2. **griot-db** (SQL Server 2022) — if separate Railway service
3. **griot-redis** (Redis 7) — if separate Railway service
4. **(Optional) griot-staging** — staging environment node

**Netdata Cloud claim:** All nodes claimed to same "Griot" space → single dashboard view.

### 2.3 Comparison: Netdata vs Prometheus + Grafana

| | Netdata | Prometheus + Grafana |
|---|---|---|
| **Setup** | Zero-config, live in minutes | Manual exporters + scrape config |
| **Resolution** | Per-second, out of the box | 15–60s typical |
| **Model** | Agent-based (local storage) | Central pull + TSDB |
| **Storage** | Local dbengine per node | Central Prometheus TSDB |
| **Dashboards** | Auto-built (less customizable) | Fully custom (requires Grafana) |
| **Anomaly detection** | Built-in ML (free tier) | None (requires external tools) |
| **Best for** | Instant node-level troubleshooting | Centralized querying at scale |
| **Cost** | Free (Community tier) | Free (self-hosted) |

**Decision:** Use **Netdata for per-node instant visibility**; defer Prometheus+Grafana until we need cross-team custom dashboards (post-bootcamp).

---

## 3. Implementation Details

### 3.1 Installation (one-line per Railway node)

**Method 1: Kickstart script (recommended for Railway)**

SSH into each Railway node (or add to Dockerfile):

```bash
# Install Netdata agent
wget -O /tmp/netdata-kickstart.sh https://get.netdata.cloud/kickstart.sh
sh /tmp/netdata-kickstart.sh --stable-channel --disable-telemetry

# Verify installation
systemctl status netdata
curl http://localhost:19999/api/v1/info
```

**Method 2: Docker sidecar (alternative for containerized deployments)**

Add to `docker-compose.yml`:

```yaml
services:
  netdata:
    image: netdata/netdata:latest
    container_name: netdata
    hostname: griot-api-prod  # unique per node
    cap_add:
      - SYS_PTRACE
      - SYS_ADMIN
    security_opt:
      - apparmor:unconfined
    volumes:
      - /etc/passwd:/host/etc/passwd:ro
      - /etc/group:/host/etc/group:ro
      - /proc:/host/proc:ro
      - /sys:/host/sys:ro
      - /var/run/docker.sock:/var/run/docker.sock:ro
      - ./netdata/config:/etc/netdata
    ports:
      - "19999:19999"  # local dashboard
    environment:
      - NETDATA_CLAIM_TOKEN=${NETDATA_CLAIM_TOKEN}
      - NETDATA_CLAIM_ROOMS=${NETDATA_CLAIM_ROOMS}
      - NETDATA_CLAIM_URL=https://app.netdata.cloud
```

**Decision:** Use **kickstart script on Railway** (simpler for VM-based deploys); use Docker sidecar for local `docker-compose` development.

### 3.2 Configuration (tune retention to prevent disk growth)

**Edit `/etc/netdata/netdata.conf` on each node:**

```bash
cd /etc/netdata
./edit-config netdata.conf
```

**Key settings:**

```ini
[global]
    # Descriptive hostname for Netdata Cloud
    hostname = griot-api-prod-1

[db]
    # Storage engine mode
    mode = dbengine

    # Tier 0: per-second resolution (most expensive)
    # Default: 14 days → reduce to 7 days to save disk
    dbengine tier 0 retention = 604800  # 7 days in seconds

    # Tier 1: per-minute resolution (60s granularity)
    # Default: 3 months → keep as-is
    dbengine tier 1 retention = 7776000  # 90 days

    # Tier 2: per-hour resolution (3600s granularity)
    # Default: 2 years → reduce to 1 year
    dbengine tier 2 retention = 31536000  # 365 days

    # Hard disk space cap per node (in MB)
    # Netdata will rotate old data out when this limit is hit
    dbengine multihost disk space MB = 2048  # 2 GB per node

[web]
    # Bind to all interfaces (Railway exposes this via internal networking)
    bind to = *

    # Allow connections from Netdata Cloud
    allow connections from = localhost 10.* 172.* 192.168.* app.netdata.cloud

[plugins]
    # Disable unused collectors to reduce disk writes
    # Enable: yes | Disable: no

    # Keep essential collectors
    apps = yes         # per-process CPU/RAM/disk
    cgroups = yes      # Docker container metrics
    go.d = yes         # databases, web servers (Redis, SQL Server, etc.)
    proc = yes         # /proc filesystem metrics (CPU, RAM, disk, network)

    # Disable collectors we don't need on API nodes
    python.d = no      # Python-based collectors (rarely used)
    charts.d = no      # Shell-based collectors (slow)
    node.d = no        # Node.js collectors (unused)

[health]
    # Enable built-in health checks (CPU, RAM, disk, load, service down)
    enabled = yes

    # Notification method configured separately (see §3.3)
```

**Apply changes:**

```bash
systemctl restart netdata
```

**Disk usage estimate (after tuning):**
- Tier 0 (per-second, 7 days): ~1.5 GB per node
- Tier 1 (per-minute, 90 days): ~400 MB per node
- Tier 2 (per-hour, 365 days): ~100 MB per node
- **Total per node:** ~2 GB (matches `dbengine multihost disk space MB = 2048`)

### 3.3 Claiming nodes to Netdata Cloud

**Step 1: Generate claim token**

1. Go to https://app.netdata.cloud
2. Create or join "Griot" space
3. Navigate to **Nodes** → **Add Nodes**
4. Copy claim token + room ID

**Step 2: Claim each node**

SSH into each Railway node:

```bash
netdata-claim.sh \
    -token=<CLAIM_TOKEN> \
    -rooms=<ROOM_ID> \
    -url=https://app.netdata.cloud
```

**Verification:**
- Visit https://app.netdata.cloud → "Griot" space → Nodes
- All Railway nodes should appear (griot-api-prod-1, griot-db-prod, griot-redis-prod, etc.)

**Automation (optional):**

Add to Railway env vars:

```env
NETDATA_CLAIM_TOKEN=<token>
NETDATA_CLAIM_ROOMS=<room-id>
```

Then in Dockerfile or startup script:

```bash
if [ -n "$NETDATA_CLAIM_TOKEN" ]; then
    netdata-claim.sh -token="$NETDATA_CLAIM_TOKEN" -rooms="$NETDATA_CLAIM_ROOMS" -url=https://app.netdata.cloud
fi
```

### 3.4 Alerting configuration

**Edit `/etc/netdata/health_alarm_notify.conf`:**

```bash
cd /etc/netdata
./edit-config health_alarm_notify.conf
```

**Slack integration (recommended):**

```ini
# Slack webhook URL (replace with your actual webhook)
SLACK_WEBHOOK_URL="https://hooks.slack.com/services/YOUR_WORKSPACE/YOUR_CHANNEL/YOUR_TOKEN_HERE"

# Default recipient
DEFAULT_RECIPIENT_SLACK="#griot-alerts"

# Send notifications
SEND_SLACK="YES"
```

**Email fallback:**

```ini
# SMTP settings
DEFAULT_RECIPIENT_EMAIL="oncall@griot.io"
SEND_EMAIL="YES"

# Use sendmail or configure SMTP
EMAIL_SENDER="netdata@griot.io"
```

**Test notification:**

```bash
# Send test alert
/usr/libexec/netdata/plugins.d/alarm-notify.sh test
```

**Key alerts to configure (built-in, just enable):**
- CPU usage > 80% for 10 min
- RAM usage > 90% for 5 min
- Disk usage > 90%
- Disk I/O > 80% utilization
- SQL Server slow queries (>1s avg)
- Redis operations/sec spike (>5× baseline)
- API process down (ASP.NET Core exit)

### 3.5 Trimming unused collectors (reduce disk writes)

**Disable Python collectors** (unused on API nodes):

```bash
cd /etc/netdata
./edit-config python.d.conf
```

Set all sections to `enabled: no` except:
- (None needed for API/DB nodes — Python collectors are for legacy apps)

**Disable Go collectors not in use:**

```bash
./edit-config go.d.conf
```

Keep only:
- `docker` (container metrics)
- `redis` (if Redis is local)
- `sqlserver` (if SQL Server is local)

Disable:
- `mysql`, `postgres`, `mongodb` (not used)
- `nginx`, `apache` (API is Kestrel, not nginx)

**Result:** ~30% fewer metrics written → less disk usage + faster dashboard load.

---

## 4. Monitoring Strategy & Dashboard Usage

### 4.1 Key dashboard sections (from Netdata R&D presentation)

**Daily on-call checklist:**

1. **System Overview** — load average, CPU, RAM, swap
   - *First stop for "is this server healthy right now"*

2. **Applications (apps.plugin)** — CPU/RAM/disk I/O per process
   - *Answers "which process is eating this resource"*
   - Look for: ASP.NET Core, SQL Server, Redis processes

3. **Containers / cgroups** — per-container CPU, memory, network
   - *Essential for Docker deployments; isolates noisy-neighbor issues*

4. **Network** — per-interface throughput, packet drops, errors, retransmits
   - *Catch saturated links or NIC silently dropping packets*

5. **Disks** — per-device I/O, utilization, space, **Netdata's own DB usage**
   - *Where you'll spot Netdata's storage growth + SQL Server slow disk*

6. **Alarms tab** — every active/raised health alarm across the whole node
   - *Single place to check before assuming "everything's fine"*

**Incident response workflow:**

1. Alert fires (Slack/email) → open Netdata Cloud → identify affected node
2. Jump to **Alarms** tab → see which metrics triggered
3. Use **Metric Correlations** (Netdata Cloud feature) → see what else spiked at same time
4. Drill into **Applications** → identify which process is responsible
5. Check **Anomaly Rate ribbon** (heatmap above each chart) → spot abnormal behavior
6. Remediate + document in `ErrorLogs` (CHANGE-MANAGEMENT.md)

### 4.2 Metrics to watch (mapped to NFR targets)

| NFR target | Netdata metric | Alarm threshold |
|---|---|---|
| p95 board read <500ms | `apps.cpu` (ASP.NET Core process) | CPU >80% sustained |
| p95 dashboard <500ms | SQL Server query time | Avg query >1s |
| GraphQL p95 <200ms | `redis.operations_per_sec` | Ops spike >5× baseline |
| Attachment upload <2s | Disk I/O utilization | >80% utilization |
| API availability 99.5% | `apps.uptime` (ASP.NET Core) | Process down >1 min |

**Custom alert (optional — add to `/etc/netdata/health.d/api-latency.conf`):**

```yaml
# Alert if ASP.NET Core CPU usage is high (proxy for latency)
alarm: api_cpu_high
   on: apps.cpu
 lookup: average -10m of dotnet
  units: %
  every: 1m
   warn: $this > 70
   crit: $this > 85
   info: API process CPU usage is high (may indicate slow endpoints)
     to: sysadmin
```

---

## 5. Cost Analysis

### 5.1 Netdata Community (free tier)

**Included:**
- ✅ Unlimited local agents on all nodes
- ✅ Per-second metrics with local unlimited-node dashboards
- ✅ Built-in ML anomaly detection
- ✅ Health alarms + a few notification methods (Slack, email)
- ✅ Up to 5 nodes on Netdata Cloud (centralized view)
- ✅ ~90 days of alert/event history on Cloud
- ✅ Community & documentation support

**Limits:**
- ❌ No Metric Correlations (find correlated metrics across nodes during incidents)
- ❌ No AI Co-SRE root-cause assistant
- ❌ No priority support or SLA

**Verdict:** **Free tier is sufficient for v1** (5 Railway nodes = API + DB + Redis + staging + worker).

### 5.2 Netdata Business (~$4.50/node/month)

**Additional features:**
- ✅ Unlimited nodes on Netdata Cloud
- ✅ Unlimited seats & full role-based access control
- ✅ Unlimited alert/event history & audit logs
- ✅ All notification integrations (PagerDuty, Opsgenie, etc.)
- ✅ **Metric Correlations** (find what else spiked during an incident)
- ✅ **AI Co-SRE root-cause assistant** (summarizes probable cause from anomalies)
- ✅ Priority vendor support & SLA

**Cost estimate (5 nodes):**
- $4.50/node/month × 5 = **$22.50/month** (billed annually)

**Recommendation:** Start **free tier**; trial Business on production-critical nodes only (API + DB) if Metric Correlations proves valuable during Week 6 k6 load testing.

### 5.3 Disk usage impact (tuned vs untuned)

| Configuration | Disk per node | Total (5 nodes) | Notes |
|---|---|---|---|
| **Untuned (default)** | ~5–10 GB | 25–50 GB | 14-day tier-0 retention + unbounded growth |
| **Tuned (this spec)** | ~2 GB | 10 GB | 7-day tier-0, hard 2 GB cap per node |
| **Savings** | 3–8 GB | 15–40 GB | **75% reduction** in disk usage |

**Railway disk costs (estimate):**
- Railway Hobby: 1 GB free per service, $0.25/GB/month overage
- 10 GB tuned Netdata across 5 nodes = ~2 GB per node = $0.25/month per node = **$1.25/month total**

**Verdict:** Negligible cost after tuning.

---

## 6. Testing & Validation

### 6.1 Smoke tests (post-installation)

**For each Railway node:**

1. **Local dashboard accessible:**
   ```bash
   curl -I http://localhost:19999
   # Expected: HTTP/1.1 200 OK
   ```

2. **Metrics collecting:**
   ```bash
   curl http://localhost:19999/api/v1/info | jq '.collectors_count'
   # Expected: >50 active collectors
   ```

3. **Node claimed to Cloud:**
   - Visit https://app.netdata.cloud → "Griot" space → Nodes
   - Verify node appears with correct hostname

4. **Anomaly detection running:**
   - Open node dashboard → hover over any chart
   - Verify "Anomaly Rate" ribbon appears above chart

5. **Alerts working:**
   ```bash
   /usr/libexec/netdata/plugins.d/alarm-notify.sh test
   # Expected: Test notification in Slack/email
   ```

### 6.2 Load testing validation (Week 6 k6 baseline)

**During k6 load tests, monitor in Netdata:**
- CPU usage (apps.plugin → dotnet process) — should stay <70% at 500 concurrent users
- SQL Server query time (go.d/sqlserver) — p95 <200ms
- Redis operations/sec (go.d/redis) — should handle >10k ops/sec without saturation
- Disk I/O utilization — should stay <50% (indicates indexes are effective)
- Network throughput — should show even distribution (no single node bottleneck)

**Success criteria:**
- Netdata catches any p95 latency regression >500ms for board reads
- Anomaly Rate ribbon flags CPU/RAM spikes during sustained load
- Alerts fire if any resource crosses threshold (CPU >80%, RAM >90%)

### 6.3 Incident simulation (Day 2 ops drill)

**Simulate high CPU incident:**

1. SSH into API node → run `stress --cpu 4 --timeout 120s`
2. Wait for Netdata alert to fire (CPU >80% for >1 min)
3. Verify Slack notification received within 2 min
4. Open Netdata Cloud → drill into **Applications** → identify `stress` process
5. Kill process → verify alert auto-resolves

**Simulate slow SQL queries:**

1. Execute `SELECT * FROM TaskItems WHERE 1=1` (full table scan, no WHERE clause)
2. Repeat 100 times → monitor Netdata SQL Server collector
3. Verify anomaly detection flags abnormal query count spike
4. Document in `ErrorLogs` → tune alert threshold if false positive

---

## 7. Documentation & Runbooks

### 7.1 On-call runbook (add to `docs/planning/RUNBOOK-ROLLBACK.md`)

**Netdata incident response:**

1. **Alert received (Slack/email):**
   - Click alert link → opens Netdata Cloud node view
   - Note: node name, metric name, threshold exceeded

2. **Triage (first 5 min):**
   - Check **Alarms** tab → see all active alerts
   - Check **System Overview** → is server generally healthy?
   - Check **Anomaly Rate ribbon** → what else spiked at same time?

3. **Drill down (next 10 min):**
   - If CPU high → **Applications** → which process?
   - If RAM high → **Applications** → memory leak in ASP.NET Core?
   - If disk I/O high → **Disks** → slow SQL queries or attachment uploads?
   - If Redis ops spike → **Network** → DDoS or legitimate traffic?

4. **Remediate:**
   - Restart affected process (graceful): `systemctl restart dotnet-griot-api`
   - Scale Railway instance (temporary): add 2nd API node behind load balancer
   - Block abusive IPs (rate limit): update Redis rate-limit rules
   - Document in `ErrorLogs` table (FixStatus = Investigating → Fixed)

5. **Follow-up (post-incident):**
   - Use **Metric Correlations** (if Business tier) to find root cause
   - Update alert thresholds if false positive
   - Add to `CHANGE-MANAGEMENT.md` if config change needed

### 7.2 Dashboard quick reference (add to `docs/observability/MONITORING.md`)

**Netdata URLs:**
- **Local dashboards:** `http://<railway-node-ip>:19999` (per node)
- **Netdata Cloud:** https://app.netdata.cloud → "Griot" space

**Key sections:**
- **System Overview:** First stop — is this server healthy?
- **Applications:** Which process is eating resources?
- **Containers:** Docker container isolation issues
- **Network:** Saturated links, packet drops
- **Disks:** I/O utilization, Netdata DB growth
- **Alarms:** Single source of truth for active alerts

**Common queries:**
- "Why is CPU high?" → **Applications** → sort by CPU%
- "Why are API requests slow?" → SQL Server chart → query time
- "Why did Redis fail?" → Redis chart → operations/sec + errors
- "What spiked during incident?" → **Metric Correlations** (Business tier)

---

## 8. Acceptance Criteria

- [ ] Netdata agent installed on all Railway nodes (API, DB, Redis, staging)
- [ ] All nodes claimed to Netdata Cloud "Griot" space
- [ ] Retention tuned: tier-0 = 7 days, tier-1 = 90 days, tier-2 = 365 days
- [ ] Disk cap set to 2 GB per node (`dbengine multihost disk space MB = 2048`)
- [ ] Unused collectors disabled (Python, Node.js, legacy apps)
- [ ] Slack notifications configured + test alert sent successfully
- [ ] Key alarms enabled: CPU >80%, RAM >90%, disk >90%, API process down
- [ ] Local dashboards accessible at `:19999` on each node
- [ ] Netdata Cloud shows all 5 nodes online with live metrics
- [ ] Anomaly Rate ribbon visible on all charts (ML detection active)
- [ ] On-call runbook documented in `docs/planning/RUNBOOK-ROLLBACK.md`
- [ ] Dashboard quick reference added to `docs/observability/MONITORING.md`
- [ ] Disk usage monitored: all nodes <2 GB Netdata DB after 7 days

---

## 9. Open Questions & Future Enhancements

### Q1: Should we upgrade to Netdata Business tier?
**Answer:** Trial it on production-critical nodes (API + DB) during Week 6 k6 load testing. If Metric Correlations + AI Co-SRE prove valuable for incident response, upgrade. Otherwise, free tier is sufficient.

### Q2: Should we add Prometheus + Grafana alongside Netdata?
**Answer:** Not for v1. Netdata covers per-node instant visibility. Add Prometheus+Grafana post-bootcamp only if we need:
- Cross-team custom dashboards (PromQL queries)
- Long-term trend analysis (>1 year retention)
- Integration with external tools (e.g., Kubernetes)

### Q3: How do we monitor Netdata itself (meta-monitoring)?
**Answer:** Netdata monitors itself by default — see **Netdata** section in dashboard (dbengine disk usage, collector health, web requests/sec). Alerts fire if Netdata DB exceeds 2 GB cap.

### Q4: Should we parent/child topology for ephemeral nodes?
**Answer:** Not needed for Railway (persistent VMs). Use parent/child only if we deploy to Kubernetes (ephemeral pods) post-bootcamp.

---

## 10. References

- **Optimization recommendations:** `docs/planning/OPTIMIZATION-RECOMMENDATIONS.md` §2
- **Netdata R&D presentation:** `research/Netdata_RD_Presentation.pptx` (16 slides, R&D session)
- **Netdata install docs:** https://learn.netdata.cloud/docs/installing/
- **Netdata configuration:** https://learn.netdata.cloud/docs/configuring/configuration-reference
- **Netdata Cloud setup:** https://learn.netdata.cloud/docs/netdata-cloud/
- **Netdata alerting:** https://learn.netdata.cloud/docs/alerting/health-configuration-reference
- **Capacity planning:** `docs/planning/CAPACITY-PLAN.md` (1,500 concurrent users target)
- **NFR latency targets:** `docs/planning/NFR.md` (p95 board <500ms, dashboard <500ms)

---

**Engineering Excellence. Production Mindset. Professional Impact. 🚀**
