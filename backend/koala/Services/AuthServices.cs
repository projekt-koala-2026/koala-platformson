
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
    public class AuthServices
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public AuthServices(IDbContextFactory<AppDbContext> factory)
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
                    Email = "admin@admin.admin",
                    Password = "Admin**8"
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

        public async Task<UserVM> AdminPanelAddUser(UserVM user)
        {

            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY AND ROLES ADDING
            var context = await _factory.CreateDbContextAsync();
            
            var newUser = context.Users
            .FirstOrDefault(u => u.Email == user.Email);

            if (newUser != null)
            {
                return null;
            }
            
            newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = user.Email,
                Password = user.Password
            };

            var rolesFromDb = await context.Roles
                .Where(r => user.Roles.Contains(r.Value))
                .ToListAsync();

            var userRoles = rolesFromDb.Select(r => new UserRole
            {
                UserId = newUser.Id,
                RoleId = r.Id
            });

            var resultRoles = rolesFromDb.Select(r => r.Value).ToList();

            context.Users.Add(newUser);
            context.UserRoles.AddRange(userRoles);
            await context.SaveChangesAsync();

            var userVM = new UserVM 
            {   Email = newUser.Email,
                Password = newUser.Password,
                Roles = resultRoles
            };

            return userVM;
        }

        public async Task<string> AdminPanelLogin(UserVM user)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var userDB = context.Users.FirstOrDefault(u => u.Email == user.Email && u.Password == user.Password);
            if(userDB == null)
            {
                return "";
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
            return token.Value;
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

        public async Task<List<UserVM>> UserList()
        {
            using var context = await _factory.CreateDbContextAsync();

            var userVMs = await context.Users
                .Select(user => new UserVM
                {
                    Email = user.Email,
                    Password = user.Password
                })
                .ToListAsync();

            return userVMs;
        }

        public async Task<List<string>> RoleList()
        {
            using var context = await _factory.CreateDbContextAsync();

            var roles = await context.Roles
                .Select(role => role.Value)
                .ToListAsync();

            return roles;
        }
    }
}
