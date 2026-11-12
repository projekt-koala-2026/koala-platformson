using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;

namespace koala.src.Modules.Account.Tests.UnitTests
{
    public class SessionServiceTests
    {
        private static ClaimsPrincipal AuthenticatedUser(Guid? userId)
        {
            var claims = userId.HasValue
                ? new[] { new Claim(ClaimTypes.NameIdentifier, userId.Value.ToString()) }
                : Array.Empty<Claim>();
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));
        }

        private async Task<(ISessionService, AccountDbContext, Mock<ICacheService>)> SetupTestEnvironment()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var _db = new AccountDbContext(options);
            _db = await TestDataSeeder.SeedDb(_db);
            var cacheService = new Mock<ICacheService>();
            cacheService.Setup(cache => cache.RemoveKeyAsync(It.IsAny<string>())).Returns(Task.CompletedTask);
            ISessionService service = new SessionService(_db, cacheService.Object);
            return (service, _db, cacheService);
        }

        [Fact]
        public async Task User1_GetSessions_ReturnsActiveSessions()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_1.ToString()),
                new Claim("SessionId", TestDataSeeder.ID_SESSION_1.ToString()),
                new Claim("SessionToken", TestDataSeeder.TOKEN_SESSION_1.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.NAME_ROLE_1.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            var pageQueryDto = new PageQueryDto { PageNumber = 0, PageSize = 16 };
            var (response, pagination) = await service.GetActiveSessionsAsync(claimsPrincipal, pageQueryDto);

            Assert.Equal(2, response.Count);
            Assert.Contains(response, s => s.SessionId == TestDataSeeder.ID_SESSION_1);
            Assert.Contains(response, s => s.SessionId == TestDataSeeder.ID_SESSION_3);
        }

        [Fact]
        public async Task User1_CreateSession_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var requestDto = new LoginRequestDto
            (
                TestDataSeeder.EMAIL_USER_1,
                TestDataSeeder.PASSWORD_USER_1
            );

            var response = await service.CreateSessionAsync(requestDto);

            Assert.NotNull(response);
            Assert.Equal(TestDataSeeder.ID_USER_1, response.UserId);
            Assert.Contains(TestDataSeeder.NAME_ROLE_1, response.UserRoles);
        }

        [Fact]
        public async Task User1_DeleteSession_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_1.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            await service.DeleteSessionAsync(claimsPrincipal, TestDataSeeder.ID_SESSION_1);

            var deletedSession = await _db.Sessions.FindAsync(TestDataSeeder.ID_SESSION_1);
            Assert.Null(deletedSession);
            cacheMock.Verify(cache => cache.RemoveKeyAsync($"koala:account:Session:{TestDataSeeder.TOKEN_SESSION_1}"), Times.Once);
        }

        [Fact]
        public async Task User1_DeleteAllSessions_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_1.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            await service.DeleteSessionsAsync(claimsPrincipal);

            var activeSessions = await _db.Sessions
                .Where(s => s.UserId == TestDataSeeder.ID_USER_1)
                .ToListAsync();

            Assert.Empty(activeSessions);
            cacheMock.Verify(cache => cache.RemoveKeyAsync(It.IsAny<string>()), Times.Exactly(3));
        }

        [Fact]
        public async Task User3_GetActiveSessions_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_3.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            var pageQueryDto = new PageQueryDto { PageNumber = 0, PageSize = 16 };
            var (response, pagination) = await service.GetSessionsAsync(claimsPrincipal, pageQueryDto);

            Assert.Single(response);
            Assert.Equal(TestDataSeeder.ID_SESSION_4, response[0].SessionId);
        }

        [Fact]
        public async Task User3_DeleteSession_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_3.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            await service.DeleteSessionAsync(claimsPrincipal, TestDataSeeder.ID_SESSION_4);

            var deletedSession = await _db.Sessions.FindAsync(TestDataSeeder.ID_SESSION_4);
            Assert.Null(deletedSession);
        }

        [Fact]
        public async Task User3_CreateSession_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var requestDto = new LoginRequestDto
            (
                TestDataSeeder.EMAIL_USER_3,
                TestDataSeeder.PASSWORD_USER_3
            );

            var response = await service.CreateSessionAsync(requestDto);

            Assert.NotNull(response);
            Assert.Equal(TestDataSeeder.ID_USER_3, response.UserId);
            Assert.Contains(TestDataSeeder.NAME_ROLE_3, response.UserRoles);
        }

        [Fact]
        public async Task User8_GetActiveSessions_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            var pageQueryDto = new PageQueryDto { PageNumber = 0, PageSize = 16 };
            var (response, pagination) = await service.GetSessionsAsync(claimsPrincipal, pageQueryDto);

            Assert.Single(response);
            Assert.Equal(TestDataSeeder.ID_SESSION_5, response[0].SessionId);
        }

        [Fact]
        public async Task User8_CreateSession_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var requestDto = new LoginRequestDto
            (
                TestDataSeeder.EMAIL_USER_8,
                TestDataSeeder.PASSWORD_USER_8
            );

            var response = await service.CreateSessionAsync(requestDto);

            Assert.NotNull(response);
            Assert.Equal(TestDataSeeder.ID_USER_8, response.UserId);
            Assert.Contains(TestDataSeeder.NAME_ROLE_4, response.UserRoles);
        }

        [Theory]
        [InlineData(1, "ORGANIZATION_ADMIN")]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(10, "TEAM_CAPTAIN")]
        [InlineData(8, "TEAM_GUARDIAN")]
        public async Task CreateSession_ReturnsTheUserRoleForEachAccountKind(int userNumber, string expectedRole)
        {
            var (service, _, _) = await SetupTestEnvironment();
            var (email, password) = userNumber switch
            {
                1 => (TestDataSeeder.EMAIL_USER_1, TestDataSeeder.PASSWORD_USER_1),
                4 => (TestDataSeeder.EMAIL_USER_4, TestDataSeeder.PASSWORD_USER_4),
                6 => (TestDataSeeder.EMAIL_USER_6, TestDataSeeder.PASSWORD_USER_6),
                10 => (TestDataSeeder.EMAIL_USER_10, TestDataSeeder.PASSWORD_USER_10),
                _ => (TestDataSeeder.EMAIL_USER_8, TestDataSeeder.PASSWORD_USER_8)
            };

            var response = await service.CreateSessionAsync(new LoginRequestDto(email, password));

            Assert.Contains(expectedRole, response.UserRoles);
            Assert.True(response.ExpiresAt > response.CreatedAt);
        }

        [Fact]
        public async Task User13Unverified_CreateSession_ThrowsException_UserNotFound()
        {
            // User 13 ma Verified = false, więc próba logowania powinna zwrócić błąd UserNotFound
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            var requestDto = new LoginRequestDto
            (
                TestDataSeeder.EMAIL_USER_13,
                TestDataSeeder.PASSWORD_USER_13
            );

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateSessionAsync(requestDto));
            Assert.Equal(KoalaErrorCodes.InvalidCredentials, exception.ErrorCode);
        }

        [Fact]
        public async Task Unauthenticated_DeleteSession_ThrowsException_Unauthenticated()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            ClaimsPrincipal? unauthenticatedPrincipal = null;

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.DeleteSessionAsync(unauthenticatedPrincipal, TestDataSeeder.ID_SESSION_1));
            Assert.Equal(KoalaErrorCodes.Unauthenticated, exception.ErrorCode);
        }

        [Theory]
        [InlineData("missing")]
        [InlineData("unverified")]
        [InlineData("wrong-password")]
        public async Task CreateSession_InvalidCredentials_ThrowsInvalidCredentials(string scenario)
        {
            var (service, _, _) = await SetupTestEnvironment();
            var request = scenario switch
            {
                "missing" => new LoginRequestDto("missing@test.pl", TestDataSeeder.PASSWORD_USER_1),
                "unverified" => new LoginRequestDto(TestDataSeeder.EMAIL_USER_13, TestDataSeeder.PASSWORD_USER_13),
                _ => new LoginRequestDto(TestDataSeeder.EMAIL_USER_1, "WrongPassword1!")
            };

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateSessionAsync(request));

            Assert.Equal(KoalaErrorCodes.InvalidCredentials, exception.ErrorCode);
        }

        [Theory]
        [InlineData("delete")]
        [InlineData("deactivate")]
        [InlineData("get")]
        [InlineData("get-active")]
        [InlineData("delete-all")]
        public async Task SessionOperations_MissingUserClaim_ThrowUnauthenticated(string operation)
        {
            var (service, _, _) = await SetupTestEnvironment();
            var principal = AuthenticatedUser(null);
            var page = new PageQueryDto { PageNumber = 0, PageSize = 10 };

            var exception = await Assert.ThrowsAsync<KoalaException>(async () =>
            {
                switch (operation)
                {
                    case "delete": await service.DeleteSessionAsync(principal, TestDataSeeder.ID_SESSION_1); break;
                    case "deactivate": await service.DeactivateSessionAsync(principal, TestDataSeeder.ID_SESSION_1); break;
                    case "get": await service.GetSessionsAsync(principal, page); break;
                    case "get-active": await service.GetActiveSessionsAsync(principal, page); break;
                    default: await service.DeleteSessionsAsync(principal); break;
                }
            });

            Assert.Equal(KoalaErrorCodes.Unauthenticated, exception.ErrorCode);
        }

        [Fact]
        public async Task DeleteSession_OtherUsersSession_ThrowsSessionNotFound()
        {
            var (service, _, cache) = await SetupTestEnvironment();
            var principal = AuthenticatedUser(TestDataSeeder.ID_USER_3);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.DeleteSessionAsync(principal, TestDataSeeder.ID_SESSION_1));

            Assert.Equal(KoalaErrorCodes.SessionNotFound, exception.ErrorCode);
            cache.Verify(cacheService => cacheService.RemoveKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task DeactivateSession_ActiveSession_MarksInactiveAndEvictsCache()
        {
            var (service, db, cache) = await SetupTestEnvironment();

            await service.DeactivateSessionAsync(AuthenticatedUser(TestDataSeeder.ID_USER_1), TestDataSeeder.ID_SESSION_1);

            var session = await db.Sessions.FindAsync(TestDataSeeder.ID_SESSION_1);
            Assert.Equal(SessionStates.STATE_INACTIVE, session!.State);
            cache.Verify(cacheService => cacheService.RemoveKeyAsync($"koala:account:Session:{TestDataSeeder.TOKEN_SESSION_1}"), Times.Once);
        }

        [Fact]
        public async Task DeactivateSession_InactiveSession_ThrowsSessionNotFound()
        {
            var (service, _, cache) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.DeactivateSessionAsync(AuthenticatedUser(TestDataSeeder.ID_USER_1), TestDataSeeder.ID_SESSION_2));

            Assert.Equal(KoalaErrorCodes.SessionNotFound, exception.ErrorCode);
            cache.Verify(cacheService => cacheService.RemoveKeyAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task GetSessions_AppliesPaginationAndReportsUnpagedTotal()
        {
            var (service, _, _) = await SetupTestEnvironment();
            var page = new PageQueryDto { PageNumber = 1, PageSize = 1 };

            var (sessions, pagination) = await service.GetSessionsAsync(AuthenticatedUser(TestDataSeeder.ID_USER_1), page);

            Assert.Single(sessions);
            Assert.Equal(1, pagination.PageNumber);
            Assert.Equal(1, pagination.PageSize);
            Assert.Equal(3, pagination.TotalNumber);
        }
    }
}
