﻿using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.World.Room;

namespace ArchaicQuestII.GameLogic.Combat
{
    public interface ICombat
    {
        public void InitFightStatus(Player player, Player target);
        public void TargetKilled(Player player, Player target, Room room);

        public int CalculateSkillDamage(Player player, Player target, int damage);
        public Player FindTarget(Player attacker, string target, Room room, bool isMurder);

        public Item.Item GetWeapon(Player player, bool dualWield);

        public void DisplayDamage(Player player, Player target, Room room, Item.Item weapon, int damage);

        public void Fight(Player player, string victim, Room room, bool isMurder);

        public void DeathCry(Room room, Player target);

        public void AddCharToCombat(Player character);

        // public void AutoAttack(Player player, Player target, Room room, bool isMurder);
    }
}
