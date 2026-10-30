"use client";

import { useState, type FormEvent } from "react";
import { submitLoanApplication, type LoanApplication, type SubmitResult } from "@/lib/loan-api";
import { US_STATES } from "@/lib/us-states";
import styles from "./loan-application-form.module.css";

export default function LoanApplicationForm() {
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [result, setResult] = useState<SubmitResult | null>(null);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsSubmitting(true);
    setResult(null);

    const form = new FormData(event.currentTarget);
    const application: LoanApplication = {
      firstName: String(form.get("firstName")),
      lastName: String(form.get("lastName")),
      street: String(form.get("street")),
      city: String(form.get("city")),
      state: String(form.get("state")),
      zipCode: String(form.get("zipCode")),
      companyName: String(form.get("companyName")),
      ssn: String(form.get("ssn")),
      requestedAmount: Number(form.get("requestedAmount")),
    };

    setResult(await submitLoanApplication(application));
    setIsSubmitting(false);
  }

  const fieldErrors = result?.kind === "invalid" ? result.fieldErrors : {};

  return (
    <form className={styles.form} onSubmit={handleSubmit}>
      <fieldset className={styles.section}>
        <legend>Applicant</legend>
        <div className={styles.row}>
          <Field name="firstName" label="First name" error={fieldErrors.firstName} autoComplete="given-name" maxLength={100} />
          <Field name="lastName" label="Last name" error={fieldErrors.lastName} autoComplete="family-name" maxLength={100} />
        </div>
        <Field name="companyName" label="Company" error={fieldErrors.companyName} autoComplete="organization" maxLength={200} />
        <Field
          name="ssn"
          label="Social Security number"
          error={fieldErrors.ssn}
          placeholder="123-45-6789"
          inputMode="numeric"
          autoComplete="off"
          pattern="\d{3}-?\d{2}-?\d{4}"
          title="9 digits, e.g. 123-45-6789"
          maxLength={11}
        />
      </fieldset>

      <fieldset className={styles.section}>
        <legend>Address</legend>
        <Field name="street" label="Street" error={fieldErrors.street} autoComplete="address-line1" maxLength={200} />
        <div className={styles.row}>
          <Field name="city" label="City" error={fieldErrors.city} autoComplete="address-level2" maxLength={100} />
          <label className={styles.field}>
            <span>State</span>
            <select name="state" required defaultValue="" aria-invalid={Boolean(fieldErrors.state)}>
              <option value="" disabled>
                Select…
              </option>
              {US_STATES.map((state) => (
                <option key={state.code} value={state.code}>
                  {state.name}
                </option>
              ))}
            </select>
            {fieldErrors.state && <small className={styles.error}>{fieldErrors.state}</small>}
          </label>
          <Field
            name="zipCode"
            label="ZIP"
            error={fieldErrors.zipCode}
            inputMode="numeric"
            autoComplete="postal-code"
            pattern="\d{5}"
            title="5 digits"
            maxLength={5}
          />
        </div>
      </fieldset>

      <fieldset className={styles.section}>
        <legend>Loan</legend>
        <Field
          name="requestedAmount"
          label="Requested amount (USD)"
          error={fieldErrors.requestedAmount}
          type="number"
          inputMode="decimal"
          min={1}
          step="0.01"
          placeholder="10000"
        />
      </fieldset>

      <button className={styles.submit} type="submit" disabled={isSubmitting}>
        {isSubmitting ? "Submitting…" : "Submit application"}
      </button>

      <ResultMessage result={result} />
    </form>
  );
}

type FieldProps = React.InputHTMLAttributes<HTMLInputElement> & {
  name: string;
  label: string;
  error?: string;
};

function Field({ label, error, ...inputProps }: FieldProps) {
  return (
    <label className={styles.field}>
      <span>{label}</span>
      <input required aria-invalid={Boolean(error)} {...inputProps} />
      {error && <small className={styles.error}>{error}</small>}
    </label>
  );
}

function ResultMessage({ result }: { result: SubmitResult | null }) {
  if (!result || result.kind === "invalid") return null;

  if (result.kind === "error") {
    return (
      <p role="alert" className={`${styles.result} ${styles.failure}`}>
        {result.message}
      </p>
    );
  }

  if (result.approved) {
    return (
      <div role="status" className={`${styles.result} ${styles.approved}`}>
        <strong>Approved</strong>
        <p>Your loan application has been approved. We&apos;ll be in touch with the next steps.</p>
      </div>
    );
  }

  return (
    <div role="status" className={`${styles.result} ${styles.denied}`}>
      <strong>Not approved</strong>
      <p>We can&apos;t approve this application:</p>
      <ul>
        {result.denialReasons.map((reason) => (
          <li key={reason}>{reason}</li>
        ))}
      </ul>
    </div>
  );
}
