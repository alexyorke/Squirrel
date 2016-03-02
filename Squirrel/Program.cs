using System;
using System.Threading;
using System.Drawing;
using PlayerIOClient;

namespace Decagon.EE
{
	using System.Collections.Generic;
	using System.Diagnostics;
	class Program
	{
		/// <summary>
		/// The global connection
		/// </summary>
		static Connection globalConn = null;

		/// <summary>
		/// The world identifier
		/// </summary>
		static string worldID = "PWnwS8Rypgb0I";

		static bool LOAD_FROM_BIGDB = true;
        private static ManualResetEvent generating_minimap = new ManualResetEvent(false);

        static void Main(string[] args)
		{
			// Measure variables
			DateTime stamp_1, stamp_2;

			// Log on
			Client cli = PlayerIO.QuickConnect.SimpleConnect("everybody-edits-su9rn58o40itdbnw69plyw", Config.Email, Config.Password, null);
			//Console.Write("Connected, enter a worldID: ");
			//worldID = Console.ReadLine();
			stamp_1 = DateTime.Now;

			if (LOAD_FROM_BIGDB) {
				DatabaseObject obj = cli.BigDB.Load("Worlds", worldID);
				if (obj.ExistsInDatabase)
					FromDatabaseObject(obj);
				else
					Console.WriteLine("Error: Unknown WorldID");
			} else {
				cli.Multiplayer.JoinRoom(worldID, null, delegate(Connection connection) {
					connection.OnMessage += Connection_OnMessage;
					globalConn = connection;
					connection.Send("init");
				});
			}

            generating_minimap.WaitOne(); // wait until minimap generation is finished

			stamp_2 = DateTime.Now;

			Console.WriteLine("Total time: " + (stamp_2 - stamp_1).TotalMilliseconds + " ms");
			Console.WriteLine("Press any key to exit.");
			Console.ReadKey(false);
		}

		/// <summary>
		/// Extracts the world from the BigDB database.
		/// </summary>
		/// <param name="obj">The object.</param>
		public static void FromDatabaseObject(DatabaseObject obj)
		{
			int width = obj.GetInt("width", 200);
			int height = obj.GetInt("height", 200);
			if (!obj.Contains("worlddata")) {
				Console.WriteLine("Error: No world data available");
				return;
			}

			UnserializeFromComplexObject(obj, width, height);
		}

		/// <summary>
		/// Unserializes the BigDB database world object.
		/// </summary>
		/// <param name="worlddata">The world data.</param>
		/// <param name="width">The width of the world.</param>
		/// <param name="height">The height of the world.</param>
		public static void UnserializeFromComplexObject(DatabaseObject input, int width, int height)
		{
			Minimap minimap = new Minimap();
			minimap.width = width;
			minimap.height = height;
			minimap.initialize();

			Console.WriteLine("Unserializing complex object...");

            if (input.Contains("worlddata"))
            {
                foreach (DatabaseObject ct in input.GetArray("worlddata"))
                {
                    if (ct.Count == 0) continue;
                    uint blockId = ct.GetUInt("type");
                    int layer = ct.GetInt("layer");

                    byte[] x = ct.GetBytes("x", new byte[0]), y = ct.GetBytes("y", new byte[0]),
                           x1 = ct.GetBytes("x1", new byte[0]), y1 = ct.GetBytes("y1", new byte[0]);
                    
                    for (int j = 0; j < x1.Length; j++)
                    {
                        byte nx = x1[j];
                        byte ny = y1[j];

                        minimap.drawBlock(layer, nx, ny, blockId);
                    }
                    for (int k = 0; k < x.Length; k += 2)
                    {
                        uint nx2 = (uint)(((int)x[k] << 8) + (int)x[k + 1]);
                        uint ny2 = (uint)(((int)y[k] << 8) + (int)y[k + 1]);

                        minimap.drawBlock(layer, (int)nx2, (int)ny2, blockId);
                    }
                }
            }
            else if (input.Contains("world"))
            {

            }



			// Write them "on top" of backgrounds
			minimap.rewriteForegroundBlocks();

			minimap.Save(worldID + "_bigdb.png");
            generating_minimap.Set();
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

			Console.WriteLine("Inited");
			globalConn.Disconnect();

			Minimap minimap = new Minimap();
			minimap.width = m.GetInt(15);
			minimap.height = m.GetInt(16);

			minimap.initialize();

			Console.WriteLine("Parsing init data...");

			uint p = 22;
			while (m[p] as string != "ws") p++;

			p++;
			// Parse world data
			while (p < m.Count) {
				uint blockId = m.GetUInt(p);
				int layer = m.GetInt(p + 1);
				byte[] xs = m.GetByteArray(p + 2),
					ys = m.GetByteArray(p + 3);

				for (var b = 0; b < xs.Length; b += 2) {
					int nx = (xs[b] << 8) | xs[b + 1],
						ny = (ys[b] << 8) | ys[b + 1];

					minimap.drawBlock(layer, nx, ny, blockId);
				}

				p += 4;

				if (m[p] as string == "we")
					break;

				while (p + 3 < m.Count) {
					if (m[p + 2] is byte[])
						break;
					p++;
				}
			}

			minimap.rewriteForegroundBlocks();
			minimap.Save(worldID + ".png");
            generating_minimap.Set();
		}

        private class Config
        {
            public static string Email { get; internal set; } = "guest";
            public static string Password { get; internal set; } = "guest";
        }
    }
}
