import { NavigationService } from "@astroapps/client/service/navigation";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { parse, stringify } from "querystring";
import { AnchorHTMLAttributes, FC } from "react";
import { getMatchingRoute, RouteData } from "@astroapps/client/app/routeData";

export function useNextNavigationService<T = {}>(
  routes?: Record<string, RouteData<T>>,
  defaultRoute?: RouteData<T>,
): NavigationService<T> {
  const router = useRouter();
  const searchParams = useSearchParams()!;
  const pathname = usePathname()!;
  const pathSegments = pathname
    ? pathname.split("/").filter((x) => x.length)
    : [];

  const query = parse(searchParams.toString());
  const route =
    (routes && getMatchingRoute(routes, pathSegments)) ??
    defaultRoute ??
    ({} as RouteData<T>);
  return {
    query,
    pathSegments,
    pathname,
    isReady: true,
    ...router,
    get: searchParams.get,
    getAll: searchParams.getAll,
    Link: Link as FC<AnchorHTMLAttributes<HTMLAnchorElement>>,
    route,
    pathAndQuery: () =>
      pathname + (searchParams.size > 0 ? "?" + stringify(query) : ""),
  };
}
