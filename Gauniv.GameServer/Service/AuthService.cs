using System;
using System.Collections.Concurrent;

namespace Gauniv.GameServer.Service
{
    public class AuthService
    {
        private readonly ConcurrentDictionary<string, string> _users = new();

        public AuthService()
        {
            // demo user
            _users["user"] = "user";
        }

        public string Authenticate(string login, string password)
        {
            if (_users.TryGetValue(login, out var pw) && pw == password)
            {
                // issue simple token (in production, use JWT or similar)
                return Guid.NewGuid().ToString("N");
            }

            return null;
        }
    }
}