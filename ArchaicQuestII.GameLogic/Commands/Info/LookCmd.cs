using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Info
{
    public class LookCmd : ICommand
    {
        public LookCmd(IWriteToClient writeToClient, ICache cache, IUpdateClientUI updateClient, IRoomActions roomActions)
        {
            Aliases = new[] {"look"};
            Description = "Shows info about room or object.";
            Usages = new[] {"Type: look"};
            UserRole = UserRole.Player;
            Writer = writeToClient;
            Cache = cache;
            UpdateClient = updateClient;
            RoomActions = roomActions;
        }
        
        public string[] Aliases { get; }
        public string Description { get; }
        public string[] Usages { get; }
        public UserRole UserRole { get; }
        public IWriteToClient Writer { get; }
        public ICache Cache { get; }
        public IUpdateClientUI UpdateClient { get; }
        public IRoomActions RoomActions { get; }

        public void Execute(Player player, Room room, string[] input)
        {
            var target = input.ElementAtOrDefault(1);
            RoomActions.Look(target, room, player);
        }
    }
}