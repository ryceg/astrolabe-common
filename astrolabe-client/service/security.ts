import { useContext } from "react";
import { AppContext } from "./index";
import { Control, newControl } from "@react-typed-forms/core";

export interface UserState {
	busy: boolean;
	loggedIn: boolean;
	accessToken?: string | null;
}

export interface SecurityService {
	currentUser: Control<UserState>;

	fetcher: { fetch(url: RequestInfo, init?: RequestInit): Promise<Response> };

	checkAuthentication(): void;

	login(): Promise<void>;

	logout(): Promise<void>;

	authCallback(): Promise<void>;
}

export interface SecurityServiceContext {
	security: SecurityService;
}

export function useSecurityService(): SecurityService {
	const sc = useContext(AppContext).security;
	if (!sc) throw "No SecurityService present";
	return sc;
}

const guestUserState = newControl<UserState>({ busy: true, loggedIn: false });
export const guestSecurityService: SecurityService = {
	checkAuthentication() {},
	currentUser: guestUserState,
	fetcher: typeof window !== "undefined" ? window : { fetch },
	async logout() {},
	async authCallback() {},
	async login() {},
};
