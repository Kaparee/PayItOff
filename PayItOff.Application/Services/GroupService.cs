using FluentValidation;
using Microsoft.Extensions.Configuration;
using PayItOff.Application.Interfaces;
using PayItOff.Domain.Entities;
using PayItOff.Domain.Exceptions;
using PayItOff.Domain.Interfaces;
using PayItOff.Shared.Requests;
using PayItOff.Shared.Responses;

namespace PayItOff.Application.Services;

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateGroupRequest> _validator;
    private readonly IConfiguration _configuration;

    public GroupService(IGroupRepository groupRepository, IUserRepository userRepository, IGroupMemberRepository groupMemberRepository, IUnitOfWork unitOfWork, IValidator<CreateGroupRequest> validator, IJWTService jwtService, IConfiguration configuration)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _groupMemberRepository = groupMemberRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _configuration = configuration;
    }
    public async Task CreateAsync(CreateGroupRequest request, int userId)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        if (user is null) { throw new UserNotFoundException(); }

        var validationResult = await _validator.ValidateAsync(request);
        if (!validationResult.IsValid) throw new ValidationException(validationResult.Errors);

        var group = Group.Create(
            request.Name,
            request.AvatarUrl
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

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw new Exception("Błąd podczas tworzenia grupy. Spróbuj ponownie później.");
        }
    }

    public async Task<List<GroupInfoResponse>> GetUserGroupsAsync(int userId)
    {
        var groups = await _groupRepository.GetUserGroupsAsync(userId);

        var response = groups.Select(groups => new GroupInfoResponse
        {
            Name = groups!.Name,
            AvatarUrl = groups.AvatarUrl,
            UpdatedAt = groups.UpdatedAt
        }).ToList();

        return response;
    }
}
