# 31 — Address Validation & UX (No Manual Transactions)



---


**Goal:** Clean address entry & persistence using your fixed `AddressService` without manual transactions.

### Steps
1) Use client‑side validation + server‐side `[Required]`, `[StringLength]`.
2) On save, **do not** wrap manual transactions (EF strategy will retry).
3) After save, re‑load to fetch DB‑generated `PublicId` (NEWID default).

### Acceptance Criteria
- Address create/update works under transient DB faults (retries OK).
- Unique `PublicId` present after creation.
