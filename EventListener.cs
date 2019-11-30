using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using RemoteAdmin;
using ServerMod2.API;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using UnityEngine;

namespace com.mattymatty.AnalyticsAndStorage
{
    public class EventListener : IEventHandler, IEventHandlerUpdate, IEventHandlerPlayerDie
    {
        private readonly Database db;

        public EventListener(Database db)
        {
            this.db = db;
        }

        public void OnPlayerDie(PlayerDeathEvent ev)
        {
            if (db.IsConnected())
            {
                if (ev.Killer != null)
                {
                    db.ExecuteNonQuery(
                        $"INSERT INTO Analytics_PlayerDie(Target, TRole, Killer, KRole, Time) VALUES ('{ev.Player.SteamId}','{ev.Player.TeamRole.Role}','{ev.Killer.SteamId}','{ev.Killer.TeamRole.Role}',strftime('%s',datetime('now','localtime')))"
                    );
                }
                else
                {
                    db.ExecuteNonQuery(
                        $"INSERT INTO Analytics_PlayerDie(Target, TRole, Time) VALUES ('{ev.Player.SteamId}','{ev.Player.TeamRole.Role}',strftime('%s',datetime('now','localtime')))"
                    );
                }
            }
        }
        

        DateTime last = DateTime.MinValue;
        public void OnUpdate(UpdateEvent ev)
        {
            if (DateTime.Compare(DateTime.Now.AddSeconds(-30),last)>0)
            {
                last = DateTime.Now;
                if (db.IsConnected())
                {
                    List<Player> players = Analytics.instance.Server.GetPlayers();
                    players.ForEach(player =>
                    {
                        if ((GameObject) player.GetGameObject() != PlayerManager.localPlayer)
                            db.ExecuteNonQuery(
                                $"INSERT INTO Analytics_PlayerSnap (SteamID64, Role, Time) VALUES ('{player.SteamId}','{player.TeamRole.Role}',strftime('%s',datetime('now','localtime')))");
                    });
                }
                else
                {
                    db.StartConnection();
                }
            }
        }
    }
}