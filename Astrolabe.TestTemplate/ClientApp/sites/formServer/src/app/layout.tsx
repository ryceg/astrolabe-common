"use client";

import "./globals.css";
import "react-quill/dist/quill.snow.css";
import { useNextNavigationService } from "@astroapps/client-nextjs";
import { AppContextProvider } from "@astroapps/client/service";
import { HTML5Backend } from "react-dnd-html5-backend";
import { DndProvider } from "react-dnd";

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const navigation = useNextNavigationService();
  return (
    <html lang="en">
      <DndProvider backend={HTML5Backend}>
        <AppContextProvider value={{ navigation }}>
          <body className="h-screen">{children}</body>
        </AppContextProvider>
      </DndProvider>
    </html>
  );
}
