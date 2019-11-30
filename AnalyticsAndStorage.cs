using System;
using System.Reflection;
using Newtonsoft.Json.Linq;
using RemoteAdmin;
using Smod2;
using Smod2.Attributes;
using Smod2.Config;
using Smod2.Events;

namespace com.mattymatty.AnalyticsAndStorage
{
    [PluginDetails(
        name = "AnalyticsAndStorage",
        author = "The Matty",
        description = "analytics for SCP:SL ",
        id = "com.mattymatty.analytics",
        SmodMajor = 3,
        SmodMinor = 6,
        SmodRevision = 0,
        version = "1.0.0"
    )]
    public class Analytics : Plugin
    {
        public static Analytics instance;

        public int next_id = 100;

        private readonly Database db = new Database();

        private readonly ReadyListener _listener;

        public Analytics()
        {
            _listener = new ReadyListener(db);
        }

        public override void Register()
        {
            instance = this;
            AddEventHandlers(_listener, Priority.Highest);
            AddConfig(new ConfigSetting("Analytics_server","localhost",false,true,"The python server handling the database"));
            AddConfig(new ConfigSetting("Analytics_port",10000,false,true,"The port where the python server is listening"));

            db.StartConnection();
        }

        public override void OnEnable()
        {
            bool condition;

            condition = db.IsConnected();

            condition = condition && db.ExecuteNonQuery(
                            "create table if not exists Users ( SteamID64 text,UserID int default 0,Name text, primary key (SteamID64))");


            condition = condition && db.ExecuteNonQuery(
                            "create unique index if not exists Users_UserID_uindex on Users (UserID)");
            
            if (condition)
            {
                JObject id = db.ExecuteQuery("SELECT MAX(UserID) as max FROM Users");
                if (id != null && !id.ContainsKey("error"))
                {
                    JArray arr = id["result"].ToObject<JArray>();
                    JObject res = arr[0].ToObject<JObject>();
                    if (res["max"].Type == JTokenType.Integer)
                    {
                        next_id = Math.Max(next_id,res["max"].ToObject<int>()+1);
                    }
                }
            }

            condition = condition && db.ExecuteNonQuery("CREATE TABLE if not exists Plugins (Tag text, Name text, Version text )");

            if (condition)
            {
                JObject id = db.ExecuteQuery($"SELECT * FROM Plugins WHERE Tag='{Details.id}' AND Version='{Details.version}'");
                if (id != null && !id.ContainsKey("error"))
                {
                    JArray arr = id["result"].ToObject<JArray>();
                    if (arr.Count == 0)
                    {
                        condition = db.ExecuteNonQuery($"INSERT INTO Plugins (Tag, Name, Version) VALUES ('{Details.id}','{Details.name}','{Details.version}')");
                    }
                }
                else
                {
                    condition = false;
                }
            }
            
            condition = condition && db.ExecuteNonQuery("create table if not exists Analytics_PlayerJoin(SteamID64 text references Users,Time int);");
            
            condition = condition && db.ExecuteNonQuery("create table if not exists Analytics_PlayerSnap(SteamID64 text references Users,Role text,Time int, primary key (SteamID64, Role, Time));");
            
            condition = condition && db.ExecuteNonQuery("create table if not exists Analytics_PlayerDie(Target text,TRole text,Killer text,KRole text,Time int,primary key (Target, Time),foreign key (Target, Killer) references Users (SteamID64, SteamID64));");
            
            if (!condition)
            {
                Logger.Error("Analytics","Error initializing Database");
            }

            //fix id assigning ( start from 0 )
            typeof(QueryProcessor).GetField("_idIterator", BindingFlags.NonPublic | BindingFlags.Static)
                ?.SetValue(null, -1);
        }

        public override void OnDisable()
        {
        }
    }
}