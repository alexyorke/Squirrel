using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Decagon.EE
{
	/// <summary>
	/// The acorn class.
	/// </summary>
	public class Acorn
	{
		/// <summary>
		/// Loads the blocks.
		/// </summary>
		/// <returns>Dictionary containing the blocks with their respective ids and colors.</returns>
		/// <exception cref="InvalidDataException">
		/// The blocks did not download correctly.
		/// or
		/// The blocks are not in the correct format.
		/// </exception>
		public static Dictionary<uint, byte[]> LoadBlocks()
		{
			// if the acorn file does not exist...
			string text = null;

            if (!File.Exists("Colors.txt"))
            {
                using (var wc = new System.Net.WebClient())
                    text = wc.DownloadString("https://raw.githubusercontent.com/EEJesse/EEBlocks/master/Colors.txt");
            } else
            {
                text = File.ReadAllText("Colors.txt");
            }

			if (text == null)
				throw new InvalidDataException("The blocks did not download correctly.");

			if (!text.StartsWith("ID: <block id> Mapcolor: <color id>"))
				throw new InvalidDataException("The blocks are not in the correct format.");

			text = text.Replace('\r', ' ');
            text = text.Replace("ID: <block id> Mapcolor: <color id>", "");
			Dictionary<uint, byte[]> blockDict = new Dictionary<uint, byte[]>();

			string[] lines = text.Split('\n');
			for (int i = 0; i < lines.Length; i++) {
				if (lines[i].Length < 2)
					continue;

                string[] line = lines[i].Split(' ');

                uint u32color = Convert.ToUInt32(line[1]);

                blockDict.Add(Convert.ToUInt32(line[0]), UIntToByte(u32color));
			}

			return blockDict;
		}

		/// <summary>
		/// Converts a UInt to a Color object.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>Color object</returns>
		public static byte[] UIntToByte(uint color, bool remove_alpha = true)
		{
			byte a = (byte)(color >> 24),
			r = (byte)(color >> 16),
			g = (byte)(color >> 8),
			b = (byte)(color >> 0);

            int a_old = a;
			int a_new = a_old > 128 ? 0xFF : 0;

            a = (byte)a_new;

            return new byte[] { b, g, r, a };
            
		}
	}

	/// <summary>
	/// This class only contains bytes R, G and B
	/// </summary>
	public class RawColor
	{
		public byte R, G, B;
		public RawColor(Color c)
		{
			R = c.R;
			G = c.G;
			B = c.B;
		}
		public Color ToColor()
		{
			return Color.FromArgb(0xFF, R, G, B);
		}
	}
}