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
        static Stopwatch stopwatch = new Stopwatch();
        static Stopwatch wasted_seconds = new Stopwatch();
        static Connection globalConn = null;

        public static Dictionary<string, Color> blockDict = new Dictionary<string, Color>();
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

                    Color c = new Color();

                    blockDict.TryGetValue(Convert.ToString(type), out c);

                    if (layerNum == 0)
                    {
                        // background block
                        rewrittenBlocks.Add(new Tuple<int, int, Color>(nx, ny, Color.FromArgb(255, c.R, c.G, c.B)));
                    }

                    fp.SetPixel(Convert.ToInt32(nx), Convert.ToInt32(ny), Color.FromArgb(255, c.R, c.G, c.B));
                }
            }

            // have to rewrite foreground blocks because they may have been written before
            // the background blocks were.


            wasted_seconds.Start();
            foreach (var element in rewrittenBlocks)
            {
                fp.SetPixel(Convert.ToInt32(element.Item1), Convert.ToInt32(element.Item2), element.Item3);
            }

            wasted_seconds.Stop();

            fp.Unlock(true);

            bmp.Save(worldID + "_bigdb.png");
            Console.WriteLine("Saved image");
        }

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

        static void Connection_OnMessage(object sender, Message e)
        {
            switch (e.Type)
            {
                case "init":
                    globalConn.Disconnect(); // already have init data; don't need to be connected

                    Bitmap bmp;
                    FastPixel fp;
                    InitializeBitmapWithColor(e.GetInt(15), e.GetInt(16), Color.Black, out bmp, out fp);

                    var chunks = InitParse.Parse(e);
                    foreach (var chunk in chunks)
                    {
                        foreach (var pos in chunk.Locations)
                        {
                            Color c;

                            blockDict.TryGetValue(Convert.ToString(chunk.Type), out c);
                            fp.SetPixel(pos.X, pos.Y, Color.FromArgb(255, c.R, c.G, c.B));
                        }
                    }

                    fp.Unlock(true);
                    bmp.Save(worldID + ".png");
                    pngCompressor();

                    stopwatch.Stop();

                    Console.WriteLine("Elapsed: " + stopwatch.ElapsedMilliseconds + "ms");
                    Console.WriteLine("Wasted time: " + wasted_seconds.ElapsedMilliseconds + "ms");
                    break;
            }
        }

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
