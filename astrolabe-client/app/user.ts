import { Control, notEmpty, useControl } from "@react-typed-forms/core";
import { isApiResponse, validateAndRunResult } from "../util/validation";
import { useNavigationService } from "../service/navigation";
import { createContext, useContext, useEffect } from "react";
import { RouteData } from "./routeData";
import { PageSecurity } from "../service/security";

export interface AuthPageSetup {
  hrefs: {
    login: string;
    signup: string;
    resetPassword: string;
    changePassword: string;
  };
  errors: {
    emptyEmail?: string;
    emptyUsername: string;
    emptyPassword: string;
    credentials: string;
    verify: string;
  };
  queryParams: {
    verifyCode: string;
    resetCode: string;
  };
}

export const defaultUserAuthPageSetup: AuthPageSetup = {
  hrefs: {
    login: "/login",
    signup: "/signup",
    resetPassword: "/resetPassword",
    changePassword: "/changePassword",
  },
  errors: {
    emptyUsername: "Please enter your email address",
    emptyPassword: "Please enter your password",
    credentials: "Incorrect username/password",
    verify: "You could not be verified",
  },
  queryParams: {
    verifyCode: "verificationCode",
    resetCode: "resetCode",
  },
};

export const AuthPageSetupContext = createContext(defaultUserAuthPageSetup);

export function useAuthPageSetup() {
  return useContext(AuthPageSetupContext);
}

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

  const {
    queryParams: { resetCode: rcp },
  } = useAuthPageSetup();
  const searchParams = useNavigationService();
  const resetCode = searchParams.get(rcp);

  return {
    control,
    confirmPrevious: !resetCode,
    changePassword: () =>
      validateAndRunResult(control, () => runChange(resetCode, control.value)),
  };
}

export interface LoginProps {
  control: Control<LoginFormData>;
  authenticate: () => Promise<boolean>;
}

export function useLoginPage(
  runAuthenticate: (login: LoginFormData) => Promise<any>,
): LoginProps {
  const {
    errors: { emptyUsername, emptyPassword, credentials },
  } = useAuthPageSetup();
  const control = useControl(emptyLoginForm, {
    fields: {
      username: { validator: notEmpty(emptyUsername) },
      password: { validator: notEmpty(emptyPassword) },
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
            control.error = credentials;
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
  const {
    errors: { emptyUsername, emptyEmail },
  } = useAuthPageSetup();
  const control = useControl(emptyResetPasswordForm, {
    fields: { email: { validator: notEmpty(emptyEmail ?? emptyUsername) } },
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

export function useVerifyPage(
  runVerify: (code: string) => Promise<unknown>,
): Control<unknown> {
  const {
    errors: { verify },
    queryParams: { verifyCode },
  } = useAuthPageSetup();

  const searchParams = useNavigationService();
  const verificationCode = searchParams.get(verifyCode);

  const control = useControl(undefined);

  useEffect(() => {
    doVerify();
  }, [verificationCode]);

  return control;

  async function doVerify() {
    if (verificationCode) {
      try {
        await runVerify(verificationCode);
      } catch (e) {
        if (isApiResponse(e) && e.status === 401) {
          control.error = verify;
        } else throw e;
      }
    } else {
      control.error = verify;
    }
  }
}

export const defaultUserRoutes = {
  login: { label: "Login", allowGuests: true, forwardAuthenticated: true },
  changePassword: { label: "Change password", allowGuests: true },
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
  verify: {
    label: "Verify email",
    allowGuests: true,
    forwardAuthenticated: true,
  },
} satisfies Record<string, RouteData<PageSecurity>>;
