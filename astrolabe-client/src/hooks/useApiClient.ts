import { useSecurityService } from "../service/security";

export function useApiClient<A>(
  f: new (
    baseUrl: string | undefined,
    http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> },
  ) => A,
) {
  const securityContext = useSecurityService();
  return createApiClient(f, securityContext);
}

export function createApiClient<A>(
  f: new (
    baseUrl: string | undefined,
    http?: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> },
  ) => A,
  fetcher: {
    fetch(url: RequestInfo, init?: RequestInit): Promise<Response>;
    baseApiUrl?: string;
  },
) {
  return new f(
    fetcher.baseApiUrl ?? typeof window === "undefined"
      ? undefined
      : window.origin,
    fetcher,
  );
}
