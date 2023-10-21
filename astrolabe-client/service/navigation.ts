import { useContext } from "react";
import { AppContext } from "./index";

export interface NavigationService {
  query: URLSearchParams;
  pathname: string;
  isReady: boolean;
  replace(path: string): void;
  push(path: string): void;
  pathSegments: string[];
}

export interface NavigationServiceContext {
  navigation: NavigationService;
}

export function useNavigationService(): NavigationService {
  const sc = useContext(AppContext).navigation;
  if (!sc) throw "No NavigationService present";
  return sc;
}
