using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Dtos;
using Microsoft.EntityFrameworkCore;
using koala.src.Shared.Account;
using System.Security.Claims;
using koala.src.Shared;

namespace koala.src.Modules.Account.Services
{
    public class UserService
    {
        private readonly AccountDbContext _db;
        private readonly EmailService _emailService;
        public UserService(AccountDbContext db, EmailService emailService)
        {
            _db = db;
            _emailService = emailService;
        }
        public async Task RegisterUserAsync(ClaimsPrincipal? claimsPrincipal, RegisterUserRequestDto requestDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(isAuthenticated && !isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Forbiden");
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == requestDto.Email);
            if(user != null && user.Verified == true)
            {
                throw new AccountException(AccountErrorCodes.UserAlreadyExists,"User with this email already exists");
            }
            
            DateTime timeNow = DateTime.UtcNow;

            if(user != null)
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
                Censored = false,
                Verified = false,
                CreatedAt = timeNow,
                VerifiedAt = null
            };            

            var roles = await _db.Roles.ToListAsync();

            
            string role_prefix ="";
            if(isOrganizationAdmin)
            {
                role_prefix="ORGANIZATION_";
            }
            else
            {
                role_prefix="TEAM_";
            }

            var validTeamRoleNames = roles
                .Where(r => r.Name.StartsWith(role_prefix, StringComparison.Ordinal))
                .Select(r => r.Name)
                .ToHashSet(StringComparer.Ordinal);

            foreach (var role in requestDto.Roles)
            {
                if (!validTeamRoleNames.Contains(role))
                {
                    throw new AccountException(AccountErrorCodes.IncorectRoles,"Specified role is not a team user role");
                }
            }

            List<UserRole> userRoles = new List<UserRole>();

            foreach (var role in requestDto.Roles)
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
                Type = "REGISTER",
                Active = true,
                CreatedAt = timeNow,
                ExpiresAt = timeNow.AddDays(1.0f)
            };

            await _db.Users.AddAsync(user);
            await _db.UserRoles.AddRangeAsync(userRoles);
            await _db.Links.AddAsync(link);
            await _db.SaveChangesAsync();
            
            //FIXME: SEND AN EMAIL WITH A VERIFY LINK TO FINISH THE ACCOUNT CREATION
            await _emailService.SendRegisterEmailAsync(user.Email, link.Token.ToString());
        }
        public async Task<UserDto> GetUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if(userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            if(!isOrganizationAdmin && !isSelf)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Dont havepermission to do it");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"User does not exist");
            }
            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();
            return new UserDto(user.Id,user.NameFirst!,user.NameLast!,user.Email,user.Censored,userRolesNames);

        }
        public async Task<UserListDto> GetUsersAsync(ClaimsPrincipal? claimsPrincipal, PageQueryDto pageQueryDto)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid claims principal missing user id");
            }

            if(!isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Dont havepermission to do it");
            }

            var queryResults = await _db.Users.Where(u => u.Verified == true && u.Id != userId).AsNoTracking().AsQueryable().ToListAsync();
            List<UserDto> users = queryResults
                .Select(u => new UserDto
                (
                    u.Id,
                    u.NameFirst!,
                    u.NameLast!,
                    u.Email,
                    u.Censored,
                    u.UserRoles.Select(ur=> ur.Role.Name).Distinct().ToList()
                ))
                .Skip(pageQueryDto.PageSize* pageQueryDto.PageNumber)
                .Take(pageQueryDto.PageSize)
                .ToList();
            
            return new UserListDto(users, new ApiPagination(pageQueryDto.PageNumber, pageQueryDto.PageSize, queryResults.Count));
        }
        public async Task<UserDto> UpdateUserNamesAsync(ClaimsPrincipal? claimsPrincipal, Guid id, UserChangeNamesDto request)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if(userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid claims principal missing user id");
            }

            if(!isOrganizationAdmin && !isSelf)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Dont havepermission to do it");
            }
            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"User does not exist");
            }

            user.NameFirst = request.NameFirst;
            user.NameLast = request.NameLast;

            await _db.SaveChangesAsync();

            var userRolesNames = await _db.UserRoles
                .AsNoTracking()
                .Where(ur => ur.UserId == user.Id)
                .Select(ur => ur.Role.Name)
                .Distinct()
                .ToListAsync();
            return new UserDto(user.Id,user.NameFirst!,user.NameLast!,user.Email,user.Censored,userRolesNames);
        }
        public async Task DeleteUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);
            bool isSelf = userId == id;

            if(userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid claims principal missing user id");
            }

            if(!isOrganizationAdmin && !isSelf)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Dont havepermission to do it");
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"User does not exist");
            }

            _db.Users.Remove(user);
            await _db.SaveChangesAsync();
            return;
        }
        public async Task CensorUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid claims principal missing user id");
            }

            if(!isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Dont havepermission to do it");
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"User does not exist");
            }

            user.Censored = true;
            await _db.SaveChangesAsync();
            return;
        }
        public async Task UnCensorUserAsync(ClaimsPrincipal? claimsPrincipal, Guid id)
        {
            bool isAuthenticated = ClaimsHelper.IsAuthenticated(claimsPrincipal);
            if(!isAuthenticated)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"User not loged in");
            }

            Guid userId = ClaimsHelper.GetUserGuid(claimsPrincipal);
            bool isOrganizationAdmin = ClaimsHelper.IsOrganizationAdmin(claimsPrincipal);

            if(userId == Guid.Empty)
            {
                throw new AccountException(AccountErrorCodes.Unauthenticated,"Invalid claims principal missing user id");
            }

            if(!isOrganizationAdmin)
            {
                throw new AccountException(AccountErrorCodes.Forbiden,"Dont havepermission to do it");
            }

            User? user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id && u.Verified == true);
            if(user == null)
            {
                throw new AccountException(AccountErrorCodes.UserNotFound,"User does not exist");
            }

            user.Censored = false;
            await _db.SaveChangesAsync();
            return;
        }
    }
}