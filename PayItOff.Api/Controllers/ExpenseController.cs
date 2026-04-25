using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public ExpenseController(IExpenseService expenseService) { _expenseService = expenseService; }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateExpenseBatchRequest request)
    {
        await _expenseService.CreateExpenseBatch(GetUserId(), request);
        return Ok();
    }

}