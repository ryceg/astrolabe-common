"use client";

import { useVerifyPage } from "@astroapps/client-localusers";
import { useNavigationService, useSecurityService } from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { config } from "../../config";

export default function VerifyPage() {
  const { push, Link } = useNavigationService();
  const security = useSecurityService();

  const { control, mfaControl, authenticate, send } = useVerifyPage(
    async (verificationCode: string) => {
      const response = await fetch(`${config.apiUrl}/api/users/verify`, {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `code=${encodeURIComponent(verificationCode)}`,
      });

      if (!response.ok) {
        throw response;
      }

      const token = await response.text();
      return { token, requiresMfa: false };
    },
    async (mfaData) => {
      const response = await fetch(`${config.apiUrl}/api/users/mfaVerify`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(mfaData),
      });

      if (!response.ok) {
        throw response;
      }

      const token = await response.text();
      security.currentUser.value = {
        loggedIn: true,
        accessToken: token,
      };
    },
    async (mfaData) => {
      const response = await fetch(`${config.apiUrl}/api/users/mfaCode/authenticate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token: mfaData.token }),
      });

      if (!response.ok) {
        throw response;
      }
    }
  );

  const handleMfaSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const success = await authenticate();
    if (success) {
      push("/");
    }
  };

  if (control.error) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8 text-center">
          <div>
            <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
              Verification Failed
            </h2>
            <p className="mt-2 text-sm text-red-600">{control.error}</p>
          </div>
          <Link
            href="/signup"
            className="font-medium text-indigo-600 hover:text-indigo-500"
          >
            Try signing up again
          </Link>
        </div>
      </div>
    );
  }

  if (!control.value.requiresMfa && control.value.token) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8 text-center">
          <div>
            <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
              Email Verified!
            </h2>
            <p className="mt-2 text-sm text-gray-600">
              Your email has been verified successfully.
            </p>
          </div>
          <Link
            href="/login"
            className="inline-block px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
          >
            Continue to login
          </Link>
        </div>
      </div>
    );
  }

  // MFA required
  if (control.value.requiresMfa) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <div>
            <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
              Two-Factor Authentication
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600">
              Please enter the code sent to your phone.
            </p>
          </div>
          <form className="mt-8 space-y-6" onSubmit={handleMfaSubmit}>
            <div>
              <Finput
                type="text"
                className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm text-center text-2xl tracking-widest"
                placeholder="000000"
                control={mfaControl.fields.code}
                maxLength={6}
              />
            </div>

            <button
              type="submit"
              className="w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700"
            >
              Verify
            </button>

            <button
              type="button"
              onClick={() => send()}
              className="w-full text-center font-medium text-indigo-600 hover:text-indigo-500"
            >
              Resend Code
            </button>
          </form>
        </div>
      </div>
    );
  }

  // Loading state
  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8 text-center">
        <div>
          <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
            Verifying...
          </h2>
          <p className="mt-2 text-sm text-gray-600">
            Please wait while we verify your email.
          </p>
        </div>
      </div>
    </div>
  );
}
