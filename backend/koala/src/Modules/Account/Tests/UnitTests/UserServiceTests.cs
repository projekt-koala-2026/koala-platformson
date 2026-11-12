using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace koala.src.Modules.Account.Tests.UnitTests
{
    public class UserServiceTests
    {
        private async Task<(UserService Service, AccountDbContext Db, Mock<IEmailService> Email)> Setup()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = await TestDataSeeder.SeedDb(new AccountDbContext(options));
            var email = new Mock<IEmailService>();
            email.Setup(service => service.SendRegisterEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            return (new UserService(db, email.Object), db, email);
        }

        private static ClaimsPrincipal Principal(Guid? userId, params string[] roles)
        {
            var claims = userId.HasValue
                ? new List<Claim> { new(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
                : new List<Claim>();
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));
        }

        private static string RoleFor(int userNumber) => userNumber switch
        {
            1 or 2 => RoleNames.NAME_ORGANIZATION_ADMIN,
            3 or 4 or 5 => RoleNames.NAME_ORGANIZATION_EDITOR,
            6 or 7 => RoleNames.NAME_ORGANIZATION_REVIEWER,
            8 or 9 => RoleNames.NAME_TEAM_GUARDIAN,
            _ => RoleNames.NAME_TEAM_CAPTAIN
        };

        private static Guid UserId(int userNumber) => userNumber switch
        {
            1 => TestDataSeeder.ID_USER_1, 2 => TestDataSeeder.ID_USER_2,
            3 => TestDataSeeder.ID_USER_3, 4 => TestDataSeeder.ID_USER_4,
            5 => TestDataSeeder.ID_USER_5, 6 => TestDataSeeder.ID_USER_6,
            7 => TestDataSeeder.ID_USER_7, 8 => TestDataSeeder.ID_USER_8,
            9 => TestDataSeeder.ID_USER_9, 10 => TestDataSeeder.ID_USER_10,
            11 => TestDataSeeder.ID_USER_11, 12 => TestDataSeeder.ID_USER_12,
            13 => TestDataSeeder.ID_USER_13, 14 => TestDataSeeder.ID_USER_14,
            _ => TestDataSeeder.ID_USER_15
        };

        [Theory]
        [InlineData("ORGANIZATION_ADMIN", true)]
        [InlineData("ORGANIZATION_EDITOR", true)]
        [InlineData("ORGANIZATION_REVIEWER", true)]
        [InlineData("TEAM_CAPTAIN", false)]
        [InlineData("TEAM_GUARDIAN", false)]
        public async Task RegisterUser_ValidRoleForCallerCreatesUnverifiedAccountAndSendsLink(string role, bool organization)
        {
            var (service, db, email) = await Setup();
            var caller = organization
                ? Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN)
                : (ClaimsPrincipal?)null;
            var request = new UserRegisterRequestDto($"{role.ToLowerInvariant()}@new.test", new List<string> { role });

            await service.RegisterUserAsync(caller, request);

            var user = await db.Users.SingleAsync(candidate => candidate.Email == request.Email);
            Assert.False(user.Verified);
            Assert.Null(user.PasswordHash);
            Assert.False(user.AcceptedRodo);
            Assert.False(user.AcceptedRules);
            Assert.Equal(TestDataSeeder.VERSION, user.Version);
            Assert.Single(await db.UserRoles.Where(userRole => userRole.UserId == user.Id).ToListAsync());
            var link = await db.Links.SingleAsync(candidate => candidate.UserId == user.Id);
            Assert.Equal(LinkTypes.TYPE_REGISTRATION, link.Type);
            Assert.Equal(LinkStates.STATE_ACTIVE, link.State);
            email.Verify(mock => mock.SendRegisterEmailAsync(request.Email, link.Token.ToString()), Times.Once);
        }

        [Theory]
        [InlineData(3, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(8, "TEAM_GUARDIAN")]
        [InlineData(10, "TEAM_CAPTAIN")]
        public async Task RegisterUser_AuthenticatedNonAdminIsForbidden(int userNumber, string role)
        {
            var (service, db, email) = await Setup();
            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.RegisterUserAsync(
                Principal(UserId(userNumber), RoleFor(userNumber)), new UserRegisterRequestDto("forbidden@test.pl", new() { role })));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
            Assert.Null(await db.Users.FirstOrDefaultAsync(user => user.Email == "forbidden@test.pl"));
            email.Verify(mock => mock.SendRegisterEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterUser_AlreadyVerifiedEmailThrowsWithoutChangingExistingAccount()
        {
            var (service, db, email) = await Setup();
            var before = await db.Users.FindAsync(TestDataSeeder.ID_USER_1);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.RegisterUserAsync(
                null, new UserRegisterRequestDto(TestDataSeeder.EMAIL_USER_1, new() { RoleNames.NAME_TEAM_CAPTAIN })));

            Assert.Equal(KoalaErrorCodes.UserAlreadyExists, exception.ErrorCode);
            Assert.Same(before, await db.Users.FindAsync(TestDataSeeder.ID_USER_1));
            email.Verify(mock => mock.SendRegisterEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(false, "ORGANIZATION_ADMIN")]
        [InlineData(true, "TEAM_CAPTAIN")]
        [InlineData(true, "TEAM_GUARDIAN")]
        public async Task RegisterUser_RoleFromWrongRoleFamilyThrows(bool organization, string role)
        {
            var (service, db, email) = await Setup();
            var caller = organization ? Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN) : null;

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.RegisterUserAsync(
                caller, new UserRegisterRequestDto("wrong-role@test.pl", new() { role })));

            Assert.Equal(KoalaErrorCodes.UserInvalidRoles, exception.ErrorCode);
            Assert.Null(await db.Users.FirstOrDefaultAsync(user => user.Email == "wrong-role@test.pl"));
            email.Verify(mock => mock.SendRegisterEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Theory]
        [InlineData(false, "ORGANIZATION_ADMIN", "TEAM_CAPTAIN")]
        [InlineData(false, "ORGANIZATION_EDITOR", "TEAM_GUARDIAN")]
        [InlineData(true, "ORGANIZATION_REVIEWER", "TEAM_CAPTAIN")]
        [InlineData(true, "ORGANIZATION_ADMIN", "TEAM_GUARDIAN")]
        public async Task RegisterUser_MixedOrganizationAndTeamRolesAreRejected(bool authenticatedAdmin, string organizationRole, string teamRole)
        {
            var (service, db, email) = await Setup();
            ClaimsPrincipal? caller = authenticatedAdmin
                ? Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN)
                : null;
            const string address = "mixed-roles@test.pl";

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.RegisterUserAsync(
                caller, new UserRegisterRequestDto(address, new() { organizationRole, teamRole })));

            Assert.Equal(KoalaErrorCodes.UserInvalidRoles, exception.ErrorCode);
            Assert.Null(await db.Users.SingleOrDefaultAsync(user => user.Email == address));
            email.Verify(mock => mock.SendRegisterEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task OrganizationAdminCanRegisterUserWithAllThreeOrganizationRoles()
        {
            var (service, db, _) = await Setup();
            const string address = "all-org-roles@test.pl";

            await service.RegisterUserAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN),
                new UserRegisterRequestDto(address, new()
                {
                    RoleNames.NAME_ORGANIZATION_ADMIN,
                    RoleNames.NAME_ORGANIZATION_EDITOR,
                    RoleNames.NAME_ORGANIZATION_REVIEWER
                }));

            var user = await db.Users.SingleAsync(candidate => candidate.Email == address);
            var roles = await db.UserRoles.Where(userRole => userRole.UserId == user.Id)
                .Select(userRole => userRole.Role.Name).OrderBy(name => name).ToListAsync();
            Assert.Equal(TestDataSeeder.VERSION, user.Version);
            Assert.Equal(new[]
            {
                RoleNames.NAME_ORGANIZATION_ADMIN,
                RoleNames.NAME_ORGANIZATION_EDITOR,
                RoleNames.NAME_ORGANIZATION_REVIEWER
            }.OrderBy(name => name), roles);
        }

        [Fact]
        public async Task RegisterUser_InvalidReplacementRolesPreserveExistingUnverifiedAccountAndLink()
        {
            var (service, db, email) = await Setup();
            var existingLink = await db.Links.SingleAsync(link => link.UserId == TestDataSeeder.ID_USER_13);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.RegisterUserAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN),
                new UserRegisterRequestDto(TestDataSeeder.EMAIL_USER_13,
                    new() { RoleNames.NAME_ORGANIZATION_EDITOR, RoleNames.NAME_TEAM_CAPTAIN })));

            Assert.Equal(KoalaErrorCodes.UserInvalidRoles, exception.ErrorCode);
            Assert.NotNull(await db.Users.FindAsync(TestDataSeeder.ID_USER_13));
            Assert.Same(existingLink, await db.Links.FindAsync(existingLink.Id));
            email.Verify(mock => mock.SendRegisterEmailAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task RegisterUser_ReplacesExistingUnverifiedAccountAndItsRegistrationLink()
        {
            var (service, db, email) = await Setup();
            var request = new UserRegisterRequestDto(TestDataSeeder.EMAIL_USER_13, new() { RoleNames.NAME_ORGANIZATION_ADMIN });

            await service.RegisterUserAsync(Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), request);

            var user = await db.Users.SingleAsync(candidate => candidate.Email == request.Email);
            Assert.NotEqual(TestDataSeeder.ID_USER_13, user.Id);
            Assert.False(user.Verified);
            Assert.Equal(TestDataSeeder.VERSION, user.Version);
            Assert.Single(await db.Links.Where(link => link.UserId == user.Id && link.State == LinkStates.STATE_ACTIVE).ToListAsync());
            Assert.Null(await db.Users.FindAsync(TestDataSeeder.ID_USER_13));
            email.Verify(mock => mock.SendRegisterEmailAsync(request.Email, It.IsAny<string>()), Times.Once);
        }

        [Theory]
        [InlineData(1, "ORGANIZATION_ADMIN")]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(8, "TEAM_GUARDIAN")]
        [InlineData(10, "TEAM_CAPTAIN")]
        public async Task GetUser_AllVerifiedAccountKindsCanReadTheirOwnProfile(int userNumber, string role)
        {
            var (service, db, _) = await Setup();

            var result = await service.GetUserAsync(Principal(UserId(userNumber), role), UserId(userNumber));

            Assert.Equal(UserId(userNumber), result.Id);
            Assert.Contains(role, result.Roles);
        }

        [Fact]
        public async Task User1ProfileIncludesAllThreeOrganizationRoles()
        {
            var (service, _, _) = await Setup();

            var result = await service.GetUserAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), TestDataSeeder.ID_USER_1);

            Assert.Equal(3, result.Roles.Count);
            Assert.Contains(RoleNames.NAME_ORGANIZATION_ADMIN, result.Roles);
            Assert.Contains(RoleNames.NAME_ORGANIZATION_EDITOR, result.Roles);
            Assert.Contains(RoleNames.NAME_ORGANIZATION_REVIEWER, result.Roles);
            Assert.Equal(TestDataSeeder.VERSION, result.Version);
        }

        [Fact]
        public async Task EverySeededVersionedAccountEntityStartsAtVersionOne()
        {
            var (_, db, _) = await Setup();

            var users = await db.Users.ToListAsync();
            var teams = await db.Teams.ToListAsync();
            var members = await db.TeamMembers.ToListAsync();
            var joinCodes = await db.TeamJoinCodes.ToListAsync();

            Assert.Equal(15, users.Count);
            Assert.All(users, user => Assert.Equal(TestDataSeeder.VERSION, user.Version));
            Assert.All(teams, team => Assert.Equal(TestDataSeeder.VERSION, team.Version));
            Assert.All(members, member => Assert.Equal(TestDataSeeder.VERSION, member.Version));
            Assert.All(joinCodes, code => Assert.Equal(TestDataSeeder.VERSION, code.Version));
        }

        [Fact]
        public async Task GetUser_OrganizationAdminCanReadAnotherUsersProfile()
        {
            var (service, _, _) = await Setup();

            var result = await service.GetUserAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), TestDataSeeder.ID_USER_8);

            Assert.Equal(TestDataSeeder.ID_USER_8, result.Id);
            Assert.Contains(RoleNames.NAME_TEAM_GUARDIAN, result.Roles);
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("wrong-id")]
        public async Task GetUser_RejectsUnauthenticatedMalformedOrOtherUsers(string scenario)
        {
            var (service, _, _) = await Setup();
            ClaimsPrincipal? principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                _ => Principal(TestDataSeeder.ID_USER_4, TestDataSeeder.NAME_ROLE_2)
            };

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetUserAsync(principal, TestDataSeeder.ID_USER_8));

            Assert.Equal(scenario == "wrong-id" ? KoalaErrorCodes.Forbidden : KoalaErrorCodes.Unauthenticated, exception.ErrorCode);
        }

        [Theory]
        [InlineData("unverified")]
        [InlineData("missing")]
        public async Task GetUser_UnverifiedAndMissingAccountsAreNotFound(string scenario)
        {
            var (service, _, _) = await Setup();
            var id = scenario == "unverified" ? TestDataSeeder.ID_USER_13 : Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetUserAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), id));

            Assert.Equal(KoalaErrorCodes.UserNotFound, exception.ErrorCode);
        }

        [Theory]
        [InlineData(1, "ORGANIZATION_ADMIN")]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(8, "TEAM_GUARDIAN")]
        [InlineData(10, "TEAM_CAPTAIN")]
        public async Task GetDeclaration_UserCanReadOwnDeclaration(int userNumber, string role)
        {
            var (service, _, _) = await Setup();

            var declaration = await service.GetUserDeclarationAsync(Principal(UserId(userNumber), role), UserId(userNumber));

            Assert.Equal(UserId(userNumber), declaration.Id);
        }

        [Fact]
        public async Task GetDeclaration_AdminCanReadAnotherUsersDeclaration()
        {
            var (service, _, _) = await Setup();
            var result = await service.GetUserDeclarationAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), TestDataSeeder.ID_USER_8);
            Assert.Equal(TestDataSeeder.ID_USER_8, result.Id);
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("other-user")]
        [InlineData("unverified-target")]
        public async Task GetDeclaration_EnforcesIdentityOwnershipAndVerification(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "other-user" => Principal(TestDataSeeder.ID_USER_4, TestDataSeeder.NAME_ROLE_2),
                _ => Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1)
            };
            var id = scenario == "unverified-target" ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8;

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetUserDeclarationAsync(principal, id));

            var expected = scenario switch
            {
                "anonymous" or "missing-id" => KoalaErrorCodes.Unauthenticated,
                "other-user" => KoalaErrorCodes.Forbidden,
                _ => KoalaErrorCodes.UserNotFound
            };
            Assert.Equal(expected, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateDeclaration_SelfCanChangeAgeAndConsentFields()
        {
            var (service, db, _) = await Setup();
            var request = new UserDeclarationUpdateDto(true, true, TestDataSeeder.VERSION);

            var result = await service.UpdateUserDeclarationAsync(Principal(TestDataSeeder.ID_USER_4), TestDataSeeder.ID_USER_4, request);

            Assert.True(result.DeclaredAtLeast13);
            Assert.True(result.DeclaredAtLeast18);
            var user = await db.Users.FindAsync(TestDataSeeder.ID_USER_4);
            Assert.Equal(TestDataSeeder.VERSION + 1, user!.Version);
            Assert.True(user.UpdatedAt >= TestDataSeeder.TIME_CREATED);
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("other-user")]
        [InlineData("unverified-target")]
        [InlineData("stale-version")]
        public async Task UpdateDeclaration_RejectsInvalidCallerTargetOrVersion(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "other-user" => Principal(TestDataSeeder.ID_USER_4, TestDataSeeder.NAME_ROLE_2),
                _ => Principal(scenario.Contains("unverified") ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8)
            };
            var id = scenario == "unverified-target" ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8;
            var request = new UserDeclarationUpdateDto(true, true, scenario == "stale-version" ? 999 : TestDataSeeder.VERSION);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserDeclarationAsync(principal, id, request));

            var expected = scenario switch
            {
                "anonymous" or "missing-id" => KoalaErrorCodes.Unauthenticated,
                "other-user" => KoalaErrorCodes.Forbidden,
                "unverified-target" => KoalaErrorCodes.UserNotFound,
                _ => KoalaErrorCodes.ResourceVersionConflict
            };
            Assert.Equal(expected, exception.ErrorCode);
        }

        [Theory]
        [InlineData(1, "ORGANIZATION_ADMIN")]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(8, "TEAM_GUARDIAN")]
        public async Task UpdateDeclaration_AdultRolesMustDeclare18(int userNumber, string role)
        {
            var (service, _, _) = await Setup();
            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserDeclarationAsync(
                Principal(UserId(userNumber), role), UserId(userNumber), new UserDeclarationUpdateDto(true, false, TestDataSeeder.VERSION)));
            Assert.Equal(KoalaErrorCodes.UserRoleAgeRequirementNotMet, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateDeclaration_CaptainMustDeclare13Or18()
        {
            var (service, _, _) = await Setup();
            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserDeclarationAsync(
                Principal(TestDataSeeder.ID_USER_10, RoleNames.NAME_TEAM_CAPTAIN), TestDataSeeder.ID_USER_10,
                new UserDeclarationUpdateDto(false, false, TestDataSeeder.VERSION)));
            Assert.Equal(KoalaErrorCodes.UserRoleAgeRequirementNotMet, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateDeclaration_AdminCannotUpdateAnotherUsersDeclaration()
        {
            var (service, _, _) = await Setup();
            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserDeclarationAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), TestDataSeeder.ID_USER_4,
                new UserDeclarationUpdateDto(true, false, TestDataSeeder.VERSION)));
            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task GetUsers_AdminFiltersAcrossFieldsAndPaginates()
        {
            var (service, _, _) = await Setup();
            var principal = Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN);
            var page = new PageQueryDto { PageNumber = 0, PageSize = 100 };
            var (all, allPagination) = await service.GetUsersAsync(principal, page, new UserQueryDto(ShowCensored: null));
            var (emailResult, emailPage) = await service.GetUsersAsync(principal, page, new UserQueryDto(Email: TestDataSeeder.EMAIL_USER_4, ShowCensored: null));
            var (firstName, _) = await service.GetUsersAsync(principal, page, new UserQueryDto(NameFirst: TestDataSeeder.NAME_FIRST_USER_4, ShowCensored: null));
            var (lastName, _) = await service.GetUsersAsync(principal, page, new UserQueryDto(NameLast: TestDataSeeder.NAME_LAST_USER_4, ShowCensored: null));
            var (editors, _) = await service.GetUsersAsync(principal, page, new UserQueryDto(UserRoles: new() { RoleNames.NAME_ORGANIZATION_EDITOR }, ShowCensored: null));
            var (uncensored, _) = await service.GetUsersAsync(principal, page, new UserQueryDto(ShowCensored: false));
            var (censored, _) = await service.GetUsersAsync(principal, page, new UserQueryDto(ShowCensored: true));
            var (secondPage, secondPageInfo) = await service.GetUsersAsync(principal,
                new PageQueryDto { PageNumber = 1, PageSize = 5 }, new UserQueryDto(ShowCensored: null));

            Assert.Equal(15, all.Count);
            Assert.Equal(15, allPagination.TotalNumber);
            Assert.Single(emailResult);
            Assert.Equal(1, emailPage.TotalNumber);
            Assert.Contains(emailResult, user => user.Id == TestDataSeeder.ID_USER_4);
            Assert.Contains(firstName, user => user.Id == TestDataSeeder.ID_USER_4);
            Assert.Contains(lastName, user => user.Id == TestDataSeeder.ID_USER_4);
            Assert.Contains(editors, user => user.Id == TestDataSeeder.ID_USER_4);
            Assert.All(uncensored, user => Assert.False(user.Censored));
            Assert.All(censored, user => Assert.True(user.Censored));
            Assert.Equal(5, secondPage.Count);
            Assert.Equal(15, secondPageInfo.TotalNumber);
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("editor")]
        [InlineData("reviewer")]
        [InlineData("guardian")]
        [InlineData("captain")]
        public async Task GetUsers_OnlyOrganizationAdminCanListUsers(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "editor" => Principal(TestDataSeeder.ID_USER_4, RoleNames.NAME_ORGANIZATION_EDITOR),
                "reviewer" => Principal(TestDataSeeder.ID_USER_6, RoleNames.NAME_ORGANIZATION_REVIEWER),
                "guardian" => Principal(TestDataSeeder.ID_USER_8, RoleNames.NAME_TEAM_GUARDIAN),
                _ => Principal(TestDataSeeder.ID_USER_10, RoleNames.NAME_TEAM_CAPTAIN)
            };

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetUsersAsync(
                principal, new PageQueryDto { PageNumber = 0, PageSize = 10 }, new UserQueryDto(ShowCensored: null)));

            Assert.Equal(scenario is "anonymous" or "missing-id" ? KoalaErrorCodes.Unauthenticated : KoalaErrorCodes.Forbidden,
                exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateUserRoles_SelfCanChangeTeamRolesAndVersion()
        {
            var (service, db, _) = await Setup();
            var user = await db.Users.FindAsync(TestDataSeeder.ID_USER_4);
            user!.DeclaredAtLeast13 = true;
            await db.SaveChangesAsync();

            var result = await service.UpdateUserRolesAsync(Principal(user.Id, RoleNames.NAME_ORGANIZATION_EDITOR), user.Id,
                new UserUpdateRolesDto(new() { RoleNames.NAME_TEAM_CAPTAIN }, TestDataSeeder.VERSION));

            Assert.Contains(RoleNames.NAME_TEAM_CAPTAIN, result.Roles);
            Assert.DoesNotContain(RoleNames.NAME_ORGANIZATION_EDITOR, result.Roles);
            Assert.Equal(TestDataSeeder.VERSION + 1, result.Version);
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("forbidden")]
        [InlineData("unverified")]
        [InlineData("stale-version")]
        public async Task UpdateUserRoles_RejectsCallerTargetOrVersion(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "forbidden" => Principal(TestDataSeeder.ID_USER_4, RoleNames.NAME_ORGANIZATION_EDITOR),
                _ => Principal(scenario.Contains("unverified") ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8)
            };
            var id = scenario == "unverified" ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8;
            var request = new UserUpdateRolesDto(new() { RoleNames.NAME_TEAM_CAPTAIN }, scenario == "stale-version" ? 99 : TestDataSeeder.VERSION);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserRolesAsync(principal, id, request));

            var expected = scenario switch
            {
                "anonymous" or "missing-id" => KoalaErrorCodes.Unauthenticated,
                "forbidden" => KoalaErrorCodes.Forbidden,
                "unverified" => KoalaErrorCodes.UserNotFound,
                _ => KoalaErrorCodes.ResourceVersionConflict
            };
            Assert.Equal(expected, exception.ErrorCode);
        }

        [Theory]
        [InlineData("organization-admin", false)]
        [InlineData("organization-editor", false)]
        [InlineData("organization-reviewer", false)]
        [InlineData("team-guardian", false)]
        [InlineData("team-captain", true)]
        public async Task UpdateUserRoles_EnforcesAgeForSelectedRole(string role, bool captain)
        {
            var (service, db, _) = await Setup();
            var selectedRole = role switch
            {
                "organization-admin" => RoleNames.NAME_ORGANIZATION_ADMIN,
                "organization-editor" => RoleNames.NAME_ORGANIZATION_EDITOR,
                "organization-reviewer" => RoleNames.NAME_ORGANIZATION_REVIEWER,
                "team-guardian" => RoleNames.NAME_TEAM_GUARDIAN,
                _ => RoleNames.NAME_TEAM_CAPTAIN
            };
            var targetId = role.StartsWith("organization", StringComparison.Ordinal)
                ? TestDataSeeder.ID_USER_4
                : role == "team-guardian" ? TestDataSeeder.ID_USER_8 : TestDataSeeder.ID_USER_10;
            var target = await db.Users.FindAsync(targetId);
            target!.DeclaredAtLeast18 = false;
            target.DeclaredAtLeast13 = false;
            var callerRole = role.StartsWith("organization", StringComparison.Ordinal)
                ? RoleNames.NAME_ORGANIZATION_ADMIN
                : role == "team-guardian" ? RoleNames.NAME_TEAM_GUARDIAN : RoleNames.NAME_TEAM_CAPTAIN;
            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserRolesAsync(
                Principal(targetId, callerRole), targetId, new UserUpdateRolesDto(new() { selectedRole }, TestDataSeeder.VERSION)));
            Assert.Equal(KoalaErrorCodes.UserRoleAgeRequirementNotMet, exception.ErrorCode);
            Assert.Equal(captain, selectedRole == RoleNames.NAME_TEAM_CAPTAIN);
        }

        [Theory]
        [InlineData(false, "ORGANIZATION_EDITOR")]
        [InlineData(false, "ORGANIZATION_REVIEWER")]
        [InlineData(true, "TEAM_GUARDIAN")]
        public async Task UpdateUserRoles_RejectsRolesOutsideCallerRoleFamily(bool organization, string role)
        {
            var (service, _, _) = await Setup();
            var caller = organization
                ? Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN)
                : Principal(TestDataSeeder.ID_USER_4, RoleNames.NAME_ORGANIZATION_EDITOR);
            var target = organization ? TestDataSeeder.ID_USER_8 : TestDataSeeder.ID_USER_4;

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserRolesAsync(
                caller, target, new UserUpdateRolesDto(new() { role }, TestDataSeeder.VERSION)));

            Assert.Equal(KoalaErrorCodes.UserInvalidRoles, exception.ErrorCode);
        }

        [Theory]
        [InlineData("ORGANIZATION_ADMIN", "TEAM_CAPTAIN")]
        [InlineData("ORGANIZATION_EDITOR", "TEAM_GUARDIAN")]
        [InlineData("ORGANIZATION_REVIEWER", "TEAM_CAPTAIN")]
        [InlineData("ORGANIZATION_ADMIN", "TEAM_GUARDIAN")]
        public async Task UpdateUserRoles_CannotMixOrganizationAndTeamRoles(string organizationRole, string teamRole)
        {
            var (service, db, _) = await Setup();
            var before = await db.UserRoles.Where(userRole => userRole.UserId == TestDataSeeder.ID_USER_4)
                .Select(userRole => userRole.Role.Name).ToListAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserRolesAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), TestDataSeeder.ID_USER_4,
                new UserUpdateRolesDto(new() { organizationRole, teamRole }, TestDataSeeder.VERSION)));

            Assert.Equal(KoalaErrorCodes.UserInvalidRoles, exception.ErrorCode);
            var after = await db.UserRoles.Where(userRole => userRole.UserId == TestDataSeeder.ID_USER_4)
                .Select(userRole => userRole.Role.Name).ToListAsync();
            Assert.Equal(before, after);
        }

        [Theory]
        [InlineData(1, "ORGANIZATION_ADMIN")]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(8, "TEAM_GUARDIAN")]
        [InlineData(10, "TEAM_CAPTAIN")]
        public async Task UpdateUserNames_SelfCanUpdateProfileNames(int userNumber, string role)
        {
            var (service, db, _) = await Setup();
            var userId = UserId(userNumber);
            var rolesBefore = await db.UserRoles.Where(userRole => userRole.UserId == userId)
                .Select(userRole => userRole.Role.Name).OrderBy(name => name).ToListAsync();
            var result = await service.UpdateUserNamesAsync(Principal(userId, role), userId,
                new UserChangeNamesDto("NewFirst", "NewLast", TestDataSeeder.VERSION));

            Assert.Equal("NewFirst", result.NameFirst);
            Assert.Equal("NewLast", result.NameLast);
            Assert.Equal(TestDataSeeder.VERSION + 1, result.Version);
            Assert.Equal(rolesBefore, result.Roles.OrderBy(name => name));
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("forbidden")]
        [InlineData("unverified")]
        [InlineData("stale-version")]
        public async Task UpdateUserNames_RejectsCallerTargetOrVersion(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "forbidden" => Principal(TestDataSeeder.ID_USER_4, RoleNames.NAME_ORGANIZATION_EDITOR),
                _ => Principal(scenario.Contains("unverified") ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8)
            };
            var id = scenario == "unverified" ? TestDataSeeder.ID_USER_13 : TestDataSeeder.ID_USER_8;
            var request = new UserChangeNamesDto("Updated", "Name", scenario == "stale-version" ? 99 : TestDataSeeder.VERSION);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateUserNamesAsync(principal, id, request));

            var expected = scenario switch
            {
                "anonymous" or "missing-id" => KoalaErrorCodes.Unauthenticated,
                "forbidden" => KoalaErrorCodes.Forbidden,
                "unverified" => KoalaErrorCodes.UserNotFound,
                _ => KoalaErrorCodes.ResourceVersionConflict
            };
            Assert.Equal(expected, exception.ErrorCode);
        }

        [Fact]
        public async Task OrganizationAdminCanUpdateAnotherUsersNames()
        {
            var (service, db, _) = await Setup();
            var result = await service.UpdateUserNamesAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), TestDataSeeder.ID_USER_4,
                new UserChangeNamesDto("AdminEdited", "Profile", TestDataSeeder.VERSION));

            Assert.Equal("AdminEdited", result.NameFirst);
            Assert.Equal("Profile", result.NameLast);
            Assert.Equal(TestDataSeeder.VERSION + 1, result.Version);
            Assert.Equal("AdminEdited", (await db.Users.FindAsync(TestDataSeeder.ID_USER_4))!.NameFirst);
        }

        [Fact]
        public async Task OrganizationAdminCanReplaceAnotherUsersRolesWhenAgeRequirementIsMet()
        {
            var (service, db, _) = await Setup();
            var target = await db.Users.FindAsync(TestDataSeeder.ID_USER_4);
            target!.DeclaredAtLeast18 = true;

            var result = await service.UpdateUserRolesAsync(
                Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN), target.Id,
                new UserUpdateRolesDto(new() { RoleNames.NAME_ORGANIZATION_REVIEWER }, TestDataSeeder.VERSION));

            Assert.Equal(new[] { RoleNames.NAME_ORGANIZATION_REVIEWER }, result.Roles);
            Assert.Equal(TestDataSeeder.VERSION + 1, result.Version);
            Assert.Single(await db.UserRoles.Where(role => role.UserId == target.Id).ToListAsync());
        }

        [Fact]
        public async Task UpdateUserRoles_EmptyRoleListRemovesAllRoles()
        {
            var (service, db, _) = await Setup();
            var result = await service.UpdateUserRolesAsync(
                Principal(TestDataSeeder.ID_USER_4, RoleNames.NAME_ORGANIZATION_EDITOR), TestDataSeeder.ID_USER_4,
                new UserUpdateRolesDto(new(), TestDataSeeder.VERSION));

            Assert.Empty(result.Roles);
            Assert.Empty(await db.UserRoles.Where(role => role.UserId == TestDataSeeder.ID_USER_4).ToListAsync());
        }

        [Theory]
        [InlineData(1, "ORGANIZATION_ADMIN")]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(8, "TEAM_GUARDIAN")]
        [InlineData(10, "TEAM_CAPTAIN")]
        public async Task DeleteUser_SelfCanDeleteAccountAndCascadeAccountData(int userNumber, string role)
        {
            var (service, db, _) = await Setup();
            var userId = UserId(userNumber);

            await service.DeleteUserAsync(Principal(userId, role), userId);

            Assert.Null(await db.Users.FindAsync(userId));
            Assert.Empty(await db.Sessions.Where(session => session.UserId == userId).ToListAsync());
            Assert.Empty(await db.Links.Where(link => link.UserId == userId).ToListAsync());
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("forbidden")]
        [InlineData("unverified")]
        [InlineData("missing")]
        public async Task DeleteUser_RejectsInvalidCallerOrTarget(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "forbidden" => Principal(TestDataSeeder.ID_USER_4, RoleNames.NAME_ORGANIZATION_EDITOR),
                _ => Principal(TestDataSeeder.ID_USER_1, RoleNames.NAME_ORGANIZATION_ADMIN)
            };
            var id = scenario == "unverified" ? TestDataSeeder.ID_USER_13 : scenario == "missing" ? Guid.NewGuid() : TestDataSeeder.ID_USER_8;

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.DeleteUserAsync(principal, id));

            var expected = scenario switch
            {
                "anonymous" or "missing-id" => KoalaErrorCodes.Unauthenticated,
                "forbidden" => KoalaErrorCodes.Forbidden,
                _ => KoalaErrorCodes.UserNotFound
            };
            Assert.Equal(expected, exception.ErrorCode);
        }

        [Theory]
        [InlineData("censor", true)]
        [InlineData("uncensor", false)]
        public async Task CensorAdmin_ChangesCensorStateAndVersion(string action, bool censored)
        {
            var (service, db, _) = await Setup();
            var id = TestDataSeeder.ID_USER_4;
            var request = new UserCensorRequestDto(TestDataSeeder.VERSION);

            if (action == "censor") await service.CensorUserAsync(Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1), id, request);
            else await service.UnCensorUserAsync(Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1), id, request);

            var user = await db.Users.FindAsync(id);
            Assert.Equal(censored, user!.Censored);
            Assert.Equal(TestDataSeeder.VERSION + 1, user.Version);
        }

        [Theory]
        [InlineData("anonymous")]
        [InlineData("missing-id")]
        [InlineData("editor")]
        [InlineData("reviewer")]
        [InlineData("guardian")]
        [InlineData("captain")]
        [InlineData("unverified-target")]
        [InlineData("missing-target")]
        [InlineData("stale-version")]
        public async Task CensorEndpoints_EnforceAdminTargetAndVersion(string scenario)
        {
            var (service, _, _) = await Setup();
            var principal = scenario switch
            {
                "anonymous" => null,
                "missing-id" => Principal(null),
                "editor" => Principal(TestDataSeeder.ID_USER_4, TestDataSeeder.NAME_ROLE_2),
                "reviewer" => Principal(TestDataSeeder.ID_USER_6, TestDataSeeder.NAME_ROLE_3),
                "guardian" => Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4),
                "captain" => Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5),
                _ => Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1)
            };
            var id = scenario switch
            {
                "unverified-target" => TestDataSeeder.ID_USER_13,
                "missing-target" => Guid.NewGuid(),
                _ => TestDataSeeder.ID_USER_4
            };
            var request = new UserCensorRequestDto(scenario == "stale-version" ? 99 : TestDataSeeder.VERSION);

            var exception = await Assert.ThrowsAsync<KoalaException>(async () =>
            {
                if (scenario == "uncensor") await service.UnCensorUserAsync(principal, id, request);
                else await service.CensorUserAsync(principal, id, request);
            });

            var expected = scenario switch
            {
                "anonymous" or "missing-id" => KoalaErrorCodes.Unauthenticated,
                "editor" or "reviewer" or "guardian" or "captain" => KoalaErrorCodes.Forbidden,
                "unverified-target" or "missing-target" => KoalaErrorCodes.UserNotFound,
                _ => KoalaErrorCodes.ResourceVersionConflict
            };
            Assert.Equal(expected, exception.ErrorCode);
        }
    }
}
