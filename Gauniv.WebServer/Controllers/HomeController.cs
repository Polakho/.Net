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
using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Models;
using Gauniv.WebServer.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using X.PagedList.Extensions;

namespace Gauniv.WebServer.Controllers
{
    public class HomeController(ILogger<HomeController> logger, ApplicationDbContext applicationDbContext, UserManager<User> userManager) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;
        private readonly ApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly UserManager<User> userManager = userManager;

        public async Task<IActionResult> Index(int[]? tagIds)
        {
            var local_query = applicationDbContext.Games.Include(g => g.Tags).AsQueryable();

            if (tagIds != null && tagIds.Length > 0)
            {
                local_query = local_query.Where(g => g.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            var local_games = await local_query.ToListAsync();
            var local_tags = await applicationDbContext.Tags.ToListAsync();

            ViewData["Tags"] = local_tags;
            ViewData["SelectedTagIds"] = tagIds ?? new int[] { };

            return View(local_games);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {

            var details = await applicationDbContext.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id);
            if (details == null) { return NotFound(); }

            bool local_userOwnsGame = false;
            if (User?.Identity?.IsAuthenticated is not null or false)
            {
                var user = await userManager.Users
                    .Include(u => u.OwnedGames)
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                if (user != null)
                {
                    local_userOwnsGame = user.OwnedGames.Any(g => g.Id == id);
                }
            }
            ViewData["userOwnsGame"] = local_userOwnsGame;
            return View(details);
        }

        [HttpGet]
        public async Task<IActionResult> OwnedGames()
        {
            if (User?.Identity?.IsAuthenticated is null or false)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await userManager.Users
                .Include(u => u.OwnedGames)
                .ThenInclude(g => g.Tags)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null)
            {
                return NotFound();
            }

            var ownedGames = user.OwnedGames;

            return View(ownedGames);
        }

        [HttpPost]
        public async Task<IActionResult> Purchase(int id)
        {
            if (User?.Identity?.IsAuthenticated is null or false)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await userManager.Users
                .Include(u => u.OwnedGames)
                .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

            if (user == null)
            {
                return NotFound();
            }

            var gameToPurchase = await applicationDbContext.Games.FindAsync(id);
            if (gameToPurchase == null)
            {
                return NotFound();
            }

            if (!user.OwnedGames.Any(g => g.Id == id))
            {
                user.OwnedGames.Add(gameToPurchase);
                await userManager.UpdateAsync(user);
            }

            return RedirectToAction("Details", new { id = id });
        }
    }
}
