﻿using Application.Exceptions.CustomExceptions;
using Application.Features.Auth.Rules.BusinessRules;
using Application.Repositories;
using AutoMapper;
using Domain.Entities.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommandRequest, RegisterCommandResponse>
{
    private readonly AuthBusinessRules _authBusinessRules;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;
    private RoleManager<Role> _roleManager;

    public RegisterCommandHandler(AuthBusinessRules authBusinessRules, IUserRepository userRepository, IMapper mapper, RoleManager<Role> roleManager)
    {
        _authBusinessRules = authBusinessRules;
        _userRepository = userRepository;
        _mapper = mapper;
        _roleManager = roleManager;

    }

    public async Task<RegisterCommandResponse> Handle(RegisterCommandRequest request, CancellationToken cancellationToken)
    {
        await _authBusinessRules.CheckUserExistsToRegister(request.Email);
        //_authBusinessRules.CheckPasswordToRequiredParameters(request.Password);
        User user = _mapper.Map<User>(request);
        user.UserName = request.Email;

        var result = await _userRepository.CreateUserAsync(user,request.Password);
        if (result.Succeeded)
        {
            if (!await _roleManager.RoleExistsAsync("user"))
            {
                await _roleManager.CreateAsync(new Role { Name="user",NormalizedName="USER",ConcurrencyStamp=""});
            }
            await _userRepository.AddRolesToUserAsync(user, new[] { "user" });
            var response = _mapper.Map<RegisterCommandResponse>(user);
            return response;
        }
        throw new BusinessException(result.Errors.ToString());
    }
}
