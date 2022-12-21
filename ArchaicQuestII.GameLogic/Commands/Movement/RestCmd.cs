using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Movement;

public class RestCmd : ICommand
{
    public RestCmd(ICore core)
    {
        Aliases = new[] {"rest"};
        Description = "Your character will rest.";
        Usages = new[] {"Type: rest"};
        Title = "";
        DeniedStatus = new[]
        {
            CharacterStatus.Status.Busy,
            CharacterStatus.Status.Dead,
            CharacterStatus.Status.Fighting,
            CharacterStatus.Status.Ghost,
            CharacterStatus.Status.Fleeing,
            CharacterStatus.Status.Incapacitated,
            CharacterStatus.Status.Sleeping,
            CharacterStatus.Status.Stunned,
            CharacterStatus.Status.Resting
        };
        UserRole = UserRole.Player;
        Core = core;
    }
    
    public string[] Aliases { get; }
    public string Description { get; }
    public string[] Usages { get; }
    public string Title { get; }
    public CharacterStatus.Status[] DeniedStatus { get; }
    public UserRole UserRole { get; }
    public ICore Core { get; }

    public void Execute(Player player, Room room, string[] input)
    {
        if (!string.IsNullOrEmpty(player.Mounted.Name))
        {
            Core.Writer.WriteLine("<p>You can't do that while mounted.</p>", player.ConnectionId);
            return;
        }

        if (player.Status == CharacterStatus.Status.Resting)
        {
            Core.Writer.WriteLine("<p>You are already resting!</p>", player.ConnectionId);
            return;
        }

        Core.Writer.WriteLine("<p>You sprawl out haphazardly.</p>", player.ConnectionId);
        SetCharacterStatus(player, "is sprawled out here", CharacterStatus.Status.Resting);
        Core.Writer.WriteToOthersInRoom($"<p>{player.Name} sprawls out haphazardly.</p>", room, player);
    }

    private void SetCharacterStatus(Player player, string longName, CharacterStatus.Status status)
    {
        player.Status = status;
        player.LongName = longName;
        player.Pose = "";
    }
}