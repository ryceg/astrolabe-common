"use client";

import { useResetPasswordPage } from "@astroapps/client-localusers";
import { useNavigationService } from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { config } from "../../config";

export default function ResetPasswordPage() {
  const { push, Link } = useNavigationService();

  const { control, resetPassword } = useResetPasswordPage(
    async (resetCode: string, passwordData) => {
      const response = await fetch(
        `${config.apiUrl}/api/users/resetPassword?resetCode=${encodeURIComponent(resetCode)}`,
        {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({
            password: passwordData.password,
            confirm: passwordData.confirm,
          }),
        }
      );

      if (!response.ok) {
        throw response;
      }

      return response.json();
    }
  );

  const { fields } = control;

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const success = await resetPassword();
    if (success) {
      push("/login?message=Password reset successful! Please sign in.");
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Reset your password
          </h2>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div>
              <label
                htmlFor="password"
                className="block text-sm font-medium text-gray-700"
              >
                New Password
              </label>
              <Finput
                id="password"
                type="password"
                autoComplete="new-password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                control={fields.password}
              />
              {fields.password.error && (
                <p className="mt-1 text-sm text-red-600">
                  {fields.password.error}
                </p>
              )}
            </div>

            <div>
              <label
                htmlFor="confirm"
                className="block text-sm font-medium text-gray-700"
              >
                Confirm Password
              </label>
              <Finput
                id="confirm"
                type="password"
                autoComplete="new-password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                control={fields.confirm}
              />
              {fields.confirm.error && (
                <p className="mt-1 text-sm text-red-600">
                  {fields.confirm.error}
                </p>
              )}
            </div>
          </div>

          {control.error && (
            <p className="text-sm text-red-600 text-center">{control.error}</p>
          )}

          <div>
            <button
              type="submit"
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-indigo-600 hover:bg-indigo-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-indigo-500"
            >
              Reset Password
            </button>
          </div>

          <div className="text-center">
            <Link
              href="/login"
              className="font-medium text-indigo-600 hover:text-indigo-500"
            >
              Back to login
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
