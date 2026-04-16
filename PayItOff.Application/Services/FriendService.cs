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

        var invite = Friend.Invite(
            user,
            friend!
        );

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _friendRepository.AddAsync(invite);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw new Exception("Błąd podczas wysłania zaproszenia. Spróbuj ponownie później.");
        }
    }

    public async Task AcceptInviteAsync(int userId, UpdateInviteRequest request)
    {
        var invitation = await _friendRepository.GetInviteByIdAsync(userId, request.InviteId);
        if (invitation == null) { throw new FriendInviteNotFoundException(); }

        invitation.Accept(userId);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _friendRepository.UpdateAsync(invitation);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw new Exception("Błąd podczas akceptacji zaproszenia. Spróbuj ponownie później.");
        }

    }

    public async Task DeclineInviteAsync(int userId, UpdateInviteRequest request)
    {
        var invitation = await _friendRepository.GetInviteByIdAsync(userId, request.InviteId);
        if (invitation == null) { throw new FriendInviteNotFoundException(); }

        invitation.Decline(userId);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _friendRepository.UpdateAsync(invitation);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw new Exception("Błąd podczas odrzucania zaproszenia. Spróbuj ponownie później.");
        }
    }

    public async Task RemoveFriendAsync(int userId, UpdateInviteRequest request)
    {
        var invitation = await _friendRepository.GetInviteByIdAsync(userId, request.InviteId);
        if (invitation == null) { throw new FriendInviteNotFoundException(); }

        invitation.Remove(userId);

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            await _friendRepository.UpdateAsync(invitation);

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw new Exception("Błąd podczas usuwania znajomego. Spróbuj ponownie później.");
        }
    }
}
