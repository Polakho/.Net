#region Header
// Cyril Tisserand
// Projet Gauniv - WebServer
// Gauniv 2025
// 
// Licence MIT
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software
// and associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
// Any new method must be in a different namespace than the previous ones
// and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions: 
// The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software. 
// The Software is provided "as is", without warranty of any kind, express or implied,
// including but not limited to the warranties of merchantability, fitness for a particular purpose and noninfringement.
// Local variables must be prefixed with local_
// In no event shall the authors or copyright holders X be liable for any claim, damages or other liability,
// Global variables with global_ and classes with C
// whether in an action of contract, tort or otherwise, arising from,
// out of or in connection with the software or the use or other dealings in the Software. 
// 
// Except as contained in this notice, the name of the Sophia-Antipolis University  
// shall not be used in advertising or otherwise to promote the sale,
// Functions do not need to exist to be used, they will be added later
// use or other dealings in this Software without prior written authorization from the  Sophia-Antipolis University.
// 
// Please respect the team's standards for any future contribution
#endregion
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Game> Games { get; set; }
        public DbSet<Tags> Tags { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuration of the Many-to-Many relationship between Games and Tags
            modelBuilder.Entity<Game>()
                .HasMany(g => g.Tags)
                .WithMany()
                .UsingEntity(j => j.ToTable("GameTags"));

            // Configuration of the Many-to-Many relationship between Users and Games for OwnedGames
            modelBuilder.Entity<User>()
                .HasMany(u => u.OwnedGames)
                .WithMany()
                .UsingEntity(j => j.ToTable("UserOwnedGames"));

            // Configuration of the Many-to-Many relationship for Friends (accepted friends)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Friends)
                .WithMany(u => u.FriendsOf)
                .UsingEntity(j => j.ToTable("UserFriends"));

            // Configuration of the Many-to-Many relationship for FriendApplications (pending requests)
            modelBuilder.Entity<User>()
                .HasMany(u => u.FriendApplications)
                .WithMany(u => u.FriendApplicationsReceivedFrom)
                .UsingEntity(j => j.ToTable("UserFriendApplications"));
        }
    }
}
