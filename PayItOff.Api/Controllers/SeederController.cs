using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Infrastructure.Persistence;

namespace PayItOff.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SeederController : ControllerBase
{
    private readonly PayItOffDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IWebHostEnvironment _environment;

    public SeederController(PayItOffDbContext dbContext, IPasswordHasher passwordHasher, IWebHostEnvironment environment)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _environment = environment;
    }

    [HttpPost("heavy-login-seed")]
    [AllowAnonymous]
    public async Task<IActionResult> HeavyLoginSeed([FromQuery] string password)
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

        await DeleteAllDataAsync();
        await SeedUsersAsync();
        await SeedGroupsWithMembersAsync();
        await SeedExpensesAsync();
        await SeedFriendshipsAsync();
        await SeedSettlementsAsync();
        await SeedNotificationsAsync();
        await SeedGroupDebtsAsync();
        await ApplyMethodCoveragePassAsync();
        await SeedAuditLogsAsync();

        return Ok(new
        {
            Message = "Seeder wykonany.",
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
        var user1 = PayItOff.Domain.Entities.User.Register(
            "jakub.plocica@payitoff.local",
            _passwordHasher.Hash("JakubPlocica123!"),
            "JakubPlocica",
            "Jakub",
            "Płocica",
            null,
            "500100200",
            "PL10105000997603123456789123");
        user1.ConfirmVerification(user1.VerificationToken!);
        user1.UpdateNotifications(new NotificationsSettings(
            DailySummary: false,
            NotifyOnGroupJoined: true,
            NotifyOnExpenseAdded: true,
            NotifyOnGroupRemoved: true,
            NotifyOnFriendRemoved: true,
            NotifyOnExpenseChanged: true,
            NotifyOnTransferConfirmed: true));

        var user2 = PayItOff.Domain.Entities.User.Register(
            "janina.kwiat@payitoff.local",
            _passwordHasher.Hash("JaninaKwiat123!"),
            "JaninaKwiat",
            "Janina",
            "Kwiat",
            "7f68c0bc-4565-4791-b29e-f3e1e0511020_Zrzut ekranu 2026-04-27 182852.png",
            "501300400",
            "PL61109010140000071219812874");
        user2.ConfirmVerification(user2.VerificationToken!);
        user2.UpdateNotifications(new NotificationsSettings(
            DailySummary: false,
            NotifyOnGroupJoined: true,
            NotifyOnExpenseAdded: true,
            NotifyOnGroupRemoved: false,
            NotifyOnFriendRemoved: true,
            NotifyOnExpenseChanged: true,
            NotifyOnTransferConfirmed: true));

        var user3 = PayItOff.Domain.Entities.User.Register(
            "marek.kowalski@payitoff.local",
            _passwordHasher.Hash("MarekKowalski123!"),
            "MarekK",
            "Marek",
            "Kowalski",
            null,
            null,
            null);
        user3.ConfirmVerification(user3.VerificationToken!);
        user3.UpdateNotifications(new NotificationsSettings(
            DailySummary: true,
            NotifyOnGroupJoined: false,
            NotifyOnExpenseAdded: true,
            NotifyOnGroupRemoved: true,
            NotifyOnFriendRemoved: false,
            NotifyOnExpenseChanged: true,
            NotifyOnTransferConfirmed: true));

        var user4 = PayItOff.Domain.Entities.User.Register(
            "zosia.wisniewska@payitoff.local",
            _passwordHasher.Hash("ZosiaWisniewska123!"),
            "ZosiaW",
            "Zofia",
            "Wiśniewska",
            null,
            "509909808",
            null);
        user4.ConfirmVerification(user4.VerificationToken!);
        user4.UpdateNotifications(new NotificationsSettings(
            DailySummary: false,
            NotifyOnGroupJoined: false,
            NotifyOnExpenseAdded: true,
            NotifyOnGroupRemoved: true,
            NotifyOnFriendRemoved: true,
            NotifyOnExpenseChanged: false,
            NotifyOnTransferConfirmed: true));

        _dbContext.Users.AddRange(user1, user2, user3, user4);
        await _dbContext.SaveChangesAsync();

        user1.UpdateInfo("JakubPlocica", "Jakub", "Płocica", "500100200", "PL10105000997603123456789123");
        user1.GeneratePasswordResetToken();
        user1.ResetPassword(user1.PasswordResetToken!, _passwordHasher.Hash("JakubPlocica123!"));
        user1.ModifyPassword(_passwordHasher.Hash("JakubPlocica123!"));
        user1.GenerateEmailChangeToken("jakub.plocica+verified@payitoff.local");
        user1.EmailChange(user1.EmailChangeToken!);

        user2.UpdateInfo("JaninaKwiat", "Janina", "Kwiat", "501300400", "PL61109010140000071219812874");
        user2.UpdateAvatar("7f68c0bc-4565-4791-b29e-f3e1e0511020_Zrzut ekranu 2026-04-27 182852.png");
        user2.GeneratePasswordResetToken();
        user2.ResetPassword(user2.PasswordResetToken!, _passwordHasher.Hash("JaninaKwiat123!"));
        user2.ModifyPassword(_passwordHasher.Hash("JaninaKwiat123!"));
        user2.GenerateEmailChangeToken("janina.kwiat+verified@payitoff.local");
        user2.EmailChange(user2.EmailChangeToken!);

        user3.GeneratePasswordResetToken();
        user3.ResetPassword(user3.PasswordResetToken!, _passwordHasher.Hash("MarekKowalski123!"));
        user4.GeneratePasswordResetToken();
        user4.ResetPassword(user4.PasswordResetToken!, _passwordHasher.Hash("ZosiaWisniewska123!"));

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedGroupsWithMembersAsync()
    {
        var users = await _dbContext.Users.OrderBy(x => x.Id).ToListAsync();
        var user1 = users[0];
        var user2 = users[1];
        var user3 = users[2];
        var user4 = users[3];

        var trip = Group.Create("Wyjazd Chorwacja", "");
        var home = Group.Create("Mieszkanie Centrum", "");
        var work = Group.Create("Biuro i Eventy", "");
        var gaming = Group.Create("Gaming Team", "");

        _dbContext.Groups.AddRange(trip, home, work, gaming);
        await _dbContext.SaveChangesAsync();

        trip.Edit("Wyjazd Chorwacja 2026", "");
        home.Edit("Mieszkanie Centrum+", "");
        work.UpdateTimestamp();
        gaming.Delete();
        gaming.Edit("Gaming Team Reactivated", "");
        await _dbContext.SaveChangesAsync();

        var members = new List<GroupMember>
        {
            GroupMember.CreateOwner(user1, trip),
            GroupMember.CreateOwner(user2, home),
            GroupMember.CreateOwner(user3, work),
            GroupMember.CreateOwner(user4, gaming),

            CreateAcceptedMember(user2, trip, GroupMemberRole.Admin, true),
            CreateAcceptedMember(user3, trip, GroupMemberRole.Member, false),
            CreatePendingMember(user4, trip, GroupMemberRole.Member),

            CreateAcceptedMember(user1, home, GroupMemberRole.Admin, true),
            CreateAcceptedMember(user3, home, GroupMemberRole.Member, false),
            CreateDeclinedMember(user4, home, GroupMemberRole.Member),

            CreateAcceptedMember(user1, work, GroupMemberRole.Member, false),
            CreateAcceptedMember(user2, work, GroupMemberRole.Admin, true),
            CreateLeftMember(user4, work, GroupMemberRole.Member),

            CreateAcceptedMember(user1, gaming, GroupMemberRole.Member, true),
            CreateAcceptedMember(user2, gaming, GroupMemberRole.Member, false),
            CreateKickedMember(user3, gaming, GroupMemberRole.Member)
        };

        _dbContext.GroupMembers.AddRange(members);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedExpensesAsync()
    {
        var groups = await _dbContext.Groups.OrderBy(x => x.Id).ToListAsync();
        
        var groupMembers = await _dbContext.GroupMembers
            .Where(m => m.Status == GroupMemberStatus.Accepted)
            .Include(m => m.User)
            .ToListAsync();
            
        var groupUsersMap = groupMembers
            .GroupBy(m => m.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.User!).ToList());

        var rnd = new Random(20260602);
        var categories = new[] { "Jedzenie", "Transport", "Dom", "Praca", "Rozrywka", "Sport", "Subskrypcje", "Zdrowie" };
        var expenseNames = new[] { "Zakupy w Biedronce", "Orlen - Paliwo", "Uber z imprezy", "Czynsz za wynajem", "Kino i popcorn", "Kolacja w restauracji", "Ikea - nowe meble", "Żabka - przekąski", "Bilety na koncert", "KFC zestaw", "Pizza we wtorek", "Abonament za internet" };
        var itemNames = new[] { "Chleb i masło", "Woda mineralna", "Piwo", "Bilety", "Paliwo PB95", "Przejazd", "Zestaw powiększony", "Stolik", "Czipsy", "Kiełbasa na grilla", "Soki", "Karkówka", "Rachunek za prąd", "Kawa i ciastko", "Owoce" };
        var packageNames = new[] { "Napoje", "Jedzenie", "Zrzutka na alkohol", "Inne", "Dla wszystkich", "Opcjonalne" };

        var baseDate = DateTime.UtcNow.AddDays(-90);
        for (var i = 0; i < 80; i++)
        {
            var group = groups[rnd.Next(groups.Count)];
            if (!groupUsersMap.TryGetValue(group.Id, out var activeMembers) || activeMembers.Count < 2) continue;

            var creator = activeMembers[rnd.Next(activeMembers.Count)];
            var payer = activeMembers[rnd.Next(activeMembers.Count)];

            var expenseDate = DateTime.UtcNow.AddDays(-rnd.Next(1, 90)).AddMinutes(rnd.Next(10, 1200));
            var expense = Expense.Create(
                group,
                creator,
                payer,
                expenseNames[rnd.Next(expenseNames.Length)],
                i % 7 == 0 ? $"receipt-{i + 1}.png" : null,
                expenseDate);

            _dbContext.Expenses.Add(expense);
            await _dbContext.SaveChangesAsync();

            var expenseGroup = ExpenseGroup.Create(expense, packageNames[rnd.Next(packageNames.Length)], 0m);
            _dbContext.ExpenseGroups.Add(expenseGroup);
            expense.AddGroup(expenseGroup);
            await _dbContext.SaveChangesAsync();

            var itemsCount = rnd.Next(3, 8);
            for (var itemIndex = 0; itemIndex < itemsCount; itemIndex++)
            {
                var quantity = Math.Round((decimal)(rnd.NextDouble() * 5 + 1), 2);
                var unitPrice = Math.Round((decimal)(rnd.NextDouble() * 120 + 5), 2);
                var useGroup = itemIndex % 2 == 0;
                var item = ExpenseItem.Create(
                    expense,
                    useGroup ? expenseGroup : null,
                    itemNames[rnd.Next(itemNames.Length)],
                    categories[rnd.Next(categories.Length)],
                    quantity,
                    unitPrice);

                _dbContext.ExpenseItems.Add(item);
                expense.AddItem(item);
                if (useGroup)
                {
                    expenseGroup.AddItem(item);
                    expenseGroup.UpdateAmount(expenseGroup.TotalAmount + item.TotalPrice);
                }
                await _dbContext.SaveChangesAsync();

                var participants = activeMembers.OrderBy(_ => rnd.Next()).Take(rnd.Next(2, activeMembers.Count + 1)).ToList();
                var rawWeights = participants.Select(_ => Math.Round((decimal)(rnd.NextDouble() * 3 + 1), 3)).ToList();
                var sumWeights = rawWeights.Sum();
                decimal assigned = 0m;

                for (var p = 0; p < participants.Count; p++)
                {
                    decimal owed = p == participants.Count - 1
                        ? Math.Round(item.TotalPrice - assigned, 2)
                        : Math.Round(item.TotalPrice * (rawWeights[p] / sumWeights), 2);

                    if (owed <= 0) owed = 0.01m;
                    assigned += owed;

                    var split = ExpenseSplit.Create(item, participants[p], owed);
                    _dbContext.ExpenseSplits.Add(split);
                    item.AddSplit(split);
                }

                await _dbContext.SaveChangesAsync();
            }

            expense.RecalculateTotal();
            await _dbContext.SaveChangesAsync();
        }
    }

    private async Task SeedFriendshipsAsync()
    {
        var users = await _dbContext.Users.OrderBy(x => x.Id).ToListAsync();
        var user1 = users[0];
        var user2 = users[1];
        var user4 = users[3];

        var f1 = Friend.Invite(user1, user2);
        f1.Accept(user2.Id);

        var f4 = Friend.Invite(user4, user1);

        _dbContext.Friends.AddRange(f1, f4);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedSettlementsAsync()
    {
        var groups = await _dbContext.Groups.OrderBy(x => x.Id).ToListAsync();
        
        var groupMembers = await _dbContext.GroupMembers
            .Where(m => m.Status == GroupMemberStatus.Accepted)
            .Include(m => m.User)
            .ToListAsync();
            
        var groupUsersMap = groupMembers
            .GroupBy(m => m.GroupId)
            .ToDictionary(g => g.Key, g => g.Select(m => m.User!).ToList());

        var rnd = new Random(606);

        var settlementsList = new List<Settlement>();
        var settlementNames = new[] { "Oddaję za pizzę", "Czynsz za maj", "Zaliczka na wyjazd", "Za wczorajszego Ubera", "Rozliczenie miesięczne", "Oddaję za Biedrę", "Za prezent", "Szybki przelew", "Blik za kino", "Wyrównanie długów" };

        for (var i = 0; i < 36; i++)
        {
            var group = groups[rnd.Next(groups.Count)];
            if (!groupUsersMap.TryGetValue(group.Id, out var activeMembers) || activeMembers.Count < 2) continue;

            var sender = activeMembers[rnd.Next(activeMembers.Count)];
            var receiver = activeMembers[rnd.Next(activeMembers.Count)];
            if (sender.Id == receiver.Id)
            {
                receiver = activeMembers[(activeMembers.IndexOf(sender) + 1) % activeMembers.Count];
            }
            var amount = Math.Round((decimal)(rnd.NextDouble() * 500 + 20), 2);

            var settlement = Settlement.Create(sender, receiver, group, amount, settlementNames[rnd.Next(settlementNames.Length)]);

            if (i % 3 == 0)
            {
                settlement.Confirm();
            }
            else if (i % 5 == 0)
            {
                settlement.Reject();
            }
            else if (i % 2 == 0)
            {
                settlement.Remind();
            }

            settlementsList.Add(settlement);
        }

        _dbContext.Settlements.AddRange(settlementsList);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedNotificationsAsync()
    {
        var users = await _dbContext.Users.OrderBy(x => x.Id).ToListAsync();
        var groups = await _dbContext.Groups.OrderBy(x => x.Id).ToListAsync();
        var expenses = await _dbContext.Expenses.OrderBy(x => x.Id).ToListAsync();
        var settlements = await _dbContext.Settlements.OrderBy(x => x.Id).ToListAsync();
        var rnd = new Random(17);

        var notifications = new List<Notification>();
        for (var i = 0; i < 220; i++)
        {
            var user = users[rnd.Next(users.Count)];
            var actor = users[rnd.Next(users.Count)];
            if (user.Id == actor.Id)
            {
                actor = users[(users.IndexOf(actor) + 1) % users.Count];
            }
            
            var group = groups[rnd.Next(groups.Count)];
            
            var type = (NotificationType)((i % 4) + 1);
            var entityType = (EntityType)((i % 8) + 1);
            var amount = Math.Round((decimal)(rnd.NextDouble() * 150 + 10), 2);
            
            string body = type switch
            {
                NotificationType.Adding => i % 2 == 0 
                    ? $"Użytkownik {actor.FullName} przyjął twoje zaproszenie do listy znajomych"
                    : $"{actor.FullName} dodał wydatek na kwotę {amount} zł w grupie '{group.Name}'",
                
                NotificationType.Deleting => i % 2 == 0
                    ? $"Użytkownik {actor.FullName} odrzucił twoje zaproszenie do listy znajomych"
                    : $"{actor.FullName} usunął Cię z grupy '{group.Name}'",
                
                NotificationType.NeedAction => i % 2 == 0
                    ? $"Użytkownik {actor.FullName} zaprosił Cię do grupy: '{group.Name}'"
                    : $"{actor.FullName} zadeklarował spłatę {amount} zł w grupie '{group.Name}'",
                
                NotificationType.Normal => i % 3 == 0
                    ? $"{actor.FullName} przypomina o zapłacie {amount} PLN w grupie '{group.Name}'."
                    : (i % 3 == 1 
                        ? $"{actor.FullName} zatwierdził twoją spłatę długu, która wynosiła: {amount} zł" 
                        : $"Twoja rola w grupie '{group.Name}' została zmieniona przez {actor.FullName} na Admin"),
                
                _ => $"Nowa aktywność od użytkownika {actor.FullName}"
            };

            var entityId = entityType switch
            {
                EntityType.Expenses => expenses[rnd.Next(expenses.Count)].Id,
                EntityType.Groups => group.Id,
                EntityType.Settlements => settlements[rnd.Next(settlements.Count)].Id,
                EntityType.Users => actor.Id,
                EntityType.GroupMembers => group.Id,
                EntityType.GroupDebts => group.Id,
                EntityType.Friends => actor.Id,
                _ => expenses[rnd.Next(expenses.Count)].Id
            };

            var notification = Notification.Create(
                user.Id,
                actor.Id,
                type,
                body,
                entityId,
                entityType);

            if (i % 3 == 0)
            {
                notification.MarkAsRead();
            }
            else if (i % 7 == 0)
            {
                notification.Hide();
            }

            if (i % 8 == 0)
            {
                notification.ChangeTypeToNormal();
            }

            if (i % 11 == 0)
            {
                notification.Delete();
            }

            notifications.Add(notification);
        }

        _dbContext.Notifications.AddRange(notifications);
        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedGroupDebtsAsync()
    {
        var expenses = await _dbContext.Expenses
            .Include(e => e.Items).ThenInclude(i => i.Splits)
            .Include(e => e.Groups).ThenInclude(g => g.Items).ThenInclude(i => i.Splits)
            .ToListAsync();
        var settlements = await _dbContext.Settlements.Where(s => s.Status == SettlementStatus.Confirmed).ToListAsync();
        var groups = await _dbContext.Groups.ToDictionaryAsync(g => g.Id);
        var users = await _dbContext.Users.ToDictionaryAsync(u => u.Id);

        var netDebts = new Dictionary<string, decimal>();

        void AddDebt(int groupId, int debtorId, int creditorId, decimal amount)
        {
            if (debtorId == creditorId) return;
            var key1 = $"{groupId}:{debtorId}:{creditorId}";
            var key2 = $"{groupId}:{creditorId}:{debtorId}";

            if (netDebts.ContainsKey(key2))
            {
                netDebts[key2] -= amount;
                if (netDebts[key2] < 0)
                {
                    netDebts[key1] = -netDebts[key2];
                    netDebts.Remove(key2);
                }
            }
            else
            {
                if (!netDebts.ContainsKey(key1)) netDebts[key1] = 0;
                netDebts[key1] += amount;
            }
        }

        foreach (var expense in expenses)
        {
            var expenseDebts = expense.CalculateDebts();
            foreach (var kvp in expenseDebts)
            {
                AddDebt(expense.GroupId, kvp.Key, expense.PayerId, kvp.Value);
            }
        }

        foreach (var settlement in settlements)
        {
            AddDebt(settlement.GroupId, settlement.ReceiverId, settlement.SenderId, settlement.Amount);
        }

        var debts = new List<GroupDebt>();
        foreach (var kvp in netDebts.Where(x => x.Value > 0))
        {
            var parts = kvp.Key.Split(':');
            var groupId = int.Parse(parts[0]);
            var debtorId = int.Parse(parts[1]);
            var creditorId = int.Parse(parts[2]);

            var debt = GroupDebt.Create(groups[groupId], users[debtorId], users[creditorId], kvp.Value);
            debts.Add(debt);
        }

        _dbContext.GroupDebts.AddRange(debts);
        await _dbContext.SaveChangesAsync();
    }

    private async Task ApplyMethodCoveragePassAsync()
    {
        var accepted = await _dbContext.GroupMembers
            .Where(x => x.Status == GroupMemberStatus.Accepted && x.Role != GroupMemberRole.Owner)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();
        if (accepted != null)
        {
            accepted.UpdateRole(GroupMemberRole.Admin);
            accepted.UpdateRole(GroupMemberRole.Member);
        }

        var declined = await _dbContext.GroupMembers
            .Where(x => x.Status == GroupMemberStatus.Declined)
            .OrderBy(x => x.Id)
            .FirstOrDefaultAsync();
        if (declined != null)
        {
            declined.ReInvite(GroupMemberRole.Member);
            declined.Accept();
        }

        var firstExpense = await _dbContext.Expenses
            .Include(e => e.Items)
            .ThenInclude(i => i.Splits)
            .Include(e => e.Groups)
            .ThenInclude(g => g.Items)
            .OrderBy(e => e.Id)
            .FirstOrDefaultAsync();
        if (firstExpense != null)
        {
            firstExpense.CalculateDebts();
        }

        var firstItem = await _dbContext.ExpenseItems
            .Include(i => i.Splits)
            .OrderBy(i => i.Id)
            .FirstOrDefaultAsync();
        if (firstItem != null)
        {
            firstItem.Edit($"{firstItem.Name} PRO", firstItem.Category);
            firstItem.UpdateQuantity(firstItem.Quantity + 1);
            firstItem.UpdateUnitPrice(firstItem.UnitPrice + 0.50m);
            var snapshot = firstItem.Splits.Select(s => new { s.User, s.OwedAmount }).ToList();
            firstItem.ClearSplits();
            foreach (var split in snapshot)
            {
                var recreated = ExpenseSplit.Create(firstItem, split.User!, Math.Max(0.01m, split.OwedAmount));
                _dbContext.ExpenseSplits.Add(recreated);
                firstItem.AddSplit(recreated);
            }
        }

        var firstSplit = await _dbContext.ExpenseSplits.OrderBy(x => x.Id).FirstOrDefaultAsync();
        if (firstSplit != null)
        {
            firstSplit.UpdateAmount(firstSplit.OwedAmount + 0.25m);
        }

        var firstGroupExpenseGroup = await _dbContext.ExpenseGroups
            .Include(g => g.Items)
            .OrderBy(g => g.Id)
            .FirstOrDefaultAsync();
        if (firstGroupExpenseGroup != null)
        {
            firstGroupExpenseGroup.Edit($"{firstGroupExpenseGroup.Name} Final");
            firstGroupExpenseGroup.UpdateAmount(firstGroupExpenseGroup.Items.Sum(x => x.TotalPrice));
        }

        await _dbContext.SaveChangesAsync();
    }

    private async Task SeedAuditLogsAsync()
    {
        var users = await _dbContext.Users.OrderBy(x => x.Id).ToListAsync();
        var groups = await _dbContext.Groups.OrderBy(x => x.Id).ToListAsync();
        var expenses = await _dbContext.Expenses.OrderBy(x => x.Id).ToListAsync();
        var settlements = await _dbContext.Settlements.OrderBy(x => x.Id).ToListAsync();
        var debts = await _dbContext.GroupDebts.OrderBy(x => x.Id).ToListAsync();
        var rnd = new Random(1410);

        var logs = new List<AuditLog>();
        for (var i = 0; i < 140; i++)
        {
            var actor = users[rnd.Next(users.Count)];
            var entityType = (EntityType)((i % 7) + 1);
            var entityId = entityType switch
            {
                EntityType.Users => users[rnd.Next(users.Count)].Id,
                EntityType.Groups => groups[rnd.Next(groups.Count)].Id,
                EntityType.Expenses => expenses[rnd.Next(expenses.Count)].Id,
                EntityType.Settlements => settlements[rnd.Next(settlements.Count)].Id,
                EntityType.GroupDebts => debts[rnd.Next(debts.Count)].Id,
                EntityType.Friends => users[rnd.Next(users.Count)].Id,
                EntityType.GroupMembers => groups[rnd.Next(groups.Count)].Id,
                _ => expenses[rnd.Next(expenses.Count)].Id
            };

            var action = (i % 3) switch
            {
                0 => AuditLogAction.Created,
                1 => AuditLogAction.Updated,
                _ => AuditLogAction.Deleted
            };

            if (i % 2 == 0)
            {
                logs.Add(AuditLog.CreateWithUserId(
                    entityType,
                    entityId,
                    actor.Id,
                    action,
                    "{\"old\":\"value\"}",
                    "{\"new\":\"value\"}"));
            }
            else
            {
                logs.Add(AuditLog.Create(
                    entityType,
                    entityId,
                    actor,
                    action,
                    "{\"old\":\"value2\"}",
                    "{\"new\":\"value2\"}"));
            }
        }

        _dbContext.AuditLogs.AddRange(logs);
        await _dbContext.SaveChangesAsync();
    }

    private static GroupMember CreateAcceptedMember(User user, Group group, GroupMemberRole role, bool isFavorite)
    {
        var gm = GroupMember.Invite(user, group, role);
        gm.Accept();
        if (isFavorite) gm.ToggleFavorite();
        return gm;
    }

    private static GroupMember CreatePendingMember(User user, Group group, GroupMemberRole role)
    {
        return GroupMember.Invite(user, group, role);
    }

    private static GroupMember CreateDeclinedMember(User user, Group group, GroupMemberRole role)
    {
        var gm = GroupMember.Invite(user, group, role);
        gm.Decline();
        return gm;
    }

    private static GroupMember CreateLeftMember(User user, Group group, GroupMemberRole role)
    {
        var gm = GroupMember.Invite(user, group, role);
        gm.Accept();
        gm.Leave();
        return gm;
    }

    private static GroupMember CreateKickedMember(User user, Group group, GroupMemberRole role)
    {
        var gm = GroupMember.Invite(user, group, role);
        gm.Accept();
        gm.Kick();
        return gm;
    }
}
