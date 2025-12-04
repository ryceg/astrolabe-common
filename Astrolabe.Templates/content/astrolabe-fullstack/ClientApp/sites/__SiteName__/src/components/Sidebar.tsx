"use client";

import { useNavigationService, useSecurityService } from "@astroapps/client";

interface SidebarProps {
  isOpen?: boolean;
  onClose?: () => void;
}

export function Sidebar({ isOpen = true, onClose }: SidebarProps) {
  const { Link } = useNavigationService();
  const security = useSecurityService();
  const user = security.currentUser.value;

  const navItems = [
    { href: "/", label: "Home", icon: "fa-home" },
    //#if (IncludeDemoData)
    { href: "/tea", label: "Tea Manager", icon: "fa-mug-hot" },
    { href: "/search", label: "Tea Search", icon: "fa-search" },
    { href: "/form-demo", label: "Form Demo", icon: "fa-file-alt" },
    //#endif
    //#if (IncludeOrleans)
    { href: "/tearoom", label: "Tea Room", icon: "fa-coffee" },
    //#endif
    { href: "/editor", label: "Schema Editor", icon: "fa-edit" },
  ];

  return (
    <aside
      className={`fixed inset-y-0 left-0 z-50 w-64 bg-white border-r border-gray-200 transform transition-transform duration-300 ease-in-out lg:relative lg:translate-x-0 ${
        isOpen ? "translate-x-0" : "-translate-x-full"
      }`}
    >
      <div className="flex flex-col h-full">
        {/* Logo/Brand */}
        <div className="flex items-center justify-between h-16 px-6 border-b border-gray-200">
          <Link href="/" className="text-xl font-bold text-gray-900">
            AstrolabeApp
          </Link>
          {onClose && (
            <button
              onClick={onClose}
              className="lg:hidden p-2 text-gray-500 hover:text-gray-700"
            >
              <i className="fa fa-times" />
            </button>
          )}
        </div>

        {/* Navigation */}
        <nav className="flex-1 px-4 py-4 overflow-y-auto">
          <ul className="space-y-1">
            {navItems.map((item) => (
              <li key={item.href}>
                <Link
                  href={item.href}
                  className="flex items-center px-4 py-2.5 text-gray-700 rounded-lg hover:bg-gray-100 hover:text-gray-900 transition-colors"
                >
                  <i className={`fa ${item.icon} w-5 text-gray-400`} />
                  <span className="ml-3">{item.label}</span>
                </Link>
              </li>
            ))}
          </ul>
        </nav>

        {/* User Section */}
        <div className="border-t border-gray-200 p-4">
          {user.busy ? (
            <div className="flex items-center justify-center py-2">
              <i className="fa fa-spinner fa-spin text-gray-400" />
            </div>
          ) : user.loggedIn ? (
            <div className="space-y-3">
              <div className="flex items-center px-2">
                <div className="flex-shrink-0">
                  <div className="w-10 h-10 rounded-full bg-indigo-100 flex items-center justify-center">
                    <i className="fa fa-user text-indigo-600" />
                  </div>
                </div>
                <div className="ml-3 overflow-hidden">
                  <p className="text-sm font-medium text-gray-900 truncate">
                    {user.name ?? user.email ?? "User"}
                  </p>
                  {user.email && user.name && (
                    <p className="text-xs text-gray-500 truncate">{user.email}</p>
                  )}
                </div>
              </div>
              <div className="flex gap-2">
                <Link
                  href="/logout"
                  className="flex-1 flex items-center justify-center px-3 py-2 text-sm text-gray-700 bg-gray-100 rounded-lg hover:bg-gray-200 transition-colors"
                >
                  <i className="fa fa-sign-out-alt mr-2" />
                  Log out
                </Link>
              </div>
            </div>
          ) : (
            <div className="space-y-2">
              <Link
                href="/login"
                className="flex items-center justify-center w-full px-4 py-2 text-sm font-medium text-white bg-indigo-600 rounded-lg hover:bg-indigo-700 transition-colors"
              >
                <i className="fa fa-sign-in-alt mr-2" />
                Log in
              </Link>
              <Link
                href="/signup"
                className="flex items-center justify-center w-full px-4 py-2 text-sm font-medium text-indigo-600 bg-indigo-50 rounded-lg hover:bg-indigo-100 transition-colors"
              >
                <i className="fa fa-user-plus mr-2" />
                Sign up
              </Link>
            </div>
          )}
        </div>
      </div>
    </aside>
  );
}

export function MobileSidebarToggle({ onClick }: { onClick: () => void }) {
  return (
    <button
      onClick={onClick}
      className="lg:hidden fixed top-4 left-4 z-40 p-2 bg-white rounded-lg shadow-md border border-gray-200"
    >
      <i className="fa fa-bars text-gray-600" />
    </button>
  );
}
