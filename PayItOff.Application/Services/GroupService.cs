using FluentValidation;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PayItOff.Application.Helpers;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Globalization;

namespace PayItOff.Application.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateGroupRequest> _validator;
    private readonly IConfiguration _configuration;
    private readonly IFileService _fileService;
    private readonly IGroupDebtRepository _groupDebtRepository;
    private readonly IExpenseRepository _expenseRepository;

    public GroupService(IGroupRepository groupRepository, IUserRepository userRepository, IGroupMemberRepository groupMemberRepository, IUnitOfWork unitOfWork, IValidator<CreateGroupRequest> validator, IJWTService jwtService, IConfiguration configuration, IFileService fileService, IGroupDebtRepository groupDebtRepository, IExpenseRepository expenseRepository)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _groupMemberRepository = groupMemberRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _fileService = fileService;
        _configuration = configuration;
        _groupDebtRepository = groupDebtRepository;
        _expenseRepository = expenseRepository;
    }
    public async Task CreateAsync(CreateGroupRequest request, int userId, IFormFile? avatar)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var savedFileName = await _fileService.SaveAvatarAsync(avatar);

        var group = Group.Create(
            request.Name,
            savedFileName!
        );

        var groupMember = GroupMember.CreateOwner(
            user,
            group
        );

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _groupRepository.AddAsync(group);
            await _groupMemberRepository.AddAsync(groupMember);

            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task<List<GroupInfoResponse>> GetUserGroupsAsync(int userId)
    {
        var members = await _groupRepository.GetUserGroupsAsync(userId);

        var balances = await _groupDebtRepository.GetUserGroupBalancesAsync(userId);

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var response = members.Select(member =>
        {
            var groupId = member.Group!.Id;

            var (income, expense) = balances.ContainsKey(groupId) ? balances[groupId] : (0m, 0m);
            return new GroupInfoResponse
            {
                Id = groupId,
                Name = member.Group.Name,
                AvatarUrl = AvatarUrlHelper.BuildGroupAvatarUrl(baseUrl!, member.Group.AvatarUrl),
                UpdatedAt = member.Group.UpdatedAt,
                IsFavorite = member.IsFavorite,
                Income = income,
                Expense = expense,
                Balance = income - expense,
            };
        }).ToList();

        return response;
    }

    public async Task<List<ActiveGroupsDisplayResponse>> GetTop4UserActiveGroupsAsync(int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var groups = await _groupRepository.GetTop4UserActiveGroupsAsync(userId);
        if (groups is null) { throw new GroupNotFoundException(); }

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var plCulture = new CultureInfo("pl-PL");


        return groups.Select(group =>
        {
            var diff = DateTime.UtcNow - group.Group!.UpdatedAt;

            var lastUpdateText = diff.TotalMinutes < 1
                ? "Teraz"
                : $"{diff.Humanize(precision: 1, culture: plCulture)} temu";

            return new ActiveGroupsDisplayResponse
            {
                Id = group.GroupId,
                Name = group.Group!.Name,
                AvatarUrl = AvatarUrlHelper.BuildGroupAvatarUrl(baseUrl!, group.Group.AvatarUrl),
                LastUpdate = lastUpdateText
            };
        }).ToList();
    }

    public async Task EditGroupInfoAsync(int userId, EditGroupInfoRequest request, IFormFile? avatar)
    {
        if (request == null) throw new ArgumentNullException(nameof(request), "Payload nie został zmapowany.");
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }
        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
        if (group is null) { throw new GroupNotFoundException(); }
        var isOwnerOrAdmin = await _groupMemberRepository.IsUserOwnerOrAdmin(userId, request.GroupId);
        if (!isOwnerOrAdmin) { throw new InvalidUserRoleException(); }

        var savedFileName = await _fileService.SaveAvatarAsync(avatar);

        if (savedFileName != null && group.AvatarUrl != null)
        {
            _fileService.DeleteFile(group.AvatarUrl);
        }

        group.Edit(request.NewName, savedFileName);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeleteGroupAsync(int userId, DeleteGroupRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }
        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
        if (group is null) { throw new GroupNotFoundException(); }
        var isOwner = await _groupMemberRepository.IsUserOwner(userId, request.GroupId);
        if (!isOwner) { throw new InvalidUserRoleException(); }
        var hasDebt = await _groupDebtRepository.HasActiveGroupDebt(request.GroupId);
        if (hasDebt) { throw new InvalidGroupActionException(); }

        if (group!.AvatarUrl != null)
        {
            _fileService.DeleteFile(group.AvatarUrl);
        }

        group.Delete();
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<GroupDetailsResponse> GetGroupDetailsAsync(int groupId, int userId)
    {
        var group = await _groupRepository.GetGroupInfoByIdAsync(groupId);
        if (group == null) throw new GroupNotFoundException();

        var currentMember = await _groupMemberRepository.GetMemberAsync(groupId, userId);
        if (currentMember == null) throw new GroupMemberNotFoundException();

        var members = await _groupMemberRepository.GetAllActiveGroupMembersAsync(groupId);
        var debts = await _groupDebtRepository.GetGroupDebtsByGroupIdAsync(groupId);
        var expenses = await _expenseRepository.GetExpensesByGroupIdAsync(groupId);

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var memberDtos = new List<GroupMemberBalanceDto>();
        foreach (var member in members)
        {
            var user = member.User;
            if (user == null) continue;

            var owedToUser = debts.Where(d => d.CreditorId == member.UserId).Sum(d => d.Amount);
            var userOwes = debts.Where(d => d.DebtorId == member.UserId).Sum(d => d.Amount);
            var overallBalance = owedToUser - userOwes;

            var isCreditorToCurrent = debts.Any(d =>
                d.CreditorId == member.UserId && d.DebtorId == userId && d.Amount > 0);

            var lines = expenses
                .Where(e => e.PayerId != member.UserId)
                .SelectMany(e => e.Items.Concat(e.Groups.SelectMany(g => g.Items)).SelectMany(i => i.Splits
                    .Where(s => s.UserId == member.UserId && e.Payer != null)
                    .Select(s => new { PayerId = e.PayerId, Payer = e.Payer!, Amount = s.OwedAmount })))
                .GroupBy(x => x.PayerId)
                .Select(g => new GroupMemberDebtLineDto
                {
                    CounterpartyName = ShortPersonLabel(g.First().Payer),
                    AvatarUrl = AvatarUrlHelper.BuildUserAvatarUrl(baseUrl!, g.First().Payer.AvatarUrl),
                    Amount = g.Sum(x => x.Amount),
                    MemberOwes = true
                })
                .OrderByDescending(l => l.Amount)
                .ToList();

            memberDtos.Add(new GroupMemberBalanceDto
            {
                UserId = member.UserId,
                FullName = $"{user.Name} {user.Surname}",
                AvatarUrl = AvatarUrlHelper.BuildUserAvatarUrl(baseUrl!, user.AvatarUrl),
                OverallBalance = overallBalance,
                IsCurrentUser = member.UserId == userId,
                IsCreditorToCurrentUser = isCreditorToCurrent,
                Lines = lines,
                LinesTotal = lines.Sum(l => l.Amount),
                Expenses = new List<MemberExpenseLineDto>(),
                ExpensesTotal = 0
            });
        }

        var expenseDtos = expenses
            .SelectMany(e => e.Items.Concat(e.Groups.SelectMany(g => g.Items)).Select(i => new ExpenseSummaryDto
            {
                ExpenseId = e.Id,
                ItemId = i.Id,
                Title = i.Name,
                PayerName = e.Payer != null ? $"{e.Payer.Name} {e.Payer.Surname}" : "Nieznany",
                TotalAmount = i.TotalPrice,
                Date = e.PurchasedAt
            }))
            .OrderByDescending(dto => dto.Date)
            .ToList();

        return new GroupDetailsResponse
        {
            GroupId = group.Id,
            GroupName = group.Name,
            UserRole = currentMember.Role.ToString(),

            Members = memberDtos.OrderByDescending(m => m.IsCurrentUser).ThenBy(m => m.FullName).ToList(),
            Expenses = expenseDtos
        };
    }

    private static string ShortPersonLabel(User u)
    {
        var name = (u.Name ?? string.Empty).Trim();
        var sur = (u.Surname ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(sur)) return name;
        return $"{name} {char.ToUpperInvariant(sur[0])}.";
    }
}
