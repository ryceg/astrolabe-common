"use client";

import "./globals.css";
import "react-quill-new/dist/quill.snow.css";
import { useNextNavigationService } from "@astroapps/client-nextjs";
import { AppContextProvider } from "@astroapps/client";
import { useControlTokenSecurity } from "@astroapps/client";
import { usePathname } from "next/navigation";
import { config } from "../config";
import { getRouteConfig } from "./routes";
import { MainLayout } from "../components/MainLayout";
//#if (IncludeLocalUsers)
import { AuthPageSetupContext, defaultUserAuthPageSetup } from "@astroapps/client-localusers";

const authSetup = {
  ...defaultUserAuthPageSetup,
  hrefs: {
    login: "/login",
    signup: "/signup",
    forgotPassword: "/forgotPassword",
    resetPassword: "/resetPassword",
    mfa: "/mfa",
  },
};
//#endif

//#if (IncludeLocalUsers)
function AuthWrapper({ children }: { children: React.ReactNode }) {
  return (
    <AuthPageSetupContext.Provider value={authSetup}>
      {children}
    </AuthPageSetupContext.Provider>
  );
}
//#else
function AuthWrapper({ children }: { children: React.ReactNode }) {
  return <>{children}</>;
}
//#endif

function LayoutContent({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const routeConfig = getRouteConfig(pathname);

  // If sidebar should be hidden, render children directly
  if (routeConfig.hideSidebar) {
    return <>{children}</>;
  }

  // Otherwise, wrap in MainLayout with sidebar
  return <MainLayout>{children}</MainLayout>;
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const navigation = useNextNavigationService();
  const security = useControlTokenSecurity();
  // Set the base API URL for the security service
  security.baseApiUrl = config.apiUrl;

  return (
    <html lang="en">
      <head>
        <link
          rel="stylesheet"
          href="https://kit.fontawesome.com/95cc77b353.css"
          crossOrigin="anonymous"
        />
      </head>
      <AppContextProvider value={{ navigation, security }}>
        <AuthWrapper>
          <body className="h-screen">
            <LayoutContent>{children}</LayoutContent>
          </body>
        </AuthWrapper>
      </AppContextProvider>
    </html>
  );
}
