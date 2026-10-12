interface JwtPayload {
  exp?: number;
  "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"?: string | string[];
  role?: string | string[];
  roles?: string | string[];
}

function decodeJwtPayload(token: string): JwtPayload | null {
  const payloadPart = token.split(".")[1];
  if (!payloadPart) {
    return null;
  }

  try {
    // Base64url → Base64 → UTF-8
    const base64 = payloadPart.replace(/-/g, "+").replace(/_/g, "/");
    const json = decodeURIComponent(
      atob(base64)
        .split("")
        .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, "0")}`)
        .join("")
    );

    return JSON.parse(json) as JwtPayload;
  } catch {
    return null;
  }
}

/**
 * Trích xuất danh sách role từ JWT access token.
 * Backend sinh claim role ở cả 2 định dạng: ClaimTypes.Role và "role".
 */
export function getRolesFromToken(accessToken: string | null): string[] {
  if (!accessToken) {
    return [];
  }

  const payload = decodeJwtPayload(accessToken);
  if (!payload) {
    return [];
  }

  const raw =
    payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"] ??
    payload.role ??
    payload.roles ??
    [];

  const list = Array.isArray(raw) ? raw : [raw];
  return list.filter((role): role is string => typeof role === "string");
}

/** Kiểm tra token còn hạn (dựa trên claim exp). */
export function isTokenExpired(accessToken: string | null): boolean {
  const payload = accessToken ? decodeJwtPayload(accessToken) : null;
  if (!payload?.exp) {
    return true;
  }

  return payload.exp * 1000 <= Date.now();
}
