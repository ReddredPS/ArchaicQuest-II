
using System.Linq;
using ArchaicQuestII.GameLogic.Account;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Skill.Model;
using ArchaicQuestII.GameLogic.Utilities;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Commands.Skills
{
    public class OverHeadCrushCmd :  SkillCore, ICommand
    {
        public OverHeadCrushCmd(ICore core): base (core)
        {
            Aliases = new[] { "overheadcrush", "ohc", "overh", "overheadc"};
            Description = "Bring down your weapon with a powerful overhead crush, bonus if the weapon type is blunt. ";
            Usages = new[] { "Type: ohc bob, overheadcrush bob", "overh bob" };
            DeniedStatus = new [] { CharacterStatus.Status.Sleeping, CharacterStatus.Status.Resting, CharacterStatus.Status.Dead, CharacterStatus.Status.Mounted, CharacterStatus.Status.Stunned };
            Title = DefineSkill.OverheadCrush().Name;
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
   
            var canDoSkill = CanPerformSkill(DefineSkill.OverheadCrush(), player);
            if (!canDoSkill)
            { 
                return;
            }
            
            if (player.Equipped.Wielded == null)
            {
                Core.Writer.WriteLine("You need to have a weapon equipped to do this.", player.ConnectionId);
                return;
            }
 

            var obj = input.ElementAtOrDefault(1)?.ToLower() ?? player.Target;
            if (string.IsNullOrEmpty(obj))
            {
                Core.Writer.WriteLine("Overhead crush to  What!?.", player.ConnectionId);
                return;
            }
          
            var target = FindTargetInRoom(obj, room, player);
            if (target == null)
            {
                return;
            }
            
            var textToTarget = string.Empty;
            var textToRoom = string.Empty;

            var skillSuccess = SkillSuccessWithMessage(player, DefineSkill.OverheadCrush(), $"You attempt to crush the skull of {target.Name} but miss.");
            if (!skillSuccess)
            { 
                textToTarget = $"{player.Name} tries to crush your skull but misses."; 
                textToRoom = $"{player.Name} tries to crush the skull of {target.Name} but misses.";
                
                EmoteAction(textToTarget, textToRoom, target.Name, room, player);
                player.FailedSkill(SkillName.OverheadCrush, out var message);
                Core.Writer.WriteLine(message, player.ConnectionId);
                player.Lag += 1;
                return;
            }


            var weaponDam = player.Equipped.Wielded.Damage.Maximum + 
                            player.Equipped.Wielded.WeaponType == SkillName.Hammer
                ? 10
                : 0;
            var str = player.Attributes.Attribute[EffectLocation.Strength];
            var damage = weaponDam + DiceBag.Roll(1, 3, 10) + str / 5;


            DamagePlayer(DefineSkill.OverheadCrush().Name, damage, player, target, room);

            player.Lag += 1;

            updateCombat(player, target, room);
            
        }
    }

}