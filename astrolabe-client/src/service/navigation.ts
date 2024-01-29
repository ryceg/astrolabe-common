import { AnchorHTMLAttributes, FC, useContext } from "react";
import { AppContext } from "./index";
import { ParsedUrlQuery } from "querystring";
import { RouteData } from "../app/routeData";

export interface NavigationService<T = {}> {
  query: ParsedUrlQuery;
  get(queryParam: string): string | null;
  getAll(queryParam: string): string[];
  pathSegments: string[];
  pathname: string;
  isReady: boolean;
  replace(path: string): void;
  push(path: string): void;
  Link: FC<AnchorHTMLAttributes<HTMLAnchorElement>>;
  route: RouteData<T>;
  pathAndQuery(): string;
}

export interface NavigationServiceContext {
  navigation: NavigationService;
}

export function useNavigationService<T = {}>(): NavigationService<T> {
  const sc = useContext(AppContext).navigation;
  if (!sc) throw "No NavigationService present";
  return sc;
}
