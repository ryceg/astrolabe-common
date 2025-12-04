"use client";

import { useResetPasswordPage } from "@astroapps/client-localusers";
import { useNavigationService, useApiClient } from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { UsersClient } from "client-common";

export default function ResetPasswordPage() {
  const { push, Link } = useNavigationService();
  const usersClient = useApiClient(UsersClient);

  const { control, resetPassword } = useResetPasswordPage(
    async (resetCode: string, passwordData) => {
      await usersClient.resetPassword(resetCode, {
        password: passwordData.password,
        confirm: passwordData.confirm,
      });
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
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
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
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
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
              className="group relative w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500"
            >
              Reset Password
            </button>
          </div>

          <div className="text-center">
            <Link
              href="/login"
              className="font-medium text-primary-600 hover:text-primary-500"
            >
              Back to login
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
