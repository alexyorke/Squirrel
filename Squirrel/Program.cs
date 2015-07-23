using PlayerIOClient;
using System.Threading;
using System.Drawing;

namespace Decagon.EE
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Drawing.Imaging;
	class Program
	{
		/// <summary>
		/// The wasted_seconds
		/// </summary>
		static Stopwatch wasted_seconds = new Stopwatch();
		/// <summary>
		/// The global connection
		/// </summary>
		static Connection globalConn = null;

		/// <summary>
		/// The world identifier
		/// </summary>
		static string worldID = "PWL2NjNOdhbEI";

		static bool LOAD_FROM_BIGDB = true;
		static bool generating_minimap;

		static void Main(string[] args)
		{
			wasted_seconds.Start();
			// Log on
			Client conn = PlayerIO.QuickConnect.SimpleConnect("everybody-edits-su9rn58o40itdbnw69plyw", Config.Email, Config.Password);

			Console.WriteLine("Connected");
			generating_minimap = true;
			if (LOAD_FROM_BIGDB) {
				DatabaseObject obj = conn.BigDB.Load("Worlds", worldID);
				if (obj.ExistsInDatabase)
					FromDatabaseObject(obj);
				else
					Console.WriteLine("Error: Unknown WorldID");
			} else {
				conn.Multiplayer.JoinRoom(worldID, null, delegate(Connection connection) {
					connection.OnMessage += Connection_OnMessage;
					globalConn = connection;
					connection.Send("init");
				});
			}

			while(generating_minimap)
				Thread.Sleep(10);

			wasted_seconds.Stop();

			Console.WriteLine("Generated minimap in " + wasted_seconds.Elapsed.Milliseconds + " ms");
			Console.ReadKey(false);
		}

		/// <summary>
		/// Extracts the world from the BigDB database.
		/// </summary>
		/// <param name="obj">The object.</param>
		public static void FromDatabaseObject(DatabaseObject obj)
		{
			var width = obj.GetInt("width", 200);
			var height = obj.GetInt("height", 200);
			if (!obj.Contains("worlddata"))
				Console.WriteLine("Error: No world data available");

			UnserializeFromComplexObject(obj.GetArray("worlddata"), width, height);
		}

		/// <summary>
		/// Unserializes the BigDB database world object.
		/// </summary>
		/// <param name="worlddata">The world data.</param>
		/// <param name="width">The width of the world.</param>
		/// <param name="height">The height of the world.</param>
		public static void UnserializeFromComplexObject(DatabaseArray worlddata, int width, int height)
		{
			Minimap minimap = new Minimap();
			minimap.width = width;
			minimap.height = height;
			minimap.initialize();

			Console.WriteLine("Unserializing complex object...");

			Color[,] foreground = new Color[width, height];
			foreach (DatabaseObject ct in worlddata) {
				if (ct.Count == 0) continue;
				uint blockId = ct.GetUInt("type");
				int layer = ct.GetInt("layer");
				byte[] xs = ct.GetBytes("x");
				byte[] ys = ct.GetBytes("y");

				for (var b = 0; b < xs.Length; b += 2) {
					int nx = (xs[b] << 8) | xs[b + 1];
					int ny = (ys[b] << 8) | ys[b + 1];

					minimap.drawBlock(layer, nx, ny, blockId);
				}
			}

			// have to rewrite foreground blocks because they may have been written before
			// the background blocks were.

			minimap.rewriteForegroundBlocks();

			minimap.Save(worldID + "_bigdb.png");
			generating_minimap = false;
		}

		/// <summary>
		/// Handles all incoming PlayerIO messages
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="m">The message.</param>
		static void Connection_OnMessage(object sender, Message m)
		{
			if (m.Type != "init")
				return;

			globalConn.Disconnect();

			Minimap minimap = new Minimap();
			minimap.width = m.GetInt(15);
			minimap.height = m.GetInt(16);

			minimap.initialize();

			uint p = 18;
			while (m[p] as string != "ws") p++;

			// Parse world data
			while (p < m.Count) {
				uint blockId = m.GetUInt(p);
				int layer = m.GetInt(p + 1);
				byte[] ys = m.GetByteArray(p + 2);
				byte[] xs = m.GetByteArray(p + 3);

				for (var b = 0; b < xs.Length; b += 2) {
					int nx = (xs[b] << 8) | xs[b + 1];
					int ny = (ys[b] << 8) | ys[b + 1];

					minimap.drawBlock(layer, nx, ny, blockId);
				}

				p += 4;

				if (m[p] as string == "we")
					break;

				while (p < m.Count) {
					if (m[p + 2] is byte[])
						break;
					p++;
				}
			}

			minimap.rewriteForegroundBlocks();
			minimap.Save(worldID + ".png");
			generating_minimap = false;
		}
	}
}
