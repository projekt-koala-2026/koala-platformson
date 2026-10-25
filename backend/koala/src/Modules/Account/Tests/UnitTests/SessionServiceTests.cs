using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Moq;
using Xunit;
using koala.src.Modules.Account.Dtos;

namespace koala.src.Modules.Account.Tests.UnitTests
{
    public class SessionServiceTests
    {
        private async Task<(ISessionService, AccountDbContext, Mock<ICacheService>)> SetupTestEnvironment()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var _db = new AccountDbContext(options);
            _db = await TestDataSeeder.SeedDb(_db);
            Mock<ICacheService> cacheService = new Mock<ICacheService>();
            ISessionService service = new SessionService(_db, cacheService.Object);
            return (service, _db, cacheService);
        }

        [Fact]
        public async Task User1_GetSessions_ReturnsActiveSessions()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();
            cacheMock
                .Setup(c => c.GetValueAtKeyAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

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
            var (response, pagination) = await service.GetSessionsAsync(claimsPrincipal, pageQueryDto);

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
            Assert.Contains("ORGANIZATION_ADMIN", response.UserRoles);
        }

        [Fact]
        public async Task User1_DeleteSession_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();
            cacheMock.Setup(c => c.RemoveKeyAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_1.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            await service.DeleteSessionAsync(claimsPrincipal, TestDataSeeder.ID_SESSION_1);

            var deletedSession = await _db.Sessions.FindAsync(TestDataSeeder.ID_SESSION_1);
            Assert.Null(deletedSession);
            cacheMock.Verify(c => c.RemoveKeyAsync(It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task User1_DeleteAllSessions_Success()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();
            cacheMock.Setup(c => c.RemoveKeyAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_1.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            await service.DeleteSessionsAsync(claimsPrincipal);

            var activeSessions = await _db.Sessions
                .Where(s => s.UserId == TestDataSeeder.ID_USER_1 && s.Active == true)
                .ToListAsync();

            Assert.Empty(activeSessions);
            cacheMock.Verify(c => c.RemoveKeyAsync(It.IsAny<string>()), Times.Exactly(2));
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
            cacheMock.Setup(c => c.RemoveKeyAsync(It.IsAny<string>())).Returns(Task.CompletedTask);

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
            Assert.Contains("ORGANIZATION_EDITOR", response.UserRoles);
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
            Assert.Contains("TEAM_ADMIN", response.UserRoles);
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

            var exception = await Assert.ThrowsAsync<AccountException>(() => service.CreateSessionAsync(requestDto));
            Assert.Equal(AccountErrorCodes.UserNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task Unauthenticated_DeleteSession_ThrowsException_Unauthenticated()
        {
            (var service, var _db, var cacheMock) = await this.SetupTestEnvironment();

            ClaimsPrincipal? unauthenticatedPrincipal = null;

            var exception = await Assert.ThrowsAsync<AccountException>(() => service.DeleteSessionAsync(unauthenticatedPrincipal, TestDataSeeder.ID_SESSION_1));
            Assert.Equal(AccountErrorCodes.Unauthenticated, exception.ErrorCode);
        }
    }
}