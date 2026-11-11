using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CaldovaAssetManager.Models;

namespace CaldovaAssetManager.Security;

public class LegacyUserStore(CaldovaAssetManagerContext context, IdentityErrorDescriber errors)
    : UserStore<ApplicationUser, IdentityRole, CaldovaAssetManagerContext>(context, errors)
{
    public override Task<ApplicationUser> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken = default) =>
        Users.SingleOrDefaultAsync(u => u.UserName.ToUpper() == normalizedUserName, cancellationToken);

    public override Task<ApplicationUser> FindByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default) =>
        Users.SingleOrDefaultAsync(u => u.Email.ToUpper() == normalizedEmail, cancellationToken);

    protected override Task<IdentityRole> FindRoleAsync(string normalizedRoleName, CancellationToken cancellationToken) =>
        Context.Roles.SingleOrDefaultAsync(r => r.Name.ToUpper() == normalizedRoleName, cancellationToken);

    public override Task<IList<ApplicationUser>> GetUsersInRoleAsync(string normalizedRoleName, CancellationToken cancellationToken = default) =>
        FindUsersInRoleAsync(normalizedRoleName, cancellationToken);

    private async Task<IList<ApplicationUser>> FindUsersInRoleAsync(string name, CancellationToken cancellationToken) =>
        await (from user in Users
               join userRole in Context.UserRoles on user.Id equals userRole.UserId
               join role in Context.Roles on userRole.RoleId equals role.Id
               where role.Name.ToUpper() == name
               select user).ToListAsync(cancellationToken);
}

public class LegacyRoleStore(CaldovaAssetManagerContext context) : IQueryableRoleStore<IdentityRole>
{
    public IQueryable<IdentityRole> Roles => context.Roles;

    public Task<IdentityRole> FindByNameAsync(string normalizedName, CancellationToken cancellationToken = default) =>
        Roles.SingleOrDefaultAsync(r => r.Name.ToUpper() == normalizedName, cancellationToken);

    public Task<IdentityRole> FindByIdAsync(string roleId, CancellationToken cancellationToken) =>
        Roles.SingleOrDefaultAsync(r => r.Id == roleId, cancellationToken);

    public Task<string> GetRoleIdAsync(IdentityRole role, CancellationToken cancellationToken) => Task.FromResult(role.Id);
    public Task<string> GetRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) => Task.FromResult(role.Name);
    public Task<string> GetNormalizedRoleNameAsync(IdentityRole role, CancellationToken cancellationToken) =>
        Task.FromResult(role.Name?.ToUpperInvariant());

    public Task SetRoleNameAsync(IdentityRole role, string roleName, CancellationToken cancellationToken)
    {
        role.Name = roleName;
        return Task.CompletedTask;
    }

    public Task SetNormalizedRoleNameAsync(IdentityRole role, string normalizedName, CancellationToken cancellationToken)
    {
        role.NormalizedName = normalizedName;
        return Task.CompletedTask;
    }

    public async Task<IdentityResult> CreateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> UpdateAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        context.Roles.Update(role);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public async Task<IdentityResult> DeleteAsync(IdentityRole role, CancellationToken cancellationToken)
    {
        context.Roles.Remove(role);
        await context.SaveChangesAsync(cancellationToken);
        return IdentityResult.Success;
    }

    public void Dispose() { }
}
