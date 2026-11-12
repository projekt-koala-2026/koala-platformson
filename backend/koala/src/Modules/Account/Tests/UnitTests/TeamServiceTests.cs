using System.Security.Claims;
using koala.src.Modules.Account.Data;
using koala.src.Modules.Account.Dtos;
using koala.src.Modules.Account.Entities;
using koala.src.Modules.Account.Services;
using koala.src.Shared;
using koala.src.Shared.Core;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace koala.src.Modules.Account.Tests.UnitTests
{
    public class TeamServiceTests
    {
        private static ClaimsPrincipal Principal(Guid userId, params string[] roles)
        {
            var claims = new List<Claim> { new(ClaimTypes.NameIdentifier, userId.ToString()) };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));
        }

        private static _EditionDto ActiveEdition(DateTime? opens = null, DateTime? closes = null, Guid? id = null) =>
            new _EditionDto(
                id ?? TestDataSeeder.MOCK_ID_EDITION_1,
                "Active Edition",
                "ACTIVE",
                opens ?? DateTime.UtcNow.AddDays(-1),
                closes ?? DateTime.UtcNow.AddDays(1),
                TestDataSeeder.TIME_CREATED,
                TestDataSeeder.TIME_CREATED,
                TestDataSeeder.VERSION);

        private static _SchoolDto School(Guid id) => new(
            id, "TEST", "TEST", "TEST", "TEST", "TEST", "TEST", "TEST",
            "TYPE_1_TIER_1", "TIER_1", "TEST", TestDataSeeder.TIME_CREATED,
            TestDataSeeder.TIME_CREATED, TestDataSeeder.VERSION);

        private Mock<ICoreModule> CreateCoreMock()
        {
            var core = new Mock<ICoreModule>();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync()).ReturnsAsync(ActiveEdition());
            core.Setup(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_1))
                .ReturnsAsync(School(TestDataSeeder.MOCK_ID_SCHOOL_1));
            core.Setup(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_2))
                .ReturnsAsync(School(TestDataSeeder.MOCK_ID_SCHOOL_2));
            return core;
        }

        private async Task<(TeamService Service, AccountDbContext Db, Mock<ICoreModule> Core)> SetupTestEnvironment()
        {
            var options = new DbContextOptionsBuilder<AccountDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            var _db = new AccountDbContext(options);
            _db = await TestDataSeeder.SeedDb(_db);

            var core = CreateCoreMock();

            var service = new TeamService(_db, core.Object);
            return (service, _db, core);
        }

        [Fact]
        public async Task User8_CreateTeam_ThrowException_Forbiden()
        {
            //NOTE: USER8 IS AN TEAM_ADMIN AND CANOT CREATE A TEAM
            var (service, db, coreMock) = await this.SetupTestEnvironment();

            coreMock.Setup(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_3))
                .ReturnsAsync(School(TestDataSeeder.MOCK_ID_SCHOOL_3));

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, TestDataSeeder.ID_USER_8.ToString()),
                new Claim(ClaimTypes.Role, TestDataSeeder.ID_ROLE_4.ToString())
            };
            var claimsPrincipal = new ClaimsPrincipal(new ClaimsIdentity(claims, "SessionCookie"));

            var requestDto = new TeamCreateRequestDto
            (
                "Nowa Druzyna Testowa",
                TestDataSeeder.MOCK_ID_SCHOOL_3
            );

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(claimsPrincipal, requestDto));
            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
            coreMock.Verify(module => module.Internal_GetTheActiveEditionAsync(), Times.Never);
            coreMock.Verify(module => module.Internal_GetSchoolAsync(It.IsAny<Guid>()), Times.Never);
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
            Assert.NotNull(result.TeamGuardian);
            Assert.Equal(TestDataSeeder.ID_USER_8, result.TeamGuardian.Id);
            Assert.Equal(TestDataSeeder.ID_USER_11, result.TeamCaptain.Id);
            Assert.NotEmpty(result.TeamMembers);
            Assert.Single(result.TeamMembers);
            coreMock.Verify(module => module.Internal_GetTheActiveEditionAsync(), Times.Once);
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

            var requestDto = new TeamMemberUpsertRequestDto
            (
                "Jan",
                "Kowalski",
                TestDataSeeder.VERSION
            );

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.AddTeamMemberAsync(claimsPrincipal, TestDataSeeder.ID_TEAM_1, requestDto));
            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
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

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.DeleteTeamMemberAsync(claimsPrincipal, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_2));
            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task Unauthenticated_CreateTeam_ThrowsException_Unauthenticated()
        {
            //NOTE: UNAUTHENTICATED USER CANOT CREATE A TEAM
            var (service, _, _) = await this.SetupTestEnvironment();

            ClaimsPrincipal? unauthenticatedPrincipal = null;

            var requestDto = new TeamCreateRequestDto
            (
                "Test Team",
                TestDataSeeder.MOCK_ID_SCHOOL_1
            );

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(unauthenticatedPrincipal, requestDto));

            Assert.Equal(KoalaErrorCodes.Unauthenticated, exception.ErrorCode);
        }

        [Fact]
        public async Task CreateTeam_NoActiveEdition_ThrowsActiveEditionNotFound()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync()).ReturnsAsync((_EditionDto?)null);

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(
                Principal(TestDataSeeder.ID_USER_14, TestDataSeeder.NAME_ROLE_5),
                new TeamCreateRequestDto("Team", TestDataSeeder.MOCK_ID_SCHOOL_1)));

            Assert.Equal(KoalaErrorCodes.ActiveEditionNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task CreateTeam_OutsideRegistrationWindow_ThrowsTeamChangesWindowClosed()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync())
                .ReturnsAsync(ActiveEdition(closes: DateTime.UtcNow.AddMinutes(-1)));

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(
                Principal(TestDataSeeder.ID_USER_14, TestDataSeeder.NAME_ROLE_5),
                new TeamCreateRequestDto("Team", TestDataSeeder.MOCK_ID_SCHOOL_1)));

            Assert.Equal(KoalaErrorCodes.TeamChangesWindowClosed, exception.ErrorCode);
        }

        [Fact]
        public async Task CreateTeam_SchoolNotFound_ThrowsSchoolNotFound()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(
                Principal(TestDataSeeder.ID_USER_14, TestDataSeeder.NAME_ROLE_5),
                new TeamCreateRequestDto("Team", TestDataSeeder.MOCK_ID_SCHOOL_3)));

            Assert.Equal(KoalaErrorCodes.SchoolNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task CreateTeam_UnverifiedUser_ThrowsUserNotFound()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_1))
                .ReturnsAsync(School(TestDataSeeder.MOCK_ID_SCHOOL_1));

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(
                Principal(TestDataSeeder.ID_USER_13, TestDataSeeder.NAME_ROLE_5),
                new TeamCreateRequestDto("Team", TestDataSeeder.MOCK_ID_SCHOOL_1)));

            Assert.Equal(KoalaErrorCodes.UserNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task CreateTeam_ExistingCaptain_ThrowsUserIsCaptainOfTeamAlready()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_1))
                .ReturnsAsync(School(TestDataSeeder.MOCK_ID_SCHOOL_1));

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5),
                new TeamCreateRequestDto("Team", TestDataSeeder.MOCK_ID_SCHOOL_1)));

            Assert.Equal(KoalaErrorCodes.UserIsCaptainOfTeamAlready, exception.ErrorCode);
        }

        [Fact]
        public async Task CreateTeam_ValidCaptain_CreatesTeamAndCaptainMember()
        {
            var (service, db, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_1))
                .ReturnsAsync(School(TestDataSeeder.MOCK_ID_SCHOOL_1));

            var result = await service.CreateTeamAsync(Principal(TestDataSeeder.ID_USER_9, TestDataSeeder.NAME_ROLE_5),
                new TeamCreateRequestDto("Created Team", TestDataSeeder.MOCK_ID_SCHOOL_1));

            Assert.Equal("Created Team", result.Name);
            Assert.Equal(TestDataSeeder.ID_USER_9, result.TeamCaptain.Id);
            Assert.Contains(await db.Teams.ToListAsync(), team => team.Id == result.Id);
            Assert.Contains(await db.TeamMembers.ToListAsync(), member => member.Id == TestDataSeeder.ID_USER_9 && member.TeamId == result.Id);
            core.Verify(module => module.Internal_GetTheActiveEditionAsync(), Times.Once);
            core.Verify(module => module.Internal_GetSchoolAsync(TestDataSeeder.MOCK_ID_SCHOOL_1), Times.Once);
        }

        [Fact]
        public async Task AddTeamMember_AtCapacity_ThrowsTeamMemberLimitExceeded()
        {
            var (service, db, _) = await SetupTestEnvironment();
            db.TeamMembers.Add(new TeamMember { Id = Guid.NewGuid(), TeamId = TestDataSeeder.ID_TEAM_1,
                NameFirst = "Extra", NameLast = "Member", RodoState = TeamMemberRodoState.RODOSTATE_NONE,
                CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow, Version = 1 });
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.AddTeamMemberAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1,
                new TeamMemberUpsertRequestDto("New", "Member", null)));

            Assert.Equal(KoalaErrorCodes.TeamMemberLimitExceeded, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateTeamMember_StaleVersion_ThrowsResourceVersionConflict()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateTeamMemberAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1,
                TestDataSeeder.ID_TEAM_MEMBER_1, new TeamMemberUpsertRequestDto("Updated", "Member", 99)));

            Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateTeamMember_MissingMember_ThrowsTeamMemberNotFound()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateTeamMemberAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1,
                Guid.NewGuid(), new TeamMemberUpsertRequestDto("Updated", "Member", 1)));

            Assert.Equal(KoalaErrorCodes.TeamMemberNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task DeleteTeamMember_MissingMember_ThrowsTeamMemberNotFound()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.DeleteTeamMemberAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1, Guid.NewGuid()));

            Assert.Equal(KoalaErrorCodes.TeamMemberNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateTeamName_StaleVersion_ThrowsResourceVersionConflict()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.UpdateTeamNameAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1,
                new TeamUpdateNameRequestDto("New name", 99)));

            Assert.Equal(KoalaErrorCodes.ResourceVersionConflict, exception.ErrorCode);
        }

        [Fact]
        public async Task UpdateTeamName_ValidVersion_UpdatesNameAndVersion()
        {
            var (service, db, _) = await SetupTestEnvironment();

            var result = await service.UpdateTeamNameAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1,
                new TeamUpdateNameRequestDto("Updated name", TestDataSeeder.VERSION));

            Assert.Equal("Updated name", result.Name);
            Assert.Equal(TestDataSeeder.VERSION + 1, result.Version);
            Assert.True(result.IsCensored);
            Assert.Equal("Updated name", (await db.Teams.FindAsync(TestDataSeeder.ID_TEAM_1))!.Name);
        }

        [Fact]
        public async Task CreateJoinTeamCode_CreatesEightCharacterCode()
        {
            var (service, db, _) = await SetupTestEnvironment();

            var code = await service.CreateJoinTeamCodeAsync(
                Principal(TestDataSeeder.ID_USER_11, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_2);

            Assert.Equal(8, code.JoinCode.Length);
            Assert.Equal(TestDataSeeder.ID_TEAM_2, (await db.TeamJoinCodes.SingleAsync(c => c.TeamId == TestDataSeeder.ID_TEAM_2)).TeamId);
            Assert.True(code.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task CreateJoinTeamCode_ReissuesExistingCode()
        {
            var (service, db, _) = await SetupTestEnvironment();
            var oldCreatedAt = (await db.TeamJoinCodes.FindAsync(TestDataSeeder.ID_TEAM_1))!.CreatedAt;

            var refreshed = await service.CreateJoinTeamCodeAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1);

            Assert.Equal(8, refreshed.JoinCode.Length);
            Assert.True(refreshed.CreatedAt >= oldCreatedAt);
            Assert.True(refreshed.ExpiresAt > DateTime.UtcNow);
        }

        [Fact]
        public async Task JoinTeamWithCode_UnknownCode_ThrowsTeamJoinCodeNotFound()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4), "MISSING1"));

            Assert.Equal(KoalaErrorCodes.TeamJoinCodeNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task JoinTeamWithCode_UserWithoutGuardianRole_ThrowsForbidden()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.CODE_TEAM_JOIN_CODE_1));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task JoinTeamWithCode_OrphanedCode_ThrowsTeamNotFound()
        {
            var (service, db, _) = await SetupTestEnvironment();
            db.TeamJoinCodes.Add(new TeamJoinCode
            {
                TeamId = Guid.NewGuid(), JoinCode = "ORPHAN01", CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow, ExpiresAt = DateTime.UtcNow.AddDays(1)
            });
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_9, TestDataSeeder.NAME_ROLE_4), "ORPHAN01"));

            Assert.Equal(KoalaErrorCodes.TeamNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task JoinTeamWithCode_ExpiredCode_ThrowsAndRemovesCode()
        {
            var (service, db, _) = await SetupTestEnvironment();
            db.TeamJoinCodes.Add(new TeamJoinCode { TeamId = TestDataSeeder.ID_TEAM_2, JoinCode = "EXPIRED1",
                CreatedAt = DateTime.UtcNow.AddDays(-2), UpdatedAt = DateTime.UtcNow.AddDays(-2), ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
            await db.SaveChangesAsync();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4), "EXPIRED1"));

            Assert.Equal(KoalaErrorCodes.TeamJoinCodeNotFound, exception.ErrorCode);
            Assert.Null(await db.TeamJoinCodes.FindAsync(TestDataSeeder.ID_TEAM_2));
        }

        [Fact]
        public async Task JoinTeamWithCode_TeamAlreadyHasGuardian_ThrowsTeamMemberLimitExceeded()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_9, TestDataSeeder.NAME_ROLE_4), TestDataSeeder.CODE_TEAM_JOIN_CODE_1));

            Assert.Equal(KoalaErrorCodes.TeamMemberLimitExceeded, exception.ErrorCode);
        }

        [Fact]
        public async Task JoinTeamWithCode_NoActiveEdition_ThrowsNoActiveEditionException()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync()).ReturnsAsync((_EditionDto?)null);

            await Assert.ThrowsAsync<NoActiveEditionException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4), TestDataSeeder.CODE_TEAM_JOIN_CODE_1));
        }

        [Fact]
        public async Task GetMyTeams_NoTeamRole_ThrowsForbidden()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetMyTeamsAsync(
                Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1), new PageQueryDto { PageNumber = 0, PageSize = 10 },
                new TeamQueryDto(null, null, null, null, null)));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task GetTeam_NonMemberWithoutAdminRole_ThrowsForbidden()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetTeamAsync(
                Principal(TestDataSeeder.ID_USER_4, TestDataSeeder.NAME_ROLE_2), TestDataSeeder.ID_TEAM_1));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task GetTeam_OrganizationAdminForMissingTeam_ThrowsTeamNotFound()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetTeamAsync(
                Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1), Guid.NewGuid()));

            Assert.Equal(KoalaErrorCodes.TeamNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task GetTeams_NonAdmin_ThrowsForbidden()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetTeamsAsync(
                Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4), new PageQueryDto { PageNumber = 0, PageSize = 10 },
                new TeamQueryDto(null, null, null, null, null), new TeamMemberQueryDto(null)));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Theory]
        [InlineData(4, "ORGANIZATION_EDITOR")]
        [InlineData(6, "ORGANIZATION_REVIEWER")]
        [InlineData(10, "TEAM_CAPTAIN")]
        public async Task GetTeams_EditorReviewerAndCaptainAreForbidden(int userNumber, string role)
        {
            var (service, _, _) = await SetupTestEnvironment();
            var userId = userNumber == 4 ? TestDataSeeder.ID_USER_4 : userNumber == 6 ? TestDataSeeder.ID_USER_6 : TestDataSeeder.ID_USER_10;

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetTeamsAsync(
                Principal(userId, role), new PageQueryDto { PageNumber = 0, PageSize = 10 },
                new TeamQueryDto(null, null, null, null, null), new TeamMemberQueryDto(null)));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task Captain_GetMyTeams_ReturnsOwnTeam()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var (teams, pagination) = await service.GetMyTeamsAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5),
                new PageQueryDto { PageNumber = 0, PageSize = 10 }, new TeamQueryDto(null, null, null, null, null));

            Assert.Contains(teams, team => team.Id == TestDataSeeder.ID_TEAM_1);
            Assert.Equal(1, pagination.TotalNumber);
        }

        [Fact]
        public async Task Captain_AddUpdateDeleteMember_ChangesPersistedTeamMembers()
        {
            var (service, db, _) = await SetupTestEnvironment();
            var captain = Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5);

            var added = await service.AddTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1,
                new TeamMemberUpsertRequestDto("New", "Member", null));
            var addedMember = Assert.Single(added, member => member.NameFist == "New");
            Assert.Equal(1, addedMember.Version);

            var updated = await service.UpdateTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, addedMember.TeamMemberId,
                new TeamMemberUpsertRequestDto("Updated", "Member", 1));
            Assert.Contains(updated, member => member.TeamMemberId == addedMember.TeamMemberId && member.NameFist == "Updated" && member.Version == 2);

            await service.DeleteTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, addedMember.TeamMemberId);
            Assert.Null(await db.TeamMembers.FindAsync(addedMember.TeamMemberId));
        }

        [Fact]
        public async Task Captain_DeleteTeam_RemovesOwnedTeam()
        {
            var (service, db, _) = await SetupTestEnvironment();

            await service.DeleteTeamAsync(Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1);

            Assert.Null(await db.Teams.FindAsync(TestDataSeeder.ID_TEAM_1));
            Assert.Empty(await db.TeamMembers.Where(member => member.TeamId == TestDataSeeder.ID_TEAM_1).ToListAsync());
        }

        [Fact]
        public async Task GetTeam_ReturnsForCaptainGuardianAndOrganizationAdmin()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var captainResult = await service.GetTeamAsync(Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_1);
            var guardianResult = await service.GetTeamAsync(Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4), TestDataSeeder.ID_TEAM_1);
            var adminResult = await service.GetTeamAsync(Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1), TestDataSeeder.ID_TEAM_1);

            Assert.Equal(TestDataSeeder.ID_TEAM_1, captainResult.Id);
            Assert.Equal(TestDataSeeder.ID_TEAM_1, guardianResult.Id);
            Assert.Equal(TestDataSeeder.ID_TEAM_1, adminResult.Id);
        }

        [Fact]
        public async Task OrganizationAdmin_GetTeams_ReturnsFilteredResults()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var (teams, pagination) = await service.GetTeamsAsync(
                Principal(TestDataSeeder.ID_USER_1, TestDataSeeder.NAME_ROLE_1),
                new PageQueryDto { PageNumber = 0, PageSize = 10 },
                new TeamQueryDto(null, null, null, null, null), new TeamMemberQueryDto(null));

            Assert.Equal(2, teams.Count);
            Assert.Equal(2, pagination.TotalNumber);
        }

        [Theory]
        [InlineData("admin", 1, "ORGANIZATION_ADMIN")]
        [InlineData("editor", 4, "ORGANIZATION_EDITOR")]
        [InlineData("reviewer", 6, "ORGANIZATION_REVIEWER")]
        [InlineData("guardian", 8, "TEAM_GUARDIAN")]
        public async Task CreateTeam_OnlyCaptainRoleIsAllowed(string _, int userNumber, string role)
        {
            var (service, _, _) = await SetupTestEnvironment();
            var userId = userNumber switch
            {
                1 => TestDataSeeder.ID_USER_1,
                4 => TestDataSeeder.ID_USER_4,
                6 => TestDataSeeder.ID_USER_6,
                _ => TestDataSeeder.ID_USER_8
            };

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.CreateTeamAsync(
                Principal(userId, role), new TeamCreateRequestDto("No access", TestDataSeeder.MOCK_ID_SCHOOL_1)));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task TeamEndpoints_RejectUnauthenticatedPrincipal()
        {
            var (service, _, _) = await SetupTestEnvironment();
            var page = new PageQueryDto { PageNumber = 0, PageSize = 10 };
            var teamQuery = new TeamQueryDto(null, null, null, null, null);
            var memberQuery = new TeamMemberQueryDto(null);
            var memberUpsert = new TeamMemberUpsertRequestDto("A", "B", 1);
            var calls = new Func<Task>[]
            {
                () => service.CreateTeamAsync(null, new TeamCreateRequestDto("T", TestDataSeeder.MOCK_ID_SCHOOL_1)),
                () => service.DeleteTeamAsync(null, TestDataSeeder.ID_TEAM_1),
                () => service.AddTeamMemberAsync(null, TestDataSeeder.ID_TEAM_1, memberUpsert),
                () => service.UpdateTeamMemberAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, memberUpsert),
                () => service.DeleteTeamMemberAsync(null, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1),
                () => service.UpdateTeamNameAsync(null, TestDataSeeder.ID_TEAM_1, new TeamUpdateNameRequestDto("T", 1)),
                () => service.CreateJoinTeamCodeAsync(null, TestDataSeeder.ID_TEAM_1),
                () => service.JoinTeamWithCodeAsync(null, TestDataSeeder.CODE_TEAM_JOIN_CODE_1),
                () => service.GetMyTeamsAsync(null, page, teamQuery),
                () => service.GetTeamAsync(null, TestDataSeeder.ID_TEAM_1),
                () => service.GetTeamsAsync(null, page, teamQuery, memberQuery)
            };

            foreach (var call in calls)
            {
                var exception = await Assert.ThrowsAsync<KoalaException>(call);
                Assert.Equal(KoalaErrorCodes.Unauthenticated, exception.ErrorCode);
            }
        }

        [Fact]
        public async Task TeamEndpoints_RequireAnActiveEditionWhereApplicable()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync()).ReturnsAsync((_EditionDto?)null);
            var captain = Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5);
            var guardian = Principal(TestDataSeeder.ID_USER_8, TestDataSeeder.NAME_ROLE_4);
            var member = new TeamMemberUpsertRequestDto("A", "B", 1);

            Assert.Equal(KoalaErrorCodes.ActiveEditionNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.CreateTeamAsync(Principal(TestDataSeeder.ID_USER_14, TestDataSeeder.NAME_ROLE_5),
                    new TeamCreateRequestDto("T", TestDataSeeder.MOCK_ID_SCHOOL_1)))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.ActiveEditionNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamAsync(captain, TestDataSeeder.ID_TEAM_1))).ErrorCode);
            await Assert.ThrowsAsync<NoActiveEditionException>(() => service.AddTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, member));
            await Assert.ThrowsAsync<NoActiveEditionException>(() => service.UpdateTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, member));
            await Assert.ThrowsAsync<NoActiveEditionException>(() => service.DeleteTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1));
            Assert.Equal(KoalaErrorCodes.ActiveEditionNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamNameAsync(captain, TestDataSeeder.ID_TEAM_1, new TeamUpdateNameRequestDto("T", 1)))).ErrorCode);
            await Assert.ThrowsAsync<NoActiveEditionException>(() => service.CreateJoinTeamCodeAsync(captain, TestDataSeeder.ID_TEAM_1));
            await Assert.ThrowsAsync<NoActiveEditionException>(() => service.JoinTeamWithCodeAsync(guardian, TestDataSeeder.CODE_TEAM_JOIN_CODE_1));
        }

        [Fact]
        public async Task CaptainManagementEndpoints_RejectTeamsOwnedByAnotherCaptain()
        {
            var (service, _, _) = await SetupTestEnvironment();
            var captain = Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5);
            var member = new TeamMemberUpsertRequestDto("A", "B", 1);

            Assert.Equal(KoalaErrorCodes.UserNotPartOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamAsync(captain, TestDataSeeder.ID_TEAM_2))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.AddTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_2, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_2, TestDataSeeder.ID_TEAM_MEMBER_3, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_2, TestDataSeeder.ID_TEAM_MEMBER_3))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamNameAsync(captain, TestDataSeeder.ID_TEAM_2, new TeamUpdateNameRequestDto("T", 1)))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.CreateJoinTeamCodeAsync(captain, TestDataSeeder.ID_TEAM_2))).ErrorCode);
        }

        [Fact]
        public async Task CaptainManagementEndpoints_RejectVerifiedUserWithoutCaptainTeam()
        {
            var (service, _, _) = await SetupTestEnvironment();
            var nonCaptain = Principal(TestDataSeeder.ID_USER_12, TestDataSeeder.NAME_ROLE_5);
            var member = new TeamMemberUpsertRequestDto("A", "B", 1);

            Assert.Equal(KoalaErrorCodes.UserNotCaptainOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamAsync(nonCaptain, TestDataSeeder.ID_TEAM_1))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotCaptainOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.AddTeamMemberAsync(nonCaptain, TestDataSeeder.ID_TEAM_1, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotCaptainOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamMemberAsync(nonCaptain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotCaptainOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamMemberAsync(nonCaptain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotCaptainOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamNameAsync(nonCaptain, TestDataSeeder.ID_TEAM_1, new TeamUpdateNameRequestDto("T", 1)))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotCaptainOfThisTeam, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.CreateJoinTeamCodeAsync(nonCaptain, TestDataSeeder.ID_TEAM_1))).ErrorCode);
        }

        [Fact]
        public async Task JoinTeamWithCode_CodeForInactiveEdition_ThrowsTeamJoinCodeNotFound()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync())
                .ReturnsAsync(ActiveEdition(id: Guid.NewGuid()));

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.JoinTeamWithCodeAsync(
                Principal(TestDataSeeder.ID_USER_9, TestDataSeeder.NAME_ROLE_4), TestDataSeeder.CODE_TEAM_JOIN_CODE_1));

            Assert.Equal(KoalaErrorCodes.TeamJoinCodeNotFound, exception.ErrorCode);
        }

        [Fact]
        public async Task GetMyTeams_EmptyUserIdClaim_ThrowsForbidden()
        {
            var (service, _, _) = await SetupTestEnvironment();
            var claims = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Role, TestDataSeeder.NAME_ROLE_4) }, "SessionCookie"));

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetMyTeamsAsync(
                claims, new PageQueryDto { PageNumber = 0, PageSize = 10 }, new TeamQueryDto(null, null, null, null, null)));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task GetTeam_TeamRoleWithoutMembership_ThrowsForbidden()
        {
            var (service, _, _) = await SetupTestEnvironment();

            var exception = await Assert.ThrowsAsync<KoalaException>(() => service.GetTeamAsync(
                Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5), TestDataSeeder.ID_TEAM_2));

            Assert.Equal(KoalaErrorCodes.Forbidden, exception.ErrorCode);
        }

        [Fact]
        public async Task TeamRegistrationMutations_OutsideWindow_ThrowTeamChangesWindowClosed()
        {
            var (service, _, core) = await SetupTestEnvironment();
            core.Setup(module => module.Internal_GetTheActiveEditionAsync())
                .ReturnsAsync(ActiveEdition(opens: DateTime.UtcNow.AddDays(-2), closes: DateTime.UtcNow.AddDays(-1)));
            var captain = Principal(TestDataSeeder.ID_USER_10, TestDataSeeder.NAME_ROLE_5);
            var member = new TeamMemberUpsertRequestDto("A", "B", 1);

            Assert.Equal(KoalaErrorCodes.TeamChangesWindowClosed, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.CreateTeamAsync(Principal(TestDataSeeder.ID_USER_14, TestDataSeeder.NAME_ROLE_5),
                    new TeamCreateRequestDto("T", TestDataSeeder.MOCK_ID_SCHOOL_1)))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamChangesWindowClosed, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.AddTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamChangesWindowClosed, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamChangesWindowClosed, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamMemberAsync(captain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.TeamChangesWindowClosed, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamNameAsync(captain, TestDataSeeder.ID_TEAM_1, new TeamUpdateNameRequestDto("T", 1)))).ErrorCode);
        }

        [Fact]
        public async Task CaptainManagementEndpoints_UnknownUser_ThrowUserNotFound()
        {
            var (service, _, _) = await SetupTestEnvironment();
            var unknownCaptain = Principal(Guid.NewGuid(), TestDataSeeder.NAME_ROLE_5);
            var member = new TeamMemberUpsertRequestDto("A", "B", 1);

            Assert.Equal(KoalaErrorCodes.UserNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamAsync(unknownCaptain, TestDataSeeder.ID_TEAM_1))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.AddTeamMemberAsync(unknownCaptain, TestDataSeeder.ID_TEAM_1, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamMemberAsync(unknownCaptain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1, member))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.DeleteTeamMemberAsync(unknownCaptain, TestDataSeeder.ID_TEAM_1, TestDataSeeder.ID_TEAM_MEMBER_1))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.UpdateTeamNameAsync(unknownCaptain, TestDataSeeder.ID_TEAM_1, new TeamUpdateNameRequestDto("T", 1)))).ErrorCode);
            Assert.Equal(KoalaErrorCodes.UserNotFound, (await Assert.ThrowsAsync<KoalaException>(() =>
                service.CreateJoinTeamCodeAsync(unknownCaptain, TestDataSeeder.ID_TEAM_1))).ErrorCode);
        }
    }
}
