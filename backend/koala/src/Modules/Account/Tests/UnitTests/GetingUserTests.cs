// using System.Security.Claims;
// using koala.src.Modules.Account.Data;
// using koala.src.Modules.Account.Dtos;
// using koala.src.Modules.Account.Entities;
// using koala.src.Modules.Account.Services;
// using koala.src.Shared;
// using Microsoft.EntityFrameworkCore;
// using Moq;
// using Xunit;

// namespace koala.src.Modules.Account.Tests.UnitTests
// {
//     public class GetingUserTests
//     {
//         [Fact]
//         public async Task Can_Get_Diffrent_User_As_ORGANIZATION_ADMIN()
//         {
//             // PREPERE SERVICES
//             var options = new DbContextOptionsBuilder<AccountDbContext>()
//                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                 .Options;
//             await using var db = new AccountDbContext(options);
//             var emailMock = new Mock<IEmailService>();
//             var service = new UserService(db, emailMock.Object);

//             // PREPERE TEST DATA
//             Guid tempRoleId1 = Guid.CreateVersion7();

//             Guid tempUserId = Guid.CreateVersion7();
//             string tempUserEmail = "test@test.com";
//             string tempUserNameFirst = "First";
//             string tempUserNameLast = "Last";
//             bool tempUserCensored = false;


//             Guid userId = Guid.CreateVersion7();
//             Guid sessionId = Guid.CreateVersion7();
//             Guid token = Guid.CreateVersion7();
//             List<string> roles = ["ORGANIZATION_ADMIN"];

//             await db.Roles.AddRangeAsync(
//                  new Role { Id = tempRoleId1, Name = "TEAM_ADMIN" }
//              );
//             await db.Users.AddAsync(
//                 new User
//                 {
//                     Id = tempUserId,
//                     Email = tempUserEmail,
//                     NameFirst = tempUserNameFirst,
//                     NameLast = tempUserNameLast,
//                     Censored = tempUserCensored,
//                     Verified = true
//                 }
//             );
//             db.UserRoles.Add(new UserRole { UserId = tempUserId, RoleId = tempRoleId1 });
//             await db.SaveChangesAsync();


//             ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

//             // TEST SERVICE
//             var user = await service.GetUserAsync(claimsPrincipal, tempUserId);

//             // ASSERT THE RETURN VALUE
//             Assert.NotNull(user);
//             Assert.Equal(user.Id, tempUserId);
//             Assert.Equal(user.Email, tempUserEmail);
//             Assert.Equal(user.NameFirst, tempUserNameFirst);
//             Assert.Equal(user.NameLast, tempUserNameLast);
//             Assert.Equal(user.Censored, tempUserCensored);
//             Assert.Equal(user.Roles, ["TEAM_ADMIN"]);
//         }
//         [Fact]
//         public async Task Can_Get_Myself_As_ORGANIZATION_ADMIN()
//         {
//             // PREPERE SERVICES
//             var options = new DbContextOptionsBuilder<AccountDbContext>()
//                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                 .Options;
//             await using var db = new AccountDbContext(options);
//             var emailMock = new Mock<IEmailService>();
//             var service = new UserService(db, emailMock.Object);

//             // PREPERE TEST DATA
//             Guid tempRoleId1 = Guid.CreateVersion7();

//             string tempUserEmail = "test@test.com";
//             string tempUserNameFirst = "First";
//             string tempUserNameLast = "Last";
//             bool tempUserCensored = false;


//             Guid userId = Guid.CreateVersion7();
//             Guid sessionId = Guid.CreateVersion7();
//             Guid token = Guid.CreateVersion7();
//             List<string> roles = ["ORGANIZATION_ADMIN"];

//             db.Roles.AddRange(
//                 new Role { Id = tempRoleId1, Name = "ORGANIZATION_ADMIN" }
//             );
//             db.Users.Add(
//                 new User
//                 {
//                     Id = userId,
//                     Email = tempUserEmail,
//                     NameFirst = tempUserNameFirst,
//                     NameLast = tempUserNameLast,
//                     Censored = tempUserCensored,
//                     Verified = true
//                 }
//             );
//             db.UserRoles.Add(new UserRole { UserId = userId, RoleId = tempRoleId1 });
//             await db.SaveChangesAsync();


//             ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

//             // TEST SERVICE
//             var user = await service.GetUserAsync(claimsPrincipal, userId);

//             // ASSERT THE RETURN VALUE
//             Assert.NotNull(user);
//             Assert.Equal(user.Id, userId);
//             Assert.Equal(user.Email, tempUserEmail);
//             Assert.Equal(user.NameFirst, tempUserNameFirst);
//             Assert.Equal(user.NameLast, tempUserNameLast);
//             Assert.Equal(user.Censored, tempUserCensored);
//             Assert.Equal(user.Roles, roles);
//         }

//         [Fact]
//         public async Task Can_Get_Myself_As_ORGANIZATION_EDITOR()
//         {
//             // PREPERE SERVICES
//             var options = new DbContextOptionsBuilder<AccountDbContext>()
//                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                 .Options;
//             await using var db = new AccountDbContext(options);
//             var emailMock = new Mock<IEmailService>();
//             var service = new UserService(db, emailMock.Object);

//             // PREPERE TEST DATA
//             Guid tempRoleId1 = Guid.CreateVersion7();

//             string tempUserEmail = "test@test.com";
//             string tempUserNameFirst = "First";
//             string tempUserNameLast = "Last";
//             bool tempUserCensored = false;


//             Guid userId = Guid.CreateVersion7();
//             Guid sessionId = Guid.CreateVersion7();
//             Guid token = Guid.CreateVersion7();
//             List<string> roles = ["ORGANIZATION_EDITOR"];

//             db.Roles.AddRange(
//                 new Role { Id = tempRoleId1, Name = "ORGANIZATION_EDITOR" }
//             );
//             db.Users.Add(
//                 new User
//                 {
//                     Id = userId,
//                     Email = tempUserEmail,
//                     NameFirst = tempUserNameFirst,
//                     NameLast = tempUserNameLast,
//                     Censored = tempUserCensored,
//                     Verified = true
//                 }
//             );
//             db.UserRoles.Add(new UserRole { UserId = userId, RoleId = tempRoleId1 });
//             await db.SaveChangesAsync();


//             ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

//             // TEST SERVICE
//             var user = await service.GetUserAsync(claimsPrincipal, userId);

//             // ASSERT THE RETURN VALUE
//             Assert.NotNull(user);
//             Assert.Equal(user.Id, userId);
//             Assert.Equal(user.Email, tempUserEmail);
//             Assert.Equal(user.NameFirst, tempUserNameFirst);
//             Assert.Equal(user.NameLast, tempUserNameLast);
//             Assert.Equal(user.Censored, tempUserCensored);
//             Assert.Equal(user.Roles, roles);
//         }

//         [Fact]
//         public async Task Can_Get_Myself_As_ORGANIZATION_REVIEWER()
//         {
//             // PREPERE SERVICES
//             var options = new DbContextOptionsBuilder<AccountDbContext>()
//                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                 .Options;
//             await using var db = new AccountDbContext(options);
//             var emailMock = new Mock<IEmailService>();
//             var service = new UserService(db, emailMock.Object);

//             // PREPERE TEST DATA
//             Guid tempRoleId1 = Guid.CreateVersion7();

//             string tempUserEmail = "test@test.com";
//             string tempUserNameFirst = "First";
//             string tempUserNameLast = "Last";
//             bool tempUserCensored = false;


//             Guid userId = Guid.CreateVersion7();
//             Guid sessionId = Guid.CreateVersion7();
//             Guid token = Guid.CreateVersion7();
//             List<string> roles = ["ORGANIZATION_REVIEWER"];

//             db.Roles.AddRange(
//                 new Role { Id = tempRoleId1, Name = "ORGANIZATION_REVIEWER" }
//             );
//             db.Users.Add(
//                 new User
//                 {
//                     Id = userId,
//                     Email = tempUserEmail,
//                     NameFirst = tempUserNameFirst,
//                     NameLast = tempUserNameLast,
//                     Censored = tempUserCensored,
//                     Verified = true
//                 }
//             );
//             db.UserRoles.Add(new UserRole { UserId = userId, RoleId = tempRoleId1 });
//             await db.SaveChangesAsync();


//             ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

//             // TEST SERVICE
//             var user = await service.GetUserAsync(claimsPrincipal, userId);

//             // ASSERT THE RETURN VALUE
//             Assert.NotNull(user);
//             Assert.Equal(user.Id, userId);
//             Assert.Equal(user.Email, tempUserEmail);
//             Assert.Equal(user.NameFirst, tempUserNameFirst);
//             Assert.Equal(user.NameLast, tempUserNameLast);
//             Assert.Equal(user.Censored, tempUserCensored);
//             Assert.Equal(user.Roles, roles);
//         }
//         [Fact]
//         public async Task Can_Get_Myself_As_TEAM_ADMIN()
//         {
//             // PREPERE SERVICES
//             var options = new DbContextOptionsBuilder<AccountDbContext>()
//                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                 .Options;
//             await using var db = new AccountDbContext(options);
//             var emailMock = new Mock<IEmailService>();
//             var service = new UserService(db, emailMock.Object);

//             // PREPERE TEST DATA
//             Guid tempRoleId1 = Guid.CreateVersion7();

//             string tempUserEmail = "test@test.com";
//             string tempUserNameFirst = "First";
//             string tempUserNameLast = "Last";
//             bool tempUserCensored = false;


//             Guid userId = Guid.CreateVersion7();
//             Guid sessionId = Guid.CreateVersion7();
//             Guid token = Guid.CreateVersion7();
//             List<string> roles = ["TEAM_ADMIN"];

//             db.Roles.AddRange(
//                 new Role { Id = tempRoleId1, Name = "TEAM_ADMIN" }
//             );
//             db.Users.Add(
//                 new User
//                 {
//                     Id = userId,
//                     Email = tempUserEmail,
//                     NameFirst = tempUserNameFirst,
//                     NameLast = tempUserNameLast,
//                     Censored = tempUserCensored,
//                     Verified = true
//                 }
//             );
//             db.UserRoles.Add(new UserRole { UserId = userId, RoleId = tempRoleId1 });
//             await db.SaveChangesAsync();


//             ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

//             // TEST SERVICE
//             var user = await service.GetUserAsync(claimsPrincipal, userId);

//             // ASSERT THE RETURN VALUE
//             Assert.NotNull(user);
//             Assert.Equal(user.Id, userId);
//             Assert.Equal(user.Email, tempUserEmail);
//             Assert.Equal(user.NameFirst, tempUserNameFirst);
//             Assert.Equal(user.NameLast, tempUserNameLast);
//             Assert.Equal(user.Censored, tempUserCensored);
//             Assert.Equal(user.Roles, roles);
//         }
//         [Fact]
//         public async Task Can_Get_Myself_As_TEAM_PLAYER()
//         {
//             // PREPERE SERVICES
//             var options = new DbContextOptionsBuilder<AccountDbContext>()
//                 .UseInMemoryDatabase(Guid.NewGuid().ToString())
//                 .Options;
//             await using var db = new AccountDbContext(options);
//             var emailMock = new Mock<IEmailService>();
//             var service = new UserService(db, emailMock.Object);

//             // PREPERE TEST DATA
//             Guid tempRoleId1 = Guid.CreateVersion7();

//             string tempUserEmail = "test@test.com";
//             string tempUserNameFirst = "First";
//             string tempUserNameLast = "Last";
//             bool tempUserCensored = false;


//             Guid userId = Guid.CreateVersion7();
//             Guid sessionId = Guid.CreateVersion7();
//             Guid token = Guid.CreateVersion7();
//             List<string> roles = ["TEAM_PLAYER"];

//             db.Roles.AddRange(
//                 new Role { Id = tempRoleId1, Name = "TEAM_PLAYER" }
//             );
//             db.Users.Add(
//                 new User
//                 {
//                     Id = userId,
//                     Email = tempUserEmail,
//                     NameFirst = tempUserNameFirst,
//                     NameLast = tempUserNameLast,
//                     Censored = tempUserCensored,
//                     Verified = true
//                 }
//             );
//             db.UserRoles.Add(new UserRole { UserId = userId, RoleId = tempRoleId1 });
//             await db.SaveChangesAsync();


//             ClaimsPrincipal claimsPrincipal = await TestHelper.GenerateTestFilledClaimsPrincipalAsync(userId, sessionId, token, roles);

//             // TEST SERVICE
//             var user = await service.GetUserAsync(claimsPrincipal, userId);

//             // ASSERT THE RETURN VALUE
//             Assert.NotNull(user);
//             Assert.Equal(user.Id, userId);
//             Assert.Equal(user.Email, tempUserEmail);
//             Assert.Equal(user.NameFirst, tempUserNameFirst);
//             Assert.Equal(user.NameLast, tempUserNameLast);
//             Assert.Equal(user.Censored, tempUserCensored);
//             Assert.Equal(user.Roles, roles);
//         }
//     }
// }