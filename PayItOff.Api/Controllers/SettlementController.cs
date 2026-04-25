using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettlementController : ControllerBase
{
    private readonly ISettlementService _settlementService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public SettlementController(ISettlementService settlementService) { _settlementService = settlementService; }

    [HttpGet("get-user-incomes-summ")]
    public async Task<ActionResult<GlobalDebtSummaryResponse>> GetAllUserIncomeSummaries()
    {
        var result = await _settlementService.GetUserAllIncomesSummaryAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("get-user-expenses-summ")]
    public async Task<ActionResult<GlobalDebtSummaryResponse>> GetAllUserExpenseSummaries()
    {
        var result = await _settlementService.GetUserAllExpensesSummaryAsync(GetUserId());
        return Ok(result);
    }
}

