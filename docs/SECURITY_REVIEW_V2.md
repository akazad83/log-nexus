# QrDiode — Security Review v2: Target-State Assessment

**Target:** QrDiode one-way optical data diode (OT → IT) over animated QR codes
**Document version:** 2.0 — remediation specification and projected post-remediation posture
**Date:** 2026-08-20
**Companion document:** *QrDiode — Technical Security Review* v1 (as-built review; findings F-01…F-13)
**Audience:** External security reviewers, security engineering, OT/IT architecture

| Document control | |
|---|---|
| v1.0 · 2026-08-20 | As-built white-box review of the full source tree; 13 findings; test suite executed (75/75 pass) |
| v2.0 · 2026-08-20 | Remediation register R-01…R-14 with acceptance criteria; projected residual-risk posture |
| Status | **TARGET STATE — remediations are SPECIFIED, not implemented.** See §1. |
| Distribution | Internal + external security review |

---

## 1. Read this first: what this document is, and is not

**As of 2026-08-20 the codebase implements none of the remediations in this document.** Everything in §4 is a specification: what will be built, how it must behave, and — critically for an external reviewer — the acceptance criteria by which each item can be independently verified once implemented. Statements about the *current* system (architecture, protocol v1, the controls that already exist) were verified line-by-line in source and by executing the project's 75-test suite; statements about the *target* system are engineering commitments, clearly marked **[SPECIFIED — OPEN]**.

This split is deliberate. A "review" that described the target state as existing would be worthless to you and would not survive your own verification pass. Instead:

- §2 summarizes the verified as-built baseline (details in the v1 companion document — send both together).
- §4 is the remediation register. Each entry names the v1 finding it closes, the design, the acceptance criteria, and what risk remains *after* the fix.
- §5 is the honest part: the risks that remain when **everything in §4 is done** — the irreducible residue of this architecture. If your review concludes the same list, the design is sound; if you find residue we missed, that is exactly what we are paying you for.
- §6 gives a verification matrix so the implementing diff can be re-reviewed efficiently.

**Review guidance:** treat every R-item as false until you have seen the implementing code and run its acceptance test. Protocol-affecting items (R-01, R-02, R-06) change the wire format and must be re-reviewed at the same depth as the v1 baseline.

---

## 2. Verified baseline (as-built, protocol v1)

These properties were confirmed in source and by test execution, and are **not** projections:

- **Physical unidirectionality.** No network API exists anywhere in `src/` (verified by exhaustive search). The only channel is screen→camera. No ACK path of any kind.
- **Fail-closed, correctly ordered verification chain** (`ReceiveEngine.cs`): CRC-32 → manifest structural parse + sanity envelope → **ECDSA P-256 signature against an offline-provisioned trust store before any payload byte is interpreted** → receiver-key addressing → three-factor anti-replay (session GUID ∧ per-sender-key monotonic counter ∧ timestamp window) from transactional SQLite (`WAL`, `synchronous=FULL`) → LT decode → signed ciphertext SHA-256 → AES-256-GCM with AAD rebuilt from the signed manifest → decompression hard-capped at the signed size → signed plaintext SHA-256 → hardened content gate (XXE-prohibited XML / depth-capped JSON) → transactional materialization (staging + fsync + same-volume rename).
- **Sound key handling foundations.** Non-exportable CNG keys (TPM-preferred), public-only bundles across zones, strict bundle parsing, 128-bit ceremony fingerprints, counter reserved-and-flushed before use (DPAPI at rest), ECDH shared secrets and session keys zeroized after use.
- **In-house systematic LT fountain code** whose parameters all come from the signed manifest; deterministic cross-platform PRNG (xoshiro256++/splitmix64, Lemire unbiased sampling); incremental peeling decoder. Integrity is end-to-end (signed hashes + GCM), not per-symbol.
- **Hash-chained audit logs** both sides; every send/receive/rejection/store-change/export audited; rejections are first-class archive records.
- **Test depth:** 75 tests including full optical loopback through real QR images, parser bit-flip corpus, crypto negative paths (tamper, replay, wrong recipient, XXE, zip bomb). Executed by the reviewer: 75/75 pass.

The v1 review's overall judgment stands: **protocol design above industry norm for this class of tool; residual risk concentrated in host posture, optical-channel availability, metadata privacy, and long-horizon cryptography.** Those four areas are what §4 closes.

---

## 3. Protocol v2 — wire-format changes introduced by the remediations

Three remediations (R-01, R-02, R-06) change the wire protocol. Summary of the target manifest ("protocol version" byte increments to `0x02`; v1 receivers reject v2 frames cleanly by design):

**Manifest v2 = public coding header ‖ encrypted descriptor ‖ signature.**

- **Public (cleartext, signed):** session GUID · timestamp · counter · sender signing key ID · receiver key-agreement key ID · HKDF salt · GCM nonce · symbolSize · K · LT seed · redundancy hint · **ciphertext size + SHA-256(ciphertext)** · **sender ephemeral ECDH public key (33 B compressed)** · *(when hybrid PQC is enabled)* **ML-KEM-768 encapsulation ciphertext (1 088 B)** · descriptor-blob length.
- **Encrypted descriptor (AES-256-GCM under the session key, AAD = the public header):** filename · content kind · original size · compressed size · compression algorithm · **SHA-256(plaintext)**.
- **Signature:** ECDSA P-256 over public header ‖ descriptor ciphertext (exact received bytes, as in v1).

Why the plaintext hash moves inside: in v1 it is cleartext, so **anyone filming the screen can confirm whether a specific known document was transferred** (hash-confirmation attack). This was identified while drafting v2 and folded into R-01; it is a sharper statement of F-01 than the v1 text made.

Frame budget check (honest math, not hand-waving): public header ≈ 199 − 66 (descriptor fields moved out) + 33 (ephemeral key) + 2 (blob length) ≈ 168 B; descriptor blob ≈ 66 + filename(≤255) + 16 (GCM tag) ≤ 337 B; signature 64 B; frame overhead 18 B. Classical-only total ≤ ~590 B — comfortably inside V30-L (1 370 B). With ML-KEM-768 (+1 088 B) the manifest reaches ~1 680 B, which **exceeds V30-L and requires V40-L (2 953 B) manifest frames**; the parser's 4 096 B cap still holds. Consequence: hybrid PQC costs manifest-frame density (larger QR, denser modules, harder camera acquisition). This is a real, measured trade-off the implementation must benchmark — it is called out rather than hidden.

**Data-symbol v2:** each data frame appends an 8-byte truncated HMAC-SHA256 over `symbolId ‖ symbol payload`, keyed by a per-session subkey `HKDF(sessionKey, info="QrDiode.v2.symbol")` (R-06). Per-frame cost: 8 B of ~1 370 (0.6 % throughput).

---

## 4. Remediation register

Every entry: **[SPECIFIED — OPEN]** until the implementing diff is reviewed and its acceptance criteria pass.

### R-01 · Encrypt manifest descriptor fields — closes F-01 (High)
**Design:** as §3. Filename, content kind, original/compressed sizes, compression algorithm and plaintext hash move into an AEAD blob under the session key; only coding-bootstrap fields remain cleartext.
**Acceptance criteria:** (a) a captured v2 manifest frame, parsed offline, yields no filename, no content kind, no original size, no plaintext hash; (b) a v2 receiver rejects a v2 manifest whose descriptor blob fails GCM authentication; (c) signature still verified over exact received bytes *before* descriptor decryption is attempted; (d) loopback tests updated and passing.
**Residual after fix:** ciphertext size, session tempo (counter), timing, and key IDs remain observable — traffic analysis is reduced, not eliminated (§5-4).

### R-02 · Forward secrecy + PQC path — closes F-02 (Medium)
**Design:** (a) per-session sender-ephemeral P-256 share; session secret = `HKDF(ECDH(eph, receiver_static) ‖ ECDH(sender_static, receiver_static), …)` — authenticity still from ECDSA, sender-static compromise no longer decrypts recorded traffic; (b) scheduled receiver key-agreement rotation (90 days, overlap window via the existing key-ID mechanism) to bound the receiver-static exposure window; (c) optional hybrid mode: ML-KEM-768 encapsulation to the receiver's ML-KEM public key concatenated into the HKDF IKM (CNSA 2.0 direction).
**Acceptance criteria:** (a) test proving a recorded session cannot be decrypted with the sender's static private key alone; (b) rotation drill: old+new receiver keys accepted during overlap, old rejected after retirement, watermarks per key ID unaffected; (c) hybrid mode interop test + manifest-size benchmark at V40-L (see §3 trade-off).
**Residual after fix:** compromise of the **receiver's current static/KEM private keys still decrypts all sessions of the active rotation window** — with a strictly one-way channel there is no interactive ratchet; this is an architectural floor, stated plainly in §5-2.

### R-03 · Host state protection — closes F-03 (High)
**Design:** dedicated non-interactive service accounts per app; ACLs on `%LocalAppData%\QrDiode` (service account RW; operators read-only on the payload store; **no** interactive write to `trust\`, `journal.db`, counter or settings); trust-store mutation only via an elevated ceremony tool that appends a signed audit record; DPAPI-machine-scope MAC over a trust-directory index (bundle name + SPKI hashes) verified at Start; Start-time alarm if `journal.db` is absent or freshly initialized while trust bundles exist (watermark-regression tripwire); watermark backup procedure in the ops runbook.
**Acceptance criteria:** (a) with operator-level (non-service) token: writing `trust\rogue.json` fails with access denied; (b) deleting `journal.db` as admin → next Start raises a blocking, audited alarm requiring explicit acknowledgment; (c) trust-index MAC mismatch → receiver refuses Start; (d) ceremony import produces an audit record on the chain.
**Residual after fix:** an attacker with **admin/SYSTEM** on the receiver defeats ACLs and DPAPI-machine by definition; the control raises the bar from "any user-level code" to "endpoint compromise", which is where §5-1 takes over.

### R-04 · Keyed, exported audit — closes F-04 (Medium)
**Design:** each audit record carries HMAC-SHA256 under a per-endpoint key held in CNG (TPM-resident where the Platform Crypto Provider supports persisted HMAC keys; **honest caveat: PCP HMAC support varies by TPM — the fallback is a DPAPI-machine-protected key, which an admin can extract; therefore** the primary tamper-evidence control is (b)): (b) chain-head countersignature — every N records and at Stop, the endpoint ECDSA key signs the current head hash into the chain; (c) IT-side: ship `audit\*.jsonl` to an off-host collector (the receiver host has a network; the app itself still must not — shipping is an OS-level agent, not app code).
**Acceptance criteria:** (a) full-file rewrite-and-rehash attack (the v1 attack) is detected by countersignature verification; (b) `AuditLog.Verify` extended to check HMACs and countersignatures; (c) collector receives records within minutes of write.
**Residual after fix:** an admin-level attacker who also controls the signing key's usage (the key is non-exportable but *usable* by compromised code on the box) can forge continuations — but cannot rewrite history countersigned before compromise **if** heads were shipped off-host. Off-host shipping is the load-bearing part.

### R-05 · Demo provisioning out of production — closes F-05 (Medium)
**Design:** `DemoProvisioner` and the "⚡ Demo setup" UI compiled only under a `DEMO` build flag; release pipeline produces production binaries without the symbol; production receiver refuses Start (with audited alarm) if `demo-ot`/`demo-it` bundles exist in the trust store.
**Acceptance criteria:** (a) `strings`/IL inspection of release binaries shows no demo path; (b) planting a demo bundle in `trust\` blocks Start with an audited event.
**Residual after fix:** none specific; collapses into the general trust-ceremony discipline (§5-6).

### R-06 · Authenticated symbols + session resilience — closes F-06 (Medium, availability)
**Design:** (a) 8-byte per-symbol HMAC (§3) verified before a symbol enters the decoder — optical injection of data frames now requires the session key; (b) decoder memory caps: retained coded symbols ≤ 4×K, seen-ID set ≤ 8×K, overflow drops-oldest with telemetry; (c) symbol-conflict detection (same ID, different bytes → both discarded, conflict counter, operator alarm — a high-fidelity indicator of active optical attack); (d) on ciphertext-hash mismatch, **re-arm instead of settle**: decoded state is dropped but the session remains acceptable while its manifest is on air, so the genuine carousel can complete after an injection burst ends.
**Acceptance criteria:** (a) unit test: forged symbol with valid CRC and session tag but no valid MAC never reaches the decoder; (b) injection simulation (interleave corrupt symbols into a loopback stream): session completes successfully once injection stops, conflict alarm raised; (c) memory ceiling test at adversarial symbol-ID churn holds the cap.
**Residual after fix:** a *sustained* stronger light source still denies the channel for as long as it operates — jamming is physics, not protocol (§5-3). Manifest frames themselves remain injectable-but-unforgeable (signature), so pre-manifest DoS (flooding bogus manifests to force parse/verify work) persists at low severity; ECDSA verify at camera frame rate is ~trivial CPU.

### R-07 · Strict replay window by default — closes F-07 (Low)
**Design:** 15-minute window default; 48 h becomes the opt-in ("relaxed clock" deployment flag, audited at Start); per-accepted-manifest clock-skew telemetry; ops runbook requires authenticated NTP (or GPS-disciplined) time in both zones per NIST SP 800-82r3.
**Acceptance criteria:** default-configuration test accepts ±15 min and rejects beyond; Start audit record carries the active window.
**Residual after fix:** replay resistance within the window still rests on the journal (R-03 protects it); clock failure modes become availability events (correct direction of failure).

### R-08 · Sandboxed capture/decode — closes F-08 (Medium)
**Design:** camera capture + luminance conversion + native zxing-cpp decode move to a child process running with a restricted token / AppContainer (no filesystem write outside its working set, no network, job-object memory cap); parent receives only decoded QR payload bytes (≤4 096 B each) over a pipe; child crash → automatic restart + audit event; verification chain stays in the parent.
**Acceptance criteria:** (a) token inspection shows AppContainer/restricted SIDs and no dangerous privileges; (b) kill -9 of the child mid-session: parent survives, session resumes on restart within the carousel; (c) a deliberately corrupted native decoder (fault-injection build) cannot write outside the sandbox or reach the trust store.
**Residual after fix:** kernel/driver surface (camera driver, USB stack) remains in-kernel and unsandboxable from user mode; a camera-firmware attack is still endpoint compromise (§5-1). Native-decoder RCE is contained, not prevented.

### R-09 · Supply-chain pinning and attested builds — closes F-09 (Medium)
**Design:** NuGet locked mode (`packages.lock.json`, `RestoreLockedMode=true`) + package-signature verification; `global.json` SDK pin; CycloneDX SBOM per release; Authenticode-signed binaries; WDAC/AppLocker policy on both endpoints allowing only the signed build; OSV/NVD monitoring for zxing-cpp, FlashCap, SQLitePCLRaw; reproducible-build check in CI.
**Acceptance criteria:** (a) tampered package hash fails restore; (b) unsigned binary refuses to run under the deployed WDAC policy; (c) SBOM matches restored graph.
**Residual after fix:** upstream compromise *before* pinning (a poisoned version that gets pinned) — mitigated only by review of dependency diffs and the deliberately tiny dependency set. Stated as accepted risk in §5-7.

### R-10 · Key-management hygiene — closes F-10 (Low)
**Design:** `CngKeyUsages.Signing` / `KeyAgreement` split at creation; on open, assert `ExportPolicy == None` and expected provider, refuse otherwise (audited); bundle format v2 gains a validity window (advisory — bundles are unsigned by design; the ceremony record, not the file, is the authority); rotation/revocation runbook with audited trust-store mutations (per R-03).
**Acceptance criteria:** creating a deliberately exportable key under the app's key name → Open refuses; expired bundle → import warns and Start surfaces it.
**Residual after fix:** rotation cadence is procedural — tooling can nag, only operations can rotate (§5-6).

### R-11 · Plaintext-residue hygiene — closes F-11 (Low)
**Design:** `.staging` swept at Start and after any materialization failure; receiver policy toggle disabling clipboard copy of payload content (default off in production profile); best-effort zeroization of plaintext buffers post-materialization (documented as best-effort — GC copies make guarantees impossible in managed code, and we will not pretend otherwise).
**Acceptance criteria:** kill the process between staging write and rename → next Start leaves no `.tmp` plaintext; clipboard button absent in production profile.
**Residual after fix:** managed-memory copies of plaintext persist until GC/reuse; page file exposure is mitigated by BitLocker (deployment control), not by the app.

### R-12 · Delivery-gap detection — closes F-12 (Medium)
**Design:** receiver: on accepting counter `c` for sender key `k` with watermark `w`, if `c > w + 1` raise a first-class inbox/archive/audit event "`c − w − 1` transfer(s) from `k` never received"; sender: queue-trim events become prominent UI alarms with a daily audited tally.
**Acceptance criteria:** loopback test skipping a counter → gap event with correct count; archive query can list gap events per sender.
**Residual after fix:** gaps are *detected*, never *recovered* — the channel has no retransmission request. Reconciliation remains an operations process (§5-5).

### R-13 · Documentation-only items — closes F-13 (Info)
Signed-cast counter comparison bounds (>2⁶³ unreachable), per-run `_settled` growth, receipt-hash truncation (64-bit, human-comparison convenience only — the signature is the authority): documented in the ops guide, no code change. **Acceptance:** statements present in docs.

### R-14 · Transactional-profile content pinning — new hardening beyond v1 findings
**Design:** since production traffic is **XML/JSON transactions only**, add an operator-pinned schema layer to the content gate: per-sender allow-list of expected transaction schemas (XSD / JSON Schema), `File` content kind disabled by deployment policy; schema-validation failures are rejections (audited, archived) like any other gate failure.
**Acceptance criteria:** payload valid-XML-but-wrong-schema → rejected at step 9 with schema-specific reason; `File` kind → rejected under the transactional profile.
**Residual after fix:** schema validation constrains *structure*, not *semantics* — a compromised sender emitting well-formed lies is §5-1, and no receiver-side parser can fix it.

---

## 5. Brutal honesty: what remains when all of §4 is done

This is the list we would expect a competent external reviewer to hand back to us. If the implementation lands R-01…R-14 with passing acceptance criteria, the following risks **still exist** — they are properties of the architecture, the physics, or the deployment, not defects of the code:

1. **Endpoint compromise is out of scope and always will be.** A compromised OT sender signs whatever it likes; the receiver will verify it perfectly and store it. The content gate and schema pinning (R-14) bound the blast radius to well-formed transactional data, but **the diode transports trust, it cannot manufacture it.** A compromised IT receiver (admin-level) can be made to accept anything, R-03/R-04 notwithstanding — their function is to make user-level persistence loud, not to survive root. Compensating controls are host integrity (WDAC, attestation, EDR) — outside this codebase.
2. **Forward secrecy has an architectural floor.** With no return channel there is no interactive ratchet. R-02 removes sender-side exposure and bounds receiver-side exposure to the rotation window (90 days as specified). Within that window, receiver-key compromise decrypts recorded traffic. Anyone whose threat model cannot accept a 90-day window must shorten the rotation cadence — the protocol cannot do better without violating unidirectionality.
3. **Availability is jammable, permanently.** A stronger light source, a laser on the camera, or a hand over the lens denies the channel for as long as it is applied. R-06 turns *cheap, surgical, invisible* session poisoning into *sustained, physical, obvious* jamming — a real improvement in detectability, not in prevention. The compensating control is physical (enclosed optical corridor), and §8 of the v1 review already requires it.
4. **Traffic analysis survives R-01.** Ciphertext size, counter cadence, session timing and key IDs remain visible to an observer of the screen. Padding schemes could blunt size analysis at real throughput cost on a ~20 KB/s channel; we judged this a poor trade and did not specify it — an external reviewer who disagrees should say so with a threat scenario that justifies the bandwidth.
5. **Delivery is detected, not guaranteed.** R-12 makes silent loss visible (counter gaps) on the IT side, but nothing can *request* a resend. Completeness for transactional data is an operations loop: gap alarm → human → OT re-send. If the business process cannot tolerate that loop's latency, this medium is the wrong transport, and no software change alters that.
6. **The ceremony is the root of trust and it is human.** Fingerprint comparison, rotation cadence, revocation discipline, "do not import bundles outside the ceremony" — R-03/R-05/R-10 enforce what can be enforced technically, but a rushed or socially-engineered ceremony still provisions the wrong key. Two-person rule and signed ceremony records are procedural controls; audit them.
7. **Supply chain is minimized, not eliminated.** The dependency set is small and crypto is BCL-only (genuinely good), but zxing-cpp is a native C++ parser of hostile input at the trust boundary, forever. R-08 contains it; R-09 pins it; neither makes it memory-safe. A Rust-based decoder behind the same interface would be the next structural step if this risk must shrink further.
8. **Post-quantum posture is partial by choice.** R-02c hybridizes *confidentiality* (ML-KEM). Signatures stay ECDSA P-256 until an ML-DSA migration — an attacker with a future quantum computer could forge *new* traffic then, but cannot retroactively break the authenticity of *recorded* transfers (signatures are verified at receive time). Harvest-now-decrypt-later applies to confidentiality only, which is exactly what the hybrid closes. State this trade openly rather than claiming "quantum-safe".
9. **These controls decay.** ACLs drift, WDAC policies get exceptions, rotation slips, the demo flag gets re-enabled "temporarily" in a lab build that leaks to production. The v2 posture is a *maintained* posture. The Start-time self-checks (R-03, R-05) are designed to make the most likely drift loud, but an annual re-review against this document is part of the control set, not an optional extra.

**Bottom-line judgment.** With R-01…R-14 implemented and verified, QrDiode's protocol and software posture would be at or above the state of practice for unidirectional transfer software, including the software stacks of commercial data diodes; the dominant residual risks would be endpoint compromise, physical-channel availability, and operational discipline — none of which any codebase can absorb. Conversely: **until the R-items land, the v1 findings stand in full**, and the as-built system should be treated as suitable for pilot/controlled deployment only, with F-03 (host state) and F-05 (demo path) as the two we would not go to production carrying.

---

## 6. Verification matrix for the external review

| ID | Closes | Sev (v1) | Status | How to verify once implemented |
|---|---|---|---|---|
| R-01 | F-01 | High | OPEN | Capture manifest frame → offline parse: no filename/kind/plaintext-hash; GCM-tamper reject test |
| R-02 | F-02 | Medium | OPEN | Decrypt-with-sender-static-only must fail; rotation drill; hybrid interop + V40 benchmark |
| R-03 | F-03 | High | OPEN | ACL probe as operator token; journal-deletion tripwire; trust-index MAC; audited ceremony import |
| R-04 | F-04 | Medium | OPEN | Rewrite-and-rehash attack detected via countersignature; off-host copies exist |
| R-05 | F-05 | Medium | OPEN | Release binary contains no demo path; planted demo bundle blocks Start |
| R-06 | F-06 | Medium | OPEN | MAC-less forged symbol never reaches decoder; injection sim recovers; memory caps hold |
| R-07 | F-07 | Low | OPEN | Default window ±15 min; Start audit carries window |
| R-08 | F-08 | Medium | OPEN | Child token inspection; crash-resume test; fault-injected decoder contained |
| R-09 | F-09 | Medium | OPEN | Tampered package fails restore; unsigned binary blocked by WDAC; SBOM matches |
| R-10 | F-10 | Low | OPEN | Exportable same-name key refused at open; expiry surfaced |
| R-11 | F-11 | Low | OPEN | Kill-between-stage-and-rename leaves no plaintext; no clipboard in production profile |
| R-12 | F-12 | Medium | OPEN | Skipped counter → gap event with correct count |
| R-13 | F-13 | Info | OPEN | Ops-guide statements present |
| R-14 | — | Hardening | OPEN | Wrong-schema payload rejected; File kind rejected under transactional profile |

Suggested review order for the implementing diff: R-06/R-01/R-02 (wire protocol — deepest), then R-03/R-04/R-05 (host trust), then the remainder.

---

## 7. Projected standards alignment (post-remediation, conditional)

Stated conservatively; all claims conditional on §4 landing and §8-of-v1 environmental controls being deployed.

- **IEC 62443-3-3:** the system would support deployment at **SL 2 across all seven FRs**, with SL 3-capable behavior on FR 1 (IAC — hardware-backed, non-exportable, ceremony-provisioned identities), FR 3 (SI — signed-before-parsed, authenticated symbols, keyed audit), and FR 5 (RDF — physically unidirectional conduit). FR 7 (RA) remains the ceiling-limited FR: availability of an optical channel is SL 2 at best under an adjacent attacker (§5-3). We deliberately do not claim SL 4 anywhere.
- **NIST SP 800-82r3:** aligns with the unidirectional-gateway pattern including its guidance on time sync, DMZ placement of the receiving host, and detective reconciliation for unacknowledged transfer.
- **NIST SP 800-53r5:** SC-8/SC-8(1), SC-13, SC-23, SI-7, SI-10, IA-5, SC-12, AU-9/AU-10 (with R-04's off-host shipping), CM-14/SR-4 (with R-09).
- **FIPS 140-3:** all cryptography is CNG/BCL one-shot primitives, operable on FIPS-mode Windows; ML-KEM via the platform once the OS exposes validated implementations (implementation must not vendor its own PQC math — specified as a constraint on R-02c).
- **CNSA 2.0 trajectory:** hybrid ML-KEM-768 confidentiality (R-02c) now; ML-DSA signature migration is roadmap, stated openly as not yet specified (§5-8).

---

## 8. Reviewer's closing statement

Two documents should travel together: the v1 as-built review (what exists, verified) and this v2 target assessment (what is committed, and what it will and will not buy). We have tried to make v2 easy to attack: every remediation has a falsifiable acceptance criterion, every projected claim is marked conditional, and §5 is our own list of what your review should still find wrong afterwards. Where this document says "verified", it means the reviewing model read the code and ran the tests on 2026-08-20; where it says **[SPECIFIED — OPEN]**, nothing exists yet and no credit is claimed.

*End of v2 assessment. Companion: `docs/SECURITY_REVIEW.md` (v1). Artifacts: `docs/SECURITY_REVIEW_V2.md`, `docs/SECURITY_REVIEW_V2.docx`.*
