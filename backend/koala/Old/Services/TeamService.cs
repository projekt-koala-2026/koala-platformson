using koala.Data;
using koala.Data.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace koala.Services
{
    public class TeamService
    {
        private readonly IDbContextFactory<AppDbContext> _factory;

        public TeamService(IDbContextFactory<AppDbContext> factory)
        {
            _factory = factory;
        }

        public async Task<IEnumerable<TeamUpdateVM>> GetAllTeamsAsync()
        {
            using var context = await _factory.CreateDbContextAsync();
            
            var teams = await context.Teams.ToListAsync();
            
            return teams.Select(MapToViewModel);
        }

        public async Task<TeamUpdateVM?> GetTeamByIdAsync(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            var team = await context.Teams.FindAsync(id);
            return team == null ? null : MapToViewModel(team);
        }

        public async Task<TeamUpdateVM?> GetTeamByCaptainIdAsync(Guid captainId)
        {
            using var context = await _factory.CreateDbContextAsync();
            var team = await context.Teams.FirstOrDefaultAsync(t => t.CaptainId == captainId);
            return team == null ? null : MapToViewModel(team);
        }

        public async Task<TeamUpdateVM> CreateTeamAsync(string userToken,TeamCreateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var session = await context.Tokens
            .FirstOrDefaultAsync(t => t.Value == userToken);
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
            {
                return null;
            }
            Guid cId = user.Id;

            var team = await context.Teams.FirstOrDefaultAsync(t => t.CaptainId == cId);
            
            if(team != null)
            {
                return null;
            }
            
            team = new Team
            {
                Id = Guid.NewGuid(),
                CaptainId = cId,
                SchoolRSPO = model.SchoolRSPO,
                TeamName = model.TeamName,
                Name1 = model.Name1,
                Name2 = model.Name2,
                Name3 = model.Name3,
                Name4 = model.Name4
            };

            context.Teams.Add(team);
            await context.SaveChangesAsync();

            return MapToViewModel(team);
        }

        public async Task<bool> UpdateTeamAsync(Guid id, TeamUpdateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var team = await context.Teams.FindAsync(id);
            
            if (team == null) return false;

            team.TeamName = model.TeamName;
            team.Name1 = model.Name1;
            team.Name2 = model.Name2;
            team.Name3 = model.Name3;
            team.Name4 = model.Name4;

            context.Entry(team).State = EntityState.Modified;

            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateConcurrencyException)
            {
                return false;
            }
        }

        public async Task<bool> DeleteTeamAsync(Guid id)
        {
            using var context = await _factory.CreateDbContextAsync();
            var team = await context.Teams.FindAsync(id);
            
            if (team == null) return false;

            context.Teams.Remove(team);
            await context.SaveChangesAsync();
            return true;
        }

        public async Task<TeamUpdateVM> UpdateCaptainTeamAsync(string userToken, TeamUpdateVM model)
        {
            using var context = await _factory.CreateDbContextAsync();
            var session = await context.Tokens
            .FirstOrDefaultAsync(t => t.Value == userToken);
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
            {
                return null;
            }
            Guid cId = user.Id;
            var team = await context.Teams.FirstOrDefaultAsync(t => t.CaptainId == cId);
            
            if(team == null)
            {
                return null;
            }

            team.SchoolRSPO = model.SchoolRSPO;
            team.TeamName = model.TeamName;
            team.Name1 = model.Name1;
            team.Name2 = model.Name2;
            team.Name3 = model.Name3;
            team.Name4 = model.Name4;

            await context.SaveChangesAsync();
            return MapToViewModel(team);
        }

        public async Task<bool> DeleteCaptainTeamAsync(string userToken)
        {
            using var context = await _factory.CreateDbContextAsync();
            var session = await context.Tokens
            .FirstOrDefaultAsync(t => t.Value == userToken);
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
            {
                return false;
            }
            Guid cId = user.Id;
            
            if (cId == Guid.Empty)
            {
                return false;
            }

            var team = await context.Teams.FirstOrDefaultAsync(t => t.CaptainId == cId);

            if(team == null)
            {
                return false;
            }

            context.Teams.Remove(team);
            await context.SaveChangesAsync();
            return true;
        }
        
        private async Task<Guid> GetCaptainIdByToken(string userToken)
        {
            using var context = await _factory.CreateDbContextAsync();
            var session = await context.Tokens
            .FirstOrDefaultAsync(t => t.Value == userToken);
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == session.UserId);

            if (user == null)
            {
                return Guid.Empty;
            }
            return user.Id;
        }

        private static TeamUpdateVM MapToViewModel(Team team)
        {
            return new TeamUpdateVM
            {
                Id = team.Id,
                TeamName = team.TeamName,
                SchoolRSPO = team.SchoolRSPO,
                Name1 = team.Name1,
                Name2 = team.Name2,
                Name3 = team.Name3,
                Name4 = team.Name4
            };
        }
    }
}