export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  status: number;
  problem?: ProblemDetails;
  constructor(status: number, message: string, problem?: ProblemDetails) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.problem = problem;
  }
}

export function parseProblemDetails(payload: unknown): ProblemDetails | undefined {
  if (!payload || typeof payload !== "object") return undefined;
  const p = payload as Record<string, unknown>;
  return {
    type: typeof p.type === "string" ? p.type : undefined,
    title: typeof p.title === "string" ? p.title : undefined,
    status: typeof p.status === "number" ? p.status : undefined,
    detail: typeof p.detail === "string" ? p.detail : undefined,
    instance: typeof p.instance === "string" ? p.instance : undefined,
    errors: p.errors && typeof p.errors === "object" ? (p.errors as Record<string, string[]>) : undefined,
  };
}

export function toApiError(response: Response, payload: unknown): ApiError {
  const problem = parseProblemDetails(payload);
  const message =
    problem?.detail ||
    problem?.title ||
    (typeof payload === "string" ? payload : "") ||
    `Request failed with status ${response.status}`;
  return new ApiError(response.status, message, problem);
}

export function isNetworkError(error: unknown): boolean {
  return error instanceof TypeError && /fetch|network/i.test(error.message);
}
