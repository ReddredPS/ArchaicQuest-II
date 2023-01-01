using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Model;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Item;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Objects;

public class HarvestCmd : ICommand
{
    public HarvestCmd(ICore core)
    {
        Aliases = new[] {"harvest", "forage"};
        Description = "Some plants or other objects can be harvested. Harvesting takes some time and the amount harvested is random. " +
                      "You may also fail the task or be attacked while harvesting. Wood-Elfs are the best at harvesting. <br />Forage or harvest can be used for this command";
        Usages = new[] {"Type: harvest strawberry, forage bush"};
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
        var target = input.ElementAtOrDefault(1);

        if (string.IsNullOrEmpty(target))
        {
            Core.Writer.WriteLine("Harvest what?", player.ConnectionId);
            return;
        }
        
        if (player.Status == CharacterStatus.Status.Busy)
        {
            Core.Writer.WriteLine("You are already doing it.", player.ConnectionId);
            return;
        }
        
        var thingToHarvest =
            room.Items.FirstOrDefault(x => x.Name.StartsWith(target, StringComparison.OrdinalIgnoreCase));

        if (thingToHarvest == null)
        {
            Core.Writer.WriteLine($"You don't see that here.", player.ConnectionId);
            return;
        }

        if (!thingToHarvest.Container.Items.Any())
        {
            Core.Writer.WriteLine("There's nothing left to harvest.", player.ConnectionId);
            return;
        }

        Harvest(player, room, thingToHarvest);
    }

    private async void Harvest(Player player, Room room, Item.Item thingToHarvest)
    {
        player.Status = CharacterStatus.Status.Busy;

        Core.UpdateClient.PlaySound("foraging", player);

        Core.Writer.WriteLine($"<p>You begin harvesting from {thingToHarvest.Name}.</p>", player.ConnectionId);
        
        await Task.Delay(4000);
        
        if (!room.Players.Contains(player) || player.Status != CharacterStatus.Status.Busy)
        {
            return;
        }

        Core.UpdateClient.PlaySound("foraging", player);

        Core.Writer.WriteLine("<p>You rummage through the foliage looking for something to harvest.</p>",
            player.ConnectionId);
        
        await Task.Delay(4000);
        
        if (!room.Players.Contains(player) || player.Status != CharacterStatus.Status.Busy)
        {
            return;
        }

        Core.UpdateClient.PlaySound("foraging", player);

        Core.Writer.WriteLine("<p>You continue searching.</p>", player.ConnectionId);

        await Task.Delay(4000);
        
        if (!room.Players.Contains(player) || player.Status != CharacterStatus.Status.Busy)
        {
            return;
        }
        
        var roll = DiceBag.Roll(1, 1, 10);

        var randomMobObj = roll switch
        {
            1 => new
            {
                Name = "A snake",
                Description = "A black and red snake",
                LongName = "A black snake slithers along here.",
                DefaultAttack = "bite"
            },
            2 => new
            {
                Name = "A giant hornet",
                Description = "An red and orange hornet",
                LongName = "An angry red and orange hornet buzzes around here.",
                DefaultAttack = "sting"
            },
            3 => new
            {
                Name = "An angry fairy",
                Description = "A short winged fairy, angry at being disturbed",
                LongName = "An angry fairy, annoyed at being disturbed is here.",
                DefaultAttack = "punch"
            },
            _ => new
            {
                Name = "A racoon", Description = "A large grey racoon dog",
                LongName = "A large racoon looks at you angrily", DefaultAttack = "bite"
            }
        };

        var randomMob = new Player
        {
            Name = randomMobObj.Name,
            ClassName = "Fighter",
            Target = string.Empty,
            Status = CharacterStatus.Status.Standing,
            Race = "Other",
            Level = roll + 2,
            RoomId = player.RoomId,
            Attributes = new Attributes
            {
                Attribute = new Dictionary<EffectLocation, int>
                {
                    { EffectLocation.Mana, 30 },
                    { EffectLocation.Hitpoints, 300 },
                    { EffectLocation.Moves, 30 },
                    { EffectLocation.Strength, 30 },
                    { EffectLocation.Dexterity, 30 },
                    { EffectLocation.Constitution, 30 },
                    { EffectLocation.Intelligence, 30 },
                    { EffectLocation.Wisdom, 30 },
                    { EffectLocation.Charisma, 30 },
                    { EffectLocation.HitRoll, 2 },
                    { EffectLocation.DamageRoll, 1 },
                    { EffectLocation.SavingSpell, 1 },
                }
            },
            MaxAttributes = new Attributes
            {
                Attribute = new Dictionary<EffectLocation, int>
                {
                    { EffectLocation.Mana, 30 },
                    { EffectLocation.Hitpoints, 300 },
                    { EffectLocation.Moves, 30 },
                    { EffectLocation.Strength, 30 },
                    { EffectLocation.Dexterity, 30 },
                    { EffectLocation.Constitution, 30 },
                    { EffectLocation.Intelligence, 30 },
                    { EffectLocation.Wisdom, 30 },
                    { EffectLocation.Charisma, 30 },
                    { EffectLocation.HitRoll, 2 },
                    { EffectLocation.DamageRoll, 1 },
                    { EffectLocation.SavingSpell, 1 },
                }
            },

            Description = randomMobObj.Description,
            LongName = randomMobObj.LongName,
            ArmorRating = new ArmourRating
            {
                Armour = 5,
                Magic = 5
            },
            DefaultAttack = randomMobObj.DefaultAttack,
            UniqueId = Guid.NewGuid(),
            Id = Guid.NewGuid()
        };
        
        if (roll <= 1)
        {
            Core.Writer.WriteLine(
                $"<p>{{yellow}}{randomMob.Name} jumps out from the {thingToHarvest.Name} and attacks you!{{/}}</p>",
                player.ConnectionId);
            room.Mobs.Add(randomMob);
            player.Status = CharacterStatus.Status.Standing;
            InitFightStatus(randomMob, player);
            return;
        }

        if (roll <= 3)
        {
            Core.Writer.WriteLine(
                $"<p>{{yellow}}You cut yourself foraging, OUCH!{{/}}</p>",
                player.ConnectionId);
            player.Status = CharacterStatus.Status.Standing;
            return;
        }

        var canDoSkill = Helpers.SkillSuccessCheck(player, "foraging");

        if (!canDoSkill)
        {
            Core.Writer.WriteLine("<p>You fail to harvest a thing.</p>", player.ConnectionId);
            Core.Writer.WriteLine(Helpers.SkillLearnMistakes(player, "foraging", Core.Gain), player.ConnectionId);
            player.Status = CharacterStatus.Status.Standing;
            return;
        }

        var collected = "";
        var collectedCount = 1;
        
        foreach (var harvestItem in thingToHarvest.Container.Items.Where(harvestItem => DiceBag.Roll(1, 1, 10) <= 3))
        {
            collected = harvestItem.Name;
            collectedCount++;

            // if the User has herbalism the conditions will be higher rated
            // on success skill check the roll could be 50, 100
            // if elven the condition can be 10 points higher
            harvestItem.Condition = DiceBag.Roll(1, 1, 65);

            switch (player.Race)
            {
                case "Wood-Elf":
                    harvestItem.Condition += DiceBag.Roll(1, 10, 20);
                    break;
                case "Elf":
                case "Drow":
                    harvestItem.Condition += DiceBag.Roll(1, 5, 10);
                    break;
            }

            player.Inventory.Add(harvestItem);
        }

        for (var i = 0; i < thingToHarvest.Container.Items.Count; i++)
        {
            thingToHarvest.Container.Items.Remove(thingToHarvest.Container.Items[i]);
        }

        Core.Writer.WriteLine(
            $"<p>Ah you have collected {collectedCount} {collected}{(collectedCount > 1 ? "'s" : "")}</p>",
            player.ConnectionId);
        player.Status = CharacterStatus.Status.Standing;
    }

    private void InitFightStatus(Player player, Player target)
    {
        player.Target = string.IsNullOrEmpty(player.Target) ? target.Name : player.Target;
        player.Status = CharacterStatus.Status.Fighting;
        target.Status = (target.Status & CharacterStatus.Status.Stunned) != 0 ? CharacterStatus.Status.Stunned : CharacterStatus.Status.Fighting;
        target.Target = string.IsNullOrEmpty(target.Target) ? player.Name : target.Target; //for group combat, if target is ganged, there target should not be changed when combat is initiated.

        if (player.Target == player.Name)
        {
            player.Status = CharacterStatus.Status.Standing;
            return;
        }

        if (!Core.Cache.IsCharInCombat(player.Id.ToString()))
        {
            Core.Cache.AddCharToCombat(player.Id.ToString(), player);
        }

        if (!Core.Cache.IsCharInCombat(target.Id.ToString()))
        {
            Core.Cache.AddCharToCombat(target.Id.ToString(), target);
        }
    }
}