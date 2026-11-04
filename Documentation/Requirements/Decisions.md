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
| BD-16 | Admin user foreign keys | Order status history references the ASP.NET Core Identity string user key. | Developer clarification, 2026-11-03 |
| BD-17 | Admin access policy | Only accounts with the SuperAdmin role may sign in to and use the administration system. SuperAdmin alone manages admin accounts and assigns the built-in Admin and SuperAdmin roles. | Developer clarification, 2026-11-03 |
| BD-18 | Order status workflow | New may move to Confirmed or Cancelled; Confirmed may move to InProgress or Cancelled; InProgress may move to Completed; Completed and Cancelled are terminal. | Developer clarification, 2026-11-03 |
| BD-19 | Customer submission link | Submission links expire 14 days after creation. Rotating a link revokes all previous active submission links for the order. | Developer clarification, 2026-11-04 |
| BD-20 | Customer upload limits | Allow JPEG, PNG, and WebP images up to 10 MB each; MP4 video up to 100 MB; MP3/M4A audio up to 20 MB; total active customer media is limited to 500 MB per order. | Developer clarification, 2026-11-04 |
| BD-21 | File storage | Use local file storage for development behind a replaceable storage interface. Production provider selection is deferred. | Developer clarification, 2026-11-04 |
| BD-22 | Invitation preview link | Preview links expire 14 days after creation. Creating a replacement revokes all previous active preview links for that order. | Developer clarification, 2026-11-04 |
| BD-23 | Invitation extensions | An admin may extend a published or paused, unexpired invitation by one calendar month at a time. An expired invitation cannot be extended or reactivated. | Developer clarification, 2026-11-04 |
| BD-24 | Invitation lifecycle timing | Configure default validity as whole months (`InvitationLifecycle:DefaultValidityMonths`, default 2; applies only to new publications). A hosted job checks every 5 minutes by default and idempotently marks due Published/Paused invitations Expired. Store and compare UTC instants derived from the invitation's local wedding time zone; move skipped daylight-saving boundary times forward to the first valid minute and resolve repeated times to the earlier UTC instant. | Implementation default selected for P10-T1; configurable operational setting |
| BD-25 | Expired media recovery | At three months after the wedding, delete temporary media objects permanently and mark their database metadata deleted; do not keep an application-level recovery copy. Missing objects count as already deleted so retries are safe. Production backup policy remains a deployment decision. | Implementation default selected for P10-T2; follows BD-03 |

## Decisions to revisit when implementation reaches them

These are configuration or technical choices rather than unresolved business rules. They will be proposed with a clear rationale before the dependent task where appropriate:

- Production backup and recovery provider for media outside the application's retention window.
- Admin role and permission breakdown.
- Exact approval and change-request record fields.
- Production file storage provider and PNG generation storage.
- Egypt time-zone storage and daylight-saving implementation details.

No implementation should silently change the confirmed business decisions above.

## P11 reporting and audit behavior

- Dashboard dates are inclusive Cairo calendar dates; the default range is the current calendar month. Supplying only one boundary is invalid; ranges are limited to ten years.
- Period counts use the selected interval. Active invitations are a current snapshot. Collected payments use their recorded date; outstanding balances are current balances for orders created in the interval, grouped by currency.
- Audit records are created for authenticated admin write actions and retain actor, controller/action, entity type and route ID when present, UTC time, HTTP status, and outcome. Request bodies, query values, passwords, tokens, and other secrets are never recorded. Audit search is SuperAdmin-only.
