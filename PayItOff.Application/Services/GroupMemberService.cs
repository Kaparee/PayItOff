using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Org.BouncyCastle.Asn1.Ocsp;
using Org.BouncyCastle.Asn1.X509;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Enums;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;
using System.Data;
using System.Numerics;
using System.Xml.Linq;

namespace PayItOff.Application.Services;

public class GroupMemberService : IGroupMemberService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public GroupMemberService(IGroupRepository groupRepository, IUserRepository userRepository, IGroupMemberRepository groupMemberRepository, IUnitOfWork unitOfWork, IJWTService jwtService, IConfiguration configuration)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _groupMemberRepository = groupMemberRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task InviteUserAsync(GroupInviteUserRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(request.UserId);
        if (user is null) { throw new UserNotFoundException(); }

        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
        if(group is null) { throw new GroupNotFoundException(); }

        var existingMember = await _groupMemberRepository.GetMemberAsync(request.GroupId, request.UserId);

        if (existingMember is not null)
        {
            if (existingMember.Status == GroupMemberStatus.Accepted || existingMember.Status == GroupMemberStatus.Pending)
            {
                throw new FriendInvitationAlreadyExistsException();
            }

            existingMember.ReInvite(request.Role);
            await _groupMemberRepository.UpdateAsync(existingMember);
        }
        else
        {
            var invite = GroupMember.Invite(
                user,
                group!,
                request.Role
            );
            await _groupMemberRepository.AddAsync(invite);
        }

        await _unitOfWork.SaveChangesAsync();

    }

    public async Task AcceptInviteAsync(int userId, int invitationId)
    {
        var invitation = await _groupMemberRepository.GetActiveInvitationById(invitationId);
        if (invitation is null) { throw new InvitationNotFoundException(); }

        if (userId != invitation.UserId) { throw new InvalidUserInvitationException(); }

        invitation.Accept();

        await _groupMemberRepository.UpdateAsync(invitation);
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task DeclineInviteAsync(int userId, int invitationId)
    {
        var invitation = await _groupMemberRepository.GetActiveInvitationById(invitationId);
        if (invitation is null) { throw new InvitationNotFoundException(); }

        if (userId != invitation.UserId) { throw new InvalidUserInvitationException(); }

        invitation.Decline();

        await _groupMemberRepository.UpdateAsync(invitation);
        await _unitOfWork.SaveChangesAsync();

    }
    public async Task UpdateRoleAsync(int userId, GroupMemberUpdateRequest request)
    {
        var actor = await _groupMemberRepository.GetMemberAsync(request.GroupId, userId);
        if (actor is null || actor.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        var targetUser = await _groupMemberRepository.GetMemberAsync(request.GroupId, request.TargetUserId);
        if (targetUser is null || targetUser.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        if(actor.Role == GroupMemberRole.Member) { throw new InvalidUserRoleException(); }
        if(actor.Role == GroupMemberRole.Admin && targetUser!.Role == GroupMemberRole.Owner) { throw new InvalidUserRoleException(); }
        if(userId == request.TargetUserId) { throw new InvalidUserRoleException(); }

        if (actor.Role == GroupMemberRole.Admin && request.NewRole == GroupMemberRole.Owner) { throw new InvalidUserRoleException(); }

        targetUser!.UpdateRole(request.NewRole);

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
        if(user.Role == GroupMemberRole.Owner) { throw new InvalidUserRoleException(); }

        user.Leave();

        await _groupMemberRepository.UpdateAsync(user);
        await _unitOfWork.SaveChangesAsync();
    }
    public async Task KickUserFromGroupAsync(int userId, int groupId, int targetUserId)
    {
        var actor = await _groupMemberRepository.GetMemberAsync(groupId, userId);
        if (actor is null || actor.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        var targetUser = await _groupMemberRepository.GetMemberAsync(groupId, targetUserId);
        if (targetUser is null || targetUser.Status != GroupMemberStatus.Accepted) { throw new GroupMemberNotFoundException(); }

        if(actor.Role == GroupMemberRole.Member) { throw new InvalidUserRoleException(); }
        if (actor.Role == GroupMemberRole.Admin && targetUser.Role != GroupMemberRole.Member) { throw new InvalidUserRoleException(); }
        if (userId == targetUserId) { throw new InvalidUserRoleException(); }

        targetUser.Kick();
        await _groupMemberRepository.UpdateAsync(targetUser);
        await _unitOfWork.SaveChangesAsync();
    }

    public async Task<List<PendingInvitationResponse>> GetPendingInvitationsAsync(int userId)
    {
        var invitations = await _groupMemberRepository.GetPendingInvitationsByUserIdAsync(userId);

        return invitations.Select(x => new PendingInvitationResponse
        {
            InvitationId = x.Id,
            GroupId = x.GroupId,
            GroupName = x.Group!.Name,
            GroupAvatarUrl = x.Group.AvatarUrl ?? string.Empty,
            Role = x.Role,
            InvitedAt = x.InvitedAt
        }).ToList();
    }

    public async Task<List<GroupMemberResponse>> GetAllActiveGroupMembersAsync(int groupId)
    {
        var members = await _groupMemberRepository.GetAllActiveGroupMembersAsync(groupId);

        return members.Select(x => new GroupMemberResponse
        {
            AvatarUrl = x.User!.AvatarUrl ?? string.Empty,
            Name = x.User.Name,
            Surname = x.User.Surname,
            Email = x.User.Email,
            Nickname = x.User.Nickname,
            Role = x.Role
        }).ToList();
    }
}