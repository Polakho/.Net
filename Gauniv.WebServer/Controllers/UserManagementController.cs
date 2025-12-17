using System.ComponentModel.Design;
using System.Data;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using CommunityToolkit.HighPerformance;
using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Gauniv.WebServer.Models;
using Gauniv.WebServer.ViewModels;
using Gauniv.WebServer.Websocket;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging;
using X.PagedList.Extensions;   

namespace Gauniv.WebServer.Controllers
{
    public class UserManagementController(ILogger<UserManagementController> logger, ApplicationDbContext applicationDbContext, UserManager<User> userManager) : Controller
    {
        private readonly ILogger<UserManagementController> logger = logger;
        private readonly ApplicationDbContext applicationDbContext = applicationDbContext;
        private readonly UserManager<User> userManager = userManager;

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> SeeUserList()
        {
            var users = await userManager.Users.ToListAsync();
            var usersDto = users.Select(user => new UsersDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                IsOnline = user.IsOnline,
                OwnedGames = user.OwnedGames.Select(game => new GameDto
                {
                    Id = game.Id,
                    Name = game.Name,
                    Description = game.Description
                }).ToList()
            }).ToList();
            return View(usersDto);
        }
    }
}