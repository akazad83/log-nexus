# Legacy System Modernization Playbook
## Electric Utility Mission-Critical Application Suite

> **Scope:** End-to-end guidance for translating a completed Discovery Phase into detailed design, build, test, cutover, and hypercare — with feature parity to the legacy system plus enhanced business value.

---

## 0. Program at a Glance

### 0.1 Program Phases (Post-Discovery)

| # | Phase | Purpose | Exit Gate |
|---|-------|---------|-----------|
| 1 | **Detailed Design** | Decompose requirements; produce architecture, data, UX, integration, network, and security designs | Design Authority Board (DAB) approval; baselined backlog; signed NFRs |
| 2 | **MVP Build** | Deliver a narrow, end-to-end vertical slice demonstrating parity on the core workflow with enhanced UX | MVP UAT sign-off; production-like environment passing NFRs |
| 3 | **Incremental Build to Parity** | Deliver remaining feature parity + enhancement stories sprint by sprint | Feature parity matrix 100% green; regression suite stable |
| 4 | **System Integration Testing (SIT)** | End-to-end testing across all integrated systems | SIT exit report; defect profile acceptable |
| 5 | **User Acceptance Testing (UAT)** | Business validation by real operators, ideally in shadow/parallel mode | Signed UAT sign-off per module |
| 6 | **Cutover** | Migrate data, switch traffic, retire legacy | Post-cutover validation pass; rollback unused |
| 7 | **Hypercare & Stabilization** | Elevated support; residual defects; optimization | Hypercare exit (typically 30–90 days) |

### 0.2 Governance Cadence

| Forum | Frequency | Purpose |
|-------|-----------|---------|
| Steering Committee | Monthly | Budget, risk, scope changes, executive alignment |
| Design Authority Board (DAB) | Bi-weekly | Architecture decisions (ADRs), NFR deviations |
| Change Advisory Board (CAB) | Weekly during build, daily near cutover | Production changes, release approvals |
| Scrum of Scrums | Weekly | Cross-team dependencies |
| Sprint Ceremonies | Bi-weekly | Planning, review, retro per squad |
| Daily Stand-up | Daily | Team-level sync |

### 0.3 Honest Timeline Expectations

For a mission-critical electric utility suite with strict feature parity requirements, realistic durations are:

| Phase | Duration (Realistic) | Duration (Aggressive — higher risk) |
|-------|----------------------|-------------------------------------|
| Detailed Design | 4–6 months | 3 months |
| MVP Build | 6–9 months | 4–5 months |
| Incremental Build to Parity | 12–24 months | 9–12 months |
| SIT | 2–3 months (overlapped) | 1–2 months |
| UAT (incl. parallel run) | 3–6 months | 2 months |
| Cutover | 2–6 weeks | 2 weeks |
| Hypercare | 30–90 days | 30 days |
| **Total** | **2.5–4 years** | **18–24 months (high risk)** |

> **Brutal honesty:** Vendors who promise utility modernization in under 18 months either (a) scope out the hard integrations, (b) plan to cut over before parity is proven, or (c) underestimate regulatory/compliance cycles (NERC CIP revisions, PUC filings). Budget for 2.5–4 years and communicate that clearly to executive sponsors. Cost overruns on utility modernizations are most often caused by **commitment to unrealistic dates**, not by engineering failure.

---

## 1. Phase 1 — Detailed Design

### 1.1 Requirements Decomposition Hierarchy

```
Strategic Theme
   └── Initiative (quarterly/annual business objective)
        └── Epic (large body of work, 1–3 sprints minimum, often much larger)
             └── Feature (user-recognizable capability)
                  └── User Story (INVEST-compliant, fits in one sprint)
                       └── Task (engineering unit of work, ≤1–2 days)
```

**Rules of thumb:**
- An **Epic** should be decomposable into 5–20 stories and usually maps to a feature-catalog entry from Discovery.
- A **User Story** is *vertical* (UI + logic + data + integration) — never a technical layer. Horizontal splits ("build the database layer") are anti-patterns.
- A **Task** is a technical subdivision of a story: "create API endpoint," "write unit tests," "update schema." Tasks are team-facing; stories are business-facing.

### 1.2 User Story Standard

**Format (Connextra):**
```
As a <persona>,
I want <capability>,
So that <business outcome>.
```

**INVEST checklist** — each story must be:
- **I**ndependent — minimal coupling to other stories
- **N**egotiable — scope can be refined
- **V**aluable — delivers user or business value
- **E**stimable — team can size it
- **S**mall — fits in one sprint
- **T**estable — has verifiable acceptance criteria

**Acceptance criteria — Gherkin (Given/When/Then):**
```gherkin
Feature: Outage ticket creation from SCADA alarm

  Scenario: Auto-create outage ticket on breaker trip alarm
    Given a breaker "BR-4471" is monitored by SCADA
      And the breaker is currently in "CLOSED" state
    When SCADA publishes a "BREAKER_TRIP" event for "BR-4471"
    Then the system SHALL create an outage ticket within 5 seconds
      And the ticket SHALL be assigned to the "Distribution Operations" queue
      And affected customers SHALL be calculated via the connectivity model
      And a notification SHALL be sent to the on-shift dispatcher
```

Keep scenarios focused — one behavior per scenario. Use Scenario Outlines for parameterized cases.

### 1.3 Backlog Construction Workflow

1. **Import the feature catalog** from Discovery into the ALM tool (Jira / Azure DevOps).
2. **Group features into Epics** — by business capability, not by technical layer. Typical utility epics: Outage Management, Customer Information, Work Order Management, Asset Management, Metering/AMI Integration, Billing, Reporting, SCADA Integration, GIS Integration, Mobile Workforce, Customer Portal.
3. **Define feature parity matrix** — a row per legacy feature with columns: Feature Name, Legacy Behavior, Target Behavior (parity / enhanced / deprecated), Epic, Priority (MoSCoW), MVP candidate (Y/N), Risk.
4. **Decompose each feature into stories** via story mapping workshops with SMEs and operators.
5. **Write acceptance criteria** with the business rules catalog from Discovery as the source of truth — every business rule maps to at least one AC line.
6. **Prioritize** using a scoring model: WSJF (Weighted Shortest Job First) or RICE. Core dispatcher workflow stories rank highest.
7. **Estimate** — Planning Poker with Fibonacci story points. Calibrate against a reference story.
8. **Definition of Ready (DoR)** — story must pass before entering a sprint (template in Appendix A.3).

### 1.4 MVP Definition Criteria

Given the strict user satisfaction with existing workflows, the MVP must be carefully scoped. Use these criteria:

- **Full workflow, narrow data scope** — e.g., outage management end-to-end for one operating region, not half of outage management for all regions.
- **Must include all integration touchpoints** on the critical path (SCADA-in, CIS-read, GIS-read, notifications-out). Stubbed integrations hide risk.
- **Must demonstrate NFRs** — not just functional behavior. Performance, availability, and security must be provable at MVP.
- **Must include the UX patterns users care about** — not a wireframe demo.
- **MVP target: 3–6 months of build after design, with parity on one complete user journey plus one enhanced capability.**

The MVP is not a throwaway — it is the first production-quality increment. Architect the MVP assuming it will evolve into the full system.

### 1.5 Technical Architecture Deliverables

**Use the C4 Model** for architecture documentation — it scales and stays readable:

| Level | Artifact | Audience |
|-------|----------|----------|
| 1 — Context | System context diagram (system + external actors/systems) | Executives, business stakeholders |
| 2 — Container | Containers (apps, services, databases, queues) and their interactions | Architects, senior engineers |
| 3 — Component | Components within each container | Development teams |
| 4 — Code | Class/module diagrams (rarely maintained; generate on demand) | Developers |

Supplement with:
- **Sequence diagrams** for critical flows (outage creation, restoration, meter read ingestion)
- **Deployment diagrams** per environment
- **Integration landscape diagram** — all systems, protocols, data directions, SLAs
- **Architecture Decision Records (ADRs)** — see Appendix A.4. One ADR per significant decision: language, framework, message broker choice, DB choice, auth model, etc.

**Cross-cutting architecture decisions to lock down in this phase:**

| Area | Decision Points |
|------|-----------------|
| Style | Monolith vs. modular monolith vs. microservices. For utility: **modular monolith** is usually right — avoid premature microservice proliferation. |
| API | REST vs. GraphQL vs. gRPC. OpenAPI-first is non-negotiable. |
| Async | Message broker choice (Kafka, RabbitMQ, Azure Service Bus). Required for SCADA event ingestion, asynchronous workflows. |
| Auth | OIDC/OAuth 2.1 with central IdP. SSO to AD/Entra. MFA enforced. |
| Auth-Z | RBAC + ABAC. Policy-as-code (OPA) for complex rules. |
| Observability | OpenTelemetry standard. Correlate logs/metrics/traces. |
| Deploy | Containers + Kubernetes, or managed PaaS. Utility on-prem constraints may drive this. |
| Resilience | Circuit breakers, retries with jitter, bulkheads, idempotency keys. |

### 1.6 Data Architecture Deliverables

1. **Conceptual data model** — business entities and relationships, vendor/tech-neutral.
2. **Logical data model** — normalized (3NF typically), independent of DBMS.
3. **Physical data model** — denormalized where needed for performance, with chosen DBMS specifics.
4. **Data dictionary** — every table/column, data type, constraint, owner, classification (PII, CEII, confidential).
5. **Data lineage map** — source → transformation → target for every data element, covering both runtime flows and migration.

**Database optimization checklist:**

- Indexing strategy (covering, composite, partial) — driven by query patterns, not guesswork
- Partitioning strategy for large tables (outage history, meter reads, audit logs) — by time typically
- Read replicas for reporting workloads; separate OLTP from analytics
- Query plan review for top-N queries; cache layer (Redis) for hot read paths
- Connection pooling sized realistically (not default 10)
- Archival/purge policies — utility data retention often driven by regulator (e.g., 7 years billing, indefinite for some operational records)
- Backup + PITR; tested restore (not just "backups exist")

**Data migration strategy** — treat this as its own sub-project:

| Step | Description |
|------|-------------|
| Source profiling | Row counts, null rates, referential integrity violations, orphaned records, character encoding issues |
| Mapping specification | Per target table: source fields, transformations, defaults, rejects |
| ETL/ELT build | Idempotent, restartable, with checkpoints |
| Reconciliation | Row counts + checksums + business-rule validation (e.g., total customer count matches billing total) |
| Dress rehearsals | Minimum 3 full-volume dry runs before go-live; time each |
| Delta migration plan | Strategy for data changes during the cutover window |
| Exception handling | Rejected-record workflow with business sign-off |

### 1.7 Integration Architecture

Map every integration with these attributes (template in Appendix A.6):

- Direction (inbound/outbound/bidirectional)
- Protocol (REST, SOAP, file, message, database link)
- Sync or async
- Data volume (peak + average)
- Latency SLA
- Availability SLA
- Error handling strategy
- Authentication mechanism
- Owner (internal / vendor)
- Test environment availability

Apply an **anti-corruption layer (ACL)** for every legacy/vendor integration — isolate the new system from legacy schemas and semantics. This pays off during the build and during future vendor changes.

### 1.8 Network & Security Design

**Zone model** (minimum):
- External DMZ (customer portal, public APIs)
- Internal DMZ (partner integrations)
- Application tier
- Data tier
- Management/jump-host tier
- OT boundary (SCADA/ICS systems — strictest controls; often NERC CIP-regulated)

**Security design deliverables:**
- Threat model (STRIDE per major component)
- Data classification + handling rules
- IAM design (roles, groups, JIT access, break-glass)
- Secrets management (Vault, cloud KMS — never env vars or repos)
- Certificate management + rotation
- Logging + SIEM integration
- Incident response runbook
- NERC CIP applicability assessment (if system touches BES Cyber Systems)
- State PUC / regulatory compliance review

### 1.9 UX/UI Design

Because users are satisfied with existing workflows:

1. **Keep the workflow topology** — screen counts, task sequences, and keyboard navigation should match where possible. Operators have muscle memory; breaking it causes rejection.
2. **Modernize the chrome, not the workflow** — visual refresh, accessibility, responsiveness, information density options.
3. **Build a design system** (tokens, components, patterns) before building screens. Storybook or equivalent.
4. **Conduct parity walkthroughs** — operator + designer + SME sit with the legacy system, then confirm the new screen captures every data element and action.
5. **WCAG 2.2 AA** minimum; utility systems often have colorblind users (status indicators).
6. **Usability testing** every 2–3 sprints with real operators. Not just the same two friendly SMEs.

### 1.10 Non-Functional Requirements (NFRs) — Lock These Down Now

Don't let NFRs drift. Baseline and test against:

| NFR | Typical Utility Target |
|-----|------------------------|
| Availability | 99.9% (8.76 hrs downtime/yr) for customer systems; 99.99%+ for operational/SCADA-adjacent |
| RPO / RTO | RPO ≤ 15 min, RTO ≤ 1 hr for critical modules (tune per module) |
| Transaction performance | Dispatcher screens ≤ 2s P95, ≤ 5s P99 under peak storm load |
| Peak concurrency | Storm-day peak: 3–10× normal load. **Design for storm day, not Tuesday afternoon.** |
| Data retention | Per regulatory and business rules |
| Security | Pen test clean; SAST/DAST in pipeline; SBOM maintained |
| Accessibility | WCAG 2.2 AA |
| Localization | As required |

---

## 2. Phase 2 & 3 — Build (MVP + Incremental to Parity)

### 2.1 Squad / Team Structure

Organize by business capability, not technical layer (Team Topologies "stream-aligned teams"):

```
Team A — Outage Management stream
Team B — Customer & Billing stream
Team C — Work & Asset Management stream
Team D — Integration & Data Platform (enabling team)
Team E — Platform / DevOps (platform team)
Team F — UX / Design (enabling team)
```

Each stream-aligned squad: 6–9 people (product owner, tech lead, 3–5 engineers, QA, designer shared). Platform and enabling teams service multiple squads.

### 2.2 Sprint Mechanics

- **Cadence:** 2-week sprints (utility modernization rarely benefits from 1-week; too much ceremony overhead).
- **Ceremonies:** Backlog refinement (mid-sprint, 90 min), Sprint planning (3–4 hrs), Daily stand-up (15 min), Sprint review (1–2 hrs), Retrospective (1 hr), Scrum of Scrums (weekly 45 min).
- **Definition of Ready** — Appendix A.3.
- **Definition of Done** — Appendix A.5.
- **Capacity planning:** Assume 70–75% of calendar time is productive after meetings, support, interrupts. New teams: 50–60%.

### 2.3 CI/CD Pipeline Stages

```
 ┌────────────┐     ┌────────────┐     ┌────────────┐     ┌────────────┐
 │   Commit   │ ──> │    CI      │ ──> │   Staging  │ ──> │  Production│
 │            │     │            │     │   / UAT    │     │            │
 └────────────┘     └────────────┘     └────────────┘     └────────────┘
   Trunk-based      Build, unit,      Deploy from         Promotion
   Feature flags    SAST, SCA,        artifact, DAST,     (manual gate),
   PR review        container scan,   integration,        smoke, canary
   Signed commits   contract tests,   E2E, perf smoke     or blue/green
                    artifact + SBOM
```

**Non-negotiables:**
- Trunk-based development with short-lived branches (≤ 2 days)
- Feature flags for incomplete capability
- Everything as code: infra (Terraform), pipelines, configuration
- Signed artifacts; supply chain attestation (SLSA level appropriate to risk)
- Environment parity (prod, staging, UAT, dev — same topology, smaller sizing)
- Immutable infrastructure; no SSH-to-fix in production
- One-click rollback — tested weekly

### 2.4 Environments Strategy

| Env | Purpose | Data | Lifetime |
|-----|---------|------|----------|
| Dev | Engineer workstation / shared dev cluster | Synthetic or masked | Per-commit ephemeral where possible |
| CI | Automated testing | Synthetic | Ephemeral per build |
| Integration | Cross-team integration | Synthetic + contract stubs | Persistent |
| UAT | Business testing | Masked production-like | Persistent, refreshed per milestone |
| Staging | Pre-prod validation, perf testing | Masked prod-like, full volume | Persistent |
| **Parallel Run** | Run new system alongside legacy with live (or replayed) data | Live or shadow | Persistent, critical for utility |
| Production | Live | Live | Persistent |
| DR | Disaster recovery | Replicated from prod | Persistent |

**Parallel run is the risk-reduction lever that most distinguishes successful utility modernizations from failed ones.** Plan for it from day one.

### 2.5 Observability

From sprint 1:
- Structured logs (JSON), correlation IDs
- Metrics (RED: Rate, Errors, Duration + USE for infra)
- Distributed traces (OpenTelemetry)
- Dashboards per service + business KPIs (e.g., outage ticket creation rate, meter read throughput)
- Alerts tied to SLOs, not metrics thresholds. Error budget policy documented.

---

## 3. Testing Strategy

### 3.1 Test Pyramid

```
           /\
          /  \      E2E (5%) — critical user journeys, run nightly
         /----\
        /      \    Integration (20%) — service + DB + queue + external stubs
       /--------\
      /          \  Contract (10%) — consumer-driven, API boundaries
     /------------\
    /              \ Component / Unit (65%) — fast, deterministic, run per commit
   /----------------\
```

### 3.2 Test Types and Ownership

| Test Type | Tool Examples | Who Owns | When |
|-----------|---------------|----------|------|
| Unit | Language-native (JUnit, xUnit, pytest) | Developers | Every commit |
| Component | In-process integration tests | Developers | Every commit |
| Contract | Pact, Spring Cloud Contract | Developers (both sides) | Every commit |
| Integration | Testcontainers, API tests | Developers + QA | Every commit |
| E2E | Playwright, Cypress | QA | Nightly + pre-release |
| Performance | k6, JMeter, Gatling | Performance engineer | Weekly on staging |
| Security — SAST | SonarQube, Semgrep, CodeQL | DevSecOps | Every commit |
| Security — SCA | Snyk, Dependabot, Trivy | DevSecOps | Every commit |
| Security — DAST | OWASP ZAP, Burp | DevSecOps | Per release |
| Security — Pen test | External firm | Security | Pre-production + annual |
| Accessibility | axe-core, manual | QA + Design | Per release |
| Chaos | Chaos Mesh, Gremlin | Platform | Quarterly (mature stage) |
| DR drill | Manual runbook | Operations | Quarterly |

### 3.3 Shift-Left Principles

- Write tests with the code, not after
- Contract tests protect integration boundaries
- Every defect found in UAT triggers a retrospective: "why didn't a lower-level test catch this?"

### 3.4 Regression Automation

A utility system has thousands of business rules. Manual regression does not scale. Aim for **≥ 80% of critical business rules covered by automated tests** by end of incremental build phase. Build this continuously; do not defer to the end.

---

## 4. User Acceptance Testing (UAT)

### 4.1 UAT Model for Utility Systems

Recommended approach: **Shadow / Parallel Run UAT**

1. Run the new system in parallel with legacy using the same inputs (live or replayed SCADA events, same transactions).
2. Operators work in legacy during their shift; designated UAT operators work in new system on identical scenarios.
3. Reconcile outputs daily; investigate any divergence.
4. Gradually increase new-system operator share (5% → 25% → 50% → 100%) per module.

### 4.2 UAT Planning

- **UAT cohort:** Real operators (not just test engineers or SMEs). Rotate through shifts to hit edge cases.
- **UAT entry criteria:** SIT complete; defect profile — 0 critical, 0 high, ≤10 medium; training delivered; UAT environment refreshed.
- **UAT exit criteria:** All P1/P2 defects closed or deferred with business acceptance; parity matrix validated; parallel-run divergence < defined threshold; sign-off by business owner per module.
- **Training during UAT** — build operator training on the real new system, not screenshots; capture training materials from UAT sessions.
- **UAT duration:** Budget 8–16 weeks. Compressing UAT is the #1 cause of post-cutover disasters.

### 4.3 UAT Sign-off Process

Each module has a business owner who signs off. Sign-off document includes:
- Test scenarios executed and results
- Outstanding defects and business decision per defect (fix-before-cutover, fix-post-cutover, accept-as-change)
- Training completion attestation
- Parity matrix confirmation

---

## 5. Cutover Strategy

### 5.1 Cutover Patterns Compared

| Pattern | Description | Risk | When Appropriate |
|---------|-------------|------|------------------|
| Big Bang | Switch everything at once | **Very High** | Rarely; only when phased impossible |
| Phased by Module | Outage first, then billing, then... | Medium | When modules are loosely coupled |
| Phased by User Group | Region A goes first, then Region B | Medium | When regional segmentation is clean |
| Phased by Functionality | Read-only first, then read-write | Medium-Low | When data-migration complexity is the risk |
| Parallel Run → Cutover | Both systems live, gradually shift, cut when confident | **Lowest — recommended** | Mission-critical systems |

> For an electric utility, **parallel run with phased cutover by module or region** is the industry standard. Big-bang for an outage management system during storm season is a career-limiting move.

### 5.2 Cutover Preparation

- **Cutover runbook** — minute-by-minute plan. Every task has an owner, precondition, success check, rollback trigger, duration estimate. See Appendix A.7.
- **Dress rehearsals** — minimum 3 full rehearsals on production-like environment at full data volume.
- **Data migration dry runs** — each rehearsal times the migration; shrink the cutover window to what you've proven achievable.
- **Cutover window** — typically weekend nights; for utility, avoid storm season and regulatory reporting cycles.
- **Communications plan** — stakeholders, customers, regulators, field crews.
- **Freeze period** — no non-essential changes for 2 weeks before cutover.
- **War room** — physical or virtual; 24/7 during cutover and for first week.

### 5.3 Rollback Planning

**A rollback plan you haven't tested is not a rollback plan.**

- Define rollback trigger criteria explicitly (e.g., critical function failing > 30 min with no fix in sight).
- Rehearse rollback in dress rehearsals.
- Keep legacy system warm and ready for an agreed duration post-cutover.
- Reverse-migration plan: if new system wrote data during live period, how does it flow back to legacy?

### 5.4 Hypercare

30–90 days post-cutover:
- Elevated staffing on support and engineering
- Daily triage meetings for defects
- Daily business KPI review vs. baseline (was there an operational regression?)
- Operator feedback channel (dedicated Slack / Teams with product team)
- Formal hypercare exit review before returning to normal operations

---

## 6. DevOps / Engineering Practice Maturity

| Practice | Target |
|----------|--------|
| Source control | Git, trunk-based, protected branches, signed commits |
| PR reviews | Required; at least one approval; CODEOWNERS enforced |
| Build cadence | On every commit; < 15 min ideal |
| Deploy cadence | On-demand to non-prod; per-sprint minimum to prod during build; weekly+ after go-live |
| Lead time for change | < 1 day (Elite per DORA) aspirational; realistic target: 1–3 days |
| Change failure rate | < 15% |
| MTTR | < 1 hr for P1 incidents after hypercare |
| Deployment approach | Blue/green or canary; feature flags for risky changes |
| Config management | 12-factor; secrets in vault; config in repo |
| Infrastructure | 100% IaC (Terraform + Ansible or equivalent) |
| Documentation | ADRs, runbooks, API docs generated from OpenAPI, all in repo |

---

## 7. Resource Planning (Honest)

### 7.1 Typical Team for Utility Modernization at Scale

| Role | Count | Notes |
|------|-------|-------|
| Program Manager | 1 | Full program accountability |
| Project Managers | 2–3 | Per workstream |
| Scrum Masters | 1 per squad | |
| Product Owner | 1 per squad | Paired with business SMEs |
| Business Analysts | 3–6 | Deep utility domain |
| Solution Architect (Lead) | 1 | |
| Domain Architects | 2–3 | Data, integration, security |
| Tech Leads | 1 per squad | |
| Senior Engineers | 2–3 per squad | |
| Mid/Junior Engineers | 1–2 per squad | |
| QA Engineers | 1–2 per squad + 1 central | |
| Performance Engineer | 1 | Shared across squads |
| DevOps / Platform Engineers | 3–5 | Central team |
| Data Engineers | 2–4 | Migration + ETL |
| Database Administrator | 1–2 | |
| Security Engineer | 1–2 | |
| UX Designer | 1–2 | Shared |
| UX Researcher | 1 | Shared |
| Technical Writer | 1 | Documentation, training |
| Release / Cutover Manager | 1 | Ramps up in build phase |
| OCM / Training Lead | 1 | Critical and often under-resourced |

**Typical total: 40–70 FTE for a medium-large utility modernization.** Significant variance by scope.

### 7.2 Skills that are Always Under-Estimated

- **Utility domain SMEs** — dispatchers, meter specialists, billing analysts embedded with the team. Without them, you will build what engineers *think* utility is, not what it is.
- **Data migration engineering** — specialized skill; one senior data engineer is not enough.
- **Organizational change management (OCM)** — new system rollout fails without it. Budget 5–10% of program cost.
- **Training content development** — not the same as documentation. Plan for it.
- **Production support engineers** trained before go-live — not during.

### 7.3 Budgeting Rules of Thumb

- **Design phase:** ~15% of total program cost
- **Build phase:** ~55%
- **Test + UAT:** ~15% (often under-budgeted)
- **Cutover + Hypercare:** ~10%
- **Reserve:** 15–20% management reserve. Utility modernizations *always* hit surprises. The reserve is for the knowns you don't know yet.

### 7.4 Estimation Anti-Patterns to Avoid

- Estimating only development; ignoring test, deployment, documentation, training, support.
- Anchoring on an arbitrary go-live date and working backward.
- Assuming vendor and third-party systems will respond on your schedule.
- Single-point estimates instead of ranges with confidence intervals.
- Burning through contingency on scope creep rather than true surprises.
- Treating NFR testing as something to "fit in later."

---

## 8. Risk Management

### 8.1 Top Risks for Utility Modernization (with Mitigations)

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Feature parity incomplete at cutover | High | High | Parity matrix tracked weekly; UAT gates include parity sign-off |
| User rejection due to UX change | Medium | Very High | Involve operators from design; keep workflow topology; parallel usability testing |
| Data migration surprises | High | Very High | Source data profiling in design phase; 3+ migration dress rehearsals |
| Integration with vendor systems unstable | Medium | High | Contract tests; ACL pattern; early vendor engagement |
| Regulatory change mid-project (NERC CIP, PUC) | Medium | Medium | Regulatory liaison; quarterly compliance review |
| Cutover window blown | Medium | High | Dress rehearsals time the migration; shrink cutover to what's proven |
| Key staff departure | Medium | Medium | Pair programming; knowledge base; avoid hero dependencies |
| Performance under storm load | Medium | Very High | Load test at 3–10× normal from MVP; chaos engineering pre-go-live |
| Security breach / NERC CIP violation | Low | Catastrophic | Threat model; pen test; SOC integration |
| Scope creep from business stakeholders | **Very High** | High | Change control; feature parity + MVP discipline; enhancements on roadmap, not sneaked in |
| Vendor contract / license surprises | Medium | Medium | License review in design; plan B identified |
| Training and adoption under-invested | High | High | OCM workstream funded from day one |

### 8.2 Risk Register Maintenance

- Owned by the Program Manager, reviewed weekly.
- Template in Appendix A.8.
- Every risk has an owner, mitigation, contingency, and trigger.

---

## 9. Templates Appendix

### A.1 User Story Template

```markdown
## [STORY-ID] Title

**As a** <persona>
**I want** <capability>
**So that** <business outcome>

### Business Rules Referenced
- BR-xxx: <description>
- BR-yyy: <description>

### Acceptance Criteria (Gherkin)
```gherkin
Scenario: ...
  Given ...
  When ...
  Then ...
```

### Non-Functional Criteria
- Performance: <target>
- Accessibility: WCAG 2.2 AA
- Audit: <logging requirements>

### Dependencies
- Upstream: <story or system>
- Downstream: <story or system>

### Out of Scope
- <explicit exclusions>

### Design / UX
- <link to Figma / design system component>

### Estimate
- Story points: <number>
```

### A.2 Acceptance Criteria Patterns

**Happy path:** system does the right thing when inputs are valid.
**Negative path:** system rejects invalid input gracefully with correct error.
**Boundary:** min/max/edge values.
**Security:** authorization enforced; sensitive data handled correctly.
**Observability:** appropriate logs / metrics / audit records emitted.
**Resilience:** behavior when downstream dependency fails.

### A.3 Definition of Ready (DoR)

A story is Ready when:
- [ ] Clear title and description (Connextra format)
- [ ] Acceptance criteria written in Gherkin
- [ ] Business rules referenced and agreed
- [ ] UX/UI design approved (if user-facing)
- [ ] Dependencies identified and not blocking
- [ ] Estimate agreed by team
- [ ] NFRs identified
- [ ] Testable — QA confirms test approach
- [ ] Out-of-scope explicit
- [ ] Fits within one sprint

### A.4 Architecture Decision Record (ADR) Template

```markdown
# ADR-NNN: <Title>

**Status:** Proposed | Accepted | Superseded by ADR-MMM | Deprecated
**Date:** YYYY-MM-DD
**Deciders:** <names>

## Context
<What forces are at play? What is the problem?>

## Decision
<What did we decide?>

## Alternatives Considered
- **Option A:** <description>. Pros: ... Cons: ...
- **Option B:** <description>. Pros: ... Cons: ...

## Consequences
### Positive
- ...
### Negative
- ...
### Neutral
- ...

## Compliance / Security / Cost Impact
<if applicable>

## References
- <links>
```

### A.5 Definition of Done (DoD)

A story is Done when:
- [ ] Code implemented and peer-reviewed
- [ ] Unit tests written, ≥ 80% coverage on new code
- [ ] Integration tests pass
- [ ] Contract tests pass (if integration boundary)
- [ ] Acceptance criteria demonstrably met
- [ ] Security scans clean (SAST, SCA)
- [ ] Observability instrumentation added (logs, metrics, traces)
- [ ] Documentation updated (API docs, runbook if operational change)
- [ ] Feature flag in place if partial functionality
- [ ] Deployed to staging and smoke-tested
- [ ] Accepted by Product Owner in sprint review
- [ ] No new critical / high defects open on this story

### A.6 Integration Catalog Row Template

| Field | Value |
|-------|-------|
| Integration ID | INT-nnn |
| Source System | |
| Target System | |
| Direction | Inbound / Outbound / Bidirectional |
| Trigger | Event / Schedule / On-demand |
| Protocol | REST / SOAP / Kafka / file / DB |
| Payload | <brief> |
| Volume (peak / avg) | |
| Latency SLA | |
| Availability SLA | |
| Auth Mechanism | |
| Data Classification | |
| Error Handling | |
| Retry Policy | |
| Owner (internal) | |
| Owner (external) | |
| Test environment availability | |
| Notes | |

### A.7 Cutover Runbook Row Template

| Step | Start Time | Duration | Owner | Task | Precondition | Success Check | Rollback Trigger | Status |
|------|-----------|----------|-------|------|--------------|---------------|------------------|--------|
| 1 | T-0:00 | 5m | DBA | Put legacy DB in read-only mode | Comms sent | `SHOW read_only` = ON | N/A | |
| 2 | T+0:05 | 45m | Data Eng | Run final delta migration | Step 1 complete | Reconciliation report clean | Migration fails > 2 retries | |
| ... | | | | | | | | |

### A.8 Risk Register Row Template

| ID | Risk | Category | Likelihood (1–5) | Impact (1–5) | Score | Owner | Mitigation | Contingency | Trigger | Status |
|----|------|----------|------------------|--------------|-------|-------|------------|-------------|---------|--------|
| R-01 | | | | | | | | | | |

### A.9 RACI Template (sample — adapt per workstream)

| Activity | Program Mgr | Solution Arch | Tech Lead | Product Owner | Business SME | DevOps | Security | Business Owner |
|----------|-------------|---------------|-----------|---------------|--------------|--------|----------|----------------|
| Architecture decisions | I | A | R | C | C | C | C | I |
| Story prioritization | I | C | C | A/R | C | I | I | I |
| Production deployment | I | C | C | C | I | R | C | A |
| UAT sign-off | I | I | I | C | R | I | I | A |
| Cutover go/no-go | R | C | C | C | C | C | C | A |

*R = Responsible, A = Accountable, C = Consulted, I = Informed*

### A.10 Feature Parity Matrix Template

| Legacy Feature ID | Feature Name | Legacy Behavior Summary | Target Behavior | Parity (Y/Partial/N) | Enhancement | MVP (Y/N) | Priority | Epic | Status | UAT Sign-off |
|-------------------|--------------|-------------------------|-----------------|----------------------|-------------|-----------|----------|------|--------|--------------|
| LF-001 | | | | | | | | | | |

### A.11 Sprint Planning Checklist

- [ ] Prior sprint closed; retro actions captured
- [ ] Team capacity calculated (PTO, meetings, support duty)
- [ ] Top of backlog refined to DoR
- [ ] Stories estimated
- [ ] Sprint goal defined (single sentence, business-meaningful)
- [ ] Dependencies across teams flagged
- [ ] Test approach agreed per story
- [ ] Spike work (if any) time-boxed

### A.12 UAT Test Case Template

```markdown
## UAT-nnnn: <Title>

**Module:** <module>
**User Story / Epic:** <link>
**Tester:** <name>
**Date:** <date>

### Preconditions
- ...

### Test Data
- ...

### Steps
1. ...
2. ...

### Expected Result
- ...

### Actual Result
- ...

### Pass / Fail
- ...

### Defects Raised
- DEF-nnn

### Notes / Evidence
- <screenshot, log refs>
```

---

## 10. Recommended Tooling by Category

| Category | Tools (pick one unless noted) |
|----------|-------------------------------|
| ALM / Backlog | Jira, Azure DevOps, Linear |
| Source Control | GitHub, GitLab, Azure Repos |
| CI/CD | GitHub Actions, GitLab CI, Azure Pipelines, Jenkins |
| IaC | Terraform (+ Terragrunt), Pulumi |
| Containers | Docker, Kubernetes (managed where possible) |
| Secrets | HashiCorp Vault, cloud KMS |
| Observability | OpenTelemetry + (Datadog / New Relic / Grafana stack) |
| API | OpenAPI + Swagger/Stoplight; Postman; Pact for contracts |
| Testing | JUnit/xUnit/pytest; Playwright; k6; OWASP ZAP; axe-core |
| Data modeling | ER/Studio, erwin, dbdiagram.io (light) |
| Architecture docs | Structurizr (C4), draw.io, PlantUML; ADRs in repo (markdown) |
| Design | Figma + Storybook |
| Docs | Confluence, Notion, or Markdown in repo + Backstage |
| Incident mgmt | PagerDuty, Opsgenie |
| SIEM | Splunk, Sentinel, Elastic |

---

## 11. The Short List of Do-Not-Skip Items

If executive pressure forces cuts, these are the items you must defend:

1. **Parallel run environment** — biggest risk reducer.
2. **Data migration dress rehearsals** (at least 3 full-volume).
3. **Performance testing at storm-day scale** from MVP onward.
4. **Real operators in UAT**, not only SMEs.
5. **OCM and training funded** from day one.
6. **Feature parity matrix** tracked weekly and signed off at UAT.
7. **Management reserve** of 15–20%.
8. **Hypercare period** of at least 30 days.
9. **Cutover rollback plan actually tested** — not just documented.
10. **NFR testing** treated as first-class, not "we'll get to it."

---

## 12. Closing Notes

Modernizing a mission-critical utility system with strict feature parity is one of the harder classes of enterprise software programs. The pattern of success is unglamorous: disciplined decomposition, honest estimates, narrow MVP with broad integration coverage, parallel-run UAT, phased cutover, and patient hypercare. Most failures in this domain are not engineering failures — they are **governance failures**: unrealistic timelines pushed from above, scope creep tolerated during build, testing compressed to protect a date, or cutover forced before the parity matrix is green.

The templates in this playbook are starting points; adapt them to the tooling and culture of your organization. Keep them living — the playbook should evolve with each sprint retrospective.
