using PlayerIOClient;
using System.Threading;
using Rabbit;
using System.Drawing;
namespace Decagon.EE
{
    using nQuant;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing.Imaging;
    class Program
    {
        /// <summary>
        /// The stopwatch
        /// </summary>
        static Stopwatch stopwatch = new Stopwatch();
        /// <summary>
        /// The wasted_seconds
        /// </summary>
        static Stopwatch wasted_seconds = new Stopwatch();
        /// <summary>
        /// The global connection
        /// </summary>
        static Connection globalConn = null;

        /// <summary>
        /// The block dictionary
        /// </summary>
        public static Dictionary<string, Color> blockDict = new Dictionary<string, Color>();
        /// <summary>
        /// The world identifier
        /// </summary>
        static string worldID = "PW5WNPqd3ia0I";

        static void Main(string[] args)
        {
            // Load the blocks into memory
            blockDict = Acorn.LoadBlocks();

            // Log on
            Client conn = new RabbitAuth().LogOn("everybody-edits-su9rn58o40itdbnw69plyw", Config.Email, Config.Password);
            while (conn.ConnectUserId == "") { Thread.Sleep(100); } // wasteful

            Console.WriteLine("Connected");

            stopwatch.Start();
            conn.Multiplayer.JoinRoom(worldID, null, delegate (Connection connection)
            {
                connection.OnMessage += Connection_OnMessage;
                globalConn = connection;
                connection.Send("init");

            });
            
            DatabaseObject obj = conn.BigDB.Load("Worlds", worldID);
            FromDatabaseObject(obj);
            Thread.Sleep(Timeout.Infinite);
        }


        /// <summary>
        /// Extracts the world from the BigDB database.
        /// </summary>
        /// <param name="obj">The object.</param>
        public static void FromDatabaseObject(DatabaseObject obj)
        {
            var width = obj.GetInt("width", 200);
            var height = obj.GetInt("height", 200);
            var worldData = obj.GetArray("worlddata");
            if (worldData != null)
            {
                UnserializeFromComplexObject(worldData,width,height);
            }
        }

        /// <summary>
        /// Unserializes the BigDB database world object.
        /// </summary>
        /// <param name="worlddata">The world data.</param>
        /// <param name="width">The width of the world.</param>
        /// <param name="height">The height of the world.</param>
        public static void UnserializeFromComplexObject(DatabaseArray worlddata,int width,int height)
        {
            Bitmap bmp;
            FastPixel fp;
            InitializeBitmapWithColor(width, height, Color.Black, out bmp, out fp, true);
            

            List<Tuple<int, int, Color>> rewrittenBlocks = new List<Tuple<int, int, Color>>();
            foreach (DatabaseObject ct in worlddata)
            {
                if (ct.Count == 0) continue;
                var type = (uint)ct.GetValue("type");
                var layerNum = ct.GetInt("layer", 0);
                var xs = ct.GetBytes("x", new byte[0]);
                var ys = ct.GetBytes("y", new byte[0]);

                for (var b = 0; b < xs.Length; b += 2)
                {
                    var nx = (xs[b] << 8) + xs[b + 1];
                    var ny = (ys[b] << 8) + ys[b + 1];

                    rewrittenBlocks.Add(drawBlock(fp, new DataChunk(layerNum, type, xs, ys, new object[0]), new Point(nx,ny)));
                }
            }

            // have to rewrite foreground blocks because they may have been written before
            // the background blocks were.

            rewriteForegroundBlocks(fp, rewrittenBlocks);

            fp.Unlock(true);

            bmp.Save(worldID + "_bigdb.png");
            Console.WriteLine("Saved image");
        }

        /// <summary>
        /// Initializes the color of a new bitmap.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="background">The background.</param>
        /// <param name="bmp">The BMP reference.</param>
        /// <param name="fp">The fast pixel reference.</param>
        /// <param name="shouldLock">if set to <c>true</c> the image should be locked for editing.</param>
        private static void InitializeBitmapWithColor(int width, int height, Color background, out Bitmap bmp, out FastPixel fp, bool shouldLock = true)
        {
            bmp = new Bitmap(width, height);
            var gr = Graphics.FromImage(bmp);
            gr.Clear(background); // empty blocks (null) are black
            gr.DrawImage(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height));

            fp = new FastPixel(bmp);

            if (shouldLock)
            {
                fp.Lock();
            }
        }

        /// <summary>
        /// Handles all incoming PlayerIO messages
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The message.</param>
        static void Connection_OnMessage(object sender, Message e)
        {
            switch (e.Type)
            {
                case "init":
                    globalConn.Disconnect(); // already have init data; don't need to be connected

                    Bitmap bmp;
                    FastPixel fp;
                    InitializeBitmapWithColor(e.GetInt(15), e.GetInt(16), Color.Black, out bmp, out fp);
                    List<Tuple<int, int, Color>> rewrittenBlocks = new List<Tuple<int, int, Color>>();

                    var chunks = InitParse.Parse(e);
                    foreach (var chunk in chunks)
                    {
                        foreach (var pos in chunk.Locations)
                        {
                            rewrittenBlocks.Add(drawBlock(fp, chunk, pos));
                        }
                    }

                    rewriteForegroundBlocks(fp, rewrittenBlocks);

                    fp.Unlock(true);
                    bmp.Save(worldID + ".png");
                    pngCompressor();

                    stopwatch.Stop();

                    Console.WriteLine("Elapsed: " + stopwatch.ElapsedMilliseconds + "ms");
                    Console.WriteLine("Wasted time: " + wasted_seconds.ElapsedMilliseconds + "ms");
                    break;
            }
        }

        private static void rewriteForegroundBlocks(FastPixel fp, List<Tuple<int, int, Color>> rewrittenBlocks)
        {
            wasted_seconds.Start();
            foreach (var element in rewrittenBlocks)
            {
                if (element != null)
                {
                    fp.SetPixel(Convert.ToInt32(element.Item1), Convert.ToInt32(element.Item2), element.Item3);
                }
            }
            wasted_seconds.Stop();
        }

        private static Tuple<int, int, Color> drawBlock(FastPixel fp, DataChunk chunk, Point pos)
        {
            Color c;
            Tuple<int, int, Color> rewrittenBlock_local = null;

            blockDict.TryGetValue(Convert.ToString(chunk.Type), out c);
            if (chunk.Layer == 1)
            {
                // background block
                rewrittenBlock_local = new Tuple<int,int,Color>(pos.X, pos.Y, Color.FromArgb(255, c.R, c.G, c.B));
            }

            fp.SetPixel(pos.X, pos.Y, Color.FromArgb(255, c.R, c.G, c.B));

            
            // workaround to get reference to a tuple
            return rewrittenBlock_local;
        }

        /// <summary>
        /// Compresses PNG files.
        /// </summary>
        private static void pngCompressor()
        {
            var quantizer = new WuQuantizer();
            using (var bitmap = new Bitmap(worldID + ".png"))
            {
                using (var quantized = quantizer.QuantizeImage(bitmap, 0, 0))
                {
                    quantized.Save(worldID + "_nquant.png", ImageFormat.Png);
                }
            }
        }
    }
}
