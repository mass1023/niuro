// Talks to the .NET API. Kept out of the component so the UI only deals with plain results.

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5088";

export type LoanApplication = {
  firstName: string;
  lastName: string;
  street: string;
  city: string;
  state: string;
  zipCode: string;
  companyName: string;
  ssn: string;
  requestedAmount: number;
};

export type SubmitResult =
  | { kind: "decision"; approved: boolean; denialReasons: string[] }
  | { kind: "invalid"; fieldErrors: Record<string, string> }
  | { kind: "error"; message: string };

export async function submitLoanApplication(application: LoanApplication): Promise<SubmitResult> {
  let response: Response;
  try {
    response = await fetch(`${API_URL}/loan-applications`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(application),
    });
  } catch {
    return { kind: "error", message: "We couldn't reach the server. Please try again in a moment." };
  }

  if (response.ok) {
    const body = await response.json();
    return { kind: "decision", approved: body.approved, denialReasons: body.denialReasons };
  }

  if (response.status === 400) {
    const problem = await response.json();
    return { kind: "invalid", fieldErrors: toFieldErrors(problem.errors ?? {}) };
  }

  if (response.status === 409) {
    return { kind: "error", message: "This application was submitted twice at the same time. Please try again." };
  }

  return { kind: "error", message: "Something went wrong on our side. Please try again." };
}

// The API returns { "Ssn": ["..."] }; the form fields are camelCase ("ssn"). Keep the first message per field.
function toFieldErrors(errors: Record<string, string[]>): Record<string, string> {
  return Object.fromEntries(
    Object.entries(errors).map(([field, messages]) => [field.charAt(0).toLowerCase() + field.slice(1), messages[0]]),
  );
}
