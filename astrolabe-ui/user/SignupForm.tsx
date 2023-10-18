"use client";
import { useControl } from "@react-typed-forms/core";
import { Textfield } from "../Textfield";
import clsx from "clsx";
import { Button } from "../Button";

interface SignupFormData {
  email: string;
  password: string;
  confirm: string;
}

export function SignupForm({
  className,
  loginHref = "/login",
}: {
  className?: string;
  loginHref?: string;
}) {
  const form = useControl<SignupFormData>({
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
        <h2>Create an account</h2>
        <form className="space-y-4 md:space-y-6" action="#">
          <Textfield control={email} label="Email" />
          <Textfield control={password} label="Password" type="password" />
          <Textfield
            control={confirm}
            label="Confirm Password"
            type="password"
          />
          <Button className="w-full">Create an account</Button>
          <p className="text-sm font-light text-gray-500 dark:text-gray-400">
            Already have an account?{" "}
            <a
              href={loginHref}
              className="font-medium text-primary-600 hover:underline dark:text-primary-500"
            >
              Login here
            </a>
          </p>
        </form>
      </div>
    </div>
  );
}
