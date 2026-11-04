# Public Website and Design Showcase

WedLink serves one anonymous public website at `/` for service information, active design discovery, demo previews, and WhatsApp enquiries. The protected administration interface remains at `/admin`.

## Public experience

- The home page introduces WedLink, links to the design collection, explains the request/review/delivery steps, and provides a configurable WhatsApp contact link.
- The design collection comes from `GET /api/v1/public/templates`. Only active templates with an active version are returned. The response contains the template name, description, safe preview image reference, active version number, and enabled public feature descriptions. It does not contain package keys, customer/order data, or private credentials.
- Each design opens `/designs/{templateId}`. The demonstration uses fixed sample names, date, and location. It is visibly labeled as sample content and does not submit RSVP or message data.
- Preview image references accept HTTPS URLs without embedded credentials or same-origin absolute paths. Other schemes are omitted from the public response.

## Contact configuration

Copy `Frontend/.env.example` to `Frontend/.env.local` and set `VITE_PUBLIC_WHATSAPP_PHONE` to the business number in international digits without `+` or spaces. `VITE_PUBLIC_WHATSAPP_MESSAGE` controls the prefilled message. Local environment files are ignored by Git. The sample phone is a placeholder and must be replaced for live contact.

## Availability behavior

Activating a template version makes that version eligible for the public list when its parent template is active. Deactivating the parent template removes it from the public list immediately. The API chooses the most recently created active version and exposes only enabled features.
