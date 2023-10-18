"use client";

import { Textfield } from "../Textfield";
import { Fcheckbox, useControl } from "@react-typed-forms/core";
import { Button } from "../Button";

interface LoginFormData {
  username: string;
  password: string;
  rememberMe: boolean;
}

export function LoginForm({
  className,
  signupHref = "/signup",
  resetPasswordHref = "/resetPassword",
}: {
  className?: string;
  signupHref?: string;
  resetPasswordHref?: string;
}) {
  const form = useControl<LoginFormData>({
    password: "",
    username: "",
    rememberMe: false,
  });
  const { password, username, rememberMe } = form.fields;
  return (
    <div className={className}>
      <h2>Login</h2>
      <div className="my-2 space-y-4">
        <div className="flex">
          <div>Do you have an account yet?</div>
          <a
            className="ml-1 font-medium text-primary-600 hover:underline dark:text-primary-500"
            href={signupHref}
          >
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
            <a
              href={resetPasswordHref}
              className="font-medium text-primary-600 hover:underline dark:text-primary-500"
            >
              Forgot your password?
            </a>
          </div>
        </div>
        <Button className="w-full">Login</Button>
      </div>
    </div>
  );
}
