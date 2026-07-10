using Microsoft.Extensions.Configuration;
using PayItOff.Application.Helpers;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Data;

namespace PayItOff.Application.Services;

public class GroupMemberService : IGroupMemberService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly INotificationService _notificationService;

    public GroupMemberService(IGroupRepository groupRepository, IUserRepository userRepository, IGroupMemberRepository groupMemberRepository, IUnitOfWork unitOfWork, IJWTService jwtService, IConfiguration configuration, INotificationService notificationService)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _groupMemberRepository = groupMemberRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _notificationService = notificationService;
    }

    public async Task InviteUserAsync(int userId, GroupInviteUserRequest request)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var user = await _userRepository.GetUserByIdAsync(request.UserId);
            if (user is null) { throw new UserNotFoundException(); }

            var inviter = await _userRepository.GetUserByIdAsync(userId);
            if (inviter == null) { throw new UserNotFoundException(); }

            var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
            if (group is null) { throw new GroupNotFoundException(); }

            var existingMember = await _groupMemberRepository.GetMemberAsync(request.GroupId, request.UserId);

            int notificationEntityId;

            if (existingMember is not null)
            {
                if (existingMember.Status == GroupMemberStatus.Accepted || existingMember.Status == GroupMemberStatus.Pending)
                {
                    throw new FriendInvitationAlreadyExistsException();
                }

                existingMember.ReInvite(request.Role);
                await _groupMemberRepository.UpdateAsync(existingMember);
                await _unitOfWork.SaveChangesAsync();
                notificationEntityId = existingMember.Id;
            }
            else
            {
                var invite = GroupMember.Invite(
                    user,
                    group!,
                    request.Role
                );
                await _groupMemberRepository.AddAsync(invite);
                await _unitOfWork.SaveChangesAsync();
                notificationEntityId = invite.Id;
            }

            await _notificationService.CreateNotificationAsync(request.UserId, userId, NotificationType.NeedAction, $"Użytkownik {inviter.FullName} zaprosił Cię do grupy: '{group.Name}'", notificationEntityId, EntityType.GroupMembers);

            group!.UpdateTimestamp();
            await _groupRepository.UpdateAsync(group);
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }

    public async Task AcceptInviteAsync(int userId, int invitationId)
    {
        var invitation = await _groupMemberRepository.GetActiveInvitationById(invitationId);
        if (invitation is null) { throw new InvitationNotFoundException(); }

        if (userId != invitation.UserId) { throw new InvalidUserInvitationException(); }

        invitation.Accept();
        invitation.Group?.UpdateTimestamp();

        if (invitation.Group != null)
        {
            await _groupRepository.UpdateAsync(invitation.Group);
        }

        await _groupMemberRepository.UpdateAsync(invitation);

        await _notificationService.ResolveActionNotificationAsync(userId, invitationId, EntityType.GroupMembers, true);

        await _unitOfWork.SaveChangesAsync();
    }
    public async Task DeclineInviteAsync(int userId, int invitationId)
    {
        var invitation = await _groupMemberRepository.GetActiveInvitationById(invitationId);
        if (invitation is null) { throw new InvitationNotFoundException(); }

        if (userId != invitation.UserId) { throw new InvalidUserInvitationException(); }

        invitation.Decline();

        await _groupMemberRepository.UpdateAsync(invitation);

        await _notificationService.ResolveActionNotificationAsync(userId, invitationId, EntityType.GroupMembers, false);

        await _unitOfWork.SaveChangesAsync();

    }
    public async Task UpdateRoleAsync(int userId, GroupMemberUpdateRequest request)
    {
        var actor = await _groupMemberRepository.GetMemberAsync(request.GroupId, userId);
        if (actor is null || actor.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        var targetUser = await _groupMemberRepository.GetMemberAsync(request.GroupId, request.TargetUserId);
        if (targetUser is null || targetUser.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
        if (group == null) { throw new GroupNotFoundException(); }

        if (actor.Role == GroupMemberRole.Member) { throw new InvalidUserRoleException(); }
        if (actor.Role == GroupMemberRole.Admin && targetUser!.Role == GroupMemberRole.Owner) { throw new InvalidUserRoleException(); }
        if (userId == request.TargetUserId) { throw new InvalidUserRoleException(); }

        if (actor.Role == GroupMemberRole.Admin && request.NewRole == GroupMemberRole.Owner) { throw new InvalidUserRoleException(); }

        if (request.NewRole == GroupMemberRole.Owner && actor.Role == GroupMemberRole.Owner)
        {
            actor.UpdateRole(GroupMemberRole.Admin);
            await _groupMemberRepository.UpdateAsync(actor);
        }

        targetUser!.UpdateRole(request.NewRole);
        targetUser.Group?.UpdateTimestamp();

        if (targetUser.Group != null)
        {
            await _groupRepository.UpdateAsync(targetUser.Group);
        }

        await _notificationService.CreateNotificationAsync(targetUser.UserId, userId, NotificationType.Normal, $"Twoja rola w grupie '{group.Name}' została zmieniona przez {actor.User!.FullName} na {request.NewRole.ToString()}", request.GroupId, EntityType.Groups);
        await _groupMemberRepository.UpdateAsync(targetUser);
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task SetGroupAsFavoriteAsync(int userId, int groupId)
    {
        var user = await _groupMemberRepository.GetMemberAsync(groupId, userId);
        if (user is null || user.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        user.ToggleFavorite();

        await _groupMemberRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task LeaveGroupAsync(int userId, int groupId)
    {
        var user = await _groupMemberRepository.GetMemberAsync(groupId, userId);
        if (user is null || user.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }
        if (user.Role == GroupMemberRole.Owner) { throw new InvalidUserRoleException(); }

        user.Leave();
        user.Group?.UpdateTimestamp();

        if (user.Group != null)
        {
            await _groupRepository.UpdateAsync(user.Group);
        }

        await _groupMemberRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task KickUserFromGroupAsync(int userId, int groupId, int targetUserId)
    {
        var actor = await _groupMemberRepository.GetMemberAsync(groupId, userId);
        if (actor is null || actor.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        var group = await _groupRepository.GetGroupInfoByIdAsync(groupId);
        if (group == null) { throw new GroupNotFoundException(); }

        var targetUser = await _groupMemberRepository.GetMemberAsync(groupId, targetUserId);
        if (targetUser is null || targetUser.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        if (actor.Role == GroupMemberRole.Member) { throw new InvalidUserRoleException(); }
        if (actor.Role == GroupMemberRole.Admin && targetUser.Role != GroupMemberRole.Member) { throw new InvalidUserRoleException(); }
        if (userId == targetUserId) { throw new InvalidUserRoleException(); }

        targetUser.Kick();
        targetUser.Group?.UpdateTimestamp();

        if (targetUser.Group != null)
        {
            await _groupRepository.UpdateAsync(targetUser.Group);
        }

        await _notificationService.CreateNotificationAsync(targetUserId, userId, NotificationType.Deleting, $"{actor.User!.FullName} usunął Cię z grupy {group.Name}", groupId, EntityType.GroupMembers);

        await _groupMemberRepository.UpdateAsync(targetUser);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<GroupPendingInvitationResponse>> GetPendingInvitationsAsync(int userId)
    {
        var invitations = await _groupMemberRepository.GetPendingInvitationsByUserIdAsync(userId);

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        return invitations.Select(x => new GroupPendingInvitationResponse
        {
            InvitationId = x.Id,
            GroupId = x.GroupId,
            GroupName = x.Group!.Name,
            GroupAvatarUrl = UrlHelper.BuildGroupAvatarUrl(baseUrl!, x.Group.AvatarUrl),
            Role = x.Role,
            InvitedAt = x.InvitedAt
        }).ToList();
    }

    public async Task<List<GroupMemberResponse>> GetAllActiveGroupMembersAsync(int groupId)
    {
        var members = await _groupMemberRepository.GetAllActiveGroupMembersAsync(groupId);

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        return members.Select(x => new GroupMemberResponse
        {
            UserId = x.UserId,
            GroupMemberId = x.Id,
            AvatarUrl = UrlHelper.BuildUserAvatarUrl(baseUrl!, x.User!.AvatarUrl),
            Name = x.User.Name,
            Surname = x.User.Surname,
            Email = x.User.Email,
            Nickname = x.User.Nickname,
            Role = x.Role
        }).ToList();
    }

    public async Task<bool> IsInviteAlreadyExistsAsync(int groupId, int userId)
    {
        return await _groupMemberRepository.IsInviteAlreadyExistsAsync(groupId, userId);
    }
}
