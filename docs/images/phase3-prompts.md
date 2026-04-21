# Phase 3 — Build
## Prompt Pack

> **Phase goal:** Deliver working software sprint by sprint. MVP complete by Month 16; full scope by Month 34.
>
> **Who uses these prompts:** Sr Engs (squad leads), Senior Engineer Deputy, Product Owners, QA Automation, BAs, and individual engineers during code review and story work.
>
> **Sprint rhythm:** 2-week sprints. Monday refinement → Tuesday planning → Daily standup → Thursday demo → Friday retro on sprint-end week.
>
> **Output target:** Prompt outputs feed specific tabs in `phase3-artifacts.xlsx`. Mapping at end of this document.
>
> **Standing context block (paste once per session, same as Phase 1–2):** see Phase 1 prompt pack.

---

## Prompt Index

1. [P3-01 — Epic → Sprint-Ready Story Decomposition](#p3-01--epic--sprint-ready-story-decomposition)
2. [P3-02 — Definition of Ready (INVEST) Validation](#p3-02--definition-of-ready-invest-validation)
3. [P3-03 — Gherkin Acceptance Criteria Generation](#p3-03--gherkin-acceptance-criteria-generation)
4. [P3-04 — AC → Test Case Derivation](#p3-04--ac--test-case-derivation)
5. [P3-05 — Story Point Estimation Facilitation](#p3-05--story-point-estimation-facilitation)
6. [P3-06 — Sprint Planning Assistant](#p3-06--sprint-planning-assistant)
7. [P3-07 — Code Review: Business Rule Compliance](#p3-07--code-review-business-rule-compliance)
8. [P3-08 — Code Review: Safety-Critical Path](#p3-08--code-review-safety-critical-path)
9. [P3-09 — Code Review: Legacy Pattern Detection in Code](#p3-09--code-review-legacy-pattern-detection-in-code)
10. [P3-10 — Technical Debt Registration](#p3-10--technical-debt-registration)
11. [P3-11 — Feature Flag Strategy per Story](#p3-11--feature-flag-strategy-per-story)
12. [P3-12 — Sprint Retrospective Facilitation](#p3-12--sprint-retrospective-facilitation)
13. [P3-13 — Impediment Analysis](#p3-13--impediment-analysis)
14. [P3-14 — Refactoring Decision Framework](#p3-14--refactoring-decision-framework)
15. [P3-15 — Integration Test Scenario Generation](#p3-15--integration-test-scenario-generation)
16. [P3-16 — Sprint Demo Script & Release Notes](#p3-16--sprint-demo-script--release-notes)
17. [P3-17 — Build Phase Risk Identification](#p3-17--build-phase-risk-identification)
18. [P3-18 — Daily Status Synthesis](#p3-18--daily-status-synthesis)

---

## P3-01 — Epic → Sprint-Ready Story Decomposition

**Purpose:** Refine an Epic's initial stories (from Phase 2 P2-02) into sprint-ready form, 1–2 sprints before they enter planning.

**When:** Rolling wave refinement. Typical cadence: Mondays, 2 sprints ahead of current.

**Inputs:** Epic, its initial stories, applicable BRs, FRs, NFRs, related state machines.

**Output target:** `1. Story Backlog` tab; individual stories advance from Backlog → Refined.

### Prompt

```
Refine the stories below into sprint-ready form. Each ready story must:
- Be independently valuable (vertical slice, not horizontal layer)
- Fit in one sprint (<= 13 points; split if larger)
- Have clear acceptance criteria in Gherkin (P3-03 can be run after this)
- Have dependencies resolved or identified
- Have a clear persona and business outcome

For each story, produce or refine:

STORY ID: US-[EPIC-SHORT]-[NUMBER]
TITLE: Specific; verb + noun phrase; 6-10 words
USER STORY (Connextra):
   As a [specific persona — not "user"]
   I want [capability — not UI activity]
   So that [business outcome — measurable where possible]

DESCRIPTION: 2-3 sentence expansion; what scenarios this covers

SCOPE
- IN SCOPE: what's delivered by this story
- OUT OF SCOPE: explicitly enumerate related things NOT done here
  (prevents scope creep; enables splitting)

APPLICABLE RULES:
- Related BRs: IDs (flag safety-critical with *)
- Related FRs: IDs
- Related NFRs: IDs that constrain this story
- State machines and transitions: SM-IDs + specific transition (if any)

DEPENDENCIES
- Prerequisite stories (done or in-flight): IDs
- Integration contracts required: INT-IDs (must be signed)
- Data migration required: MIG-IDs (must be available in test env)
- Infra/platform capability required: description
- Blocking spikes: SP-IDs

AC OUTLINE (full Gherkin via P3-03):
   3-8 bullet points covering happy path, primary negative paths, edge cases,
   safety invariants (if safety-critical), audit/observability requirements

UI IMPLICATIONS (if applicable):
   Which screen, component changes, new patterns

DATA IMPLICATIONS:
   Schema changes (new Flyway migration? altered table?)
   Reference data needed
   Seed data for test

OBSERVABILITY:
   Metrics to emit
   Logs at INFO level
   Audit events produced

SAFETY-CRITICAL:
   Y/N with rationale
   Pair-program requirement (Y for safety-critical)
   Product Council demo requirement (Y for safety-critical)

ESTIMATE:
   Initial t-shirt: S/M/L/XL
   Initial points estimate (Fibonacci)
   Flag to split if > 13 points

SPLITTING CHECK:
   If complex, split proposals: describe 2-3 smaller stories that deliver
   vertical slices toward the Epic

DOR CHECKLIST (will P3-02 pass?):
   - Valued (business outcome clear) Y/N
   - AC present Y/N
   - Dependencies identified Y/N
   - Sized Y/N
   - Safety path handled Y/N
   - Team accepts they could start this next sprint Y/N

For the Epic overall after refinement:
- TOTAL STORIES refined vs total stories planned
- RECOMMENDED SPRINT ORDER (which first; which together)
- CROSS-STORY DEPENDENCIES within this Epic
- STORIES NEEDING A SPIKE BEFORE COMMITTING
- STORIES LIKELY TO SPLIT during estimation

EPIC CONTEXT:
{{paste Epic description from register}}

INITIAL STORIES (from Phase 2 P2-02):
{{paste initial story list}}

APPLICABLE BUSINESS RULES:
{{paste BRs}}

APPLICABLE NFRs:
{{paste NFRs}}

RELEVANT STATE MACHINES:
{{paste SM transition table if applicable}}
```

---

## P3-02 — Definition of Ready (INVEST) Validation

**Purpose:** Gatekeeping check. Stories that fail go back to refinement, not into sprint planning.

**When:** Before sprint planning. Run against each candidate story in the planning pool.

**Inputs:** Story in its current refined form.

**Output target:** Story's DoR Check column in `1. Story Backlog` (Y = passed).

### Prompt

```
Evaluate this story against Definition of Ready / INVEST. Report PASS or
FAIL for each criterion with specific reason.

INVEST (per story):

I — INDEPENDENT: Can this story be done without another story being done
first (or with clearly identified dependencies that are ready)?
PASS/FAIL + reason.

N — NEGOTIABLE: Is the "how" open vs. over-specified? AC should describe
observable outcomes, not implementation details. PASS/FAIL + reason.

V — VALUABLE: Is there a clear business or platform outcome? Not "build
DB schema" as a story. PASS/FAIL + reason.

E — ESTIMABLE: Does the team have enough information to size? PASS/FAIL
+ reason (if no, what additional info would make it estimable).

S — SMALL: <=13 points. PASS/FAIL; if not, propose split points.

T — TESTABLE: Can the AC be validated? Does each AC element map to a
concrete test? PASS/FAIL + reason.

ADDITIONAL DoR CRITERIA for this program:

A — AUDITABLE: Are audit event requirements specified? PASS/FAIL.

O — OBSERVABLE: Are metrics/logs requirements specified? PASS/FAIL.

S — SAFE: If safety-critical, are invariants enumerated and is Product
Council aware? PASS/FAIL.

D — DEPENDENCIES: Are all dependencies (prior stories, integration
contracts, migrations, spikes) status = ready? If not, this story is
not ready. PASS/FAIL.

C — COMPLIANT: NERC CIP applicability considered if relevant? PASS/FAIL.

OVERALL VERDICT:
- READY — all PASS; can enter sprint planning
- NOT READY — list specific failures and what would resolve each
- BLOCKED — dependency or info outside team's control

If NOT READY, provide:
- Questions to resolve
- Who can answer (role)
- Rough effort to resolve (minutes / hours / days)
- Recommended action (more refinement / defer / split / spike first)

If the story looks ready but you have CONCERNS worth flagging for sprint
planning, list them as "Planning Flags" (e.g., "Strong precondition on
SCADA reconciliation being working — validate in planning").

STORY:
{{paste refined story with AC, dependencies, etc.}}

DEPENDENCY STATUS:
{{confirm status of each dependency, or ask the user to check}}
```

---

## P3-03 — Gherkin Acceptance Criteria Generation

**Purpose:** Produce testable Gherkin AC from story description + applicable rules.

**When:** During story refinement, after P3-01 decomposition.

**Inputs:** Refined story, applicable BRs, FRs, NFRs, state machine transitions.

**Output target:** Story's AC in backlog/tracking tool; informs P3-04 test case derivation.

### Prompt

```
Generate Gherkin acceptance criteria for this story.

Guidelines:
- Format: Scenario (or Scenario Outline) / Given / When / Then
- One scenario per distinct behavior — not a single omnibus scenario
- Happy path first, then negative paths, then edge cases
- Each Given/When/Then clause is ONE specific observable condition
- Avoid UI language ("user clicks") unless story is explicitly UI-focused;
  use domain language ("operator authorizes")
- Data Tables or Examples when combinatorial
- For safety-critical stories: invariant scenarios MUST appear

Structure:

```gherkin
Feature: [Story title]

Background:
  Given [setup common to all scenarios, e.g., specific state]

Scenario: [Name — specific about what's being tested]
  Given [precondition — specific]
  And [additional precondition if needed]
  When [the action]
  Then [primary outcome]
  And [secondary observable outcome]
  And [audit event emitted with specific fields]

Scenario: [Negative path]
  ...

Scenario Outline: [Combinatorial cases]
  Given [template]
  When [template]
  Then [template]

  Examples:
    | field1 | field2 | expected |
    | val1   | val2   | result1  |
```

REQUIRED SCENARIOS for THIS story:

1. HAPPY PATH — the primary success case
2. INPUT VALIDATION — at least one negative case per key input
3. AUTHORIZATION — permission-denied case
4. PRECONDITION FAILURE — state not allowing the action
5. CONCURRENT MODIFICATION — if multi-user story
6. AUDIT EMISSION — specific audit fields in Then clause
7. OBSERVABILITY — metric or log emission verified

FOR SAFETY-CRITICAL STORIES, additionally:
8. SAFETY INVARIANT UPHELD — invariant that must never be violated
9. FAILURE-MODE SAFETY — failure causes safe state, not permissive state
10. OVERRIDE PATH — if override applies; authorization + audit
11. POSTCONDITION VERIFICATION — actual state matches intended state

FOR INTEGRATION-HEAVY STORIES, additionally:
8. INTEGRATION FAILURE RESPONSE — counterparty returns error
9. IDEMPOTENCY — retry produces same result
10. TIMEOUT HANDLING — counterparty doesn't respond

FOR UI-HEAVY STORIES, additionally:
8. LOADING STATE — async data fetch indicator
9. ERROR STATE — error messaging
10. EMPTY STATE — no data available

After generating scenarios, produce:
- COUNT of scenarios
- COVERAGE CHECK: does every AC bullet from story outline map to at
  least one scenario? If not, flag gaps.
- SAFETY COVERAGE CHECK: does every safety invariant have a scenario?
- TEST DATA REQUIREMENTS: what fixtures or reference data each
  scenario needs
- QUESTIONS FOR PO: ambiguities in the story that you cannot resolve
  without clarification (list them rather than guessing)

STORY:
{{paste refined story including AC outline from P3-01}}

APPLICABLE BUSINESS RULES:
{{paste BRs — especially safety-critical}}

APPLICABLE NFRs:
{{paste relevant NFRs}}

STATE MACHINE TRANSITIONS:
{{paste transitions this story exercises}}
```

---

## P3-04 — AC → Test Case Derivation

**Purpose:** From Gherkin AC, generate specific test cases organized by type (unit, integration, end-to-end).

**When:** After AC approved. Tests are written during the sprint; this prompt primes that work.

**Inputs:** Approved Gherkin AC + technical context.

**Output target:** Test case inventory; automation backlog; manual test scripts if applicable.

### Prompt

```
Derive test cases from these acceptance criteria. Categorize by type
and produce specific, actionable test definitions.

For each scenario, produce test cases in this structure:

SCENARIO NAME: [from Gherkin]
PRIMARY TEST TYPE: Unit / Integration / Contract / End-to-End / Manual
   (based on what's being validated)
RATIONALE: why this test type

TEST CASE(S):

For UNIT TESTS:
- COMPONENT under test
- METHOD / CLASS under test
- MOCKED DEPENDENCIES
- INPUT (specific values)
- EXPECTED OUTPUT / STATE
- EDGE CASE VARIATIONS

For INTEGRATION TESTS:
- SCOPE (which services / layers)
- TEST ENV REQUIREMENT
- EXTERNAL SYSTEM FIXTURES (mock SCADA, stub Field Work, etc.)
- DATA SETUP (what fixtures)
- STEPS
- EXPECTED OBSERVABLE OUTCOMES

For CONTRACT TESTS (for integration-heavy stories):
- COUNTERPARTY SPEC
- CONSUMER or PROVIDER side
- MESSAGE / ENDPOINT
- EXPECTATIONS

For END-TO-END / UI TESTS:
- TEST ENV REQUIREMENT
- USER PERSONA
- STEPS (operator-level, not click-level)
- EXPECTED OBSERVABLE OUTCOMES
- UI ELEMENTS NAMED (so tests survive UI changes)

For MANUAL TESTS (use only if automation impractical or for usability):
- STEPS
- EXPECTED RESULTS
- ACCESSIBILITY CHECK (if UI)
- PRODUCT COUNCIL OPERATOR REVIEW (if safety-critical)

TEST DATA REQUIREMENTS per scenario:
- Reference data needed
- Historical data needed
- Simulated events needed

AUTOMATION CLASSIFICATION per test:
- CI: run every commit
- Regression: run nightly
- Pre-release: run before each cutover
- Manual: not automated (justify)

After all scenarios, produce:
1. TEST CASE INVENTORY (count by type)
2. AUTOMATION COVERAGE TARGET for this story (should be >= 90% for
   non-UI logic, >= 80% overall)
3. TEST ENV PREREQUISITES summary
4. KNOWN GAPS: scenarios hard to test, flag for manual or explicit
   acceptance
5. SAFETY-CRITICAL COVERAGE STATEMENT: confirm every safety invariant
   has at least 2 tests (positive and negative)

STORY + GHERKIN AC:
{{paste from P3-03 output}}

TECHNICAL CONTEXT:
- Primary component(s) involved:
- Integrations touched:
- UI components touched:
- Data model touched:
{{fill in or note if unclear}}
```

---

## P3-05 — Story Point Estimation Facilitation

**Purpose:** Generate an estimation starting point and identify factors the team should discuss in planning poker.

**When:** Before estimation ceremony.

**Inputs:** Refined story + team's recent velocity for similar stories.

**Output target:** Feeds estimation ceremony; final number goes into backlog.

### Prompt

```
Produce an estimation analysis for this story. Your output is NOT the
final estimate — it's a starting point for planning poker plus a
discussion prompt.

DIMENSIONS TO ASSESS (1-5 each, 1 = low / simple, 5 = high / complex):

1. PROBLEM COMPLEXITY: how intricate is the behavior?
2. DATA COMPLEXITY: how many entities, tables, migrations?
3. INTEGRATION COMPLEXITY: how many systems touched?
4. UI COMPLEXITY: how many screens, states, interactions?
5. SAFETY CRITICALITY: lower weight if purely operational;
   higher if safety-critical (pair-programming + additional review
   overhead)
6. TEST COMPLEXITY: unit coverage straightforward or difficult?
7. UNKNOWNS: how much investigation needed beyond the story?
8. DEPENDENCIES: how many in-flight or risky dependencies?

For each dimension: score + one-sentence rationale.

COMPARABLE STORIES RECENTLY DONE:
List 2-3 similar completed stories with their actual point values.

PROPOSED POINT ESTIMATE:
Fibonacci number (1, 2, 3, 5, 8, 13).
If > 13: DO NOT estimate; require splitting. Propose split points.

CONFIDENCE:
- HIGH: very similar to past work
- MEDIUM: some novelty, but manageable
- LOW: significant unknowns — consider spike first

DISCUSSION POINTS for the team:
- Factors that could push the estimate up
- Factors that could push the estimate down
- Assumptions being made (ask PO to confirm)
- Spike-if-uncertain: would a 1-2 day spike reduce the estimate variance?

FLAGS:
- Estimation outliers: if initial estimate differs >2x from similar past
  stories, name why
- Pair-program impact: if safety-critical, add expected overhead
- Context-switch impact: if assignee will be split across 2+ stories

STORY:
{{paste refined story}}

SIMILAR PAST STORIES (last 3 sprints):
{{paste story IDs, titles, and actual point totals}}
```

---

## P3-06 — Sprint Planning Assistant

**Purpose:** Structured sprint commitment proposal given available capacity and candidate stories.

**When:** Sprint planning meeting.

**Inputs:** Capacity from `3. Capacity Calculator`, candidate stories with points, prior-sprint velocity.

**Output target:** `2. Sprint Backlog` tab commitment.

### Prompt

```
Produce a sprint plan given the capacity and candidate stories below.

1. CAPACITY ANALYSIS
   - Total team capacity this sprint (points)
   - Per-squad capacity
   - Capacity adjustments (PTO, ceremonies, spillover from prior sprint)
   - Rolling 3-sprint average velocity (committed vs. completed)
   - Recommended commitment (cap at rolling avg * 0.9 to allow for
     stretch but not over-commit)

2. CANDIDATE REVIEW
   For each candidate story:
   - Ready? (DoR passed)
   - Points
   - Assignee available
   - Dependencies status (ready? blocked?)
   - Safety-critical pair requirement
   - Flag any ready story with concerns

3. PROPOSED COMMITMENT
   - List of stories selected
   - Total points vs. recommended cap
   - Per-squad load balance
   - Safety-critical coverage (ensure Sr Eng + pair available)
   - Skills coverage (UI work vs. backend vs. integration)

4. STRETCH GOALS
   - Stories to pick up if team finishes early
   - Criteria to start stretch (e.g., all committed in review by Day 7)

5. DEFERRALS with rationale
   - Stories not included and why

6. RISK FLAGS for this sprint
   - Over-commitment risk (if commitment > rolling avg)
   - Key-person risk (multiple safety-critical stories on same person)
   - External dependency risk (story depends on un-ready external)
   - Knowledge concentration risk

7. SPRINT GOAL
   Narrative 1-2 sentence sprint goal — what this sprint delivers to
   the program.

8. DEMO OUTLINE
   What will be demo-able at end-of-sprint and to whom

CAPACITY:
{{paste from capacity calculator}}

CANDIDATE STORIES (DoR-passed):
{{paste list from story backlog (Ready status)}}

PRIOR-SPRINT VELOCITY HISTORY:
{{paste from velocity tracker, last 3-5 sprints}}

PRIOR-SPRINT CARRY-OVER:
{{list any stories carrying from prior sprint}}

KNOWN CONSTRAINTS FOR THIS SPRINT:
{{paste any team-specific constraints: PTO, conflicting ceremonies,
   key integration event, etc.}}
```

---

## P3-07 — Code Review: Business Rule Compliance

**Purpose:** Verify code implementing a story correctly reflects business rules.

**When:** PR review stage. Run by reviewer as a check before or alongside manual review.

**Inputs:** PR diff + related story + applicable BRs.

**Output target:** Review comments on PR; systemic findings go to `9. Code Review Findings`.

### Prompt

```
Review this code against its applicable business rules. Your role is NOT
general code quality (another reviewer handles that); focus on whether
the code correctly implements the rules.

FOR EACH APPLICABLE BUSINESS RULE:

RULE: BR-ID + statement
WHERE IT'S IMPLEMENTED: file, function, approximate location
IMPLEMENTATION CHECK:
- Is the rule fully implemented?
- Is the rule correctly implemented (logic matches)?
- Are PRECONDITIONS enforced?
- Are POSTCONDITIONS verified?
- Is state-dependent variation handled?
- Are edge cases handled (per rule's EDGE CASES field)?
- Is override policy correctly enforced (if rule has override policy)?

FINDINGS by severity:
- BLOCKER: rule violated or missing; must fix before merge
- MAJOR: rule handled but has a gap; should fix before merge
- MODERATE: rule handled but could be more robust; post-merge ok
- MINOR: code style or minor robustness; post-merge ok

SAFETY-CRITICAL RULES (flag = Y):
Apply stricter standard. Any MAJOR or BLOCKER finding on a safety-
critical rule is a merge blocker.

ORPHANED RULES: rules that should apply but have no corresponding code
INFERRED RULES: code that seems to enforce something NOT in the rule
   catalog — flag for catalog review (might be undocumented rule)

TEST COVERAGE CHECK:
For each rule, is there at least one test case covering it? Which test?
If not, request test before merge (or document why not).

AUDIT EVENT CHECK:
If rule requires audit evidence, is the audit event emitted with correct
fields?

OVERRIDE PATH CHECK (if rule has override policy):
- Authorization correctly checked?
- Reason capture present?
- Audit event records override context?

Provide findings as reviewable code comments: file:line specific, with
suggested change where possible.

STORY:
{{paste story + AC}}

APPLICABLE BUSINESS RULES:
{{paste BR entries with safety-critical flag and override policy}}

CODE DIFF:
{{paste PR diff}}
```

---

## P3-08 — Code Review: Safety-Critical Path

**Purpose:** Dedicated review for safety-critical code. Stricter standard than general review.

**When:** PR review for safety-critical stories. This is IN ADDITION TO P3-07.

**Inputs:** Code + safety-critical BRs + state machine transitions + applicable invariants.

**Output target:** Review comments; systemic patterns go to `9. Code Review Findings`.

### Prompt

```
Review this code under safety-critical standards. Assume anything not
explicitly addressed by the design is wrong in implementation. Demand
that implicit assumptions be made explicit. Nothing is too small to
call out.

SAFETY INVARIANTS
For each invariant relevant to this code:
- INVARIANT STATEMENT
- WHERE IN CODE is it enforced?
- WHAT HAPPENS if enforcement fails at runtime?
- CAN IT BE BYPASSED via an exception path, concurrent access, or
  failure mode?
- IS IT TESTED explicitly (positive and negative cases)?

STATE TRANSITIONS
For each state transition in this code:
- Preconditions verified? (read the code; don't trust comments)
- Postconditions verified after action completes?
- Atomicity: can the transition leave intermediate unsafe state if
  partially fails?
- Audit record written before or after transition? What if audit write
  fails — does state change proceed or abort?
- Is failure-compensating action present?

OVERRIDE PATHS
- Authorization checked (not just role — specific permission)?
- Step-up authentication required (MFA)?
- Reason captured from user (free text vs. controlled list)?
- Audit event includes override reason and authorization evidence?
- Override logged at higher level than normal action?
- Absolute rules (never-override) are genuinely unbypassable?

CONCURRENCY
- Can two threads / requests produce unsafe combined state?
- Are safety-relevant updates pessimistically or optimistically locked?
- Race conditions around check-then-act?
- Does the design assume exactly-once when at-least-once is reality?

FAILURE MODES
- Integration timeout during transition: safe or permissive default?
- DB failure during transition: rollback correct? state consistent?
- Application restart mid-transition: state recovery safe?
- Partial write: detected and compensated?

OBSERVABILITY
- Can we reconstruct the exact sequence of safety-relevant actions
  from logs + audit + metrics?
- Does an operator see safety-relevant state clearly, or are they
  guessing?

DEFENSIVE DEPTH
- Is safety enforcement single-layer or multi-layer? (prefer multi)
- Can a developer accidentally bypass via a new code path? What
  prevents that?

TESTABILITY
- Is the safety logic structured to be easily testable?
- Are safety tests testing BEHAVIOR (invariant upheld) rather than
  implementation?

FINDINGS:
- BLOCKER: must fix before merge; safety-critical defect risk
- MAJOR: must fix before merge; weaker safety posture than required
- MODERATE: should fix soon; workable in production
- MINOR: note for future

Every BLOCKER or MAJOR requires a concrete test demonstrating the
fix works.

PRODUCT COUNCIL DEMO: does the change's behavior warrant operator
demo before deployment? Y/N.

STORY:
{{paste story}}

SAFETY-CRITICAL BRs:
{{paste BRs with flag = Y}}

RELEVANT STATE MACHINE:
{{paste transition table with precondition/postcondition predicates}}

CODE DIFF:
{{paste PR diff}}
```

---

## P3-09 — Code Review: Legacy Pattern Detection in Code

**Purpose:** Scan code for the legacy patterns previously flagged in design (P2-05 at design level; this is the code-level continuation).

**When:** Every PR review, particularly for components touched by anyone from the legacy vendor background.

**Inputs:** Code diff + component context.

**Output target:** Review comments; patterns go to `9. Code Review Findings`.

### Prompt

```
Scan this code for legacy thinking patterns. These are patterns that
felt natural in the original legacy system but are suboptimal now.

SCAN FOR:

1. SCHEMA-SHAPE COUPLING — DTOs named after legacy tables; field names
   that match legacy column names even when modern domain-driven names
   would fit.

2. STORED-PROC-LIKE METHODS — massive methods that mirror legacy
   stored procedures; multiple responsibilities in one function.

3. NULL-AS-SIGNAL — using null to signal "not applicable" or "unknown"
   rather than using sealed types or explicit state.

4. DATA-FLAG STATE — boolean flags used to represent state instead of
   explicit state machines.

5. SILENT EXCEPTION SWALLOWING — catch-and-continue without logging or
   alerting; "this can't happen" comments next to broad catches.

6. STRING-TYPED STATES — state represented as strings with implicit
   values throughout the code; no enum, no constants, no validation.

7. MAGIC NUMBERS / TIMING — hardcoded timeouts, thresholds, retry
   counts, poll intervals that should be configured.

8. MANUAL SQL CONSTRUCTION — string concat to build queries (SQL
   injection risk + readability).

9. MANUAL TRANSACTION BOUNDARIES — explicit begin/commit where Spring
   or domain patterns would handle it.

10. POLLING IN CODE — manually polling for state changes in code rather
    than using events / reactive subscribers.

11. IMPLICIT DEPENDENCIES — using statics, thread locals, or globals
    instead of DI.

12. SKIPPED ABSTRACTION LAYER — SCADA vendor specifics leaked into
    business logic (field names, codes, protocols) — should be
    translated in an Anti-Corruption Layer.

13. AUDIT AS AFTERTHOUGHT — audit logging added at method end as a
    side call; not structured as a first-class event.

14. CIRCULAR DEPENDENCIES — packages that import each other; signs
    of missing domain boundary.

15. SHARED MUTABLE STATE — mutable singletons; shared maps without
    synchronization discipline.

16. EVENT-AS-NOTIFICATION-ONLY — publishing events that only notify
    (e.g., "something happened") vs. carrying meaningful payload.

17. MISSING IDEMPOTENCY — external-facing operations without
    idempotency keys; retry would duplicate effect.

18. UI LOGIC IN BACKEND — formatting, sorting, conditional display
    logic in backend that should be client-side; OR business logic
    in frontend that should be backend.

For each pattern found:

LOCATION: file:line
PATTERN: which from list
WHAT IT DOES: specific
WHY PROBLEMATIC in current codebase: specific
MODERN ALTERNATIVE: specific change
SEVERITY:
- HIGH: merge blocker or post-merge tech debt item
- MEDIUM: post-merge tech debt
- LOW: cosmetic cleanup

Also note:
- SYSTEMIC PATTERNS (same pattern appearing many places — tracking
  flag for tab 9 / retro discussion)
- PATTERNS WORTH PRESERVING (not all legacy-looking code is bad; call
  out when the legacy approach is actually better fit)

CODE DIFF:
{{paste PR diff}}

COMPONENT CONTEXT:
{{what module/service is this; any relevant design notes from Phase 2}}
```

---

## P3-10 — Technical Debt Registration

**Purpose:** Structured registration when the team deliberately takes on debt.

**When:** When a story ships with a known shortcut, a code review finds something that works but isn't ideal, or a post-hoc "we should revisit this" conversation.

**Inputs:** Description of the debt and what motivated it.

**Output target:** `8. Tech Debt Register`.

### Prompt

```
Register this technical debt in structured form.

DEBT ID: TD-[NUMBER]

DESCRIPTION: what the debt IS (specific; not "messy code")

CATEGORY: Architecture / Design / Testing / Test coverage / Integration /
   UI / Tooling / Observability / Performance / Resilience / Migration /
   Platform / Process / SAFETY

WHY INCURRED (motivation):
- Time pressure
- Deliberate simplification to ship; full solution in backlog
- Awaiting upstream decision
- Skill gap worked around
- External constraint

WHERE IT LIVES: file / component / pattern

AFFECTED:
- Stories that depend on this
- Future work that will be harder / slower until paid down
- Risks that grow while debt exists

SEVERITY (for priority):
- CRITICAL: safety-critical risk; 30-day hard cap
- HIGH: operationally risky; pay down before next quarter
- MEDIUM: friction; pay down within 2-3 sprints
- LOW: cosmetic; address when convenient

ORIGIN SPRINT: when this was incurred

PAYDOWN ESTIMATE (points)

PROPOSED PAYDOWN PLAN:
- Target sprint
- Approach (refactor / rewrite / tooling / training)
- Dependencies before paydown is feasible
- Validation: how we know it's resolved

IF CRITICAL: what compensating control is in place while debt exists
(e.g., additional monitoring, manual process, feature flag gating).

DEBT RATIO CHECK:
- What % of recent velocity is now tied up in open debt?
- If >15%, trigger paydown sprint decision

Be specific. "Refactor later" is not a plan. Name the target, approach,
and triggering condition.

DEBT DESCRIPTION:
{{describe the debt and what motivated it}}

CONTEXT:
{{related story, PR, or code location}}

CURRENT TEAM DEBT RATIO:
{{from dashboard, or note if unknown}}
```

---

## P3-11 — Feature Flag Strategy per Story

**Purpose:** For each story, determine if a feature flag applies and what strategy.

**When:** Story refinement — during P3-01 decomposition, before DoR.

**Inputs:** Story description, risk profile, cutover context.

**Output target:** `7. Feature Flag Register` (new flag if created); informs implementation.

### Prompt

```
Determine feature flag strategy for this story. Produce either a flag
spec or a justified "no flag" decision.

FLAG DECISION CRITERIA:

A flag is WARRANTED if any of:
- This story goes live per-district (cutover coordination)
- This story carries deploy risk; kill-switch desirable
- This story is part of a larger Epic rolling out progressively
- This story is safety-critical and needs emergency disable
- This story depends on upstream capability not yet stable
- This story is a breaking change with v1/v2 coexistence period
- This story is diagnostic / performance-costly (opt-in)

A flag is NOT warranted if:
- Standard behavior change, not user-facing per-district
- Has unit test coverage and no runtime risk
- Cost of adding flag > operational value

If flag IS warranted:

FLAG NAME: dot-separated identifier (e.g., swexec.new-state-machine)
FLAG TYPE:
- Cutover (time-bound; removed after cutover)
- Rollout (progressive enablement; removed after full adoption)
- Safety kill-switch (permanent)
- Migration (old-vs-new path; removed when old retired)
- Diagnostic (runtime tunable; may be permanent)

DEFAULT VALUE (safe default):
- Off until enabled explicitly (most cases)
- On with kill-switch for emergencies only

SCOPE:
- Global (one value system-wide)
- Per-district (values vary by district — relevant for cutover)
- Per-environment (dev/stage/prod differ)
- Per-user or per-role (rare; narrow use)

OWNER: role responsible for flag decisions

REMOVAL CRITERIA: explicit condition or date
"Permanent" requires stated justification

EMERGENCY ENABLE/DISABLE:
- Who has authority
- What triggers use
- Rollback semantics when toggled

AUDIT REQUIREMENTS:
- Flag evaluation logged? (avoid hot-path impact)
- Flag changes audited (who, when, value)

CODE HYGIENE:
- Wrap flag evaluation at component entry, not scattered through code
- Unit test BOTH flag states
- Integration test default state

If flag is NOT warranted, state:
- Why not (explicit reason)
- Alternative risk mitigation (canary deploy, back-out plan, etc.)

STORY:
{{paste story}}

RELATED EPIC + CUTOVER CONTEXT:
{{which Epic; per-district cutover applicability}}

EXISTING FLAGS that might cover this story:
{{paste flag register rows in same area}}
```

---

## P3-12 — Sprint Retrospective Facilitation

**Purpose:** Structured retro producing actions, not just venting.

**When:** Sprint end. Cadence: every sprint.

**Inputs:** Sprint metrics, events, team's prepared items.

**Output target:** `5. Sprint Retrospectives`.

### Prompt

```
Facilitate a structured sprint retrospective. Produce talking points,
discussion questions, and a framework for capturing actionable output.

1. SPRINT CONTEXT
   - Sprint ID and dates
   - Committed vs. completed points
   - Significant events (incidents, impediments resolved, new capabilities)
   - Stories with surprises (bigger than expected, delivered faster, etc.)

2. DATA-DRIVEN OBSERVATIONS (not opinions)
   - Velocity trend
   - Defect escape count
   - Impediments resolved vs. raised
   - Code review findings pattern
   - Tech debt trend
   - PR cycle time

3. RETROSPECTIVE FORMAT: Keep / Stop / Start / More-of / Less-of

   For each category, prompt questions:

   KEEP (what should continue):
   - What practices paid off?
   - What discipline held under pressure?
   - What saved us from a problem?

   STOP (what should cease):
   - What's wasting time?
   - What's consistently painful?
   - What friction isn't worth the cost?

   START (what new practice):
   - What did other squads try that worked?
   - What would address the top 2 frustrations?
   - What experiment is worth a sprint?

4. SAFETY-CRITICAL RETROSPECTIVE (additional for these sprints):
   - Any near-misses (incident avoided due to discipline)?
   - Any safety-related findings that weren't caught upstream?
   - Product Council feedback on any safety demo

5. THEMES EMERGING:
   Patterns appearing 3+ sprints in a row that signal structural issue
   worth escalating beyond the squad:
   - Knowledge concentration
   - Test environment reliability
   - External dependency friction
   - Design clarity issues
   - Capacity / estimation patterns

6. ACTION CAPTURE FORMAT:
   For each action:
   - What will change
   - Who owns
   - By when (specific sprint target)
   - Measurable / observable outcome
   - Success signal

7. ACTIONS FROM PRIOR RETROS - STATUS CHECK:
   Walk through open actions; close, update, or drop each.

8. SHIELD-AND-ESCALATE:
   What should the team NOT tolerate longer without escalation? Who
   escalates what to whom?

After retro, the output to capture in the Sprint Retrospectives tab:
- One row per Keep / Stop / Start / Action
- Each with theme, owner, status

DATA INPUTS:
{{paste sprint metrics, significant events, team-prepared items}}

PRIOR RETROSPECTIVE ACTIONS STILL OPEN:
{{paste from Sprint Retrospectives tab - actions with status Open / In Progress}}
```

---

## P3-13 — Impediment Analysis

**Purpose:** Structured analysis of an impediment — root cause, escalation decision, options.

**When:** When an impediment is raised OR when an impediment lingers > 3 business days.

**Inputs:** Impediment description and context.

**Output target:** `6. Impediment Log` entry or update.

### Prompt

```
Analyze this impediment and recommend action.

1. CLARIFY THE IMPEDIMENT
   What specifically is blocking? (Not "X is slow" — what concrete thing
   cannot proceed?)
   What team members / stories are affected?
   What started this — timestamp the first time it mattered.

2. ROOT CAUSE INQUIRY
   What condition caused it?
   Is it a one-time event or a recurring condition?
   Is there a systemic cause (tool, process, policy)?

3. CLASSIFICATION
   Category: Internal / External / Process / Technical / People / Safety /
      Compliance
   Severity: High (blocks critical path) / Medium (slows work) /
      Low (workaround available)

4. CURRENT OWNER
   Who's trying to resolve this?
   What's their authority level — can they resolve, or do they need help?

5. OPTIONS
   List 3-5 options to resolve, including "accept and work around":
   - Effort
   - Timeline
   - Who executes
   - Risk

6. RECOMMENDED ACTION
   Specific next step (not "investigate").
   Who does it and when.

7. ESCALATION DECISION
   If not resolved by [DATE], escalate to whom?
   What's the escalation trigger (time, worsening, spread)?

8. COMPENSATING MITIGATIONS while impediment persists
   What can the team do to maintain velocity?
   What work can continue?
   What should pause?

9. LESSON LEARNED
   If this is the 2nd+ time a similar impediment occurred, what's the
   structural fix?
   Add to tech debt or risk register if systemic.

10. COMMUNICATION PLAN
    Who needs to know this impediment exists? By when?
    Do stakeholders waiting on the affected work need status?

IMPEDIMENT:
{{describe the impediment, when raised, affected work, current state}}

RELATED IMPEDIMENTS from recent history:
{{check Impediment Log for similar past items}}
```

---

## P3-14 — Refactoring Decision Framework

**Purpose:** Decide whether a proposed refactor is worth the investment vs. continuing with current code.

**When:** Someone proposes a refactor; or code review reveals structural issue that's not a safety/security blocker.

**Inputs:** Current code state, proposed refactor, affected stories.

**Output target:** Decision: proceed / defer / document-and-move-on. If proceed, a tech debt item is closed or a refactoring story is added.

### Prompt

```
Evaluate this refactoring proposal. Produce a decision framework, not
just opinion.

1. CURRENT STATE
   - What's the code doing now?
   - What works?
   - What's painful?

2. PROPOSED CHANGE
   - What changes?
   - What stays the same?
   - Scope in time / files / tests affected

3. MOTIVATION EVIDENCE
   - Defect rate in this area?
   - Change cost (how often touched; estimation accuracy for stories
     here)?
   - Developer complaints or slower PRs in this area?
   - Blocker for upcoming work?
   - Tech debt item already registered?

4. VALUE ASSESSMENT (quantitative where possible)
   - Expected defect reduction
   - Expected velocity improvement
   - Expected cost reduction
   - Unblocking of planned future work

5. COST ASSESSMENT
   - Effort estimate (points)
   - Risk of introducing new defects
   - Test coverage adequacy (is current coverage sufficient to refactor
     safely?)
   - Coordination cost with other in-flight work

6. TIMING
   - Do it now
   - Do it after current sprint
   - Do it next quarter
   - Do it when pre-cutover paydown sprint arrives
   - Never — fold into next major rewrite

7. DECISION MATRIX
   - High value + Low cost = DO NOW
   - High value + High cost = PLAN A SPRINT / REFACTORING STORY
   - Low value + Low cost = DO AS OPPORTUNISTIC (during next nearby work)
   - Low value + High cost = DEFER / document as debt

8. ALTERNATIVES
   - Can we get 60% of value with 20% of cost?
   - Is there a simpler change that addresses the pain without full
     refactor?

9. RECOMMENDATION
   - Decision (one of the 4 options)
   - If proceed: owner, target sprint, success criteria
   - If defer: clear trigger condition for revisiting

REFACTOR PROPOSAL:
{{describe current state and proposed change}}

RELATED METRICS:
{{paste any change-cost, defect, velocity data for the area}}

UPCOMING WORK in this area:
{{list planned stories that would be affected}}
```

---

## P3-15 — Integration Test Scenario Generation

**Purpose:** Generate integration test scenarios from a story that crosses service or external boundaries.

**When:** Alongside P3-04 test derivation for integration-heavy stories.

**Inputs:** Story + integration contract + related services.

**Output target:** Integration test suite for this story.

### Prompt

```
Generate integration test scenarios for this story.

1. INTEGRATION SCOPE
   - Which services / systems are exercised end-to-end
   - Which boundaries are real (prod-like) vs. mocked
   - Test environment assumptions

2. SCENARIO MATRIX
   For each integration boundary:

   HAPPY PATH — normal flow through the boundary
   COUNTERPARTY ERROR — each error code from error catalog
   COUNTERPARTY TIMEOUT — our timeout threshold exceeded
   COUNTERPARTY SLOW — approaching timeout
   COUNTERPARTY UNAVAILABLE — full outage
   COUNTERPARTY UNDER LOAD — delayed responses; concurrency
   AUTHENTICATION FAILURE — credentials expired, cert problem
   VERSION MISMATCH — schema version drift
   MESSAGE MALFORMED — invalid payload received
   OUT-OF-ORDER EVENTS (if applicable)
   DUPLICATE EVENTS (if applicable)
   PARTIAL DELIVERY (if applicable)

3. IDEMPOTENCY TESTS (for non-read operations)
   - Retry produces same result
   - Concurrent duplicate produces single effect

4. RECONCILIATION TESTS (for stateful integrations)
   - State drift detection
   - Drift resolution

5. CIRCUIT BREAKER BEHAVIOR
   - Opens after threshold
   - Half-opens as expected
   - Closes on success

6. BACKPRESSURE / RATE LIMITING
   - Under storm-day load profile

7. END-TO-END FLOW TESTS
   - Full user flow exercising multiple integration points

8. DATA CONSISTENCY TESTS
   - After integration flow, all sides show consistent state

9. OBSERVABILITY TESTS
   - Metrics emitted during flow
   - Traces captured across boundary
   - Audit records complete

For each scenario:
- Scenario name
- Pre-conditions / test data
- Mock configuration (if using mocks)
- Steps
- Expected observable outcomes
- Measurement if applicable (latency, throughput)

TEST ENV REQUIREMENTS:
- Dedicated or shared env?
- Fixture data needed
- External system test endpoints available?
- Known test env limitations

AUTOMATION PRIORITY:
- CI (fast subset)
- Nightly (full matrix)
- Pre-release (storm-day profile)

STORY:
{{paste story}}

INTEGRATION CONTRACT:
{{paste INT-X contract from Phase 2}}

AVAILABLE TEST ENV DESCRIPTION:
{{describe what the test env can simulate}}
```

---

## P3-16 — Sprint Demo Script & Release Notes

**Purpose:** Sprint demo for stakeholders + release notes.

**When:** End of sprint.

**Inputs:** Stories completed this sprint + their AC.

**Output target:** Demo script (used live); release notes (filed).

### Prompt

```
Produce a sprint demo script and accompanying release notes.

DEMO SCRIPT (30-45 minutes):

1. SPRINT GOAL RECAP (1 minute)
   What we said we'd deliver this sprint

2. ACHIEVEMENTS (remainder)
   For each completed story worth demoing (group related ones):

   STORY / CAPABILITY NAME
   BUSINESS OUTCOME — in operator or stakeholder terms, not tech terms
   BEFORE / AFTER — what changed
   LIVE DEMO SEQUENCE — specific steps to demo (if UI-visible)
     OR
   EVIDENCE TO SHOW — for backend/platform work (dashboard, log, audit
     record, metric)
   WHO PRESENTS — person or pair; default to the engineer who built it
   PRODUCT COUNCIL ATTENTION — any safety-critical acceptance signal
     needed during the demo
   QUESTIONS LIKELY and prepared answers

3. WHAT'S NEXT (3-5 minutes)
   Sprint N+1 goal preview
   Dependencies requiring stakeholder attention

4. RISKS / IMPEDIMENTS worth stakeholder awareness

RELEASE NOTES:

For internal distribution (squad leads, program manager, stakeholders,
operations readiness team).

STRUCTURE:

## Sprint [ID] Release Notes — [Dates]

### Delivered
- Capability 1 (user-facing language) — [Epic, Story IDs]
- Capability 2 — ...

### Platform / Infrastructure
- Platform changes not user-visible

### Integrations
- Integration endpoints new/changed

### Feature Flags Changed
- Flags added, enabled, removed

### Data Migrations Applied
- Schema changes applied to this env

### Observability Additions
- New dashboards, alerts

### Known Issues
- Defects carried or not addressed

### Operational Notes
- Runbook additions
- Configuration changes needed
- Dependencies on external systems

### Upgrade Path (for staged envs)
- Prerequisite steps
- Post-deploy validation

### Security / Compliance
- NERC CIP-relevant changes
- New audit events
- Access control changes

Tone: factual, specific, no marketing language.

COMPLETED STORIES THIS SPRINT:
{{paste list of Done stories with titles and epic links}}

CHANGE DETAILS:
{{paste any significant changes beyond story titles, e.g., flags,
   migrations, runbook updates}}
```

---

## P3-17 — Build Phase Risk Identification

**Purpose:** Periodic Build-specific risk review (distinct from program risks).

**When:** Every 2 sprints. Program Manager + Sr Engs.

**Inputs:** Current build state, recent retros, impediment log, velocity trend.

**Output target:** `10. Build Phase Risks`.

### Prompt

```
Identify Build-phase risks based on current state. Focus on risks
specific to delivery rather than program-level risks.

Examine for risks in these areas:

1. VELOCITY / PREDICTABILITY
   - Velocity below rolling avg recent sprints
   - Widening commit-vs-complete gap
   - Frequent carry-over
   - Points per sprint varying widely

2. QUALITY
   - Defect escape rate growing
   - Code review findings pattern indicating systemic issue
   - Test coverage declining
   - Safety-critical findings in review

3. TECH DEBT
   - Debt ratio >15%
   - Critical debt items open past cap
   - Debt paydown velocity low
   - Same area accumulating debt sprint after sprint

4. TEAM HEALTH
   - Jr/Mid engineer ramp slower than planned
   - Key person always on critical path
   - Sick time or PTO patterns signaling burnout
   - Recurring retro themes indicating frustration

5. DEPENDENCIES
   - Integration vendor / counterparty unresponsiveness
   - Test env instability
   - Infrastructure capacity approaching limits
   - Other team dependencies slipping

6. SCOPE
   - Non-MVP creeping into MVP
   - Stories growing during refinement (originally 5 now 13)
   - Discovery gaps re-emerging during implementation
   - PO commitments changing mid-sprint

7. CUTOVER READINESS
   - Data migration dress rehearsals slipping
   - Feature flag strategy getting complex
   - Documentation lagging build
   - Training prep not started

8. COMPLIANCE
   - NERC CIP evidence generation lagging
   - Audit completeness testing deferred

For each risk:
RISK ID: BR-[NUMBER]
DESCRIPTION: specific observable phenomenon
LIKELIHOOD (1-5) and IMPACT (1-5) and SCORE
TRIGGER/EARLY WARNING: what makes this risk material
MITIGATION: specific actions already in place or planned
CONTINGENCY: what happens if risk materializes
OWNER: role
ESCALATION: when steering is informed

New risks this review:
Changes to existing risks (likelihood or impact shift):
Resolved risks:
Risks blocking Phase 4 entry if not resolved:

CURRENT BUILD STATE INPUTS:
{{paste dashboard metrics, recent retros summary, impediment log trend,
   velocity tracker observations}}

EXISTING BUILD RISK LOG:
{{paste current Build Phase Risk log to avoid duplicates}}
```

---

## P3-18 — Daily Status Synthesis

**Purpose:** Aggregate squad-level status into a program-level daily view. Supports PM's daily decision making without requiring meetings.

**When:** Daily during Build; after standups.

**Inputs:** Squad standup notes, blockers, story status changes.

**Output target:** Daily program status artifact (wiki or dashboard); input to steering weekly.

### Prompt

```
Synthesize today's squad-level standup inputs into a program status view.

STRUCTURE:

## Program Status — [Date]

### Headline
One sentence: is the program on track, watch, or action-needed?

### Squad-level summary
For each of 4 squads:
- SQUAD NAME
- SPRINT GOAL PROGRESS: on track / behind / ahead
- IN FLIGHT: stories actively being worked
- COMPLETED YESTERDAY: stories moved to Done/In Test
- BLOCKED: stories with blockers + owner
- KEY CONCERNS: anything the squad lead flagged

### Cross-squad
- SHARED CONCERNS (multiple squads hitting same issue)
- SYNCHRONIZATION ITEMS (squads waiting on each other)
- EMERGENT RISKS from today

### Metrics snapshot
- Points completed this sprint to date vs. target line
- PR cycle time this sprint
- Defects caught vs. escaped
- Open impediments: total, escalated

### Actions requiring PM / Sr Eng Deputy today
- Specific items needing action today
- Who, what, when

### Tomorrow's watch list
- Stories / integrations / dependencies to monitor

### Stakeholder updates
- What, to whom, by when — if anything needs communicating

DISCIPLINE:
- Specific, not vague
- Name names (story IDs, people's roles)
- Dates and numbers, not "soon" / "many"
- Don't hide bad news; don't over-dramatize

SQUAD INPUTS:
{{paste today's standup notes from each squad — or aggregate of moved cards}}

YESTERDAY'S STATUS (for comparison):
{{optional; paste yesterday's synthesis}}
```

---

## Prompt-to-Tab Mapping

| Prompt | Output flows to |
|--------|-----------------|
| P3-01 Epic → Story | `1. Story Backlog` (new/refined stories) |
| P3-02 DoR Check | `1. Story Backlog` (DoR Check column) |
| P3-03 Gherkin AC | Story AC in backlog tool |
| P3-04 Test Derivation | Test automation backlog |
| P3-05 Estimation | Points in `1. Story Backlog` |
| P3-06 Sprint Planning | `2. Sprint Backlog` commitment |
| P3-07 BR Code Review | PR comments; systemic → `9. Code Review Findings` |
| P3-08 Safety Code Review | PR comments (stricter); systemic → `9. Code Review Findings` |
| P3-09 Legacy Pattern Review | PR comments; systemic → `9. Code Review Findings` |
| P3-10 Tech Debt Registration | `8. Tech Debt Register` |
| P3-11 Feature Flag Strategy | `7. Feature Flag Register` |
| P3-12 Retro Facilitation | `5. Sprint Retrospectives` |
| P3-13 Impediment Analysis | `6. Impediment Log` |
| P3-14 Refactoring Decision | Decision; updates to debt register |
| P3-15 Integration Tests | Integration test suite backlog |
| P3-16 Demo + Release Notes | Demo script + release notes (filed) |
| P3-17 Build Risk ID | `10. Build Phase Risks` |
| P3-18 Daily Status | Daily program status artifact |

---

## Sprint Rhythm (how prompts align to the week)

**Monday — Backlog refinement (90 min)**
- P3-01 Epic → Story for 2-sprints-ahead stories
- P3-03 Gherkin AC for stories being refined
- P3-11 Feature Flag Strategy for refined stories

**Tuesday — Sprint planning (90 min)**
- P3-02 DoR Check on candidates
- P3-05 Estimation on candidates
- P3-06 Sprint Planning to produce commitment

**Daily — Standup + status (15 min standup + 15 min synthesis)**
- P3-18 Daily Status Synthesis after squad standups

**Throughout sprint — Development and review**
- P3-04 Test Derivation as stories enter development
- P3-07 BR Code Review on every PR
- P3-08 Safety Code Review on safety-critical PRs
- P3-09 Legacy Pattern Review on every PR (quick)
- P3-10 Tech Debt Registration ad-hoc as debt incurred
- P3-13 Impediment Analysis when blockers raised
- P3-14 Refactoring Decision when refactor proposed
- P3-15 Integration Test Generation for integration-heavy stories

**Friday (sprint-end week) — Demo + Retro (90 min each)**
- P3-16 Demo Script before demo
- P3-12 Retro Facilitation in retro ceremony

**Every 2 sprints — Build Risk review**
- P3-17 Build Phase Risk Identification

---

## Discipline Reminders

1. **Definition of Ready is non-negotiable.** A story in sprint that wasn't DoR-checked will consume more capacity than its points suggest. Resist the urge to "just add this one thing" on sprint day.

2. **Safety-critical stories = pair programmed, dual reviewed, Product Council demo-ed.** No exceptions, even under deadline pressure. Schedule Product Council availability in advance.

3. **Velocity is a forecast input, not a commitment.** Commit to <= rolling 3-sprint average × 0.9. Consistent under-commitment beats consistent over-commitment for reliability.

4. **Tech debt has a budget.** 15% of points per sprint max, or a paydown sprint is triggered. Safety debt has 30-day cap regardless of budget.

5. **Retros without actions are expensive complaining.** Every retro produces specific actions with owners and dates, or the retro was a failure.

6. **Impediments open > 5 days escalate automatically.** Not "we'll keep trying." A specific name goes to the PM.

7. **Feature flags are not free.** Adding a flag commits the team to its removal. Flags with no removal criteria become permanent debt.

8. **Code review against BRs is non-negotiable.** Especially safety-critical. Rubber-stamping merges accumulates silent risk.

9. **Story splitting is a sign of clarity, not weakness.** A 13-point story becoming three 5-point stories with clear AC is a win, not a failure.

10. **Demo + Release Notes every sprint.** Demoing to real operators early catches misunderstanding cheap. Release notes force clarity about what actually shipped.
