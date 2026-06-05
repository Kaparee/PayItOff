using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Infrastructure.Persistence;
using PayItOff.Shared.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeederController : ControllerBase
{
    private readonly PayItOffDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebHostEnvironment _environment;
    private readonly IUserService _userService;
    private readonly IGroupService _groupService;
    private readonly IGroupMemberService _groupMemberService;
    private readonly IExpenseService _expenseService;
    private readonly IFriendService _friendService;
    private readonly ISettlementService _settlementService;

    public SeederController(
        PayItOffDbContext dbContext,
        IPasswordHasher passwordHasher,
        IWebHostEnvironment environment,
        IUserService userService,
        IGroupService groupService,
        IGroupMemberService groupMemberService,
        IExpenseService expenseService,
        IFriendService friendService,
        ISettlementService settlementService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _environment = environment;
        _userService = userService;
        _groupService = groupService;
        _groupMemberService = groupMemberService;
        _expenseService = expenseService;
        _friendService = friendService;
        _settlementService = settlementService;
    }

    [HttpPost("heavy-login-seed")]
    [AllowAnonymous]
    public async Task<IActionResult> HeavyLoginSeed([FromQuery] string? password)
    {
        if (password != "admin1234")
        {
            return Unauthorized("Złe hasło do seedera.");
        }

        if (!_environment.IsDevelopment())
        {
            return Forbid();
        }

        HttpContext.User = new System.Security.Claims.ClaimsPrincipal();

        PayItOff.Application.Services.EmailService.IsDisabledForSeeder = true;
        try
        {
            await DeleteAllDataAsync();
            await SeedUsersAsync();
            await SeedGroupsWithMembersAsync();
            await SeedFriendshipsAsync();
            await SeedExpensesAsync();
            await SeedSettlementsAsync();
        }
        finally
        {
            PayItOff.Application.Services.EmailService.IsDisabledForSeeder = false;
        }

        return Ok(new
        {
            Message = "Seeder wykonany z użyciem serwisów aplikacyjnych.",
            Users = await _dbContext.Users.CountAsync(),
            Groups = await _dbContext.Groups.CountAsync(),
            GroupMembers = await _dbContext.GroupMembers.CountAsync(),
            Expenses = await _dbContext.Expenses.CountAsync(),
            ExpenseItems = await _dbContext.ExpenseItems.CountAsync(),
            ExpenseSplits = await _dbContext.ExpenseSplits.CountAsync(),
            Notifications = await _dbContext.Notifications.CountAsync(),
            Settlements = await _dbContext.Settlements.CountAsync(),
            Friends = await _dbContext.Friends.CountAsync(),
            GroupDebts = await _dbContext.GroupDebts.CountAsync()
        });
    }

    private async Task DeleteAllDataAsync()
    {
        await _dbContext.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"AuditLogs\", \"Notifications\", \"Settlements\", \"GroupDebts\", \"ExpenseSplits\", \"ExpenseItems\", \"ExpenseGroups\", \"Expenses\", \"GroupMembers\", \"Groups\", \"Friends\", \"Users\" RESTART IDENTITY CASCADE;");
    }

    private async Task SeedUsersAsync()
    {
        await _userService.RegisterAsync(new RegisterRequest
        {
            Email = "jakub.plocica@payitoff.local",
            Password = "JakubPlocica123!",
            Nickname = "JakubPlocica",
            Name = "Jakub",
            Surname = "Płocica",
            PhoneNumber = "500100200",
            IBAN = "PL10105000997603123456789123"
        }, null);

        await _userService.RegisterAsync(new RegisterRequest
        {
            Email = "janina.kwiat@payitoff.local",
            Password = "JaninaKwiat123!",
            Nickname = "JaninaKwiat",
            Name = "Janina",
            Surname = "Kwiat",
            PhoneNumber = "501300400",
            IBAN = "PL61109010140000071219812874"
        }, null);

        await _userService.RegisterAsync(new RegisterRequest
        {
            Email = "marek.kowalski@payitoff.local",
            Password = "MarekKowalski123!",
            Nickname = "MarekKowalski",
            Name = "Marek",
            Surname = "Kowalski"
        }, null);

        await _userService.RegisterAsync(new RegisterRequest
        {
            Email = "zosia.wisniewska@payitoff.local",
            Password = "ZosiaWisniewska123!",
            Nickname = "ZosiaWisniewska",
            Name = "Zofia",
            Surname = "Wiśniewska",
            PhoneNumber = "509909808"
        }, null);

        var users = await _dbContext.Users.ToListAsync();
        foreach (var user in users)
        {
            await _userService.VerifyUserAsync(user.VerificationToken!);
        }

        var u1 = users.First(u => u.Email == "jakub.plocica@payitoff.local");
        await _userService.UpdateNotificationAsync(u1.Id, new UserNotificationChangeRequest
        {
            Notifications = new UserNotificationSettingsRequest(false, true, true, true, true, true, true)
        });

        var u2 = users.First(u => u.Email == "janina.kwiat@payitoff.local");
        await _userService.UpdateNotificationAsync(u2.Id, new UserNotificationChangeRequest
        {
            Notifications = new UserNotificationSettingsRequest(false, true, true, false, true, true, true)
        });

        var u3 = users.First(u => u.Email == "marek.kowalski@payitoff.local");
        await _userService.UpdateNotificationAsync(u3.Id, new UserNotificationChangeRequest
        {
            Notifications = new UserNotificationSettingsRequest(true, false, true, true, false, true, true)
        });

        var u4 = users.First(u => u.Email == "zosia.wisniewska@payitoff.local");
        await _userService.UpdateNotificationAsync(u4.Id, new UserNotificationChangeRequest
        {
            Notifications = new UserNotificationSettingsRequest(false, false, true, true, true, false, true)
        });

        await _userService.UpdateInfoAsync(u1.Id, new UserInfoUpdateRequest
        {
            Nickname = "JakubPlocica",
            Name = "Jakub",
            Surname = "Płocica",
            PhoneNumber = "500100200",
            IBAN = "PL10105000997603123456789123"
        });

        _dbContext.ChangeTracker.Clear();
        await _userService.RequestPasswordResetAsync("janina.kwiat@payitoff.local");
    }

    private async Task SeedGroupsWithMembersAsync()
    {
        var users = await _dbContext.Users.OrderBy(x => x.Id).ToListAsync();
        var u1 = users[0]; var u2 = users[1]; var u3 = users[2]; var u4 = users[3];

        await _groupService.CreateAsync(new CreateGroupRequest { Name = "Wyjazd Chorwacja" }, u1.Id, null);
        await _groupService.CreateAsync(new CreateGroupRequest { Name = "Mieszkanie Centrum" }, u2.Id, null);
        await _groupService.CreateAsync(new CreateGroupRequest { Name = "Biuro i Eventy" }, u3.Id, null);
        await _groupService.CreateAsync(new CreateGroupRequest { Name = "Gaming Team" }, u4.Id, null);

        var groups = await _dbContext.Groups.OrderBy(x => x.Id).ToListAsync();
        var trip = groups[0]; var home = groups[1]; var work = groups[2]; var gaming = groups[3];

        await _groupMemberService.InviteUserAsync(u1.Id, new GroupInviteUserRequest { GroupId = trip.Id, UserId = u2.Id, Role = GroupMemberRole.Admin });
        await _groupMemberService.InviteUserAsync(u1.Id, new GroupInviteUserRequest { GroupId = trip.Id, UserId = u3.Id, Role = GroupMemberRole.Member });
        await _groupMemberService.InviteUserAsync(u1.Id, new GroupInviteUserRequest { GroupId = trip.Id, UserId = u4.Id, Role = GroupMemberRole.Member });

        await AcceptAllPendingInvitationsAsync(u2.Id, trip.Id);
        await AcceptAllPendingInvitationsAsync(u3.Id, trip.Id);

        await _groupService.EditGroupInfoAsync(u1.Id, new EditGroupInfoRequest { GroupId = trip.Id, NewName = "Wyjazd Chorwacja 2026+" }, null);
        await _groupMemberService.SetGroupAsFavoriteAsync(u1.Id, trip.Id);
        await _groupMemberService.UpdateRoleAsync(u1.Id, new GroupMemberUpdateRequest { GroupId = trip.Id, TargetUserId = u3.Id, NewRole = GroupMemberRole.Admin });

        await _groupMemberService.InviteUserAsync(u2.Id, new GroupInviteUserRequest { GroupId = home.Id, UserId = u1.Id, Role = GroupMemberRole.Admin });
        await _groupMemberService.InviteUserAsync(u2.Id, new GroupInviteUserRequest { GroupId = home.Id, UserId = u3.Id, Role = GroupMemberRole.Member });
        await _groupMemberService.InviteUserAsync(u2.Id, new GroupInviteUserRequest { GroupId = home.Id, UserId = u4.Id, Role = GroupMemberRole.Member });

        await AcceptAllPendingInvitationsAsync(u1.Id, home.Id);
        await AcceptAllPendingInvitationsAsync(u3.Id, home.Id);
        await DeclineAllPendingInvitationsAsync(u4.Id, home.Id);

        await _groupMemberService.InviteUserAsync(u3.Id, new GroupInviteUserRequest { GroupId = work.Id, UserId = u1.Id, Role = GroupMemberRole.Member });
        await _groupMemberService.InviteUserAsync(u3.Id, new GroupInviteUserRequest { GroupId = work.Id, UserId = u2.Id, Role = GroupMemberRole.Admin });
        await _groupMemberService.InviteUserAsync(u3.Id, new GroupInviteUserRequest { GroupId = work.Id, UserId = u4.Id, Role = GroupMemberRole.Member });

        await AcceptAllPendingInvitationsAsync(u1.Id, work.Id);
        await AcceptAllPendingInvitationsAsync(u2.Id, work.Id);
        await AcceptAllPendingInvitationsAsync(u4.Id, work.Id);
        await _groupMemberService.LeaveGroupAsync(u4.Id, work.Id);

        await _groupMemberService.InviteUserAsync(u4.Id, new GroupInviteUserRequest { GroupId = gaming.Id, UserId = u1.Id, Role = GroupMemberRole.Member });
        await _groupMemberService.InviteUserAsync(u4.Id, new GroupInviteUserRequest { GroupId = gaming.Id, UserId = u2.Id, Role = GroupMemberRole.Member });
        await _groupMemberService.InviteUserAsync(u4.Id, new GroupInviteUserRequest { GroupId = gaming.Id, UserId = u3.Id, Role = GroupMemberRole.Member });

        await AcceptAllPendingInvitationsAsync(u1.Id, gaming.Id);
        await AcceptAllPendingInvitationsAsync(u2.Id, gaming.Id);
        await AcceptAllPendingInvitationsAsync(u3.Id, gaming.Id);
        await _groupMemberService.KickUserFromGroupAsync(u4.Id, gaming.Id, u3.Id);
    }

    private async Task AcceptAllPendingInvitationsAsync(int userId, int groupId)
    {
        var invite = await _dbContext.GroupMembers.FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId && m.Status == GroupMemberStatus.Pending);
        if (invite != null)
        {
            await _groupMemberService.AcceptInviteAsync(userId, invite.Id);
        }
    }

    private async Task DeclineAllPendingInvitationsAsync(int userId, int groupId)
    {
        var invite = await _dbContext.GroupMembers.FirstOrDefaultAsync(m => m.UserId == userId && m.GroupId == groupId && m.Status == GroupMemberStatus.Pending);
        if (invite != null)
        {
            await _groupMemberService.DeclineInviteAsync(userId, invite.Id);
        }
    }

    private async Task SeedExpensesAsync()
    {
        var groups = await _dbContext.Groups.OrderBy(x => x.Id).ToListAsync();

        var rnd = new Random(20260602);
        var categories = new[] { "Jedzenie", "Transport", "Dom", "Praca", "Rozrywka", "Sport", "Subskrypcje", "Zdrowie" };
        var expenseNames = new[] { "Zakupy w Biedronce", "Orlen - Paliwo", "Uber z imprezy", "Czynsz za wynajem", "Kino i popcorn", "Kolacja w restauracji", "Ikea - nowe meble", "Żabka - przekąski", "Bilety na koncert", "KFC zestaw", "Pizza we wtorek", "Abonament za internet" };
        var itemNames = new[] { "Chleb i masło", "Woda mineralna", "Piwo", "Bilety", "Paliwo PB95", "Przejazd", "Zestaw powiększony", "Stolik", "Czipsy", "Kiełbasa na grilla", "Soki", "Karkówka", "Rachunek za prąd", "Kawa i ciastko", "Owoce" };
        var packageNames = new[] { "Napoje", "Jedzenie", "Zrzutka na alkohol", "Inne", "Dla wszystkich", "Opcjonalne" };

        for (var i = 0; i < 80; i++)
        {
            var group = groups[rnd.Next(groups.Count)];
            var activeMembers = await _dbContext.GroupMembers
                .Where(m => m.GroupId == group.Id && m.Status == GroupMemberStatus.Accepted)
                .Select(m => m.User!)
                .ToListAsync();

            if (activeMembers.Count < 2) continue;

            var creator = activeMembers[rnd.Next(activeMembers.Count)];
            var payer = activeMembers[rnd.Next(activeMembers.Count)];

            var expenseDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 90)).AddMinutes(rnd.Next(10, 1200));

            var itemsCount = rnd.Next(3, 8);
            var items = new List<ExpenseItemDto>();
            var groupsDto = new List<ExpenseGroupDto>();

            for (var itemIndex = 0; itemIndex < itemsCount; itemIndex++)
            {
                var quantity = Math.Round((decimal)(rnd.NextDouble() * 5 + 1), 2);
                var unitPrice = Math.Round((decimal)(rnd.NextDouble() * 120 + 5), 2);
                var useGroup = itemIndex % 2 == 0;

                var participantsCount = rnd.Next(2, activeMembers.Count + 1);
                var participants = activeMembers.OrderBy(_ => rnd.Next()).Take(participantsCount).Select(u => u.Id).ToList();

                var itemDto = new ExpenseItemDto
                {
                    Name = itemNames[rnd.Next(itemNames.Length)],
                    Category = categories[rnd.Next(categories.Length)],
                    Quantity = quantity,
                    UnitPrice = unitPrice,
                    ParticipantIds = participants,
                    RemainderRecipientId = null
                };

                if (useGroup)
                {
                    groupsDto.Add(new ExpenseGroupDto
                    {
                        Name = packageNames[rnd.Next(packageNames.Length)],
                        ParticipantIds = participants,
                        Items = new List<ExpenseItemDto> { itemDto },
                        RemainderRecipientId = null
                    });
                }
                else
                {
                    items.Add(itemDto);
                }
            }

            var request = new CreateExpenseBatchRequest
            {
                GroupId = group.Id,
                Expenses = new List<SubExpenseDto>
                {
                    new SubExpenseDto
                    {
                        PayerId = payer.Id,
                        Name = expenseNames[rnd.Next(expenseNames.Length)],
                        PurchasedAt = expenseDate,
                        ReciptImageUrl = i % 7 == 0 ? $"receipt-{i + 1}.png" : null,
                        Groups = groupsDto,
                        Items = items
                    }
                }
            };

            await _expenseService.CreateExpenseBatch(creator.Id, request);
        }

        var firstExpense = await _dbContext.Expenses.Include(e => e.Items).FirstOrDefaultAsync();
        if (firstExpense != null && firstExpense.Items.Any())
        {
            var itemToUpdate = firstExpense.Items.First();
            var updateReq = new UpdateExpenseItemRequest
            {
                Name = itemToUpdate.Name + " (Zaktualizowano)",
                Category = itemToUpdate.Category,
                TotalPrice = itemToUpdate.TotalPrice,
                Splits = new List<ExpenseSplitDto>()
            };
            await _expenseService.UpdateExpenseItemAsync(firstExpense.PayerId, firstExpense.Id, itemToUpdate.Id, updateReq);
        }
    }

    private async Task SeedFriendshipsAsync()
    {
        var users = await _dbContext.Users.OrderBy(x => x.Id).ToListAsync();
        var user1 = users[0];
        var user2 = users[1];
        var user4 = users[3];

        await _friendService.InviteAsync(user1.Id, new FriendInviteRequest { TargetUserId = user2.Id });
        var f1 = await _dbContext.Friends.FirstOrDefaultAsync(f => f.InviterId == user1.Id && f.ReceiverId == user2.Id);
        if (f1 != null)
        {
            await _friendService.AcceptInviteAsync(user2.Id, new UpdateInviteRequest { InviteId = f1.Id });
        }

        await _friendService.InviteAsync(user4.Id, new FriendInviteRequest { TargetUserId = user1.Id });

        var user3 = users[2];
        await _friendService.InviteAsync(user3.Id, new FriendInviteRequest { TargetUserId = user4.Id });
        var f2 = await _dbContext.Friends.FirstOrDefaultAsync(f => f.InviterId == user3.Id && f.ReceiverId == user4.Id);
        if (f2 != null)
        {
            await _friendService.DeclineInviteAsync(user4.Id, new UpdateInviteRequest { InviteId = f2.Id });
        }

        await _friendService.InviteAsync(user1.Id, new FriendInviteRequest { TargetUserId = user3.Id });
        var f3 = await _dbContext.Friends.FirstOrDefaultAsync(f => f.InviterId == user1.Id && f.ReceiverId == user3.Id);
        if (f3 != null)
        {
            await _friendService.AcceptInviteAsync(user3.Id, new UpdateInviteRequest { InviteId = f3.Id });
            await _friendService.RemoveFriendAsync(user1.Id, new UpdateInviteRequest { InviteId = f3.Id });
        }
    }

    private async Task SeedSettlementsAsync()
    {
        var actualDebts = await _dbContext.GroupDebts
            .Where(d => d.Amount > 0)
            .ToListAsync();

        var rnd = new Random(606);
        var settlementNames = new[] { "Oddaję za pizzę", "Czynsz za maj", "Zaliczka na wyjazd", "Za wczorajszego Ubera", "Rozliczenie miesięczne", "Oddaję za Biedrę", "Za prezent", "Szybki przelew", "Blik za kino", "Wyrównanie długów" };

        int i = 0;
        foreach (var debt in actualDebts)
        {
            if (i % 2 == 0)
            {
                await _settlementService.SendDebtReminderAsync(debt.CreditorId, new RemindDebtRequest { GroupId = debt.GroupId, DebtorUserId = debt.DebtorId });
                i++;
                _dbContext.ChangeTracker.Clear();
                continue;
            }

            var percentage = (decimal)(rnd.NextDouble() * 0.9 + 0.1);
            var amount = Math.Round(debt.Amount * percentage, 2);
            if (amount <= 0) amount = debt.Amount;

            var req = new CreateSettlementRequest { GroupId = debt.GroupId, ReceiverId = debt.CreditorId, Amount = amount };
            var settlementId = await _settlementService.CreateSettlementAsync(debt.DebtorId, req);

            if (i % 3 == 0)
            {
                await _settlementService.AcceptSettlementAsync(debt.CreditorId, settlementId);
            }
            else if (i % 5 == 0)
            {
                await _settlementService.RejectSettlementAsync(debt.CreditorId, settlementId);
            }

            i++;
            _dbContext.ChangeTracker.Clear();
        }
    }
}
