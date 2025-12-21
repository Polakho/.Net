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
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Websocket;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Pages;
using System.Text;

namespace Gauniv.WebServer.Services
{
    public class SetupService : IHostedService
    {
        private ApplicationDbContext? applicationDbContext;
        private readonly IServiceProvider serviceProvider;
        private Task? task;
        private RoleManager<IdentityRole>? roleManager;

        public SetupService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = serviceProvider.CreateScope()) // this will use `IServiceScopeFactory` internally
            {
                applicationDbContext = scope.ServiceProvider.GetService<ApplicationDbContext>();
                var userSignInManager = scope.ServiceProvider.GetService<UserManager<User>>();
                var signInManager = scope.ServiceProvider.GetService<SignInManager<User>>();
                roleManager = scope.ServiceProvider.GetService<RoleManager<IdentityRole>>();

                roleManager?.CreateAsync(new IdentityRole("Admin")).Wait();
                roleManager?.CreateAsync(new IdentityRole("User")).Wait();

                if (roleManager is null)
                {
                    throw new Exception("RoleManager is null");
                }

                var games = applicationDbContext?.Games;

                if (applicationDbContext is null)
                {
                    throw new Exception("ApplicationDbContext is null");
                }

                User local_admin = new User()
                {
                    UserName = "admin@test.com",
                    Email = "admin@test.com",
                    EmailConfirmed = true,
                };

                var adminResult = userSignInManager?.CreateAsync(local_admin, "adminpassword").Result;
                if (adminResult != null && adminResult.Succeeded)
                {
                    userSignInManager?.AddToRoleAsync(local_admin, "Admin").Wait();
                }

                // Create users
                List<User> local_users = new List<User>();
                for (int i = 0; i < 10; i++)
                {
                    var local_user = new User()
                    {
                        UserName = $"test{i}@test.com",
                        Email = $"test{i}@test.com",
                        EmailConfirmed = true,
                    };
                    var r = userSignInManager?.CreateAsync(local_user, "password").Result;
                    local_users.Add(local_user);
                }

                var tagList = new List<Tags>()
                {
                    new Tags() { Name = "Action"},
                    new Tags() { Name = "Aventure"},
                    new Tags() { Name = "RPG"},
                    new Tags() { Name = "Rythme"}
                };

                applicationDbContext.Tags.AddRange(tagList);
                applicationDbContext.SaveChanges();

                var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "BinaryFilesGames");
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                }

                // Ensure the base executable exists for all seeded games
                var zoomItFileName = "ZoomIt.exe";
                var zoomItFilePath = Path.Combine(uploadsPath, zoomItFileName);

                if (!File.Exists(zoomItFilePath) || new FileInfo(zoomItFilePath).Length == 0)
                {
                    var hostEnv = scope.ServiceProvider.GetService<IHostEnvironment>();
                    string? sourceCandidate = hostEnv != null
                        ? Path.Combine(hostEnv.ContentRootPath, "BinaryFilesGames", zoomItFileName)
                        : null;

                    if (sourceCandidate != null && File.Exists(sourceCandidate))
                    {
                        File.Copy(sourceCandidate, zoomItFilePath, overwrite: true);
                    }
                    else
                    {
                        throw new FileNotFoundException($"{zoomItFileName} introuvable. Placez le fichier dans '{uploadsPath}' ou dans le dossier de contenu '{hostEnv?.ContentRootPath}\\BinaryFilesGames'.");
                    }
                }

                // Create games using the same base executable
                List<Game> local_games = new List<Game>();
                for (int i = 0; i < 10; i++)
                {
                    var local_game = new Game()
                    {
                        Name = $"Game{i}",
                        Description = $"This is the description of game {i}",
                        Price = i * 10.0,
                        BinaryFilePath = zoomItFilePath,
                        ImagePath = "/images/Goomba.png",
                        Tags = new List<Tags>()
                        {
                            tagList[i % tagList.Count]
                        }
                    };
                    applicationDbContext.Games.Add(local_game);
                    local_games.Add(local_game);
                }

                // Assign games to first user
                if (local_users.Count > 0 && local_games.Count > 0)
                {
                    foreach (var local_game in local_games)
                    {
                        local_users[0].OwnedGames.Add(local_game);
                    }
                }

                var notOwnedGame = new Game()
                {
                    Name = $"Jeu RPG Aventure",
                    Description = $"Un jeu d'aventure et de rôle passionnant.",
                    BinaryFilePath = zoomItFilePath,
                    Price = 29.99,
                    ImagePath = "/images/Goomba.png",
                    Tags = new List<Tags>()
                    {
                        tagList.First(t => t.Name == "Aventure"),
                        tagList.First(t => t.Name == "RPG")
                    }
                };
                applicationDbContext.Games.Add(notOwnedGame);
                applicationDbContext.SaveChanges();

                return;
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
