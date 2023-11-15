import { HTMLAttributes, ReactNode } from "react";
import { useNavigationService } from "@astrolabe/client/service/navigation";
import { RouteData } from "@astrolabe/client/app/routeData";
import { PageSecurity } from "@astrolabe/client/service/security";

export interface NavLink {
  /** The label to display for the link. */
  label?: string;
  /** The path to navigate to when the link is clicked. When passed a function, it fires that function instead. */
  path: string | null | (() => void);

  render?: (props: NavLinkProps) => ReactNode;
  /**
   * An optional icon
   */
  icon?: ReactNode;
}

export interface NavLinkProps {
  navLink: NavLink;
  className?: string;
  iconClass?: string;
  labelClass?: string;
  href?: string;
  onClick?: () => void;
}

interface NavListClasses {
  className: string;
  entryClass: string;
  linkClass: string;
  labelClass: string;
  iconClass: string;
}
export interface NavListProps {
  links: NavLink[];
  defaultRenderLink?: (link: NavLinkProps) => ReactNode;
  classes?: Partial<NavListClasses>;
}

export const defaultNavListClasses: NavListClasses = {
  className: "p-4 w-80 space-y-2 font-bold",
  entryClass: "hover:bg-surface-100 rounded-md p-2",
  iconClass: "text-2xl w-8 text-center",
  labelClass: "hover:underline",
  linkClass: "flex gap-x-4 items-center",
};

export function NavList({
  links,
  classes,
  defaultRenderLink = (p) => <DefaultNavLink {...p} />,
}: NavListProps) {
  const { className, entryClass, linkClass, labelClass, iconClass } = {
    ...defaultNavListClasses,
    ...classes,
  };
  return (
    <ul className={className}>
      {links.map((navLink, i) => (
        <li className={entryClass} key={i}>
          {doRender(navLink)}
        </li>
      ))}
    </ul>
  );

  function doRender(navLink: NavLink) {
    const { path } = navLink;
    const props = {
      navLink,
      labelClass,
      iconClass,
      className: linkClass,
      href: typeof path === "string" ? path : undefined,
      onClick: typeof path === "function" ? path : undefined,
    };
    return (navLink?.render ?? defaultRenderLink)(props);
  }
}

export function DefaultNavLink({
  navLink: { icon, label },
  labelClass,
  iconClass,
  ...linkProps
}: NavLinkProps) {
  const { Link } = useNavigationService();
  return (
    <Link {...linkProps}>
      <span className={iconClass}>{icon}</span>
      <span className={labelClass}>{label}</span>
    </Link>
  );
}

export interface NavLinkRouteData {
  navLink?: boolean;
  icon?: ReactNode;
  linkOrder?: number;
}

export function createNavLinks(
  routes: Record<string, RouteData<NavLinkRouteData>>,
): NavLink[] {
  const sorted = Object.entries(routes)
    .filter((x) => x[1].navLink)
    .sort((a, b) => (a[1].linkOrder ?? 0) - (b[1].linkOrder ?? 0));
  return sorted.map(([path, r]) => ({ path, label: r.label, icon: r.icon }));
}
