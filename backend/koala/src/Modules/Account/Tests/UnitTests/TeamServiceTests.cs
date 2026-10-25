using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Services;
using koala.src.Modules.Core.Dtos;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace koala.src.Modules.Account.Tests.UnitTests
{
    public class TeamServiceTests
    {
        private async Task<(TeamService Service, AccountDbContext Db, Mock<ICoreModule> CoreMock)> SetupTestEnvironment()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var _db = new AccountDbContext(options);
            _db = await TestDataSeeder.SeedDb(_db);

            var coreMock = new Mock<ICoreModule>();
            coreMock.Setup(c => c.Internal_GetTheActiveEditionAsync())
                .ReturnsAsync(new _EditionDto(TestDataSeeder.MOCK_ID_EDITION_1, "Active Edition", DateTime.UtcNow, null));

            var service = new TeamService(_db, coreMock.Object);
            return (service, _db, coreMock);
        }

        [Fact]
        public async Task User8_CreateTeam_ThrowException_Forbiden()
        {
            //NOTE: USER8 IS AN TEAM_ADMIN AND CANOT CREATE A TEAM
            var (service, db, coreMock) = await this.SetupTestEnvironment();

            coreMock.Setup(c => c.Internal_ExistSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_3))
                .ReturnsAsync(true);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.ID_ROLE_4.ToString())
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));

            var requestDto = new CreateTeamRequestDto
            (
                "Nowa Druzyna Testowa",
                TestDataSeeder.MOCK_ID_SCHOOL_3
            );

            var exception = await Assert.ThrowsAsync<AccountException>(() => service.CreateTeamAsync(claimsPrincipal, requestDto));
            Assert.Equal(AccountErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task User8_GetMyTeams_ReturnsTeams()
        {
            //NOTE: USER8 IS A TEAM_ADMIN AND IS PART OF A TEAM1
            var (service, _, _) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.NAME_ROLE_4)
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));

            var pageQuery = new PageQueryDto { PageNumber = 0, PageSize = 10 };
            var teamQuery = new TeamQueryDto(null, null, null, null, null);

            var (teams, pagination) = await service.GetMyTeamsAsync(claimsPrincipal, pageQuery, teamQuery);

            Assert.NotEmpty(teams);
            Assert.Contains(teams, t => t.Id == TestDataSeeder.ID_TEAM_1);
            Assert.Equal(1, pagination.TotalNumber);
        }

        [Fact]
        public async Task User8_JoinTeamWithCode_Success()
        {
            //NOTE: USER8 IS A TEAM_ADMIN AND CAN JOIN TO BE A TEAM_ADMIN OF TEAM2
            var (service, db, coreMock) = await this.SetupTestEnvironment();

            //ADING A TEAMJOINCODE FOR TEAM2
            var newCode = new TeamJoinCode
            {
                TeamId = TestDataSeeder.ID_TEAM_2,
                JoinCode = "ABC12345",
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(1)
            };
            await db.TeamJoinCodes.AddAsync(newCode);
            await db.SaveChangesAsync();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.NAME_ROLE_4)
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));

            var result = await service.JoinTeamWithCodeAsync(claimsPrincipal, "ABC12345");

            Assert.NotNull(result);
            Assert.Equal(TestDataSeeder.ID_TEAM_2, result.Id);
            Assert.NotNull(result.TeamAdmin);
            Assert.Equal(TestDataSeeder.ID_USER_8, result.TeamAdmin.Id);
            Assert.Equal(TestDataSeeder.ID_USER_11, result.TeamCaptain.Id);
            Assert.NotEmpty(result.TeamMembers);
            Assert.Single(result.TeamMembers);
        }

        [Fact]
        public async Task User8_AddTeamMember_ThrowException_Forbiden()
        {
            //NOTE: USER8 IS A TEAM_ADMIN AND CANOT ADD A TEAM_MEMBER
            var (service, _, coreMock) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.NAME_ROLE_4)
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));

            var requestDto = new MemberUpsertRequestDto
            (
                "Jan",
                "Kowalski"
            );

            var exception = await Assert.ThrowsAsync<AccountException>(() => service.AddTeamMemberAsync(claimsPrincipal, TestDataSeeder.ID_TEAM_1, requestDto));
            Assert.Equal(AccountErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task User8_DeleteTeamMember_ThrowException_Forbiden()
        {
            //NOTE: USER8 IS A TEAM_ADMIN AND IT CANOT DELETE TEAM_MEMBER
            var (service, _, _) = await this.SetupTestEnvironment();

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.NAME_ROLE_4)
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));

            var exception = await Assert.ThrowsAsync<AccountException>(() => service.DeleteTeamMemberAsync(claimsPrincipal, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_2));
            Assert.Equal(AccountErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task Unauthenticated_CreateTeam_ThrowsException_Unauthenticated()
        {
            //NOTE: UNAUTHENTICATED USER CANOT CREATE A TEAM
            var (service, _, coreMock) = await this.SetupTestEnvironment();
            coreMock.Setup(c => c.Internal_ExistSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_1))
                .ReturnsAsync(true);

            ClaimsPrincipal? unauthenticatedPrincipal = null;

            var requestDto = new CreateTeamRequestDto
            (
                "Test Team",
                TestDataSeeder.MOCK_ID_SCHOOL_1
            );

            var exception = await Assert.ThrowsAsync<AccountException>(() => service.CreateTeamAsync(unauthenticatedPrincipal, requestDto));

            Assert.Equal(AccountErrorCodes.Unauthenticated, exception.ErrorCode);
        }
    }
}