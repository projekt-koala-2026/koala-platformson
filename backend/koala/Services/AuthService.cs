using koala.Data;
using koala.Data.ViewModels;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Web;

namespace koala.Services
{
    //FIXME: DECIDE ON THE ENDPOINT STYLE (POST, PUT, GET OR DELETE) !!!
    //FIXME: MAKE SURE QUERIES ARE OPTIMAL AND AS SPECIFIC AS POSSIBLE !!!
    public class AuthService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AuthService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        //FIXME: TEMPORARY CREATE HERE ON START "ROOT ADMIN USER AND ROLES"
        public async Task InicializeDB()
        {
            using var context = await _factory.CreateDbContextAsync();

            // 1. Check existing roles
            var existingRoles = await context.Roles
                .Select(r => r.Value)
                .ToListAsync();

            var roles = new List<Role>();

            if (!existingRoles.Contains("ADMIN"))
                roles.Add(new Role { Value = "ADMIN" });

            if (!existingRoles.Contains("EDITOR"))
                roles.Add(new Role { Value = "EDITOR" });

            if (!existingRoles.Contains("CAPTAIN"))
                roles.Add(new Role { Value = "CAPTAIN" });

            if (!existingRoles.Contains("GUARDIAN"))
                roles.Add(new Role { Value = "GUARDIAN" });

            if (roles.Any())
            {
                context.Roles.AddRange(roles);
                await context.SaveChangesAsync();
            }

            // 2. Ensure admin user exists
            var userExists = await context.Users
                .AnyAsync(u => u.Email == "admin");

            if (!userExists)
            {
                var adminUser = new User
                {
                    Id = Guid.NewGuid(),
                    Email = "admin",
                    Password = "admin"
                };

                context.Users.Add(adminUser);
                await context.SaveChangesAsync();

                // 3. Fetch ADMIN role from DB (IMPORTANT)
                var adminRole = await context.Roles
                    .FirstAsync(r => r.Value == "ADMIN");

                context.UserRoles.Add(new UserRole
                {
                    UserId = adminUser.Id,
                    RoleId = adminRole.Id
                });

                await context.SaveChangesAsync();
            }
        }

        public async Task<(string Token, UserInfoVM userInfoVM)> AdminPanelLogin(UserLoginVM userLoginVM)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var userDB = context.Users.FirstOrDefault(u => u.Email == userLoginVM.Email && u.Password == userLoginVM.Password);
            if(userDB == null)
            {
                return ("", null);
            }

            var token = await context.Tokens.FirstOrDefaultAsync(t => t.UserId == userDB.Id);

            if (token == null)
            {
                token = new Token
                {
                    UserId = userDB.Id
                };

                context.Tokens.Add(token);
            }
            //NOTE: UPDATE TOKEN
            token.Value = Guid.NewGuid().ToString();
            token.CreatedAt = DateTime.UtcNow;
            token.ExpiresAt = DateTime.UtcNow.AddHours(1);

            await context.SaveChangesAsync();
            
            var resultRoles = await context.UserRoles
                .Where(ur => ur.UserId == userDB.Id)
                .Join(context.Roles, 
                      ur => ur.RoleId, 
                      r => r.Id, 
                      (ur, r) => r.Value)
                .ToListAsync();

            var ruser = new UserInfoVM
            {
                Id = userDB.Id,
                Email = userLoginVM.Email,
                Roles = resultRoles 
            };

            return (token.Value, ruser);
        }

        public async Task AdminPanelLogout(string tokenValue)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var token = await context.Tokens.FirstOrDefaultAsync(t => t.Value == tokenValue);

            if (token == null)
            {
                return; //invalid token
            }

            // update token
            token.Value = null;
            token.CreatedAt = null;
            token.ExpiresAt = null;

            await context.SaveChangesAsync();
            return;
        }
    }
}
