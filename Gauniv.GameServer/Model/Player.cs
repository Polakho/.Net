using System;
using System.Net.Sockets;
using System.IO;

namespace Gauniv.GameServer.Model
{
    public class Player
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string Token { get; set; } = "";
        public TcpClient Tcp { get; set; }
        public Stream Stream { get; set; }
        public StoneColor Color { get; set; }
        
        public Player(TcpClient tcp, string name)
        {
            Id = Guid.NewGuid();
            Name = name;
            Tcp = tcp;
            Stream = tcp.GetStream();
        }
    }
}
