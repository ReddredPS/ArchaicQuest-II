using System.Collections.Generic;
using ArchaicQuestII.DataAccess;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.World.Room;
using Newtonsoft.Json;

namespace ArchaicQuestII.GameLogic.Commands.Character
{
    public class SaveCmd : ICommand
    {
        public SaveCmd(ICore core)
        {
            Aliases = new[] {"save"};
            Description = "Save your character manually, character is saved when you quit and automatically every 15 minutes.";
            Usages = new[] {"Type: save"};
            UserRole = UserRole.Player;
            Title = "";
            DeniedStatus = new []
            {
                CharacterStatus.Status.Sleeping,
                CharacterStatus.Status.Dead,
            };
            Core = core;
        }
        
        public string[] Aliases { get; }
        public string Description { get; }
        public string[] Usages { get; }
        public string Title { get; }
        public UserRole UserRole { get; }
        public CharacterStatus.Status[] DeniedStatus { get; }
        public ICore Core { get; }

        public void Execute(Player player, Room room, string[] input)
        {

            var newPlayer = JsonConvert.DeserializeObject<Player>(
                JsonConvert.SerializeObject(player));
            
            newPlayer.Followers = new List<Player>();
            newPlayer.Following = string.Empty;
            newPlayer.Grouped = false;
    
            Core.PlayerDataBase.Save(newPlayer, PlayerDataBase.Collections.Players);
            Core.Writer.WriteLine("<p>Character saved.</p>", player.ConnectionId);
        }
    }
}