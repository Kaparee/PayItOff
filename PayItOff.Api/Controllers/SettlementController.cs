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
    public async Task<ActionResult<GlobalSettlementResponse>> GetAllUserIncomeSummaries()
    {
        var result = await _settlementService.GetUserAllIncomesSummaryAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("get-user-expenses-summ")]
    public async Task<ActionResult<GlobalSettlementResponse>> GetAllUserExpenseSummaries()
    {
        var result = await _settlementService.GetUserAllExpensesSummaryAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("get-user-expense-history")]
    public async Task<ActionResult<PagedTransactionResponse>> GetHistory([FromQuery] UserExpenseHistoryRequest request)
    {
        var result = await _settlementService.GetHistoryAsync(GetUserId(), request);
        return Ok(result);
    }

    [HttpGet("current-debt")]
    public async Task<ActionResult<decimal>> GetCurrentDebt([FromQuery] int? targetId)
    {
        var result = await _settlementService.GetUserCurrentTotalDebtAsync(GetUserId(), targetId);
        return Ok(result);
    }

    [HttpGet("payable-options")]
    public async Task<ActionResult<List<PayableDebtOptionResponse>>> GetPayableOptions()
    {
        var result = await _settlementService.GetPayableDebtOptionsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateSettlementRequest request)
    {
        await _settlementService.CreateSettlementAsync(GetUserId(), request);
        return Ok();
    }

    [HttpPost("create-net-pay")]
    public async Task<ActionResult<PayNetDebtResponse>> CreateNetPay([FromBody] PayNetDebtRequest request)
    {
        var result = await _settlementService.CreateNetDebtSettlementsAsync(GetUserId(), request);
        return Ok(result);
    }

    [HttpPost("accept/{id}")]
    public async Task<IActionResult> Accept(int id)
    {
        await _settlementService.AcceptSettlementAsync(GetUserId(), id);
        return Ok();
    }

    [HttpPost("reject/{id}")]
    public async Task<IActionResult> Reject(int id)
    {
        await _settlementService.RejectSettlementAsync(GetUserId(), id);
        return Ok();
    }

    [HttpPost("accept-net/{senderId}")]
    public async Task<IActionResult> AcceptNet(int senderId)
    {
        await _settlementService.AcceptNetSettlementsAsync(GetUserId(), senderId);
        return Ok();
    }

    [HttpPost("reject-net/{senderId}")]
    public async Task<IActionResult> RejectNet(int senderId)
    {
        await _settlementService.RejectNetSettlementsAsync(GetUserId(), senderId);
        return Ok();
    }

    [HttpPost("remind-debt")]
    public async Task<IActionResult> RemindDebt([FromBody] RemindDebtRequest request)
    {
        await _settlementService.SendDebtReminderAsync(GetUserId(), request);
        return Ok();
    }

    [HttpPost("compensate")]
    public async Task<IActionResult> CompensateMutualDebts([FromBody] CompensateDebtsRequest request)
    {
        await _settlementService.CompensateMutualDebtsAsync(GetUserId(), request);
        return Ok();
    }
}