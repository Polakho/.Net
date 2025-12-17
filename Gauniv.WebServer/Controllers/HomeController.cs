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

        [HttpGet]
        public async Task<IActionResult> Index(string? searchString,int[]? tagIds, double? minPrice = null, double? maxPrice = null, string? seeOwned = "true", string? notOwned = "true")
        {
            var local_query = applicationDbContext.Games.Include(g => g.Tags).AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                local_query = local_query.Where(g => g.Name.Contains(searchString));
            }
            if (tagIds != null && tagIds.Length > 0)
            {
                local_query = local_query.Where(g => g.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            if (minPrice.HasValue)
            {
                local_query = local_query.Where(g => g.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                local_query = local_query.Where(g => g.Price <= maxPrice.Value);
            } else {
                // We set the max price to the highest value in games if not specified
                maxPrice = await applicationDbContext.Games.MaxAsync(g => g.Price);
            }

            // Filter by ownership - by default both are shown (true), uncheck to exclude
            if (User?.Identity?.IsAuthenticated is true)
            {
                var user = await userManager.Users
                    .Include(u => u.OwnedGames)
                    .FirstOrDefaultAsync(u => u.UserName == User.Identity.Name);

                if (user != null)
                {
                    bool local_seeOwnedBool = seeOwned == "true";
                    bool local_notOwnedBool = notOwned == "true";
                    var ownedGameIds = user.OwnedGames.Select(g => g.Id).ToList();
                    
                    // If seeOwned is false, exclude owned games
                    // If notOwned is false, exclude not owned games
                    if (!local_seeOwnedBool && !local_notOwnedBool)
                    {
                        local_query = local_query.Where(g => false); // Show nothing
                    }
                    else if (!local_seeOwnedBool)
                    {
                        local_query = local_query.Where(g => !ownedGameIds.Contains(g.Id));
                    }
                    else if (!local_notOwnedBool)
                    {
                        local_query = local_query.Where(g => ownedGameIds.Contains(g.Id));
                    }
                    // If both are true or not specified, show all
                }
            }

            var local_games = await local_query.ToListAsync();
            var local_tags = await applicationDbContext.Tags.ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["Tags"] = local_tags;
            ViewData["SelectedTagIds"] = tagIds ?? new int[] { };
            ViewData["MinPrice"] = minPrice;
            ViewData["MaxPrice"] = maxPrice;
            ViewData["SeeOwned"] = seeOwned == "true";
            ViewData["NotOwned"] = notOwned == "true";

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
        public async Task<IActionResult> OwnedGames(string? searchString, int[]? tagIds, double? minPrice = null, double? maxPrice = null)
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

            var local_query = user.OwnedGames.AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                local_query = local_query.Where(g => g.Name.Contains(searchString));
            }

            if (tagIds != null && tagIds.Length > 0)
            {
                local_query = local_query.Where(g => g.Tags.Any(t => tagIds.Contains(t.Id)));
            }

            if (minPrice.HasValue)
            {
                local_query = local_query.Where(g => g.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                local_query = local_query.Where(g => g.Price <= maxPrice.Value);
            }
            else
            {
                maxPrice = await applicationDbContext.Games.MaxAsync(g => g.Price);
            }

            var local_ownedGames = local_query.ToList();
            var local_tags = await applicationDbContext.Tags.ToListAsync();

            ViewData["CurrentFilter"] = searchString;
            ViewData["Tags"] = local_tags;
            ViewData["SelectedTagIds"] = tagIds ?? new int[] { };
            ViewData["MinPrice"] = minPrice;
            ViewData["MaxPrice"] = maxPrice;

            return View(local_ownedGames);
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
