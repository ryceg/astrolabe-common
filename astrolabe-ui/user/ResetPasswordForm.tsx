"use client";
import { useControl } from "@react-typed-forms/core";
import { Textfield } from "../Textfield";
import clsx from "clsx";
import { Button } from "../Button";

interface ResetPasswordFormData {
  email: string;
  password: string;
  confirm: string;
}

export function ResetPasswordForm({
  className,
  loginHref = "/login",
}: {
  className?: string;
  loginHref?: string;
}) {
  const form = useControl<ResetPasswordFormData>({
    password: "",
    confirm: "",
    email: "",
  });
  const { password, confirm, email } = form.fields;

  return (
    <div
      className={clsx(
        className,
        "w-full bg-white rounded-lg shadow dark:border md:mt-0 xl:p-0 dark:bg-gray-800 dark:border-gray-700",
      )}
    >
      <div className="p-6 space-y-4 md:space-y-6 sm:p-8">
        <h2>Forgot your password?</h2>
        <p className="font-light text-gray-500 dark:text-gray-400">
          Don't fret! Just type in your email and we will send you a code to
          reset your password!
        </p>
        <form className="space-y-4 md:space-y-6" action="#">
          <Textfield control={email} label="Email" />
          <Button className="w-full">Reset Password</Button>
        </form>
      </div>
    </div>
  );
}
