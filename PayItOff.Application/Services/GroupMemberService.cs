using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

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

        var invite = GroupMember.Invite(
            user,
            group!,
            request.Role
        );
        await _groupMemberRepository.AddAsync(invite);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitAsync();
    }
}