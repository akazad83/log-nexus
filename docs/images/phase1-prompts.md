# Phase 1 — Discovery Validation & Augmentation
## Prompt Pack

> **Phase goal:** Close gaps in Discovery outputs before Detailed Design accelerates. Time-boxed 4–6 weeks.
>
> **Who uses these prompts:** Chief Architect, Senior Engineer Deputy, Integration Architect, Data Architect, BAs, vendor code-archaeology resource. Validated by Product Council where domain judgment is required.
>
> **How each prompt is structured:**
> - **Purpose** — what the prompt produces
> - **When** — signal that you should run this
> - **Inputs** — what to paste/attach
> - **Output target** — which tab in `phase1-artifacts.xlsx` the results populate
> - **Prompt template** — copy-paste ready with placeholders

---

## Prompt Index

1. [P1-01 — Discovery Output Gap Analysis (structured review)](#p1-01--discovery-output-gap-analysis)
2. [P1-02 — Feature Catalog Completeness Check](#p1-02--feature-catalog-completeness-check)
3. [P1-03 — Functional Requirement Depth Audit](#p1-03--functional-requirement-depth-audit)
4. [P1-04 — Business Rule Extraction from Legacy Code](#p1-04--business-rule-extraction-from-legacy-code)
5. [P1-05 — Business Rule Enrichment](#p1-05--business-rule-enrichment)
6. [P1-06 — NFR Grounding & Testability Validation](#p1-06--nfr-grounding--testability-validation)
7. [P1-07 — Integration Depth Analysis](#p1-07--integration-depth-analysis)
8. [P1-08 — System Inventory Completeness](#p1-08--system-inventory-completeness)
9. [P1-09 — Data Entity & Aggregate Boundary Analysis](#p1-09--data-entity--aggregate-boundary-analysis)
10. [P1-10 — State Machine Formalization](#p1-10--state-machine-formalization)
11. [P1-11 — Failure Mode & Degraded Operation Analysis](#p1-11--failure-mode--degraded-operation-analysis)
12. [P1-12 — Storm-Day Scenario Modeling](#p1-12--storm-day-scenario-modeling)
13. [P1-13 — Cross-Domain Coverage Verification](#p1-13--cross-domain-coverage-verification)
14. [P1-14 — Overall Discovery Quality Posture Review](#p1-14--overall-discovery-quality-posture-review)
15. [P1-15 — Validation Workshop Facilitation Aid](#p1-15--validation-workshop-facilitation-aid)

---

## Conventions

- `{{artifact}}` = attach or paste the full document
- `[VALUE]` = short inline parameter
- **Standard context block** — paste once at the start of every session; the prompts below assume it's already in context:

```
STANDING CONTEXT FOR THIS SESSION:

Program: Modernization of legacy switching order and restoration management
system for an electric utility. System manages Distribution Feeders,
Transmission Feeders, Di-Electric Systems, RTU/Telephonic Line Components
(SCADA communications infrastructure), and Steam Systems. Integrates
upstream with SCADA/EMS for event ingestion, downstream with a Field Work
System for crew dispatch, plus GIS and CIS. District Operators orchestrate
all switching operations in strict sequential order. Safety-critical;
NERC CIP applicable for transmission portions.

Deployment: On-premises, non-containerized. No public cloud.

Current phase: Discovery Validation & Augmentation. Time-boxed 4-6 weeks.

Discovery was led by a senior front-end developer acting as tech lead.
Behavioral work (features, FRs, UX) is relatively strong. Systems-level
dimensions (backend, integration depth, data model depth, NFR grounding,
state machines, failure modes) are suspected to be under-addressed.

Current technical leadership: Chief Architect (formerly legacy vendor's
lead architect, now employee) + Senior Engineer Deputy. Four internal
senior engineers are the technical core. Active-duty District Operators
who mentor newer DOs serve as Product Council. Old vendor resource is
engaged specifically for business rule extraction from legacy code.

Task quality expectation: Honest, specific, grounded. Do not soften
findings. Do not invent details that aren't supported. Flag uncertainty
explicitly rather than fabricating. Safety-critical items get extra
scrutiny.
```

---

## P1-01 — Discovery Output Gap Analysis

**Purpose:** Systematic gap identification across any Discovery artifact.

**When:** First prompt to run on any Discovery artifact. Use as the entry point for validation; follow up with category-specific prompts (P1-02 through P1-13).

**Inputs:** One Discovery artifact at a time (feature catalog, FR doc, BR register, NFR catalog, system inventory, integration map, data model, use cases).

**Output target:** `2. Gap Register` tab; for each gap, one row.

### Prompt

```
Review the following Discovery artifact and identify gaps using this five-lens
evaluation framework:

1. COMPLETENESS — What's missing that should be there?
2. DEPTH — Where is coverage surface-level when it needs to be detailed?
3. SYSTEMS THINKING — Where are system-level behaviors (state consistency,
   failure modes, concurrency, temporal coupling, ordering) under-specified?
4. SAFETY & COMPLIANCE — Where are safety-critical or NERC CIP-relevant
   dimensions under-addressed?
5. TESTABILITY — Where are requirements stated in ways that cannot be
   objectively validated?

For each gap identified, provide:

GAP ID: G-[suggested number]
CATEGORY: [one of the 10 checklist categories: Feature Catalog / Functional
   Requirements / Business Rules / NFRs / System Inventory / Integration Depth /
   Data Model / State Machines / Operational & Edge / Domain Coverage]
LENS: [which of the 5 lenses above detected this]
DESCRIPTION: Specific gap (not vague)
WHY IT MATTERS: What risk is created if this gap is carried into Design
RECOMMENDED ACTION: Specific, executable (workshop topic, research task,
   deliverable to produce)
EFFORT: S (hours) / M (days) / L (weeks)
SEVERITY:
   CRITICAL = must close before Design entry (blocks baseline acceptance)
   HIGH = close early in Design (first 30 days)
   MEDIUM = address during Design phase
   LOW = track only, no immediate action
PROPOSED DISPOSITION: Close Now / Defer to Design / Defer to Build / Accept Risk

Group findings by severity. Within severity, group by category.

After listing gaps, provide:
- OVERALL ARTIFACT HEALTH: Strong / Adequate / Weak / Unusable-as-is
- TOP 3 MOST CONSEQUENTIAL GAPS
- STRENGTHS WORTH PRESERVING (not only criticisms)
- QUESTIONS FOR THE DISCOVERY AUTHORS (clarifications needed before we
  decide whether something is a gap or just incomplete documentation)

Be honest. Do not soften findings. Do not invent gaps that aren't there,
but do not miss ones that are.

ARTIFACT TO REVIEW:
{{paste the Discovery artifact}}
```

**Tip:** Run once per artifact rather than all at once — outputs lose specificity when scope is too broad.

---

## P1-02 — Feature Catalog Completeness Check

**Purpose:** Detect missing categories of features that operational teams typically omit when frontend-led Discovery dominates.

**When:** After P1-01 identifies Feature Catalog as having potential gaps.

**Inputs:** Full feature catalog.

**Output target:** `2. Gap Register` (category: Feature Catalog).

### Prompt

```
Evaluate this feature catalog for completeness across these dimensions.
Frontend-led Discovery often misses non-UI-visible features — examine
particularly hard for these.

EXPECTED FEATURE CATEGORIES (not all will apply, but verify each):

A. Operational workflow — switching order, clearance, card lifecycle,
   dispatch, restoration
B. Supervisory / dashboard — situational awareness, multi-district view,
   SLA tracking
C. Reporting — regulatory (SAIDI, SAIFI, NERC submissions), operational,
   ad-hoc
D. Administration — user management, role assignment, permission changes,
   feature flags, config management
E. Integration-facing — SCADA event consumption, field work dispatch,
   GIS sync, CIS lookup, AMI integration (if applicable), enterprise auth
F. Observability — metrics dashboards, alerting, log search, trace
   exploration, audit retrieval
G. Security / audit — authentication, authorization, audit log UI,
   regulatory evidence export, NERC CIP evidence support
H. Training / simulation — dry-run mode, training scenarios, simulated
   SCADA events for training, post-event replay
I. Cross-cutting — notifications, search, export, batch operations,
   attachments, comments/notes
J. Scheduled / planned — planned outage workflow, maintenance windows,
   switching templates, approval workflows
K. Domain-specific — is each of the 5 domains (Distribution, Transmission,
   Di-Electric, RTU/Telephonic, Steam) represented with equivalent depth?

For each category:
- LIST FEATURES present for this category
- ASSESS coverage: Complete / Partial / Missing / Not Applicable
- If Partial or Missing: what specific features should be present but aren't?

Also check:
- Every feature has an owner persona or system
- Every feature has priority
- Every feature has MVP classification (In / Out / Partial)
- Cross-cutting capabilities appear as features, not assumed
- "Hidden" features: admin tools, data fix workflows, emergency bypasses —
  these are often missed

Produce:
1. Per-category assessment table
2. List of specific features that should be added (with suggested ID and
   description)
3. Features that look mis-categorized or too coarse (one "feature" hiding
   10 sub-features)
4. Features that appear missing an owner or priority

FEATURE CATALOG:
{{paste feature catalog}}
```

---

## P1-03 — Functional Requirement Depth Audit

**Purpose:** Check FRs are specified deeply enough for Design, particularly for system-internal and edge behaviors.

**When:** Discovery FRs exist but you suspect they focus on user-visible happy paths only.

**Inputs:** Full FR document or sample section.

**Output target:** `2. Gap Register` (category: Functional Requirements).

### Prompt

```
Audit these Functional Requirements for depth. Frontend-led Discovery often
documents user-visible behavior well but underspecifies internal behaviors.

For each FR or group of FRs, evaluate:

1. HAPPY PATH — described?
2. NEGATIVE PATHS — input validation, precondition failures, authorization
   failures documented?
3. BOUNDARY & EDGE CASES — min/max values, empty inputs, timing edges,
   concurrent-user scenarios?
4. STATE TRANSITIONS — not just "user does X" but how state changes?
5. TIMING & ORDERING — when things happen; what must precede what?
6. CONCURRENCY — two operators on same entity? Shift handoff? Conflicting
   switching orders?
7. OVERRIDE / EXCEPTION WORKFLOWS — supervisor bypass? Emergency override?
8. RETRY / RECONCILIATION — system-internal behaviors when transient
   failures occur?
9. CLEANUP — what happens when a workflow is abandoned or times out?
10. OBSERVABILITY — what must be logged, metered, or alerted?
11. AUDITABILITY — what is auditable and with what context?
12. PERSISTENCE — what state must survive restart vs. session-only?

For each FR, produce:
- FR ID: [from source]
- COMPLETENESS SCORE: Strong / Adequate / Shallow / Severely under-specified
- DIMENSIONS UNDER-SPECIFIED: [list from 1-12 above]
- SPECIFIC QUESTIONS raised by the shallow coverage (actionable for workshop)
- RECOMMENDED AUGMENTATION: what to add

Pattern-level observations at the end:
- Recurring under-coverage pattern (e.g., "concurrency consistently missing")
- FRs that appear to be UI acceptance criteria masquerading as FRs
- FRs that are actually business rules in disguise (and should be moved
  to the BR register)

FUNCTIONAL REQUIREMENTS:
{{paste FRs or section}}
```

---

## P1-04 — Business Rule Extraction from Legacy Code

**Purpose:** Collaborate with vendor code archaeology resource to extract business rules from legacy code into structured catalog entries.

**When:** Vendor is reviewing a specific module/function and finds rule-bearing logic. Use to convert their findings into catalog format.

**Inputs:** Legacy code snippet and/or vendor's verbal/written description.

**Output target:** Feed into `Business Rule Catalog` in `01_scope_backlog_management.xlsx` (Phase 1 uses `8. Code Archaeology` tab to track batches; actual rules populate the BR catalog).

### Prompt

```
The code snippet and/or description below contains one or more business rules
embedded in legacy code. Extract them into structured catalog entries.

For each rule identified, produce:

RULE ID: BR-[SUGGESTED-ID based on domain and sequence]
NAME: Short descriptive name (6-10 words)
DOMAIN: Distribution / Transmission / Di-Electric / RTU-Telephonic / Steam /
   Cross-cutting
CATEGORY: Safety / Operational / Regulatory / Workflow / Data Integrity / Other
SAFETY-CRITICAL: Yes / No (Yes if violation could cause personnel or grid harm,
   regulatory violation, or material operational harm)

TRIGGER CONDITIONS: The state or event that invokes this rule
RULE STATEMENT: Clear, unambiguous language — avoid legalese; no ambiguity
CONSEQUENCE: What the system must do when the rule fires
RATIONALE: Why this rule exists (safety, regulatory, operational — cite
   source if known)

INPUTS REQUIRED: Data the rule evaluates
OUTPUTS / SIDE EFFECTS: What the rule produces or changes
PRECONDITIONS: State the system must be in for this rule to apply
POSTCONDITIONS: State the system will be in after the rule fires

STATE-DEPENDENT: Yes / No (Yes if rule behavior varies by system state)
IF STATE-DEPENDENT, VARIANTS: state → behavior mapping

EDGE CASES: Known situations where the rule behaves unusually
OVERRIDE POLICY: Can this rule be overridden? By whom? Under what conditions?
   What is logged?

LEGACY CODE REFERENCE: File path, module, function, approximate line range
EXTRACTION CONFIDENCE: High / Medium / Low
TEST CASES: At least 2 in Gherkin (happy path + one edge case); mark TBD
   if insufficient information to write test

UNCERTAINTIES: Anything requiring further investigation, operator input,
   or cross-check with functional spec — be specific about what you don't
   know rather than inventing details

If the code/description contains MULTIPLE distinct rules, produce one entry
per rule. Cross-reference related rules.

If code suggests a rule but you cannot fully characterize it, still include
it with UNCERTAINTIES populated. Do not fabricate.

CODE OR DESCRIPTION TO ANALYZE:
{{paste legacy code or vendor's description}}

RELATED FUNCTIONAL SPEC EXCERPTS (if available):
{{paste related spec content if any}}
```

**Tip:** After the AI produces entries, Product Council operator validates each safety-critical rule and the rationale — they often know the "why" vendor forgot.

---

## P1-05 — Business Rule Enrichment

**Purpose:** For rules already in the BR register but incomplete, fill in missing dimensions.

**When:** Reviewing existing BR register finds rules with missing fields (test cases, state dependency, override policy, edge cases).

**Inputs:** Existing BR entry + any supporting context.

**Output target:** Enriched row in BR catalog.

### Prompt

```
The following business rule is in our register but is incomplete. Enrich
it by filling in the missing dimensions. Where you cannot determine a
value from the provided information, mark it as "NEEDS INVESTIGATION"
and specify what question to resolve.

Fields to enrich (check each):
- SAFETY-CRITICAL classification with rationale
- PRECONDITIONS and POSTCONDITIONS formalized as predicates
- STATE-DEPENDENT flag with variants if applicable
- EDGE CASES (at least 3 if safety-critical, at least 1 otherwise)
- OVERRIDE POLICY including authorization and audit requirements
- TEST CASES in Gherkin (at least 2)
- RELATED RULES (cross-references)
- OBSOLESCENCE CHECK — is this rule still operationally relevant, or a
   legacy artifact?

Also flag:
- Ambiguous wording in the current rule statement
- Missing rationale
- Conflicts with other rules (cite BR-ID)
- Rules that seem to be duplicates of each other at the behavior level
- Rules whose legacy code reference is stale (file/function no longer exists)

EXISTING RULE ENTRY:
{{paste rule in whatever format it's currently in}}

RELATED CONTEXT:
{{optionally paste related FRs, other rules, operator input, vendor notes}}
```

---

## P1-06 — NFR Grounding & Testability Validation

**Purpose:** Validate NFRs are architecturally grounded rather than aspirational round numbers.

**When:** Even though NFRs are signed, validate each before Design uses them as anchors.

**Inputs:** NFR catalog with current target values and any existing measurement notes.

**Output target:** `2. Gap Register` (category: NFRs) for any NFR flagged; also augments the NFR Tracker in the main scope workbook.

### Prompt

```
For each NFR below, evaluate and report:

1. GROUNDING: Is the target value supported by legacy system measurement,
   workload projection, regulatory requirement, or industry benchmark?
   Or is it an aspirational round number?
   Classification: MEASURED / PROJECTED / REGULATED / BENCHMARKED /
   ASPIRATIONAL / UNKNOWN

2. MEASUREMENT METHOD: How will this NFR be objectively measured during
   build, SIT, UAT, and production? If unclear, flag as gap.

3. ARCHITECTURAL IMPLICATIONS: What architecture or infrastructure choice
   does this NFR require? (e.g., 99.99% availability implies specific
   redundancy). Are those choices explicit elsewhere?

4. TESTABILITY: Can this be tested? At what cost? In what environment?
   Production-only? DR-drill-required?

5. INTERDEPENDENCIES: Dependencies on or conflicts with other NFRs?

6. STORM-DAY BEHAVIOR: How does this NFR behave at 5-10x normal load?
   Is that explicit in the NFR statement or only in design?

7. CONSISTENCY WITH DOMAIN: Is the target reasonable for utility
   switching order systems? Flag unrealistically high or suspiciously low.

8. LEGACY COMPARISON: How does this compare to legacy system's actual
   behavior? If much better than legacy, is the improvement justified and
   achievable? If same as or worse than legacy, is that acceptable?

9. RECOMMENDATION:
   ADEQUATE (accept as stated)
   ADEQUATE with measurement method clarification (minor gap)
   NEEDS GROUNDING (investigate before accepting)
   POTENTIALLY UNREALISTIC (re-negotiate with stakeholders)
   MISSING COUNTERPART (e.g., availability without measurement window)

10. COUNTERPARTS: What other NFRs should exist alongside this one?
   (e.g., availability should have an MTTR companion)

Produce a table with one row per NFR and a column per dimension above.

End with:
- NFRs recommended for steering committee re-negotiation with specific
  rationale
- NFRs adequate but needing measurement method definition (Design phase task)
- NFRs suggesting missing counterparts
- OVERALL NFR POSTURE: Strong / Adequate / Needs Work

NFR CATALOG:
{{paste NFRs with current target values and measurement notes}}
```

---

## P1-07 — Integration Depth Analysis

**Purpose:** Expand shallow integration entries into full 22-point specifications. Likely the single highest-value Phase 1 activity if Discovery under-addressed integration.

**When:** Per critical integration (SCADA/EMS, Field Work System, GIS, CIS, Auth, Asset Mgmt). Run once per integration.

**Inputs:** Current integration description plus any vendor/counterparty documentation available.

**Output target:** One column per integration in `4. Integration Augmentation` tab.

### Prompt

```
Produce a complete 22-point integration specification for the integration
below. For each point, either provide specific answer or mark UNKNOWN with
a specific question to ask the integration counterparty. Do not fabricate.

SPECIFICATION POINTS:

1. Protocol / message format / schema reference (exact, versioned)
2. Interaction pattern: sync request/response / async event / batch / poll,
   with rationale
3. Ordering semantics: global / per-partition / per-entity / unordered —
   what can we assume
4. Idempotency keys used, deduplication window, retry-safety
5. Volume — normal events/sec, requests/sec
6. Volume — peak storm-day events/sec (specific number, not "high")
7. Latency profile: P50, P95, P99 under normal and peak
8. Availability SLA from counterparty + observed historical uptime
9. Our system's behavior when this integration is unavailable (degraded
   mode spec)
10. Authentication mechanism (mTLS, OAuth, API key, other)
11. Credential lifecycle: rotation frequency, process, emergency rotation
12. Error response catalog: every error the counterparty can return, and
    our correct response to each
13. Failure modes: partial delivery / duplicate / out-of-order / silent
    loss / silent corruption — for each, detection mechanism and response
14. Reconciliation: how we detect state drift from counterparty; how we
    resolve
15. Security: encryption in transit / at rest / data classification /
    access controls
16. Observability: what we monitor; what alerts; what runbook entry point
17. Ownership end-to-end: who to call at 3am; escalation path
18. Test environment: exists? fidelity to prod? known differences?
19. Known quirks / historical issues: the hard-won knowledge (vendor
    resource valuable here)
20. Retirement / vendor roadmap: is this system being replaced? When?
    What's our bridge strategy?
21. Validation status: Not Started / In Progress / Complete / Verified
22. Gap references: IDs in Gap Register (G-###) linked to this integration

For each UNKNOWN, the specific question to answer in the counterparty
workshop.

INTEGRATION NAME: [NAME]

CURRENT DESCRIPTION:
{{paste current integration entry from Discovery}}

SUPPORTING DOCUMENTATION:
{{paste vendor API docs, existing integration specs, vendor communications,
   operator knowledge — or mark "none available"}}
```

**Tip:** Run this before the integration workshop (e.g., WS-01 SCADA Deep-Dive). The UNKNOWN list becomes the workshop agenda.

---

## P1-08 — System Inventory Completeness

**Purpose:** Verify every touched system is cataloged with its characteristics, including systems often forgotten (auth, monitoring, logging infrastructure, backup, DNS).

**When:** Reviewing Discovery's system inventory.

**Inputs:** Current system inventory.

**Output target:** `2. Gap Register` (category: System Inventory).

### Prompt

```
Review the System Inventory and check for completeness. Systems commonly
missed from Discovery inventories include:

- Enterprise directory / SSO / MFA provider
- Enterprise certificate authority / PKI
- Enterprise secrets management (Vault, CyberArk)
- SIEM / log aggregation
- Monitoring (metrics, APM, tracing)
- Backup infrastructure
- DNS and internal naming service
- Email / notification gateway
- Shared file storage used by any integration
- Historical data warehouse (often referenced but not cataloged)
- Change management / ticketing system (where integration is needed)
- Mobile device management (if field-facing components)
- Report distribution service
- GIS topology authority (not just consumer GIS)
- Customer notification system
- Regulatory submission portal
- Outage mapping service (public-facing)

For each system present in the inventory:
- Name, vendor, version
- Ownership: internal team / external vendor / shared
- Protocol + integration pattern
- Criticality: Critical / Important / Nice-to-have
- Availability expectation
- Our system's behavior when it's unavailable
- Authentication mechanism
- Test environment availability
- Data classification touched
- Retirement / replacement roadmap

Assess each entry:
- COMPLETE (all fields populated)
- PARTIAL (some fields empty — list which)
- SHALLOW (fields populated but vague)

Then list:
- SYSTEMS MISSING FROM INVENTORY (from the "commonly missed" list or
  otherwise suspected)
- SYSTEMS WITH SHALLOW CHARACTERIZATION (rework targets)
- INTEGRATION DEPENDENCIES BETWEEN CATALOGED SYSTEMS (system X depends
  on system Y but not captured)
- RETIREMENT/REPLACEMENT RISKS (systems being retired that our
  modernization depends on)

CURRENT SYSTEM INVENTORY:
{{paste system inventory}}
```

---

## P1-09 — Data Entity & Aggregate Boundary Analysis

**Purpose:** Formalize data model depth — entities, relationships, aggregate boundaries, ownership, volumes, retention.

**When:** Discovery's data model is entity-list-level; need architectural grounding.

**Inputs:** Current data entity inventory, any ER diagrams, domain notes.

**Output target:** `2. Gap Register` (category: Data Model); produces input for Design-phase data model ADRs.

### Prompt

```
For each data entity present in the Discovery output, produce an augmented
entry addressing:

1. ENTITY NAME and BUSINESS DEFINITION (1-2 sentences, operator-meaningful)
2. DOMAIN ALIGNMENT: Distribution / Transmission / Di-Electric / RTU-
   Telephonic / Steam / Cross-cutting / Platform
3. ENTITY TYPE:
   - Aggregate Root (a consistency boundary)
   - Aggregate Member (part of a larger consistency boundary)
   - Reference Data (slowly changing lookup)
   - Event (immutable record of something that happened)
   - View / Projection (derived from other entities)
4. RELATIONSHIPS: to other entities; cardinality; ownership direction
5. AGGREGATE BOUNDARY: if aggregate root, what's inside the consistency
   boundary? What's outside?
6. OWNERSHIP: which service/module owns this entity's lifecycle
7. IDENTITY: how identified; globally unique or scoped?
8. STATE MACHINE: does this entity have a lifecycle with distinct states?
   (reference to State Machine Register SM-ID if yes)
9. VOLUME: current rows (from legacy if known), 1-year projection,
   5-year projection
10. GROWTH PATTERN: append-only / updated-in-place / capped
11. DATA SENSITIVITY: public / internal / confidential / regulated
12. RETENTION: business requirement, regulatory requirement, effective
    duration
13. ARCHIVAL strategy if retention window is bounded
14. INDEXING / QUERY PATTERNS: dominant read patterns
15. TEMPORAL CHARACTER: point-in-time state or time-series?

Then identify:

A. MISSING ENTITIES suggested by FRs or BRs but not in inventory
B. DUPLICATE ENTITIES (same concept under different names)
C. AMBIGUOUS AGGREGATE BOUNDARIES (consistency expectations unclear)
D. ENTITIES WITH OWNERSHIP UNCLEAR (multiple services claim; none claim)
E. ENTITIES LIKELY NEEDING TIME-SERIES STORAGE (pressure readings,
   metrics, etc.) that are currently modeled as regular tables
F. SENSITIVE DATA ENTITIES (PII, CPNI, CIP BES Cyber System Information)
   needing access controls

End with proposed aggregate-boundary groupings — which entities travel
together in a transaction.

CURRENT DATA ENTITY INVENTORY:
{{paste current data model or entity list}}

RELEVANT FRs/BRs (for ownership inference):
{{optionally paste FRs or BRs that reference data}}
```

---

## P1-10 — State Machine Formalization

**Purpose:** Convert informal lifecycle descriptions into formal state machine specifications. The most under-addressed area when Discovery is frontend-led.

**When:** One run per long-lived workflow object (Trouble Card, Switching Order, Switching Step, Clearance, Hold-Off, Dispatch, Incident, Di-Electric Pressure Event, Steam Operation).

**Inputs:** Informal lifecycle description + related FRs and BRs + any legacy code knowledge from vendor resource.

**Output target:** `5. State Machine Register`; formal transition table attached to that entry; gaps into `2. Gap Register`.

### Prompt

```
Produce a complete formal state machine specification for the workflow
object below.

1. STATES — every state with:
   - Name (SCREAMING_SNAKE_CASE)
   - Operational meaning (one sentence, operator-understandable)
   - Initial state flag (exactly one)
   - Terminal state flag(s)
   - Expected duration in this state (typical, max)

2. EVENTS / TRIGGERS — every event that can cause a transition:
   - User-initiated (operator clicks, commands)
   - System-initiated (timer, scheduler, reconciliation)
   - External (SCADA event, field work ack, GIS refresh)
   - Terminal (system shutdown, user abandonment)

3. TRANSITIONS — complete transition table with columns:
   - From State
   - Event / Trigger
   - Guard / Precondition (predicate that must be true)
   - Authorization required (role or permission)
   - Action (what happens during the transition)
   - Side effects (events emitted, integrations called, data written)
   - To State
   - Postcondition (predicate that must be true after)
   - Audit record fields required
   - Failure handling (if guard fails, if action fails mid-transition)

4. INVALID TRANSITIONS — explicitly enumerate what is NOT allowed, with
   rationale (particularly safety-critical prohibitions).

5. TIMEOUTS — per state, what happens if object stays too long.

6. CONCURRENT ACCESS — can multiple actors operate on this object
   simultaneously? What coordination? Pessimistic lock? Optimistic?
   Merge?

7. COMPENSATING ACTIONS — if a transition partially fails, what reversal
   is required to return to consistent state?

8. CROSS-OBJECT COORDINATION — how this object's state interacts with
   other workflow objects' states. Reference SM-IDs.

9. EDGE CASES — explicit handling for:
   - SCADA unavailable during transition attempt
   - Field work system unavailable during transition attempt
   - Shift handoff with object in non-terminal state
   - System restart with object in non-terminal state (recovery semantics)
   - Supervisor override attempt

10. INVARIANTS — properties that are ALWAYS true regardless of state
    (e.g., "audit record always exists for the most recent transition").

11. OPEN QUESTIONS — what couldn't be resolved from available inputs.
    Flag explicitly; do not assume.

Output format:
- State list (as table or list)
- Transition table (as table)
- Invalid transitions (as list with rationale)
- Invariants (as list)
- Edge case handling (as list)
- Open questions (as list for workshop)

WORKFLOW OBJECT: [OBJECT NAME, e.g., Switching Order]

INFORMAL DESCRIPTION FROM DISCOVERY:
{{paste whatever description exists}}

RELATED BUSINESS RULES:
{{paste BRs that constrain this object}}

RELATED FRs:
{{paste FRs that reference this object's lifecycle}}

LEGACY CODE REFERENCES (from vendor):
{{paste vendor notes on where this lifecycle is implemented in legacy,
   if available}}
```

---

## P1-11 — Failure Mode & Degraded Operation Analysis

**Purpose:** Systematic enumeration of failure scenarios beyond what informal Discovery captured, with current design adequacy assessment.

**When:** Workshop WS-10 preparation; also during integration deep-dives.

**Inputs:** High-level architecture description + current resilience / DR notes.

**Output target:** `6. Failure Mode Catalog`.

### Prompt

```
Produce a failure mode analysis covering the system area below. Use FMEA
discipline — Risk Priority Number (RPN) = Likelihood × Impact × Detection
Difficulty, each on 1-5 scale.

DIMENSIONS TO COVER:

1. EXTERNAL FAILURE MODES — for each integrated system:
   - SCADA/EMS unavailable (various durations: seconds, minutes, hours)
   - Field Work System unavailable
   - GIS unavailable
   - CIS unavailable
   - Auth/SSO unavailable
   - Other enterprise system failures
   For each: detection / our behavior / data handling (queue vs. drop) /
   recovery procedure / operator experience / safety implications

2. INTERNAL FAILURE MODES:
   - Single app-tier node failure
   - Database primary failure
   - Database connection saturation
   - Database slow queries cascading
   - Kafka broker loss
   - Kafka consumer lag
   - Full datacenter failure (DR scenario)
   - Network partition
   - Disk full
   - Memory pressure
   For each: detection / behavior / recovery / user experience

3. CASCADING FAILURES:
   - Storm + SCADA high volume + internal degradation
   - DB failover + in-flight switching order
   - Shift change + integration failure
   - Restart + non-terminal workflow objects

4. HUMAN-FACTOR FAILURE MODES:
   - Operator misclick
   - Operator override of safety precondition
   - Two operators conflicting
   - Shift handoff information loss

5. CYBER FAILURE MODES:
   - Credential compromise
   - Malicious integration traffic
   - Insider threat

For each failure mode:
FM ID: FM-[NN]
SCENARIO: Specific, narratively clear
CLASS: External / Internal / Cascading / Operational / Human Factor / Cyber
LIKELIHOOD (1-5): frequency of occurrence
IMPACT (1-5): severity if unmitigated
DETECTION DIFFICULTY (1-5): 1 = fires obvious alarm, 5 = silent failure
RPN: Likelihood × Impact × Detection Difficulty
CURRENT DESIGN ADEQUACY: Adequate (tested) / Adequate / Partial / Needs
   Work / Not Addressed
MITIGATION PLAN: specific, executable
OWNER: role responsible
SAFETY IMPLICATIONS: explicit (escalates priority regardless of RPN)

End with:
- TOP 5 RPN failure modes
- TOP 3 NOT ADDRESSED failure modes (regardless of RPN)
- PATTERNS: systemic resilience gaps (e.g., "consistent lack of backpressure
  design across integrations")
- INVARIANTS SAFETY-CRITICAL across all failure modes (never do X regardless
  of what fails)

SYSTEM AREA:
{{paste architecture overview}}

EXISTING RESILIENCE NOTES:
{{paste any HA/DR documentation, or "none"}}

EXISTING FMs already identified:
{{paste existing Failure Mode Catalog entries to avoid duplicates}}
```

---

## P1-12 — Storm-Day Scenario Modeling

**Purpose:** End-to-end model of storm-day operational load, concurrency, and failure scenarios. Foundation for capacity planning and NFR grounding.

**When:** Workshop WS-11.

**Inputs:** Historical storm-day data if available; operator anecdotes; SCADA event volume records.

**Output target:** Workshop output; populates NFR grounding, capacity plan inputs.

### Prompt

```
Model an end-to-end storm-day scenario for the switching order and
restoration management system. The model should be concrete enough to
drive NFR sizing and capacity planning.

STORM SCENARIO DEFINITION:
A severe storm event affecting 30% of service territory over 8 hours,
with aftermath restoration over 48 hours.

MODEL DIMENSIONS:

1. SCADA EVENT VOLUME:
   - Baseline: events/sec
   - Storm peak hour: events/sec (5-10x baseline)
   - Sustained storm: events/sec over 8-hour window
   - Post-storm: events/sec during restoration
   - Event type mix during storm vs. baseline
   - Backlog after comms outages (catch-up waves)

2. TROUBLE CARD CREATION:
   - New cards/hour peak
   - Concurrent active cards peak
   - Card resolution rate lagging creation
   - District imbalance (some districts at 10x normal, others near baseline)

3. SWITCHING ORDERS:
   - Concurrent active orders peak
   - Average order complexity (steps per order) during emergency vs. planned
   - Expedited authoring pressure

4. CLEARANCES:
   - Concurrent active clearances peak
   - Multi-device clearance complexity

5. FIELD DISPATCHES:
   - Dispatches/hour peak
   - Dispatch queue depth
   - Crew reassignment rate

6. OPERATOR CONCURRENCY:
   - Active operators normal vs. storm
   - Supervisor role activation
   - Mutual aid / external operators (if applicable)
   - Shift handoffs during active event

7. UI WORKLOAD:
   - Page loads/sec peak
   - Real-time update subscribers
   - Dashboard refresh traffic

8. INTEGRATION VOLUMES:
   - SCADA throughput already covered above
   - Field work API calls/sec
   - CIS lookup rate
   - GIS query rate

9. REPORTING / SUPERVISORY:
   - Dashboard concurrent viewers (executives, news media liaison?)
   - Regulatory notifications
   - Customer-facing outage map updates

10. INFRASTRUCTURE STRESS:
    - CPU/memory/IO profile expected
    - DB load pattern
    - Kafka lag pattern
    - Storage write rate

11. CROSS-CUTTING:
    - Audit record creation rate
    - Log volume
    - Alert fatigue risk

12. DEGRADED-MODE TRANSITIONS:
    - Thresholds that trigger degraded behaviors
    - Features to shed / throttle under extreme load
    - Operator UI adjustments (fewer widgets, longer refresh intervals)

13. SAFETY INVARIANTS DURING STORM:
    - Precondition checks not relaxed
    - Audit trail not sampled or dropped
    - Safety-critical rules not bypassed
    - Override discipline maintained

14. HUMAN FACTORS:
    - Operator fatigue after 6+ hours
    - Decision velocity vs. deliberation balance
    - Communication load (phone, radio, in-system notifications)

End with:
- PROJECTED PEAK NUMBERS (table: dimension, baseline, storm peak, factor)
- NFR IMPLICATIONS (which NFRs need to cover these peaks)
- INFRASTRUCTURE SIZING IMPLICATIONS
- DEGRADED-MODE BEHAVIOR SPEC SKELETON
- PRODUCT COUNCIL VALIDATION POINTS (what to confirm with operators)

Where specific numbers are not available, propose ranges with rationale
and mark as NEEDS VALIDATION. Do not fabricate specific numbers.

INPUT DATA:
{{paste any historical storm data, event volumes, operator estimates,
   legacy system capacity observations, or mark "operator estimates only
   available"}}

SYSTEM ARCHITECTURE CONTEXT:
{{paste high-level architecture}}
```

---

## P1-13 — Cross-Domain Coverage Verification

**Purpose:** Confirm the five domains (Distribution, Transmission, Di-Electric, RTU/Telephonic, Steam) have equivalent coverage in Discovery. Under-coverage of non-Distribution domains is a common pattern.

**When:** Workshop WS-13.

**Inputs:** Full Discovery outputs.

**Output target:** `2. Gap Register` (category: Domain Coverage).

### Prompt

```
The five domains below should have equivalent coverage in Discovery.
Under-coverage of less-frequent domains is a common Discovery pattern,
particularly when the program originates from Distribution operations.

DOMAINS:
1. Distribution Feeders
2. Transmission Feeders (often NERC CIP-applicable)
3. Di-Electric Systems (fluid-filled cables; domain-specific pressure rules)
4. RTU / Telephonic Line Components (SCADA comms infrastructure;
   self-managed assets)
5. Steam Systems (utility-specific; often sparsely documented)

FOR EACH DOMAIN, ASSESS COVERAGE ACROSS:

- Features cataloged (count, depth, MVP classification)
- Functional Requirements (count, depth)
- Business Rules (count, safety-critical flag distribution)
- NFRs (domain-specific vs. cross-cutting)
- Integrations (domain-specific integration touchpoints)
- Data entities (domain-specific entities)
- State machines (domain-specific lifecycle objects)
- Operator personas (domain-specific roles if any)
- Failure modes (domain-specific failure scenarios)
- Regulatory scope (CIP, PUC, environmental, safety regs specific to domain)

Produce a coverage matrix: domains × coverage dimensions. Populate with:
- STRONG (coverage equivalent to Distribution)
- ADEQUATE (covered but shallower than Distribution)
- WEAK (named but not specified)
- MISSING (not in Discovery at all)

For each WEAK or MISSING cell:
- Gap description
- Domain SME to engage
- Workshop or investigation task
- Severity (how much this matters for MVP vs. later phases)

End with:
- DOMAIN-SPECIFIC QUESTIONS to raise in WS-13 for each under-covered
  domain
- RECOMMENDED ADDITIONAL DOMAIN SME ENGAGEMENTS
- IMPACT ON MVP SCOPE: does under-coverage of a non-MVP domain affect
  current plans, or can it defer?

DISCOVERY ARTIFACTS TO ANALYZE:
{{paste feature catalog, FR doc, BR register, or reference them as attached}}
```

---

## P1-14 — Overall Discovery Quality Posture Review

**Purpose:** Chief Architect's assessment summary for the Baseline Acceptance Memo. Run near end of validation window (Week 5-6).

**When:** Workshop WS-16 preparation.

**Inputs:** Gap Register fully populated, augmented artifacts, validation checklist completed.

**Output target:** Baseline Acceptance Memo; executive communication.

### Prompt

```
Produce an overall Discovery quality posture assessment based on the
validation work completed.

SECTIONS:

1. HEADLINE ASSESSMENT (2-3 sentences)
   Overall Discovery quality after validation and augmentation. State
   honestly: Strong / Adequate for Design Entry / Adequate with Caveats /
   Requires Further Work.

2. QUALITY BY CATEGORY
   For each of the 10 checklist categories, summarize:
   - Original Discovery quality (Strong / Adequate / Weak)
   - Post-validation quality after augmentation
   - Residual gaps (count and severity)
   - Readiness for Design consumption

3. KEY GAPS CLOSED DURING VALIDATION
   Top 10 gaps that the validation window successfully addressed.

4. RESIDUAL GAPS CARRIED INTO DESIGN
   Gaps that could not be closed in 4-6 weeks but have dispositions:
   - Closed in Design phase (timing and owner)
   - Deferred to Build
   - Accepted as residual risk

5. STRENGTHS OF THE DISCOVERY OUTPUT
   What Discovery produced well (important to acknowledge; not only
   criticism).

6. AREAS WHERE DISCOVERY WAS DISPROPORTIONATELY WEAK
   Honest assessment — which categories were most affected by the
   frontend-led leadership and required the most augmentation.

7. IMPACT ON PROGRAM PLAN
   Any changes to Design phase scope, duration, or approach based on
   validation findings.

8. KEY RISKS FROM RESIDUAL GAPS
   Top 3-5 risks carried forward; each with early warning indicator and
   Design-phase mitigation approach.

9. RECOMMENDATIONS FOR DESIGN PHASE
   - Architectural posture recommendations (what to emphasize given gaps)
   - Order of ADR work (which architectural decisions to sequence first)
   - Integration work sequencing
   - Specific workshops/validations to continue into Design

10. BASELINE ACCEPTANCE RECOMMENDATION
    - Accept baseline for Design entry (YES / YES with conditions / NO)
    - Specific conditions if applicable
    - Signatories required

INPUTS:
{{paste Gap Register summary counts and top items, validation checklist
   summary, augmented artifact list, any other validation window outputs}}

PROGRAM CONTEXT:
{{standing context block applies}}
```

---

## P1-15 — Validation Workshop Facilitation Aid

**Purpose:** Pre-workshop brief that ensures the workshop produces specific, actionable outputs rather than open-ended discussion.

**When:** Before each workshop (WS-01 through WS-16).

**Inputs:** Workshop topic, attendees, prior artifacts relevant to topic.

**Output target:** Workshop agenda and facilitation aid; outputs populate `7. Validation Workshops` and category-specific tabs.

### Prompt

```
Produce a facilitation aid for the validation workshop described below.
The workshop must produce specific, actionable outputs — not open-ended
discussion. Time-boxed.

WORKSHOP BRIEF:

TOPIC: [e.g., SCADA/EMS Integration Deep-Dive]
DURATION: [half-day or full-day]
LEAD: [role]
ATTENDEES: [roles]

PRODUCE:

1. WORKSHOP OBJECTIVES (3-5 specific, measurable)
   Each objective should state what artifact or decision the workshop
   produces. Avoid "discuss" — use "decide," "document," "validate,"
   "produce."

2. PRE-WORKSHOP PREP (24-48 hours before)
   - What artifacts attendees must review in advance
   - What questions they should come prepared to answer
   - What information to bring (specific data, logs, contracts, etc.)

3. AGENDA with timing
   For each segment:
   - Duration
   - Activity (not just topic)
   - Output produced
   - Who owns the output

4. KEY QUESTIONS the workshop must answer
   Specific, enumerated, non-ambiguous.

5. DECISION POINTS the workshop must resolve
   For each: options under consideration, decision criteria, who decides.

6. RED-FLAG SIGNALS that would extend scope
   Things that, if they emerge, should be acknowledged and parked (not
   addressed in this workshop) to keep scope bounded.

7. OUTPUTS and where they go
   Each output: name, format, owner, target tab in phase1-artifacts.xlsx.

8. FOLLOW-UPS
   Anticipated follow-up workshops or actions if certain findings emerge.

9. FACILITATION TIPS
   Specific to this workshop topic: where discussion tends to derail,
   how to bring it back, how to get specific answers.

10. SUCCESS CRITERIA
    How the lead knows the workshop succeeded (vs. "we talked about it").

CONTEXT FOR FACILITATOR:
{{paste prior artifacts relevant to the workshop — current integration
   description for WS-01, existing state machine notes for WS-07, etc.}}
```

---

## How These Prompts Feed the Workbook

| Prompt | Output flows to |
|--------|----------------|
| P1-01 Gap Analysis | `2. Gap Register` |
| P1-02 Feature Catalog | `2. Gap Register` (Feature Catalog) + augments Feature Parity Matrix in scope workbook |
| P1-03 FR Depth | `2. Gap Register` (Functional Requirements) |
| P1-04 Rule Extraction | `8. Code Archaeology` tracker + BR Catalog in scope workbook |
| P1-05 Rule Enrichment | BR Catalog updates in scope workbook |
| P1-06 NFR Grounding | `2. Gap Register` (NFRs) + augments NFR Tracker in scope workbook |
| P1-07 Integration Depth | `4. Integration Augmentation` (one column per integration) |
| P1-08 System Inventory | `2. Gap Register` (System Inventory) |
| P1-09 Data Model | `2. Gap Register` (Data Model) |
| P1-10 State Machine | `5. State Machine Register` + transition table artifacts |
| P1-11 Failure Modes | `6. Failure Mode Catalog` |
| P1-12 Storm-Day | Workshop WS-11 output; NFR grounding inputs |
| P1-13 Domain Coverage | `2. Gap Register` (Domain Coverage) |
| P1-14 Posture Review | Baseline Acceptance Memo (executive artifact) |
| P1-15 Workshop Aid | Per-workshop agenda and facilitation notes |

---

## Typical Phase 1 Cadence

**Week 1:**
- Run P1-01 against each Discovery artifact (5-8 runs) → populate Gap Register
- Run P1-07 for SCADA/EMS and Field Work integrations (P1-15 for each workshop prep)
- Workshops: WS-01, WS-02
- Start vendor code archaeology Batch B-01 (P1-04 ongoing)

**Week 2:**
- Run P1-07 for GIS and CIS
- Run P1-09 data entity analysis
- Workshops: WS-03, WS-04, WS-05
- Vendor: Batches B-02, B-03

**Week 3:**
- Run P1-10 for Trouble Card, Switching Order, Switching Step state machines
- Workshops: WS-06, WS-07, WS-08
- Vendor: Batch B-04 (safety-critical switching step execution)

**Week 4:**
- Run P1-10 for Clearance, other workflow objects
- Run P1-11 for failure modes across external and internal scenarios
- Run P1-12 storm-day modeling
- Workshops: WS-09, WS-10, WS-11
- Vendor: Batches B-05, B-06

**Week 5:**
- Run P1-06 NFR grounding across all NFRs
- Run P1-13 domain coverage verification
- Workshops: WS-12, WS-13, WS-14 (BR batch review ongoing since week 2)
- Workshop WS-15: Gap Register disposition

**Week 6:**
- Consolidate augmented artifacts
- Run P1-14 overall posture review
- Workshop WS-16: Baseline Acceptance

---

## Discipline Reminders

1. **Time-box rigorously.** If a prompt's output suggests deep investigation, park it in the Design phase backlog with a Gap Register entry. Do not expand validation into a second Discovery.

2. **Don't re-open behavioral decisions.** If Discovery agreed a feature works a certain way and operators concurred, do not relitigate. Validation is for systems-level grounding, not re-deciding scope.

3. **Product Council validates safety-critical findings.** Any safety-critical rule, state machine invariant, or override policy gets explicit operator validation before entering the augmented baseline.

4. **Chief Architect + Senior Engineer Deputy sign every output.** Dual signature discipline applies from Phase 1 onward.

5. **Version-control every artifact.** Augmented versions supersede Discovery versions but both are preserved for traceability.

6. **Gaps without dispositions are incomplete.** Every row in Gap Register must have a disposition by Week 5 review.
