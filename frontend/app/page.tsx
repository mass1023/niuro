import LoanApplicationForm from "./loan-application-form";
import styles from "./page.module.css";

export default function Home() {
  return (
    <main className={styles.page}>
      <div className={styles.card}>
        <header className={styles.header}>
          <h1>Apply for a loan</h1>
          <p>Fill in your details and you&apos;ll get a decision right away.</p>
        </header>
        <LoanApplicationForm />
      </div>
    </main>
  );
}
