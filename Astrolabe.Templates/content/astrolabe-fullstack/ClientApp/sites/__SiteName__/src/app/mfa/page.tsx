"use client";

import { useMfaPage, MfaFormData } from "@astroapps/client-localusers";
import {
  useNavigationService,
  useSecurityService,
  useApiClient,
  TokenSecurityService,
} from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { UsersClient } from "client-common";

export default function MfaPage() {
  const { push } = useNavigationService();
  const security = useSecurityService<TokenSecurityService>();
  const usersClient = useApiClient(UsersClient);

  const { control, authenticate, send } = useMfaPage(
    async (mfaData: MfaFormData) => {
      const token = await usersClient.mfaAuthenticate({
        token: mfaData.token,
        code: mfaData.code,
        number: mfaData.number,
      });

      await security.setToken(token);
    },
    async (mfaData: MfaFormData) => {
      await usersClient.sendMfaCode({
        token: mfaData.token,
        updateNumber: mfaData.updateNumber,
        number: mfaData.number,
      });
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
              className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm text-center text-2xl tracking-widest"
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
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
            >
              Verify
            </button>
          </div>

          <div className="text-center">
            <button
              type="button"
              onClick={handleResendCode}
              className="font-medium text-primary-600 hover:text-primary-500"
            >
              Resend code
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
