using LibraryHub.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace LibraryHub.Extensions
{
    public static class SoftDeleteExtensions
    {
        public static void ApplySoftDelete(this ChangeTracker changeTracker)
        {
            foreach (var entry in changeTracker.Entries<ISoftDelete>())
            {
                if (entry.State == EntityState.Deleted)
                {
                    entry.State = EntityState.Modified;
                    entry.Entity.IsDeleted = true;
                    entry.Entity.DeletedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
