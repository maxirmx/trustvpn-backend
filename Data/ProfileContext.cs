using o_service_api.Models;
using Microsoft.EntityFrameworkCore;

namespace o_service_api.Data;
public class ProfileContext : DbContext
{
    public ProfileContext(DbContextOptions<ProfileContext> options) : base(options)
    {
    }

    public DbSet<Profile> Profiles { get; set; }

    public bool Exists(int id)
    {
        return Profiles.Any(e => e.Id == id);
    }

}
