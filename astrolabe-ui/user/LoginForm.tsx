import { Textfield } from "../Textfield";
import {
  Control,
  Fcheckbox,
  useControl,
  useControlEffect,
} from "@react-typed-forms/core";
import { Button } from "../Button";
import clsx from "clsx";
import { LoginContainer } from "./LoginContainer";
import { LoginFormData } from "@astrolabe/client/app/user";

export function LoginForm({
  className,
  control,
  signupHref = "/signup",
  resetPasswordHref = "/resetPassword",
  authenticate,
}: {
  className?: string;
  signupHref?: string;
  resetPasswordHref?: string;
  control: Control<LoginFormData>;
  authenticate: () => void;
}) {
  const { password, username, rememberMe } = control.fields;
  const { error } = control;
  useControlEffect(
    () => [username.value, password.value],
    () => (control.error = null),
  );
  const linkStyle =
    "font-medium text-primary-600 hover:underline dark:text-primary-500";
  return (
    <LoginContainer className={className}>
      <h2>Login</h2>
      <div className="my-2 space-y-4">
        <div className="flex">
          <div>Do you have an account yet?</div>
          <a className={clsx("ml-1 ", linkStyle)} href={signupHref}>
            Signup
          </a>
        </div>
        <Textfield control={username} label="Username" />
        <Textfield control={password} label="Password" type="password" />
        <div className="flex justify-between text-sm">
          <div>
            <Fcheckbox control={rememberMe} /> <label>Remember me</label>
          </div>
          <div>
            <a href={resetPasswordHref} className={linkStyle}>
              Forgot your password?
            </a>
          </div>
        </div>
        {error && <p className="text-danger">{error}</p>}
        <Button className="w-full" onClick={authenticate}>
          Login
        </Button>
      </div>
    </LoginContainer>
  );
}
