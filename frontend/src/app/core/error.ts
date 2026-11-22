/** Extrae el mensaje de un ProblemDetails devuelto por la API. */
export function extractErrorMessage(error: unknown, fallback: string): string {
  const response = error as { error?: { detail?: string; title?: string }; message?: string };
  return response?.error?.detail ?? response?.error?.title ?? response?.message ?? fallback;
}
