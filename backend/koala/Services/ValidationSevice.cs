
using koala.Data;
using koala.Data.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace koala.Services
{
    public class ValidationService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public ValidationService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public bool USERVM_IsEmailValid(UserVM user)
        {
            if(string.IsNullOrEmpty(user.Email))
            {
                return false;
            }
            if(!Regex.IsMatch(user.Email, @"^\S+@\S+\.\S+$"))
            {
                return false;
            }
            return true;
        }

        public bool USERVM_IsPasswordValid(UserVM user)
        {
            if(string.IsNullOrEmpty(user.Password))
            {
                return false;
            }
            if(!Regex.IsMatch(user.Password, @"^(?=.*[A-Z])(?=.*\d).{8,}$"))
            {
                return false;
            }

            return true;
        }

        public async Task<bool> USERVM_IsRolesValidAsync(UserVM user)
        {
            using var context = await _factory.CreateDbContextAsync();

            var existingRoles = await context.Roles
                .Select(r => r.Value)
                .ToListAsync();

            var existingSet = existingRoles.ToHashSet();

            return user.Roles.All(r => existingSet.Contains(r));
        }

        public bool USERVM_IsAnyFieldEmpty(UserVM user)
        {
            if(string.IsNullOrEmpty(user.Email))
            {
                return false;
            }
            if(string.IsNullOrEmpty(user.Password))
            {
                return false;
            }
            if(user.Roles == null)
            {
                return false;
            }
            return true;
        }

    }
}