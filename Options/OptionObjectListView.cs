using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BrightIdeasSoftware;
using Rhino;

namespace SolidUtils
{
    public class OptionObjectListView : OptionBaseT<ObjectListView>
    {
        private readonly string gridSubOptionName;
        private readonly Func<string, bool> canSaveOptions;

        public OptionObjectListView(ObjectListView grid, Type[] relatedTo, string gridSubOptionName = "", Func<string, bool> canSaveOptions = null)
            : base( grid.Name + gridSubOptionName, grid, grid.Name + gridSubOptionName + " options", relatedTo, OptionType.Hidden)
        {
            this.gridSubOptionName = gridSubOptionName;
            this.canSaveOptions = canSaveOptions;
        }

        public override bool Load()
        {
            var res = Settings.GetString(KeyFull, "DEF_VALUE");
            if (res != "DEF_VALUE")
            {
                FromString(res);
                return true;
            }
            return false;
        }

        public override bool  Save()
        {
            var save = true;
            if (canSaveOptions != null)            
            {
                try
                {
                    save = canSaveOptions(gridSubOptionName);
                }
                catch
                {
                    // just stop issues
                }
            }

            if (save)
            {
                var s = ToString();
                Settings.SetString(KeyFull, s);
                return true;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            var grid = Value;
            var state = grid.SaveState();

            // From byte array to string
            string res = ByteArrayToHexString(state);
            return res;
        }

        

        public void FromString(string s)
        {
            if (!String.IsNullOrEmpty(s))
            {
                var grid = Value;
                // From string to byte array
                try
                {
                    byte[] buffer = HexStringToByteArray(s);
                    grid.RestoreState(buffer);
                }
                catch
                {
                    // nothing
                }
            }
        }

        public static string ByteArrayToHexString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] HexStringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }
    }
}
