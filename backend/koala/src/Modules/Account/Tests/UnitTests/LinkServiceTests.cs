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
    public class LinkServiceTests
    {
        private async Task<(LinkService Service, AccountDbContext Db, Mock<IEmailService> Email)> Setup()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
            var db = await TestDataSeeder.SeedDb(new AccountDbContext(options));
            var email = new Mock<IEmailService>();
            email.Setup(service => service.SendPasswordResetEmailAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            return (new LinkService(db, email.Object), db, email);
        }

        private static ClaimsPrincipal User(Guid? id, params string[] roles)
        {
            var claims = id.HasValue
                ? new List<Claim> { new(ClaimTypes.NameIdentifier, id.Value.ToString()) }
                : new List<Claim>();
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));
        }

        private static LinkConsumeRegisterRequestDto Registration(bool declared13 = true, bool declared18 = false) =>
            new("Test", "User", "StrongPass1!", true, true, declared13, declared18);

        private static Link ActiveLink(Guid userId, string type, DateTime? expires = null) => new()
        {
            Id = Guid.NewGuid(), Token = Guid.NewGuid(), UserId = userId, Type = type,
            State = LinkStates.STATE_ACTIVE, CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow, ExpiresAt = expires ?? DateTime.UtcNow.AddDays(1)
        };

        [Fact]
        public async Task GetLinks_ReturnsOwnedLinksAndPaginates()
        {
            var (service, db, _) = await Setup();
            await db.Links.AddAsync(ActiveLink(TestDataSeeder.ID_USER_13, LinkTypes.TYPE_PASSWORD_RESET));
            await db.SaveChangesAsync();
            var principal = User(TestDataSeeder.ID_USER_13);
            var page = new PageQueryDto { PageNumber = 0, PageSize = 1 };

            var (all, allPage) = await service.GetUserLinksAsync(principal, page);
            var (active, activePage) = await service.GetActiveUserLinksAsync(principal, page);

            Assert.Single(all);
            Assert.Single(active);
            Assert.Equal(2, allPage.TotalNumber);
            Assert.Equal(2, activePage.TotalNumber);
        }

        [Theory]
        [InlineData("all")]
        [InlineData("active")]
        public async Task GetLinks_RequiresAuthenticatedUserId(string mode)
        {
            var (service, _, _) = await Setup();
            var page = new PageQueryDto { PageNumber = 0, PageSize = 10 };

            var unauthenticated = await Assert.ThrowsAsync<KoalaException>(async () =>
            {
                if (mode == "all") await service.GetUserLinksAsync(null, page);
                else await service.GetActiveUserLinksAsync(null, page);
            });
            var missingId = await Assert.ThrowsAsync<KoalaException>(async () =>
            {
                if (mode == "all") await service.GetUserLinksAsync(User(null), page);
                else await service.GetActiveUserLinksAsync(User(null), page);
            });

            Assert.Equal(KoalaErrorCodes.Unauthenticated, unauthenticated.ErrorCode);
            Assert.Equal(KoalaErrorCodes.Unauthenticated, missingId.ErrorCode);
        }

        [Fact]
        public async Task DeactivateLink_MarksOwnedActiveLinkInactive()
        {
            var (service, db, _) = await Setup();

            await service.DeactivateUserLinkAsync(User(TestDataSeeder.ID_USER_13), TestDataSeeder.ID_LINK_1);

            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(TestDataSeeder.ID_LINK_1))!.State);
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("already-inactive")]
        [InlineData("other-user")]
        public async Task DeactivateLink_InvalidLink_ThrowsLinkNotFound(string scenario)
        {
            var (service, db, _) = await Setup();
            var id = scenario == "missing" ? Guid.NewGuid() : TestDataSeeder.ID_LINK_1;
            var userId = scenario == "other-user" ? TestDataSeeder.ID_USER_14 : TestDataSeeder.ID_USER_13;
            if (scenario == "already-inactive")
            {
                (await db.Links.FindAsync(id))!.State = LinkStates.STATE_INACTIVE;
                await db.SaveChangesAsync();
            }

            var exception = await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeactivateUserLinkAsync(User(userId), id));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task PrivateLinkManagementEndpoints_RejectUnauthenticatedAndMissingUserClaims()
        {
            var (service, _, _) = await Setup();
            var calls = new Func<ClaimsPrincipal?, Task>[]
            {
                principal => service.GetUserLinksAsync(principal, new PageQueryDto { PageNumber = 0, PageSize = 10 }),
                principal => service.GetActiveUserLinksAsync(principal, new PageQueryDto { PageNumber = 0, PageSize = 10 }),
                principal => service.DeleteUserLinkAsync(principal, TestDataSeeder.ID_LINK_1),
                principal => service.DeactivateUserLinkAsync(principal, TestDataSeeder.ID_LINK_1),
                principal => service.DeleteUserLinksAsync(principal)
            };

            foreach (var call in calls)
            {
                var noPrincipal = await Assert.ThrowsAsync<KoalaException>(() => call(null));
                var noUserId = await Assert.ThrowsAsync<KoalaException>(() => call(User(null)));
                Assert.Equal(KoalaErrorCodes.Unauthenticated, noPrincipal.ErrorCode);
                Assert.Equal(KoalaErrorCodes.Unauthenticated, noUserId.ErrorCode);
            }
        }

        [Fact]
        public async Task DeleteLink_DeletesOwnedLink()
        {
            var (service, db, _) = await Setup();

            await service.DeleteUserLinkAsync(User(TestDataSeeder.ID_USER_13), TestDataSeeder.ID_LINK_1);

            Assert.Null(await db.Links.FindAsync(TestDataSeeder.ID_LINK_1));
        }

        [Fact]
        public async Task DeleteLink_OtherUsersLink_ThrowsLinkNotFound()
        {
            var (service, _, _) = await Setup();

            var exception = await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteUserLinkAsync(User(TestDataSeeder.ID_USER_14), TestDataSeeder.ID_LINK_1));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task DeleteLink_MissingLink_ThrowsLinkNotFound()
        {
            var (service, _, _) = await Setup();

            var exception = await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteUserLinkAsync(User(TestDataSeeder.ID_USER_13), Guid.NewGuid()));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task DeleteAllLinks_RemovesEveryLinkForCurrentUser()
        {
            var (service, db, _) = await Setup();

            await service.DeleteUserLinksAsync(User(TestDataSeeder.ID_USER_13));

            Assert.Empty(await db.Links.Where(link => link.UserId == TestDataSeeder.ID_USER_13).ToListAsync());
            Assert.NotEmpty(await db.Links.Where(link => link.UserId == TestDataSeeder.ID_USER_14).ToListAsync());
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("wrong-type")]
        public async Task ConsumeRegistrationLink_MissingOrWrongType_ThrowsLinkNotFound(string scenario)
        {
            var (service, _, _) = await Setup();
            var token = scenario == "wrong-type" ? TestDataSeeder.TOKEN_LINK_4 : Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<KoalaException>(() =>
                service.ConsumeRegisterLinkAsync(token, Registration()));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task ConsumeRegistrationLink_ExpiredLink_DeactivatesAndThrows()
        {
            var (service, db, _) = await Setup();
            var link = ActiveLink(TestDataSeeder.ID_USER_14, LinkTypes.TYPE_REGISTRATION, DateTime.UtcNow.AddMinutes(-1));
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.ConsumeRegisterLinkAsync(link.Token, Registration()));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(link.Id))!.State);
        }

        [Theory]
        [InlineData("admin")]
        [InlineData("editor")]
        [InlineData("reviewer")]
        [InlineData("guardian")]
        public async Task ConsumeRegistrationLink_AdultRolesRequire18Plus(string role)
        {
            var (service, db, _) = await Setup();
            var userId = role switch
            {
                "admin" => TestDataSeeder.ID_USER_13,
                "editor" => TestDataSeeder.ID_USER_14,
                "reviewer" => TestDataSeeder.ID_USER_15,
                _ => TestDataSeeder.ID_USER_14
            };
            var roleId = role switch
            {
                "editor" => TestDataSeeder.ID_ROLE_2,
                "reviewer" => TestDataSeeder.ID_ROLE_3,
                "guardian" => TestDataSeeder.ID_ROLE_4,
                _ => (Guid?)null
            };
            if (roleId.HasValue)
            {
                db.UserRoles.RemoveRange(await db.UserRoles.Where(userRole => userRole.UserId == userId).ToListAsync());
                db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId.Value });
                await db.SaveChangesAsync();
            }
            var link = ActiveLink(userId, LinkTypes.TYPE_REGISTRATION);
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() =>
                service.ConsumeRegisterLinkAsync(link.Token, Registration(declared13: true, declared18: false)));

            Assert.Equal(KoalaErrorCodes.UserRoleAgeRequirementNotMet, exception.ErrorCode);
        }

        [Fact]
        public async Task ConsumeRegistrationLink_CaptainMustConfirmOneAgeRequirement()
        {
            var (service, db, _) = await Setup();
            var link = ActiveLink(TestDataSeeder.ID_USER_14, LinkTypes.TYPE_REGISTRATION);
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() =>
                service.ConsumeRegisterLinkAsync(link.Token, Registration(declared13: false, declared18: false)));

            Assert.Equal(KoalaErrorCodes.UserRoleAgeRequirementNotMet, exception.ErrorCode);
        }

        [Fact]
        public async Task ConsumeRegistrationLink_CaptainRegistrationVerifiesAccountAndConsumesLink()
        {
            var (service, db, _) = await Setup();
            var request = Registration(declared13: true, declared18: false);

            await service.ConsumeRegisterLinkAsync(TestDataSeeder.TOKEN_LINK_2, request);

            var user = await db.Users.FindAsync(TestDataSeeder.ID_USER_14);
            Assert.True(user!.Verified);
            Assert.Equal(request.NameFirst, user.NameFirst);
            Assert.True(PasswordHasher.VerifyAndMaybeRehash(user.PasswordHash!, request.Password).Verified);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(TestDataSeeder.ID_LINK_2))!.State);
        }

        [Fact]
        public async Task ConsumeRegistrationLink_CaptainMayDeclare18InsteadOf13()
        {
            var (service, db, _) = await Setup();
            var link = ActiveLink(TestDataSeeder.ID_USER_14, LinkTypes.TYPE_REGISTRATION);
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            await service.ConsumeRegisterLinkAsync(link.Token, Registration(declared13: false, declared18: true));

            var user = await db.Users.FindAsync(TestDataSeeder.ID_USER_14);
            Assert.True(user!.Verified);
            Assert.False(user.DeclaredAtLeast13);
            Assert.True(user.DeclaredAtLeast18);
        }

        [Fact]
        public async Task ConsumeRegistrationLink_AlreadyVerifiedLinkedUser_DeactivatesAndThrowsUserNotFound()
        {
            var (service, db, _) = await Setup();
            var link = ActiveLink(TestDataSeeder.ID_USER_1, LinkTypes.TYPE_REGISTRATION);
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.ConsumeRegisterLinkAsync(link.Token, Registration()));

            Assert.Equal(KoalaErrorCodes.UserNotFound, exception.ErrorCode);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(link.Id))!.State);
        }

        [Fact]
        public async Task ConsumeRegistrationLink_AgeQualifiedAdminCanRegister()
        {
            var (service, db, _) = await Setup();
            var request = Registration(declared13: false, declared18: true);

            await service.ConsumeRegisterLinkAsync(TestDataSeeder.TOKEN_LINK_1, request);

            var user = await db.Users.FindAsync(TestDataSeeder.ID_USER_13);
            Assert.True(user!.Verified);
            Assert.True(user.DeclaredAtLeast18);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(TestDataSeeder.ID_LINK_1))!.State);
        }

        [Fact]
        public async Task ConsumeResetPasswordLink_UpdatesPasswordAndConsumesLink()
        {
            var (service, db, _) = await Setup();
            var request = new LinkConsumeResetPasswordRequestDto("AnotherStrong1!");

            await service.ConsumeResetPasswordLinkAsync(TestDataSeeder.TOKEN_LINK_4, request);

            var user = await db.Users.FindAsync(TestDataSeeder.ID_USER_12);
            Assert.True(PasswordHasher.VerifyAndMaybeRehash(user!.PasswordHash!, request.Password).Verified);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(TestDataSeeder.ID_LINK_4))!.State);
        }

        [Fact]
        public async Task ConsumeResetPasswordLink_ExpiredLink_DeactivatesAndThrows()
        {
            var (service, db, _) = await Setup();
            var link = ActiveLink(TestDataSeeder.ID_USER_1, LinkTypes.TYPE_PASSWORD_RESET, DateTime.UtcNow.AddMinutes(-1));
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.ConsumeResetPasswordLinkAsync(
                link.Token, new LinkConsumeResetPasswordRequestDto("AnotherStrong1!")));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(link.Id))!.State);
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("wrong-type")]
        public async Task ConsumeResetPasswordLink_MissingOrWrongType_ThrowsLinkNotFound(string scenario)
        {
            var (service, _, _) = await Setup();
            var token = scenario == "wrong-type" ? TestDataSeeder.TOKEN_LINK_1 : Guid.NewGuid();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.ConsumeResetPasswordLinkAsync(
                token, new LinkConsumeResetPasswordRequestDto("AnotherStrong1!")));

            Assert.Equal(KoalaErrorCodes.LinkNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task ConsumeResetPasswordLink_UnverifiedLinkedUser_DeactivatesAndThrowsUserNotFound()
        {
            var (service, db, _) = await Setup();
            var unverifiedUser = await db.Users.FindAsync(TestDataSeeder.ID_USER_13);
            unverifiedUser!.Verified = false;
            var link = ActiveLink(TestDataSeeder.ID_USER_13, LinkTypes.TYPE_PASSWORD_RESET);
            await db.Links.AddAsync(link);
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.ConsumeResetPasswordLinkAsync(
                link.Token, new LinkConsumeResetPasswordRequestDto("AnotherStrong1!")));

            Assert.Equal(KoalaErrorCodes.UserNotFound, exception.ErrorCode);
            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(link.Id))!.State);
        }

        [Fact]
        public async Task GenerateResetPasswordLink_UnknownOrUnverifiedEmailDoesNothing()
        {
            var (service, db, email) = await Setup();

            await service.GenerateResetPasswordLinkAsync(new LinkGenerateResetPasswordRequestDto("unknown@test.pl"));
            await service.GenerateResetPasswordLinkAsync(new LinkGenerateResetPasswordRequestDto(TestDataSeeder.EMAIL_USER_13));

            email.Verify(service => service.SendPasswordResetEmailAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            Assert.Equal(4, await db.Links.CountAsync());
        }

        [Theory]
        [InlineData("organization", "organization")]
        [InlineData("team", "team")]
        public async Task GenerateResetPasswordLink_CreatesLinkAndSendsToAccountArea(string accountType, string expectedArea)
        {
            var (service, db, email) = await Setup();
            var emailAddress = accountType == "organization" ? TestDataSeeder.EMAIL_USER_1 : TestDataSeeder.EMAIL_USER_8;

            await service.GenerateResetPasswordLinkAsync(new LinkGenerateResetPasswordRequestDto(emailAddress));

            email.Verify(service => service.SendPasswordResetEmailAsync(
                emailAddress,
                It.IsAny<string>(),
                expectedArea), Times.Once);
            Assert.Contains(await db.Links.ToListAsync(), link => link.UserId ==
                (accountType == "organization" ? TestDataSeeder.ID_USER_1 : TestDataSeeder.ID_USER_8) &&
                link.State == LinkStates.STATE_ACTIVE && link.Type == LinkTypes.TYPE_PASSWORD_RESET);
        }

        [Fact]
        public async Task GenerateResetPasswordLink_DeactivatesPreviousActiveResetLink()
        {
            var (service, db, email) = await Setup();

            await service.GenerateResetPasswordLinkAsync(new LinkGenerateResetPasswordRequestDto(TestDataSeeder.EMAIL_USER_12));

            Assert.Equal(LinkStates.STATE_INACTIVE, (await db.Links.FindAsync(TestDataSeeder.ID_LINK_4))!.State);
            Assert.Single(await db.Links.Where(link => link.UserId == TestDataSeeder.ID_USER_12 &&
                link.Type == LinkTypes.TYPE_PASSWORD_RESET && link.State == LinkStates.STATE_ACTIVE).ToListAsync());
            email.Verify(service => service.SendPasswordResetEmailAsync(
                TestDataSeeder.EMAIL_USER_12, It.IsAny<string>(), "team"), Times.Once);
        }
    }
}
