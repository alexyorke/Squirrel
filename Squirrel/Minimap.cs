using Decagon.EE;
using System;
using System.Drawing;
using System.Collections.Generic;
using nQuant;

public class Minimap
{
	private Bitmap bmp;
	public FastPixel stage;

	public int height { get; internal set; }
	public int width { get; internal set; }
	public byte[,][] foreground_cache;
    private WuQuantizer quantizer = new WuQuantizer();
    public Minimap()
	{

	}

	public void initialize()
	{
        foreground_cache = new byte[width, height][];
        bmp = new Bitmap(width, height);
        Graphics gr = Graphics.FromImage(bmp);
		gr.Clear(Color.Black); // Set the empty color: black
		gr.DrawImage(bmp, new Rectangle(0, 0, width, height));

		stage = new FastPixel(bmp);
		stage.Lock();
	}

	public void drawBlock(int layer, int x, int y, uint blockId)
	{
		byte[] c;
		if (!Program.blockDict.TryGetValue(blockId, out c)) {
			// Unknown blockId: skip
			return;
		}
        /*if (c.R > 200 && c.G > 200 && c.B > 200) {
			Console.WriteLine("B: " + line[1] + "\t C: " + c.A + "," + c.R + "," + c.G + "," + c.B);
			System.Threading.Thread.Sleep(200);
		}*/

        if (layer == 1)
            // Write backgrounds directly
            stage.SetPixel(x, y, c);
        else
            // Cache foregrounds
            foreground_cache[x, y] = new byte[] { 0xFF, c[2], c[1], c[0] };
	}

	public void Save(string v, bool shouldCompress = false)
	{
		stage.Unlock(true);
        if (shouldCompress)
        {
            Image quantized = quantizer.QuantizeImage(bmp, 128, 0);
            quantized.Save(v, System.Drawing.Imaging.ImageFormat.Png);
        } else
        {
            bmp.Save(v, System.Drawing.Imaging.ImageFormat.Png);
        }
	}

	public void rewriteForegroundBlocks()
	{
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++) {
				if (foreground_cache[x, y] == null)
					continue;

				stage.SetPixel(x, y, foreground_cache[x, y]);
			}
	}
}