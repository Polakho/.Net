using Gauniv.WebServer.Data;
using Gauniv.WebServer.Dtos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using System.Text;
using CommunityToolkit.HighPerformance.Memory;
using CommunityToolkit.HighPerformance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;
using MapsterMapper;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace Gauniv.WebServer.Api
{
    [Route("api/1.0.0/[controller]/[action]")]
    [ApiController]
    public class AuthController(ApplicationDbContext appDbContext, IMapper mapper, UserManager<User> userManager, MappingProfile mp) : ControllerBase
    {
        private readonly ApplicationDbContext appDbContext = appDbContext;
        private readonly IMapper mapper = mapper;
        private readonly UserManager<User> userManager = userManager;
        private readonly MappingProfile mp = mp;

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> CreateAccount([FromBody] RegisterRequestDto request)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = new User
            {
                Email = request.Email, 
                UserName = request.Email
            };

            var result = await userManager.CreateAsync(user, request.Password);

            if (!result.Succeeded){return BadRequest(result.Errors);}

            return Ok(new { Message = "User created successfully", userId = user.Id });

        }
    }
}