﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Character.Gain;
using ArchaicQuestII.GameLogic.Character.Status;
using ArchaicQuestII.GameLogic.Core;
using ArchaicQuestII.GameLogic.Effect;
using ArchaicQuestII.GameLogic.Item;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Combat
{
   public class Combat: ICombat
    {
        private readonly IWriteToClient _writer;
        private readonly IUpdateClientUI _clientUi;
        private readonly IGain _gain;
        private readonly IDamage _damage;
        private readonly IFormulas _formulas;
        private readonly ICache _cache;
        public Combat(IWriteToClient writer, IUpdateClientUI clientUi, IDamage damage, IFormulas formulas, IGain gain, ICache cache)
        {
            _writer = writer;
            _clientUi = clientUi;
            _damage = damage;
            _formulas = formulas;
            _gain = gain;
            _cache = cache;
        }

        public Player FindTarget(Player attacker, string target, Room room, bool isMurder)
        {
            // If mob
            if (!isMurder && attacker.ConnectionId != "mob")
            {
                return (Player)room.Mobs.FirstOrDefault(x => x.Name.Contains(target));
            }

            if (attacker.ConnectionId == "mob")
            {
                return (Player)room.Players.FirstOrDefault(x => x.Name.Equals(target));
            }

            return (Player)room.Players.FirstOrDefault(x => x.Name.StartsWith(target));
        }

    

        public Item.Item GetWeapon(Player player)
        {
            return player.Equipped.Wielded;
        }


        public void HarmTarget(Player victim, int damage)
        {
            victim.Attributes.Attribute[EffectLocation.Hitpoints] -= damage;

            if (victim.Attributes.Attribute[EffectLocation.Hitpoints] < 0)
            {
                victim.Attributes.Attribute[EffectLocation.Hitpoints] = 0;
            }

        }

        public bool IsTargetAlive(Player victim)
        {
           return victim.Attributes.Attribute[EffectLocation.Hitpoints] > 0;
        }

        public void DisplayDamage(Player player, Player target, Room room, Item.Item weapon, int damage)
        {
            var damText = _damage.DamageText(damage);
            var attackType = "";
            if (weapon == null)
            {
                attackType = "punch";
            }
            else
            {
                attackType = nameof(weapon.AttackType);
            }


            _writer.WriteLine($"<p>Your {attackType} {damText.Value} {target.Name}. <span>[{damage}]</span></p>", player.ConnectionId);
            _writer.WriteLine($"<p>{player.Name} {attackType} {damText.Value} you. <span>[{damage}]</span></p></p>", target.ConnectionId);

            foreach (var pc in room.Players)
            {
                if (pc.Name == player.Name || pc.Name == target.Name)
                {
                    continue;
                }

                _writer.WriteLine($"<p>{player.Name}'s {attackType} {damText.Value} {target.Name}.</p>", pc.ConnectionId);
            }
        }

        public void DisplayMiss(Player player, Player target, Room room, Item.Item weapon)
        {
            var attackType = "";
            if (weapon == null)
            {
                attackType = "punch";
            }
            else
            {
                attackType = nameof(weapon.AttackType);
            }
            
            _writer.WriteLine($"<p>Your {attackType} misses {target.Name}.</p>", player.ConnectionId);
            _writer.WriteLine($"<p>{player.Name} {attackType} misses you.</p>", target.ConnectionId);

            foreach (var pc in room.Players)
            {
                if (pc.Name == player.Name || pc.Name == target.Name)
                {
                    continue;
                }

                _writer.WriteLine($"<p>{player.Name}'s {attackType} misses {target.Name}.</p>", pc.ConnectionId);
            }
        }

        public void Fight(Player player, string victim, Room room, bool isMurder)
        {
            var target = FindTarget(player, victim, room, isMurder);

            if (target == null)
            {
                _writer.WriteLine("<p>They are not here.</p>", player.ConnectionId);
                return;
            }

            player.Target = target.Name;
            player.Status = CharacterStatus.Status.Fighting;
            target.Status = CharacterStatus.Status.Fighting;
            target.Target = player.Name;

           if(!_cache.IsCharInCombat(player.Id.ToString()))
            {
                _cache.AddCharToCombat(player.Id.ToString(), player);
            }

            if(!_cache.IsCharInCombat(target.Id.ToString()))
            {
                _cache.AddCharToCombat(target.Id.ToString(), target);
            }
            var chanceToHit = _formulas.ToHitChance(player, target);
            var doesHit = _formulas.DoesHit(chanceToHit);
            var weapon = GetWeapon(player);
            if (doesHit)
            {
             
                var hasEvadedDamage = false;

                // avoidance percentage can be improved by core skills 
                // such as improved parry, acrobatic etc 
                // instead of rolling a D10, roll a D6 for a close to 15% increase in chance

                var avoidanceRoll = new Dice().Roll(1, 1, 10);


                //10% chance to attempt a dodge
                if (avoidanceRoll == 1)
                {

                }

                //10% chance to parry
                if (!hasEvadedDamage && avoidanceRoll == 2)
                {

                }

                // Block
                if (!hasEvadedDamage && avoidanceRoll == 3)
                {
                    var chanceToBlock = _formulas.ToBlockChance(target, player);
                    var doesBlock = _formulas.DoesHit(chanceToBlock);

                    if (doesBlock)
                    {

                    }
                    else
                    {
                        // block fail
                    }
                }
 

                var damage = _formulas.CalculateDamage(player, target, weapon);

                if (_formulas.IsCriticalHit())
                {
                    // double damage
                    damage *= 2;
                }


                HarmTarget(target, damage);
                DisplayDamage(player, target, room, weapon, damage);

                if (!IsTargetAlive(target))
                {
                    _gain.GainExperiencePoints(player, target);
                }


                 
                // award xp if dead
                // create corpse container holding targets inventory and money
                // end combat
            }
            else
            {
                DisplayMiss(player, target, room, weapon);
                // miss message
                // gain improvements on weapon skill
            }

        }
    }
}
 