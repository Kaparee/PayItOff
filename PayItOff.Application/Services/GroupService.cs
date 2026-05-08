using FluentValidation;
using Humanizer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
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
                AvatarUrl = $"{baseUrl}/avatars/{member.Group.AvatarUrl ?? "default-group-avatar.png"}",
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
                Id = group.Id,
                Name = group.Group!.Name,
                AvatarUrl = $"{baseUrl}/avatars/{group.Group.AvatarUrl ?? "default-group-avatar.png"}",
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
}
