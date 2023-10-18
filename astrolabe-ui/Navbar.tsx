"use client";

import { cn } from "@astrolabe/client/util/utils";
import {
  AnchorHTMLAttributes,
  Attributes,
  HTMLAttributes,
  MouseEventHandler,
  PropsWithChildren,
  ReactElement,
} from "react";
import { Button } from "./Button";

export interface NavLink {
  label?: string;
  path: string | null | (() => void);
  children?: NavLink[] | ReactElement;
}

export function Sidebar({ links }: { links: NavLink[] }) {
  return (
    <nav>
      <NavList>{links.map((item) => makeNavItem(item))}</NavList>;
    </nav>
  );
}

export function NewSidebar() {
  return (
    <NavList>
      <NavItem path={"/"} label={"Hello world"}>
        <Button>Test</Button>
      </NavItem>
      <NavItem path={null} label={"Hello world again!"}>
        <NavItem path={"/"} label={"Hello world for the third time!"}></NavItem>
      </NavItem>
      <NavItem path={"/"} label={"Hello world"} />
    </NavList>
  );
}

export function NavList({
  children,
  className,
  ...props
}: PropsWithChildren<HTMLAttributes<HTMLUListElement>>) {
  return (
    <ul {...props} className={cn("flex flex-col gap-2", className)}>
      {children}
    </ul>
  );
}

const navList: NavLink[] = [
  {
    path: "/",
    label: "Hello world",
  },
  {
    path: null,
    children: [
      {
        path: "/",
        label: "Hello world",
      },
      {
        path: "/",
        label: "Hello world",
      },
      {
        path: "/",
        label: "Hello world",
        children: <Sidebar links={[]}></Sidebar>,
      },
    ],
  },
];

export function makeNavItem(item: NavLink) {
  const { children, ...props } = item;
  if (Array.isArray(children)) {
    return (
      <NavList {...props}>
        {children.map((subitem) => (
          <NavItem {...subitem}>{subitem.children}</NavItem>
        ))}
      </NavList>
    );
  }
  return <NavItem {...item}>{children}</NavItem>;
}

export const makeNavItems = (items: NavLink[]) =>
  items.map((item) => makeNavItem(item));

export function NavItem(
  { children, path, label, ...props }: NavLink
  // & HTMLAttributes<HTMLAnchorElement>
) {
  const child = Array.isArray(children)
    ? children.map((child) => makeNavItem(child))
    : label ?? children;
  if (typeof path === "function") {
    const handleClick: MouseEventHandler = () => path();
    return (
      <li className="flex flex-col gap-2">
        <a href="javascript:;" onClick={handleClick} {...props}>
          {child}
        </a>
      </li>
    );
  }
  return (
    <li className="flex flex-col gap-2">
      <a {...props}>{child}</a>
    </li>
  );
}
