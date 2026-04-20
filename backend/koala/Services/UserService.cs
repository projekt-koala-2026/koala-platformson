
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
    public class UserService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public UserService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<UserVM> AdminPanelAddUser(UserVM user)
        {

            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
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
                Password = newUser.Password, //FIXME: decide if remove password here?
                Roles = resultRoles
            };

            return userVM;
        }
        public async Task<UserVM> AdminPanelChangeUser(UserVM newUserData, Guid userId)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();
            
            var user = context.Users
            .FirstOrDefault(u => u.Id == userId);

            if (user == null)
            {
                return null;
            }

            if(!string.IsNullOrEmpty(newUserData.Email))
            {
                user.Email = newUserData.Email;
            }

            if(!string.IsNullOrEmpty(newUserData.Password))
            {
                user.Password = newUserData.Password;
            }

            context.SaveChangesAsync();

            var userVM = await context.Users
                .Where(u => u.Id == userId)
                .Select(user => new UserVM
                {
                    Email = user.Email,
                    Password = user.Password, //TODO: DECIDE IF TO RETURN THE NEW PASSWORD
                    Roles = context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles, 
                          ur => ur.RoleId, 
                          r => r.Id, 
                          (ur, r) => r.Value)
                    .ToList()
                })
                .FirstOrDefaultAsync();

            return userVM;
        }

        public async Task<List<UserVM>> AdminPanelGetUsersInfo()
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            using var context = await _factory.CreateDbContextAsync();

            var userVMs = await context.Users
                .Select(user => new UserVM
                {
                    Email = user.Email,
                    Password = null,
                    Roles = context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles, 
                          ur => ur.RoleId, 
                          r => r.Id, 
                          (ur, r) => r.Value)
                    .ToList()
                })
                .ToListAsync();

            return userVMs;
        }
    }
}