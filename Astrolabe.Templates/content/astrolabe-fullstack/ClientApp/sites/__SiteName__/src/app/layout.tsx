"use client";

import "./globals.css";
import "react-quill-new/dist/quill.snow.css";
import { useNextNavigationService } from "@astroapps/client-nextjs";
import { AppContextProvider } from "@astroapps/client";
import { useControlTokenSecurity } from "@astroapps/client";
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

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const navigation = useNextNavigationService();
  const security = useControlTokenSecurity();
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
          <body className="h-screen">{children}</body>
        </AuthWrapper>
      </AppContextProvider>
    </html>
  );
}
