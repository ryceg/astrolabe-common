import { Control } from "@react-typed-forms/core";
import { useContext } from "react";
import { AppContext } from "./index";
import { UserState } from "./security";

export interface BreadcrumbService {
	setBreadcrumbLabel(href: string, label: string | undefined): void;
}

export interface BreadcrumbServiceContext {
	breadcrumbs: BreadcrumbService;
}

export function useBreadcrumbService(): BreadcrumbService {
	const sc = useContext(AppContext).breadcrumbs;
	if (!sc) throw "No BreadcrumbService present";
	return sc;
}

export function createBreadcrumbService(
	control: Control<Record<string, string | undefined>>
): BreadcrumbService {
	return {
		setBreadcrumbLabel(href: string, label: string | undefined) {
			control.setValue((o) => ({ ...o, [href]: label }));
		},
	};
}
