"use client";

import { useSignupPage, SignupFormData } from "@astroapps/client-localusers";
import { useNavigationService } from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { config } from "../../config";

interface ExtendedSignupForm extends SignupFormData {
  firstName: string;
  lastName: string;
}

const emptyExtendedSignupForm: ExtendedSignupForm = {
  email: "",
  password: "",
  confirm: "",
  firstName: "",
  lastName: "",
};

export default function SignupPage() {
  const { push, Link } = useNavigationService();

  const { control, createAccount } = useSignupPage(
    emptyExtendedSignupForm,
    async (signupData: ExtendedSignupForm) => {
      const response = await fetch(`${config.apiUrl}/api/users/create`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(signupData),
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
    const success = await createAccount();
    if (success) {
      push("/login?message=Account created! Please check your email to verify.");
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Create your account
          </h2>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label htmlFor="firstName" className="block text-sm font-medium text-gray-700">
                  First name
                </label>
                <Finput
                  id="firstName"
                  type="text"
                  autoComplete="given-name"
                  className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                  control={fields.firstName}
                />
              </div>
              <div>
                <label htmlFor="lastName" className="block text-sm font-medium text-gray-700">
                  Last name
                </label>
                <Finput
                  id="lastName"
                  type="text"
                  autoComplete="family-name"
                  className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                  control={fields.lastName}
                />
              </div>
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700">
                Email address
              </label>
              <Finput
                id="email"
                type="email"
                autoComplete="email"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                control={fields.email}
              />
              {fields.email.error && (
                <p className="mt-1 text-sm text-red-600">{fields.email.error}</p>
              )}
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700">
                Password
              </label>
              <Finput
                id="password"
                type="password"
                autoComplete="new-password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                control={fields.password}
              />
              {fields.password.error && (
                <p className="mt-1 text-sm text-red-600">{fields.password.error}</p>
              )}
            </div>

            <div>
              <label htmlFor="confirm" className="block text-sm font-medium text-gray-700">
                Confirm password
              </label>
              <Finput
                id="confirm"
                type="password"
                autoComplete="new-password"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-indigo-500 focus:border-indigo-500 sm:text-sm"
                control={fields.confirm}
              />
              {fields.confirm.error && (
                <p className="mt-1 text-sm text-red-600">{fields.confirm.error}</p>
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
              Create account
            </button>
          </div>

          <div className="text-center">
            <Link
              href="/login"
              className="font-medium text-indigo-600 hover:text-indigo-500"
            >
              Already have an account? Sign in
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
