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
    public class TestDataSeeder
    {
        public static Guid ID_ROLE_1 = Guid.CreateVersion7();
        public static Guid ID_ROLE_2 = Guid.CreateVersion7();
        public static Guid ID_ROLE_3 = Guid.CreateVersion7();
        public static Guid ID_ROLE_4 = Guid.CreateVersion7();
        public static Guid ID_ROLE_5 = Guid.CreateVersion7();
        public static Guid ID_USER_1 = Guid.CreateVersion7();
        public static Guid ID_USER_2 = Guid.CreateVersion7();
        public static Guid ID_USER_3 = Guid.CreateVersion7();
        public static Guid ID_USER_4 = Guid.CreateVersion7();
        public static Guid ID_USER_5 = Guid.CreateVersion7();
        public static Guid ID_USER_6 = Guid.CreateVersion7();
        public static Guid ID_USER_7 = Guid.CreateVersion7();
        public static Guid ID_USER_8 = Guid.CreateVersion7();
        public static Guid ID_USER_9 = Guid.CreateVersion7();
        public static Guid ID_USER_10 = Guid.CreateVersion7();
        public static Guid ID_USER_11 = Guid.CreateVersion7();
        public static Guid ID_USER_12 = Guid.CreateVersion7();
        public static Guid ID_USER_13 = Guid.CreateVersion7();
        public static Guid ID_USER_14 = Guid.CreateVersion7();
        public static Guid ID_USER_15 = Guid.CreateVersion7();
        public static Guid ID_TEAM_1 = Guid.CreateVersion7();
        public static Guid ID_TEAM_2 = Guid.CreateVersion7();
        public static Guid ID_LINK_1 = Guid.CreateVersion7();
        public static Guid ID_LINK_2 = Guid.CreateVersion7();
        public static Guid ID_LINK_3 = Guid.CreateVersion7();
        public static Guid ID_LINK_4 = Guid.CreateVersion7();
        public static Guid ID_MEMBER_1 = Guid.CreateVersion7();
        public static Guid ID_MEMBER_2 = Guid.CreateVersion7();
        public static Guid ID_MEMBER_3 = Guid.CreateVersion7();
        public static Guid ID_TEAM_MEMBER_1 = Guid.CreateVersion7();
        public static Guid ID_TEAM_MEMBER_2 = Guid.CreateVersion7();
        public static Guid ID_TEAM_MEMBER_3 = Guid.CreateVersion7();
        public static Guid ID_TEAM_MEMBER_4 = Guid.CreateVersion7();
        public static Guid ID_TEAM_MEMBER_5 = Guid.CreateVersion7();
        public static Guid ID_TEAM_JOIN_CODE_1 = Guid.CreateVersion7();
        public static Guid ID_SESSION_1 = Guid.CreateVersion7();
        public static Guid ID_SESSION_2 = Guid.CreateVersion7();
        public static Guid ID_SESSION_3 = Guid.CreateVersion7();
        public static Guid ID_SESSION_4 = Guid.CreateVersion7();
        public static Guid ID_SESSION_5 = Guid.CreateVersion7();
        public static Guid ID_SESSION_6 = Guid.CreateVersion7();
        public static Guid ID_SESSION_7 = Guid.CreateVersion7();
        public static Guid ID_SESSION_8 = Guid.CreateVersion7();
        public static Guid TOKEN_LINK_1 = Guid.CreateVersion7();
        public static Guid TOKEN_LINK_2 = Guid.CreateVersion7();
        public static Guid TOKEN_LINK_3 = Guid.CreateVersion7();
        public static Guid TOKEN_LINK_4 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_1 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_2 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_3 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_4 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_5 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_6 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_7 = Guid.CreateVersion7();
        public static Guid TOKEN_SESSION_8 = Guid.CreateVersion7();
        public static Guid MOCK_ID_SCHOOL_1 = Guid.CreateVersion7();
        public static Guid MOCK_ID_SCHOOL_2 = Guid.CreateVersion7();
        public static Guid MOCK_ID_SCHOOL_3 = Guid.CreateVersion7();
        public static Guid MOCK_ID_EDITION_1 = Guid.CreateVersion7();
        public static string CODE_TEAM_JOIN_CODE_1 = "XAB287C3";
        public static string EMAIL_USER_1 = "user1@test.pl";
        public static string EMAIL_USER_2 = "user1@test.pl";
        public static string EMAIL_USER_3 = "user3@test.pl";
        public static string EMAIL_USER_4 = "user4@test.pl";
        public static string EMAIL_USER_5 = "user5@test.pl";
        public static string EMAIL_USER_6 = "user6@test.pl";
        public static string EMAIL_USER_7 = "user7@test.pl";
        public static string EMAIL_USER_8 = "user8@test.pl";
        public static string EMAIL_USER_9 = "user9@test.pl";
        public static string EMAIL_USER_10 = "user10@test.pl";
        public static string EMAIL_USER_11 = "user11@test.pl";
        public static string EMAIL_USER_12 = "user12@test.pl";
        public static string EMAIL_USER_13 = "user13@test.pl";
        public static string EMAIL_USER_14 = "user14@test.pl";
        public static string EMAIL_USER_15 = "user15@test.pl";
        public static string PASSWORD_USER_1 = "user1@";
        public static string PASSWORD_USER_2 = "user2@";
        public static string PASSWORD_USER_3 = "user3@";
        public static string PASSWORD_USER_4 = "user4@";
        public static string PASSWORD_USER_5 = "user5@";
        public static string PASSWORD_USER_6 = "user6@";
        public static string PASSWORD_USER_7 = "user7@";
        public static string PASSWORD_USER_8 = "user8@";
        public static string PASSWORD_USER_9 = "user9@";
        public static string PASSWORD_USER_10 = "user10@";
        public static string PASSWORD_USER_11 = "user11@";
        public static string PASSWORD_USER_12 = "user12@";
        public static string PASSWORD_USER_13 = "user13@";
        public static string PASSWORD_USER_14 = "user14@";
        public static string PASSWORD_USER_15 = "user15@";
        public static string NAME_ROLE_1 = "ORGANIZATION_ADMIN";
        public static string NAME_ROLE_2 = "ORGANIZATION_EDITOR";
        public static string NAME_ROLE_3 = "ORGANIZATION_REVIEWER";
        public static string NAME_ROLE_4 = "TEAM_ADMIN";
        public static string NAME_ROLE_5 = "TEAM_PLAYER";
        public static string NAME_TEAM_1 = "TEAM_1";
        public static string NAME_TEAM_2 = "TEAM_2";
        public static string NAME_FIRST_USER_1 = "Alicja";
        public static string NAME_FIRST_USER_2 = "Jakub";
        public static string NAME_FIRST_USER_3 = "Bartek";
        public static string NAME_FIRST_USER_4 = "Kajetan";
        public static string NAME_FIRST_USER_5 = "Wojtek";
        public static string NAME_FIRST_USER_6 = "Igor";
        public static string NAME_FIRST_USER_7 = "Maciej";
        public static string NAME_FIRST_USER_8 = "Mateusz";
        public static string NAME_FIRST_USER_9 = "Adam";
        public static string NAME_FIRST_USER_10 = "Filip";
        public static string NAME_FIRST_USER_11 = "Barbara";
        public static string NAME_FIRST_USER_12 = "Ela";
        public static string NAME_FIRST_USER_13 = "Jeremi";
        public static string NAME_FIRST_USER_14 = "Teresa";
        public static string NAME_FIRST_USER_15 = "Julka";
        public static string NAME_LAST_USER_1 = "Sołtysiak";
        public static string NAME_LAST_USER_2 = "Panasewicz";
        public static string NAME_LAST_USER_3 = "Sztos";
        public static string NAME_LAST_USER_4 = "Rybak";
        public static string NAME_LAST_USER_5 = "Nowak";
        public static string NAME_LAST_USER_6 = "Nowacki";
        public static string NAME_LAST_USER_7 = "Kowalski";
        public static string NAME_LAST_USER_8 = "Proza";
        public static string NAME_LAST_USER_9 = "Małysz";
        public static string NAME_LAST_USER_10 = "Wojtyło";
        public static string NAME_LAST_USER_11 = "Oziębła";
        public static string NAME_LAST_USER_12 = "Warzywo";
        public static string NAME_LAST_USER_13 = "Kucharek";
        public static string NAME_LAST_USER_14 = "Lewandowska";
        public static string NAME_LAST_USER_15 = "Zielińska";
        public static string NAME_FIRST_MEMBER_1 = "Kamil";
        public static string NAME_FIRST_MEMBER_2 = "Robert";
        public static string NAME_FIRST_MEMBER_3 = "Olga";
        public static string NAME_LAST_MEMBER_1 = "Kucharek";
        public static string NAME_LAST_MEMBER_2 = "Lewandowska";
        public static string NAME_LAST_MEMBER_3 = "Zielińska";
        public static string TYPE_LINK_1 = "REGISTER";
        public static string TYPE_LINK_2 = "RESETPASSWORD";
        public static string POSITION_USER_TEAM_1 = "ADMIN";
        public static string POSITION_USER_TEAM_2 = "CAPTAIN";
        public static DateTime TIME_CREATED = DateTime.UtcNow;
        public static DateTime TIME_EXPIRES_SESSION = TIME_CREATED.AddHours(12);
        public static DateTime TIME_EXPIRES_LINK = TIME_CREATED.AddDays(1);
        public static DateTime TIME_EXPIRES_TEAM_JOIN_CODE = TIME_CREATED.AddDays(1);
        public static async Task<AccountDbContext> SeedDb(AccountDbContext db)
        {
            List<Role> roles = new List<Role>
            {
                new Role { Id = ID_ROLE_1, Name = NAME_ROLE_1},
                new Role { Id = ID_ROLE_2, Name = NAME_ROLE_2},
                new Role { Id = ID_ROLE_3, Name = NAME_ROLE_3},
                new Role { Id = ID_ROLE_4, Name = NAME_ROLE_4},
                new Role { Id = ID_ROLE_5, Name = NAME_ROLE_5},
            };
            List<User> users = new List<User>
            {
                new User { Id = ID_USER_1, NameFirst = NAME_FIRST_USER_1, NameLast = NAME_LAST_USER_1, Email = EMAIL_USER_1, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_1), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_2, NameFirst = NAME_FIRST_USER_2, NameLast = NAME_LAST_USER_2, Email = EMAIL_USER_2, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_2), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_3, NameFirst = NAME_FIRST_USER_3, NameLast = NAME_LAST_USER_3, Email = EMAIL_USER_3, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_3), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_4, NameFirst = NAME_FIRST_USER_4, NameLast = NAME_LAST_USER_4, Email = EMAIL_USER_4, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_4), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_5, NameFirst = NAME_FIRST_USER_5, NameLast = NAME_LAST_USER_5, Email = EMAIL_USER_5, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_5), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_6, NameFirst = NAME_FIRST_USER_6, NameLast = NAME_LAST_USER_6, Email = EMAIL_USER_6, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_6), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_7, NameFirst = NAME_FIRST_USER_7, NameLast = NAME_LAST_USER_7, Email = EMAIL_USER_7, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_7), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_8, NameFirst = NAME_FIRST_USER_8, NameLast = NAME_LAST_USER_8, Email = EMAIL_USER_8, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_8), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_9, NameFirst = NAME_FIRST_USER_9, NameLast = NAME_LAST_USER_9, Email = EMAIL_USER_9, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_9), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_10, NameFirst = NAME_FIRST_USER_10, NameLast = NAME_LAST_USER_10, Email = EMAIL_USER_10, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_10), Verified = true, VerifiedAt = TIME_CREATED, Censored = false, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_11, NameFirst = NAME_FIRST_USER_11, NameLast = NAME_LAST_USER_11, Email = EMAIL_USER_11, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_11), Verified = true, VerifiedAt = TIME_CREATED, Censored = true, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_12, NameFirst = NAME_FIRST_USER_12, NameLast = NAME_LAST_USER_12, Email = EMAIL_USER_12, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_12), Verified = true, VerifiedAt = TIME_CREATED, Censored = true, CreatedAt = TIME_CREATED, AcceptedRodo = true, AcceptedRules = true},
                new User { Id = ID_USER_13, NameFirst = NAME_FIRST_USER_13, NameLast = NAME_LAST_USER_13, Email = EMAIL_USER_13, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_13), Verified = false, VerifiedAt = null, Censored = true, CreatedAt = TIME_CREATED, AcceptedRodo = false, AcceptedRules = false},
                new User { Id = ID_USER_14, NameFirst = NAME_FIRST_USER_14, NameLast = NAME_LAST_USER_14, Email = EMAIL_USER_14, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_14), Verified = false, VerifiedAt = null, Censored = true, CreatedAt = TIME_CREATED, AcceptedRodo = false, AcceptedRules = false},
                new User { Id = ID_USER_15, NameFirst = NAME_FIRST_USER_15, NameLast = NAME_LAST_USER_15, Email = EMAIL_USER_15, PasswordHash = PasswordHasher.Hash(PASSWORD_USER_15), Verified = false, VerifiedAt = null, Censored = true, CreatedAt = TIME_CREATED, AcceptedRodo = false, AcceptedRules = false},
            };
            List<UserRole> userRoles = new List<UserRole>
            {
                new UserRole { UserId = ID_USER_1, RoleId = ID_ROLE_1},
                new UserRole { UserId = ID_USER_1, RoleId = ID_ROLE_2},
                new UserRole { UserId = ID_USER_1, RoleId = ID_ROLE_3},
                new UserRole { UserId = ID_USER_2, RoleId = ID_ROLE_1},
                new UserRole { UserId = ID_USER_3, RoleId = ID_ROLE_2},
                new UserRole { UserId = ID_USER_3, RoleId = ID_ROLE_3},
                new UserRole { UserId = ID_USER_4, RoleId = ID_ROLE_2},
                new UserRole { UserId = ID_USER_5, RoleId = ID_ROLE_2},
                new UserRole { UserId = ID_USER_6, RoleId = ID_ROLE_3},
                new UserRole { UserId = ID_USER_7, RoleId = ID_ROLE_3},
                new UserRole { UserId = ID_USER_8, RoleId = ID_ROLE_4},
                new UserRole { UserId = ID_USER_9, RoleId = ID_ROLE_4},
                new UserRole { UserId = ID_USER_10, RoleId = ID_ROLE_5},
                new UserRole { UserId = ID_USER_11, RoleId = ID_ROLE_5},
                new UserRole { UserId = ID_USER_12, RoleId = ID_ROLE_5},
                new UserRole { UserId = ID_USER_12, RoleId = ID_ROLE_4},
                new UserRole { UserId = ID_USER_13, RoleId = ID_ROLE_1},
                new UserRole { UserId = ID_USER_14, RoleId = ID_ROLE_5},
                new UserRole { UserId = ID_USER_15, RoleId = ID_ROLE_5},
            };
            List<Team> teams = new List<Team>
            {
                new Team { Id = ID_TEAM_1, Name = NAME_TEAM_1, EditionId = MOCK_ID_EDITION_1, SchoolId = MOCK_ID_SCHOOL_1, NameAccepted = true, CreatedAt = TIME_CREATED},
                new Team { Id = ID_TEAM_2, Name = NAME_TEAM_2, EditionId = MOCK_ID_EDITION_1, SchoolId = MOCK_ID_SCHOOL_2, NameAccepted = false, CreatedAt = TIME_CREATED},
            };
            List<UserTeam> userTeams = new List<UserTeam>
            {
                new UserTeam { UserId = ID_USER_8, TeamId = ID_TEAM_1, Position = POSITION_USER_TEAM_1},
                new UserTeam { UserId = ID_USER_10, TeamId = ID_TEAM_1, Position = POSITION_USER_TEAM_2},
                new UserTeam { UserId = ID_USER_11, TeamId = ID_TEAM_2, Position = POSITION_USER_TEAM_2},
            };
            List<Member> members = new List<Member>
            {
                new Member { Id = ID_MEMBER_1, NameFirst = NAME_FIRST_MEMBER_1, NameLast = NAME_LAST_MEMBER_1},
                new Member { Id = ID_MEMBER_2, NameFirst = NAME_FIRST_MEMBER_2, NameLast = NAME_LAST_MEMBER_2},
                new Member { Id = ID_MEMBER_3, NameFirst = NAME_FIRST_MEMBER_3, NameLast = NAME_LAST_MEMBER_3},
            };
            List<TeamMember> teamMembers = new List<TeamMember>
            {
                new TeamMember { Id = ID_TEAM_MEMBER_1, MemberId = null, TeamId = ID_TEAM_1},
                new TeamMember { Id = ID_TEAM_MEMBER_2, MemberId = ID_MEMBER_1, TeamId = ID_TEAM_1},
                new TeamMember { Id = ID_TEAM_MEMBER_3, MemberId = ID_MEMBER_2, TeamId = ID_TEAM_1},
                new TeamMember { Id = ID_TEAM_MEMBER_4, MemberId = null, TeamId = ID_TEAM_2},
                new TeamMember { Id = ID_TEAM_MEMBER_5, MemberId = ID_MEMBER_3, TeamId = ID_TEAM_2},
            };
            List<TeamJoinCode> teamJoinCodes = new List<TeamJoinCode>
            {
                new TeamJoinCode { TeamId = ID_TEAM_1, JoinCode = CODE_TEAM_JOIN_CODE_1, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_TEAM_JOIN_CODE},
            };
            List<Rodo> rodos = new List<Rodo>
            {
                // NOTHING HERE
            };
            List<Link> links = new List<Link>
            {
                new Link { Id = ID_LINK_1, UserId = ID_USER_13, Token = TOKEN_LINK_1, Active = true, Type = TYPE_LINK_1, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_LINK},
                new Link { Id = ID_LINK_2, UserId = ID_USER_14, Token = TOKEN_LINK_2, Active = true, Type = TYPE_LINK_1, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_LINK},
                new Link { Id = ID_LINK_3, UserId = ID_USER_15, Token = TOKEN_LINK_3, Active = true, Type = TYPE_LINK_1, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_LINK},
                new Link { Id = ID_LINK_4, UserId = ID_USER_12, Token = TOKEN_LINK_4, Active = true, Type = TYPE_LINK_2, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_LINK},
            };
            List<Session> sessions = new List<Session>
            {
                new Session { Id = ID_SESSION_1, UserId = ID_USER_1, Token = TOKEN_SESSION_1, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_2, UserId = ID_USER_1, Token = TOKEN_SESSION_2, Active = false, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_3, UserId = ID_USER_1, Token = TOKEN_SESSION_3, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_4, UserId = ID_USER_3, Token = TOKEN_SESSION_4, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_5, UserId = ID_USER_8, Token = TOKEN_SESSION_5, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_6, UserId = ID_USER_10, Token = TOKEN_SESSION_6, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_7, UserId = ID_USER_11, Token = TOKEN_SESSION_7, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
                new Session { Id = ID_SESSION_8, UserId = ID_USER_12, Token = TOKEN_SESSION_8, Active = true, CreatedAt = TIME_CREATED, ExpiresAt = TIME_EXPIRES_SESSION},
            };

            await db.Users.AddRangeAsync(users);
            await db.Roles.AddRangeAsync(roles);
            await db.Teams.AddRangeAsync(teams);
            await db.Links.AddRangeAsync(links);
            await db.Sessions.AddRangeAsync(sessions);
            await db.Members.AddRangeAsync(members);
            await db.TeamJoinCodes.AddRangeAsync(teamJoinCodes);
            await db.TeamMembers.AddRangeAsync(teamMembers);
            await db.UserTeams.AddRangeAsync(userTeams);
            await db.UserRoles.AddRangeAsync(userRoles);

            await db.SaveChangesAsync();

            return db;
        }
    }
}