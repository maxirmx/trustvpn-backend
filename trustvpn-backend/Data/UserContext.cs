using TrustVpn.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace TrustVpn.Data;
public class UserContext(DbContextOptions<UserContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    public bool Exists(int id)
    {
        return Users.Any(e => e.Id == id);
    }

    public bool Exists(string email)
    {
        return Users.Any(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<List<UserViewItem>> UserViewItems()
    {
        return await Users.AsNoTracking().Select(x => new UserViewItem(x)).ToListAsync();
    }

    public async Task<UserViewItem?> UserViewItem(int id)
    {
        var user = await Users.AsNoTracking().Where(x => x.Id == id).Select(x => new UserViewItem(x)).FirstOrDefaultAsync();
        return user ?? null;
    }

    public async Task<ActionResult<bool>> CheckAdmin(int cuid)
    {
        var curUser = await UserViewItem(cuid);
        return curUser != null && curUser.IsAdmin;
    }

    public async Task<ActionResult<bool>> CheckAdminOrSameUser(int id, int cuid)
    {
        if (cuid == 0) return false;
        if (cuid == id) return true;
        return await CheckAdmin(cuid);
    }
    public bool CheckSameUser(int id, int cuid)
    {
        if (cuid == 0) return false;
        if (cuid == id) return true;
        return false;
    }

}
