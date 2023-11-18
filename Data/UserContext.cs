using o_service_api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace o_service_api.Data;
public class UserContext : DbContext
{
    public UserContext(DbContextOptions<UserContext> options) : base(options) { }

    public DbSet<User> Users { get; set; }

    public bool Exists(int id)
    {
        return Users.Any(e => e.Id == id);
    }

    public bool Exists(string email)
    {
        return Users.Any(e => e.Email == email);
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
