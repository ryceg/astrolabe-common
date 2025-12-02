"use client";

import { useForgotPasswordPage } from "@astroapps/client-localusers";
import { useNavigationService } from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { useState } from "react";
import { config } from "../../config";

export default function ForgotPasswordPage() {
  const [submitted, setSubmitted] = useState(false);
  const { Link } = useNavigationService();

  const { control, requestResetPassword } = useForgotPasswordPage(
    async (email: string) => {
      const response = await fetch(
        `${config.apiUrl}/api/users/forgotPassword?email=${encodeURIComponent(email)}`,
        {
          method: "POST",
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
    const success = await requestResetPassword();
    if (success) {
      setSubmitted(true);
    }
  };

  if (submitted) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8 text-center">
          <div>
            <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
              Check your email
            </h2>
            <p className="mt-2 text-sm text-gray-600">
              If an account exists with that email address, we've sent password
              reset instructions.
            </p>
          </div>
          <Link
            href="/login"
            className="font-medium text-indigo-600 hover:text-indigo-500"
          >
            Return to login
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Reset your password
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            Enter your email address and we'll send you a link to reset your
            password.
          </p>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div>
            <label htmlFor="email" className="sr-only">
              Email address
            </label>
            <Finput
              id="email"
              type="email"
              autoComplete="email"
              className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
              placeholder="Email address"
              control={fields.email}
            />
            {fields.email.error && (
              <p className="mt-1 text-sm text-red-600">{fields.email.error}</p>
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
              Send reset link
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
