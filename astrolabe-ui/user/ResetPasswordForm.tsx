import { Control, useControl } from "@react-typed-forms/core";
import { Textfield } from "../Textfield";
import { Button } from "../Button";
import { LoginContainer } from "./LoginContainer";
import { ResetPasswordFormData } from "@astrolabe/client/app/user";
import { CircularProgress } from "../CircularProgress";

export function ResetPasswordForm({
  className,
  control,
  resetPassword,
}: {
  className?: string;
  control: Control<ResetPasswordFormData>;
  resetPassword: () => Promise<boolean>;
}) {
  const {
    fields: { email },
    disabled,
  } = control;
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
          <form
            className="space-y-4 md:space-y-6"
            onSubmit={(e) => {
              e.preventDefault();
              doReset();
            }}
          >
            <Textfield control={email} label="Email" autoComplete="username" />
            {disabled && <CircularProgress />}
            <Button className="w-full" type="submit" disabled={disabled}>
              Reset Password
            </Button>
          </form>
        </>
      )}
    </LoginContainer>
  );
  async function doReset() {
    control.disabled = true;
    const wasReset = await resetPassword();
    hasBeenReset.value = wasReset;
    if (!wasReset) control.disabled = false;
  }
}
