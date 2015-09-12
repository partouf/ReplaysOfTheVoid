namespace ReplaysOfTheVoid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class Replay
    {
        private DateTime m_replaydate;
        private string m_renameformat;
        private string m_race1 = string.Empty;
        private string m_race2 = string.Empty;
        private string m_map = string.Empty;
        private string m_filename = string.Empty;
        private string m_directory = string.Empty;
        private DateTime m_filecreated = DateTime.MinValue;

        public Replay(string filename, DateTime datecreated, string directory, string renameformat)
        {
            this.m_renameformat = renameformat;
            this.DoRename = true;
            this.m_filecreated = datecreated;
            this.m_replaydate = datecreated;
            this.m_filename = filename;
            this.m_directory = directory;
            this.Player1 = string.Empty;
            this.Player2 = string.Empty;
            this.m_race1 = string.Empty;
            this.m_race2 = string.Empty;
        }

        public bool DoRename
        {
            get;
            set;
        }

        public DateTime ReplayDate
        {
            get
            {
                return m_replaydate;
            }
        }

        public string Race1
        {
            get
            {
                return m_race1;
            }
        }

        public string Race2
        {
            get
            {
                return m_race2;
            }
        }

        public string Map
        {
            get
            {
                return m_map;
            }
        }

        public string Player1
        {
            get;
            set;
        }

        public string Player2
        {
            get;
            set;
        }

        public string RenameTo
        {
            get
            {
                return GetAlternateRename(0);
            }
        }

        public string Filename
        {
            get
            {
                return m_filename;
            }
        }

        public string Directory
        {
            get
            {
                return m_directory;
            }
        }

        public DateTime FileCreated
        {
            get
            {
                return m_filecreated;
            }
        }

        public void SetReplayDate(DateTime dt)
        {
            this.m_replaydate = dt;
        }

        public void SetMap(string mapname)
        {
            this.m_map = mapname;
        }

        public string GetAlternateRename(int count)
        {
            string formatted = m_renameformat;

            formatted = formatted.Replace("[RACE1]", Race1);
            formatted = formatted.Replace("[RACE2]", Race2);
            formatted = formatted.Replace("[MAP]", Map);
            formatted = formatted.Replace("[PLAYER1]", Player1);
            formatted = formatted.Replace("[PLAYER2]", Player2);

            if (count == 0)
            {
                return formatted + ".SC2Replay";
            }
            else
            {
                return formatted + " (" + count.ToString() + ")" + ".SC2Replay";
            }
        }

        public void AddPlayer(ReplayPlayer player)
        {
            if (player.TeamId == 0)
            {
                if (this.Player1 != string.Empty)
                {
                    this.Player1 += " and ";
                }

                this.Player1 = this.Player1 + (player.ClanTag + " " + player.Name).Trim();
                if (player.Race != string.Empty)
                {
                    this.m_race1 = string.Empty + player.Race[0];
                }
            }
            else if (player.TeamId == 1)
            {
                if (this.Player2 != string.Empty)
                {
                    this.Player2 += " and ";
                }

                this.Player2 = this.Player2 + (player.ClanTag + " " + player.Name).Trim();
                if (player.Race != string.Empty)
                {
                    this.m_race2 = string.Empty + player.Race[0];
                }
            }
        }
    }
}
