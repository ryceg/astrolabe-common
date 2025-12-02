"use client";

import { useMfaPage, MfaFormData } from "@astroapps/client-localusers";
import { useNavigationService, useSecurityService } from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { config } from "../../config";

export default function MfaPage() {
  const { push } = useNavigationService();
  const security = useSecurityService();

  const { control, authenticate, send } = useMfaPage(
    async (mfaData: MfaFormData) => {
      const response = await fetch(`${config.apiUrl}/api/users/mfaAuthenticate`, {
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
    async (mfaData: MfaFormData) => {
      const response = await fetch(`${config.apiUrl}/api/users/mfaCode/authenticate`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ token: mfaData.token }),
      });

      if (!response.ok) {
        throw response;
      }

      return response.json();
    }
  );

  const { fields } = control;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const success = await authenticate();
    if (success) {
      push("/");
    }
  };

  const handleResendCode = async () => {
    const success = await send();
    if (success) {
      alert("New code sent!");
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Two-Factor Authentication
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Please enter the verification code sent to your phone.
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div>
            <label htmlFor="code" className="sr-only">
              Verification Code
            </label>
            <Finput
              id="code"
              type="text"
              autoComplete="one-time-code"
              className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm text-center text-2xl tracking-widest"
              placeholder="000000"
              control={fields.code}
              maxLength={6}
            />
            {fields.code.error && (
              <p className="mt-1 text-sm text-red-600 text-center">
                {fields.code.error}
              </p>
            )}
          </div>

          {control.error && (
            <p className="text-sm text-red-600 text-center">{control.error}</p>
          )}

          <div>
            <button
              type="submit"
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              Verify
            </button>
          </div>

          <div className="text-center">
            <button
              type="button"
              onClick={handleResendCode}
              className="font-medium text-indigo-600 hover:text-indigo-500"
            >
              Resend code
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
