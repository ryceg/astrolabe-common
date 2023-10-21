import { Control, notEmpty, useControl } from "@react-typed-forms/core";
import { isApiResponse, validateAndRunResult } from "../util/validation";
import { useNavigationService } from "../service/navigation";
import { useEffect } from "react";
import { RouteData } from "./routeData";
import { PageSecurity } from "../service/security";

export interface LoginFormData {
  username: string;
  password: string;
  rememberMe: boolean;
}

export const emptyLoginForm: LoginFormData = {
  password: "",
  username: "",
  rememberMe: false,
};

export interface ResetPasswordFormData {
  email: string;
}

export const emptyResetPasswordForm = {
  email: "",
};

export interface SignupFormData {
  email: string;
  password: string;
  confirm: string;
}

export const emptySignupForm: SignupFormData = {
  password: "",
  confirm: "",
  email: "",
};

export interface ChangePasswordFormData {
  oldPassword: string;
  password: string;
  confirm: string;
}

export const emptyChangePasswordForm: ChangePasswordFormData = {
  password: "",
  confirm: "",
  oldPassword: "",
};

export interface PasswordChangeProps {
  control: Control<ChangePasswordFormData>;
  changePassword: () => Promise<boolean>;
  confirmPrevious: boolean;
}

export function useChangePasswordPage(
  runChange: (
    resetCode: string | null,
    change: ChangePasswordFormData,
  ) => Promise<any>,
): PasswordChangeProps {
  const control = useControl(emptyChangePasswordForm);

  const searchParams = useNavigationService().query;
  const resetCode = searchParams.get("resetCode");

  return {
    control,
    confirmPrevious: !resetCode,
    changePassword: () =>
      validateAndRunResult(control, () => runChange(resetCode, control.value)),
  };
}

export interface LoginProps {
  control: Control<LoginFormData>;
  authenticate: () => void;
}

export function useLoginPage(
  runAuthenticate: (login: LoginFormData) => Promise<any>,
): LoginProps {
  const control = useControl(emptyLoginForm, {
    fields: {
      username: { validator: notEmpty("Please enter your email address") },
      password: { validator: notEmpty("Please enter your password") },
    },
  });

  return {
    control,
    authenticate: () =>
      validateAndRunResult(
        control,
        () => runAuthenticate(control.value),
        (e) => {
          if (isApiResponse(e) && e.status === 401) {
            control.error = "Incorrect username/password";
            return true;
          } else return false;
        },
      ),
  };
}

export interface ResetPasswordProps {
  control: Control<ResetPasswordFormData>;
  resetPassword: () => Promise<any>;
}

export function useResetPasswordPage(
  runResetPassword: (email: string) => Promise<any>,
): ResetPasswordProps {
  const control = useControl(emptyResetPasswordForm, {
    fields: { email: { validator: notEmpty("Please enter your email") } },
  });

  return {
    control,
    resetPassword: () =>
      validateAndRunResult(control, () =>
        runResetPassword(control.fields.email.value),
      ),
  };
}

export interface SignupProps<A extends SignupFormData> {
  control: Control<A>;
  createAccount: () => Promise<boolean>;
}

export function useSignupPage<A extends SignupFormData = SignupFormData>(
  initialForm: A,
  runCreateAccount: (signupData: A) => Promise<any>,
): SignupProps<A> {
  const control = useControl(initialForm);

  return {
    control,
    createAccount: () =>
      validateAndRunResult(control, () => runCreateAccount(control.value)),
  };
}

export function useVerifyPage(runVerify: (code: string) => Promise<any>) {
  const searchParams = useNavigationService().query;
  const verificationCode = searchParams.get("verificationCode");

  useEffect(() => {
    if (verificationCode) {
      runVerify(verificationCode);
    }
  }, [verificationCode]);
}

export const defaultUserRoutes: Record<string, RouteData<PageSecurity>> = {
  login: { label: "Login", allowGuests: true, forwardAuthenticated: true },
  changePassword: { label: "Change password" },
  resetPassword: {
    label: "Reset password",
    allowGuests: true,
    forwardAuthenticated: true,
  },
  signup: {
    label: "Create account",
    allowGuests: true,
    forwardAuthenticated: true,
  },
  verify: { label: "Verify email", allowGuests: true },
};
