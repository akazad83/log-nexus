# QrDiode — Technical Security Review

**Target:** QrDiode one-way optical data diode (OT → IT) over animated QR codes — full workspace `d:\WORKSPACE\qr-code`
**Review date:** 2026-08-20
**Review type:** White-box source review (100 % of non-generated source read), static analysis, verification of documented controls against implementation, execution of the project test suite (75/75 passed on the review machine).
**Audience:** Security engineering / OT-IT cyber security staff
**Scope note:** Per the operator, the production use case is **transactional payloads only (XML/JSON)** — the file-transfer path exists in code and is covered here, but recommendations weight the transactional case.

> **Evidence discipline.** Every claim in this document is tied to a file (and usually a line) in the repository as reviewed. Nothing is asserted from the project's own documentation without independent confirmation in code. Where the reviewer could not verify a property (e.g., physical-channel behavior), that is stated explicitly.

---

## 1. Executive summary

QrDiode moves data from an OT network to an IT network with **no electrical or network path**: the OT machine renders a carousel of QR codes on a screen; the IT machine watches it with a camera. Unidirectionality is physical, not logical — there is **no back-channel of any kind**, and a repository-wide search confirms **zero network API usage** (no sockets, HTTP, or RPC anywhere in `src/`). Reliability without acknowledgements is achieved with a systematic **Luby Transform (LT) fountain code**.

Contrary to most hobby QR-transfer projects, the security layer here is real and correctly ordered: **ECDSA P-256 signature verification of a manifest happens before a single payload byte is interpreted**, payloads are **AES-256-GCM** encrypted under a per-session key from **static-static ECDH P-256 + HKDF-SHA256**, and a three-factor anti-replay scheme (session GUID, per-sender-key monotonic counter, timestamp window) is enforced from a transactional SQLite store. The receive pipeline is fail-closed at every step and the ordering was verified line-by-line (`ReceiveEngine.cs`).

**Overall assessment: strong protocol-level design, above industry norm for this class of tool.** The residual risk concentrates in four areas:

1. **Cleartext manifest metadata** — filenames, sizes, timestamps and key IDs are visible to anyone who can film the screen (F-01).
2. **Host-side trust and state stores** are plain user-writable files without ACL hardening — the local-machine attacker model is under-addressed relative to the protocol's strength (F-03, F-04, F-05).
3. **Availability under optical injection** — data symbols are only CRC-protected before decode completion; a hostile light source can poison and permanently "settle" an in-flight session (F-06). Integrity and confidentiality hold; delivery does not.
4. **Long-horizon cryptography** — static-static ECDH provides no forward secrecy, and P-256/ECDH is not post-quantum; the "film the screen now, decrypt later" scenario is credible for this medium (F-02).

None of the findings is a remotely exploitable compromise of integrity or confidentiality within the stated threat model. All are hardening gaps with concrete, implementable fixes (§7).

---

## 2. System inventory (as reviewed)

| Component | Purpose | Trust position |
|---|---|---|
| `src/QrDiode.Core` | Protocol library: framing, LT codec, crypto envelope, manifest, pipelines, content gate, audit | Shared, UI-free; the security kernel |
| `src/QrDiode.Sender` (WPF) | OT side: payload queue, encrypt/sign pipeline, QR stage renderer | Runs inside the OT zone |
| `src/QrDiode.Receiver` (WPF) | IT side: camera capture, decode, verification chain, quarantine store, SQLite journal, archive | Runs inside the IT zone; primary attack surface |
| `src/QrDiode.KeyCeremony` (console) | Offline keygen (CNG/TPM), public-bundle export/import, fingerprints | Provisioning tool, both zones |
| `tests/QrDiode.Core.Tests` | 75 tests incl. optical loopback, bit-flip fuzzing, crypto negative paths | Executed by reviewer: **75/75 passed** |

**Runtime / dependencies** (from `.csproj` files): .NET 10, WPF. Crypto is **exclusively in-box `System.Security.Cryptography`** (no third-party crypto — supply-chain positive). Third-party packages: `Net.Codecrete.QrCodeGenerator` 3.1.0 (pure C# QR encode), `ZXingCpp` 0.5.3 (**native** zxing-cpp decoder, P/Invoked), `FlashCap` 1.11.0 (camera), `Microsoft.Data.Sqlite` 10.0.10 + `SQLitePCLRaw.bundle_e_sqlite3` 3.0.5, `System.IO.Hashing` 10.0.10 (CRC-32), `System.Security.Cryptography.ProtectedData` 10.0.10 (DPAPI).

**Confirmed absent** (grep over all of `src/`): `HttpClient`, sockets, `WebRequest`, `BinaryFormatter` or any polymorphic deserialization, registry access. `Process.Start` occurs only to open Explorer on a stored path (receiver UI). The only P/Invoke authored in this codebase is `SetThreadExecutionState` (keep display awake, `StageWindow.xaml.cs:329`).

---

## 3. The transfer process, bit by bit

### 3.1 Wire format (protocol v1)

One QR code carries exactly one frame. Byte mode, all integers little-endian, fixed offsets (`FrameFormat.cs`). Every frame ends with **CRC-32 over all preceding bytes**. Hard parser bounds: 17 ≤ frame ≤ 4096 bytes.

**Common header — 12 bytes:**

| Offset | Size | Field | Value |
|---|---|---|---|
| 0 | 2 | Magic | `0x4F 0x44` ("OD") |
| 2 | 1 | Protocol version | `0x01` |
| 3 | 1 | Frame type | `0x01` manifest / `0x02` data symbol |
| 4 | 8 | Session tag | First 8 bytes of the session GUID |

**Data-symbol frame** (`header + 4 + symbolSize + 4` bytes):

| Offset | Size | Field |
|---|---|---|
| 12 | 4 | `symbolId` (uint32 LE) |
| 16 | symbolSize | LT symbol payload (default symbolSize = min(1350, ciphertext length)) |
| end−4 | 4 | CRC-32 |

**Manifest frame** (`header + 2 + bodyLen + 64 + 4` bytes): 12-byte header, `bodyLen` (uint16), manifest body, **ECDSA P-256 signature (64 bytes, IEEE P1363 r‖s)**, CRC-32. The parser enforces `frame.Length == 82 + bodyLen` exactly (`FrameParser.cs:76-78`).

**Manifest body — fixed 199-byte prefix + variable filename** (`TransferManifest.Serialize`, `TransferManifest.cs:52-82`):

| Offset | Size | Field |
|---|---|---|
| 0 | 16 | Session GUID |
| 16 | 8 | Timestamp (Unix ms, int64) |
| 24 | 8 | Per-sender-key monotonic counter (uint64) |
| 32 | 8 | Sender signing key ID = SHA-256(SPKI)[0..8) |
| 40 | 8 | Receiver key-agreement key ID (pins intended recipient) |
| 48 | 32 | HKDF salt (CSPRNG) |
| 80 | 12 | AES-GCM nonce (CSPRNG) |
| 92 | 2 | Symbol size (uint16) |
| 94 | 4 | K — source symbol count (uint32) |
| 98 | 8 | LT session seed (uint64, CSPRNG) |
| 106 | 2 | Redundancy hint |
| 108 | 8 | Original (plaintext) size |
| 116 | 8 | Compressed size |
| 124 | 8 | Ciphertext size |
| 132 | 1 | Compression algorithm (0 none / 1 Brotli) |
| 133 | 32 | **SHA-256 of ciphertext** |
| 165 | 32 | **SHA-256 of plaintext** |
| 197 | 1 | Content kind (0 file / 1 XML / 2 JSON) |
| 198 | 1 | Filename length |
| 199 | ≤255 | Filename (UTF-8) |

The signature covers the manifest body exactly as received; the manifest carries the ciphertext hash; the GCM AAD reconstructs the session identity from the signed manifest. This creates a **non-circular bidirectional binding**: a foreign manifest cannot be paired with a substituted ciphertext or vice versa.

### 3.2 Sender pipeline (`SendSession.Create`, `SendSession.cs:50-126`)

1. `SHA-256(plaintext)` computed first.
2. **Brotli compress** (`SmallestSize` ≤ 4 MB, else `Optimal`); the smaller of raw/compressed is kept and the choice recorded in the manifest.
3. **Counter reservation before session creation**: `CounterStore.Next()` increments and **durably flushes (write-temp + fsync + atomic rename) before the value is used** (`CounterStore.cs:26-35, 57-64`), DPAPI-protected at rest — a crash can never issue the same counter twice.
4. Fresh CSPRNG material: session GUID, 32-byte HKDF salt, 12-byte GCM nonce, 8-byte LT seed (`RandomNumberGenerator`).
5. **AAD** = `sessionGuid(16) ‖ salt(32) ‖ counter(8, LE) ‖ senderSigKeyId(8) ‖ receiverKexKeyId(8)` — 72 bytes (`Envelope.BuildAad`).
6. **Key derivation**: raw ECDH P-256 shared secret (sender static private × receiver static public) → `HKDF-SHA256(ikm=Z, salt=salt32, info="QrDiode.v1"‖sessionGuid, L=32)`. The shared secret is zeroized in a `finally` block (`Envelope.cs:35-50`); the derived key is zeroized after seal (`SendSession.cs:84-91`).
7. **AES-256-GCM** one-shot encrypt with explicit 16-byte tag (`AesGcm(key, 16)`), output `ciphertext ‖ tag`.
8. Manifest assembled (including `SHA-256(ciphertext)`), serialized, **ECDSA-P-256/SHA-256 signed**; frame written; receipt hash = first 8 bytes of `SHA-256(body ‖ signature)` shown to the operator for out-of-band comparison.
9. **LT encoder** instantiated over the ciphertext. Note the *ciphertext* is fountain-coded — filming the screen yields ciphertext only.

### 3.3 The carousel (`SendSession.Carousel`, `SendSession.cs:141-161`)

Because there is no acknowledgement, sessions end on an **air-time policy**, not on success:

- The manifest frame is emitted first and re-emitted every 20 frames (late-joiner acquisition ≤ ~1 s at 15 fps).
- Each loop emits `Redundancy × K` data frames (default 3×K): first a **systematic pass** (`symbolId 0..K−1` — the ciphertext blocks verbatim), then fresh coded symbols whose IDs **keep increasing across loops** so every loop contributes new equations to the decoder.
- The session leaves the air when *both* `loops ≥ LoopTarget (3)` **and** `airtime ≥ 6 s` (`TransmitEngine.PumpFrame`), then a 1.5 s dark gap lets the receiver finalize before the next manifest appears.

Frames are rendered as fixed-version QR (smallest version fitting the frame, ECC level L, 8-module quiet zone, 1 byte/pixel Gray8) by a ring-buffered producer thread so the UI thread only blits (`QrFrameRenderer.cs`, `CarouselPump.cs`).

### 3.4 Receiver chain — strict order, fail-closed (`ReceiveEngine.cs`)

Camera frames (FlashCap) are converted to a luminance plane (manual bounds-checked DIB parse, or WIC decode for MJPEG/PNG modes — `QrDecodePipeline.cs:123-207`) and passed to native zxing-cpp. Every decoded QR payload enters `ReceiveEngine.Feed` under a lock. The chain, with nothing later running until the earlier step passes:

| # | Step | Implementation | On failure |
|---|---|---|---|
| 0 | Structural parse + **CRC-32** | `FrameParser.TryParse` — span-based, every length checked before any read, allocation-free and exception-free on reject | Silently ignored (noise/misdecode) |
| 1 | Manifest structural parse + **sanity bounds** | sizes ∈ (0, 128 MB], `ciphertextSize == compressedSize + 16`, `K == ceil(ciphertextSize/symbolSize)`, compression/content enums valid (`TransferManifest.cs:107-116`) | Rejected + audited |
| 2 | **ECDSA signature** over the exact received body bytes, sender key ID must resolve in the trust store | `ReceiveEngine.cs:129-133` — unknown key → dropped **before any payload byte is interpreted** | Rejected + audited |
| 3 | **Addressing** — manifest's receiver key ID must equal this receiver's key-agreement key ID | `ReceiveEngine.cs:136-137` | Rejected |
| 4 | **Anti-replay** — unseen session GUID ∧ counter > per-sender-key high-watermark ∧ timestamp within window (48 h default; 15 min in Strict mode) | `SqliteJournal.Check` (`SqliteJournal.cs:81-104`) | Rejected + session settled |
| 5 | LT decode completes → **SHA-256(ciphertext) equals the signed hash** | `ReceiveEngine.cs:185` | Session poisoned → reset |
| 6 | **AES-256-GCM open** with AAD rebuilt from the signed manifest | `ReceiveEngine.cs:189-206`; derived key zeroized in `finally` | Rejected |
| 7 | **Decompression cap** — Brotli output hard-capped at the signed original size; any trailing byte beyond it fails closed (zip-bomb guard) | `TryDecompressBrotli`, `ReceiveEngine.cs:241-266` | Rejected |
| 8 | **SHA-256(plaintext) equals the signed hash** (and exact length) | `ReceiveEngine.cs:221-223` | Rejected |
| 9 | **Content gate** — XML: `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 0`, depth ≤ 64; JSON: `Utf8JsonReader`, `MaxDepth = 64`, no comments/trailing commas; opaque files uninterpreted (`ContentGate.cs`) | Rejected |
| 10 | **Anti-replay commit** (watermark + `verified` row, single SQLite transaction, `synchronous=FULL`, WAL) | `SqliteJournal.Commit` | — |
| 11 | Content dedupe (same sender + same plaintext hash → `duplicate`, nothing written) → **atomic materialization**: staging file inside the store root + `fsync` + same-volume rename (`overwrite:false`) → journal row promoted → hash-chained audit record | `MainWindow.HandleCompletion`, `PayloadStore.Save` | `store_failed` row, audited |

Data symbols for the locked session are accepted only if the 8-byte session tag matches and the payload length equals the signed symbol size; duplicates are dropped by a seen-ID set. Filenames are treated as hostile: sanitized against `Path.GetInvalidFileNameChars`, leading dots stripped, prefixed with the session GUID (`PayloadStore.SanitizeFilename`) — path traversal via manifest filename is not possible.

**Crash semantics** are honest: watermark + `verified` row commit at verification time; a crash before materialization leaves a `verified` row with no file, which is visible to the operator rather than silently lost.

---

## 4. How the fountain code works

The in-house codec (~600 lines, `src/QrDiode.Core/Fountain/`) is a **systematic Luby Transform code** — chosen over RaptorQ explicitly because no maintained managed implementation exists and auditable C# was preferred over a P/Invoked native codec for a security tool (a defensible supply-chain decision; the cost is ~5–10 % reception overhead vs RaptorQ's ~0.2 %).

**Encoding.** The ciphertext is split into K source blocks of `symbolSize` bytes (last block zero-padded). Any 32-bit `symbolId` deterministically defines one symbol:

- `symbolId < K` → **systematic**: source block `symbolId` verbatim (fast path — a loss-free pass needs exactly K frames).
- `symbolId ≥ K` → **coded**: the per-symbol seed is domain-separated as `mix = symbolId × 0x9E3779B97F4A7C15 ⊕ sessionSeed`, run through **splitmix64**, and used to seed a **xoshiro256++** PRNG (`DeterministicRandom.cs`). `System.Random` was deliberately avoided — its algorithm is not a cross-version contract, and both ends must derive identical results. A **degree** d is sampled from the precomputed CDF of the **robust soliton distribution** (Luby 2002; c = 0.1, δ = 0.05: ideal soliton ρ plus the spike τ at K/R — `RobustSoliton.cs`), then d **distinct neighbor indices** in [0, K) are drawn — partial Fisher–Yates when d > K/2, rejection sampling otherwise; bounded integers use Lemire multiply-shift rejection, so there is **no modulo bias** and the neighbor sets match bit-for-bit on both sides. The symbol is the XOR of its neighbors.

**Decoding** (`LtDecoder.cs`) is incremental **belief-propagation peeling**: arriving symbols are XOR-reduced by every already-known source block; a symbol with one unknown neighbor immediately resolves it, and each resolution is pushed through a work queue that peels every buffered symbol depending on it, cascading until quiescent. Symbols may arrive in any order, duplicated, or never — the carousel guarantees more equations come. Decoding completes when all K blocks are resolved; assembly truncates the final padded block to the signed ciphertext length.

**Security posture of the codec:** all coding parameters (K, symbolSize, seed, sizes) come **from the signed manifest**, so an attacker cannot choose pathological parameters without defeating ECDSA first. The PRNG is not cryptographic and does not need to be — it only schedules XORs; integrity is enforced end-to-end by the signed SHA-256 and the GCM tag, and the property tests confirm round-trips under 50 % random loss and full shuffling. The one consequence of unauthenticated *symbols* (only CRC-32 protects a data frame before decode completion) is an availability issue, treated as finding F-06.

---

## 5. Security measures in place, mapped to industry standards

All verified in source. Standard references: IEC 62443-3-3 (SR = system requirement), NIST SP 800-53r5, NIST SP 800-82r3.

| Control | Implementation (evidence) | Standards alignment |
|---|---|---|
| Physical unidirectionality (data-diode semantics) | No network code exists in any project (verified by search); the only channel is screen→camera; no ACK path | 62443-3-3 SR 5.1/5.2 (zone segmentation); 800-82r3 unidirectional-gateway guidance |
| Authenticity before parsing | ECDSA P-256 manifest signature verified against an offline-provisioned trust store before any payload byte is touched (`ReceiveEngine.cs:129-133`) | SR 1.2, SR 2.12 (non-repudiation), 800-53 SC-8/SC-23, SI-7 |
| Confidentiality on the optical path | AES-256-GCM; per-session key via static-static ECDH P-256 + HKDF-SHA256; unique 32-byte salt and GUID in `info` per session; 12-byte CSPRNG nonce; explicit 16-byte tag | SR 4.1; SC-13, SC-28; NIST SP 800-131A-acceptable algorithms; FIPS-friendly (CNG primitives) |
| Cryptographic binding | Signed manifest carries ciphertext hash; AAD carries session identity incl. both key IDs — no mix-and-match | SC-8(1) |
| Recipient pinning | Manifest names the receiver key ID; receiver enforces addressing (`ReceiveEngine.cs:136`) | SR 2.1 |
| Anti-replay, three independent factors | Session-GUID dedupe + per-sender-key monotonic counter high-watermark + timestamp window, enforced from SQLite (WAL, `synchronous=FULL`) in one transaction; sender counter reserved-then-flushed before use, DPAPI at rest | SR 3.1/3.5; SC-23; 800-53 IA-3 |
| Hostile-input parsing | Span-based frame parser, every length checked first, Try-pattern, no allocation/exception on reject (`FrameParser.cs`); manifest sanity envelope (≤128 MB, K/size consistency) | SR 3.5; SI-10; fuzz-tested (bit-flip and garbage corpus tests, executed) |
| Injection-resistant content gate | XML: DTD prohibited, resolver null, entity chars = 0, depth cap; JSON: depth cap, strict syntax; identical hardening in the *display* formatter both sides (`ContentGate.cs`, `PayloadFormatter.cs`) | SI-10; OWASP XXE prevention (fully aligned) |
| Zip-bomb defense | Brotli output hard-capped at the signed size; trailing data fails closed | SI-10 |
| Path-traversal defense | Filename sanitization + GUID prefix + fixed store root; `overwrite:false` move | SI-10 |
| Transactional, atomic materialization | Staging + `Flush(flushToDisk:true)` + same-volume rename; journal/audit in the same flow; crash states operator-visible | SR 3.4; 800-82 integrity of recorded data |
| Idempotency / dedupe | Same sender + same plaintext hash → `duplicate`, never re-written | Transactional-integrity good practice |
| Key management | CNG named keys, `ExportPolicy = None` (non-exportable), TPM Platform Crypto Provider preferred with software-KSP fallback surfaced in the UI; only public SPKI bundles cross zones; strict bundle JSON (`UnmappedMemberHandling.Disallow`); 128-bit whole-bundle fingerprint compared out-of-band; import refuses to overwrite (explicit two-step rotation) | SR 1.5; IA-5; SC-12; FIPS 140-3-validated platform providers |
| Audit | Append-only hash-chained JSONL on both ends, fsync per record, torn-tail tolerant, `AuditLog.Verify` detects edits from the break point; every send, receive, rejection, store change and export audited | SR 6.1/6.2; AU-9/AU-10 (partially — see F-04) |
| Least attack surface | No third-party crypto; no dynamic deserialization; parameterized SQL everywhere incl. escaped `LIKE`; UI payload viewers bounded and non-interpreting | SR 7.x resource availability; secure-coding norms |
| Verification depth | 75 tests incl. full optical loopback through real QR images, tamper/replay/XXE negative paths, parser bit-flip corpus — **executed by the reviewer: 75/75 pass** | 62443-4-1 SVV practices |

---

## 6. Findings — what stands between this and "brutally secure"

Severity is qualitative (impact × attacker feasibility) within the stated deployment: OT sender zone, IT receiver zone, optical corridor observable/injectable by insiders, local attackers assumed possible at user privilege.

### F-01 · Manifest metadata is cleartext on the wire — **High** (confidentiality of metadata)
The manifest body is signed but **not encrypted**. Anyone filming the screen reads: filename (≤255 bytes — in OT contexts frequently sensitive on its own, e.g. recipe or batch identifiers), exact plaintext/compressed sizes, timestamps, counter (volume/tempo intelligence), and both key IDs (infrastructure enumeration). The README acknowledges size/timing traffic analysis; filename and key-ID exposure exceeds that acknowledgement.
**Fix:** encrypt the descriptive tail of the manifest (filename, content kind, sizes beyond what LT bootstrap needs) under the same session key in a second AEAD envelope, or move to opaque transfer IDs with the human-readable name inside the encrypted payload. Coding parameters (K, symbolSize, seed, ciphertext size/hash) must stay cleartext-signed; nothing else must.

### F-02 · No forward secrecy; quantum-harvest exposure — **Medium** (long horizon)
Key derivation is **static-static** ECDH: compromise of *either* endpoint's long-term agreement private key decrypts **every recorded past session** (salt and nonce are public in the manifest). The optical medium makes recording trivially deniable — a phone pointed at the screen archives ciphertext forever. TPM non-exportability mitigates software theft but not endpoint compromise, and the silent software-KSP fallback (`CngEndpointKeys.cs:82`) weakens the floor on TPM-less machines. P-256/ECDH is also not post-quantum; harvest-now-decrypt-later applies.
**Fix (layered):** (a) sender-side **ephemeral** ECDH share carried in the signed manifest (ephemeral-static, HPKE-auth-like) — removes sender-key exposure entirely; (b) scheduled receiver key rotation (the key-ID mechanism already supports overlap) to bound the receiver-static window; (c) roadmap item: hybrid **ML-KEM-768 + ECDH** encapsulation per CNSA 2.0 / NIST SP 800-227 direction.

### F-03 · Trust store and anti-replay state are user-writable plaintext files — **High** (local attacker)
`%LocalAppData%\QrDiode\trust\*.json`, `journal.db`, `sender-counter.bin` and `receiver-settings.json` are created with default user ACLs. Consequences for any code running as the receiver's user: **dropping a JSON bundle into `trust\` silently provisions a new trusted sender** (no signature over the store, no audit event on out-of-band imports — `DirectoryTrustStore` loads whatever parses); deleting `journal.db` erases session/counter watermarks, reopening the full timestamp window (48 h default) to replays of filmed traffic; deleting the sender's DPAPI counter file resets counters to 1, which the receiver will then reject (availability). The protocol is strong; its **roots of trust sit in soft filesystem state**.
**Fix:** dedicated service accounts and restrictive ACLs on `%LocalAppData%\QrDiode` (deny interactive users write); trust-store changes only via an elevated ceremony path that appends to the audit chain; optionally seal the trust directory index with a DPAPI-machine MAC; alarm at Start when the journal is missing/reinitialized (watermark regression detection); back up watermarks.

### F-04 · Audit chain is unkeyed — **Medium**
The hash chain (`AuditLog.cs`) detects casual edits, but an attacker with write access can **rewrite the entire file and recompute every hash** — there is no secret in the chain and no external anchor. AU-10-style non-repudiation is not achieved against a local writer.
**Fix:** HMAC each record with a key held in the TPM/CNG (or countersign the chain head periodically with the endpoint's signing key), and/or ship records to an off-host collector (see §8 SIEM note) so the local file is not the only copy.

### F-05 · Demo provisioning is reachable in production binaries — **Medium**
Both WPF apps expose "⚡ Demo setup" and the CLI has `demo`: it generates `demo-ot`/`demo-it` key pairs **on the local machine** and cross-trusts them (`DemoProvisioner.cs`). On a production receiver, one click (or one social-engineered operator) adds a trusted sender **whose private key lives on the receiver itself**, enabling any local process to sign manifests the receiver will accept. The mechanism is honest (nothing is stubbed) but it is a standing trust-injection primitive.
**Fix:** compile demo provisioning out of production builds (build flag), or gate it behind an explicit machine-level policy marker plus an audit event; `demo-clean` on first production Start if demo bundles are detected.

### F-06 · Data symbols are unauthenticated until decode completes — optical-injection DoS — **Medium** (availability; integrity unaffected)
A data frame needs only a valid CRC-32 and the 8-byte session tag — both public on the wire. An attacker who can out-shine the legitimate screen (projector, laser, replayed video) during an active session can inject a symbol with a legitimate ID and wrong bytes. The seen-ID set then **discards the genuine symbol as a duplicate** (`LtDecoder.AddSymbol`), decode completes with corrupt ciphertext, the hash check fails — correctly — but `RejectAndReset` **permanently settles the session** (`ReceiveEngine.cs:276-287`): the remainder of the genuine carousel is ignored and the sender, having no back-channel, never learns the payload was lost. Additionally, decoder memory (`_seenSymbolIds`, `_buffered`) grows without bound across the 2³² symbol-ID space, so sustained injection is also a memory-exhaustion vector.
**Fix:** (a) on ciphertext-hash mismatch, *re-arm* rather than settle — drop decoded state, keep the session ID acceptable while its manifest is still on air, and prefer last-writer or conflict-tracking for symbol IDs seen twice with different bytes (a 4-byte per-symbol XXH/CRC retained per accepted ID makes conflicts detectable cheaply); (b) cap retained coded symbols (e.g. 4×K) and the seen-ID set; (c) alarm the operator on symbol-conflict detection — it is a high-fidelity indicator of active optical attack; (d) longer term, derive a per-symbol MAC tag from the session key for data frames (costs ~8 bytes/frame) to authenticate symbols pre-decode.

### F-07 · Replay/timestamp posture defaults are permissive — **Low**
48 h acceptance window by default; the 15-minute Strict mode exists but is opt-in (`MainWindow.xaml.cs:227-229`). Counter and GUID checks still block true replays while the journal survives (see F-03 for the coupling). The window matters most exactly when the journal is lost.
**Fix:** make Strict the default; document the OT/IT time-sync requirement (independent NTP/GPS per 800-82; the receiver clock is the enforcement clock); log skew observed per accepted manifest as telemetry.

### F-08 · Native and OS-codec parsing of untrusted images, in-process — **Medium**
The highest-volume untrusted input (camera frames, up to 60/s) is parsed by **native zxing-cpp** and, for MJPEG/PNG camera modes, **Windows WIC codecs**, all inside the receiver process (`QrDecodePipeline.cs:143-168`). The hand-written DIB→luminance converter is properly bounds-checked (verified, lines 171-207), but the native surfaces are outside this codebase's control; a memory-safety bug there is remote-code-execution-adjacent *within the IT zone* for an attacker who controls what the camera sees.
**Fix:** isolate capture+decode into a separate low-privilege process (AppContainer / restricted token) that passes only decoded QR payload bytes over a pipe to the verifying process; keep zxing-cpp/FlashCap patched (watch CVEs); extend the planned Phase-3 fuzzing to the luminance-conversion and native-decode boundary.

### F-09 · Software supply chain not yet pinned or attested — **Medium**
No `packages.lock.json`, no hash pinning, no SBOM, unsigned builds. The native `ZXing.dll`/`libZXing` blobs arrive via NuGet with no local attestation. For a security appliance this is the classic residual: the protocol trusts no one, the build trusts the registry.
**Fix:** enable NuGet locked mode + `RestorePackagesWithLockFile`; verify package signatures; produce a CycloneDX SBOM per release; sign binaries (Authenticode) and enforce WDAC/AppLocker on both endpoints so only the signed build runs; pin the .NET SDK in `global.json`; OSV/NVD monitoring for zxing-cpp, FlashCap, SQLitePCLRaw.

### F-10 · Key-management hygiene details — **Low**
(a) Keys are created with `CngKeyUsages.AllUsages` (`CngEndpointKeys.cs:66`) — signing and agreement keys should be usage-constrained. (b) `OpenOrCreateKey` opens a pre-existing key of the same name without checking its export policy or provider — a previously created *exportable* key would be silently adopted. (c) Bundles carry `createdUtc` but no expiry, and nothing enforces rotation; revocation is manual file deletion with no audit event. Rotation tooling is an acknowledged Phase-3 item.
**Fix:** set `KeyUsage` to `Signing`/`KeyAgreement` respectively; on open, assert `ExportPolicy == None` (and log provider); add bundle validity windows and a revocation/rotation runbook with audited trust-store mutations.

### F-11 · Residual plaintext and cleanup — **Low**
If the final `File.Move` or a later step fails, the staging `.tmp` (full plaintext) can remain in `<store>\.staging`. Verified-payload previews are held in memory and can be copied to the Windows clipboard (clipboard is unaudited and readable by other user-session processes; the export path *is* audited). Plaintext buffers are not zeroized (the session key **is** zeroized — verified both sides).
**Fix:** sweep `.staging` at Start and after failures; consider a policy toggle disabling clipboard copy on the receiver; (optional) best-effort zeroization of plaintext after materialization.

### F-12 · Silent delivery gaps are invisible on the IT side — **Medium** (OT transactional completeness)
The sender drops the oldest queued payloads beyond depth 14 ("air-time budget exhausted") and records this only in the **sender-side** queue/audit. The receiver has the tool to detect loss — the per-sender monotonic counter — but never uses it: a gap between the watermark and the next accepted counter is not flagged. For transactional OT data (the stated use case), silent gaps are an integrity-of-record issue even when every delivered payload is perfect.
**Fix:** on accept, if `counter > watermark + 1`, raise a visible "N transfers from this sender were never received" event into the inbox/archive/audit. This is cheap and materially improves the completeness story; pair with sender-side alarming when the queue trims.

### F-13 · Small-payload and enum edge notes — **Info**
`ushort` counter storage in SQLite uses `unchecked` signed casts — comparisons would misorder only beyond 2⁶³ (unreachable in practice). `RejectionInfo` after signature verification correctly gates what the UI is entitled to display. The `_settled` set grows per run (bounded by sessions/run; cleared on restart). No action required; documented for completeness.

---

## 7. Prioritized hardening roadmap

**P0 — do before production exposure**
1. ACL-harden `%LocalAppData%\QrDiode` under dedicated service accounts; alarm on missing/fresh journal; audit every trust-store mutation (F-03).
2. Remove/flag-out demo provisioning from production builds (F-05).
3. Default Strict replay window; document time-sync requirements (F-07).
4. Counter-gap detection on the receiver (F-12).
5. NuGet locked mode + signed binaries + WDAC/AppLocker allow-listing on both endpoints (F-09).

**P1 — next protocol/build iteration**
6. Encrypt manifest descriptive fields (filename, sizes, content kind) (F-01) — protocol v2 field, receiver keeps v1 compat during overlap.
7. Re-arm instead of settle on ciphertext-hash mismatch; cap decoder memory; conflict-detection alarm (F-06).
8. Ephemeral sender ECDH share in the signed manifest; scheduled receiver key rotation (F-02a/b).
9. Sandbox the capture/decode stage into a low-privilege child process (F-08).
10. Keyed (TPM-HMAC) audit records or off-host audit shipping (F-04).
11. Key-usage constraints, export-policy assertion on open, bundle expiry (F-10).

**P2 — strategic**
12. Hybrid PQC key encapsulation (ML-KEM-768 + ECDH) and ML-DSA signature migration path (F-02c) — the filmed-ciphertext HNDL scenario justifies this earlier than most IT systems.
13. Per-symbol authentication tags (F-06d) if the deployment's optical corridor cannot be physically controlled.
14. Formal fuzzing campaign (SharpFuzz on `FrameParser`/`TransferManifest.TryParse`; corpus from bench captures) — already on the project's Phase-3 plan; extend to the luminance path.

---

## 8. IT/OT deployment guidance (beyond the codebase)

These are environmental controls the software cannot provide for itself, aligned to IEC 62443 and NIST SP 800-82r3:

- **Zones and conduits:** treat the optical link as a formally documented conduit between an OT zone and an IT zone; the receiver host belongs in a DMZ-like landing zone, not the general IT LAN. Target SL 2–3 per 62443-3-3 depending on consequence analysis.
- **Physical control of the optical corridor** is a real security dependency: F-06 (injection) and F-01 (filming) are both physical-access attacks. Enclose the screen/camera pair (light-tight cabinet or controlled room); an enclosure simultaneously removes the shoulder-surf, projector-injection and ambient-light-DoS classes.
- **Host hardening:** dedicated non-admin accounts per app; no interactive logon on the receiver beyond the operator role; disable removable media on the receiver except the audited ceremony workflow; BitLocker on both hosts (the quarantine store and journal are cleartext at rest by design — the payload store is *deliberately* plaintext for downstream AV/scanning, so full-disk encryption is the compensating control).
- **Detection pipeline:** the receiver's `audit\*.jsonl` and `payload_index` (rejections included — the schema records them, which is unusually good) are SIEM-ready JSONL/SQLite. Ship them (IT side has network; the *app* rightly does not). Alert on: unknown-sender-key rejections (someone is pointing an unprovisioned or forged stream at your camera), replay verdicts, signature failures, symbol-conflict events once F-06c lands, `verified`-without-file rows, audit-chain verification failures on a schedule.
- **AV/CDR integration:** the quarantine layout (one file + `.meta.json` sidecar, readable without the app) was explicitly designed for downstream scanning — wire the store root into scheduled AV/content-disarm before payloads are consumed by IT systems, especially for the `File` content kind. For the stated XML/JSON-only production profile, consider **disabling the opaque-file kind operationally** (schema validation of the two expected transaction schemas would be a strong addition to the content gate).
- **Time:** the anti-replay window is enforced by the receiver's clock against the sender's claimed timestamp. Give both zones authenticated, independent time (NTS or GPS-disciplined per 800-82); monitor skew.
- **Ceremony discipline:** the fingerprint-comparison step is the trust anchor for everything else. Script it, log it (signed ceremony record, two-person rule), and never let a bundle enter the trust directory outside it (enforced technically after F-03).
- **Availability engineering:** the channel is lossy and unacknowledged by design; for transactional flows define an end-to-end reconciliation (e.g., daily counter-range report from OT operations vs receiver archive totals — the archive's per-type tallies and totals were built for exactly this comparison).

---

## 9. Methodology and limitations

- **Read in full:** every `.cs` file in `src/` and `Shared/` (protocol core, both apps, ceremony tool), all four `.csproj` files, both architecture documents. Framing, crypto, fountain, pipeline, storage, capture and audit code was reviewed line-by-line; UI files were reviewed for security-relevant behavior (trust handling, storage, clipboard/export, process launches).
- **Executed:** `dotnet test tests/QrDiode.Core.Tests -c Release` on the review machine, 2026-08-20 — **75 passed / 0 failed / 0 skipped**. Test names and content were inspected to confirm they cover the claimed negative paths (tamper, replay, XXE, bit-flip, wrong-recipient, optical loopback).
- **Not performed:** dynamic penetration testing, fuzzing beyond the project's own corpus tests, side-channel analysis of the crypto primitives (they are BCL/CNG one-shots), physical optical-channel experiments, and review of the third-party native decoder's internals. Findings F-06 and F-08 are derived from code paths and threat reasoning, not live exploitation.
- **Independence of claims:** project documentation was used as a map, then every security claim in it was re-verified in source. Two documentation claims required qualification: "filming the screen yields ciphertext" is true for the payload but **not for manifest metadata** (F-01), and "append-only" audit holds only against attackers without file write access (F-04).

---

*End of review. Findings F-01 … F-13; recommendations P0-1 … P2-14. Review artifacts: this document (`docs/SECURITY_REVIEW.md`) and the DOCX rendering (`docs/SECURITY_REVIEW.docx`).*
