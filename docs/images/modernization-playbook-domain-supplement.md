# Modernization Playbook — Domain Supplement
## Switching Order & Restoration Management System

> **Companion to:** `legacy-modernization-playbook.md`
> **Scope:** System-specific adjustments for a D&T feeder switching order, trouble card, and restoration management system integrated with SCADA/EMS, managing Distribution Feeders, Transmission Feeders, Di-Electric Systems, Telephonic Line Components (RTU comms), and Steam Systems.

---

## 1. Restated System Context (Architect's View)

### 1.1 What the System Is

A **safety-critical workflow orchestration system** that:
- Receives operational events from **SCADA/EMS** (breaker trips, alarms, telemetry state changes)
- Generates **trouble cards** representing incidents requiring operator attention
- Orchestrates **switching orders** — ordered, pre-planned sequences of device operations — under strict sequence enforcement
- Tracks **restoration** from incident through normal state
- Manages **scheduled maintenance cards and switching orders** for planned work
- Dispatches **field work** to an external system and tracks completion
- Enforces operator workflow discipline for **District Operators** orchestrating the process

### 1.2 Defining Architectural Characteristics

| Characteristic | Implication |
|----------------|-------------|
| Safety-critical ordering | Switching steps MUST execute in sequence; skipping or reordering can cause arc flash, back-feed, or personnel injury |
| SCADA-driven event ingestion | Inbound event throughput, especially during storms, is the primary scaling concern |
| Human-in-the-loop orchestration | District Operators are the authoritative decision-makers; system is *decision support*, not automation replacement |
| Strong audit requirements | Every decision, every step execution, every override must be recorded with operator attribution for regulatory and post-event review |
| Multi-domain asset coverage | Distribution + Transmission + Di-Electric + RTU Comms + Steam — each has distinct state models and safety rules |
| Field work delegation | Actual field execution lives in a separate system — integration fidelity and bidirectional state sync are first-class concerns |
| Regulated environment | Transmission falls under NERC standards; distribution under state PUC; steam under local jurisdictional rules |

### 1.3 What the System Is NOT

Clarifying this avoids scope creep:
- Not a field work management system (that's the integrated system)
- Not SCADA itself (SCADA is upstream)
- Not a GIS system (though it consumes topology)
- Not a billing / CIS system
- Not an automation engine that executes switching autonomously — operators authorize every step

---

## 2. Domain-Specific Epic Catalog

The following epic structure is tailored to your system. Each epic maps to 5–20 user stories typically. Use this as a starting point for backlog construction.

### 2.1 Operational Epics

| Epic ID | Name | Description |
|---------|------|-------------|
| E-OPS-01 | **SCADA/EMS Event Ingestion** | Real-time ingestion of alarms, telemetry, and state changes; deduplication, correlation, persistence |
| E-OPS-02 | **Trouble Card Lifecycle** | Auto-creation from events, manual creation, assignment, state transitions, closure, escalation |
| E-OPS-03 | **Switching Order Authoring** | Create/edit switching orders with pre-validated step sequences, templates, approvals |
| E-OPS-04 | **Switching Order Execution** | Stepwise execution with precondition checks, state verification, operator confirmation, hold/release |
| E-OPS-05 | **Clearance & Tag Management** | Issue/release clearances, hold tags, equipment lockout tracking, permit-to-work alignment |
| E-OPS-06 | **Restoration Tracking** | Track incident from initiation to normal state; capture duration, cause codes, customers affected |
| E-OPS-07 | **District Operator Dashboard** | Real-time situational awareness: active cards, pending steps, crew status, alarms, KPIs |
| E-OPS-08 | **Operator Handoff / Shift Change** | Structured handoff with acknowledgments, active work transfer, audit trail |
| E-OPS-09 | **Abnormal Configuration Tracking** | Track when network is in non-normal state due to ongoing work; auto-flag risks |
| E-OPS-10 | **Scheduled Maintenance Planning** | Plan, approve, schedule, and coordinate planned outages and maintenance work |

### 2.2 Asset-Domain Epics

| Epic ID | Name | Description |
|---------|------|-------------|
| E-AST-01 | **Distribution Feeder Management** | Feeder topology, devices, normal configuration, sectionalizing |
| E-AST-02 | **Transmission Feeder Management** | Transmission line assets, bus configurations, protection scheme awareness |
| E-AST-03 | **Di-Electric System Management** | Pipe-type / dielectric fluid-filled cable systems, pressure/fluid state, special restoration rules |
| E-AST-04 | **RTU & Telephonic Line Component Management** | RTU inventory, comm circuit state, impact on SCADA visibility when offline |
| E-AST-05 | **Steam System Management** | Steam mains, valves, traps, pressure zones, steam-specific safety rules |
| E-AST-06 | **Device & Equipment Registry** | Unified asset model across all domains with cross-references |

### 2.3 Integration Epics

| Epic ID | Name | Description |
|---------|------|-------------|
| E-INT-01 | **SCADA/EMS Integration** | Bidirectional: event consumption inbound, state queries, supervisory context |
| E-INT-02 | **Field Work System Integration** | Dispatch outbound, status/completion inbound, reconciliation |
| E-INT-03 | **GIS Integration** | Topology + geospatial data; connectivity model sync |
| E-INT-04 | **Customer Information Integration** | Customer-count impact calculation per feeder/section |
| E-INT-05 | **Asset Management System Integration** | Equipment master data, maintenance history |
| E-INT-06 | **Notification & Comms Integration** | Outbound alerts: SMS, email, ops radio, public-facing where applicable |
| E-INT-07 | **Regulatory Reporting Integration** | NERC, state PUC, internal compliance |

### 2.4 Platform & Cross-Cutting Epics

| Epic ID | Name | Description |
|---------|------|-------------|
| E-PLT-01 | **Identity, Roles & Authorization** | Operator roles, district scope, authority levels, time-bound permissions |
| E-PLT-02 | **Audit & Event Sourcing** | Immutable audit log of every operator action, system decision, state change |
| E-PLT-03 | **Reporting & Analytics** | Operational reports, reliability metrics (SAIDI/SAIFI/CAIDI, MAIFI), compliance reports |
| E-PLT-04 | **Observability & Operations** | System health, SLO dashboards, on-call runbooks |
| E-PLT-05 | **Data Migration from Legacy** | One-time and delta migration of historical cards, switching orders, asset model |
| E-PLT-06 | **Resilience & Failover** | Active-active / active-passive DR for operational continuity |

---

## 3. Safety-Critical Design: The Switching Order Workflow

This is the heart of the system. Get this wrong and nothing else matters.

### 3.1 Conceptual State Machine for a Switching Order

```
                  [DRAFT]
                     |
                     | (author completes)
                     v
                  [UNDER_REVIEW]
                     |
                     | (peer review / supervisor approval)
                     v
                  [APPROVED]
                     |
                     | (operator accepts for execution)
                     v
                  [IN_EXECUTION] <---+
                     |               |
                     |               | (hold / resume)
                     v               |
                  [ON_HOLD] ---------+
                     |
                     | (all steps complete AND system returned to normal)
                     v
                  [COMPLETED]
                     
        Any active state -> [ABANDONED] (operator decision, with reason)
                          -> [AMENDED]   (reopens DRAFT with revision)
```

### 3.2 Switching Step State Machine

Every step within a switching order has its own lifecycle:

```
[PENDING] -> [IN_PROGRESS] -> [CONFIRMED] -> [VERIFIED]
    |             |               |              |
    |             v               |              |
    |         [FAILED]            |              |
    |             |               v              |
    |             +---> [REVERSED] <-------------+
    v
[SKIPPED] (only under explicit supervisor override with reason code)
```

**Sequence enforcement rule:** Step N cannot transition to `IN_PROGRESS` until step N-1 is `VERIFIED` (unless explicit parallel-step modeling is defined and supervisor-approved).

### 3.3 Preconditions and Postconditions

Every step definition carries:

| Attribute | Purpose |
|-----------|---------|
| Target device | What device is operated |
| Target operation | Open, close, rack-in, rack-out, ground, etc. |
| Precondition predicates | SCADA state, prior step status, clearance state, environmental conditions |
| Postcondition predicates | Expected SCADA state after execution; confirms the operation actually happened |
| Expected duration | For schedule and SLA tracking |
| Safety notes | Free-text warnings; some are mandatory-acknowledge |
| Reverse step | Link to the step that undoes this one — essential for abandonment |

**Design principle:** The system blocks forward progress when postconditions don't match SCADA reality within an expected window. This is a critical safety net — an operator thought they opened breaker X but something went wrong and the system must not allow proceeding as if success occurred.

### 3.4 The Override Problem

Real operations require overrides (SCADA comms failure, manual device operation without telemetry, etc.). Safe override design:
- Overrides require explicit supervisor authorization (two-person rule for high-impact actions)
- Every override has a mandatory reason code from a controlled vocabulary
- Overrides are flagged in the audit log for mandatory post-event review
- Some overrides are simply not permitted (e.g., skipping a grounding step on a transmission line)
- Post-execution reconciliation: when SCADA visibility returns, verify overridden steps actually achieved expected state

### 3.5 Concurrent Switching Orders

Multiple switching orders can be active simultaneously on the same or adjacent network. Required:
- **Conflict detection** at approval time: does this order touch devices or network segments affected by another active order?
- **Runtime conflict checks** before each step: has the network state changed due to another order's action?
- **Coordination workflow** when conflicts detected: notify owning operators, require coordination decision
- **Abnormal-configuration awareness** — if network is already in a non-normal state from active work, subsequent orders must account for it

### 3.6 Di-Electric System Special Considerations

Your Di-Electric system management is unusual enough to flag explicitly:
- Fluid-filled cable systems have pressure/level telemetry that must be healthy before energization
- De-energization often requires controlled cooling periods
- Re-energization sequence includes fluid system health checks as mandatory preconditions
- Leak response has different urgency and different step templates from standard electrical work
- These rules should be encoded as step templates and precondition libraries, not left to operator memory

### 3.7 Steam System Operational Model

Steam switching differs from electric:
- Operations are often slower and involve thermal considerations (warm-up, cool-down)
- Different personnel and field work dispatch paths possibly
- Different hazard model (thermal, pressure) → different safety notes and precondition sets
- Consider whether this justifies a separate bounded context within the system

---

## 4. Integration Architecture — System Specific

### 4.1 Integration Map (Concrete)

```
                  ┌───────────────┐
                  │   SCADA/EMS   │
                  └──────┬────────┘
                         │ events (push) + queries (pull)
                         v
           ┌─────────────────────────────┐
           │         GIS System          │──► topology, connectivity
           └──────┬──────────────────────┘
                  │ sync (near-real-time or scheduled)
                  v
   ┌──────────────────────────────────────────────────┐
   │  Switching Order & Restoration Management System │
   │  (THIS SYSTEM)                                   │
   └───┬───────┬────────────┬────────────┬────────────┘
       │       │            │            │
       │       │            │            │
       v       v            v            v
  ┌────────┐ ┌────────┐  ┌────────┐  ┌──────────────┐
  │ Field  │ │  CIS   │  │ Asset  │  │ Notification │
  │ Work   │ │        │  │  Mgmt  │  │   Platform   │
  │System  │ │        │  │        │  │              │
  └────────┘ └────────┘  └────────┘  └──────────────┘
       ^
       │
       │ bidirectional: dispatch + status/completion

  Reporting outbound -> NERC / PUC systems, data warehouse
```

### 4.2 Integration Catalog — Key Entries

#### INT-001 — SCADA/EMS Event Stream (Inbound)

| Attribute | Value |
|-----------|-------|
| Direction | Inbound |
| Pattern | Event-driven, message-based (preferred) or polling (fallback) |
| Protocol candidates | MQTT, Kafka, AMQP, ICCP for utility-specific, or vendor-specific API |
| Volume — normal | 10s–100s events/sec |
| Volume — storm peak | 1,000s–10,000s events/sec (design for this) |
| Latency SLA | < 5 seconds from SCADA emission to system persistence |
| Ordering | Per-device ordering required; global ordering NOT required |
| Idempotency | Events must have stable IDs; replay-safe consumption |
| Failure mode | Broker durable queue; consumer replay capability on reconnect |
| Event types | Breaker trip, breaker close, alarm raise, alarm clear, tag placed, tag removed, quality change, analog threshold breach |

**Critical design decision:** Use a **durable message broker between SCADA and the system** even if SCADA vendor offers a direct API. This decouples availability, enables replay, and supports parallel run architecture.

#### INT-002 — Field Work System (Bidirectional)

| Attribute | Value |
|-----------|-------|
| Outbound | Work dispatch: card details, switching order reference, location, priority, crew requirements |
| Inbound | Status updates: crew assigned, en route, on-site, work started, work completed, work halted |
| Protocol | REST API preferred; consider async message pattern for status updates |
| Reconciliation | Nightly reconciliation job to detect any state drift |
| Idempotency | Required both directions |
| Compensation | If dispatch fails downstream, system must not mark card as "dispatched"; include outbox pattern |

**Pattern:** Use the **transactional outbox pattern** for all outbound integration writes to ensure the database transaction and message emission stay consistent. This is non-negotiable for a system where state drift can have safety implications.

#### INT-003 — GIS (Inbound, Reference Data)

| Attribute | Value |
|-----------|-------|
| Direction | Inbound (primary); outbound for abnormal-configuration markings possibly |
| Content | Topology, connectivity model, device attributes, geospatial coordinates |
| Sync pattern | Scheduled full sync (daily) + event-driven delta (on GIS publish events) |
| Latency | Delta sync < 15 min acceptable for most cases |
| Version control | GIS edits produce versioned topology; system must handle mid-switching-order topology changes carefully (usually: block import until order completes) |

#### INT-004 — Customer Information (Inbound for Impact Calc)

| Attribute | Value |
|-----------|-------|
| Direction | Inbound query; possibly cached |
| Content | Customer-to-transformer / customer-to-feeder mapping, customer classifications (critical care, large commercial, etc.) |
| Pattern | Cached snapshot with daily refresh; query-on-demand for specific lookups |
| Failure mode | System continues without customer count if CIS unavailable; count shown as "estimating" |

### 4.3 Anti-Corruption Layer (ACL) Pattern — Required Here

Wrap every integration — especially SCADA/EMS and the field work system — in an ACL:
- Translates vendor-specific schemas and semantics into your domain model
- Isolates your core from vendor API changes
- Provides a single place to handle vendor quirks
- Enables vendor replacement without rewriting business logic

This is especially important because SCADA/EMS vendors have idiosyncratic data models and your restoration management logic should not be polluted by them.

---

## 5. Data Model — Core Entities

### 5.1 Bounded Contexts (suggested)

```
┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│   Network Model     │  │  Event & Alarm      │  │  Work Management    │
│ (topology, devices) │  │  (from SCADA)       │  │ (cards, switching   │
│                     │  │                     │  │   orders, clearance)│
└─────────────────────┘  └─────────────────────┘  └─────────────────────┘

┌─────────────────────┐  ┌─────────────────────┐  ┌─────────────────────┐
│  Operator & Shift   │  │   Audit & History   │  │   Reporting         │
│                     │  │   (event-sourced)   │  │   (read models)     │
└─────────────────────┘  └─────────────────────┘  └─────────────────────┘
```

### 5.2 Core Aggregates

| Aggregate | Key Entities |
|-----------|--------------|
| Feeder | Feeder, Section, Device, NormalConfiguration, AbnormalConfigurationFlag |
| Device | Device, DeviceType, Location, Ratings, CurrentState, LastKnownSCADAState, Tags |
| Card | Card, CardType (trouble / scheduled / informational), Status, Priority, Timeline, RelatedEvents, CustomersAffected |
| SwitchingOrder | Order, OrderStatus, Steps[], Approvals[], HoldHistory, LinkedCards |
| SwitchingStep | Step, Sequence, TargetDevice, Operation, Preconditions, Postconditions, Status, ExecutionRecord, Overrides |
| Clearance | Clearance, Type, IssuedTo, Scope (devices/sections), IssueTime, ReleaseTime, AssociatedOrders |
| Event | EventId, Source, DeviceRef, EventType, Timestamp, Quality, RawPayload |
| OperatorAction | ActionId, OperatorId, ActionType, Target, Timestamp, Outcome, Justification |

### 5.3 Event Sourcing Recommendation

Strongly consider **event sourcing for the operational core** (cards, switching orders, steps, operator actions):
- Native fit for audit requirements
- Time-travel and replay for post-event analysis
- Supports parallel run (replay events through new system)
- Rich inputs for analytics and ML-based operator assistance later

Combine with CQRS — separate read models for dashboards and reports from the write model. This keeps the write side simple and safe while allowing rich read-side projections.

### 5.4 Database Optimization — System-Specific Concerns

| Concern | Guidance |
|---------|----------|
| Event ingestion volume | Event tables partitioned by month; archived to cold storage after 90 days; hot indexes minimal |
| Network topology queries | Connectivity traversal is query-heavy; consider graph database (Neo4j, Neptune) or specialized indexing |
| Card dashboard queries | Dedicated read model / materialized views refreshed on event commit |
| Audit log | Append-only, partitioned by month, retained per regulatory requirement (often 7+ years transmission) |
| Historical restoration data | Archived yearly; accessible for SAIDI/SAIFI calculations and regulatory submissions |
| Concurrent operator reads | Read replicas sized for 2–3× peak operator count; write traffic on primary only |

---

## 6. Adjusted MVP Recommendation

Given your constraints, recommend MVP scope as follows:

### 6.1 MVP In-Scope

- **One district, Distribution feeders only** (pick a representative district, not the smallest or simplest)
- **End-to-end SCADA → Card → Switching Order → Field Dispatch → Restoration → Close** loop
- **Trouble cards** (reactive) — not yet scheduled maintenance
- **Real SCADA event ingestion** (not stubbed)
- **Real field work system integration** (not stubbed)
- **GIS and CIS integrations live** (read-only at minimum)
- **Core District Operator dashboard**
- **Audit logging and reporting at MVP quality**
- **Basic switching order authoring + execution** with precondition/postcondition validation and override workflow
- **Clearance/tag management** for the MVP scope

### 6.2 MVP Out-of-Scope (but designed-for)

- Transmission feeders
- Di-Electric systems
- RTU/Telephonic component management
- Steam systems
- Scheduled maintenance workflow (reactive only for MVP)
- Advanced analytics
- Mobile operator UI
- Multi-district coordination

### 6.3 Rationale

This MVP proves the **highest-risk path end-to-end**: safety-critical switching order orchestration with real SCADA integration and real field dispatch. The domains you exclude (transmission, di-electric, steam) add scope but not fundamentally new architectural risk — their core workflow is the same switching-order pattern with domain-specific rules. Transmission adds NERC compliance weight; budget that for Phase 2, not MVP.

### 6.4 MVP Duration Estimate

- After design phase completes: **7–10 months** of build + internal test
- Plus **2–3 months** of parallel run / UAT before MVP is considered "MVP-accepted"
- Total MVP milestone: **~10–13 months from end of design**

If pressure exists to compress: the items to protect are SCADA integration fidelity, audit logging completeness, and the switching-step state machine correctness. Everything else can be stripped further if needed.

---

## 7. Regulatory & Compliance Adjustments

### 7.1 Applicable Regimes (Confirm with your Compliance team)

| Regime | Applicability | Key Implications |
|--------|---------------|------------------|
| NERC CIP | Transmission BES Cyber Systems | Asset classification (CIP-002), security controls (CIP-005 through CIP-011), personnel training (CIP-004), change management, electronic security perimeter |
| NERC TOP / IRO | Transmission operations | Operating procedures, data reporting, coordination |
| NERC PRC | Protection systems | Maintenance testing, data retention |
| NERC EOP | Emergency preparedness | Restoration plans, drills |
| State PUC | Distribution reliability | SAIDI/SAIFI/CAIDI reporting, outage cause codes |
| OSHA 1910.269 | Electrical safety | Clearance procedures, lockout/tagout, personnel qualifications |
| Local steam regulator | Steam system | Varies by jurisdiction |

### 7.2 Design Implications

- **Audit log retention:** Minimum 3 years NERC; 7+ years common. Plan storage and retrieval accordingly.
- **Access control granularity:** NERC CIP requires role-based access with periodic review. System must support user access reports for compliance.
- **Change management records:** Configuration changes and software changes affecting BES Cyber Systems require CIP-010 change management artifacts.
- **Electronic Security Perimeter (ESP):** If the system is inside an ESP, all remote access goes through intermediate systems with multi-factor authentication, session recording, etc.
- **Operator training records:** The system should cross-reference operator actions against current training/authorization records. Operator performed an action they weren't currently authorized for? That's a finding.
- **Evidence generation:** Design reports for compliance audits from day one. Compliance is not an afterthought feature.

### 7.3 Who to Engage Early

- Internal NERC CIP compliance officer
- Reliability coordinator
- State PUC filings team
- Internal audit
- Cybersecurity / SOC team

Have them review the architecture during design phase, not during test.

---

## 8. UX Considerations for District Operators

Your note that "users are highly satisfied with the current system's operational workflow" is a strong signal. Treat this as a constraint, not a preference.

### 8.1 What to Preserve

- **Workflow topology:** same number of steps to accomplish the same task, same sequence
- **Screen layout patterns:** critical information in the same relative position
- **Keyboard shortcuts and hotkeys:** operators rely on these heavily; replicate exactly where possible
- **Card list density:** operators scan dozens of cards; don't sparsify in the name of "cleaner design"
- **Color coding conventions:** status colors familiar to operators; changing red/green conventions causes errors
- **Alarm sounds and notification patterns:** if the legacy system beeps for priority-1 alarms, preserve that behavior

### 8.2 What to Enhance

- **Visual refresh:** modern typography, improved contrast, responsive layout
- **Information density options:** allow operators to toggle compact vs. comfortable views
- **Accessibility:** WCAG 2.2 AA baseline, including colorblind-safe palettes (critical for status indicators)
- **Search and filter:** faster card filtering, saved filter sets per operator
- **Context preservation:** when drilling into a card, don't lose the list context
- **Mobile/tablet capability:** not for primary operation, but for supervisors and away-from-desk situational awareness
- **Integration fluidity:** fewer window switches between map, card list, switching order

### 8.3 Validation Approach

- **Side-by-side comparison sessions:** place legacy and new system next to each other; operator performs the same task on both; time it; count clicks; capture friction points.
- **Shadow sessions during parallel run:** observe operators working; look for hesitation, backtracking, workaround patterns.
- **Operator advisory council:** standing group of 5–8 operators from different districts who review designs every sprint. Rotate membership yearly.

### 8.4 One-Line Diagram Rendering

Specific to your system: the one-line network diagram is likely a core operator UI. Considerations:
- High-performance SVG or canvas rendering for complex feeders (1,000+ devices)
- Level-of-detail: show summary at zoomed-out, detail at zoomed-in
- Real-time state overlay: device positions, tags, abnormal configurations
- Historical replay capability: "show me feeder state at 14:23 yesterday"
- Print-to-PDF for clearance documents

---

## 9. Testing Strategy — System-Specific Additions

Supplements the general testing strategy in the main playbook.

### 9.1 Switching Order Simulation

Build a **switching order simulator** as a test asset:
- Models the network state
- Applies steps in sequence
- Verifies precondition/postcondition logic
- Can replay historical switching orders from legacy system against new system logic to validate parity
- Used in both automated regression and operator training

### 9.2 SCADA Event Replay

Capture real SCADA event streams (masked if needed) and use them for:
- **Load testing** — storm-day event volumes
- **Integration testing** — new-system event handling against production-like patterns
- **Parallel run validation** — same events into both systems, compare outputs

### 9.3 Scenario Library

Build and maintain a library of canonical scenarios, each automated:

| Scenario Family | Example |
|-----------------|---------|
| Single breaker trip | Single-feeder outage, card creation, no restoration complication |
| Cascading events | Multiple breakers trip in sequence within 60s |
| Storm load | 1,000 events/min for 30 minutes |
| Multi-card coordination | Two cards on adjacent feeders, coordination required |
| Di-Electric leak | Pressure alarm → card → switching order with fluid-system-aware steps |
| SCADA comms failure | Lose SCADA feed mid-switching-order; system enters degraded mode |
| Field work system failure | Dispatch integration down; cards queue; recovery and replay |
| Override scenarios | Supervisor override with each supported reason code |
| Shift change with active work | Handoff during active switching order |
| Abandonment | Switching order abandoned mid-execution; reverse steps executed |

Each scenario is a named test asset, executable from CI, and used in operator training.

### 9.4 Compliance Evidence Testing

Specific test suite that produces:
- Sample audit log exports
- Sample NERC-style evidence packages
- Sample PUC reliability reports
- Access control reports

Run this suite quarterly; outputs reviewed by compliance team.

### 9.5 Safety Test Cases

A dedicated suite of **safety-critical test cases** run before every release:
- Cannot execute step N without step N-1 verified
- Cannot close a breaker with a clearance active on that device
- Cannot issue conflicting clearances
- Cannot complete a switching order with steps unverified
- Cannot override without supervisor authentication
- Audit log captures every override with reason code

These tests must be **unable to be disabled** in the pipeline, and must include both happy and adversarial paths.

---

## 10. Cutover Strategy — System-Specific

### 10.1 Recommended Approach: Phased Parallel Run by District

**Phase A — Shadow mode (all districts):**
- New system receives all SCADA events and generates cards/orders
- Operators continue in legacy; new system output is not authoritative
- Reconciliation team compares outputs daily
- Duration: 4–8 weeks

**Phase B — First district pilot:**
- One district's operators transition to new system as authoritative
- Legacy kept running in shadow (reverse of Phase A)
- Extended hypercare for that district
- Duration: 4–6 weeks

**Phase C — District rollout:**
- Additional districts migrate in waves (typically one district every 2–4 weeks)
- Lessons from each wave applied to next
- Duration: 3–6 months depending on district count

**Phase D — Legacy decommission:**
- Only after all districts stable on new system for at least 30 days
- Legacy kept read-only for an additional 60–90 days for historical lookups
- Final decommission after regulatory retention requirements met or historical data migrated

### 10.2 Avoid These Windows

- **Storm season** (region-dependent; for east-coast US typically June–November and winter storm windows)
- **Regulatory reporting quarters** (usually ending Mar/Jun/Sep/Dec)
- **Planned major transmission outages** or other grid events
- **Holiday periods** with reduced staffing
- **Summer peak demand period** for distribution

### 10.3 Cutover Readiness Criteria

No district cuts over until:
- Parallel run divergence < agreed threshold (typically < 1% of events produce different outcomes, and all differences explained)
- All P1/P2 defects closed
- 100% of SCADA event types handled in production volumes
- All operator training completed and attested
- Runbooks validated by on-call team
- Rollback tested successfully within 30 days
- Compliance officer sign-off
- Reliability coordinator briefed

### 10.4 Rollback Specifics

A rollback in this system has unusual dimensions:
- Cards created in new system while it was authoritative must be reverse-migrated to legacy
- Active switching orders must be paused, state captured, then re-authored in legacy
- Clearances in force must be documented and transferred
- Audit continuity is a regulatory concern — ensure rollback preserves audit chain

Document this in the rollback runbook and rehearse it.

---

## 11. Risk Register — Domain-Specific Additions

Additions to the general risk register:

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| SCADA vendor API change mid-project | Medium | High | ACL pattern isolates impact; vendor liaison engaged; contract tests catch changes |
| Storm hits during cutover phase | Medium | Very High | Calendar-based window avoidance; go/no-go decision 48hrs before cutover based on weather |
| Operator rejects new system at pilot district | Medium | Very High | Operator advisory council from design phase; side-by-side validation; pilot district chosen with engaged leadership |
| Safety incident attributed to system behavior | Low | Catastrophic | Safety test suite that cannot be bypassed; conservative precondition logic; supervisor override audit; incident response runbook |
| Switching order precondition logic incorrect for an edge case | Medium | High | Replay historical orders from legacy; safety test scenarios; staged rollout detects issues on small population |
| NERC CIP audit during cutover | Low | High | Compliance evidence tested regularly; audit-ready posture maintained |
| Field work system integration instability | Medium | High | Outbox pattern; reconciliation jobs; degraded-mode operations |
| Di-Electric or Steam domain rules miscoded | Medium | High | Domain SMEs embedded; domain-specific test suites; phased introduction (electric first) |
| Event volume exceeds design assumptions during storm | Medium | Very High | Load test at 3× worst-ever observed; backpressure and graceful degradation design |
| Concurrent switching order conflict detection misses a case | Low | High | Formal review of conflict-detection rules; replay historical concurrent scenarios |
| Historical data migration incomplete or inaccurate | Medium | Medium | Reconciliation reports; business sign-off on rejected records; read-only legacy access preserved for historical lookup |

---

## 12. Sample User Stories (Illustrative)

To calibrate how Discovery artifacts translate into implementable stories. Use these as anchors.

### Story ST-SCADA-014

**As a** District Operator
**I want** trouble cards automatically created when SCADA reports a breaker trip on a monitored feeder
**So that** I can begin addressing the incident without manual card entry

**Business Rules Referenced:**
- BR-SCADA-003: Card auto-creation rules by device type
- BR-CARD-011: Card priority assignment from event severity
- BR-CUST-002: Customer impact calculation on card creation

**Acceptance Criteria:**
```gherkin
Scenario: Breaker trip on monitored distribution feeder
  Given a distribution feeder "DF-4471" is monitored by SCADA
    And the feeder normal configuration is "NORMAL_CLOSED"
    And no active card exists for this feeder
  When SCADA publishes a "BREAKER_TRIP" event for feeder "DF-4471"
  Then the system SHALL create a trouble card within 5 seconds
    And the card SHALL reference the SCADA event
    And the card SHALL be assigned to the district owning the feeder
    And customer impact SHALL be calculated from the CIS snapshot
    And the card SHALL appear on active District Operator dashboards
    And an audit record SHALL be created with system as actor

Scenario: Duplicate breaker trip event within deduplication window
  Given an active card exists for feeder "DF-4471"
    And the card was created less than 60 seconds ago
  When SCADA publishes another "BREAKER_TRIP" event for feeder "DF-4471"
  Then the system SHALL NOT create a second card
    And the event SHALL be linked to the existing card
    And an audit record SHALL note the duplicate suppression

Scenario: Breaker trip during SCADA communication failure recovery
  Given SCADA communication was lost for 10 minutes
    And SCADA is now recovering with event replay
  When replayed "BREAKER_TRIP" events arrive
  Then the system SHALL process them using event timestamps, not arrival time
    And deduplication SHALL still apply
```

**NFRs:**
- Card creation P95 latency ≤ 5 seconds from SCADA event commit
- Must handle 1,000 events/minute sustained without dropped events
- Audit record immutability enforced

**Out of scope:**
- Automatic switching order proposal (separate story)
- Notification to field crews (separate story)

**Estimate:** 8 story points

---

### Story ST-SWORD-027

**As a** District Operator
**I want** the system to block execution of a switching step when its preconditions are not met
**So that** I cannot accidentally proceed with an unsafe operation

**Business Rules Referenced:**
- BR-SW-014: Step precondition evaluation
- BR-SW-015: Override authorization levels
- BR-SAFETY-003: Grounding step mandatory preconditions

**Acceptance Criteria:**
```gherkin
Scenario: Precondition satisfied, step proceeds
  Given a switching order in IN_EXECUTION state
    And step 5 targets breaker "BR-8822"
    And step 5 precondition requires breaker "BR-8821" in OPEN state
    And SCADA reports breaker "BR-8821" as OPEN
  When the operator initiates execution of step 5
  Then the system SHALL transition step 5 to IN_PROGRESS

Scenario: Precondition not satisfied, step blocked
  Given a switching order in IN_EXECUTION state
    And step 5 precondition requires breaker "BR-8821" in OPEN state
    And SCADA reports breaker "BR-8821" as CLOSED
  When the operator attempts to execute step 5
  Then the system SHALL NOT transition step 5 to IN_PROGRESS
    And the system SHALL display the failing precondition to the operator
    And the system SHALL offer the operator options: wait, request supervisor override, abandon order

Scenario: Supervisor override with authorization
  Given a precondition is blocking step execution
    And the operator requests supervisor override
  When a supervisor authenticates and provides reason code "SCADA_COMMS_FAILURE"
  Then the system SHALL allow the step to proceed
    And the override SHALL be flagged in the audit log
    And the override SHALL appear in the end-of-shift anomaly report

Scenario: Override attempted for safety-mandatory precondition
  Given a step is a grounding step
    And a precondition is not met
  When any user attempts override
  Then the system SHALL refuse the override regardless of authorization level
    And SHALL display a safety policy message
```

**NFRs:**
- Precondition evaluation P95 latency ≤ 1 second
- Override audit record includes operator, supervisor, reason, full context

**Estimate:** 13 story points

---

## 13. Adjusted Timeline Expectations for Your System

Reconfirming realistic durations given the system specifics:

| Phase | Duration | Notes |
|-------|----------|-------|
| Detailed Design | 5–7 months | Extra 1–2 months vs. general utility due to safety-critical workflow modeling and multi-domain asset coverage |
| MVP Build + Internal Test | 7–10 months | |
| MVP Parallel Run + UAT | 2–3 months | |
| Incremental Build (Transmission, Di-Electric, Steam, RTU, Scheduled Maintenance) | 12–18 months | Parallel to ongoing MVP refinement |
| SIT & UAT for remaining modules | 3–6 months (overlapped) | |
| District-by-District Cutover | 3–6 months | Depends on district count |
| Full decommission + stabilization | 3–6 months | |
| **Total end-to-end** | **36–48 months** | |

**Honest observations:**
- The domain specifics (multi-domain assets, strict safety workflow, NERC-regulated transmission) push this toward the high end of utility modernization ranges.
- Anyone proposing this in under 30 months for a real system at your scope is almost certainly under-scoping testing, compliance, or cutover.
- The highest leverage for schedule is **not** compressing build — it's running design, platform/DevOps setup, and data-migration prep in parallel, and beginning compliance engagement in month one of design.

---

## 14. Immediate Next Actions (First 30 Days of Design Phase)

A concrete starter plan:

**Week 1–2:**
- Stand up the backlog tool and import feature catalog from Discovery
- Form the Operator Advisory Council (5–8 operators across districts)
- Schedule compliance kickoff (NERC CIP lead, state filings team)
- Initiate SCADA/EMS vendor technical engagement
- Initiate field work system technical engagement

**Week 3–4:**
- Story mapping workshops per epic (start with E-OPS-01 through E-OPS-06)
- First ADRs: architecture style, message broker choice, primary datastore
- Draft C4 context diagram; review with DAB
- Publish NFRs and compliance register
- Data profiling starts on legacy sources

**Week 5–8:**
- Epic decomposition continues
- Architecture runway: begin platform / DevOps foundation work in parallel
- UX discovery sessions with Operator Advisory Council; begin design system
- Integration catalog populated for top 10 integrations
- Risk register operational; weekly reviews
- MVP scope proposal drafted for sponsor review

**Week 9–12:**
- MVP scope approved
- Backlog for MVP refined to Ready state
- Data migration strategy drafted
- Cutover strategy first draft
- First sprint of MVP build begins (assuming architecture runway is ready)
- Compliance officer formally engaged with monthly review cadence

If Week 12 looks like "we're still defining epics," that's a signal the design phase needs more time — don't force the build phase to start prematurely.

---

## 15. Closing

The defining constraint of this modernization is that your system is **safety-critical orchestration software used by highly-trained operators under regulatory scrutiny, with strong user satisfaction on existing workflow**. Every design and process choice should be evaluated against these four pressures:

1. **Safety** — can this choice create a scenario where an operator could take an unsafe action the legacy system would have prevented?
2. **Auditability** — does this choice preserve the evidence chain required for NERC / PUC audits?
3. **Workflow preservation** — does this choice introduce friction that will reduce operator acceptance?
4. **Resilience** — does this choice degrade gracefully when SCADA, field work, or other integrations fail?

When in doubt, these four questions settle most arguments.
