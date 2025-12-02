"use client";

import { useSecurityService, useNavigationService } from "@astroapps/client";
import { useEffect } from "react";

export default function LogoutPage() {
  const { push } = useNavigationService();
  const security = useSecurityService();

  useEffect(() => {
    // Clear the security state for local users
    security.currentUser.value = {
      loggedIn: false,
      accessToken: undefined,
    };
    push("/login");
  }, []);

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50">
      <p className="text-gray-600">Logging out...</p>
    </div>
  );
}
