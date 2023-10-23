import { NavigationService } from "@astrolabe/client/service/navigation";
import Link from "next/link";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { parse } from "querystring";
import { AnchorHTMLAttributes, FC } from "react";

export function useNextNavigationService(): NavigationService {
  const router = useRouter();
  const searchParams = useSearchParams()!;
  const pathname = usePathname()!;
  const segments = pathname ? pathname.split("/").filter((x) => x.length) : [];

  const query = parse(searchParams.toString());
  return {
    query,
    pathname,
    isReady: true,
    ...router,
    pathSegments: segments,
    get: searchParams.get,
    getAll: searchParams.getAll,
    Link: Link as FC<AnchorHTMLAttributes<HTMLAnchorElement>>,
  };
}
