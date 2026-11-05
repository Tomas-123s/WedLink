# Future Invitation Experience Requirements

This document adds to the original WedLink BRD. The original BRD remains valid except where these requirements explicitly update it. The requirements below are recorded for later implementation; visual specifications that have not been approved remain open.

## 30. Visual-first invitation templates

Invitation designs must be created and reviewed visually before implementation as React templates. Use this workflow for each design:

1. Define the visual concept.
2. Create the visual design.
3. Specify layout, colors, typography, and sections.
4. Specify animation and transitions where applicable.
5. Review and approve the design.
6. Implement the approved design as a separate, understandable React template.
7. Test the template on mobile and desktop.
8. Add it to the template catalog.

The visual designs are discussed and approved separately before implementation. The initial concepts and detailed visual specifications will be finalized separately. The first selected direction is **Cinematic Wedding Invitation**; its appearance, colors, animation sequence, and visual assets are still pending. Do not invent or implement those details before approval.

The system must support different visual styles. Some templates may provide animated and interactive experiences; others may remain static, simple, and elegant. Animation capability belongs to the relevant template and must not be assumed for every template.

Motion-capable templates may support animated opening sequences, gradual couple-name reveals, decorative motion, section transitions, image reveals, scroll-triggered effects, countdowns, and interactive visual elements. Motion must be purposeful, performant on mobile and desktop, respect reduced-motion preferences, and never obscure important wedding information.

## 31. Invitation social sharing metadata

Every public invitation must provide suitable social sharing metadata. When a guest shares an invitation URL on WhatsApp or another supported platform, its link preview should show an invitation title or couple names, a suitable preview image, and a short description.

- Metadata is generated dynamically for each invitation.
- The preview image comes from an appropriate invitation image or generated card.
- Metadata must not expose private information.
- The preview remains appropriate after invitation expiration.
- Metadata must work with common social-sharing crawlers, which may not execute the React application.
- Account for social platforms caching link previews.

The share action uses a same-origin HTML metadata route so crawlers receive Open Graph and Twitter card tags without executing React. Active invitations may show public couple names and a public invitation card or image. Expired invitations use generic title, description, and image, without couple names. The API avoids caching metadata so each crawler request checks the current invitation state; social platforms can retain previews longer, so published changes may require a platform preview refresh.

## 32. Optional countdown

Countdown is an optional template feature. When enabled, it displays the remaining time until the wedding date and time and updates automatically using the actual event date, time, and time zone. After the event time passes, it stops and changes to a template-configurable post-event message. Countdown behavior is independent of invitation expiration.

The current generic invitation editor accepts an optional post-event message in the enabled `countdown` customization; when it is blank, the public invitation shows a neutral default message. The shared countdown presentation is static and exposes timer semantics without announcing each second to assistive technology. Template-specific placement and styling remain dependent on the pending visual design workflow.

## 33. Final RSVP attendance rule

The guest enters the number of accompanying people only. The responding guest is counted automatically by the backend:

- `Yes`: expected attendance is `1 + accompanying people`.
- `No`: expected attendance is zero and no accompanying-person count is recorded.

The frontend is not the source of the final attendance calculation. The Admin Dashboard displays the correct aggregate expected attendance.

## 34. Application areas and MVP additions

The React application has three areas:

1. **Public Marketing Website:** homepage, design gallery, design previews, and WhatsApp contact.
2. **Public Invitation Pages:** individual wedding invitations, template-specific sections and animations, RSVP and guest messages when supported, music controls, optional countdown, and social metadata.
3. **Admin Dashboard:** existing management features defined by the original BRD.

All areas remain responsive, Arabic-first, and RTL-compatible. The MVP includes the public marketing homepage and gallery, public design previews, configurable WhatsApp contact, dynamic template visibility, the visual-first approval workflow, support for static and animated templates, the Cinematic direction as the first concept, per-invitation social metadata, optional countdown with post-event state, and the final RSVP rule.

Detailed visual specifications for each template are prepared and approved separately before that template is implemented.

## Implementation guardrails

- Keep the public marketing website separate from protected administration pages.
- The public gallery reads active, publicly available templates from the backend and uses demonstration data in previews.
- Do not implement template-specific animations before their visual specifications are approved.
- Do not invent the Cinematic template's detailed appearance.
- Implement each approved template as a separate, understandable React template; share components only where they provide clear value.
- Public invitation data exposes only a supported `rendererKey` (`editorial-luxury` or `default`), resolved server-side from the internal package key. Never include the internal package key in public responses. Use `editorial-luxury-v1` (or a `cinematic-v*` key) to select the Editorial Luxury React template; unknown keys use the default renderer.
- Preserve the task-by-task development workflow. Do not begin work from this roadmap until the developer has reviewed and approved it.
