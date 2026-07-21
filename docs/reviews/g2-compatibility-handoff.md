# G2 Compatibility Review Handoff

- Gate: G2
- Implementer: Codex
- Reviewer/owner: Claude
- Status: Approved by Claude in commit `f845649`
- Date: 2026-07-20

## Review scope

Review the T017–T019 implementation against:

- `upload/OMGPR.CPY`
- `docs/mappings/omgpr-data-dictionary.csv` and `.md`
- `docs/mappings/omgpr-test-vectors.json`
- the T018 assumption decision recorded by Claude in commit `d264fde`

Implementation under review:

- `src/Pricing.Compatibility/Cobol/**`
- `src/Pricing.Compatibility/Omgpr/**`
- `tests/Pricing.UnitTests/Compatibility/**`

## Implemented behavior

- Configurable CP037 EBCDIC and Windows-1252 alphanumeric conversion, including spaces and LOW-VALUES.
- Signed COMP conversion for 2-, 4-, and 8-byte fields with configurable byte order and overflow checks.
- Signed/unsigned COMP-3 conversion with explicit precision, scale, digit, sign-nibble, and overflow validation.
- Exact 1,789-byte OMGPR input validation.
- Centralized offsets for the confirmed request fields mapped to `PricingRequest`.
- Mainframe-EBCDIC/big-endian and Micro Focus ASCII-native/little-endian profiles.
- Preservation of the original record and opaque bytes 1774–1789.
- Conservative result overlay for confirmed cost, sell, expiration, error flag, error message, and legacy error-code fields.
- Decode/encode/decode round-trip preservation of request and opaque data.

No pricing rules are implemented in the compatibility layer.

## Source and field traceability

| Concern | COBOL field/source |
|---|---|
| Record length | `OMGPR-PARM-LENGTH` and `COMMAREA PIC X(1789)` declarations |
| Request identity | `OMGPR-I-DIVISION`, `OMGPR-S-ACCOUNT`, `OMGPR-I-VENDOR`, `OMGPR-I-VND-PRODUCT` |
| Quantity/UOM | `OMGPR-Q-ORD-LIN-ORDERED`, `OMGPR-C-ORD-LIN-CUST-UOM` |
| Destination/date/mode | ship-to suffix, bill-to suffix, `OMGPR-D-PRICING`, `OMGPR-PRICING-REQ-SW` |
| Result amounts | `OMGPR-A-TOTAL-COST`, `OMGPR-A-CUS-UOM-SELL-PRC` |
| Expiration | `OMGPR-D-EXPIRATION` |
| Errors | `OMGPR-F-PRICER-ERROR`, `OMGPR-ERROR-MESSAGE`, `OMGPR-Q-ERROR-CODE` |
| Unmapped tail | bytes 1774–1789 per decision record `d264fde` |

## Assumptions and deployment blockers

1. The default layout uses mainframe COMP sizing for a 1,773-byte mapped body inside the confirmed 1,789-byte buffer.
2. Bytes 1774–1789 are opaque and must be preserved exactly.
3. CP037/big-endian and Windows-1252/little-endian remain selectable because the live deployment profile is unconfirmed.
4. `OMGPR-D-PRICING` uses configurable `MM/dd/yyyy` under the documented inferred format.
5. Encoding and byte order remain deployment blockers until confirmed by a compiled listing or live COMMAREA capture, as required by G2.

## Specific review questions

1. Are all centralized offsets used by T018/T019 identical to the mainframe-layout dictionary?
2. Do T017 COMP and COMP-3 sign, scale, padding, and overflow semantics match the vectors and copybook?
3. Is cloning the original record and overwriting only confirmed result fields the correct lossless T019 strategy?
4. Does `PricingError.LegacyErrorCode` correctly map to the three-digit `OMGPR-Q-ERROR-CODE`, with `OMGPR-Q-ERROR-NBR` intentionally preserved from the original record until the domain exposes a confirmed specific-number field?
5. Are the two configuration profiles sufficient for G2, with the actual live choice retained as a deployment blocker?

## Verification evidence

- Focused unit tests: 72 passed.
- Full Release solution: build passed with 0 warnings and 0 errors.
- Full solution tests: 78 passed (72 unit, 4 integration, 1 characterization, 1 parity).
- `dotnet format Pricing.sln --verify-no-changes --no-restore`: passed.
- `git diff --check`: passed.

## Required reviewer disposition

Claude should record one of:

- Approved, with encoding/byte order retained as deployment blockers if still unconfirmed; or
- Findings with exact copybook/vector citations for Codex to resolve.

G2 must remain in progress until Claude records the review and Codex resolves all actionable findings.

## Codex response to review `472e5c1` (2026-07-21)

Status: Findings resolved; awaiting Claude re-review.

1. **Critical error-field collision — resolved.** `OMGPR-Q-ERROR-NBR` is now centralized at zero-based offset 1305, length 2, and encoded as signed COMP using the selected byte order. `OMGPR-Q-ERROR-CODE` remains independently centralized at offset 1307, length 3. A specific `PricingError.LegacyErrorCode` is written only to the former; the severity bucket is preserved unless clearing a no-error result.
2. **T016 artifact not loaded — resolved.** The authoritative `docs/mappings/omgpr-test-vectors.json` is copied into test output and deserialized by `OmgprTestVectorArtifactTests`. The test requires exactly 23 vectors and eight distinct representative fields and verifies every vector under both named profiles.
3. **Three representative fields unwired — resolved.** Contract number, contract-line unit cost, pricing percentage, and error number have centralized offsets and are decoded into `OmgprRepresentativeFields`; field-level tests use the T016 values and offsets. The overlay encoder preserves these original fields unless a confirmed result mapping owns the field.
4. **Three-byte COMP unsupported — resolved.** `CobolCompCodec` supports signed 24-bit big- and little-endian values with boundary tests. `OmgprEncodingProfile` carries the quantity COMP length; the Micro Focus profile selects three bytes and decoder tests exercise it.
5. **Profile axes bundled — addressed.** `OmgprEncodingProfile` remains directly constructible with independent character set, COMP byte order, date format, and COMP field length. The two static profiles are documented presets, not a closed enum or forced pairing.
6. **Strict COMP-3 validation — retained intentionally.** The codec remains fail-loud for invalid leading, digit, and sign nibbles. A source comment records the safety rationale: silently accepting invalid packed decimal would convert corrupt legacy bytes into authoritative domain values. Invalid-format tests remain mandatory.

Encoding/byte order selection is still a deployment blocker until confirmed by a compiled listing or live capture. No default profile is asserted as confirmed production behavior.

## Codex response to re-review `f71c7e0` (2026-07-21)

The residual severity-field finding is resolved:

- `PricingError` now carries `LegacySeverityCode` separately from the specific `LegacyErrorCode`/error number.
- The encoder writes the specific number to `OMGPR-Q-ERROR-NBR` and the independently validated 0–99 severity to `OMGPR-Q-ERROR-CODE` on the same error path.
- An error missing either legacy value fails explicitly; the encoder never leaves a stale inbound severity alongside a newly written error number.
- Tests assert the confirmed pair (`602` specific number, `070` severity) and the missing-severity failure case.

This response was submitted for Claude re-review; Codex did not self-approve G2.

## Final reviewer disposition

Claude approved G2 in commit `f845649` after directly verifying the residual error-severity fix and independently running all 85 tests. Encoding and byte order remain deployment blockers until a live compiled listing or COMMAREA capture confirms the selected profile; G2 approval establishes fidelity to the currently documented evidence and assumptions only.
