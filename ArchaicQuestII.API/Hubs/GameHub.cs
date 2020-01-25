
using Microsoft.AspNetCore.SignalR;
using System;
using System.IO;
using System.Threading.Tasks;
using ArchaicQuestII.DataAccess;
using ArchaicQuestII.GameLogic.Character;
using Microsoft.Extensions.Logging;

namespace ArchaicQuestII.Hubs
{
    public class GameHub : Hub
    {
        private readonly ILogger<GameHub> _logger;
        private IDataBase _db { get; }
        public GameHub(IDataBase db, ILogger<GameHub> logger)
        {
            _logger = logger;
            _db = db;
        }
        /// <summary>
        /// Do action when user connects 
        /// </summary>
        /// <returns></returns>
        public override async Task OnConnectedAsync()
        {
           // await Clients.All.SendAsync("SendAction", "user", "joined");
        }

        /// <summary>
        /// Do action when user disconnects 
        /// </summary>
        /// <returns></returns>
        public override async Task OnDisconnectedAsync(Exception ex)
        {
            await Clients.All.SendAsync("SendAction", "user", "left");
        }

        /// <summary>
        /// Send message to all clients
        /// </summary>
        /// <returns></returns>
        public async Task Send(string message)
        {
            _logger.LogInformation($"Player sent {message}");
            await Clients.All.SendAsync("SendMessage", "user x", message);
        }

        /// <summary>
        /// Send message to specific client
        /// </summary>
        /// <returns></returns>
        public async Task SendToClient(string message, string hubId)
        {
            _logger.LogInformation($"Player sent {message}");
            await Clients.Client(hubId).SendAsync("SendMessage", message);
        }

        public async void Welcome(string id)
        {
            var location = System.Reflection.Assembly.GetEntryAssembly().Location;
            var directory = System.IO.Path.GetDirectoryName(location);

            var motd = File.ReadAllText(directory + "/motd");  

           await SendToClient(motd, id);
        }

        public void CreateCharacter(string name = "Liam")
        {
            var newPlayer = new Player()
            {
                Name = name
            };

            _db.Save(newPlayer, DataBase.Collections.Players);

        }
    }
}
