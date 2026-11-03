# WedLink — Business Decisions

**Status:** Task P0-T1 complete
**Source:** BRD v1.0 and developer clarification
**Date:** 2026-11-03

This document records business decisions that affect implementation. Operational defaults that can safely remain configurable will be selected and explained when their implementation task begins. Decisions requiring the developer's judgment will be raised before dependent implementation starts.

## Confirmed decisions

| ID | Topic | Decision | Source |
|---|---|---|---|
| BD-01 | RSVP accompanying people | The RSVP number is the number of accompanying people **in addition to the responding guest**. Expected attendance for a “Yes” response is therefore 1 plus the entered number. A declined response contributes zero to expected attendance. | Developer clarification, 2026-11-03 |
| BD-02 | Default invitation validity | Invitation validity ends two months after the wedding date. Once expired, it cannot be extended, reactivated, or republished. | BRD v1.0 |
| BD-03 | Media retention | Temporary images, videos, and other media are retained until three months after the wedding date, then deleted automatically. The final PNG card is retained permanently. | BRD v1.0 |
| BD-04 | Admin-managed service | Customers and guests do not create accounts. Admin manages orders and all post-delivery changes. | BRD v1.0 |
| BD-05 | Template history | Each invitation stays attached to its original template version. Deactivating a template only prevents its use for new orders. | BRD v1.0 |
| BD-06 | Historical prices | Order and add-on prices are captured at purchase/agreement time and do not change when current prices change. | BRD v1.0 |
| BD-07 | Payments | Payments are recorded manually in the MVP; no online payment gateway is included. | BRD v1.0 |
| BD-08 | MVP exclusions | Notifications, visitor analytics, customer accounts, couple replies to guest messages, and non-wedding invitation types are out of MVP scope. | BRD v1.0 |
| BD-09 | Public interactions | Guests can view published invitations without accounts. RSVP and guest messages are available only when enabled and supported by the invitation template. | BRD v1.0 |
| BD-10 | Language and currency | The primary interface language is Arabic with RTL layout; EGP is the default currency; wedding dates and expiration use Egypt local time consistently. | BRD v1.0 |
| BD-11 | Invitations per order | Each order can have at most one invitation. | Developer clarification, 2026-11-03 |
| BD-12 | Order price | The agreed base price excludes add-ons. The total order amount is the base price plus the historical prices of selected add-ons. | Developer clarification, 2026-11-03 |
| BD-13 | Preview review history | Keep an append-only history of preview approvals and change requests. | Developer clarification, 2026-11-03 |
| BD-14 | Repeated RSVP | A guest can update the previous response from the same browser using a random edit token. The database stores only a hash of the token; guests do not need accounts. | Developer clarification, 2026-11-03 |
| BD-15 | Customer phone | Customer phone numbers are unique after removing spaces; keep all other entered formatting unchanged. | Developer clarification, 2026-11-03 |

## Decisions to revisit when implementation reaches them

These are configuration or technical choices rather than unresolved business rules. They will be proposed with a clear rationale before the dependent task where appropriate:

- Initial upload limits, allowed media types, and total storage limits.
- Private submission and preview link expiration defaults.
- Backup and recovery handling for deleted media.
- Admin role and permission breakdown.
- Exact approval and change-request record fields.
- PNG generation and file storage providers.
- Egypt time-zone storage and daylight-saving implementation details.

No implementation should silently change the confirmed business decisions above.
