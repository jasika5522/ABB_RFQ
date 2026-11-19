const SENSITIVE_KEYS = /password|password_hash|passwordhash|secret|token|jwt|apikey|api_key/i;

export function formatAuditJson(value: unknown): string {
  if (value === null || value === undefined) {
    return '—';
  }
  const redacted = redactSensitive(value);
  return JSON.stringify(redacted, null, 2);
}

function redactSensitive(value: unknown): unknown {
  if (Array.isArray(value)) {
    return value.map((item) => redactSensitive(item));
  }
  if (value !== null && typeof value === 'object') {
    const record = value as Record<string, unknown>;
    const out: Record<string, unknown> = {};
    for (const [key, val] of Object.entries(record)) {
      if (SENSITIVE_KEYS.test(key)) {
        out[key] = '[redacted]';
      } else {
        out[key] = redactSensitive(val);
      }
    }
    return out;
  }
  return value;
}