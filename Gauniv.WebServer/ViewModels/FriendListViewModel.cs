using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gauniv.WebServer.Dtos;

namespace Gauniv.WebServer.ViewModels
{
    public class FriendListViewModel
    {
        public List<UsersDto> Friends { get; set; } = new List<UsersDto>();
        public List<UsersDto> FriendApplications { get; set; } = new List<UsersDto>();
        public List<UsersDto> SearchResults { get; set; } = new List<UsersDto>();
        public string SearchQuery { get; set; }
    }
}