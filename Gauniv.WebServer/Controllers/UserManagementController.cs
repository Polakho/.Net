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
        public async Task<IActionResult> SeeUserList(int page = 1)
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

            var pagedUsers = usersDto.ToPagedList(page, 10); // 10 users per page
            return View(pagedUsers);
        }

        public async Task<IActionResult> FriendList(string query = null)
        {
            var currentUserId = userManager.GetUserId(User);
            var currentUser = await applicationDbContext.Users
                .Include(u => u.Friends)
                .Include(u => u.FriendApplications)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);

            if (currentUser == null)
            {
                return NotFound();
            }

            var friends = currentUser.Friends.Select(friend => new UsersDto
            {
                Id = friend.Id,
                UserName = friend.UserName,
                Email = friend.Email,
                IsOnline = friend.IsOnline
            }).ToList();

            var friendApplications = currentUser.FriendApplications.Select(applicant => new UsersDto
            {
                Id = applicant.Id,
                UserName = applicant.UserName,
                Email = applicant.Email
            }).ToList();

            // User search functionality
            List<UsersDto> searchResults = new();
            if (!string.IsNullOrEmpty(query))
            {
                var friendIds = currentUser.Friends.Select(f => f.Id).ToList();
                var pendingRequestIds = currentUser.FriendApplications.Select(f => f.Id).ToList();

                var users = await applicationDbContext.Users
                    .Where(u => (u.UserName.Contains(query) || u.Email.Contains(query)) 
                        && u.Id != currentUserId
                        && !friendIds.Contains(u.Id)
                        && !pendingRequestIds.Contains(u.Id))
                    .ToListAsync();

                searchResults = users.Select(user => new UsersDto
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    IsOnline = user.IsOnline
                }).ToList();
            }

            var viewModel = new FriendListViewModel
            {
                Friends = friends,
                FriendApplications = friendApplications,
                SearchResults = searchResults,
                SearchQuery = query
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> SendFriendRequest(string friendId)
        {
            var currentUserId = userManager.GetUserId(User);
            var currentUser = await applicationDbContext.Users
                .Include(u => u.Friends)
                .Include(u => u.FriendApplications)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
            var targetUser = await applicationDbContext.Users
                .Include(u => u.FriendApplications)
                .FirstOrDefaultAsync(u => u.Id == friendId);
            
            if (targetUser != null && currentUser != null && currentUser.Id != friendId)
            {
                bool areAlreadyFriends = currentUser.Friends.Any(f => f.Id == friendId);
                bool hasPendingRequest = targetUser.FriendApplications.Any(f => f.Id == currentUserId);
                
                if (!areAlreadyFriends && !hasPendingRequest)
                {
                    targetUser.FriendApplications.Add(currentUser);
                    await applicationDbContext.SaveChangesAsync();
                }
            }
            return RedirectToAction("FriendList");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest(string friendId)
        {
            var currentUserId = userManager.GetUserId(User);
            var currentUser = await applicationDbContext.Users
                .Include(u => u.Friends)
                .Include(u => u.FriendApplications)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
            var requestingUser = await applicationDbContext.Users
                .Include(u => u.Friends)
                .FirstOrDefaultAsync(u => u.Id == friendId);
            
            if (requestingUser != null && currentUser != null)
            {
                // Ajouter aux amis
                currentUser.Friends.Add(requestingUser);
                requestingUser.Friends.Add(currentUser);
                
                var applicationToRemove = currentUser.FriendApplications.FirstOrDefault(u => u.Id == friendId);
                if (applicationToRemove != null)
                {
                    currentUser.FriendApplications.Remove(applicationToRemove);
                }
                
                await applicationDbContext.SaveChangesAsync();
            }
            return RedirectToAction("FriendList");
        }

        [HttpPost]
        public async Task<IActionResult> DeclineFriendRequest(string friendId)
        {
            var requestingUser = await applicationDbContext.Users
                .FirstOrDefaultAsync(u => u.Id == friendId);
            var currentUserId = userManager.GetUserId(User);
            var currentUser = await applicationDbContext.Users
                .Include(u => u.FriendApplications)
                .FirstOrDefaultAsync(u => u.Id == currentUserId);
            
            if (requestingUser != null && currentUser != null)
            {
                currentUser.FriendApplications.Remove(requestingUser);
                await applicationDbContext.SaveChangesAsync();
            }
            return RedirectToAction("FriendList");
        }
    }
}