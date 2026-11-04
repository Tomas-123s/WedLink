# WedLink — API Plan

**Status:** Task P0-T3 draft for review
**Base prefix:** `/api/v1`
**Format:** JSON over HTTPS. Public invitation media is served through configured storage URLs or a controlled media endpoint.

This is a route and responsibility plan, not a final OpenAPI contract. Request/response DTOs and exact status codes will be designed with each API task.

## Conventions

- Admin routes require authentication and a backend authorization policy unless explicitly stated public.
- Public routes are rate-limited and return only fields safe for guests.
- Use `GET` for reads, `POST` for creation/actions, `PUT` for replacing a submitted resource, and `PATCH` only for a small partial update where it improves clarity.
- Use pagination for dashboard lists. Support a small, explicit set of filters and search terms.
- Use stable IDs in admin routes. Use the public invitation slug for guest viewing.
- Do not return EF/database entities directly. Use request and response DTOs.
- Return consistent Problem Details errors. Never include stack traces, secrets, raw private tokens, or private customer data in public errors.
- Put raw private tokens in the authorization header/body after the frontend loads the private page; do not include them in ordinary application logs or analytics.

## Endpoint groups

### 1. Admin authentication

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/auth/csrf` | Issue an antiforgery request token for the browser |
| POST | `/admin/auth/login` | Sign an admin in |
| POST | `/admin/auth/logout` | End the admin session |
| GET | `/admin/auth/me` | Return current admin identity and permitted actions |

Identity and the final cookie/CSRF configuration are implemented with the authentication task.

### 2. Customers

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/customers` | Search and paginate customers |
| POST | `/admin/customers` | Create a customer |
| GET | `/admin/customers/{customerId}` | View customer details and related order/payment summaries |
| PUT | `/admin/customers/{customerId}` | Update customer contact details and notes |
| DELETE | `/admin/customers/{customerId}` | Soft-delete a customer |

Customers have no login endpoints or customer dashboard.

### 3. Orders and private submission links

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/orders` | Search/filter/paginate orders |
| POST | `/admin/orders` | Create an order with customer, selected template version, base price, and add-ons |
| GET | `/admin/orders/{orderId}` | View order, status history, payments, submission state, and invitation summary |
| PATCH | `/admin/orders/{orderId}/status` | Move an order through an allowed workflow status |
| POST | `/admin/orders/{orderId}/submission-links` | Generate or rotate a private customer submission link |
| DELETE | `/admin/orders/{orderId}/submission-links/{linkId}` | Revoke a private link |
| GET | `/admin/orders/{orderId}/submission-links/current` | Return active link metadata without exposing its token |
| GET | `/admin/orders/{orderId}/customer-submission` | Review submitted wedding details and private media metadata |
| POST | `/admin/orders/{orderId}/customer-submission/review` | Record the reviewing admin and time |
| GET | `/admin/orders/{orderId}/customer-submission/media/{mediaId}/content` | Stream a private media file to an authorized admin |
| DELETE | `/admin/orders/{orderId}/customer-submission/media/{mediaId}` | Remove an unsuitable file and record who removed it |
| GET | `/customer-submissions/current` | Read the order form associated with the private-link credential |
| PUT | `/customer-submissions/current` | Submit/update wedding information and customization selections |
| POST | `/customer-submissions/current/media` | Upload a file after server validation |
| DELETE | `/customer-submissions/current/media/{mediaId}` | Remove an uploaded file before admin review, subject to workflow rules |

A submission credential is scoped to one order, expires, and can be revoked. Raw token handling must follow the security conventions above.
The submission link expires after 14 days. Rotating it revokes prior active submission links for that order. The one-time URL carries the token in its URL fragment so it is not sent in HTTP requests or referrer headers; the page removes the fragment after reading it and sends the token only in the Authorization header. Uploads accept JPEG/PNG/WebP (10 MB each), MP4 (100 MB), and MP3/M4A (20 MB), with a 500 MB active-media limit per order. Public submission reads/updates are rate-limited; uploads have a stricter rate limit.

### 4. Templates, versions, features, and add-ons

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/templates` | List templates and active versions |
| POST | `/admin/templates` | Register template metadata |
| GET | `/admin/templates/{templateId}` | View template metadata and version history |
| PUT | `/admin/templates/{templateId}` | Update template metadata |
| PATCH | `/admin/templates/{templateId}/status` | Activate or deactivate template selection for new orders |
| POST | `/admin/templates/{templateId}/versions` | Register a trusted, deployed template version |
| GET | `/admin/template-versions/{templateVersionId}` | View features, limits, and available add-ons |
| PUT | `/admin/template-versions/{templateVersionId}/features` | Configure supported features and limits |
| PUT | `/admin/template-versions/{templateVersionId}/addons` | Configure available add-ons |
| POST | `/admin/template-versions/{templateVersionId}/activate` | Activate a version for new orders |
| GET | `/admin/features` | List supported features |
| GET | `/admin/addons` | List add-ons |
| POST | `/admin/addons` | Create an add-on |
| PUT | `/admin/addons/{addonId}` | Update current add-on metadata and price |

These endpoints manage metadata and trusted code registrations, not arbitrary executable uploads. Existing invitations remain attached to their saved version.

### 5. Invitations and private preview

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/orders/{orderId}/invitation` | View invitation draft, supported features, available media, and review events |
| PUT | `/admin/orders/{orderId}/invitation` | Create or edit invitation details, supported customizations, and selected media |
| POST | `/admin/orders/{orderId}/invitation/preview-links` | Create a private 14-day preview link and revoke its predecessor |
| POST | `/admin/invitations/{invitationId}/publish` | Publish an approved invitation |
| POST | `/admin/invitations/{invitationId}/pause` | Temporarily pause a valid invitation |
| POST | `/admin/invitations/{invitationId}/reactivate` | Reactivate a paused invitation before expiry |
| POST | `/admin/invitations/{invitationId}/extend` | Extend a valid invitation before expiry |
| POST | `/admin/invitations/{invitationId}/final-card` | Save the final PNG card once after customer approval |
| GET | `/admin/invitations/{invitationId}/final-card` | Download the stored final PNG card |
| PUT | `/admin/invitations/{invitationId}/music` | Select or clear an authorized active library track when supported |
| GET | `/invitation-previews/current` | Read preview data using its private credential |
| GET | `/invitation-previews/current/media/{mediaId}` | Read media attached to this preview only |
| POST | `/invitation-previews/current/review` | Submit approval or a change request and append a review event |

The preview link is a 14-day bearer credential whose SHA-256 hash is stored in `PrivateLinkTokens` with a distinct purpose. Its raw value appears only in the URL fragment, is removed from the address bar before requests, and is sent in the `Authorization` header. Creating a replacement revokes the prior preview link. The backend enforces that a preview is private, the invitation is not public before publication, and an expired or revoked credential is rejected.

### 6. Public invitation and guest actions

| Method | Route | Purpose |
|---|---|---|
| GET | `/public/invitations/{slug}` | Get a published invitation or its event-ended representation |
| GET | `/public/invitations/{slug}/share` | Return crawler-readable Open Graph/Twitter metadata and redirect guests to the invitation page |
| GET | `/public/invitations/{slug}/share-image` | Return the public card/image while active; redirect expired invitations to the generic share image |
| GET | `/public/invitations/{slug}/media/{mediaId}` | Read media selected for this published invitation |
| GET | `/public/invitations/{slug}/final-card` | Download its permanent PNG card, including after page expiry |
| GET | `/public/invitations/{slug}/guest-interactions` | Return enabled guest features, public messages, attendance count when enabled, and selected music metadata |
| POST | `/public/invitations/{slug}/rsvps` | Create an RSVP response or update one using the browser-held edit token |
| POST | `/public/invitations/{slug}/messages` | Submit a congratulation message if enabled |
| GET | `/public/invitations/{slug}/music` | Stream the selected active track for this published invitation |

Public responses include only enabled template sections and guest-safe content. RSVP count means one responding guest plus additional companions for a Yes response; No contributes zero. Guest message visibility follows invitation settings.

The share page includes only public couple names while an invitation is active. Expired invitations use generic text and the generic WedLink image. The API requires `PublicSite:BaseUrl` to point to the frontend origin; the `/api` reverse proxy must keep that origin shared with the frontend. Metadata and share-image responses are marked no-store so a fresh crawler request rechecks invitation state. Social platforms may keep their own previews longer; use their preview refresh tools after changing a published card or invitation names.

### 7. RSVP and guest-message administration

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/invitations/{invitationId}/rsvps` | View RSVP responses and attendance summary |
| GET | `/admin/invitations/{invitationId}/messages` | View guest messages |
| DELETE | `/admin/invitations/{invitationId}/messages/{messageId}` | Remove an inappropriate message and record the action |

Admin endpoints also include `GET /admin/music-tracks`, `POST /admin/music-tracks` (multipart MP3/M4A up to 20 MB with a recorded rights reference), and `PUT /admin/music-tracks/{trackId}`. Only active tracks can be selected for an invitation or streamed publicly.

### 8. Music and media

| Method | Route | Purpose |
|---|---|---|
| GET | `/public/music-tracks` | List active built-in tracks safe for customer selection |
| GET | `/admin/music-tracks` | List tracks for administration |
| POST | `/admin/music-tracks` | Add a licensed/authorized library track and metadata |
| PUT | `/admin/music-tracks/{trackId}` | Update track metadata or active status |
| POST | `/admin/invitations/{invitationId}/media` | Admin upload/replace invitation media |
| DELETE | `/admin/invitations/{invitationId}/media/{mediaId}` | Remove unsuitable media before publication |

Customer music upload uses the private submission credential and the same file-validation/storage rules as other uploads. The API does not accept executable template packages.

### 9. Payments

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/orders/{orderId}/payments` | List payment records |
| POST | `/admin/orders/{orderId}/payments` | Record a payment/installment |
| PUT | `/admin/payments/{paymentId}` | Correct payment details with audit history, subject to policy |
| GET | `/admin/orders/{orderId}/balance` | Return server-calculated base price, add-ons, paid amount, and balance |

Financial values are decimal amounts. The total is the agreed base price plus add-on price snapshots. The client cannot set the calculated balance or payment status directly.

### 10. Dashboard, users, and audit

| Method | Route | Purpose |
|---|---|---|
| GET | `/admin/dashboard/summary?from=YYYY-MM-DD&to=YYYY-MM-DD` | Return invitation/order summary counts, backend expected-attendance total for currently active invitations, and financial summaries for inclusive Cairo dates; defaults to the current month |
| GET | `/admin/users` | List admin users (permission restricted) |
| POST | `/admin/users` | Create an admin account (Super Admin only) |
| PUT | `/admin/users/{userId}/roles` | Assign roles/permissions (Super Admin only) |
| GET | `/admin/audit-logs` | Search audit history (Super Admin only); filters: userId, action, entityType, entityId, outcome, from, to, page, pageSize |

No visitor analytics endpoint is planned for the MVP.

### 11. Public marketing and design showcase

| Method | Route | Purpose |
|---|---|---|
| GET | `/public/templates` | List active templates that have an active version, exposing marketing copy and enabled feature descriptions only; never return package keys or order/customer information |

Public design previews use fixed demonstration couple/date/location data. The WhatsApp phone and prefilled message are configured by frontend environment variables; the phone number is not embedded in source control.

## Route shape and security notes

- `{customerId}`, `{orderId}`, `{invitationId}`, and similar placeholders are database identifiers; they are not authorization credentials.
- Public invitation slugs identify only public invitations. They must never grant access to an order, preview, customer submission, or admin data.
- Private submission and preview credentials must be validated on every request and scoped to the relevant order/invitation.
- RSVP edit tokens are separate from customer/admin credentials. A guest's browser can update only its own RSVP response.
- Rate limit public RSVP, message, token validation, and upload endpoints.
- Admin actions that change orders, invitations, payments, permissions, templates, or messages create audit records.

## Not included in the MVP API

Payment gateway callbacks, email/push notifications, visitor/page-view analytics, couple replies, customer accounts, and general customer dashboards.
