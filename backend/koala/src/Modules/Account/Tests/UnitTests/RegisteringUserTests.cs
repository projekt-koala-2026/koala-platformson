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
    public class RegisteringUserTests
    {
        [Fact]
        public async Task Can_Register_ORGANIZATION_USER_As_ORGANIZATION_ADMIN()
        {
            // PREPERE SERVICES
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var db = new AccountDbContext(options);
            var emailMock = new Mock<IEmailService>();
            var service = new UserService(db, emailMock.Object);

            // PREPERE TEST DATA
            db.Roles.AddRange(
                new Role { Id = Guid.CreateVersion7(), Name = "TEAM_ADMIN" },
                new Role { Id = Guid.CreateVersion7(), Name = "ORGANIZATION_ADMIN" }
            );
            await db.SaveChangesAsync();

            Guid userId = Guid.CreateVersion7();
            Guid sessionId = Guid.CreateVersion7();
            Guid token = Guid.CreateVersion7();
            List<string> roles = ["ORGANIZATION_ADMIN"];
            ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

            RegisterUserRequestDto requestDto = new RegisterUserRequestDto
            (
                "test@test.com",
                new List<string>(["ORGANIZATION_ADMIN"])
            );

            // TEST SERVICE
            await service.RegisterUserAsync(claimsPrincipal, requestDto);
            var link = await db.Links.Include(l => l.User).FirstOrDefaultAsync(l => l.User.Email == requestDto.Email);

            // ASSERT THE RETURN VALUE
            emailMock.Verify(
                x => x.SendRegisterEmailAsync(
                requestDto.Email,
                link.Token.ToString()),
            Times.Once);
        }
        [Fact]
        public async Task Can_Register_TEAM_USER_As_Anonymus()
        {
            // PREPERE SERVICES
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var db = new AccountDbContext(options);
            var emailMock = new Mock<IEmailService>();
            var service = new UserService(db, emailMock.Object);

            // PREPERE TEST DATA
            db.Roles.AddRange(
                new Role { Id = Guid.CreateVersion7(), Name = "TEAM_ADMIN" },
                new Role { Id = Guid.CreateVersion7(), Name = "ORGANIZATION_ADMIN" }
            );
            await db.SaveChangesAsync();

            ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestEmptyClaimsPrincipalAsync();
            RegisterUserRequestDto requestDto = new RegisterUserRequestDto
            (
                "test@test.com",
                new List<string>(["TEAM_ADMIN"])
            );

            // TEST SERVICE
            await service.RegisterUserAsync(claimsPrincipal, requestDto);
            var link = await db.Links.Include(l => l.User).FirstOrDefaultAsync(l => l.User.Email == requestDto.Email);

            // ASSERT THE RETURN VALUE
            emailMock.Verify(
                x => x.SendRegisterEmailAsync(
                requestDto.Email,
                link.Token.ToString()),
            Times.Once);
        }
    }
}