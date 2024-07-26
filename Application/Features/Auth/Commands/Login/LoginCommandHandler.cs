using Application.Features.Auth.Rules.BusinessRules;
using Application.Repositories;
using AutoMapper;
using Domain.Entities.Identity;
using Infrastructure.JWT;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Application.Features.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommandRequest, LoginCommandResponse>
{
    private readonly AuthBusinessRules _authBusinessRules;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public LoginCommandHandler(AuthBusinessRules authBusinessRules, IUserRepository userRepository, IMapper mapper, ITokenService tokenService, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _authBusinessRules = authBusinessRules;
        _userRepository = userRepository;
        _mapper = mapper;
        _tokenService = tokenService;
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;

    }

    public async Task<LoginCommandResponse> Handle(LoginCommandRequest request, CancellationToken cancellationToken)
    {
        User? user = await _userRepository.GetUserByEmailAsync(request.Email);

        await _authBusinessRules.CheckUserExistsToLogin(request.Email);
        await _authBusinessRules.CheckPasswordToLogin(user, request.Password);

        IList<string> roles = await _userRepository.GetUserRolesAsync(user);

        JwtSecurityToken token = await _tokenService.CreateToken(user, roles);
        
        string refreshToken = _tokenService.GenerateRefreshToken();
        _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInDays"], out int refreshTokenValidityInDays);

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiredTime = DateTime.UtcNow.AddDays(refreshTokenValidityInDays);

        await _userRepository.UpdateUserAsync(user);

        string _token = new JwtSecurityTokenHandler().WriteToken(token);
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to true if using HTTPS
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddMinutes(1)
        };

        _httpContextAccessor.HttpContext.Response.Cookies.Append("JwtCookie", _token, cookieOptions);
        await _userRepository.SetAccessTokenAsync(user, _token);

        return new()
        {
            AccessToken = _token,
            RefreshToken = refreshToken,
            AccessTokenExpiredTime = token.ValidTo,
            RefreshTokenExpiredTime = user.RefreshTokenExpiredTime
        };
    }
}
