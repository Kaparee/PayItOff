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

public class GroupService : IGroupService
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUserRepository _userRepository;
    private readonly IGroupMemberRepository _groupMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IValidator<CreateGroupRequest> _validator;
    private readonly IConfiguration _configuration;
    private readonly IFileService _fileService;

    public GroupService(IGroupRepository groupRepository, IUserRepository userRepository, IGroupMemberRepository groupMemberRepository, IUnitOfWork unitOfWork, IValidator<CreateGroupRequest> validator, IJWTService jwtService, IConfiguration configuration, IFileService fileService)
    {
        _groupRepository = groupRepository;
        _userRepository = userRepository;
        _groupMemberRepository = groupMemberRepository;
        _unitOfWork = unitOfWork;
        _validator = validator;
        _fileService = fileService;
        _configuration = configuration;
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

        var baseUrl = _configuration["AppUrls:BackendUrl"];

        var response = groups.Select(groups => new GroupInfoResponse
        {
            Id = groups!.Id,
            Name = groups!.Name,
            AvatarUrl = $"{baseUrl}/avatars/{groups.AvatarUrl ?? "default-avatar.png"}",
            UpdatedAt = groups.UpdatedAt
        }).ToList();

        return response;
    }

    public async Task EditGroupInfoAsync(int userId, EditGroupInfoRequest request, IFormFile? avatar)
    {
        var user = await _userRepository.GetUserByIdAsync(userId);
        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
        var isOwnerOrAdmin = await _groupMemberRepository.IsUserOwnerOrAdmin(userId, request.GroupId);
        if (!isOwnerOrAdmin) { throw new InvalidUserRoleException();  }

        var savedFileName = await _fileService.SaveAvatarAsync(avatar);

        group!.Edit(request.NewName, savedFileName);
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitAsync();
    }

    public async Task DeleteGroupAsync(int userId, DeleteGroupRequest request)
    {
        // TU MUSZE DODAĆ WALIDACJE DLA SPRAWDZENIA CZY "BUDŻET" KONTA JEST RÓWNY ZERO
        var user = await _userRepository.GetUserByIdAsync(userId);
        var group = await _groupRepository.GetGroupInfoByIdAsync(request.GroupId);
        var isOwner = await _groupMemberRepository.IsUserOwner(userId, request.GroupId);
        if (!isOwner) { throw new InvalidUserRoleException(); }

        group!.Delete();
        await _groupRepository.UpdateAsync(group);
        await _unitOfWork.SaveChangesAsync();
        await _unitOfWork.CommitAsync();
    }
}
