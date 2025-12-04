/**
 * Route configuration for the application
 * Define which routes should have specific layout behaviors
 */

export interface RouteConfig {
  /** Hide the sidebar for this route */
  hideSidebar?: boolean;
  /** Route requires authentication */
  requiresAuth?: boolean;
  /** Route is only for guests (non-authenticated users) */
  guestOnly?: boolean;
}

/**
 * Route configurations keyed by path pattern
 * Use exact paths or patterns starting with the path
 */
export const routeConfigs: Record<string, RouteConfig> = {
  // Editor should be full-screen without sidebar
  "/editor": {
    hideSidebar: true,
  },
  // Auth pages don't need sidebar
  "/login": {
    hideSidebar: true,
    guestOnly: true,
  },
  "/signup": {
    hideSidebar: true,
    guestOnly: true,
  },
  "/logout": {
    hideSidebar: true,
  },
  "/verify": {
    hideSidebar: true,
  },
  "/forgotPassword": {
    hideSidebar: true,
    guestOnly: true,
  },
  "/resetPassword": {
    hideSidebar: true,
  },
  "/mfa": {
    hideSidebar: true,
  },
};

/**
 * Get the route configuration for a given pathname
 * Matches exact paths first, then checks for prefix matches
 */
export function getRouteConfig(pathname: string): RouteConfig {
  // Check for exact match first
  if (routeConfigs[pathname]) {
    return routeConfigs[pathname];
  }

  // Check for prefix matches (e.g., "/editor/something" matches "/editor")
  for (const [path, config] of Object.entries(routeConfigs)) {
    if (pathname.startsWith(path + "/")) {
      return config;
    }
  }

  // Default config - show sidebar
  return {};
}
