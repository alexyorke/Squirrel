using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Decagon.EE
{
    public class Acorn
    {

        public static Dictionary<string,Color> LoadBlocks()
        {
            // if the acorn file does not exist...
            string text = null;
            using (var wc = new System.Net.WebClient())
                text = wc.DownloadString("https://raw.githubusercontent.com/Tunous/EEBlocks/master/Colors.txt");

            if (text == null)
            {
                throw new InvalidDataException("The blocks did not download correctly.");
            }

            if (!text.Contains("ID: ") || !text.Contains("Mapcolor:"))
            {
                throw new InvalidDataException("The blocks are not in the correct format.");
            }

            text = text.Replace("ID: ", "").Replace(" Mapcolor: ", ",");

            var blocks = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);

            var blockIDs = new List<string>();
            var blockColors = new List<Color>();
            foreach (var block in blocks)
            {
                string[] tokens = block.Split(new string[] { "," }, StringSplitOptions.None);
                blockIDs.Add(tokens[0]);
                blockColors.Add(UIntToColor(Convert.ToUInt32(tokens[1])));
            }

            Dictionary<string,Color> blockDict = blockIDs.Zip(blockColors, (s, i) => new { s, i })
                          .ToDictionary(item => item.s, item => item.i);

            System.IO.StreamWriter file = new System.IO.StreamWriter("blocks.acorn");
            file.Write(Newtonsoft.Json.JsonConvert.SerializeObject(blockDict));
            file.Close();

            return blockDict;
        }

        public static Color UIntToColor(uint color)
        {
            byte a = (byte)(color >> 24);
            byte r = (byte)(color >> 16);
            byte g = (byte)(color >> 8);
            byte b = (byte)(color >> 0);
            return Color.FromArgb(a, r, g, b);
        }
    }
}