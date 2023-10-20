import { Control, useControl } from "@react-typed-forms/core";
import { Textfield } from "../Textfield";
import clsx from "clsx";
import { Button } from "../Button";
import { LoginContainer } from "./LoginContainer";

export interface ResetPasswordFormData {
  email: string;
}

export const emptyResetPasswordForm = {
  email: "",
};

export function ResetPasswordForm({
  className,
  control,
  resetPassword,
}: {
  className?: string;
  control: Control<ResetPasswordFormData>;
  resetPassword: () => Promise<boolean>;
}) {
  const { email } = control.fields;
  const hasBeenReset = useControl(false);
  return (
    <LoginContainer className={className}>
      {hasBeenReset.value ? (
        <>
          <h2>Check email to continue</h2>
          <p className="font-light text-gray-500 dark:text-gray-400">
            You will receive an email with furthers instructions on how to reset
            your password
          </p>
        </>
      ) : (
        <>
          <h2>Forgot your password?</h2>
          <p className="font-light text-gray-500 dark:text-gray-400">
            Don't fret! Just type in your email and we will send you a code to
            reset your password!
          </p>
          <div className="space-y-4 md:space-y-6">
            <Textfield control={email} label="Email" />
            <Button className="w-full" onClick={doReset}>
              Reset Password
            </Button>
          </div>
        </>
      )}
    </LoginContainer>
  );
  async function doReset() {
    hasBeenReset.value = await resetPassword();
  }
}
