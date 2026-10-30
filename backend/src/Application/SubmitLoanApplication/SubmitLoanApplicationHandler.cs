using Loans.Application.Abstractions;
using Loans.Application.Events;
using Loans.Domain;

namespace Loans.Application.SubmitLoanApplication;

public sealed class SubmitLoanApplicationHandler(
    LoanDecisionEngine decisionEngine,
    ICustomerRepository customers,
    ILoanApplicationRepository loanApplications,
    IEventOutbox outbox,
    IUnitOfWork unitOfWork)
{
    public async Task<LoanDecision> HandleAsync(SubmitLoanApplicationCommand command, CancellationToken cancellationToken = default)
    {
        var request = new LoanRequest(
            command.FirstName,
            command.LastName,
            Address.Create(command.Street, command.City, command.State, command.ZipCode),
            command.CompanyName,
            Ssn.Create(command.Ssn),
            command.RequestedAmount);

        var decision = decisionEngine.Evaluate(request);
        if (!decision.IsApproved)
            return decision;

        var customer = await UpsertCustomerAsync(request, cancellationToken);
        var application = await UpsertLoanApplicationAsync(customer, request.RequestedAmount, cancellationToken);

        // Customer, application and outbox row are committed together in one SaveChanges (one DB transaction).
        outbox.Enqueue(CustomerApplicationSaved.From(customer, application));
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return decision;
    }

    private async Task<Customer> UpsertCustomerAsync(LoanRequest request, CancellationToken cancellationToken)
    {
        var customer = await customers.FindBySsnAsync(request.Ssn, cancellationToken);

        if (customer is null)
        {
            customer = Customer.Create(request.FirstName, request.LastName, request.Address, request.CompanyName, request.Ssn);
            customers.Add(customer);
        }
        else
        {
            customer.UpdateDetails(request.FirstName, request.LastName, request.Address, request.CompanyName);
        }

        return customer;
    }

    private async Task<LoanApplication> UpsertLoanApplicationAsync(Customer customer, decimal requestedAmount, CancellationToken cancellationToken)
    {
        var application = await loanApplications.FindByCustomerIdAsync(customer.Id, cancellationToken);

        if (application is null)
        {
            application = LoanApplication.Create(customer.Id, requestedAmount);
            loanApplications.Add(application);
        }
        else
        {
            application.UpdateRequestedAmount(requestedAmount);
        }

        return application;
    }
}
