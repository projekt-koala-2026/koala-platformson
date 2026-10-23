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
    public class SessionsTests
    {
        [Fact]
        public async Task Get_User1_Sessions_Test()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var empty_db = new AccountDbContext(options);
            var _db = await TestDataSeeder.SeedDb(empty_db);

            var cacheMock = new Mock<ICacheService>();
            cacheMock
                .Setup(c => c.GetValueAtKeyAsync(It.IsAny<string>()))
                .ReturnsAsync((string?)null);

            var service = new SessionService(_db, cacheMock.Object);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_1.ToString()),
                new Claim("SessionId", TestDataSeeder.ID_SESSION_1.ToString()),
                new Claim("SessionToken", TestDataSeeder.TOKEN_SESSION_1.ToString()),

                new Claim(ClaimTypes.Role, TestDataSeeder.ID_ROLE_1.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.ID_ROLE_2.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.ID_ROLE_3.ToString()),
            };
            var identity = new ClaimsIdentity(claims, "SessionCookie");
            ClaimsPrincipal claimsPrincipal = new ClaimsPrincipal(identity);

            PageQueryDto pageQueryDto = new PageQueryDto
            {
                PageNumber = 0,
                PageSize = 16
            };

            (var response, var pagination) = await service.GetSessionsAsync(claimsPrincipal, pageQueryDto);

            Assert.Equal
            (
                response,
                new List<SessionDto>
                {
                    new SessionDto(TestDataSeeder.ID_SESSION_1, TestDataSeeder.ID_USER_1, TestDataSeeder.TIME_CREATED, TestDataSeeder.TIME_EXPIRES_SESSION),
                    new SessionDto(TestDataSeeder.ID_SESSION_3, TestDataSeeder.ID_USER_1, TestDataSeeder.TIME_CREATED, TestDataSeeder.TIME_EXPIRES_SESSION),
                }
            );
        }
    }
}