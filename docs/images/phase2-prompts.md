# Phase 2 — Detailed Design
## Prompt Pack

> **Phase goal:** Translate validated Discovery outputs into architectural decisions, epic/story decomposition, and design artifacts sufficient to begin Build.
>
> **Who uses these prompts:** Chief Architect (primary), Senior Engineer Deputy (co-author), Integration Architect, Data Architect, Security Architect, Product Owners, Senior Engineers as architecture-leading tech leads.
>
> **Where outputs go:** Phase 2 workbook (`phase2-artifacts.xlsx`) tabs. Each prompt maps to a specific tab.
>
> **Standing context block (paste once per session, same as Phase 1):** see Phase 1 prompt pack.

---

## Prompt Index

1. [P2-01 — Feature → Epic Decomposition](#p2-01--feature--epic-decomposition)
2. [P2-02 — Epic → Initial User Story Breakdown](#p2-02--epic--initial-user-story-breakdown)
3. [P2-03 — ADR Drafting](#p2-03--adr-drafting)
4. [P2-04 — Architecture Challenger (Why Not X)](#p2-04--architecture-challenger)
5. [P2-05 — Legacy Pattern Detection](#p2-05--legacy-pattern-detection)
6. [P2-06 — Technology Selection Rationale](#p2-06--technology-selection-rationale)
7. [P2-07 — C4 System Context & Container Diagram](#p2-07--c4-system-context--container-diagram)
8. [P2-08 — C4 Component Diagram (per service/module)](#p2-08--c4-component-diagram)
9. [P2-09 — Data Model & Schema Design](#p2-09--data-model--schema-design)
10. [P2-10 — Integration Contract Design](#p2-10--integration-contract-design)
11. [P2-11 — NERC CIP Applicability Assessment](#p2-11--nerc-cip-applicability-assessment)
12. [P2-12 — Safety-Critical Design Review](#p2-12--safety-critical-design-review)
13. [P2-13 — NFR Realization Design](#p2-13--nfr-realization-design)
14. [P2-14 — Cross-Cutting Concern Design](#p2-14--cross-cutting-concern-design)
15. [P2-15 — Data Migration Strategy Design](#p2-15--data-migration-strategy-design)
16. [P2-16 — Technical Spike Specification](#p2-16--technical-spike-specification)
17. [P2-17 — Design Review Checklist](#p2-17--design-review-checklist)
18. [P2-18 — Design Phase Risk Identification](#p2-18--design-phase-risk-identification)

---

## P2-01 — Feature → Epic Decomposition

**Purpose:** Structure validated feature catalog into business-capability-aligned Epics.

**When:** Early Phase 2, after Feature Parity Matrix is baselined.

**Inputs:** Validated feature catalog (post-Phase 1 augmentation).

**Output target:** `2. Epic Register` tab.

### Prompt

```
Decompose the feature catalog into Epics organized by business capability
(stream-aligned), not by technical layer. Each Epic should:

- Represent a coherent business capability
- Include 5–20 related features
- Decompose into 5–20 user stories (per Epic)
- Map to one or more program quarters
- Have a single business owner

For each proposed Epic, produce:

EPIC ID: E-[STREAM-CODE]-[NUMBER]
   Stream codes: OPS (operations), AST (asset management), INT (integration),
   PLT (platform)
NAME: Short descriptive name
DESCRIPTION: 2-3 sentence business capability description
BUSINESS OUTCOME: What business value this Epic delivers when complete
   (quantified where possible)
INCLUDED FEATURES: Feature IDs from the catalog that roll up here
OWNER (Stream PO / role): Who owns this Epic's outcome
PRIORITY: MoSCoW (Must / Should / Could / Won't)
MVP CANDIDATE: Yes / No / Partial — does this Epic need to be in MVP?
TARGET QUARTER: Best estimate (Q[N]Y[N])
DEPENDENCIES: Other Epics, ADRs, or integrations this depends on
ESTIMATED SIZE: T-shirt (S/M/L/XL) with brief rationale
   S = <20 story points, M = 20-50, L = 50-100, XL = 100+
SUCCESS METRICS: How we measure this Epic's success (operational metric,
   business metric, user satisfaction)

After listing Epics, provide:

1. FEATURE COVERAGE CHECK — confirm every feature in the catalog is
   assigned to at least one Epic. Flag any features that don't fit cleanly.
2. GAP ANALYSIS — features suggested by FRs, BRs, or NFRs but not in the
   feature catalog, which should become Epics or features:
   - Observability features (metrics, alerts, dashboards)
   - Audit features (retrieval, export, evidence)
   - Admin tooling (config, feature flags, user management)
   - Data fix / operational workflows
   - Training / simulation features
3. EPIC DEPENDENCY MAP — which Epics depend on which; ordering implications
4. RECOMMENDED MVP EPIC SET — Epics (or portions) for first deployment
5. POST-MVP SEQUENCING — recommended order for non-MVP Epics across
   subsequent quarters
6. RESOURCE SIGNAL — if total MVP story points seem unachievable given
   team capacity, flag the ratio and suggest scope adjustments

FEATURE CATALOG:
{{paste validated feature catalog from Phase 1 output}}

FR AND BR REFERENCES (for gap analysis):
{{optionally paste FR and BR summaries so the AI can detect features
   implied by requirements but missing from catalog}}
```

---

## P2-02 — Epic → Initial User Story Breakdown

**Purpose:** Pre-Build decomposition of MVP Epics into initial stories. Full refinement happens in Phase 3, but Phase 2 needs enough story breakdown for sizing and sequencing.

**When:** After Epic Register populated, for MVP Epics only.

**Inputs:** One Epic at a time with its included features, relevant BRs, applicable NFRs.

**Output target:** Initial stories flow into Phase 3 workbook's User Story Backlog tab (built in Phase 3 pack). During Phase 2, they appear as story IDs and titles in the Epic Register's `Stories Count` column.

### Prompt

```
Decompose the Epic below into initial user stories. These stories will be
refined further in Phase 3, but produce enough detail now to:
- Size the Epic accurately
- Identify dependencies across stories
- Sequence for build
- Spot gaps (missing stories Epic implies but wasn't clear)

Each story must be:
- A vertical slice (UI + logic + data + integration, as applicable),
  NOT a horizontal layer (not "build the DB schema")
- Independent enough to fit in one sprint (<=13 story points)
- Valuable on its own (delivers operator-visible value OR clearly defined
  platform capability)

For each story:

STORY ID: US-[EPIC-SHORT]-[NUMBER]
TITLE: Descriptive title (6-10 words)
CONNEXTRA FORMAT:
   As a [persona]
   I want [capability]
   So that [business outcome]

RELATED BRs: BR-IDs referenced
RELATED FRs: FR-IDs referenced
RELATED NFRs: NFR-IDs applicable
STATE MACHINE TRANSITIONS: SM-IDs and specific transitions exercised
   (if applicable)

HIGH-LEVEL AC (1-3 bullet points; full Gherkin AC in Phase 3):
   Key behaviors the story must deliver

DEPENDENCIES:
- Stories this depends on (IDs)
- Integrations this requires (INT-IDs)
- Infrastructure or platform capability required
- Data migration dependencies (MIG-IDs)

ESTIMATED POINTS: Fibonacci (1, 2, 3, 5, 8, 13)
   — stories >13 points must be split; flag if splitting needed

SAFETY-CRITICAL: Y/N
   — safety-critical stories require additional review discipline

AUDIT REQUIREMENTS: what audit records must be produced

After listing stories:

1. EPIC COMPLETENESS CHECK — do these stories collectively deliver the
   Epic's stated business outcome? If not, what's missing?
2. TOTAL POINTS for Epic sizing
3. CROSS-STORY DEPENDENCIES — which stories must be done before others
4. RECOMMENDED SPRINT SEQUENCING
5. INTEGRATION DEPENDENCIES — which integration contracts must be
   signed before stories start
6. SPIKE RECOMMENDATIONS — stories needing a spike before committing
7. SAFETY-CRITICAL STORIES — list them separately for Product Council
   awareness

EPIC:
{{paste Epic entry from register}}

INCLUDED FEATURES:
{{paste features from catalog that roll up to this Epic}}

APPLICABLE BUSINESS RULES:
{{paste BRs from catalog for this Epic's domain}}

APPLICABLE NFRs:
{{paste NFRs that apply to this Epic}}

RELEVANT STATE MACHINES (if any):
{{paste state machine spec for workflow objects this Epic touches}}
```

---

## P2-03 — ADR Drafting

**Purpose:** Produce a proper ADR from a decision context. Every significant architecture decision gets one.

**When:** Any architectural choice being made. ADR is written BEFORE decision is accepted, not after.

**Inputs:** Decision context, options considered, current thinking.

**Output target:** `1. ADR Log` tab (status: Proposed or Under Review until DAB accepts).

### Prompt

```
Draft an Architecture Decision Record (ADR) for the decision below.

Use this structure:

# ADR-[NUMBER]: [Decision Title]

## Status
[Proposed / Under Review / Accepted / Superseded by ADR-X / Deprecated]

## Date
[YYYY-MM-DD]

## Deciders
[Chief Architect, Senior Engineer Deputy — always; plus safety/compliance
where applicable]

## Context
- What forces are at play?
- What problem are we solving?
- What constraints apply specifically: on-prem / no containers / NERC CIP
  applicability / safety-critical / team skill constraints / existing
  enterprise standards
- What is the status quo if we make no decision?
- Why now?

## Decision
State the decision clearly:
- What we ARE doing
- What we are explicitly NOT doing (equally important)
- Concrete enough that an engineer could start implementing

## Alternatives Considered
For each alternative (minimum 2; "do nothing" is valid):
- DESCRIPTION
- PROS
- CONS
- WHY NOT CHOSEN (specific; don't rely on vague reasons)

Flag if an alternative is being rejected for weak reasons ("too complex"
without elaboration, "team doesn't know it" without training consideration).

## Consequences

### Positive
- What capabilities this decision enables
- What risks it mitigates

### Negative
- What costs, complexity, or risks it introduces
- What capabilities we give up
- What becomes harder to change later

### Neutral
- Trade-offs that balance

## Compliance Impact
- NERC CIP considerations (refer to applicable standards)
- State PUC / regulatory considerations
- Audit evidence implications

## Security Impact
- Security posture change
- New attack surface introduced
- Mitigations required

## Cost Impact
- Licensing, infrastructure, operational cost
- Training cost
- Cost of reversal if decision proves wrong

## Operational Impact
- On-call load change
- Runbook additions required
- Monitoring / alerting implications
- Deployment complexity change

## Safety Impact (if applicable)
- Does this decision affect safety-critical logic?
- What invariants must be preserved?
- What additional rigor applies to implementation?

## Implementation Notes
- Key implementation considerations
- Migration path from current state (if applicable)
- Rollback strategy
- Validation approach (how we know it's working)

## Related Decisions
- ADRs this builds on
- ADRs this might supersede
- External references (documentation, RFC, standards)

---

Be specific. Use actual names of technologies, actual numbers for scale,
actual trade-offs. Avoid vague phrases like "better performance" — state
how much better and under what conditions.

If after drafting the ADR you identify questions that should block
acceptance, list them at the end under "OPEN QUESTIONS". Flag anything
that a legacy-pattern reviewer (P2-05) would call out.

DECISION TO DOCUMENT:
{{describe the decision, problem, options considered, current thinking}}

RELEVANT CONSTRAINTS:
{{paste applicable NFRs, organizational constraints, existing ADRs}}
```

---

## P2-04 — Architecture Challenger

**Purpose:** Stress-test an architectural proposal before it gets accepted. Catches legacy patterns, weak alternative rejections, safety gaps.

**When:** Against every significant ADR draft before DAB acceptance; against major design documents; when something feels "settled too quickly."

**Inputs:** Draft ADR or design document.

**Output target:** Findings feed back into ADR revision. DAB decision log captures challenger outcomes.

### Prompt

```
Your role in this prompt is SKEPTICAL CHALLENGER. Do not validate the
proposal; find its weaknesses. The proposal's author has already argued
for it; your job is to apply pressure from a different direction.

CONTEXT: Our Chief Architect is capable but comes from the legacy system
vendor. They carry invaluable knowledge and may unconsciously carry
legacy-pattern instincts. Our on-prem, non-containerized deployment
imposes constraints, but those constraints cannot be used as blanket
justification for arbitrary choices.

Apply these ten lenses to the proposal:

1. LEGACY-PATTERN CHECK
   Does this proposal replicate patterns from the legacy system
   unnecessarily? What would a greenfield architect with current best
   practice choose instead? If the answer is different, why the difference?

2. ALTERNATIVES EXAMINATION
   What are the 2-3 strongest alternatives that were NOT adopted?
   Examine the rejection reasoning for each:
   - Is it factually accurate?
   - Does it rely on assumption not evidence?
   - Is it based on team skill (fixable) vs. fundamental unsuitability?
   - Is it circular (rejecting X because it's not like Y, then choosing Y)?

3. CONSTRAINT VALIDATION
   Does this proposal use "on-prem" or "no containers" as a blanket
   justification where modern practice could still be applied?
   Are constraints being invoked accurately?

4. STORM-DAY STRESS TEST
   How does this proposal behave at 5-10x normal load?
   What is the failure mode? Recovery behavior?

5. SAFETY CRITIQUE
   Where could this introduce a safety-critical gap?
   What assumption, if wrong, would cause disastrous failure?
   Are safety invariants preserved across all states and failure modes?

6. COMPLIANCE CRITIQUE
   NERC CIP exposure?
   Audit evidence gaps?
   Access control gaps?

7. OPERATIONAL BURDEN
   What does this mean for on-call at 3am?
   How many runbook pages does this add?
   How does it change incident response?

8. EVOLUTION TEST
   If SCADA vendor changes in 5 years, what becomes expensive?
   If we need to split this monolith later, what becomes hard?
   If NFRs tighten, where's the headroom?

9. TEAM SKILL ALIGNMENT
   Does this proposal match team capability?
   Are we assuming skills we don't have?
   Is there a knowledge concentration risk?

10. UNKNOWN UNKNOWNS
    What question should have been asked but wasn't?
    What is the author clearly NOT thinking about?
    What would look obvious to a new reviewer with fresh eyes?

For each lens, provide:
- SPECIFIC CONCERN or QUESTION (not generic)
- WHY IT MATTERS
- WHAT EVIDENCE WOULD RESOLVE IT

End with:

TOP 3 RISKS in this proposal
TOP 3 QUESTIONS for the architect to answer before acceptance
CHALLENGER RECOMMENDATION:
   - Accept as-is
   - Accept with documented caveats
   - Revise and re-review
   - Reject — propose alternative
   - Needs spike before decision

Be direct. Do not soften. The point is useful friction, not consensus.

PROPOSAL TO CHALLENGE:
{{paste ADR draft or design document}}
```

**Tip:** Run this even on proposals you think are solid. The times it surfaces nothing are valuable confirmation. The times it surfaces something are even more valuable.

---

## P2-05 — Legacy Pattern Detection

**Purpose:** Specific scan for legacy thinking patterns in design artifacts. Especially important given the Chief Architect's origin.

**When:** Against every significant design artifact — ADRs, data models, integration specs, component designs.

**Inputs:** Design artifact.

**Output target:** Findings drive artifact revision; systemic patterns flagged in DAB.

### Prompt

```
Scan the design artifact below for legacy thinking patterns. These are
patterns that were appropriate in the original legacy system's era but
are suboptimal in modern practice.

Common legacy patterns to detect:

1. SCHEMA-SHAPE COUPLING — tables that match legacy screen layout rather
   than domain concepts. Field names that hint at UI labels.

2. BATCH-ORIENTED THINKING — overnight batch window assumptions where
   real-time is feasible and desirable.

3. STORED-PROCEDURE-CENTRIC LOGIC — business logic in database instead of
   application layer. "The DB will enforce this" as a pattern.

4. TIGHT UI-DATA COUPLING — data model driven by UI needs. Tables named
   after forms or screens.

5. MONOLITHIC SESSION STATE — server-side session assumptions; features
   that only work because of session context.

6. POLL-DON'T-EVENT — polling for changes when event-driven is available
   from the source system.

7. ABSENT ABSTRACTION — SCADA vendor specifics (field names, codes,
   protocols) leaking into core domain. Field-work-vendor specifics
   leaked. No Anti-Corruption Layer.

8. AUDIT-AS-AFTERTHOUGHT — audit logging treated as a side concern.
   "We'll add audit later." Audit tied to specific code paths rather than
   being first-class.

9. NON-IDEMPOTENT INTEGRATION — integrations that assume exactly-once
   delivery. Retry-unsafe operations.

10. HARDCODED WORKFLOWS — state machines encoded in procedural code rather
    than as explicit state machine definitions. Switch statements
    enumerating state transitions.

11. CONFIG-BY-CODE — environment-specific behavior in code rather than
    configuration. Hardcoded hostnames, paths, timing.

12. MISSING OBSERVABILITY — no instrumentation plan. Debug approach is
    "query the database." Logs that say "done" without context.

13. TIME-BOUND ASSUMPTIONS — "runs every night at 2am" baked into logic
    rather than expressed as scheduling policy.

14. SYNCHRONOUS CROSS-SERVICE CALLS — chains of synchronous calls where
    async would be resilient. No circuit breaker or timeout.

15. DUAL-WRITE CONSISTENCY — "write to both the DB and the queue" without
    outbox, transactional guarantees, or reconciliation.

16. MISSING TENANCY / DISTRICT SCOPE — implicit assumption of global
    scope where district-level scoping should be explicit.

17. UI AS ONLY ACCESS PATH — no programmatic API for operator-level
    operations, making automation and testing harder.

18. AD-HOC AUTHORIZATION — permission checks scattered in code rather
    than expressed declaratively.

For each instance found in the artifact:

LOCATION: where in the artifact
PATTERN: which of the above (or new pattern not on list)
WHAT IT SUGGESTS: the specific concern
WHY PROBLEMATIC: the risk if left
MODERN ALTERNATIVE: how it would be done fresh
CERTAINTY: Definitive / Probable / Possible (author may have justification)
SEVERITY: Cosmetic / Operational / Architectural / Safety-relevant

Also note explicitly:

- Patterns that are GOOD and worth preserving (not only criticism)
- Questions to ask the author to clarify intent before escalating
- SYSTEMIC PATTERNS (same kind of legacy thinking across multiple parts
  of the artifact — a signal of a broader mindset to address)
- UNCERTAINTY FLAGS where you can't tell if it's legacy thinking or
  justified choice

End with:

- Total instances detected by severity
- Systemic pattern assessment
- Recommended conversation with the author

The goal is not to criticize the author but to create useful friction so
modern practice choices get explicitly compared to legacy defaults.

DESIGN ARTIFACT:
{{paste design doc, ADR, data model, integration spec, etc.}}
```

---

## P2-06 — Technology Selection Rationale

**Purpose:** Structured technology evaluation producing defensible selection for an ADR.

**When:** Before writing an ADR for a technology choice (messaging, DB, cache, framework, etc.).

**Inputs:** Technology category, candidates, current requirements.

**Output target:** Input to ADR (P2-03) and `5. Technology Selection` tab.

### Prompt

```
Produce a technology selection analysis. Feed the result into ADR-[NUMBER].

1. SELECTION SCOPE
   - What is being selected (specific category, specific purpose)
   - Where will this technology sit in our architecture
   - What will it replace or avoid

2. REQUIREMENTS (from applicable NFRs and constraints)
   - Functional requirements it must support
   - Non-functional requirements (specific numbers, not "fast")
   - Operational requirements (deployability, on-prem, team skill fit)
   - Compliance requirements (NERC CIP if applicable, audit retention,
     encryption)
   - Constraint fit (on-prem, no containers, enterprise standards)

3. CANDIDATE LIST (minimum 3, maximum 5 realistic)
   Exclude candidates with obvious disqualifications up-front with stated
   reason, so they don't clutter the analysis.

4. PER-CANDIDATE EVALUATION
   For each candidate, evaluate across:
   - MATURITY: production use at scale, version stability
   - INDUSTRY ADOPTION: who else uses it in similar context
   - ON-PREM FIT: can it deploy without cloud dependencies?
   - HA/DR STORY WITHOUT CONTAINERS: what's the HA architecture on VMs?
   - PERFORMANCE AT STORM SCALE: evidence or credible projection
   - LICENSING: model, cost, redistribution constraints, trajectory
     (flag license changes like Elasticsearch/Redis/HashiCorp recent moves)
   - OPERATIONAL BURDEN: complexity of running it at 3am
   - SKILL AVAILABILITY: team fluency; market hiring ease
   - COMMUNITY/VENDOR SUPPORT: active community; commercial support
     options; response times
   - COMPLIANCE POSTURE: audit logs, access controls, encryption defaults,
     FIPS 140-2 if needed
   - INTEGRATION ECOSYSTEM: libraries, tools, tooling maturity
   - LONG-TERM VIABILITY: 5+ year outlook
   - MIGRATION PATH AWAY: if we need to change later, how hard
   - SECURITY TRACK RECORD: CVE history, response practices

5. WEIGHTED COMPARISON MATRIX
   Criteria (rows) × Candidates (columns), each cell scored 1-5 with
   brief justification. Weight each criterion by importance (1-5).
   Weighted score = score × weight, summed per candidate.

6. RECOMMENDATION
   - Selected candidate
   - Specific rationale (not just "highest score")
   - Residual concerns even about the winner
   - Conditions for revisiting the choice

7. RISKS of the recommendation
   - What happens if chosen candidate underperforms
   - What happens if licensing changes
   - What happens if vendor/project direction changes

8. ADOPTION PLAN OUTLINE
   - Onboarding steps
   - Training needs
   - First use case to validate
   - Metrics to confirm fit

Do not recommend based on popularity. Do flag candidates being excluded
for weak reasons (e.g., excluded because unfamiliar but learning cost is
acceptable).

TECHNOLOGY SELECTION SCOPE:
{{describe what needs to be selected and for what purpose}}

CURRENT REQUIREMENTS:
{{paste applicable NFRs, constraints, existing architectural decisions}}

INITIAL CANDIDATE IDEAS:
{{list what's currently under consideration; OK if incomplete — the
   prompt should expand if strong candidates are missing}}
```

---

## P2-07 — C4 System Context & Container Diagram

**Purpose:** Generate descriptions suitable for conversion to C4 diagrams (PlantUML, Structurizr, Mermaid, diagrams.net, or hand-drawn).

**When:** Early Phase 2. Establishes the architectural frame used for the rest of the phase.

**Inputs:** System inventory (from Phase 1), ADRs around service decomposition, integration catalog.

**Output target:** `3. Architecture Diagrams` tab — DG-001 (Context), DG-002 (Container).

### Prompt

```
Produce C4 model descriptions for Level 1 (System Context) and Level 2
(Container) for our system.

LEVEL 1: SYSTEM CONTEXT DIAGRAM

List:
- THE SYSTEM (our system, named): brief purpose
- PEOPLE (actors / user roles): role name, brief description of what they
  do with the system
- EXTERNAL SYSTEMS (integrated systems): name, purpose, relationship
- RELATIONSHIPS (edges on the diagram):
  - From → To
  - What: brief description of the interaction
  - Technology / protocol if meaningful at this level
  - Direction (who initiates)

Present as:
- Nodes list (people, external systems, our system)
- Edges list (relationships)
- Narrative summary (2-3 paragraphs describing the diagram)

LEVEL 2: CONTAINER DIAGRAM

Decompose our system into containers. A container is a deployable or
runnable unit: an application, service, database, message broker, web
server. At this level, think in VMs / process groups since we are on-prem
VM-based.

For each container:
- NAME
- TYPE: Application / Service / Database / Message Broker / Web Server /
   Batch Job / Cache / Infrastructure Service
- TECHNOLOGY: (e.g., Java Spring Boot on VM; PostgreSQL 16 with Patroni
   cluster)
- PURPOSE: one-line description
- RESPONSIBILITY: what domain area it owns
- DEPLOYED ON: VM count / infra notes
- KEY DEPENDENCIES: other containers it talks to

CONTAINER RELATIONSHIPS:
- From Container → To Container
- Interaction: synchronous HTTP / async Kafka / SQL / etc.
- Data summary: what flows

Present as:
- Container list
- Container relationship list
- Narrative summary

Explicitly address:
- Where is the modular-monolith boundary vs. strategic service separation
- Where do safety-critical modules sit and how are they insulated
- Where does the NERC CIP boundary (ESP) fall
- What crosses the ESP boundary and how (EAPs)

Flag:
- Any container whose responsibility is unclear
- Any relationship where the data contract isn't yet defined (needs P2-10)
- Any container whose technology is still TBD (needs ADR)

AVAILABLE INPUTS:
{{paste system inventory from Phase 1}}
{{paste relevant ADRs, especially service decomposition ADR}}
{{paste integration catalog}}
```

**Tip:** Output is narrative + structured lists. Feed to a diagram tool (PlantUML or Structurizr) for visual rendering.

---

## P2-08 — C4 Component Diagram

**Purpose:** Decompose one container into its components (modules/classes/packages).

**When:** Per significant container. Usually component-level detail is produced for components touching business logic or cross-cutting concerns.

**Inputs:** Container spec, module responsibilities, state machines (where applicable), cross-cutting matrix.

**Output target:** `3. Architecture Diagrams` tab (one DG entry per component diagram).

### Prompt

```
Produce a C4 Level 3 (Component) diagram description for the container below.

Components within a container are modules / packages / significant classes —
the internal structure. Think implementation-sized chunks.

For each component:

NAME
RESPONSIBILITY: single-sentence purpose (single responsibility principle
   is the lens)
DEPENDENCIES: other components inside this container that it calls
EXTERNAL DEPENDENCIES: containers or external systems it calls
INTERFACES EXPOSED: what other components or containers call this one
   (public API surface)
DATA OWNED: what data entities this component is authoritative for
   (aggregate roots)
STATE MACHINES IMPLEMENTED: SM-IDs this component implements
CROSS-CUTTING IMPLEMENTED: how this component addresses audit,
   observability, security, resilience (reference Cross-Cutting Matrix)
SAFETY-CRITICAL CLASSIFICATION: Y/N — with rationale
TESTING APPROACH: primary testing strategy (unit / integration / contract)

COMPONENT-TO-COMPONENT RELATIONSHIPS:
   For each edge:
   - Protocol (in-process method call / async event / etc.)
   - Data or command passed
   - Timing constraints if any

Address specifically:

- CLEAR MODULE BOUNDARIES: where do module-to-module dependencies flow?
  Are they one-way? Are there unintended cycles?
- DOMAIN ALIGNMENT: do components correspond to domain concepts, or are
  they technical layers (e.g., "service," "repository" as the primary
  organization would be a concern)?
- SAFETY ISOLATION: where safety-critical logic lives, how is it isolated
  from non-safety concerns? Can non-safety changes affect safety?
- FAILURE BOUNDARIES: what happens when component X fails? Does it
  cascade? Is there bulkheading?

Flag:
- Components that seem to have too many responsibilities
- Components with unclear ownership
- Dependencies that cross the container boundary unnecessarily
- Dependencies that violate aggregate boundaries from the data model

Present as:
- Component list with attributes above
- Component relationship list
- Narrative summary (2-3 paragraphs)

CONTAINER TO DECOMPOSE:
{{name and description from DG-002}}

RELEVANT MODULE RESPONSIBILITIES (from design notes):
{{paste any module-level design notes}}

APPLICABLE STATE MACHINES:
{{paste SMs that this container's components implement}}

CROSS-CUTTING MATRIX ROW FOR THIS CONTAINER:
{{paste relevant row from Cross-Cutting Matrix}}
```

---

## P2-09 — Data Model & Schema Design

**Purpose:** Move from entity inventory (Phase 1) to schema-level design with aggregate boundaries, ownership, indexing, migration considerations.

**When:** Per domain or aggregate area.

**Inputs:** Data entity catalog from Phase 1, aggregate boundary proposals, applicable BRs, volume projections.

**Output target:** Feeds data model ADR; updates DG-015 ERD; informs migration plan.

### Prompt

```
Produce a schema-level design for the domain area below. Output should be
sufficient to implement with Flyway migrations.

STRUCTURE:

1. DOMAIN OVERVIEW
   - Bounded context this covers
   - Aggregate roots within
   - Transactional consistency boundaries

2. PER-ENTITY SCHEMA

   For each entity within the bounded context:

   TABLE NAME (snake_case, no legacy names)
   PURPOSE (1-2 sentences, domain-meaningful)

   COLUMNS table:
   | Column | Type | Nullable | Default | Constraints | Notes |

   Column guidance:
   - IDs: use UUIDv7 (k-sortable) unless compelling reason otherwise
   - Timestamps: timestamptz; use now() default or triggered
   - State fields: enum or CHECK constraint where values are fixed
   - Soft-delete: only where required; prefer audit-via-event-source
   - Money / measurement: use NUMERIC with explicit precision; never FLOAT
   - Boolean: prefer explicit state machines over boolean flags
   - JSONB: for extensibility points; avoid as primary data structure

   INDEXES:
   - Primary key
   - Unique constraints with business justification
   - Query-pattern indexes (include what queries they serve)
   - Partial indexes where load profile justifies

   FOREIGN KEYS with ON DELETE behavior (restrict / cascade / set null)

   TRIGGERS (if any) with justification — prefer application logic

3. AGGREGATE BOUNDARIES
   - Which entities travel together in a transaction
   - Which entity is the aggregate root (load through it)
   - Which relationships cross aggregate boundaries (use IDs, not FK)

4. RELATIONSHIPS & CARDINALITIES
   Formal notation: One-to-One, One-to-Many, Many-to-Many

5. OWNERSHIP
   - Which service/module owns each entity's lifecycle
   - Which can read vs. write

6. INDEXING STRATEGY
   - Based on expected query patterns (list top 10)
   - Where to accept unindexed scans (low-volume admin)
   - Estimated index size

7. VOLUME PROJECTIONS
   - Row counts at migration
   - Growth rate per month
   - 5-year projection
   - Partitioning if applicable (time-based, district-based)

8. RETENTION & ARCHIVAL
   - Business retention requirement
   - Regulatory retention (NERC CIP, state, other)
   - Archival strategy and target

9. MIGRATION NOTES
   - Legacy source entity (if this replaces something)
   - Field mapping
   - Transformations required
   - Cleansing/validation needs

10. SECURITY
    - Column-level classification (public / internal / confidential /
      regulated)
    - Row-level security needs (district scope, CIP scope)
    - PII/CPNI columns identified

11. OPEN QUESTIONS
    - Decisions deferred for now
    - SME input needed

Flag as warnings:
- Legacy naming being preserved without justification
- Tables that look like UI-shape rather than domain-shape
- Denormalization choices without performance rationale
- Soft-delete flags without audit alternative

DOMAIN / AGGREGATE AREA:
{{describe the domain: e.g., "Trouble Card and related entities"}}

ENTITY INVENTORY FROM PHASE 1:
{{paste relevant entities from data catalog}}

APPLICABLE BUSINESS RULES:
{{paste BRs that constrain this data}}

VOLUME PROJECTIONS:
{{paste storm-day modeling outputs if applicable}}

LEGACY SCHEMA (for mapping):
{{paste legacy DDL or structure if available}}
```

---

## P2-10 — Integration Contract Design

**Purpose:** Produce a complete integration contract document ready for counterparty signature.

**When:** Once per significant integration. Follows Phase 1 Integration Augmentation (P1-07) output.

**Inputs:** Integration Augmentation (filled in Phase 1) + applicable ADRs.

**Output target:** `8. Integration Contracts` tab; contract document filed under version control.

### Prompt

```
Produce a signable integration contract for the integration below.

STRUCTURE:

# Integration Contract: [INTEGRATION NAME]
## Version: [X.Y] | Date: [YYYY-MM-DD]

## 1. PARTIES
- Our system (name, contact)
- Counterparty (name, contact)

## 2. PURPOSE
Integration's business purpose in 2-3 sentences.

## 3. SCOPE
- What this contract covers
- What it explicitly does NOT cover

## 4. TECHNICAL SPECIFICATION

### 4.1 Protocol & Format
- Transport protocol (Kafka / HTTPS / etc.)
- Message format (JSON schema reference / Protobuf / etc.)
- Encoding, compression

### 4.2 Message / API Catalog
   For each message type or endpoint:
   - Name
   - Direction
   - Purpose
   - Schema (full JSON schema or Protobuf, with examples)
   - Semantics (what it means operationally)
   - Frequency / expected volume

### 4.3 Versioning
- Version in message / URL
- Backward compatibility commitment
- Deprecation process and notice period

### 4.4 Idempotency
- Idempotency key per operation
- Deduplication window
- Retry-safe operations enumerated

### 4.5 Ordering
- Ordering guarantees (global / per-partition / per-entity / none)
- Partitioning strategy
- Replay behavior

## 5. SLAs

### 5.1 Availability
- Target uptime %
- Measurement window
- Exclusions (planned maintenance)

### 5.2 Performance
- Latency P50 / P95 / P99
- Throughput (events/sec or req/sec)
- Storm-day commitments (5-10x baseline)

### 5.3 Data Freshness
- How current is the data the counterparty provides
- Acceptable staleness

## 6. ERROR HANDLING

### 6.1 Error Response Catalog
   For each error the counterparty can return:
   - Error code
   - Meaning
   - Our required response (retry / alert / escalate / ignore)
   - Retry policy specifics

### 6.2 Failure Modes
- Detection mechanism for each (partial delivery, duplicate, out-of-order,
  silent loss, silent corruption)
- Response procedure

### 6.3 Reconciliation
- Frequency
- Mechanism (hash comparison, count, sample)
- Divergence threshold triggering investigation
- Divergence resolution process

## 7. SECURITY

### 7.1 Authentication
- Mechanism
- Credential lifecycle
- Rotation schedule
- Emergency rotation procedure

### 7.2 Authorization
- What operations this integration is authorized for
- Any sub-authorization (e.g., specific topics, endpoints)

### 7.3 Encryption
- In transit (TLS version, cipher suites)
- At rest (if applicable)

### 7.4 Data Classification
- Classification of data crossing this integration
- Handling requirements

## 8. OBSERVABILITY

- What we monitor
- What the counterparty monitors
- Joint incident response

## 9. CHANGE MANAGEMENT
- Process for changes to this contract
- Notice period for breaking changes
- Testing requirements before changes deploy

## 10. OWNERSHIP & ESCALATION
- Our tech contact
- Counterparty tech contact
- Escalation chain both sides
- Incident communication channel

## 11. TESTING & ENVIRONMENTS
- Available test environments
- Test environment fidelity to production
- How changes are coordinated pre-production

## 12. COMPLIANCE
- Regulatory applicability (NERC CIP for BES data)
- Audit log retention
- Evidence artifacts required

## 13. SIGNATURES
- Our side: Integration Architect + Product Owner
- Counterparty side: named equivalent

Any UNKNOWN in the source P1-07 output must be resolved before contract
signing. List remaining UNKNOWNs at the end with responsible party.

INTEGRATION:
{{paste the augmented integration spec from Phase 1 P1-07}}

APPLICABLE ADRs:
{{paste integration-related ADRs, e.g., ADR-006 outbox, ADR-015 REST/gRPC}}
```

---

## P2-11 — NERC CIP Applicability Assessment

**Purpose:** Component-by-component BES Cyber System determination and impact rating.

**When:** Per significant component (service, module, supporting infrastructure).

**Inputs:** Component description, what it processes, what it connects to.

**Output target:** `10. NERC CIP Register`.

### Prompt

```
Assess NERC CIP applicability for the component below. Err on the side of
scope inclusion; flag when uncertain.

1. BES CYBER SYSTEM DETERMINATION

   Classify as one of:
   - YES — BES Cyber System under CIP-002
   - YES (ASSOCIATED) — Associated EACMS, PACS, or Protected Cyber Asset
   - NO — Outside CIP scope
   - UNDER REVIEW — Ambiguous; Compliance Officer decision required

   Rationale referencing CIP-002 criteria (specific CIP-002 Section /
   Criterion number).

2. IMPACT RATING (if in scope)
   - High / Medium / Low per CIP-002 Attachment 1
   - Criterion number that drives the rating
   - Factors (functions performed, BES impact)

3. ELECTRONIC SECURITY PERIMETER (ESP)
   - Does this reside within an ESP?
   - Which ESP?
   - What Electronic Access Points (EAPs) does it interact through?

4. PHYSICAL SECURITY PERIMETER (PSP)
   - Within a PSP?
   - Physical access controls required

5. APPLICABLE CIP STANDARDS (list each that applies):
   - CIP-003 Security Management Controls
   - CIP-004 Personnel & Training
   - CIP-005 Electronic Security Perimeter
   - CIP-006 Physical Security
   - CIP-007 Systems Security Management (ports/services, patches, malware
     prevention, security event monitoring)
   - CIP-008 Incident Reporting & Response Planning
   - CIP-009 Recovery Plans
   - CIP-010 Configuration Change Management & Vulnerability Assessments
   - CIP-011 Information Protection
   - CIP-013 Supply Chain Risk Management

   For each applicable: specific requirements this component must meet
   (not just "applies").

6. IMPLEMENTATION IMPLICATIONS
   - Design choices required to satisfy each applicable control
   - Evidence artifacts required for audit (logs, configs, records)
   - Operational processes required (patching cadence, access review,
     vulnerability assessment frequency)

7. INTEGRATION ASSESSMENT
   - If this component integrates with components of DIFFERENT CIP scope,
     boundary requirements
   - EAP crossing requirements

8. INFORMATION PROTECTION (CIP-011)
   - Does this component handle BES Cyber System Information (BCSI)?
   - Classification of stored/transmitted data
   - Protection requirements

9. GAPS & OPEN QUESTIONS
   - Information needed to complete assessment
   - SMEs / reviewers required
   - External consultation needed

10. RECOMMENDATION
    - Proposed classification and impact rating
    - Sign-off required (Security Architect + Compliance Officer)
    - Dependencies on other component determinations

Be conservative. Flag applicability where genuinely uncertain.
Under-scoping is a compliance risk; over-scoping is a cost.
The former is much worse.

COMPONENT DESCRIPTION:
{{paste component description, its functions, what it connects to,
   data it processes}}

RELEVANT SYSTEM CONTEXT:
{{paste related components' CIP determinations if already made}}
```

---

## P2-12 — Safety-Critical Design Review

**Purpose:** Focused review of design artifacts touching safety-critical logic. Stronger discipline than general design review.

**When:** Against any design involving switching order execution, clearance, safety-critical BRs, or emergency bypass.

**Inputs:** Design artifact + related BRs (especially safety-critical) + state machine spec if applicable.

**Output target:** Findings feed design revision; documented in `6. Design Reviews`; Product Council sign-off required.

### Prompt

```
Conduct a safety-critical design review. The standard is stricter than
general design review: every safety-critical invariant must be traceably
preserved across all design paths.

1. SAFETY-CRITICAL SCOPE IDENTIFICATION
   - Which parts of this design are safety-critical?
   - Which business rules (BR-IDs, safety-critical flagged) are implicated?
   - Which failure modes would cause physical harm, grid instability, or
     regulatory violation?

2. INVARIANT ENUMERATION
   For each safety-critical invariant:
   - STATEMENT: what must always be true
   - SOURCE: BR-ID, safety manual, regulatory driver
   - IMPLEMENTATION LOCATION: where in this design
   - ENFORCEMENT MECHANISM: how the design enforces it
   - VIOLATION CONSEQUENCES: what happens if violated
   - TEST: how violation would be detected in testing

3. STATE-TRANSITION SAFETY
   For each state transition in the design:
   - Preconditions verified before transition?
   - Postconditions verified after?
   - Atomicity: can the transition partially complete leaving unsafe state?
   - Rollback: on failure, how does state return to safe?
   - Audit: is the transition logged with enough context for forensics?

4. OVERRIDE PATH REVIEW
   For each override path:
   - Authorization mechanism (role, MFA step-up, supervisor presence)
   - Reason capture (free text vs. controlled vocabulary)
   - Audit detail
   - Limits (e.g., grounding rules that cannot be overridden regardless)
   - Time-bounded vs. persistent overrides

5. FAILURE-MODE SAFETY
   Under each failure mode (integration down, DB failover, partial failure):
   - Does the system fail SAFE (no unsafe action proceeds) or fail
     PERMISSIVE (action completes with missing verification)?
   - What operator indication makes the failure visible?
   - What compensation applies?

6. CONCURRENCY SAFETY
   - Can two operators produce an unsafe combined state?
   - Are safety-relevant updates protected against race conditions?
   - Does the design assume exactly-once when at-least-once is reality?

7. RECOVERY SAFETY
   After restart, DB failover, or DR failover:
   - Can the system resume safely without operator intervention?
   - If operator intervention required, is the required action obvious?
   - Are in-flight workflows left in safe state or paused for operator?

8. TRACEABILITY
   - Can we trace each safety-critical rule to its enforcement in code?
   - Can we trace each safety-critical test case to the rule it validates?
   - Can auditors reconstruct any decision after the fact?

9. ADDITIONAL SAFEGUARDS
   - Defense-in-depth considered? (single-layer enforcement = risk)
   - Independent verification paths?
   - Safety-relevant metrics monitored?

10. FINDINGS

    For each finding:
    - Invariant at risk
    - Location in design
    - Specific concern
    - Severity: BLOCKER (design cannot proceed) / MAJOR (must fix) /
      MODERATE (fix before build) / MINOR (track)
    - Recommended remediation

    End with:
    - OVERALL SAFETY POSTURE: Acceptable / Acceptable with fixes /
      Requires redesign
    - SIGN-OFF REQUIRED: Chief Architect + Sr Eng Deputy + Product
      Council operator + Safety Officer (where applicable)

Standard of rigor: assume anything not explicitly addressed in the design
will be wrong in implementation. Call out what's implicit and demand
it be made explicit.

DESIGN ARTIFACT:
{{paste design, state machine spec, or relevant component design}}

APPLICABLE SAFETY-CRITICAL BUSINESS RULES:
{{paste BRs with safety-critical flag = Y}}

RELEVANT STATE MACHINES:
{{paste state machine transition tables}}
```

---

## P2-13 — NFR Realization Design

**Purpose:** For each NFR, show HOW the architecture realizes it — not just assert it can.

**When:** Mid-Phase 2, after major ADRs established.

**Inputs:** NFR catalog, architecture artifacts.

**Output target:** Input into NFR Tracker (in scope workbook); flagged gaps go to Gap Register or Design Risks.

### Prompt

```
For each NFR below, produce a realization plan showing HOW the design
achieves it.

For each NFR:

1. NFR ID and STATEMENT (from catalog)

2. TARGET VALUE
   - Specific number, with units and measurement window

3. ARCHITECTURAL MECHANISMS
   Components, patterns, infrastructure that contribute:
   - Component name, specific behavior, quantitative contribution to NFR

4. CONFIGURATION / TUNING
   Specific settings required (connection pool sizes, thread counts,
   buffer sizes, replication factors)

5. MEASUREMENT IMPLEMENTATION
   How the measurement method (defined during NFR grounding) is
   implemented: what's instrumented, what metric emitted, where
   aggregated, how displayed

6. TESTING APPROACH
   - Unit test contribution (if any)
   - Integration test contribution
   - Performance test contribution
   - Environment where validated

7. STORM-DAY / PEAK BEHAVIOR
   How this NFR holds (or degrades gracefully) under 5-10x load

8. INTERDEPENDENCY CHECK
   Which other NFRs depend on this one or conflict with it

9. CONFIDENCE LEVEL
   - High: evidence from spike or similar system
   - Medium: credible projection
   - Low: not yet validated — flag as risk

10. OPEN GAPS
    - What's still unclear
    - What validation is outstanding

For the full set, produce:
- NFRs with clear realization path: list
- NFRs with partial realization (gaps identified): list with specific gaps
- NFRs with no realization yet (highest risk): list with owners
- NFRs that may be unachievable at current target (escalate for renegotiation):
  list with evidence

Flag specifically:
- NFRs that depend on a single component without redundancy
- NFRs with measurement methods that cost more than the value they provide
- NFRs whose realization requires specific operational processes (not just
  architecture) — capture those processes as needed work

NFR CATALOG:
{{paste NFRs from tracker}}

ARCHITECTURE CONTEXT:
{{paste container diagram, key ADRs, HA/DR posture}}
```

---

## P2-14 — Cross-Cutting Concern Design

**Purpose:** Systematic design of audit, observability, security, resilience, compliance across all components.

**When:** Per component as component design progresses.

**Inputs:** Component design, applicable concerns.

**Output target:** `7. Cross-Cutting Matrix`.

### Prompt

```
Design cross-cutting concerns for the component below.

1. COMPONENT CONTEXT
   - Component name, purpose, type
   - Data it processes
   - Data it owns
   - External dependencies

2. AUDIT APPROACH
   - What state changes are audit-relevant
   - Audit record structure (actor, action, target, context, outcome,
     correlation ID)
   - Persistence mechanism (event-sourced log, outbox, direct write)
   - Retention and retrieval requirements
   - Safety-critical audit (stricter rules)

3. OBSERVABILITY APPROACH
   - Metrics emitted: what, cardinality, aggregation
   - Logs: level policy, what's logged at INFO vs DEBUG, what must be
     retained, what PII/CPNI must not be logged
   - Traces: what spans, baggage carried
   - Health endpoints: liveness, readiness, detailed status
   - Dashboards: key dashboards this component feeds
   - Alerts: what fires, at what threshold, to whom

4. SECURITY APPROACH
   - Authentication: service identity, user identity propagation
   - Authorization: what operations require what permissions
   - District-scope enforcement
   - Secrets handling (Vault)
   - Encryption in transit (mTLS internal, TLS external)
   - Encryption at rest (if applicable)
   - Input validation and sanitization
   - Output encoding
   - Attack-surface review (what's exposed to whom)

5. RESILIENCE APPROACH
   - Timeout policies
   - Retry policies (backoff, jitter, max attempts)
   - Circuit breaker applicable?
   - Bulkhead / thread pool isolation
   - Graceful degradation behavior
   - Failover behavior
   - Recovery after dependency outage

6. COMPLIANCE (NERC CIP if applicable)
   - BES Cyber System classification for this component
   - CIP controls applicable
   - Evidence artifacts generated
   - Access control mapping

7. DATA PROTECTION
   - Classification of data handled
   - PII / CPNI / CIP BCSI presence
   - Handling rules per classification

8. OPEN ITEMS
   - Questions deferred
   - Design decisions needed

This fills one row of the Cross-Cutting Matrix. Populate the matrix as
component designs progress.

COMPONENT TO DESIGN:
{{paste component description}}

APPLICABLE NFRs:
{{paste relevant NFRs, especially security, audit, observability,
   resilience NFRs}}

COMPONENT'S CIP REGISTER ENTRY:
{{paste relevant CIP register row if done}}
```

---

## P2-15 — Data Migration Strategy Design

**Purpose:** Per-entity migration approach with dress rehearsal plan.

**When:** Per entity or domain area during Phase 2.

**Inputs:** Entity details, legacy source, volume, retention, NFRs around migration.

**Output target:** `9. Data Migration Plan`.

### Prompt

```
Produce a migration strategy for the entity/domain below.

1. SCOPE
   - What data is in scope (time range, filters, etc.)
   - What is out of scope (archive only, purge, decommission)
   - Volume (row counts, storage size)

2. SOURCE ANALYSIS
   - Source system, schema, technology
   - Data quality profile (null rates, referential integrity, orphans,
     duplicates)
   - Known anomalies and historical oddities
   - Personally identifiable / CPNI / CIP BCSI data presence
   - Existing extract capabilities

3. TARGET MAPPING
   - Target schema and tables (reference P2-09 output)
   - Field-by-field mapping (source → transformation → target)
   - Derived fields (calculated during migration)
   - Fields with no direct source (defaults, nulls, computed)
   - Discarded source fields with rationale

4. TRANSFORMATION RULES
   - Data type conversions
   - Code / lookup value translations
   - Business rule applications during migration (e.g., deriving state
     from legacy flag combinations)
   - Data cleansing / standardization

5. MIGRATION APPROACH
   Choose ONE:
   - ONE-TIME ETL: full historical load at cutover
   - PHASED ELT: by time period, newest first or oldest first
   - TRICKLE SYNC: continuous sync during parallel run (for active data)
   - HYBRID: combinations for different parts of the entity

   Rationale for approach.

6. TOOLING
   - ETL tool or custom code
   - Orchestration (e.g., airflow, custom)
   - Restart capability from checkpoint

7. VALIDATION & RECONCILIATION
   - Row count reconciliation
   - Hash / checksum validation for row integrity
   - Business rule validation (totals, invariants)
   - Sample-based verification
   - Exception handling (rejected records workflow)
   - Sign-off approach

8. DRESS REHEARSALS
   - Number of rehearsals planned (minimum 3; more for cutover-critical)
   - Full-volume vs. subset rehearsals
   - Timing each rehearsal to size cutover window
   - Lessons-learned capture

9. CUTOVER WINDOW
   - Expected duration from rehearsals
   - If trickle-sync: delta migration on cutover

10. ROLLBACK PLAN
    - If cutover fails, reverse procedures
    - Data produced in new system while live — plan for reversal

11. PERFORMANCE & INFRASTRUCTURE
    - Source load during extract
    - Target load during load
    - Network bandwidth
    - Staging storage requirements

12. SECURITY & COMPLIANCE
    - Data in transit encryption
    - Data at rest (staging areas)
    - Access control during migration
    - Audit log preservation
    - Retention alignment

13. LEGACY DECOMMISSION
    - Read-only period after cutover
    - Data preservation for compliance
    - Decommission date and criteria

14. RISKS
    - Top 3 migration-specific risks with mitigations

15. OPEN QUESTIONS

ENTITY / DOMAIN:
{{describe entity/domain being migrated}}

SOURCE KNOWLEDGE:
{{paste source documentation, profiling results, vendor input}}

APPLICABLE NFRs:
{{paste NFRs relevant to migration (RPO, data quality, retention)}}
```

---

## P2-16 — Technical Spike Specification

**Purpose:** Define a time-boxed investigation to resolve an unknown.

**When:** Any time a design decision hinges on an unknown that can't be resolved by research or discussion.

**Inputs:** The question, its importance, available options.

**Output target:** `4. Technical Spikes`.

### Prompt

```
Specify a technical spike to resolve the question below.

SPIKE ID: SP-[NUMBER]

QUESTION (phrased as a yes/no or measurement question, not open-ended):
   [E.g., "Can Kafka sustain 5,000 events/sec with 3-broker cluster on
   our hardware?" — not "Investigate Kafka performance"]

WHY IT MATTERS:
   What decision depends on the answer? What's at risk if we decide
   without answering?

BUDGET:
   Time-box in days (typical: 2-5 days; >5 days should be multiple spikes)
   Owner (single person accountable)

METHOD:
   Specific steps to answer the question:
   - Environment used
   - Test harness / scripts
   - Measurements captured
   - Tools / libraries needed

SUCCESS CRITERIA:
   What output marks the spike as complete?
   - A specific measurement (e.g., "sustained throughput number")
   - A yes/no answer with evidence
   - A recommendation with rationale

HALT CRITERIA:
   If the spike exceeds budget, what do we learn from where we stopped?
   Capture-as-is policy: no extending budget without stakeholder approval.

OUT OF SCOPE:
   What the spike will NOT answer (prevents scope creep)

DOWNSTREAM DECISIONS:
   Which ADR(s), design choice(s), or story sizing depend on this spike's
   outcome

RISKS IF SPIKE DOES NOT COMPLETE:
   What's the fallback if time runs out without a clean answer?

ARTIFACT PRODUCED:
   - Spike report (typically 1-2 pages)
   - Any harness / data retained for reuse
   - Lessons learned

Constraints:
- Spikes are NOT feature implementation disguised as investigation
- Spikes do NOT produce production code (throwaway)
- Spikes DO produce decisions, measurements, or ruled-out options

QUESTION / CONTEXT:
{{describe the unknown, why it's blocking, what decision hinges on it}}
```

---

## P2-17 — Design Review Checklist

**Purpose:** Generate a review checklist for a specific design artifact. Ensures DAB sessions are structured.

**When:** Before each DAB session, for each artifact on the agenda.

**Inputs:** Artifact type, content.

**Output target:** Supports DAB session; captured in `6. Design Reviews`.

### Prompt

```
Generate a review checklist for the design artifact below. Checklist must
be specific to the artifact — not a generic template.

ARTIFACT TYPE: [ADR / C4 Diagram / Data Model / Integration Contract /
Component Design / Migration Plan / Other]

For this artifact type, produce:

1. COMPLETENESS CHECKS (specific to artifact type)
   E.g., for ADR: Context present, all alternatives evaluated, consequences
   both positive and negative identified, compliance impact addressed, etc.

2. QUALITY CHECKS
   E.g., rationale is specific, not vague; numbers cited; references to
   external sources specific

3. CONSISTENCY CHECKS
   E.g., consistency with existing ADRs, NFRs, state machines, data model

4. LEGACY-PATTERN CHECK (always)
   Prompts from P2-05 applied to this specific artifact content

5. SAFETY-CRITICAL CHECK (if applicable)
   Prompts from P2-12 applied to safety-relevant portions

6. COMPLIANCE CHECK (if applicable)
   NERC CIP dimensions applicable to this artifact

7. OPEN QUESTIONS IDENTIFICATION
   What's unresolved that should be called out

8. DECISION PROPOSED
   What DAB is being asked to decide — specific, not vague

9. PARTICIPANTS NEEDED
   Who must be present for a valid DAB decision on this

10. BACKGROUND REQUIRED
    What participants must read in advance

Output as a numbered checklist DAB can walk through.

ARTIFACT:
{{paste the artifact to be reviewed}}
```

---

## P2-18 — Design Phase Risk Identification

**Purpose:** Identify Phase 2-specific risks (distinct from program risks).

**When:** Monthly during Phase 2, and after significant design findings.

**Inputs:** Current state of design work, recent DAB outcomes, spike results.

**Output target:** `11. Design Risks`.

### Prompt

```
Identify risks specific to the Design phase (not program-level risks).

Focus on risks in these categories:

1. ARCHITECTURE RISKS
   - Design choices proving infeasible
   - ADRs reverted late
   - External architect review finding significant issues
   - Legacy pattern accumulation
   - Safety-critical design defects surviving into build

2. DECISION VELOCITY RISKS
   - ADR backlog vs. DAB throughput
   - Stakeholder availability for sign-offs
   - Spike budget overruns
   - Decisions deferred that block other work

3. INTEGRATION RISKS
   - Contract negotiation delays
   - Counterparty changes
   - Integration specs proving inadequate in spike

4. SCOPE RISKS
   - Business rule extraction revealing unanticipated scope
   - State machine formalization revealing missing scenarios
   - Discovery validation gaps re-emerging

5. RESOURCE RISKS (Design-specific)
   - Chief Architect capacity
   - Sr Eng Deputy bandwidth
   - Domain SME availability

6. COMPLIANCE RISKS
   - CIP scope expansion
   - Compliance officer disagreement on determinations

7. TECHNOLOGY RISKS
   - Spike results invalidating assumptions
   - Licensing model changes
   - Vendor roadmap shifts

For each risk:

RISK ID: DR-[NUMBER]
DESCRIPTION: specific (not "project could fail")
LIKELIHOOD (1-5) and IMPACT (1-5) and SCORE
TRIGGER / EARLY WARNING: observable signal
MITIGATION: specific actions already in place or planned
CONTINGENCY: what we do if risk materializes
OWNER: specific role
ESCALATION CRITERIA: when this goes to steering

Produce:
- All new risks identified
- Risks previously flagged that are now resolved (move to Closed)
- Risks that have shifted likelihood or impact since last review
- Risks that would block Design phase exit if not resolved

CURRENT DESIGN STATE:
{{paste ADR log summary, spike results, DAB outcomes, recent artifact
   reviews}}

EXISTING RISK LOG:
{{paste current Design Risk log to avoid duplicates}}
```

---

## Prompt-to-Tab Mapping

| Prompt | Output flows to |
|--------|----------------|
| P2-01 Feature → Epic | `2. Epic Register` |
| P2-02 Epic → Story | Phase 3 User Story Backlog (count tracked in Epic Register) |
| P2-03 ADR Drafting | `1. ADR Log` |
| P2-04 Challenger | Inputs to ADR revision; captured in `6. Design Reviews` |
| P2-05 Legacy Pattern | Inputs to artifact revision; systemic patterns to DAB |
| P2-06 Technology Selection | `5. Technology Selection` + triggers ADR (P2-03) |
| P2-07 C4 Context + Container | `3. Architecture Diagrams` (DG-001, DG-002) |
| P2-08 C4 Component | `3. Architecture Diagrams` (DG-00X per component) |
| P2-09 Data Model | Input to schema migrations; updates DG-015 ERD |
| P2-10 Integration Contract | `8. Integration Contracts` |
| P2-11 NERC CIP | `10. NERC CIP Register` |
| P2-12 Safety-Critical Review | Inputs to design revision; `6. Design Reviews` |
| P2-13 NFR Realization | NFR Tracker (scope workbook); gaps to Design Risks |
| P2-14 Cross-Cutting | `7. Cross-Cutting Matrix` |
| P2-15 Migration Strategy | `9. Data Migration Plan` |
| P2-16 Spike Spec | `4. Technical Spikes` |
| P2-17 Design Review Checklist | Supports DAB sessions (`6. Design Reviews`) |
| P2-18 Design Risk ID | `11. Design Risks` |

---

## Typical Phase 2 Cadence

**Month 1 (Weeks 1–4):**
- Run P2-01 against feature catalog → populate Epic Register
- Draft core platform ADRs via P2-03: language, DB, messaging, cache, deployment pattern
- Run P2-04 Challenger against each ADR draft
- Start spikes SP-01 through SP-04 (technology validation)
- DAB-001 through DAB-004

**Month 2 (Weeks 5–8):**
- Continue ADRs: outbox, audit, CQRS, auth, RBAC, secrets, observability
- Run P2-05 Legacy Pattern Detection against every ADR draft and initial design artifacts
- Run P2-07 C4 Context + Container
- Begin component designs (P2-08) for SCADA ingestion, audit service, integration gateway
- Draft integration contracts (P2-10) for SCADA, Field Work
- DAB-005 through DAB-008

**Month 3 (Weeks 9–12):**
- ADRs: service decomposition, frontend framework, API style, schema migration tooling
- Data model design (P2-09) per domain area
- Cross-cutting concerns (P2-14) per component
- NFR realization (P2-13)
- **External architect independent review** (DAB-012 dedicated session)
- NERC CIP applicability (P2-11) across components

**Month 4 (Weeks 13–16):**
- Safety-critical design reviews (P2-12) for switching order, switching step, clearance
- Remaining component designs
- Migration strategy design (P2-15) per entity
- Spike results drive ADR finalization
- DAB cadence continuing

**Month 5 (Weeks 17–20):**
- Integration contracts signed
- All MVP ADRs accepted
- All safety-critical designs Product Council signed
- Epic → Story decomposition (P2-02) for MVP Epics
- Design phase risk review (P2-18)

**Month 6 (Weeks 21–24):**
- Finalize remaining deliverables
- DAB Build Authorization review
- Design phase exit: all MVP-scope ADRs accepted, CIP register complete,
  integration contracts signed, migration plan ready for dress rehearsal,
  MVP Epics decomposed to initial story level

---

## Discipline Reminders

1. **Dual signature on every ADR.** Chief Architect + Sr Eng Deputy. This is the explicit countermeasure against unconscious legacy-pattern drift. Do not skip even when busy.

2. **Challenger prompt is non-optional.** Run P2-04 against every ADR before acceptance. Not running it is a decision you should have to justify.

3. **Safety-critical gets Product Council.** Any design touching BR-IDs flagged safety-critical requires operator-level validation. Schedule their time.

4. **Spikes are time-boxed absolutely.** Over-budget spikes get halted and captured as-is, not extended. Over-spending on one spike dilutes the phase.

5. **External architect review at Month 3.** Fresh eyes on the foundational decisions before it's too late to pivot affordably. Frame it as a check, not an audit.

6. **Legacy pattern scan regularly.** Not just on ADRs — on data models, integration specs, component designs. Patterns accumulate quietly.

7. **Don't skip C4 component diagrams for safety-critical areas.** These are where safety-critical design lives, and diagram-level clarity prevents implementation drift.

8. **NFR realization must be concrete.** "Kafka gives us durability" is not a realization. Partition counts, replication factor, consumer acknowledgment settings — that's a realization.

9. **Migration is a first-class design concern.** Treat it with the same rigor as feature design. Dress rehearsals start in Phase 3 but the plan must be complete in Phase 2.

10. **DAB minutes matter.** Decisions made in DAB without traceable minutes evaporate. Someone records; someone reviews at start of next session.
