"use client";

import "./globals.css";
import "react-quill/dist/quill.snow.css";
import { useNextNavigationService } from "@astroapps/client-nextjs";
import { AppContextProvider } from "@astroapps/client/service";

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const navigation = useNextNavigationService();
  return (
    <html lang="en">
      <AppContextProvider value={{ navigation }}>
        <body className="h-screen">{children}</body>
      </AppContextProvider>
    </html>
  );
}
