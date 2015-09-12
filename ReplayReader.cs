namespace ReplaysOfTheVoid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using IronPython.Hosting;
    using IronPython.Runtime;
    using Microsoft.Scripting;
    using Microsoft.Scripting.Hosting;

    public class ReplayReader
    {
        private string m_blizzScriptPath = "./BlizzLibs/";
        private string m_replayLibPyFile = "libsc2replay.py";

        private ScriptEngine m_engine = Python.CreateEngine();
        private ScriptScope m_scope = null;
        private ScriptSource m_source;

        public ReplayReader()
        {
            // Lib should be in SearchPath, but we need to add our own python files as well
            ICollection<string> paths = m_engine.GetSearchPaths();
            paths.Add(m_blizzScriptPath);
            m_engine.SetSearchPaths(paths);

            // create global scope, we only need 1 of those
            m_scope = m_engine.CreateScope();

            // load our API file we're going to communicate with
            m_source = m_engine.CreateScriptSourceFromFile(m_blizzScriptPath + m_replayLibPyFile);
            m_source.Execute(m_scope);
        }

        /// <summary>
        /// Load Replay information from a given replay file
        /// </summary>
        /// <param name="replaypath">full path without filename</param>
        /// <param name="filename">filename without path</param>
        /// <param name="dt">datetime the file was created</param>
        /// <returns>Replay, throws exceptions if something went wrong</returns>
        public Replay ParseReplay(string replaypath, string filename, DateTime dt, string renameformat)
        {
            object replayinfo;

            replayinfo = m_engine.Operations.Invoke(m_scope.GetVariable("ReplayInfo"));

            // loadreplay() returns 0 if succeeded, buildnumber if said replay build couldn't be parsed by the python library
            int i = this.CallFunction(replayinfo, "loadreplay", replaypath + filename);
            if (i == 0)
            {
                var replay = new Replay(filename, dt, replaypath, renameformat);

                // use replay's Timestamp and Timezone information instead of relying on local
                //  (in case replay is from a pc in a different timezone)
                replay.SetReplayDate(
                    UnixTimeStampToDateTime(
                        (ulong)this.CallFunction(replayinfo, "getTS"),
                        (int)this.CallFunction(replayinfo, "getTZ")
                    )
                );

                // getMap() returns mapname
                var map = PythonHelpers.Conv2String(this.CallFunction(replayinfo, "getMap"));
                replay.SetMap(map);

                // getPlayers() returns a List of playerinfo dictionaries
                IronPython.Runtime.List players = this.CallFunction(replayinfo, "getPlayers");
                foreach (var player in players)
                {
                    replay.AddPlayer(new ReplayPlayer((PythonDictionary)player));
                }

                replay.DoRename = replay.Filename != replay.RenameTo;

                return replay;
            }
            else
            {
                throw new Exception("Unsupported base build: " + i.ToString());
            }
        }

        /// <summary>
        /// Given a UTC UNIX TimeStamp and Timezone information in Hours, calculate the DateTime in given timezone
        /// </summary>
        /// <param name="unixTimeStamp">UTC Unix Timestamp (in seconds from epoch 1970-1-1)</param>
        /// <param name="tzdiff">Hours</param>
        /// <returns>DateTime</returns>
        private static DateTime UnixTimeStampToDateTime(ulong unixTimeStamp, int tzdiff)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Local);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            dtDateTime = dtDateTime.AddHours(tzdiff);
            return dtDateTime;
        }

        /// <summary>
        /// Calls the given function with arguments within the scope of the current replayinfo instance
        /// </summary>
        /// <param name="method">Name of the function</param>
        /// <param name="arguments">1 or more arguments passed to the function</param>
        /// <returns>dynamic, depends on function</returns>
        private dynamic CallFunction(object replayinfo, string method, params dynamic[] arguments)
        {
            return m_engine.Operations.InvokeMember(replayinfo, method, arguments);
        }
    }
}
