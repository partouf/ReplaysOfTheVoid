using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IronPython.Hosting;
using IronPython.Runtime;
using Microsoft.Scripting;
using Microsoft.Scripting.Hosting;
using System.Text.RegularExpressions;

namespace replaysofthevoid
{
    public class PythonHelper
    {
        public static string conv2string(dynamic value)
        {
            if (value is IronPython.Runtime.Bytes)
            {
                IronPython.Runtime.Bytes v = value;
                return Encoding.UTF8.GetString(v.ToByteArray());
            }
            else if (value is string)
            {
                // redo encoding to unicode, was assumed to be ANSI while string was UTF8
                string v = value;
                byte[] arr = new byte[v.Length];
                for (var i = 0; i < v.Length; i++)
                {
                    arr[i] = (byte)v[i];
                }
                return Encoding.UTF8.GetString(arr);
            }
            else
            {
                throw new Exception("unknown type");
            }
        }
    }

    public class ReplayPlayer
    {
        public string clantag = "";
        public string name = "";
        public int teamid = 0;
        public string race = "";

        public ReplayPlayer(PythonDictionary info)
        {
            var temp = PythonHelper.conv2string(info["m_name"]);
            var arr = Regex.Split(temp, "<sp/>");
            if (arr.Length == 2)
            {
                clantag = arr[0];
                name = arr[1];
            }
            else
            {
                name = temp;
            }


            teamid = (int)info["m_teamId"];
            race = PythonHelper.conv2string(info["m_race"]);
        }

        public override string ToString()
        {
            return teamid.ToString() + ". " + name + " (" + race + ")";
        }
    }

    public class Replay
    {
        private DateTime _replaydate;
        private string _race1 = "";
        private string _race2 = "";
        private string _map = "";
        private string _filename = "";
        private string _directory = "";
        private DateTime _filecreated;

        // public properties to expose to userinterface
        public Boolean dorename { get; set; }
        public DateTime replaydate { get { return _replaydate; } }
        public string race1 { get { return _race1; } }
        public string race2 { get { return _race2; } }
        public string map { get { return _map; } }
        public string player1 { get; set; }
        public string player2 { get; set; }
        public string renameto
        {
            get
            {
                // todo: make format editable

                // format: Matchup - Map - Player 1, Player 2
                return
                    race1 + "v" + race2 +
                    " - " +
                    map +
                    " - " +
                    player1 + ", " +
                    player2 +
                    ".SC2Replay";
            }
        }

        public string filename { get { return _filename; } }
        public string directory { get { return _directory; } }

        public DateTime filecreated { get { return _filecreated; } }

        public Replay(string filename, DateTime datecreated, string directory)
        {
            this.dorename = true;
            this._filecreated = datecreated;
            this._replaydate = datecreated;
            this._filename = filename;
            this._directory = directory;
            this.player1 = "";
            this.player2 = "";
            this._race1 = "";
            this._race2 = "";
        }

        public void SetReplayDate(DateTime dt)
        {
            this._replaydate = dt;
        }

        public void SetMap(string mapname)
        {
            this._map = mapname;
        }

        public void AddPlayer(ReplayPlayer player)
        {
            if (player.teamid == 0)
            {
                if (this.player1 != "")
                {
                    this.player1 += " and ";
                }
                this.player1 = this.player1 + (player.clantag + " " + player.name).Trim();
                this._race1 = "" + player.race[0];
            }
            else if (player.teamid == 1)
            {
                if (this.player2 != "")
                {
                    this.player2 += " and ";
                }
                this.player2 = this.player2 + (player.clantag + " " + player.name).Trim();
                this._race2 = "" + player.race[0];
            }
        }
    }

    class ReplayReader
    {
        private string blizzScriptPath = "./BlizzLibs/";
        private string replayLibPYFile = "libsc2replay.py";

        private ScriptEngine engine = Python.CreateEngine();
        private ScriptScope scope = null;
        private ScriptSource source;

        private object replayinfo;
        
        public ReplayReader()
        {
            // Lib should be in SearchPath, but we need to add our own python files as well
            ICollection<string> paths = engine.GetSearchPaths();
            paths.Add(blizzScriptPath);
            engine.SetSearchPaths(paths);

            // create global scope, we only need 1 of those
            scope = engine.CreateScope();

            // load our API file we're going to communicate with
            source = engine.CreateScriptSourceFromFile(blizzScriptPath + replayLibPYFile);
            source.Execute(scope);
        }

        /// <summary>
        /// Calls the given function with arguments within the scope of the current replayinfo instance
        /// </summary>
        /// <param name="method">Name of the function</param>
        /// <param name="arguments">1 or more arguments passed to the function</param>
        /// <returns>dynamic, depends on function</returns>
        private dynamic CallFunction(string method, params dynamic[] arguments)
        {
            return engine.Operations.InvokeMember(replayinfo, method, arguments);
        }

        /// <summary>
        /// Given a UTC UNIX TimeStamp and Timezone information in Hours, calculate the DateTime in given timezone
        /// </summary>
        /// <param name="unixTimeStamp">UTC Unix Timestamp (in seconds from epoch 1970-1-1)</param>
        /// <param name="tzdiff">Hours</param>
        /// <returns>DateTime</returns>
        private static DateTime UnixTimeStampToDateTime(UInt64 unixTimeStamp, int tzdiff)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            dtDateTime = dtDateTime.AddHours(tzdiff);
            return dtDateTime;
        }

        /// <summary>
        /// Load Replay information from a given replay file
        /// </summary>
        /// <param name="replaypath">full path without filename</param>
        /// <param name="filename">filename without path</param>
        /// <param name="dt">datetime the file was created</param>
        /// <returns>Replay, throws exceptions if something went wrong</returns>
        public Replay ParseReplay(string replaypath, string filename, DateTime dt)
        {
            var Replay = new Replay(filename, dt, replaypath);

            replayinfo = engine.Operations.Invoke(scope.GetVariable("ReplayInfo"));

            // loadreplay() returns 0 if succeeded, buildnumber if said replay build couldn't be parsed by the python library
            int i = CallFunction("loadreplay", replaypath + filename);
            if (i == 0)
            {
                // use replay's Timestamp and Timezone information instead of relying on local (in case replay is from a pc in a different timezone)
                Replay.SetReplayDate(UnixTimeStampToDateTime((UInt64)CallFunction("getTS"), (int)CallFunction("getTZ")));

                // getMap() returns mapname
                var map = PythonHelper.conv2string(CallFunction("getMap"));
                Replay.SetMap(map);

                // getPlayers() returns a List of playerinfo dictionaries
                IronPython.Runtime.List players = CallFunction("getPlayers");
                foreach (var player in players)
                {
                    Replay.AddPlayer(new ReplayPlayer((PythonDictionary)player));
                }
            }
            else
            {
                throw new Exception("Unsupported base build: " + i.ToString());
            }

            return Replay;
        }
    }
}
