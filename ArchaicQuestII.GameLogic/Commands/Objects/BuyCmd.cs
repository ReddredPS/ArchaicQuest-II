using System;
using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Objects;

public class BuyCmd : ICommand
{
    public BuyCmd(ICore core)
    {
        Aliases = new[] {"buy", "by", "b"};
        Description = "Buy items from a mob selling wares, Use list to view items for sale and inspect to view item properties.";
        Usages = new[] {"Type: buy <Item> or buy <number> e.g Buy Sword or Buy 2 if sword is the second item in the list."};
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
            CharacterStatus.Status.Resting,
            CharacterStatus.Status.Sitting,
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
        var itemName = input.ElementAtOrDefault(1);
        
        if (string.IsNullOrEmpty(itemName))
        {
            Core.Writer.WriteLine("<p>Buy what?</p>", player.ConnectionId);
            return;
        }
        
        
        if (int.TryParse(itemName, out var n))
        {
            BuyItem(n, room, player);
            return;
        }

        var vendor = room.Mobs.FirstOrDefault(x => x.Shopkeeper.Equals(true));

        if (vendor == null)
        {
            Core.Writer.WriteLine("<p>You can't do that here.</p>", player.ConnectionId);
            return;
        }

        var hasItem = vendor.Inventory.FirstOrDefault(x =>
            x.Name.Contains(itemName, StringComparison.InvariantCultureIgnoreCase));

        if (hasItem == null)
        {
            Core.Writer.WriteLine($"<p>{vendor.Name} says 'I don't sell that, please view my \'list\' of items for sale.'</p>", player.ConnectionId);
            return;
        }

        var haggleReduction = Core.PassiveSkills.Haggle(player, vendor);
        var goldValue = AddMarkUp(hasItem.Value);
        var trueGoldValue = goldValue - Helpers.GetPercentage(haggleReduction, (int)goldValue);
        if (player.Money.Gold < trueGoldValue)
        {
            Core.Writer.WriteLine($"<p>{vendor.Name} says 'Sorry you can't afford that.'</p>", player.ConnectionId);
            return;
        }

        player.Money.Gold -= trueGoldValue;

        player.Inventory.Add(hasItem);

        // TODO: weight
        player.Weight += hasItem.Weight;

        Core.UpdateClient.UpdateScore(player);
        Core.UpdateClient.UpdateInventory(player);

        Core.Writer.WriteLine($"<p>You buy {hasItem.Name.ToLower()} for {trueGoldValue} gold.</p>", player.ConnectionId);
    }
    
    public void BuyItem(int itemNumber, Room room, Player player)
    {

        itemNumber -= 1;
        if (itemNumber < 0)
        {
            itemNumber = 0;
        }

        var vendor = room.Mobs.FirstOrDefault(x => x.Shopkeeper.Equals(true));

        if (vendor == null)
        {
            Core.Writer.WriteLine("<p>You can't do that here.</p>", player.ConnectionId);
            return;
        }

        var hasItem = vendor.Inventory.Distinct().OrderBy(x => x.Level).ThenBy(x => x.Value).ToArray()[itemNumber];

        if (hasItem == null)
        {
            Core.Writer.WriteLine($"<p>{vendor.Name} says 'I don't sell that, please view my \'list\' of items for sale.'</p>", player.ConnectionId);
            return;
        }

        var haggleReduction = Core.PassiveSkills.Haggle(player, vendor);
        var goldValue = AddMarkUp(hasItem.Value);
        var trueGoldValue = goldValue - Helpers.GetPercentage(haggleReduction, (int)goldValue);
        if (player.Money.Gold < trueGoldValue)
        {
            Core.Writer.WriteLine($"<p>{vendor.Name} says 'Sorry you can't afford that.'</p>", player.ConnectionId);
            return;
        }

        player.Money.Gold -= trueGoldValue;

        player.Inventory.Add(hasItem);

        // TODO: weight
        player.Weight += hasItem.Weight;

        Core.UpdateClient.UpdateScore(player);
        Core.UpdateClient.UpdateInventory(player);

        Core.Writer.WriteLine($"<p>You buy {hasItem.Name.ToLower()} for {trueGoldValue} gold.</p>", player.ConnectionId);
    }
    
    private int AddMarkUp(int price)
    {
        return (int)Math.Floor(price * 1.5);
    }
    
 
}