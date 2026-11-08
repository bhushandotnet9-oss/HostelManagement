import { HttpErrorResponse } from '@angular/common/http';

/**
 * The API answers a blocked delete or a rejected update with a plain-text
 * explanation (e.g. "This room still has cots.") alongside a 409 or 400. That
 * text is what the owner needs to see, so prefer it over the generic fallback.
 */
export function apiErrorMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  const body = error.error;

  if (typeof body === 'string' && body.trim()) {
    return body.trim();
  }

  // ASP.NET Core ProblemDetails, when model binding rejects the request.
  if (body && typeof body === 'object') {
    const detail = (body as { detail?: unknown }).detail;
    if (typeof detail === 'string' && detail.trim()) {
      return detail.trim();
    }

    const title = (body as { title?: unknown }).title;
    if (typeof title === 'string' && title.trim()) {
      return title.trim();
    }
  }

  return fallback;
}
