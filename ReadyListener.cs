using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using RemoteAdmin;
using ServerMod2.API;
using Smod2;
using Smod2.API;
using Smod2.EventHandlers;
using Smod2.Events;
using UnityEngine;

namespace com.mattymatty.AnalyticsAndStorage
{
    public class ReadyListener : IEventHandlerSceneChanged, IEventHandlerPlayerJoin, IEventHandlerWaitingForPlayers
    {

        private bool _single;

        public bool ignore = false;

        private readonly Database db;

        public ReadyListener(Database db)
        {
            this.db = db;
        }

        public void OnSceneChanged(SceneChangedEvent ev)
        {
            if (ev.SceneName == "Facility" && !_single)
            {
                _single = true;
                if(!ignore)
                 Analytics.instance.AddEventHandlers(new EventListener(db), Priority.Lowest);
            }
        }
        public void OnPlayerJoin(PlayerJoinEvent ev)
        {
            if (db.IsConnected())
            {
                JObject obj = db.ExecuteQuery(
                    $"SELECT UserID FROM Users WHERE SteamID64='{ev.Player.SteamId}';");

                if (obj != null && !obj.ContainsKey("error"))
                {
                    JArray arr = obj["result"].ToObject<JArray>();
                    if (arr.Count != 0)
                    {
                        JObject id = arr[0].ToObject<JObject>();
                        SetPid(ev.Player, id["UserID"].ToObject<int>());
                    }
                    else
                    {
                        int pid = Analytics.instance.next_id++;
                        SetPid(ev.Player, pid);

                        db.ExecuteNonQuery(
                            $"INSERT INTO Users (SteamID64, UserID, Name) VALUES ('{ev.Player.SteamId}',{pid},'{ev.Player.Name.Replace("'","''")}')");
                    }

                    db.ExecuteNonQuery(
                        $"INSERT INTO Analytics_PlayerJoin (SteamID64, Time) VALUES ('{ev.Player.SteamId}',strftime('%s',datetime('now','localtime')))");

                }
            }
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
           SetPid(new SmodPlayer(PlayerManager.localPlayer), 0);
            
           //fix id assigning ( start from 0 )
           typeof(QueryProcessor).GetField("_idIterator", BindingFlags.NonPublic | BindingFlags.Static)
               ?.SetValue(null, Analytics.instance.next_id);
        }
              
        
        public static void SetPid(Player player, int pid)
        {
            try
            {
                ((GameObject) player.GetGameObject()).GetComponent<QueryProcessor>().NetworkPlayerId = pid;
                var field = player.GetType()
                    .GetField("<PlayerId>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(player, pid);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}