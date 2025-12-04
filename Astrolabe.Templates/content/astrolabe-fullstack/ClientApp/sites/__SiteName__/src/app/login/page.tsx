"use client";

import { useLoginPage, LoginFormData } from "@astroapps/client-localusers";
import {
  useSecurityService,
  useNavigationService,
  useApiClient,
  TokenSecurityService,
} from "@astroapps/client";
import { Finput } from "@react-typed-forms/core";
import { UsersClient } from "client-common";

export default function LoginPage() {
  const { push, Link } = useNavigationService();
  const security = useSecurityService<TokenSecurityService>();
  const usersClient = useApiClient(UsersClient);

  const { control, authenticate } = useLoginPage(
    async (loginData: LoginFormData) => {
      const token = await usersClient.authenticate({
        username: loginData.username,
        password: loginData.password,
        rememberMe: loginData.rememberMe,
      });

      // Check if MFA is required
      if (token.startsWith("mfa:")) {
        push(`/mfa?token=${encodeURIComponent(token.substring(4))}`);
        return;
      }

      // Update security service with logged-in user
      await security.setToken(token);
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

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            Sign in to your account
          </h2>
        </div>
        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          <div className="rounded-md shadow-sm -space-y-px">
            <div>
              <label htmlFor="email" className="sr-only">
                Email address
              </label>
              <Finput
                id="email"
                type="email"
                autoComplete="email"
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-t-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 focus:z-10 sm:text-sm"
                placeholder="Email address"
                control={fields.username}
              />
              {fields.username.error && (
                <p className="mt-1 text-sm text-red-600">{fields.username.error}</p>
              )}
            </div>
            <div>
              <label htmlFor="password" className="sr-only">
                Password
              </label>
              <Finput
                id="password"
                type="password"
                autoComplete="current-password"
                className="appearance-none rounded-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-b-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 focus:z-10 sm:text-sm"
                placeholder="Password"
                control={fields.password}
              />
              {fields.password.error && (
                <p className="mt-1 text-sm text-red-600">{fields.password.error}</p>
              )}
            </div>
          </div>

          <div className="flex items-center justify-between">
            <div className="flex items-center">
              <Finput
                id="remember-me"
                type="checkbox"
                className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
                control={fields.rememberMe}
              />
              <label
                htmlFor="remember-me"
                className="ml-2 block text-sm text-gray-900"
              >
                Remember me
              </label>
            </div>

            <div className="text-sm">
              <Link
                href="/forgotPassword"
                className="font-medium text-primary-600 hover:text-primary-500"
              >
                Forgot your password?
              </Link>
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
              Sign in
            </button>
          </div>

          <div className="text-center">
            <Link
              href="/signup"
              className="font-medium text-primary-600 hover:text-primary-500"
            >
              Don't have an account? Sign up
            </Link>
          </div>
        </form>
      </div>
    </div>
  );
}
