using System;
using System.Linq;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Skill.Enum;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Spell;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;
using ArchaicQuestII.GameLogic.Combat;

namespace ArchaicQuestII.GameLogic.Commands;

public abstract class SkillCore
{
    public Player GetValidTarget(Player player, Player target, ValidTargets validTargets)
    {
        var setTarget = target;
        if (
            validTargets.HasFlag(ValidTargets.TargetFightSelf)
            && player.Status == CharacterStatus.Status.Fighting
        )
        {
            setTarget = player;
        }

        if (
            validTargets.HasFlag(ValidTargets.TargetFightVictim)
            && player.Status == CharacterStatus.Status.Fighting
        )
        {
            setTarget = target;
        }

        if (validTargets == ValidTargets.TargetIgnore)
        {
            setTarget = player;
        }

        return setTarget;
    }

    public string ReplacePlaceholders(string str, Player player, bool isTarget)
    {
        var newString = String.Empty;
        if (isTarget)
        {
            newString = str.Replace("#target#", "You");

            return newString;
        }

        newString = str.Replace("#target#", player.Name);

        return newString;
    }

    /// <summary>
    /// Emote action to the target and to the room
    /// </summary>
    /// <param name="textToTarget">Text the target should see</param>
    /// <param name="textToRoom">Text the room should see</param>
    /// <param name="target">Target of the action</param>
    /// <param name="room">Current Room</param>
    /// <param name="player">The Player</param>
    public void EmoteAction(
        string textToTarget,
        string textToRoom,
        string target,
        Room room,
        Player player
    )
    {
        foreach (var pc in room.Players.Where(x => x.Name != player.Name))
        {
            if (pc.Name.Equals(target))
            {
                Services.Instance.Writer.WriteLine(textToTarget, pc);
                continue;
            }

            Services.Instance.Writer.WriteLine(textToRoom, pc);
        }
    }

    public void EmoteEffectWearOffAction(Player player, Room room, SkillMessage emote)
    {
        foreach (var pc in room.Players)
        {
            if (pc.ConnectionId.Equals(player.ConnectionId))
            {
                Services.Instance.Writer.WriteLine($"<p>{emote.EffectWearOff.ToPlayer}</p>", pc);
                continue;
            }

            Services.Instance.Writer.WriteLine(
                $"<p>{ReplacePlaceholders(emote.EffectWearOff.ToRoom, player, false)}</p>",
                pc
            );
        }
    }

    /// <summary>
    /// Check if skill is a success comparing the attackers dexterity to the target
    /// and any level difference. for example:
    /// Player Level = 5, Target Level = 15
    /// Player Dexterity = 60, Target Dexterity = 75
    ///
    ///  base chance = 65
    ///  65 + 60 = 125
    ///  125 - 75 = 50
    ///  50 + 5 - 15 = 40
    ///
    /// Chance for success is 40
    ///
    /// roll 1d100 if it's <= 40 then it's a hit
    ///
    /// if everyone is equal it's 65% chance for success
    /// </summary>
    /// <param name="player"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public bool DexterityAndLevelCheck(Player player, Player target)
    {
        /*dexterity check */
        var chance = 65;
        chance += player.Attributes.Attribute[EffectLocation.Dexterity];
        chance -= target.Attributes.Attribute[EffectLocation.Dexterity];

        if (player.Affects.Haste)
        {
            chance += 25;
        }

        if (target.Affects.Haste)
        {
            chance -= 25;
        }

        /* level check */
        chance += player.Level - target.Level;

        return DiceBag.Roll(1, 1, 100) <= chance;
    }

    public void DamagePlayer(string skillName, int damage, Player player, Player target, Room room)
    {
        if (target.IsAlive())
        {
            var totalDam = CombatHandler.CalculateSkillDamage(player, target, damage);

            Services.Instance.Writer.WriteLine(
                $"<p>Your {skillName} {Services.Instance.Damage.DamageText(totalDam).Value} {target.Name}  <span class='damage'>[{damage}]</span></p>",
                player
            );
            Services.Instance.Writer.WriteLine(
                $"<p>{player.Name}'s {skillName} {Services.Instance.Damage.DamageText(totalDam).Value} you!  <span class='damage'>[{damage}]</span></p>",
                target
            );

            foreach (var pc in room.Players)
            {
                if (
                    pc.ConnectionId.Equals(player.ConnectionId)
                    || pc.ConnectionId.Equals(target.ConnectionId)
                )
                {
                    continue;
                }

                Services.Instance.Writer.WriteLine(
                    $"<p>{player.Name}'s {skillName} {Services.Instance.Damage.DamageText(totalDam).Value} {target.Name}  <span class='damage'>[{damage}]</span></p>",
                    pc
                );
            }

            target.Attributes.Attribute[EffectLocation.Hitpoints] -= totalDam;

            if (!target.IsAlive())
            {
                var combatant = new Combatant(player, true);
                combatant.target = target;
                CombatHandler.TargetKilled(combatant, room);

                Services.Instance.UpdateClient.UpdateHP(target);
                return;
            }

            //update UI
            Services.Instance.UpdateClient.UpdateHP(target);

            if (player.Combat == null)
            {
                var combat = new Fight(player, target, room, false);
            }
        }
    }

    public Player FindTargetInRoom(string targetName, Room room, Player player)
    {
        var target =
            room.Players.FirstOrDefault(
                x => x.Name.StartsWith(targetName, StringComparison.CurrentCultureIgnoreCase)
            )
            ?? room.Mobs.FirstOrDefault(
                x => x.Name.StartsWith(targetName, StringComparison.CurrentCultureIgnoreCase)
            );

        if (target != null)
        {
            return target;
        }

        Services.Instance.Writer.WriteLine("They are not here.", player);
        return null;
    }

    /// <summary>
    /// Finds the item in the room or inventory
    /// </summary>
    /// <param name="obj">name of object to find</param>
    /// <param name="room">The current room</param>
    /// <param name="player">The player</param>
    /// <returns></returns>
    public Item.Item FindItem(string obj, Room room, Player player)
    {
        var nthTarget = Helpers.findNth(obj);
        return Helpers.findRoomObject(nthTarget, room) ?? player.FindObjectInInventory(nthTarget);
    }

    protected bool CanPerformSkill(Skill.Model.Skill skill, Player player)
    {
        var playerHasSkill = player.Skills.FirstOrDefault(x => x.Name.Equals(skill.Name));

        if (playerHasSkill == null)
        {
            Services.Instance.Writer.WriteLine($"You do not know this skill.", player);
            return false;
        }

        if (player.Level < playerHasSkill.Level)
        {
            Services.Instance.Writer.WriteLine(
                $"You are not of the right level to use this skill.",
                player
            );
            return false;
        }

        if (skill.ManaCost > player.Attributes.Attribute[EffectLocation.Mana])
        {
            Services.Instance.Writer.WriteLine(
                $"You do not have enough mana to cast {skill.Name}.",
                player
            );
            return false;
        }

        if (skill.MoveCost > player.Attributes.Attribute[EffectLocation.Moves])
        {
            Services.Instance.Writer.WriteLine($"You are too tired to {skill.Name}.", player);
            return false;
        }

        return true;
    }
}

public class SkillList
{
    public int Id { get; set; }
    public SkillName Name { get; set; }
    public int Level { get; set; }
    public int Proficiency { get; set; } = 1;
    public bool IsSpell { get; set; }
}
