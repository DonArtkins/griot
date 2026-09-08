# RefreshTokens auth repair amendment

Date: 2026-09-08. Scope authorized by the user's Feature 07 repair request:
add a stable `FamilyId`, atomic rotation, and revocation limited to one login.
This supplements the frozen `griot-erd-v1.0.0.png` export and the existing
`PROMPTS/week-02/07-diagram-sequence-login-refresh.md` family contract.

| Field / index | Contract |
|---|---|
| `RefreshTokens.FamilyId` | Required `uniqueidentifier`; generated once per register/login and copied to replacements. Never exposed in auth responses. |
| `IX_RefreshTokens_UserId_FamilyId` | Non-unique index for revocation scoped by both user and family. |
| `ReplacedByTokenId` | Existing nullable identifier pointing from the consumed token to its replacement. |

Rotation conditionally updates the consumed row only while `RevokedAt IS NULL`,
then inserts the replacement in the same SQL Server transaction. A zero-row
update issues no credentials and triggers revocation of the same user/family.
Unknown and malformed tokens have no trusted family to revoke and return 401.
Independent login families remain usable.

Migration `AddRefreshTokenFamilyId` backfills existing linked chains from their
root token IDs before making the column required. New sessions use new GUIDs.
No entity or enum is renamed. The original PNG remains a historical export;
read it together with this amendment for the current auth schema.
