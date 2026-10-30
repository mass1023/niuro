using Loans.Api.Contracts;
using Loans.Application.SubmitLoanApplication;
using Microsoft.AspNetCore.Mvc;

namespace Loans.Api.Controllers;

[ApiController]
[Route("loan-applications")]
public sealed class LoanApplicationsController(SubmitLoanApplicationHandler handler) : ControllerBase
{
    /// <summary>
    /// Evaluates a loan application. A denial is a valid business outcome, not an error:
    /// both approved and denied return 200 with the decision.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<LoanDecisionResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDecisionResponse>> Submit(
        SubmitLoanApplicationRequest request,
        CancellationToken cancellationToken)
    {
        var decision = await handler.HandleAsync(request.ToCommand(), cancellationToken);
        return Ok(LoanDecisionResponse.From(decision));
    }
}
