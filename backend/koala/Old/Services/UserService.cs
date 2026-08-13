
using koala.Data;
using koala.Data.ViewModels;
using koala.Utils;
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

        //TODO: MAKE SURE ADMIN / EDITOR ROLES CANOT BE ADDED THROUGH IT
        public async Task<UserInfoVM> CreateNormalUser(UserCreateNormalVM userCreateVM)
        {
            var context = await _factory.CreateDbContextAsync();
            
            var newUser = context.Users
            .FirstOrDefault(u => u.Email == userCreateVM.Email);

            if (newUser != null)
            {
                return null;
            }
            
            newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = userCreateVM.Email,
                Password = PasswordHelper.ComputeSha256Hash(userCreateVM.Password)
            };

            var rolesFromDb = await context.Roles
                .Where(r => userCreateVM.Roles.Contains(r.Value))
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

            var userInfoVM = new UserInfoVM 
            {   
                Id = newUser.Id,
                Email = newUser.Email,
                Roles = resultRoles
            };

            return userInfoVM;
        }

        public async Task<UserInfoVM> AdminPanelAddUser(UserCreateVM userCreateVM)
        {

            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();
            
            var newUser = context.Users
            .FirstOrDefault(u => u.Email == userCreateVM.Email);

            if (newUser != null)
            {
                return null;
            }
            
            newUser = new User
            {
                Id = Guid.NewGuid(),
                Email = userCreateVM.Email,
                Password = PasswordHelper.ComputeSha256Hash(userCreateVM.Password)
            };

            var rolesFromDb = await context.Roles
                .Where(r => userCreateVM.Roles.Contains(r.Value))
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

            var userInfoVM = new UserInfoVM 
            {   
                Id = newUser.Id,
                Email = newUser.Email,
                Roles = resultRoles
            };

            return userInfoVM;
        }

        public async Task<UserInfoVM> AdminPanelChangeUserEmail(UserChangeEmailVM userChangeEmailVM)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();
            
            var user = context.Users
            .FirstOrDefault(u => u.Id == userChangeEmailVM.Id);

            if (user == null)
            {
                return null;
            }

            string enteredHash = PasswordHelper.ComputeSha256Hash(userChangeEmailVM.Password);
            string storedHash = user.Password;

            bool valid = enteredHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);

            if (!valid)
            {
                return null;
            }
            
            user.Email = userChangeEmailVM.NewEmail;

            await context.SaveChangesAsync();

            var userInfoVM = await context.Users
                .Where(u => u.Id == userChangeEmailVM.Id)
                .Select(user => new UserInfoVM
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles, 
                          ur => ur.RoleId, 
                          r => r.Id, 
                          (ur, r) => r.Value)
                    .ToList()
                })
                .FirstOrDefaultAsync();

            return userInfoVM;
        }

        public async Task<UserInfoVM> AdminPanelChangeUserPassword(UserChangePasswordVM userChangePasswordVM)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();
            
            var user = context.Users
            .FirstOrDefault(u => u.Id == userChangePasswordVM.Id);

            if (user == null)
            {
                return null;
            }

            string enteredHash = PasswordHelper.ComputeSha256Hash(userChangePasswordVM.Password);
            string storedHash = user.Password;

            bool valid = enteredHash.Equals(storedHash, StringComparison.OrdinalIgnoreCase);

            if (!valid)
            {
                return null;
            }

            user.Password = PasswordHelper.ComputeSha256Hash(userChangePasswordVM.NewPassword);

            await context.SaveChangesAsync();

            var userInfoVM = await context.Users
                .Where(u => u.Id == userChangePasswordVM.Id)
                .Select(user => new UserInfoVM
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles, 
                          ur => ur.RoleId, 
                          r => r.Id, 
                          (ur, r) => r.Value)
                    .ToList()
                })
                .FirstOrDefaultAsync();

            return userInfoVM;
        }

        public async Task AdminPanelDeleteUser(UserDeleteVM userDeleteVM)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userDeleteVM.Id);

            if (user == null)
            {
                return;
            }

            var affectedRowsUserRoles = await context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .ExecuteDeleteAsync();

            var affectedRowsUser = await context.Users
                .Where(u => u.Id == user.Id)
                .ExecuteDeleteAsync();

            //TODO: CHECK WHAT HAPPEND WITH DELETION AND MAKE CORECT RETURN VALUES

            return;
        }

        public async Task<UserInfoVM> AdminPanelChangeUserRoles(UserChangeRolesVM userChangeRolesVM)
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            var context = await _factory.CreateDbContextAsync();

            var userDB = context.Users
            .FirstOrDefault(u => u.Id == userChangeRolesVM.Id);

            if (userDB == null)
            {
                return null;
            }

            var rolesFromDb = await context.Roles
                .Where(r => userChangeRolesVM.NewRoles.Contains(r.Value))
                .ToListAsync();

            var userRoles = rolesFromDb.Select(r => new UserRole
            {
                UserId = userDB.Id,
                RoleId = r.Id
            });

            var resultRoles = rolesFromDb.Select(r => r.Value).ToList();

            await context.UserRoles
                .Where(ur => ur.UserId == userDB.Id)
                .ExecuteDeleteAsync();

            context.UserRoles.AddRange(userRoles);
            await context.SaveChangesAsync();

            var userInfoVM = new UserInfoVM 
            {   
                Id = userDB.Id,
                Email = userDB.Email,
                Roles = resultRoles
            };

            return userInfoVM;
        }

        public async Task<List<UserInfoVM>> AdminPanelGetUsersInfo()
        {
            //NOTE: ASUME ALL DATA IS VALID HERE
            //TODO: ADD RETURN VALUES CORECTLY
            using var context = await _factory.CreateDbContextAsync();

            var excludedRoles = new[] { "CAPTAIN", "GUARDIAN" };

            //FIXME: OPTIMIZE THIS ATROCITY
            var userInfoVMs = await context.Users
                .Where(user => !context.UserRoles
                    .Where(ur => ur.UserId == user.Id)
                    .Join(context.Roles,
                          ur => ur.RoleId,
                          r => r.Id,
                          (ur, r) => r.Value)
                    .Any(role => excludedRoles.Contains(role)))
                .Select(user => new UserInfoVM
                {
                    Id = user.Id,
                    Email = user.Email,
                    Roles = context.UserRoles
                        .Where(ur => ur.UserId == user.Id)
                        .Join(context.Roles,
                              ur => ur.RoleId,
                              r => r.Id,
                              (ur, r) => r.Value)
                        .ToList()
                })
                .ToListAsync();

            return userInfoVMs;
        }
    }
}