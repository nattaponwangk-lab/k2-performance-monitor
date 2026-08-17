# K2 Performance Monitor — External Validation & Release Verification Plan

> Release **v0.9.0** (frozen). Application codebase is frozen; work continues only on
> external validation and release verification. **Nothing here may be marked COMPLETE
> without evidence from the target environment.**
>
> **Freeze baseline:** branch `main` @ `be21fd3` · tag `v0.9.0` @ `8f932d3` (unchanged)
> Related: [ROADMAP.md](ROADMAP.md) · [CHECKLIST.md](CHECKLIST.md) · [../RELEASE_CHECKLIST.md](../RELEASE_CHECKLIST.md) · [../PROJECT_STATE.md](../PROJECT_STATE.md)

**Legend:** ✅ Verified (evidence in this env) · ⬜ Not Verified · ⛔ Blocked External · 🏭 Requires Customer Environment · 🔑 Requires Credentials

---

## 1. K2 Integration

| Category | Detail |
|---|---|
| ✅ Verified | Contract & scaffolding only: `CollectorType` slots 10/11/12; entities `K2WorkflowStat`/`K2SmartFormStat`/`K2SmartObjectStat` (schema + migration); seeded alert rules 13–15; pages `K2Workflows`/`K2SmartForms`/`K2SmartObjects`; instance registry supports a `K2Db` connection; PoC plan in `docs/collectors.md §K2`. |
| ⬜ Not Verified | Real K2 schema/source, query correctness, metric semantics, collector persistence, per-collector performance impact. |
| ⛔ Blocked External | **Yes** — cannot proceed without a real K2 (blackpearl) instance. Status stays **BLOCKED_EXTERNAL**. |
| 🏭 Requires Customer Env | K2 host/runtime DB **or** K2 API endpoint reachable from the worker. |
| 🔑 Requires Credentials | Read-only K2 DB login (or API token) with access to workflow/form/SmartObject runtime data. |
| **Acceptance criteria** | (1) PoC confirms source of truth (host DB vs runtime tables vs API) + field meanings on sample data; (2) measured perf impact acceptable; (3) implement 3 collectors + repository persistence (removing blocked stubs); (4) integration test green against a K2 **test** instance; (5) 3 pages show real data. |
| **Evidence required to close** | Sample query outputs from real K2, schema confirmation, green K2 integration test, screenshots of live K2 pages. **Do not mark COMPLETE** until this evidence exists. |

## 2. Live Notification E2E (Email / Teams / LINE)

| Category | Detail |
|---|---|
| ✅ Verified | Providers implemented (SMTP HTML, Teams MessageCard webhook, LINE); routing by severity + channel flags; cooldown; retry/queue; failure logging; disabled-by-default; unit tests for cooldown/routing; notification failure is isolated (never crashes collector); no secrets logged (code-reviewed). |
| ⬜ Not Verified | Actual message **delivery** to a real mailbox / Teams channel / LINE recipient; end-to-end alert→notify timing. |
| ⛔ Blocked External | Partial — send path is disabled until configured; not architecturally blocked. |
| 🏭 Requires Customer Env | Reachable SMTP relay; a Teams channel with an incoming webhook; a LINE account/bot. |
| 🔑 Requires Credentials | SMTP host/port/user/password + from/to; Teams incoming webhook URL; LINE access token. (Set via env only.) |
| **Acceptance criteria** | Trigger a real rule breach (e.g., CPU > 95%) → confirm a message arrives in **each** enabled channel; confirm cooldown suppresses duplicates; confirm retry on a transient failure; confirm secrets never appear in logs. |
| **Evidence required to close** | Received-message proof per channel (message id / screenshot), `Notifications` log rows, cooldown-suppression log. |

## 3. 24–48 Hour Soak Test

| Category | Detail |
|---|---|
| ✅ Verified | Continuous write path works (short single- + multi-instance runs); retention job (daily purge, all metric tables) + rollup (raw→5m→1h) implemented; indexes `(InstanceId, CollectedAtUtc)` + `CollectedAtUtc`; per-collector `CollectorRun` audit + durations. |
| ⬜ Not Verified | DB growth curve, dashboard query latency, worker memory/CPU stability, Hangfire backlog, absence of leaks — **over 24–48h**. |
| 🏭 Requires Customer Env | A representative source SQL Server + a monitor DB running continuously for 24–48h under realistic load. |
| **Acceptance criteria** (ROADMAP §2/§11) | DB size stays bounded (retention+rollup effective); dashboard query latency stable; collector durations stable; worker process stable (no leak); no unbounded Hangfire queue. |
| **Evidence required to close** | Time-series of DB size, sampled query latencies, collector-run durations, worker RSS/CPU across the window; retention/rollup executed at least once with row-delta logged. |

## 4. UAT

| Category | Detail |
|---|---|
| ✅ Verified | Functional in dev: real-data dashboard, alert fire/dedup/ack/auto-resolve, RBAC (Admin/Operator/Viewer), instance selector, CSV export, health/readiness, login. |
| ⬜ Not Verified | Acceptance by **actual** DBA / K2 Admin / Ops users against their expectations, with their data. |
| 🏭 Requires Customer Env | Staging pointed at real source SQL/K2 with representative data + real user accounts per role. |
| 🔑 Requires Credentials | Test user accounts per role; source connection(s). |
| **Acceptance criteria** | Sign-off per role on critical paths: CPU/RAM monitoring, slow queries, blocking/deadlock, alerts→notifications, drill-down, CSV export, multi-instance selection. |
| **Evidence required to close** | Signed UAT checklist per role + resolved defect log. |

## 5. Production Deployment Readiness

| Category | Detail |
|---|---|
| ✅ Verified (this env) | `docker compose up --build` brings **SQL + worker + web healthy**; migrate-on-startup; admin seed from env; `/health/live` + `/health/ready`; non-root containers; shared Data-Protection key volume (worker↔web); secrets via env; CI green with fail-closed test gates. |
| ⬜ Not Verified (target) | Real prod infra: SQL Server with `VIEW SERVER STATE`; persisted DP key volume across restarts; backup/restore drill; TLS/reverse proxy; resource sizing; self-monitoring/alerting of the monitor. |
| 🏭 Requires Customer Env | Production SQL Server + host(s)/orchestrator for web+worker; network reach to all monitored instances; persistent storage for DP keys + backups. |
| 🔑 Requires Credentials | Production connection strings; `Auth__InitialAdminPassword`; per-instance credentials; notification creds; TLS certs. |
| **Acceptance criteria** | Deploy to prod-like env → migration clean → collectors read real source → health/readiness green → DP keys persist across restart → backup/restore drill passes → TLS enforced → runbook validated. |
| **Evidence required to close** | Deploy logs, green probes, restart-persistence proof, backup/restore test result, security sign-off in target. |

---

## Overall gate status

| Item | Current status |
|---|---|
| K2 integration | ⛔ **BLOCKED_EXTERNAL** |
| Live notifications | 🔑 Requires Credentials + 🏭 Env (code ready, unit-tested) |
| 24–48h soak | 🏭 Requires Customer Environment |
| UAT | 🏭 Requires Customer Environment |
| Prod deployment | ✅ verified in this env · 🏭/🔑 remaining in target |

**Release posture:** `v0.9.0` = **READY FOR UAT / EXTERNAL VALIDATION**. Codebase frozen. None of the five items may be marked COMPLETE until target-environment evidence exists.
