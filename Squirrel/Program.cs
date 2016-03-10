using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayerIOClient;

namespace Decagon.EE
{
    internal class Program
    {
        /// <summary>
        ///     The world identifier
        /// </summary>

        public static readonly Dictionary<uint, byte[]> BlockDict = Acorn.LoadBlocks();

        private static void Main(string[] args)
        {
            // Log on
            var cli = PlayerIO.QuickConnect.SimpleConnect("everybody-edits-su9rn58o40itdbnw69plyw", "guest",
                "guest", null);
            args = new[] {"zzxzxz", "PWlpzISupXb0I", "849$*&$*"};

            // filter keys

            var obj = cli.BigDB.LoadKeys("Worlds", args);
            Parallel.ForEach(obj, world =>
            {
                try
                {
                    if (world.ExistsInDatabase)
                        FromDatabaseObject(world, world.Key);
                    else
                        Console.WriteLine("Error: Unknown WorldID");
                }
                catch (NullReferenceException)
                {
                    Console.WriteLine("Skipping " + world.ToString() + "...");
                }
            });
        }

        /// <summary>
        ///     Extracts the world from the BigDB database.
        /// </summary>
        /// <param name="obj">The object.</param>
        public static void FromDatabaseObject(DatabaseObject obj, string worldID)
        {
            var width = obj.GetInt("width", 200);
            var height = obj.GetInt("height", 200);
            if (!obj.Contains("worlddata"))
            {
                Console.WriteLine("Error: No world data available");
                return;
            }

            UnserializeFromComplexObject(obj, width, height, worldID);
        }

        /// <summary>
        ///     Unserializes the BigDB database world object.
        /// </summary>
        /// <param name="worlddata">The world data.</param>
        /// <param name="input"></param>
        /// <param name="width">The width of the world.</param>
        /// <param name="height">The height of the world.</param>
        private static void UnserializeFromComplexObject(DatabaseObject input, int width, int height, string worldID)
        {
            var minimap = new Minimap
            {
                width = width,
                height = height
            };
            minimap.initialize();

            if (input.Contains("worlddata"))
            {
                foreach (DatabaseObject ct in input.GetArray("worlddata").Reverse())
                {
                    if (ct.Count == 0) continue;
                    var blockId = ct.GetUInt("type");

                    byte[] x = ct.GetBytes("x", new byte[0]),
                        y = ct.GetBytes("y", new byte[0]),
                        x1 = ct.GetBytes("x1", new byte[0]),
                        y1 = ct.GetBytes("y1", new byte[0]);

                    for (var j = 0; j < x1.Length; j++)
                    {
                        var nx = x1[j];
                        var ny = y1[j];

                        minimap.drawBlock(nx, ny, blockId);
                    }
                    for (var k = 0; k < x.Length; k += 2)
                    {
                        var nx2 = (uint) ((x[k] << 8) + x[k + 1]);
                        var ny2 = (uint) ((y[k] << 8) + y[k + 1]);

                        minimap.drawBlock((int) nx2, (int) ny2, blockId);
                    }
                }
            }
            else if (input.Contains("world"))
            {
            }

            minimap.Save(worldID + ".png");
        }
    }
}