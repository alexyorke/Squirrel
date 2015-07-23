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
		public static Dictionary<string, Color> LoadBlocks()
		{
			// if the acorn file does not exist...
			string text = null;
			using (var wc = new System.Net.WebClient())
				text = wc.DownloadString("https://raw.githubusercontent.com/Tunous/EEBlocks/master/Colors.txt");

			if (text == null)
				throw new InvalidDataException("The blocks did not download correctly.");

			if (!text.StartsWith("ID: 0 Mapcolor: "))
				throw new InvalidDataException("The blocks are not in the correct format.");

			text = text.Replace('\r', ' ');

			Dictionary<string, Color> blockDict = new Dictionary<string, Color>();

			string[] lines = text.Split('\n');
			for (int i = 0; i < lines.Length; i++) {
				if (lines[i].Length < 15)
					continue;

				string[] line = lines[i].Split(' ');
				if (line.Length < 4 || line[0] != "ID:" || line[2] != "Mapcolor:")
					throw new InvalidDataException("Incorrect block color format.");

				uint u32color = Convert.ToUInt32(line[3]);
				if (u32color == 0)
					continue;

				blockDict.Add(line[1], UIntToColor(u32color));
			}

			return blockDict;
		}

		/// <summary>
		/// Converts a UInt to a Color object.
		/// </summary>
		/// <param name="color">The color.</param>
		/// <returns>Color object</returns>
		public static Color UIntToColor(uint color, bool remove_alpha = true)
		{
			int a = (byte)(color >> 24),
				r = (byte)(color >> 16),
				g = (byte)(color >> 8),
				b = (byte)(color >> 0);

			if (remove_alpha)
				a = a > 128 ? 0xFF : 0;
			return Color.FromArgb(a, r, g, b);
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