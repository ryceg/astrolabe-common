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
  href?: string;
  onClick?: () => void;
}

export interface NavListProps {
  links: NavLink[];
  className?: string;
  entryClass?: string;
  linkClass?: string;
  defaultRenderLink?: (link: NavLinkProps) => ReactNode;
}
export function NavList({
  className,
  links,
  entryClass = "flex",
  linkClass,
  defaultRenderLink = (p) => <DefaultNavLink {...p} />,
}: NavListProps) {
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
      className: linkClass,
      href: typeof path === "string" ? path : undefined,
      onClick: typeof path === "function" ? path : undefined,
    };
    return (navLink?.render ?? defaultRenderLink)(props);
  }
}

export function DefaultNavLink({
  navLink: { icon, label },
  ...linkProps
}: NavLinkProps) {
  const { Link } = useNavigationService();
  return (
    <Link {...linkProps}>
      {icon}
      {label}
    </Link>
  );
}

export interface NavLinkRouteData {
  navLink?: boolean;
  icon?: ReactNode;
}

export function createNavLinks(
  routes: Record<string, RouteData<NavLinkRouteData>>,
): NavLink[] {
  return Object.entries(routes)
    .filter((x) => x[1].navLink)
    .map(([path, r]) => ({ path, label: r.label, icon: r.icon }));
}
