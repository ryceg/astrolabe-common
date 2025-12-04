"use client";

import { useVerifyPage } from "@astroapps/client-localusers";
import {
  useNavigationService,
  useSecurityService,
  useApiClient,
  TokenSecurityService,
} from "@astroapps/client";
import { Finput, useControl } from "@react-typed-forms/core";
import { UsersClient } from "client-common";
import { useSearchParams } from "next/navigation";
import { useState } from "react";

export default function VerifyPage() {
  const { push, Link } = useNavigationService();
  const security = useSecurityService<TokenSecurityService>();
  const usersClient = useApiClient(UsersClient);
  const searchParams = useSearchParams();

  // Check if we have a verification code in the URL
  const hasVerificationCode = searchParams.has("verificationCode");
  const emailFromUrl = searchParams.get("email") || "";

  // Manual entry form state
  const manualCodeControl = useControl("");
  const [manualError, setManualError] = useState<string | null>(null);
  const [isVerifying, setIsVerifying] = useState(false);
  const [isVerified, setIsVerified] = useState(false);

  const { control, mfaControl, authenticate, send } = useVerifyPage(
    async (verificationCode: string) => {
      const token = await usersClient.verifyAccount(verificationCode);
      return { token, requiresMfa: false };
    },
    async (mfaData) => {
      const token = await usersClient.mfaVerifyAccount({
        token: mfaData.token,
        code: mfaData.code,
        number: mfaData.number,
      });
      await security.setToken(token);
    },
    async (mfaData) => {
      await usersClient.sendMfaCode({
        token: mfaData.token,
        updateNumber: mfaData.updateNumber,
        number: mfaData.number,
      });
    }
  );

  const handleManualVerify = async (e: React.FormEvent) => {
    e.preventDefault();
    const code = manualCodeControl.value.trim();
    if (!code) {
      setManualError("Please enter a verification code");
      return;
    }

    setIsVerifying(true);
    setManualError(null);

    try {
      await usersClient.verifyAccount(code);
      setIsVerified(true);
    } catch (error: any) {
      if (error.status === 401) {
        setManualError("Invalid verification code. Please check and try again.");
      } else {
        setManualError("Verification failed. Please try again.");
      }
    } finally {
      setIsVerifying(false);
    }
  };

  const handleMfaSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    const success = await authenticate();
    if (success) {
      push("/");
    }
  };

  // Show success after manual verification
  if (isVerified) {
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
            className="inline-block px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700"
          >
            Continue to login
          </Link>
        </div>
      </div>
    );
  }

  // Show manual entry form if no verification code in URL
  if (!hasVerificationCode) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <div>
            <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
              Verify Your Email
            </h2>
            <p className="mt-2 text-center text-sm text-gray-600">
              {emailFromUrl
                ? `Enter the verification code sent to ${emailFromUrl}`
                : "Enter the verification code from your email"}
            </p>
            {process.env.NODE_ENV === "development" && (
              <p className="mt-2 text-center text-xs text-amber-600 bg-amber-50 p-2 rounded">
                💡 Dev mode: Check the backend console for the verification code
              </p>
            )}
          </div>
          <form className="mt-8 space-y-6" onSubmit={handleManualVerify}>
            <div>
              <label htmlFor="verificationCode" className="block text-sm font-medium text-gray-700">
                Verification Code
              </label>
              <Finput
                id="verificationCode"
                type="text"
                className="mt-1 appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm"
                placeholder="Enter verification code"
                control={manualCodeControl}
              />
            </div>

            {manualError && (
              <div className="text-sm text-red-600 text-center">{manualError}</div>
            )}

            <button
              type="submit"
              disabled={isVerifying}
              className="w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 disabled:opacity-50 disabled:cursor-not-allowed"
            >
              {isVerifying ? "Verifying..." : "Verify Email"}
            </button>

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

  // Original flow when verification code is in URL
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
            className="font-medium text-primary-600 hover:text-primary-500"
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
            className="inline-block px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700"
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
                className="appearance-none relative block w-full px-3 py-2 border border-gray-300 placeholder-gray-500 text-gray-900 rounded-md focus:outline-none focus:ring-primary-500 focus:border-primary-500 sm:text-sm text-center text-2xl tracking-widest"
                placeholder="000000"
                control={mfaControl.fields.code}
                maxLength={6}
              />
            </div>

            <button
              type="submit"
              className="w-full flex justify-center py-2 px-4 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700"
            >
              Verify
            </button>

            <button
              type="button"
              onClick={() => send()}
              className="w-full text-center font-medium text-primary-600 hover:text-primary-500"
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
