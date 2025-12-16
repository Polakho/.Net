
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Gauniv.GameServer
{
    public class Server
    {
       
        private TcpListener _listener;
        private bool _isRunning;
        private List<TcpClient> _clients = new List<TcpClient>();
        private int port = 5000;
        
        public Server(int port)
        {
            this.port = port;
        }
        
        public async Task StartAsync()
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            _isRunning = true;
            
            Console.WriteLine("Server started on port 5000");

            while (_isRunning)
            {
                var client = await _listener.AcceptTcpClientAsync();
                _clients.Add(client);
                Console.WriteLine("Client connected");
                
                _ = HandleClientAsync(client);
            }
        }
        
        private async Task HandleClientAsync(TcpClient client)
        {
            var stream = client.GetStream();
            var buffer = new byte[1024];

            try
            {
                while (client.Connected)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"Received: {message}");

                    // Echo back to client
                    await stream.WriteAsync(buffer, 0, bytesRead);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
            finally
            {
                _clients.Remove(client);
                client.Close();
                Console.WriteLine("Client disconnected");
            }
        }

        public void Stop()
        {
            _isRunning = false;
            _listener?.Stop();
        }
    }
}
