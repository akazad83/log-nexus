# Modernization Playbook — Recalibration
## Based on Actual Team Composition and Engagement Model

> **Companion to:** the three prior documents (general playbook, domain supplement, operating model supplement). This document **supersedes** specific sections of those documents where called out. Where not explicitly revised, prior guidance stands.

---

## 1. What Changed and Why This Matters

The following clarifications substantially shift the risk profile:

| Clarification | Prior Assumption | Revised Understanding |
|---------------|------------------|----------------------|
| Prior SI delivered only template deliverables; internal team produced 90% of Discovery outputs | Internal team capability unknown | Internal team has **already demonstrated** ability to carry the hardest analytical work |
| Four senior, prominent-level engineers available as core team | Generic internal capability, likely gaps | Strong technical nucleus proven at scale |
| Vendor is winding down with no commercial interest; their lead architect is joining as an employee | Vendor contract/exit complexity; commercial misalignment | Clean knowledge transfer via hiring; full alignment |
| Senior District Operators are retired/admin-role, 100% allocated | Active-duty operators pulled from grid, rotation needed | Full-focus, no backfill needed, no burnout risk from operations |

**Net effect:** The three largest risks I flagged in prior documents (internal capability gap, vendor exit dynamics, operator availability/burnout) are substantially reduced. Different risks replace them. The timeline can move back toward the earlier range, but the new risks need explicit management.

---

## 2. Recalibrated Risk Assessment

### 2.1 Risks That Are Now Much Smaller

- **Internal team cannot execute a program of this complexity** — prior assessment replaced by evidence of successful Discovery execution plus four senior anchors
- **Vendor contract and exit complexity** — effectively resolved by hiring the lead architect and the firm's winding down
- **Operator burnout or operational-pressure withdrawal** — eliminated by the retired-administrative status

### 2.2 Risks That Are Now Larger or New

| Risk | Why It Matters | Mitigation |
|------|---------------|------------|
| **Key-person concentration on four senior engineers** | Each one is architecturally load-bearing. Loss of any materially slows the program. Health, retention, engagement, even vacations become program concerns. | Named successor/shadow per senior; paired technical leadership; documented decisions (ADRs) so knowledge is transferable; retention planning (recognition, compensation, authority, career path) reviewed quarterly; no heroic overtime patterns that burn people out |
| **Lead architect carries legacy patterns unconsciously** | They designed and built the current system. Their intuitions optimize for its constraints. | Architectural reviews include a dedicated "why not X?" challenger role; at least one senior engineer's role is explicitly to push back; pattern library reviewed against modern references (e.g., DDD, event-driven patterns), not only legacy echoes |
| **Retired operators out of date on current operations** | Regulations, procedures, threat landscape, and integration partners evolve. Administrative-role seniority does not guarantee currency. | Formalized channel to 2–3 active-duty operators as "current-state advisors" (not Product Council members but consulted on changes since their time); quarterly review sessions with active shift supervisors; validation of every business-rule update against current ops manual |
| **Post-SI skepticism over-corrects into rejecting all external help** | Bad SI experience creates institutional antibodies. Some external help is genuinely valuable (specialized security, performance, accessibility, specific-vendor expertise). | Explicit policy: specialist, bounded, deliverable-specific external engagements (measured in weeks, not years) are welcomed; named boundaries distinguish these from prime-SI arrangements |
| **Senior engineers become review/advisory bottleneck** | Four seniors cannot personally tech-lead five squads, review all PRs, attend all design sessions, and still build. Calendar becomes critical path. | Strong architecture runway (patterns, libraries, templates) so mid-level engineers execute without senior-gate on every decision; ADR process decentralizes architectural authority once patterns are set; senior engineers rotate between "build mode" and "enabling mode" quarters |
| **Retired operators may lack authority to influence current operations leadership during cutover** | Cutover and adoption require buy-in from current operations leadership who may not know/respect the retired operators the same way | Bridge role: designated senior active-duty operator or operations manager who is program-aware and represents program inside current operations |
| **Hiring the lead architect is a single-employee dependency** | If that individual leaves within the first 12–18 months, their unique vendor-side knowledge goes with them. | Structured knowledge capture in first 6 months (BR catalog, data dictionary, integration quirks documented); pair this individual with at least one senior internal engineer as shadow; competitive retention package |

### 2.3 Risks That Remain Unchanged from Prior Documents

All domain-specific and technical risks from the domain supplement remain: storm-day scaling, safety-critical precondition correctness, SCADA integration fidelity, NERC CIP scope, data migration complexity, cutover-window selection, feature parity validation, regulatory change. Those are properties of the system, not the team.

---

## 3. Recalibrated Team Structure

### 3.1 Revised Leadership Team

| Role | Recommended Fill | Notes |
|------|------------------|-------|
| Chief Architect / Lead Solution Architect | The incoming lead architect from vendor | With explicit challenger-role discipline; pair with an internal senior who has veto authority on legacy-pattern drift |
| Platform / Infrastructure Architect | One of the four senior engineers (or external hire if none has this depth) | On-prem specifics matter |
| Data Architect | One of the four senior engineers or dedicated hire | Event sourcing + migration strategy |
| Integration Architect | One of the four senior engineers | SCADA, field work, GIS boundaries |
| Security / Compliance Architect | Dedicated hire or strong internal with NERC CIP experience | Cannot be a 20%-time role |
| Chief Engineer / VP-level program owner | Internal executive | Final accountability |

**Four senior engineers, assigned:**
- Senior 1 → Chief Architect partner / deputy (challenger role to incoming architect)
- Senior 2 → Tech Lead for the SCADA + Cards + Switching Order core squad (highest-risk code)
- Senior 3 → Tech Lead for the Integrations + Data Migration squad
- Senior 4 → Enabling/Platform Tech Lead (CI/CD, infrastructure, observability foundations)

**Rationale:** Spread deliberately so each critical area has senior coverage. Do not put more than one on the same squad or in the same area. Rotate annually so cross-pollination happens and succession is built.

### 3.2 Operator Product Council — Recalibrated

Given retired-admin seniors at 100%:

- **3–4 retired senior operators**, 100% allocated, serving as permanent Product Council and domain SMEs (no rotation needed given status)
- **Plus: Active-Duty Advisory Panel** — 3–5 current shift operators or supervisors, 10–15% time (1–2 sessions per month)
  - Purpose: keep designs honest against current operations, current regulations, current threat landscape
  - Specifically validate any change where legacy operational behavior is being preserved
  - Consulted on UX validation especially — they know current muscle memory
- **Plus: Designated bridge role** — one current operations manager or senior supervisor as program liaison, 20% time, attends steering committee, advocates for program inside current operations leadership

This model captures the depth of retired operator experience AND the currency of active-duty input. Both are necessary; neither is sufficient alone.

### 3.3 Revised Total Team Composition

| Role | Internal FTE | External | Notes |
|------|--------------|----------|-------|
| Program Manager | 1 | 0 | |
| Project Managers | 2–3 | 0 | |
| Chief Architect | 1 | 0 | Lead architect hire from vendor |
| Senior Engineers / Tech Leads | 4 | 0 | Your prominent seniors |
| Mid Engineers | 10–14 | 0 | Execution capacity around seniors |
| Junior Engineers | 3–5 | 0 | Growth path, pair-programming |
| Data Engineers | 2–3 | 0–1 (contractor if migration peak needs it) | |
| DBAs | 2 | 0 | |
| Infrastructure / Platform Engineers | 3–4 | 0 | VMware, network, storage, on-prem |
| DevOps / Release Engineers | 2–3 | 0 | Ansible, CI/CD, artifacts |
| Security Engineer | 1–2 | 0–1 (bounded CIP specialist if needed) | |
| QA Engineers | 4–6 | 0 | Automation-focused |
| Performance Engineer | 1 | 0 | |
| UX Designer | 1–2 | 0–1 (bounded for design-system setup) | |
| UX Researcher | 1 | 0 | |
| Technical Writer | 1 | 0 | |
| Business Analysts | 3–4 | 0 | |
| Scrum Masters | 4–5 | 0 | |
| Product Owners | 4–5 | 0 | |
| Release / Cutover Manager | 1 | 0 | |
| OCM / Training Lead | 1 | 0 | |
| Retired Operator Product Council | 3–4 @ 100% | 0 | |
| Active-Duty Advisory Panel | 3–5 @ 10–15% | 0 | Light-touch contribution |
| Ops-liaison bridge role | 1 @ 20% | 0 | |
| **Total core FTE** | **~55–70** | **~0–3 specialist bounded** | |

External spend is now almost entirely bounded specialist engagements (e.g., pen test firm, accessibility audit, performance consultancy for one engagement) rather than prime-SI presence.

### 3.4 External Help — Where It's Still Worth It

Post-SI skepticism is understandable but should not become absolute. Bounded, specialist, deliverable-based engagements remain valuable:

| Domain | When to Bring In External |
|--------|---------------------------|
| Pen testing | Annually, pre-production; required for compliance |
| NERC CIP audit readiness | Before formal compliance audit; months not years |
| Accessibility audit | Before UAT closes |
| Performance engineering | If storm-day load testing surfaces issues beyond team capability |
| Database tuning | For specific workloads; bounded engagement |
| Specific vendor-product expertise | E.g., Kafka operations, PostgreSQL internals |
| Independent architecture review | Once, before MVP build starts; second time before cutover |

**Key principle:** These are **measured in weeks**, scoped to **specific deliverables**, have **named individuals** with **verified references**, and **report to your internal team**, not to their firm's engagement manager. This is the opposite of prime-SI work and should be framed as such.

---

## 4. Recalibrated Timeline

With stronger team composition, the timeline pulls back toward the range I originally gave before the internal-team penalty, though not all the way:

| Phase | Realistic | Notes |
|-------|-----------|-------|
| Detailed Design | 5–7 months | Strong analytical team (proven in Discovery) pulls this back |
| Platform / infra foundation (parallel) | 3–5 months | On-prem still adds time vs. cloud |
| MVP Build + Internal Test | 8–11 months | Strong seniors accelerate but infrastructure still constrains |
| MVP Parallel Run + UAT | 3–4 months | Unchanged — this is risk-driven, not capacity-driven |
| Incremental Build | 14–20 months | Depends on domain breadth (transmission, steam, di-electric, RTU) |
| SIT & UAT (overlapped with build) | 4–6 months | |
| District-by-District Cutover | 4–6 months | |
| Decommission + stabilization | 3–5 months | |
| **Total end-to-end** | **36–45 months** | |

**This is 4–10 months faster than my prior "40–55 months" estimate** — driven by team strength, clean vendor situation, and committed operators. It is still slower than cloud-native SI-led modernization ranges, reflecting on-prem realities.

**What this estimate assumes:**
- The four senior engineers remain engaged through MVP at minimum
- Lead architect onboarding is smooth (first 3 months productive)
- Mid-level engineers hired/available on reasonable timeline
- No major regulatory surprises (NERC CIP scope change, new state PUC rules)
- Parallel run and UAT not compressed under schedule pressure
- Storm seasons avoided for cutover windows

**What would pull it back toward the slower range:**
- Loss of any one senior engineer before MVP cutover
- Scope expansion beyond what was baselined
- Infrastructure procurement delays
- Compliance audit findings requiring remediation
- Integration partner delays (SCADA/EMS vendor schedules, field work system)

### 4.1 MVP Timeline (Revised)

- **End of Design → MVP in production parallel run: 10–13 months**
- **MVP parallel run → MVP accepted as authoritative in one district: 3–4 months**
- **MVP committed end-to-end duration from project start: ~18–22 months**

This is within the range typically achievable with a well-resourced internal program. Do not commit shorter than 18 months externally.

---

## 5. What Stays Unchanged from Prior Documents

To avoid re-reading everything, the following remain fully valid:

**From the general playbook:**
- Phase structure and gate criteria
- Agile decomposition hierarchy (Initiative → Epic → Feature → Story → Task)
- Gherkin acceptance criteria, INVEST, DoR, DoD
- C4 model + ADR discipline
- Test pyramid and test types
- UAT and cutover pattern guidance (parallel-run, phased)
- Hypercare model
- Template appendix

**From the domain supplement:**
- All domain-specific architecture (safety-critical workflow, switching-order state machine, precondition/postcondition model)
- Integration architecture specifics (SCADA/EMS, field work, GIS, CIS)
- Event sourcing + CQRS for operational core
- Outbox pattern for field work integration
- MVP scope recommendation (one district, Distribution only, full workflow)
- Domain-specific epic catalog
- Sample user stories
- Regulatory considerations (NERC CIP, state PUC)
- UX approach (workflow preservation, design system, operator validation)

**From the operating-model / on-prem supplement:**
- On-prem infrastructure design
- Technology stack recommendations (Java/Spring, PostgreSQL, Kafka, Ansible, etc.)
- No-container deployment patterns (rolling, blue/green via LB)
- Environment strategy
- Observability stack (Prometheus, Grafana, ELK, Jaeger)
- Storm-day capacity planning
- DR approach
- CI/CD pipeline

---

## 6. Governance Adjustments

### 6.1 Architecture Governance

Given the lead architect arrives from the legacy vendor side:

- **Dual-signature pattern for architectural decisions:** Chief Architect + one internal senior engineer both sign ADRs. Single-signature ADRs don't go forward. This is not distrust — it is institutional practice to ensure legacy-pattern drift is caught.
- **Monthly architecture review:** Chief Architect presents decisions and trade-offs; senior engineers challenge; decisions recorded.
- **Annual external architecture review:** One-week engagement with a respected independent architect (not SI, a named individual). Not to second-guess day-to-day but to catch systemic blind spots.

### 6.2 Key-Person Risk Management

This now becomes a first-class program concern:

- **Shadow/successor named** for each of the four senior engineers and the Chief Architect within first 6 months of their engagement
- **Knowledge artifacts expected from each:** ADRs for all decisions, design docs for non-trivial components, recorded walk-throughs of critical systems (video, 30–60 min each, kept in program knowledge base)
- **No heroic patterns:** if a senior is working 60+ hour weeks, that is a program health issue, not commitment
- **Explicit retention review with HR quarterly:** compensation, recognition, authority, career development all active levers
- **Vacation/time-off enforcement:** seniors who don't take their PTO are burnout candidates

### 6.3 Operator Input Governance

- **Retired Product Council** owns final say on domain rules, UX, workflow topology
- **Active-Duty Advisory Panel** has advisory vote — cannot override Council, but their objections are recorded and reviewed at Steering
- **Explicit resolution protocol** when Council and Panel disagree: Steering Committee decides, documented rationale

---

## 7. Revised Next Actions (First 60 Days)

Replaces the earlier 60-day list with a set tailored to the actual situation.

**Weeks 1–2:**
- Formalize the four senior engineers' squad assignments (spread as described in 3.1)
- Onboard the Chief Architect; start 6-month knowledge-capture plan
- Stand up Retired Operator Product Council; kickoff session with four seniors + Chief Architect
- Identify Active-Duty Advisory Panel members; secure operations leadership agreement
- Post first hiring requisitions for mid-level engineers and specialist roles (infrastructure, DevOps, QA automation)
- Assess existing Discovery artifacts quality; identify gaps to fill in Design

**Weeks 3–4:**
- First architectural decisions documented as ADRs (start with language/framework, datastore, messaging, deployment pattern)
- C4 Context and Container diagrams drafted
- Platform / infrastructure foundation planning starts in parallel
- Data profiling on legacy sources begins
- Compliance engagement formalized (internal NERC CIP + state PUC)
- Begin recruitment for mid-level engineers

**Weeks 5–8:**
- Story mapping workshops for core operational epics
- Challenger-review mechanism for architectural decisions operational (Senior-1 in deputy-architect role)
- MVP scope proposal drafted for Steering Committee
- Initial risk register; weekly review cadence
- Training / development plan for junior engineers (growth investment starts early)
- Key-person successor pairings identified

**Weeks 9–12:**
- MVP scope approved
- First engineers beginning architecture-runway work (patterns library, platform components)
- Infrastructure procurement initiated (lead times can be 8–12 weeks)
- Backlog for MVP refined to Ready state
- Independent architecture review scheduled for end of Design phase
- Vendor firm's wind-down plan acknowledged; any remaining vendor obligations documented

---

## 8. Final Honest View

Your actual configuration — proven analytical team, four senior engineers as anchors, a Chief Architect transferring knowledge via employment rather than contract, fully committed retired operators — is **significantly better** than the generic internal-led scenario I initially calibrated against. I under-credited it.

The risks that remain are real but different, and all manageable with the mitigations above:

- **Concentration risk** on four senior engineers and one Chief Architect — manageable with succession discipline and retention focus
- **Legacy-pattern drift** through the Chief Architect — manageable with challenger discipline and external review
- **Currency gap** in retired operators — manageable with active-duty advisory layer
- **Over-rejection of external help** — manageable with bounded specialist policy

None of these are showstoppers. They're the normal risks of a well-staffed internal program. The program profile is now one that **has a reasonable probability of succeeding within the 36–45 month range** with good execution, rather than one with systemic capability doubt.

Three final honest observations:

1. **Your biggest advantage is also your biggest constraint.** The small-elite-core model works when the core stays intact. Treat senior engineer retention, growth, and engagement as a program-critical concern, not an HR concern.

2. **Don't let the SI experience poison the well.** Bounded external specialist engagements are different from prime-SI arrangements. Cutting yourself off entirely costs quality in narrow areas (security, performance, accessibility) where specialist depth is useful. The pattern to reject is "hand this phase to the SI," not "bring in an expert for two weeks to stress-test our perf model."

3. **The hardest thing in your plan is still the cutover, not the build.** With your team and operators, the build is tractable. The cutover — phased parallel run across districts, with real SCADA events, real field work dispatch, real regulatory exposure — is where most of the residual program risk lives. Begin cutover planning in detail by end of MVP build, not after.

Overall: this is a sensible plan executed by a credible team. Protect the core, run the program with discipline, and the outcome is reasonable to expect within the revised timeline.
