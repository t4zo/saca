﻿using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SACA.Constants;
using SACA.Entities.Identity;
using SACA.Entities.Requests;
using SACA.Entities.Responses;
using SACA.Extensions;
using SACA.Interfaces;
using System.Security.Claims;

namespace SACA.Services
{
    public class UserService : IUserService
    {
        private readonly MapperlyMapper _mapper;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserService(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, ITokenService tokenService, MapperlyMapper mapper)
        {
            _tokenService = tokenService;
            _userManager = userManager;
            _signInManager = signInManager;
            _mapper = mapper;
        }

        public async Task<ApplicationUser> SignUpAsync(SignUpRequest signUpRequest)
        {
            var user = new ApplicationUser
            {
                UserName = signUpRequest.UserName,
                Email = signUpRequest.Email
            };

            var result = await _userManager.CreateAsync(user, signUpRequest.Password);
            if (!result.Succeeded)
            {
                throw new ArgumentException(result.Errors.First().Description);
            }

            return user;
        }

        public async Task<ApplicationUser> AssignRolesAsync(ApplicationUser user, ICollection<string> roles)
        {
            var allRoles = typeof(AuthorizationConstants.Roles).GetAllPublicConstantValues<string>();

            foreach (var role in roles)
            {
                if (allRoles.Contains(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }
            }

            await _userManager.AddToRoleAsync(user, AuthorizationConstants.Roles.User);

            return user;
        }

        public async Task<UserResponse> LogInAsync(string username, string password, bool remember)
        {
            var user = await _userManager.FindByNameAsync(username) ?? await _userManager.FindByEmailAsync(username);
            if (user is null)
            { 
                throw new ArgumentException("Usuário e/ou senha inválido(s)");
            }

            var result = await _signInManager.PasswordSignInAsync(user.UserName, password, remember, false);
            if (!result.Succeeded)
            {
                throw new ArgumentException("Usuário e/ou senha inválido(s)");
            }

            //var userClaims = await _userManager.GetClaimsAsync(user);
            var roles = await _userManager.GetRolesAsync(user);

            var claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(AuthorizationConstants.Remember, remember.ToString().ToLower())
            });

            claimsIdentity.AddClaims(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var token = _tokenService.GenerateJWTToken(claimsIdentity);

            var userResponse = _mapper.MapToUserResponse(user);

            userResponse.Roles = roles;
            userResponse.Token = token;

            return userResponse;
        }

        public async Task<string> GetUserNameAsync(int userId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            return user.UserName;
        }

        public async Task<IdentityResult> DeleteAsync(int userId)
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(x => x.Id == userId);
            return await _userManager.DeleteAsync(user);
        }

        public async Task<IdentityResult> DeleteAsync(ApplicationUser user)
        {
            return await _userManager.DeleteAsync(user);
        }

        public async Task<ICollection<string>> GetRolesAsync(ApplicationUser user)
        {
            return await _userManager.GetRolesAsync(user);
        }
    }
}