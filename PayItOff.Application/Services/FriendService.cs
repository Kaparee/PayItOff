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
            AvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(baseUrl, data.Friend!.AvatarUrl),
            Name = data.Friend.Name,
            Surname = data.Friend.Surname,
            Nickname = data.Friend.Nickname,
            PhoneNumber = data.Friend.PhoneNumber,
            Balance = data.Balance,
            Income = data.Income,
            Expense = data.Expense,
            SharedGroups = data.SharedGroups
                .Select(group => new SharedGroupResponse
                {
                    GroupId = group.GroupId,
                    Name = group.Name,
                    AvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildGroupAvatarUrl(baseUrl, group.AvatarUrl)
                })
                .ToList()
        }).ToList();

        return response;
    }

    public async Task InviteAsync(int userId, FriendInviteRequest request)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        User? friend = null;

        if (request.TargetUserId.HasValue)
        {
            friend = await _userRepository.GetUserByIdAsync(request.TargetUserId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(request.Nickname))
        {
            friend = await _userRepository.GetUserByNicknameAsync(request.Nickname.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(request.Email))
        {
            friend = await _userRepository.GetUserByEmailAsync(request.Email.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
        {
            friend = await _userRepository.GetUserByPhoneNumberAsync(request.PhoneNumber.Trim());
        }

        if (friend is null) { throw new UserNotFoundException(); }
        if (friend.Id == userId) { throw new System.Exception("Nie możesz zaprosić samego siebie."); }

        var isExists = await _friendRepository.IsFriendInviteExistAsync(userId, friend.Id);
        if (isExists == true) { throw new FriendInvitationAlreadyExistsException(); }

        var existingFriendship = await _friendRepository.GetUsersFriendshipAsync(userId, friend.Id);

        if (existingFriendship is not null)
        {
            existingFriendship.ReInvite();
            await _friendRepository.UpdateAsync(existingFriendship);
        }
        else
        {
            var invite = Friend.Invite(
                user,
                friend
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
                AvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(baseUrl, targetUser.AvatarUrl),
                Name = targetUser.Name,
                Surname = targetUser.Surname,
                Nickname = targetUser.Nickname,
                SentAt = x.SentAt,
                IsIncoming = !isInviter
            };
        }).ToList();
    }

    public async Task<SearchUserResponse?> SearchUserAsync(string? nickname, string? email, string? phoneNumber)
    {
        User? friend = null;

        if (!string.IsNullOrWhiteSpace(nickname))
        {
            friend = await _userRepository.GetUserByNicknameAsync(nickname.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(email))
        {
            friend = await _userRepository.GetUserByEmailAsync(email.Trim());
        }
        else if (!string.IsNullOrWhiteSpace(phoneNumber))
        {
            friend = await _userRepository.GetUserByPhoneNumberAsync(phoneNumber.Trim());
        }

        if (friend is null) return null;

        var baseUrl = _configuration["AppUrls:BackendUrl"];
        return new SearchUserResponse
        {
            Id = friend.Id,
            AvatarUrl = PayItOff.Application.Helpers.AvatarUrlHelper.BuildUserAvatarUrl(baseUrl, friend.AvatarUrl),
            Name = friend.Name,
            Surname = friend.Surname,
            Nickname = friend.Nickname
        };
    }
}
