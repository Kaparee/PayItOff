using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Infrastructure.Services;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExpenseController : ControllerBase
{
    private readonly IExpenseService _expenseService;
    private readonly IFileService _fileService;
    private int GetUserId()
        => int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
           ?? throw new UnauthorizedAccessException());
    public ExpenseController(IExpenseService expenseService, IFileService fileService)
    {
        _expenseService = expenseService;
        _fileService = fileService;
    }

    [HttpPost("upload-receipt")]
    [EndpointSummary("Wrzucenie zdjęcia paragonu w tle")]
    [EndpointDescription("Przed przesłaniem całej treści paragonu zczytujemy go i przesyłamy na serwer")]
    public async Task<IActionResult> UploadReceipt(IFormFile file)
    {
        var fileName = await _fileService.SaveFileAsync(file);

        return Ok(new { FileName = fileName });
    }

    [HttpPost("create")]
    [EndpointSummary("Tworzenie nowego wydatku")]
    [EndpointDescription("Endpoint do utworzenia nowego wydatku, obszerny JSON, który obsłuży duże transakcje (kilkanaście produktów na paragonie)")]
    public async Task<IActionResult> Create([FromBody] CreateExpenseBatchRequest request)
    {
        await _expenseService.CreateExpenseBatch(GetUserId(), request);
        return Ok();
    }

}