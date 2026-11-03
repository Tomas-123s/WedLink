# API Error Responses

**Applies to:** `/api/v1`
**Format:** Problem Details JSON

## What the API returns

API errors use the standard Problem Details shape. Each response includes a status, a short title, and a `traceId` that can be used to find the corresponding server log entry.

A route that does not exist returns a response like:

```json
{
  "type": "about:blank",
  "title": "Not Found",
  "status": 404,
  "traceId": "0HN..."
}
```

Invalid request DTOs return `400 Bad Request` with a `ValidationProblemDetails` response. The `errors` field contains messages grouped by field. Controller actions should use `[ApiController]` and DTO properties should use simple validation attributes such as `[Required]`, `[StringLength]`, and `[Range]` where they fit.

Unhandled server errors return a generic `500 Internal Server Error` response. Exception details belong in server logs; do not send stack traces or secrets to clients.

## Where this is configured

- `Program.cs` registers the Problem Details service and adds a trace ID.
- `UseExceptionHandler()` converts unhandled exceptions into API error responses.
- `UseStatusCodePages()` gives otherwise-empty HTTP error responses (for example, 404) a body.
- `[ApiController]` enables automatic model-validation responses when request DTOs are added.

Keep business-rule validation in the Business layer as those features are implemented. Request-shape validation at the API boundary does not replace those checks.
