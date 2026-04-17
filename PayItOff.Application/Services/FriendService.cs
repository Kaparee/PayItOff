using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Services;

public class FriendService : IFriendService
{
    private readonly IFriendRepository _friendRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public FriendService(IFriendRepository friendRepository, IUserRepository userRepository, IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _friendRepository = friendRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<List<FriendListResponse>> GetUserFriendListAsync(int userId)
    {
        var friendsData = await _friendRepository.GetUserFriendListAsync(userId);

        var baseUrl = _configuration["AppUrls:BackendUrl"];
        var response = friendsData.Select(data => new FriendListResponse
        {
            FriendId = data.Friend!.Id,
            InviteId = data.InviteId,
            AvatarUrl = data.Friend!.AvatarUrl!,
            Name = data.Friend.Name,
            Surname = data.Friend.Surname,
            Nickname = data.Friend.Nickname,
            PhoneNumber = data.Friend.PhoneNumber,
            Balance = Math.Round(data.Balance, 2),
            SharedGroupAvatars = data.SharedAvatars
                .Select(avatar => $"{baseUrl}/avatars/{avatar ?? "default-avatar.png"}")
                .ToList()
        }).ToList();

        return response;
    }

    public async Task InviteAsync(int userId, FriendInviteRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var friend = await _userRepository.GetUserByIdAsync(request.TargetUserId);
        if (friend is null) { throw new UserNotFoundException(); }

        var isExists = await _friendRepository.IsFriendInviteExistAsync(userId, friend.Id);
        if (isExists == true) { throw new FriendInvitationAlreadyExistsException(); }


        var existingFriendship = await _friendRepository.GetUsersFriendshipAsync(userId, request.TargetUserId);

        if (existingFriendship is not null)
        {

            existingFriendship.ReInvite(user, friend);
            await _friendRepository.UpdateAsync(existingFriendship);
        }
        else
        {
            var invite = Friend.Invite(
                user,
                friend!
            );
            await _friendRepository.AddAsync(invite);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task AcceptInviteAsync(int userId, UpdateInviteRequest request)
    {
        var invitation = await _friendRepository.GetInviteByIdAsync(userId, request.InviteId);
        if (invitation == null) { throw new FriendInviteNotFoundException(); }

        invitation.Accept(userId);

        await _friendRepository.UpdateAsync(invitation);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task DeclineInviteAsync(int userId, UpdateInviteRequest request)
    {
        var invitation = await _friendRepository.GetInviteByIdAsync(userId, request.InviteId);
        if (invitation == null) { throw new FriendInviteNotFoundException(); }

        invitation.Decline(userId);

        await _friendRepository.UpdateAsync(invitation);

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task RemoveFriendAsync(int userId, UpdateInviteRequest request)
    {
        var invitation = await _friendRepository.GetInviteByIdAsync(userId, request.InviteId);
        if (invitation == null) { throw new FriendInviteNotFoundException(); }

        invitation.Remove(userId);

        await _friendRepository.UpdateAsync(invitation);

        await _unitOfWork.SaveChangesAsync();

    }

    public async Task<List<FriendPendingInvitationResponse>> GetPendingInvitationsAsync(int userId)
    {
        var invitations = await _friendRepository.GetPendingInvitationsByUserIdAsync(userId);
        var baseUrl = _configuration["AppUrls:BackendUrl"];

        return invitations.Select(x =>
        {
            bool isInviter = x.InviterId == userId;

            var targetUser = isInviter ? x.Receiver! : x.Inviter!;

            return new FriendPendingInvitationResponse
            {
                InvitationId = x.Id,
                FriendId = targetUser.Id,
                AvatarUrl = $"{baseUrl}/avatars/{targetUser.AvatarUrl ?? "default-avatar.png"}",
                Name = targetUser.Name,
                Surname = targetUser.Surname,
                Nickname = targetUser.Nickname,
                SentAt = x.SentAt,
                IsIncoming = !isInviter
            };
        }).ToList();
    }
}
