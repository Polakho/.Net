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
    public class GameManagementController(ILogger<GameManagementController> logger, ApplicationDbContext applicationDbContext, UserManager<User> userManager) : Controller
    {
        private readonly ILogger<GameManagementController> _logger = logger;
        private readonly ApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly UserManager<User> userManager = userManager;


        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateGamePage(int[]? tagIds)
        {
            var local_tags = await applicationDbContext.Tags.ToListAsync();

            ViewData["Tags"] = local_tags;

            var viewModel = new CreateGameViewModel
            {
                AvailableTags = local_tags
            };
            return View(viewModel);
        }

        [HttpGet]
        [Authorize(Roles= "Admin")]
        public async Task<IActionResult> DeleteGamePage(int[]? tagIds)
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditGameListPage(int[]? tagIds)
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditGamePage(int id)
        {
            var game = await applicationDbContext.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == id);
            if (game == null) { return NotFound();}

            var local_viewModel = new CreateGameViewModel
            {
                Id = game.Id,
                Name = game.Name,
                Description = game.Description,
                Price = game.Price,
                ImagePath = game.ImagePath,
                AvailableTags = await applicationDbContext.Tags.ToListAsync(),
                SelectedTagIds = game.Tags.Select(t => t.Id).ToList()
            };
            return View(local_viewModel);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTagPage()
        {
            return View();
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTagPage()
        {
            var local_tags = await applicationDbContext.Tags.ToListAsync();
            return View(local_tags);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditTagListPage()
        {
            var local_tags = await applicationDbContext.Tags.ToListAsync();
            return View(local_tags);
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditTagPage(int id)
        {
            var tag = await applicationDbContext.Tags.FirstOrDefaultAsync(t => t.Id == id);
            if (tag == null) { return NotFound();}

            var local_viewModel = new TagViewModel
            {
                Id = tag.Id,
                Name = tag.Name
            };
            return View(local_viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateGame(CreateGameViewModel model, int[] selectedTagIds)
        {
            var local_game = new Game
            {
                Name = model.Name,
                Description = model.Description,
                Price = model.Price,
                ImagePath = model.ImagePath,
                Payload = Convert.ToBase64String(Encoding.UTF8.GetBytes("Placeholder payload"))
            };

            var local_selectedTags = await applicationDbContext.Tags
                .Where(t => selectedTagIds.Contains(t.Id))
                .ToListAsync();

            local_game.Tags = local_selectedTags;

            applicationDbContext.Games.Add(local_game);
            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditGame(CreateGameViewModel model, int[] selectedTagIds)
        {
            var local_game = await applicationDbContext.Games.Include(g => g.Tags).FirstOrDefaultAsync(g => g.Id == model.Id);
            if (local_game == null)
            {
                return NotFound();
            }

            local_game.Name = model.Name;
            local_game.Description = model.Description;
            local_game.Price = model.Price;
            local_game.ImagePath = model.ImagePath;

            var local_selectedTags = await applicationDbContext.Tags
                .Where(t => selectedTagIds.Contains(t.Id))
                .ToListAsync();

            local_game.Tags = local_selectedTags;

            applicationDbContext.Games.Update(local_game);
            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteGame(int id)
        {
            var local_game = await applicationDbContext.Games.FindAsync(id);
            if (local_game == null)
            {
                return NotFound();
            }

            applicationDbContext.Games.Remove(local_game);
            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateTag(TagListModel model)
        {
            string[] tagNames = model.List.Split(';');
            foreach (var tagName in tagNames)
            {
                var newTag = new Tags{ Name = tagName.Trim() };
                applicationDbContext.Tags.Add(newTag);
            }

            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteTag(int[] SelectedTagIds)
        {
            
            foreach (var id in SelectedTagIds)
            {
                var local_tag = await applicationDbContext.Tags.FindAsync(id);
                if (local_tag != null)
                {
                    applicationDbContext.Tags.Remove(local_tag);
                }
            }
            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditTag(TagViewModel model)
        {
            var local_tag = await applicationDbContext.Tags.FirstOrDefaultAsync(t => t.Id == model.Id);
            if (local_tag == null)
            {
                return NotFound();
            }

            local_tag.Name = model.Name;

            applicationDbContext.Tags.Update(local_tag);
            await applicationDbContext.SaveChangesAsync();
            return RedirectToAction("Index", "Home");
        }

    }
}