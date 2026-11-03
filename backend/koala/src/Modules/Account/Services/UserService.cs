using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Dtos;
using Microsoft.EntityFrameworkCore;
using koala.src.Shared.Account;
using System.Security.Claims;
using koala.src.Shared;

namespace koala.src.Modules.Account.Services
{
    public interface IUserService
    {
        public Task RegisterUserAsync(ClaimsPrincipal? claimsPrincipal, UserRegisterRequestDto requestDto);
        public Task<UserDto> GetUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id);
        public Task<UserDeclarationDto> GetUserDeclarationAsync(ClaimsPrincipal? claimsPrincipal, Guid id);
        public Task<UserDeclarationDto> UpdateUserDeclarationAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserDeclarationUpdateDto requestDto);
        public Task<(List<UserDto>, ApiPagination)> GetUsersAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, UserQueryDto userQueryDto);
        public Task<UserDto> UpdateUserRolesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserUpdateRolesDto request);
        public Task<UserDto> UpdateUserNamesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserChangeNamesDto request);
        public Task DeleteUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id);
        public Task CensorUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserCensorRequestDto requestDto);
        public Task UnCensorUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserCensorRequestDto requestDto);
    }
    public class UserService : IUserService
    {
        private readonly AccountDbContext _db;
        private readonly IEmailService _emailService;
        public UserService(AccountDbContext db, IEmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }
        public async Task RegisterUserAsync(ClaimsPrincipal? claimsPrincipal, UserRegisterRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (isAuthenticated && !isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Forbidden");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email);
            if (user != null && user.Verified == true)
            {
                throw new KoalaException(KoalaErrorCodes.UserAlreadyExists, "User with this email already exists");
            }

            DateTime timeNow = DateTime.UtcNow;

            if (user != null)
            {
                _db.Users.Remove(user);
                await _db.SaveChangesAsync();
            }

            user = new User
            {
                Id = Guid.CreateVersion7(),
                NameFirst = null,
                NameLast = null,
                Email = requestDto.Email,
                PasswordHash = null,
                AcceptedRodo = false,
                AcceptedRules = false,
                DeclaredAtLeast13 = false,
                DeclaredAtLeast18 = false,
                Censored = false,
                Verified = false,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                Version = 1,
                VerifiedAt = null
            };

            var roles = await _db.Roles.ToListAsync();


            string role_prefix = "";
            if (isOrganizationAdmin)
            {
                role_prefix = "ORGANIZATION_";
            }
            else
            {
                role_prefix = "TEAM_";
            }

            var validTeamRoleNames = roles
                .Where(r => r.Name.StartsWith(role_prefix, StringComparison.Ordinal))
                .Select(r => r.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var role in requestDto.UserRoles)
            {
                if (!validTeamRoleNames.Contains(role))
                {
                    throw new KoalaException(KoalaErrorCodes.UserInvalidRoles, "Specified role is not a valid user role");
                }
            }

            List<UserRole> userRoles = new List<UserRole>();

            foreach (var role in requestDto.UserRoles)
            {
                userRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roles.FirstOrDefault(r => r.Name == role).Id
                });
            }

            Link link = new Link
            {
                Id = Guid.CreateVersion7(),
                Token = Guid.NewGuid(),
                UserId = user.Id,
                Type = LinkTypes.TYPE_REGISTRATION,
                State = LinkStates.STATE_ACTIVE,
                CreatedAt = timeNow,
                UpdatedAt = timeNow,
                ExpiresAt = timeNow.AddDays(1.0f)
            };

            await _db.Users.AddAsync(user);
            await _db.UserRoles.AddRangeAsync(userRoles);
            await _db.Links.AddAsync(link);
            await _db.SaveChangesAsync();

            await _emailService.SendRegisterEmailAsync(user.Email, link.Token.ToString());
        }
        public async Task<UserDto> GetUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin && !isSelf)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }
            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();
            return new UserDto(user.Id, user.NameFirst!, user.NameLast!, user.Email, user.Censored, userRolesNames);

        }
        public async Task<UserDeclarationDto> GetUserDeclarationAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isOrganizationAdmin && !isSelf)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }
            return new UserDeclarationDto(user.Id, user.AcceptedRodo, user.AcceptedRules, user.DeclaredAtLeast13, user.DeclaredAtLeast18);
        }
        public async Task<UserDeclarationDto> UpdateUserDeclarationAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserDeclarationUpdateDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isSelf = userId == id;

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            if (!isSelf)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }

            var rolesRequiring18 = new[] { RoleNames.NAME_ORGANIZATION_ADMIN, RoleNames.NAME_ORGANIZATION_EDITOR, RoleNames.NAME_ORGANIZATION_REVIEWER, RoleNames.NAME_TEAM_GUARDIAN };

            bool requires18 = user.UserRoles.Any(r => rolesRequiring18.Contains(r.Role.Name));
            if (requires18 && !requestDto.DeclaredAtLeast18)
            {
                throw new KoalaException(KoalaErrorCodes.UserRoleAgeRequirementNotMet, "Your roles requires you to confirm you are at least 18 years old.");
            }

            bool isCaptain = user.UserRoles.Any(r => r.Role.Name == RoleNames.NAME_TEAM_CAPTAIN);
            if (isCaptain && !requestDto.DeclaredAtLeast13 && !requestDto.DeclaredAtLeast18)
            {
                throw new KoalaException(KoalaErrorCodes.UserRoleAgeRequirementNotMet, "Captains role requires you are at least 13 years old.");
            }
            if (user.Version != requestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Version does not match make sure u have fresh data");
            }

            user.DeclaredAtLeast13 = requestDto.DeclaredAtLeast13;
            user.DeclaredAtLeast18 = requestDto.DeclaredAtLeast18;
            user.UpdatedAt = DateTime.UtcNow;
            user.Version++;

            await _db.SaveChangesAsync();

            return new UserDeclarationDto(user.Id, user.AcceptedRodo, user.AcceptedRules, user.DeclaredAtLeast13, user.DeclaredAtLeast18);
        }
        public async Task<(List<UserDto>, ApiPagination)> GetUsersAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto, UserQueryDto userQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid claims principal missing user id");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "Dont have permission to do it");
            }

            var query = _db.Users
                .AsNoTracking()
                .Include(user => user.UserRoles)
                .ThenInclude(userRole => userRole.Role)
                .AsQueryable();

            //FITLER CHECKS
            if (!string.IsNullOrEmpty(userQueryDto.Email))
            {
                query = query.Where(u => u.Email == userQueryDto.Email);
            }

            if (!string.IsNullOrEmpty(userQueryDto.NameFirst))
            {
                query = query.Where(u => u.NameFirst == userQueryDto.NameFirst);
            }

            if (!string.IsNullOrEmpty(userQueryDto.NameLast))
            {
                query = query.Where(u => u.NameLast == userQueryDto.NameLast);
            }

            if (userQueryDto.ShowCensored != null)
            {
                query = query.Where(u => u.Censored == userQueryDto.ShowCensored);
            }

            if (userQueryDto.UserRoles != null)
            {
                query = query.Include(u => u.UserRoles).Where(u => u.UserRoles.Any(ur => userQueryDto.UserRoles.Contains(ur.Role.Name)));
            }

            var queryResults = await query.ToListAsync();
            List<UserDto> users = queryResults
                .Select(u => new UserDto
                (
                    u.Id,
                    u.NameFirst!,
                    u.NameLast!,
                    u.Email,
                    u.Censored,
                    u.UserRoles.Select(ur => ur.Role.Name).Distinct().ToList()
                ))
                .Skip(pageQueryDto.PageSize * pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();

            return (users, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
        public async Task<UserDto> UpdateUserRolesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserUpdateRolesDto request)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid claims principal missing user id");
            }

            if (!isOrganizationAdmin && !isSelf)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }

            if (user.Version != request.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Version does not match make sure u have fresh data");
            }

            var roles = await _db.Roles.ToListAsync();

            string role_prefix = isOrganizationAdmin ? "ORGANIZATION_" : "TEAM_";

            var validTeamRoleNames = roles
                .Where(r => r.Name.StartsWith(role_prefix, StringComparison.Ordinal))
                .Select(r => r.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var role in request.UserRoles)
            {
                if (!validTeamRoleNames.Contains(role))
                {
                    throw new KoalaException(KoalaErrorCodes.UserInvalidRoles, "Specified role is not a valid user role");
                }
            }

            var rolesRequiring18 = new[]
            {
                RoleNames.NAME_ORGANIZATION_ADMIN,
                RoleNames.NAME_ORGANIZATION_EDITOR,
                RoleNames.NAME_ORGANIZATION_REVIEWER,
                RoleNames.NAME_TEAM_GUARDIAN
            };

            bool requires18 = request.UserRoles.Any(r => rolesRequiring18.Contains(r));
            if (requires18 && !user.DeclaredAtLeast18)
            {
                throw new KoalaException(KoalaErrorCodes.UserRoleAgeRequirementNotMet, "Your selected roles require you to confirm you are at least 18 years old.");
            }

            bool isCaptain = request.UserRoles.Contains(RoleNames.NAME_TEAM_CAPTAIN);
            if (isCaptain && !user.DeclaredAtLeast13 && !user.DeclaredAtLeast18)
            {
                throw new KoalaException(KoalaErrorCodes.UserRoleAgeRequirementNotMet, "Captain role requires you to be at least 13 years old.");
            }

            await _db.UserRoles.Where(ur => ur.UserId == id).ExecuteDeleteAsync();
            List<UserRole> userRoles = new List<UserRole>();

            foreach (var role in request.UserRoles)
            {
                userRoles.Add(new UserRole
                {
                    UserId = user.Id,
                    RoleId = roles.FirstOrDefault(r => r.Name == role).Id
                });
            }

            await _db.UserRoles.AddRangeAsync(userRoles);
            user.UpdatedAt = DateTime.UtcNow;
            user.Version++;

            await _db.SaveChangesAsync();

            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();

            return new UserDto(user.Id, user.NameFirst!, user.NameLast!, user.Email, user.Censored, userRolesNames);
        }
        public async Task<UserDto> UpdateUserNamesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserChangeNamesDto request)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid claims principal missing user id");
            }

            if (!isOrganizationAdmin && !isSelf)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }

            if (user.Version != request.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Version does not match make sure u have fresh data");
            }

            user.NameFirst = request.NameFirst;
            user.NameLast = request.NameLast;
            user.UpdatedAt = DateTime.UtcNow;
            user.Version++;

            await _db.SaveChangesAsync();

            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();
            return new UserDto(user.Id, user.NameFirst!, user.NameLast!, user.Email, user.Censored, userRolesNames);
        }
        public async Task DeleteUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid claims principal missing user id");
            }

            if (!isOrganizationAdmin && !isSelf)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return;
        }
        public async Task CensorUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserCensorRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid claims principal missing user id");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }

            if (user.Version != requestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Version does not match make sure u have fresh data");
            }

            user.Censored = true;
            user.UpdatedAt = DateTime.UtcNow;
            user.Version++;
            await _db.SaveChangesAsync();
            return;
        }
        public async Task UnCensorUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserCensorRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if (!isAuthenticated)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "User is not logged in.");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if (userId == Guid.Empty)
            {
                throw new KoalaException(KoalaErrorCodes.Unauthenticated, "Invalid claims principal missing user id");
            }

            if (!isOrganizationAdmin)
            {
                throw new KoalaException(KoalaErrorCodes.Forbidden, "User does not have permission to perform this operation.");
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if (user == null)
            {
                throw new KoalaException(KoalaErrorCodes.UserNotFound, "User does not exist");
            }

            if (user.Version != requestDto.Version)
            {
                throw new KoalaException(KoalaErrorCodes.ResourceVersionConflict, "Version does not match make sure u have fresh data");
            }

            user.Censored = false;
            user.UpdatedAt = DateTime.UtcNow;
            user.Version++;
            await _db.SaveChangesAsync();
            return;
        }
    }
}
