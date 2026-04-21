# Modernization Playbook — Operating Model & On-Prem Deployment Supplement

> **Companion to:** `legacy-modernization-playbook.md` and `modernization-playbook-domain-supplement.md`
> **Scope:** Adjustments for (a) internal team + original vendor + embedded operators execution model, and (b) on-premises deployment without containerization.

---

## 1. Brutal Honest Assessment of Your Approach

Before the detailed guidance, the unvarnished view:

### 1.1 What Your Plan Gets Right

- **Institutional ownership.** Internal team means no knowledge drain when an SI rolls off. You end up with a system your people understand deeply, in a domain where that matters.
- **Vendor as partner, not prime.** The original vendor's knowledge of legacy business rules, edge cases, and integration quirks is irreplaceable. Having them available is a major de-risker — as long as the role is bounded.
- **Operators embedded end-to-end.** This is the gold standard. Most modernization failures trace to weak operator involvement. You're starting with the right ingredient.
- **On-prem for a safety-critical OT-adjacent system.** Defensible choice. Simplifies NERC CIP posture, avoids cloud-related audit discussions, keeps latency to SCADA/EMS predictable.
- **No containerization.** Contrarian but not wrong. Containers are not a magic ingredient. For a small-to-medium fleet with stable services and an experienced sysadmin team, VM-based deployment is simpler, lower-risk, and perfectly serviceable.

### 1.2 Where the Real Risks Are

- **Internal engineering capability gap.** The biggest unknown. Does your internal team include senior engineers who have built distributed systems of this scale before? If yes, you're fine. If no, this project is where they'll try to learn — and that creates risk. Be honest in the self-assessment.
- **Vendor incentive misalignment.** The vendor's business benefits from complexity and continued engagement. Without careful contract terms and independent architecture ownership, the new system can quietly become "legacy system v2" with a fresh UI. That's the most common failure mode for vendor-partnered rebuilds.
- **Operator burnout.** Senior operators pulled full-time for 2–3 years either lose operational sharpness (don't rotate back to the grid) or disengage (if they feel abandoned). Without a rotation plan, you'll lose them partway through.
- **On-prem velocity ceiling.** You will be slower than a cloud-native team. Not because you're worse — because environment provisioning, scaling, DR, and release cycles are fundamentally heavier. Plan for this. Do not promise sponsors cloud-like speed.
- **Storm-day scaling.** Without elastic compute, you must provision for peak. Peak in storm events is 5–10× normal. That's the capacity you buy, not your average.
- **Timeline pressure.** Internal + vendor + on-prem + no containers is honest but slow. Executive sponsors who benchmark against cloud-native SaaS case studies will feel this is slow. Prepare the narrative.

### 1.3 Honest Revised Timeline

| Phase | Realistic | Aggressive (higher risk) |
|-------|-----------|--------------------------|
| Detailed Design | 6–8 months | 4–5 months |
| Platform / infra foundation (parallel) | 4–6 months | 3 months |
| MVP Build + Internal Test | 9–13 months | 7 months |
| MVP Parallel Run + UAT | 3–4 months | 2 months |
| Incremental Build (Transmission, Di-Electric, Steam, RTU, Scheduled Maintenance) | 15–22 months | 10–12 months |
| SIT & UAT for remaining modules | 4–6 months (overlapped) | 2–3 months |
| District-by-District Cutover | 4–8 months | 3 months |
| Decommission + stabilization | 3–6 months | 2 months |
| **Total end-to-end** | **40–55 months** | **28–32 months (high risk)** |

This is 4–8 months longer than my earlier estimate, driven by:
- Internal team ramp / skill-building overhead
- On-prem infrastructure provisioning time (typically +2–3 months vs. cloud)
- Slower environment iteration cycles
- No container-based deployment automation (adds ~1 month to release engineering)
- Operator rotation and backfill coordination

**Recommendation:** Commit externally to the upper end of the realistic range. Under-promise, deliver on time, keep credibility.

---

## 2. Team & Operating Model

### 2.1 Three-Party Model: Internal + Vendor + Operators

```
                 ┌────────────────────────────────────┐
                 │   Program Steering (Internal)      │
                 │   - Final accountability           │
                 │   - Budget / scope authority       │
                 └────┬───────────────────────────────┘
                      │
         ┌────────────┼───────────────┐
         │            │               │
         v            v               v
    ┌─────────┐  ┌─────────┐     ┌──────────────┐
    │Internal │  │ Vendor  │     │  District    │
    │  Team   │<─┤ (advise)├────>│  Operators   │
    │ (build) │  │         │     │ (domain +    │
    │         │  │         │     │  acceptance) │
    └─────────┘  └─────────┘     └──────────────┘
         ^            ^                 ^
         │            │                 │
         └────────────┴─────────────────┘
                 Joint ceremonies
```

### 2.2 Role Split — Non-Negotiable Principles

| Activity | Internal | Vendor | Operators |
|----------|----------|--------|-----------|
| Program ownership | **A/R** | C | C |
| Architecture decisions (ADRs) | **A/R** | C (advise) | I |
| Product ownership | **A/R** | I | C (domain input) |
| Business rules validation | R | C | **A** (they know the rules) |
| UX decisions | R | I | **A** |
| Switching-order workflow correctness | R | C | **A** |
| Build (code) | **A/R** | C (pair / advise) | I |
| Integration with SCADA/EMS | **A/R** | C | I |
| Integration with field work system | **A/R** | C | I |
| Data migration ETL | R | **A** (they know legacy data) | I |
| Legacy system quirks, undocumented behaviors | C | **A/R** | I |
| Test strategy | **A/R** | C | I |
| UAT execution | R | I | **A** |
| Cutover go/no-go | **A** | C | R (sign-off) |
| Long-term support (post-hypercare) | **A/R** | Taper to zero | I (users) |

**Key principle:** Vendor is advisory/consultative on forward-looking decisions. Vendor is authoritative on backward-looking questions (legacy behavior, historical quirks, data model details). Internal team owns all architecture and long-term sustainment.

### 2.3 Vendor Engagement — Contract Terms That Matter

Get these right in the contract:

- **IP ownership.** All code, designs, documentation produced in the program belong to you. No residual vendor IP claims. If vendor brings pre-existing tools, they're licensed under terms that survive the engagement.
- **Knowledge transfer obligation.** Vendor commits to produce documented artifacts at defined intervals: legacy business rule catalog, data dictionary, integration specifications, operational runbooks. Payment milestones tied to knowledge transfer deliverables.
- **Staffing commitments.** Named individuals with required skills and seniority. Right of refusal for vendor staff changes. No last-minute swap-outs of your senior domain experts for juniors.
- **Exit criteria.** Vendor involvement tapers to zero at defined milestones. Explicit deliverables mark each tapering step. Vendor cannot become a permanent dependency.
- **Pair-programming / shadowing clauses.** Your engineers shadow vendor engineers and vice versa. Knowledge transfer is continuous, not document-based only.
- **Non-solicitation / non-compete.** Standard but important for your operator advisory team who will work closely with vendor staff.
- **Conflict of interest disclosure.** If vendor has other active engagements related to your legacy system (e.g., ongoing maintenance contract), disclose how the modernization affects that revenue stream. This surfaces incentive misalignment.
- **Performance metrics.** Measurable outputs. Not "vendor provides support" — instead "vendor produces X artifacts per sprint, responds within Y hours to architectural questions, provides Z hours of pair-programming per week."

### 2.4 Vendor Engagement — Working Model

**Co-located or tightly integrated, not arms-length:**
- Vendor engineers attend your sprint ceremonies
- Vendor uses your tooling (Jira, Git, wiki)
- Vendor code goes through your code review process
- Vendor does not have a parallel track or separate deliverables list

**Recommended vendor staffing profile over time:**

| Phase | Vendor FTE (approx) | Role Focus |
|-------|---------------------|------------|
| Design | 4–6 | Architecture advisory, legacy deep-dive workshops, integration spec authoring |
| MVP Build | 4–6 | Pair programming on integrations, data migration lead, legacy SME on business rules |
| Incremental Build | 3–4 | Domain SME, specific module guidance, regression review |
| SIT / UAT | 2–3 | Defect triage, legacy behavior validation |
| Cutover | 2–3 | Migration execution, legacy shutdown |
| Hypercare | 1–2 | On-call for legacy edge cases |
| Post-hypercare | 0 | Exit |

If vendor staffing stays flat past MVP, that's a signal the exit plan isn't working. Escalate.

### 2.5 Internal Team — Honest Capability Assessment

Before the program starts, assess internal capability candidly. Answer these:

| Capability | Do We Have It Today? |
|------------|----------------------|
| Senior software architects (2+ with distributed-systems experience) | Y / N / Need to hire |
| Engineers experienced with event-driven architectures | Y / N / Need to hire or train |
| DBAs experienced with high-volume OLTP + HA | Y / N |
| Infrastructure engineers for on-prem VMware / SAN / networking at scale | Y / N |
| DevOps / release engineers for VM-based CI/CD | Y / N |
| QA engineers comfortable with test automation at scale | Y / N |
| Security engineers familiar with NERC CIP | Y / N |
| Data engineers for migration / ETL work | Y / N |
| UX designer experienced with high-density operational interfaces | Y / N |
| Technical writer for operational runbooks / training | Y / N |

**If the "Y" column has gaps, your options are:**
- Hire (slow, competitive market)
- Contract (faster, more expensive, knowledge walks out the door)
- Train internal staff (slowest, builds capability long-term)
- Lean harder on vendor (creates dependency)

**Brutal truth:** If more than 3 roles are "need to hire," your timeline assumption needs another 3–6 months of ramp. Don't assume you can start building at full velocity from day one.

### 2.6 Senior District Operators — Sustainable Embedding Model

Senior operators are irreplaceable and irreplaceable is exactly why you can't use them up.

**Recommended embedding model:**

- **Core Operator Product Council: 3–4 senior operators, 50% time, rotating every 6–9 months.**
  - One always-on lead who ensures continuity
  - Others rotate; departing members onboard their successors for 2 weeks of overlap
  - Their other 50% stays in operations to preserve currency and career growth
- **Sprint-level operator time: 1–2 operators per squad, ~1 day per week.**
  - Story refinement, acceptance criteria review, sprint reviews
  - Different individuals from Product Council for diversity of input
- **UAT cohort: broader pool, 8–15 operators, ramping up through build phase.**
  - Real shift operators, not just SMEs
  - Includes operators from different districts, shifts, tenure levels
- **Operator backfill plan: documented from day one.**
  - Which operations positions are vacated, for how long, who covers
  - Budget approval explicit: this is an operations cost, not a project cost
  - Succession / rotation ensures no single operator becomes a bottleneck

**Signs the embedding model is broken:**
- Same 2 operators involved for 12+ months straight (burnout risk)
- Operator Council members haven't worked a real shift in months (currency loss)
- Operators volunteer out of the project (disengagement signal)
- Sprint reviews have no operators present (shows priority collapse)

If you see these signs, fix them immediately. Operator engagement quality is the early warning system for UAT and adoption risk.

### 2.7 Organizational Risks Specific to This Model

| Risk | Mitigation |
|------|-----------|
| Vendor becomes de facto architect | Internal lead architect role filled and empowered; vendor advises, doesn't decide |
| Vendor slow-rolls knowledge transfer to preserve engagement | Milestone-based payments tied to KT artifacts |
| Internal team deferential to vendor on legacy "that's how it is" patterns | Challenge culture: every legacy pattern must be justified, not assumed |
| Internal team under-resourced vs. day-job demands | Explicit protected capacity; program staffing matrix reviewed monthly |
| Operators lose currency in operations while embedded | Rotation model; operational currency tracked as a metric |
| Vendor and internal engineers don't integrate socially | Co-location or heavy real-time collaboration; shared tooling; joint team events |
| Critical knowledge lives only in vendor staff | Pair programming mandated; KT artifact produced per integration; no solo vendor work |
| Internal team builds skills but has no career growth post-program | Explicit career pathing plan; hypercare leads to product team roles |

---

## 3. On-Prem Architecture — What Actually Changes

### 3.1 What Doesn't Change

These remain valid regardless of deployment target:
- Modular monolith or strategically decomposed service boundaries
- Event-sourcing for operational core
- API-first integration design
- OpenAPI specifications, ADRs, C4 model
- Test pyramid and test automation
- Observability (metrics, logs, traces) — though the stack differs
- Feature flags, trunk-based development, code review discipline
- Security controls, secrets management

### 3.2 What Does Change

| Area | Cloud-Native Assumption | On-Prem, No Container Reality |
|------|-------------------------|-------------------------------|
| Deployment unit | Container image | OS package (RPM / MSI) or self-contained runnable (JAR, .NET assembly) |
| Orchestration | Kubernetes | systemd / Windows Services, app server clusters |
| Environment provisioning | Minutes (cloud API) | Hours to days (VM provisioning + config) |
| Scaling | Elastic (auto-scaling groups) | Fixed capacity, manual scale |
| Load balancing | Managed (ELB, Azure LB) | Hardware LB (F5, Citrix) or software (HAProxy, nginx) |
| Storage | Managed block / object | SAN / NAS / local disk |
| Database HA | Managed (RDS Multi-AZ) | Self-managed (Patroni for Postgres, Always On for SQL Server, Oracle Data Guard) |
| DR | Cross-region managed | Secondary datacenter, manual or semi-automated failover |
| Secrets | Cloud secrets manager | HashiCorp Vault on-prem |
| Observability | Cloud-native stack | Self-hosted Prometheus/Grafana, ELK, or commercial on-prem |
| CI/CD agents | Ephemeral cloud runners | Persistent on-prem agents |
| Network isolation | VPC / security groups | VLAN / firewall rules on physical/virtual infrastructure |

### 3.3 Architecture Style Decision

Given no containers + internal team:

**Recommendation: Modular Monolith** as the default, with a small number of strategically separated processes for isolation reasons:

- **Primary application** — one deployable containing the operational core (cards, switching orders, clearances, operator dashboard). Internally modular but deployed as one unit.
- **SCADA event ingestion service** — separate process. Isolated for resilience (storm-day event flood should not take down the UI). Distinct scaling and operational characteristics.
- **Integration gateway** — separate process for outbound integrations (field work, notifications). Isolates third-party failures.
- **Reporting / analytics backend** — separate process reading from replicas. Completely independent lifecycle.
- **Batch / scheduled job runner** — separate process for maintenance jobs.

**Why not full microservices:** Without containers and orchestration, microservices are an operational burden your team will pay for daily. Each service is another VM to patch, another deployment pipeline, another failure mode, another network hop. A modular monolith delivers most of the maintainability benefits with a fraction of the operational cost.

### 3.4 Recommended Technology Stack (On-Prem, No Containers)

This is opinionated. Adjust based on existing enterprise standards.

| Layer | Primary Recommendation | Alternative | Why |
|-------|------------------------|-------------|-----|
| Language / Framework (backend) | **Java 21 + Spring Boot 3.x** | .NET 8 + ASP.NET Core | Mature, deployable as fat JAR with systemd, excellent utility-industry presence, strong library ecosystem for messaging, DB, observability |
| Language / Framework (frontend) | **TypeScript + React** | Angular, Vue | Strongest ecosystem; widely known; deploys as static assets behind nginx |
| Database (OLTP) | **PostgreSQL 16** with Patroni for HA | SQL Server (if Microsoft shop) or Oracle (if existing investment) | Open source, excellent HA story, JSON support, partitioning, full-text, reliable |
| Database (read replicas, reporting) | PostgreSQL streaming replicas | — | Same engine, operationally simple |
| Messaging | **Kafka** (on-prem, 3-broker cluster minimum) | RabbitMQ (simpler but lower throughput) | Storm-day event volume requires Kafka throughput; replayable for parallel run |
| Caching / hot reads | **Redis** (with Sentinel or Cluster for HA) | — | Standard for session state, query cache |
| Graph queries (if needed for network topology) | **PostgreSQL with recursive CTEs** or **Neo4j** (if topology depth exceeds SQL comfort) | — | Start with Postgres, upgrade only if proven need |
| Web server / reverse proxy | **nginx** | HAProxy, F5 hardware LB | Battle-tested, scriptable, TLS termination |
| Secrets management | **HashiCorp Vault** (self-hosted, HA) | — | Non-negotiable; don't use config files for secrets |
| Identity Provider | **Active Directory / Entra** (integrate with) + **Keycloak** as OIDC gateway if needed | Existing enterprise IdP | SSO to operator workstations |
| Build system | Maven (Java) or Gradle | — | Conventional |
| Source control | **GitLab CE** (self-hosted) or **GitHub Enterprise Server** (if existing) | Azure DevOps Server | Git + CI + artifact registry + container registry (for future flexibility) in one |
| CI/CD | **GitLab CI** or **Jenkins** | Azure DevOps Server | Self-hosted runners; pipeline-as-code |
| Artifact repository | **JFrog Artifactory** or **Sonatype Nexus** | — | Stores JARs, npm packages, OS packages |
| Configuration management | **Ansible** | Puppet, Chef | Agentless, simpler, large community, excellent for VM deployment |
| VM provisioning / IaC | **Terraform (VMware vSphere provider)** + **Packer** for VM images | — | Immutable infrastructure even without containers |
| Observability — metrics | **Prometheus + Grafana** (self-hosted) | Zabbix (if existing) | Open source, utility-friendly, scales well |
| Observability — logs | **Elastic Stack (ELK)** or **Grafana Loki** | Splunk (if existing enterprise license) | Centralized logging mandatory |
| Observability — tracing | **Jaeger** or **Grafana Tempo** | — | OpenTelemetry emit; pick receiver based on stack |
| APM | **Dynatrace (managed on-prem)** or **Elastic APM** | New Relic, AppDynamics | Optional but valuable; budget dependent |
| SIEM | **Splunk** or **Elastic Security** | QRadar, Sentinel | Likely corporate-standard already |

### 3.5 Physical / Virtual Infrastructure Design

**Minimum recommended environment topology per environment:**

```
                           ┌──────────────────────────┐
                           │    Load Balancer (HA)    │
                           │    F5 active/passive     │
                           └──────────────┬───────────┘
                                          │
                    ┌─────────────────────┼────────────────────┐
                    v                     v                    v
            ┌──────────────┐     ┌──────────────┐     ┌──────────────┐
            │ App Node 1   │     │ App Node 2   │     │ App Node 3   │
            │ (VM)         │     │ (VM)         │     │ (VM)         │
            └──────┬───────┘     └──────┬───────┘     └──────┬───────┘
                   │                    │                    │
                   └────────────────────┼────────────────────┘
                                        │
              ┌─────────────────────────┼────────────────────────┐
              v                         v                        v
      ┌───────────────┐         ┌───────────────┐        ┌───────────────┐
      │  SCADA        │         │   Postgres    │        │    Kafka      │
      │  Ingestion    │         │   Primary +   │        │    Cluster    │
      │  Service (VM) │         │   Replicas    │        │    (3 nodes)  │
      └───────────────┘         │   (Patroni)   │        └───────────────┘
                                └───────────────┘
                                        │
                                        v
                                ┌───────────────┐
                                │   Redis HA    │
                                │  (Sentinel)   │
                                └───────────────┘

      + Vault (HA)   + Prometheus/Grafana   + Log aggregation   + Bastion/jump
```

**Sizing rule of thumb for production (adjust by measurement):**

| Tier | Nodes | VM Spec (typical) |
|------|-------|-------------------|
| App tier | 3+ | 8 vCPU / 32 GB / 100 GB |
| SCADA ingestion | 2 (active/passive or active/active) | 8 vCPU / 16 GB / 200 GB |
| Integration gateway | 2 | 4 vCPU / 16 GB / 100 GB |
| Database primary | 1 | 16 vCPU / 128 GB / 2+ TB fast storage |
| Database replicas | 2+ | Same as primary |
| Kafka brokers | 3 | 8 vCPU / 32 GB / 1 TB per broker |
| Redis | 3 (1 primary + 2 replicas + Sentinel) | 4 vCPU / 16 GB |
| Vault | 3 (Raft) | 4 vCPU / 8 GB |
| Observability stack | 3–5 | 8 vCPU / 32 GB each |
| Reverse proxy / LB | 2 (HA pair) | 4 vCPU / 8 GB |
| Reporting | 2 | 8 vCPU / 32 GB |

**Total production footprint: ~25–35 VMs depending on service decomposition and redundancy.**

### 3.6 Scale for Storm Day

Without elastic scaling, you provision for peak. Peak for a switching/restoration system during major storm events is **5–10× normal load**. Specifically:
- SCADA event ingestion: 1,000–10,000 events/minute peak (vs. 10–100/min normal)
- Concurrent active cards: 100s–1000s (vs. 10s normal)
- Dashboard users concurrent: 2–3× normal as supervisors, support staff, and additional operators come online
- Integration outbound: field work dispatch queue depth spikes

**Storm-day provisioning strategy:**

1. **Capacity test at 3× your worst-observed storm.** If your history shows peak 3,000 events/minute, test at 9,000. Unknown unknowns are why.
2. **Headroom target: 50% CPU / 60% memory at expected peak.** If you hit design peak at 95% utilization, one variance and you're down.
3. **Burst capacity in DR site:** DR site sits warm; during forecasted major storms, operations can activate read replicas and additional app nodes in DR for 1.5–2× total capacity.
4. **Graceful degradation:** Under extreme load, system sheds non-critical work (reporting queries deferred, dashboard refresh rates reduced, non-critical notifications queued). Never drop safety-critical operations.
5. **Backpressure:** Kafka provides natural buffering between SCADA and downstream processing. Design so downstream slowness doesn't lose events — they queue and catch up.

### 3.7 Disaster Recovery — On-Prem Reality

**Target RPO/RTO realistic for on-prem dual-datacenter:**

| Tier | RPO | RTO | Pattern |
|------|-----|-----|---------|
| Database | < 5 min | < 30 min | Synchronous replication to DR (if <10ms latency between DCs) or near-synchronous; documented manual failover |
| Application | N/A (stateless) | < 15 min | Warm standby in DR; DNS or LB cutover |
| Kafka | < 1 min | < 15 min | MirrorMaker to DR cluster |
| Storage (logs, artifacts) | < 1 hour | < 1 hour | Async replication |
| Full site failover | < 30 min | < 1 hour | Tested quarterly |

**DR exercises are not optional:**
- Full failover drill quarterly (tabletop minimum, actual failover yearly)
- Document go/no-go decision criteria (declared disaster vs. regional issue)
- Runbook rehearsed until it fits on one page

**What does NOT meet RPO/RTO targets without explicit design:**
- Tape backups ("we'll restore from backup") — hours or days, not minutes
- Single-DC "high availability" only — resilient to node failure, not site failure
- DR site with cold hardware — days to provision

---

## 4. CI/CD and Environments Without Containers

### 4.1 Deployment Pipeline

```
 Developer commits
        │
        v
 ┌──────────────┐    ┌──────────────┐    ┌──────────────┐
 │  CI Build    │───>│  Package     │───>│  Artifact    │
 │  - compile   │    │  - JAR/WAR   │    │  repo        │
 │  - unit test │    │  - RPM (opt) │    │  (Artifactory│
 │  - SAST      │    │  - checksums │    │   / Nexus)   │
 │  - SCA       │    │  - sign      │    └──────┬───────┘
 └──────────────┘    └──────────────┘           │
                                                v
                     ┌──────────────────────────┴─┐
                     v                            v
              ┌──────────────┐             ┌──────────────┐
              │ Deploy CI env│             │  Manual gate │
              │  (Ansible)   │             │   for higher │
              │  run tests   │             │   envs       │
              └──────────────┘             └──────┬───────┘
                                                  │
                                                  v
                                          ┌──────────────┐
                                          │  Deploy UAT  │
                                          │  (Ansible)   │
                                          └──────┬───────┘
                                                 │
                                                 v
                                          ┌──────────────┐
                                          │  Deploy Prod │
                                          │  (Ansible)   │
                                          │  Blue/green  │
                                          │  or rolling  │
                                          └──────────────┘
```

### 4.2 Deployment Patterns (No Containers)

**Rolling deployment across app tier:**
1. Ansible takes one node out of LB rotation
2. Stop service
3. Install new package version (RPM or equivalent)
4. Start service; health check passes
5. Return to LB rotation
6. Move to next node
7. Repeat until all nodes updated

**Blue/green deployment:**
1. Deploy new version to standby cluster (green)
2. Run smoke tests against green
3. Switch LB from blue to green (instant cutover)
4. Monitor; roll back via LB switch if issues
5. Blue becomes next deployment target

**Database migrations:**
- Forward-only schema changes (no destructive operations in release)
- Expand-migrate-contract pattern for breaking changes
  - **Expand:** add new column/table alongside old
  - **Migrate:** dual-write from application; backfill old data
  - **Contract:** remove old column/table in later release after confirmation
- Schema migration tool: **Flyway** or **Liquibase**
- Migrations applied automatically on deploy (low-risk changes) or manually during maintenance window (high-risk)

### 4.3 Environment Strategy

Without container ephemerality, you get fewer environments. Make them count:

| Environment | Purpose | Persistence | Data |
|-------------|---------|-------------|------|
| Developer workstation | Local dev | Per-developer | Synthetic; embedded DB or Docker Compose for local dependencies (dev can use containers locally for ease; production doesn't) |
| Dev / Integration | Shared integration | Persistent | Synthetic, refreshed weekly |
| CI / Build env | Automated testing | Ephemeral VMs or reset weekly | Synthetic |
| Test | QA testing, exploratory | Persistent | Masked prod-like |
| Performance | Load and performance testing | Persistent (but powered down when not in use) | Masked, production-volume |
| UAT | Business testing | Persistent | Masked production-like, refreshed per milestone |
| **Parallel Run** | Production-parallel new system | Persistent | Live SCADA feed, live integrations (shadow mode) |
| Staging | Pre-production final validation | Persistent | Production-like |
| Production | Live | Persistent | Live |
| DR | Disaster recovery | Persistent, warm | Replicated from prod |
| Training | Operator training | Persistent | Synthetic scenario library |

**Tip:** Developer workstations CAN use Docker Compose for local dependency (Postgres, Kafka, Redis). This doesn't violate "no containers in production" — it's a local developer tool. This is a common pragmatic middle ground.

### 4.4 Release Cadence

- **Dev / Integration:** continuous (per merge)
- **Test:** daily or on-demand
- **UAT:** per milestone, typically bi-weekly during build, more often near UAT close
- **Parallel Run:** weekly during stabilization
- **Production:** during build phase, aim for every 2 sprints (4 weeks). Post-hypercare, aim for weekly to bi-weekly.
- **Production (steady state):** at least monthly. Letting cadence slip past monthly makes each release riskier.

**Release windows for production:**
- Avoid storm season completely for major releases
- Avoid end-of-month billing cycle impact if any
- Standard window: Tuesday/Wednesday morning low-traffic period
- Major releases: planned maintenance windows with full comms

### 4.5 Observability on On-Prem Stack

Target stack and what each answers:

| Question | Tool | How |
|----------|------|-----|
| What's the current system state? | Grafana dashboards | Aggregate from Prometheus metrics |
| What happened at 03:14:22? | Kibana / Grafana | Search structured logs with correlation ID |
| Why is this request slow? | Jaeger / Tempo | Distributed trace across services |
| Is service X up? | Prometheus blackbox + Grafana alerting | HTTP/TCP health checks |
| Are we meeting SLOs? | Grafana with SLO dashboards | Burn rate alerts |
| Audit: who did what, when? | Structured audit log → Elastic | Immutable append, separate index |
| Security: anomalous activity? | SIEM (Splunk / Elastic Security) | Rules + analytics on logs |
| Business KPI: card creation rate? | Grafana with business dashboards | Prometheus custom metrics or DB queries |

**Required from day one:**
- Structured JSON logs with correlation IDs
- OpenTelemetry instrumentation
- Prometheus metrics on every service (RED: Rate, Errors, Duration)
- Alerts tied to SLOs, not arbitrary thresholds
- Dashboard per service + business KPI dashboards
- Runbook link on every alert (even if runbook is "see dashboard X and page on-call")

---

## 5. Adjustments to Architecture Recommendations

### 5.1 Previously Recommended, Now Adjusted

| Previous Recommendation | Adjusted for On-Prem, No Containers |
|-------------------------|-------------------------------------|
| Container + Kubernetes deployment | VM-based with Ansible; systemd for Linux, Windows Services for .NET |
| Ephemeral environments | Fewer, persistent environments; disciplined refresh |
| Auto-scaling for storm day | Fixed capacity provisioned for peak; burst to DR if needed |
| Managed DB (RDS) | Self-managed PostgreSQL with Patroni for HA |
| Cloud-native observability | Prometheus + Grafana + ELK self-hosted |
| Terraform for cloud resources | Terraform for VMware + Ansible for config; Packer for VM images |
| Cloud secrets manager | HashiCorp Vault on-prem |
| Blue/green via K8s / Istio | Blue/green via LB config change |
| Canary via service mesh | Canary via LB weight adjustment; or feature flags as the real canary mechanism |

### 5.2 Still Strongly Recommended

- **Event sourcing + CQRS for operational core** — works identically on-prem
- **Modular monolith** — even more appropriate for on-prem than for cloud
- **Outbox pattern** — non-negotiable for field work dispatch
- **Anti-corruption layer for vendor integrations** — non-negotiable
- **API-first with OpenAPI** — no change
- **Feature flags** — arguably more important without container-based canaries
- **Trunk-based development** — no change
- **Test pyramid + automation** — no change
- **Immutable infrastructure** — achievable via Packer-built VM images
- **OpenTelemetry** — standard regardless of deployment target

### 5.3 What to Be More Careful About

- **Stateful services on single nodes.** Without container orchestration's self-healing, stateful service crashes require manual intervention or Ansible-driven recovery. Document recovery procedures thoroughly.
- **Deployment atomicity.** No "pod replacement." Rolling deployment means version mix during deploy. Design for backward/forward compatibility between versions.
- **Resource contention.** VM-based means services share OS resources. Resource limits less enforced. Monitor noisy neighbors on shared hardware.
- **Secrets rotation.** Vault handles this well; ensure Ansible/app integrations pull fresh secrets on each deploy or use Vault Agent for dynamic rotation.
- **Log volume.** ELK sizing is often under-estimated. Storm-day logs can be 50–100× normal. Size and tune accordingly.

---

## 6. Revised Resource and Cost Considerations

### 6.1 Revised Team Composition

For internal-led with vendor support and on-prem:

| Role | Internal FTE | Vendor FTE | Notes |
|------|--------------|------------|-------|
| Program Manager | 1 | 0 | Owns program |
| Project Managers | 2–3 | 0 | Per workstream |
| Solution Architect (Lead) | 1 | 0 | Internal owns architecture |
| Architects — data, integration, security, infrastructure | 3–4 | 1 (advisory) | Infrastructure architect new vs. cloud model |
| Senior Engineers (tech leads) | 4–6 | 2 | Per squad |
| Mid Engineers | 6–12 | 2–3 | |
| Junior Engineers | 2–4 | 0 | Growth path |
| Data Engineers | 2–3 | 2 (vendor leads migration) | Vendor-heavy here |
| DBAs | 2 | 0 | On-prem DB ops |
| Infrastructure Engineers | 3–4 | 0 | VMware, network, storage |
| DevOps / Release Engineers | 2–3 | 0 | On-prem CI/CD, Ansible |
| Security Engineer | 1–2 | 0 | NERC CIP lead |
| QA Engineers | 4–6 | 0 | Automation focus |
| Performance Engineer | 1 | 0 | Storm-day testing |
| UX Designer | 1–2 | 0 | Paired with operators |
| UX Researcher | 1 | 0 | |
| Technical Writer | 1 | 0 | |
| Business Analysts | 3–4 | 2 (domain experts) | Vendor brings legacy BR knowledge |
| Scrum Masters | 4–5 | 0 | Per squad |
| Product Owners | 4–5 | 0 | Internal |
| Release / Cutover Manager | 1 | 0 | |
| OCM / Training Lead | 1 | 0 | |
| Operator Council (part-time) | 3–4 @ 50% | 0 | Rotating |
| **Total** | **~55–75 FTE internal** | **~8–12 FTE vendor** | |

**Honest observation:** Infrastructure engineers (+1) and DBAs (+1) over the cloud-native model because you own more operational depth. Don't under-staff these roles — they're the ones who keep the system running at 3am during a storm.

### 6.2 Capacity Planning Honesty

- New team members: expect 3–6 months to reach full productivity on domain-specific work
- Vendor engineers familiar with legacy will be productive immediately but less so on modern patterns
- Internal engineers new to the domain will be productive on modern patterns but slow on legacy-specific business rules
- Pair them deliberately; productivity comes from the combination

### 6.3 Budget Line Items Often Missed (On-Prem)

- Hardware refresh cycle during program (5+ year program may hit a refresh)
- Software licenses renewed: Oracle, Microsoft, commercial APM, SIEM, backup software
- Vendor contract change orders
- Third-party security audit / pen test (annual)
- NERC CIP external audit preparation
- DR exercise costs (equipment + staff time)
- Training budget for operators AND for engineering staff (continuous skill development)
- Documentation and training content production (often 10–15% of a module's cost)
- Hypercare staffing (elevated on-call for 30–90 days)

---

## 7. Top 10 Brutal-Honest Risks (This Model)

Ranked by likelihood × impact:

1. **Internal team capability gap emerges mid-build.** Assessment said "yes" but reality surfaces as the system scales. Mitigation: real capability assessment now, hire/contract to close gaps before sprint 1 of build, retain a "glass-break" SI relationship for escalation.

2. **Vendor becomes long-term dependency despite intent.** Exit criteria not enforced; vendor knowledge not transferred; internal team defers. Mitigation: milestone-based KT artifacts; quarterly reviews of vendor exit plan; escalation when KT deliverables slip.

3. **Operators rotate out under operational pressure; product direction drifts.** Grid needs them more than project does during busy seasons. Mitigation: contractual commitment of operator time from operations leadership; rotation plan; backfill funding.

4. **Storm hits during parallel run or near cutover.** Mitigation: calendar-based cutover windows avoiding storm season; 48hr weather-informed go/no-go before cutover; ability to pause parallel run without losing state.

5. **On-prem velocity assumed to match cloud-native velocity in plans.** Sponsors benchmark incorrectly; team committed to unrealistic dates. Mitigation: set expectation explicitly from day one; track velocity transparently; re-baseline when reality diverges.

6. **Safety-critical business rules in legacy are not documented; vendor staff rotated off before transferring.** Tribal knowledge. Mitigation: mandatory BR catalog as vendor deliverable; pair coding on all business-rule-heavy code; test coverage mapping to each BR.

7. **NERC CIP scope expansion surfaces late.** Transmission-related components declared CIP-applicable after build started; controls gap. Mitigation: CIP scoping assessment completed in design phase with compliance lead; design assumes CIP-applicable until ruled out.

8. **Data migration complexity underestimated.** Vendor asserts "just an export"; reality has 47 edge cases and corrupted historical data. Mitigation: source profiling in design phase; 3+ dress rehearsals; reconciliation reports.

9. **Feature parity contested during UAT.** Operators claim features were missed; program says they were delivered; both partly right. Mitigation: feature parity matrix tracked weekly with explicit status; operator sign-off per module, not at end only.

10. **Cutover forced to meet a fiscal-year or regulatory-filing date.** Schedule wins over readiness. Mitigation: readiness criteria documented and agreed with steering committee before schedule pressure builds; empower go/no-go to pause.

---

## 8. Decisions to Make in the First 60 Days

These should be locked before serious design work accelerates. Each is an ADR or governance decision.

| # | Decision | Why Now |
|---|----------|---------|
| 1 | Vendor contract: IP, KT obligations, exit criteria, staffing | Locks the partnership on healthy terms |
| 2 | Internal team structure and hiring plan | 3–6 month hire cycle; delays compound |
| 3 | Technology stack (language, DB, messaging) | Affects everything downstream; hard to change |
| 4 | Architecture style (modular monolith vs. others) | Drives team structure and deployment model |
| 5 | Infrastructure topology and capacity plan | Hardware/VM provisioning lead time is months |
| 6 | Environment strategy (how many, naming, data refresh) | Enables CI/CD build-out |
| 7 | CI/CD tooling stack | Enables sprint 1 productivity |
| 8 | Security and compliance baseline (NERC CIP scoping) | Shapes controls in every subsequent design |
| 9 | Data migration strategy at conceptual level | Source profiling starts immediately |
| 10 | Operator Council composition and operating agreement | Locks in the engagement model |
| 11 | Cutover strategy at pattern level (phased parallel run confirmed) | Shapes parallel-run architecture decisions |
| 12 | NFR acceptance (already done per your note) — publish and reference | Makes NFRs a fixed input, not a debate |
| 13 | Governance cadence and decision rights | Prevents decision paralysis later |
| 14 | Program budget and contingency | Sets the realistic envelope |
| 15 | Risk register and review cadence | Makes risk visible, actionable |

---

## 9. What to Do If Executive Pressure Compresses Your Timeline

This will happen. Here's how to respond constructively.

### 9.1 What You Can Legitimately Cut

- **Non-MVP features** — defer to post-cutover roadmap
- **Non-critical integrations** — delay, batch instead of real-time
- **Advanced analytics** — post-go-live
- **Mobile interfaces** — post-go-live unless critical
- **Nice-to-have UX enhancements** — preserve workflow parity, defer polish
- **Non-Transmission modules** — if Distribution-only first cutover is operationally viable

### 9.2 What You Cannot Cut (and Must Defend)

- **Safety test suite and safety-critical business rules**
- **Audit logging completeness**
- **NERC CIP compliance controls (if applicable)**
- **Parallel run duration** — this is the single largest risk reducer
- **Data migration dress rehearsals** — every skipped rehearsal is a 10× risk multiplier
- **Operator training**
- **DR testing**
- **Rollback rehearsal**

### 9.3 The Honest Conversation with Sponsors

If pressure to compress is persistent, have this conversation openly:

> "We can commit to MVP by date X. Full scope by date Y. Anything earlier means we're cutting safety testing, parallel run, or training — each of which raises the probability of a post-cutover incident. Here are the three things we can cut from scope to pull in schedule: [list]. Here are the three things we will not cut: [list]. Which trade-off do you want?"

This reframes schedule pressure as a scope-or-risk decision, not an engineering-excellence question. It also documents the trade-off for when (not if) issues arise later.

---

## 10. Closing Honest View

This plan — internal team, vendor as partner, operators embedded, on-prem, no containers — is coherent and defensible. It will not be the fastest path; it will be more sustainable than the alternatives. The two things most likely to derail it are (1) underestimating internal team capability requirements and (2) letting schedule pressure compress testing and parallel run.

The two things most likely to make it succeed are (1) genuine partnership discipline with the vendor (clear role, exit criteria enforced) and (2) sustained, rotated operator engagement backed by operations leadership commitment.

The program will take 40–55 months realistically. Plan for that, communicate that, and deliver against it. Under-promising by six months and delivering is worth more than over-promising by six months and missing.

Good programs in this class are unglamorous. Disciplined decomposition, honest estimates, narrow MVP, parallel-run UAT, phased cutover, patient hypercare. No heroics required. Heroics are a failure mode.
