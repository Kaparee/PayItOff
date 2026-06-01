using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PayItOff.Application.Interfaces;
using PayItOff.Shared.Requests;

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

    [HttpGet("{id}")]
    [EndpointSummary("Szczegóły wydatku")]
    [EndpointDescription("Endpoint zwracający detale konkretnego wydatku po ID, wraz z informacją kto za niego zapłacił i jacy użytkownicy biorą w nim udział.")]
    public async Task<IActionResult> GetExpenseDetails(int id)
    {
        var response = await _expenseService.GetExpenseDetailsAsync(GetUserId(), id);
        return Ok(response);
    }

    [HttpGet("{expenseId}/item/{itemId}")]
    [EndpointSummary("Szczegóły konkretnej pozycji z wydatku")]
    [EndpointDescription("Endpoint zwracający detale konkretnej pozycji z wydatku (produktu z paragonu) wraz z informacją o jej podziale.")]
    public async Task<IActionResult> GetExpenseItemDetails(int expenseId, int itemId)
    {
        var response = await _expenseService.GetExpenseItemDetailsAsync(GetUserId(), expenseId, itemId);
        return Ok(response);
    }

    [HttpPut("{expenseId}/item/{itemId}")]
    [EndpointSummary("Edycja pozycji z wydatku")]
    [EndpointDescription("Edycja nazwy i kategorii produktu na paragonie.")]
    public async Task<IActionResult> UpdateExpenseItem(int expenseId, int itemId, [FromBody] UpdateExpenseItemRequest request)
    {
        await _expenseService.UpdateExpenseItemAsync(GetUserId(), expenseId, itemId, request);
        return Ok();
    }

    [HttpDelete("{expenseId}/item/{itemId}")]
    [EndpointSummary("Usuwanie pozycji z wydatku")]
    [EndpointDescription("Usuwa konkretny produkt z paragonu i cofa powiązane z nim zadłużenia.")]
    public async Task<IActionResult> DeleteExpenseItem(int expenseId, int itemId)
    {
        await _expenseService.DeleteExpenseItemAsync(GetUserId(), expenseId, itemId);
        return Ok();
    }
}