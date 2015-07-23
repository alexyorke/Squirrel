﻿using Decagon.EE;
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
	public Dictionary<string, Color> blockDict;
	public Color[,] foreground_cache;

	public Minimap()
	{

	}

	public void initialize()
	{
		blockDict = Acorn.LoadBlocks();
		foreground_cache = new Color[width, height];

		bmp = new Bitmap(width, height);
		Graphics gr = Graphics.FromImage(bmp);
		gr.Clear(Color.Black); // Set the empty color: black
		gr.DrawImage(bmp, new Rectangle(0, 0, width, height));

		stage = new FastPixel(bmp);
		stage.Lock();
	}

	public void drawBlock(int layer, int x, int y, uint blockId)
	{
		Color c;
		if (!blockDict.TryGetValue(blockId.ToString(), out c)) {
			// Unknown blockId: skip
			return;
		}

		if (layer == 1)
			// Write backgrounds directly
			stage.SetPixel(x, y, c);
		else
			// Cache foregrounds, when the alpha threshold is reached
			foreground_cache[x, y] = c;
	}

	public void Save(string v)
	{
		stage.Unlock(true);
		WuQuantizer quantizer = new WuQuantizer();
		Image quantized = quantizer.QuantizeImage(bmp, 128, 0);
		quantized.Save(v, System.Drawing.Imaging.ImageFormat.Png);
	}

	public void rewriteForegroundBlocks()
	{
		for (int y = 0; y < height; y++)
			for (int x = 0; x < width; x++) {
				if (foreground_cache[x, y] == null)
					continue;

				stage.SetPixel(x, y, foreground_cache[x, y]);
			}
		Console.WriteLine("Foreground blocks written");
	}
}