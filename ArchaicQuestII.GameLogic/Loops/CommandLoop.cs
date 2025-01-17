﻿using System;
using System.Collections.Generic;
using System.Linq;
using ArchaicQuestII.GameLogic.Character;
using ArchaicQuestII.GameLogic.Core;

namespace ArchaicQuestII.GameLogic.Loops
{
    public class CommandLoop : ILoop
    {
        public int TickDelay => 125; //4000 for lag
        public bool ConfigureAwait => false;
        private int LagTick = 32;
        private List<Player> _laggedPlayers = new List<Player>();
        private List<Player> _bufferedPlayers = new List<Player>();

        public void PreTick()
        {
            var players = Services.Instance.Cache.GetPlayerCache().Values;
            _laggedPlayers = players.Where(x => x.Lag > 0).ToList();
            _bufferedPlayers = players.Where(x => x.Buffer.Count > 0).ToList();
        }

        public void Tick()
        {
            //Console.WriteLine("CommandLoop");

            foreach (var player in _bufferedPlayers)
            {
                // don't action commands if player is lagged
                if (player.Lag > 0)
                {
                    continue;
                }

                var command = player.Buffer.Dequeue();

                player.LastCommandTime = DateTime.Now;

                if (player.CommandLog.Count >= 2500)
                {
                    player.CommandLog.Clear();
                }

                player.CommandLog.Add($"{string.Format("{0:f}", DateTime.Now)} - {command}");

                if (player.Combat != null)
                {
                    player.Combat.AddCommand(player, command);
                    continue;
                }

                var room = Services.Instance.Cache.GetRoom(player.RoomId);
                Services.Instance.CommandHandler.HandleCommand(player, room, command);
            }

            LagTick--;

            if (LagTick <= 0)
            {
                foreach (var player in _laggedPlayers)
                {
                    player.Lag -= 1;

                    if (player.Lag < 0)
                        player.Lag = 0;
                }

                LagTick = 32;
            }
        }

        public void PostTick()
        {
            _laggedPlayers.Clear();
            _bufferedPlayers.Clear();
        }
    }
}
