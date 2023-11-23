import { useContext, useEffect, useState } from "react";
import { AppContext } from "./index";
import { Control, newControl, useControl } from "@react-typed-forms/core";
import { RouteData } from "../app/routeData";
import { useNavigationService } from "./navigation";

export interface UserState {
  busy: boolean;
  loggedIn: boolean;
  accessToken?: string | null;
}

export interface SecurityService {
  currentUser: Control<UserState>;

  fetch(input: RequestInfo | URL, init?: RequestInit): Promise<Response>;

  login(): Promise<void>;

  logout(): Promise<void>;
}

export interface TokenSecurityService extends SecurityService {
  setToken(accessToken: string): Promise<any>;
}

export interface SecurityServiceContext {
  security: SecurityService;
}

export function useSecurityService<
  T extends SecurityService = SecurityService,
>(): T {
  const sc = useContext(AppContext).security;
  if (!sc) throw "No SecurityService present";
  return sc;
}

const guestUserState = newControl<UserState>({ busy: true, loggedIn: false });

export const guestSecurityService: SecurityService = {
  currentUser: guestUserState,
  fetch: typeof window === "undefined" ? fetch : window.fetch.bind(window),
  async logout() {},
  async login() {},
};

export function createAccessTokenFetcher(
  getToken: () => Promise<string | null | undefined>,
): (input: RequestInfo | URL, init?: RequestInit) => Promise<Response> {
  return async (url, init) => {
    const token = await getToken();
    if (token) {
      const request = new Request(url, init);
      request.headers.set("Authorization", "Bearer " + token);
      return fetch(request);
    }
    return await fetch(url, init);
  };
}

export function useControlTokenSecurity(): TokenSecurityService {
  const user = useControl<UserState>({
    busy: true,
    accessToken: null,
    loggedIn: false,
  });
  useEffect(() => {
    const accessToken = getTokenStorage().getItem("token");
    user.value = { busy: false, accessToken, loggedIn: !!accessToken };
  }, []);
  return {
    currentUser: user,
    fetch: createAccessTokenFetcher(
      async () => user.fields.accessToken.current.value,
    ),
    async logout() {
      getTokenStorage().removeItem("token");
      user.value = { busy: false, accessToken: null, loggedIn: false };
    },
    async login() {},
    async setToken(accessToken: string) {
      getTokenStorage().setItem("token", accessToken);
      user.setValue((v) => ({
        ...v,
        loggedIn: true,
        accessToken,
      }));
    },
  };
}

function getTokenStorage(): Pick<
  Storage,
  "getItem" | "setItem" | "removeItem"
> {
  if (typeof sessionStorage === "undefined") {
    return {
      getItem: () => null,
      setItem: () => {},
      removeItem() {},
    };
  }
  return sessionStorage;
}

export interface PageSecurity {
  allowGuests?: boolean;
  forwardAuthenticated?: boolean;
  dontWaitForAuth?: boolean;
}

export function usePageSecurity(
  loginHref: string = "/login",
  defaultHref: string = "/",
): boolean {
  const security = useSecurityService();
  const nav = useNavigationService<PageSecurity>();
  const route = nav.route;
  const fields = security.currentUser.fields;
  const busy = fields.busy.value && !route.dontWaitForAuth;
  const loggedIn = fields.loggedIn.value;
  const forwardLogin = !busy && !loggedIn && !route.allowGuests;
  const forwardAuth = !busy && loggedIn && route.forwardAuthenticated;

  useEffect(() => {
    if (forwardLogin) {
      nav.push(loginHref);
    }
  }, [forwardLogin]);
  useEffect(() => {
    if (forwardAuth) {
      nav.replace(defaultHref);
    }
  }, [forwardAuth]);
  return busy || forwardAuth || forwardLogin;
}
