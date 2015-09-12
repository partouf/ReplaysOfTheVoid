namespace ReplaysOfTheVoid
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using IronPython.Runtime;

    public class ReplayPlayer
    {
        public ReplayPlayer(PythonDictionary info)
        {
            var temp = PythonHelpers.Conv2String(info["m_name"]);
            var arr = Regex.Split(temp, "<sp/>");
            if (arr.Length == 2)
            {
                ClanTag = arr[0];
                Name = arr[1];
            }
            else
            {
                Name = temp;
            }

            TeamId = (int)info["m_teamId"];
            Race = PythonHelpers.Conv2String(info["m_race"]);
        }

        public string ClanTag
        {
            get;
            set;
        }

        public string Name
        {
            get;
            set;
        }

        public int TeamId
        {
            get;
            set;
        }

        public string Race
        {
            get;
            set;
        }

        public override string ToString()
        {
            return TeamId.ToString() + ". " + Name + " (" + Race + ")";
        }
    }
}
