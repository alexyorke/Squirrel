using Decagon.EE;
using nQuant;
using System.Drawing;

public class Minimap
{
	private Bitmap bmp;
	public FastPixel stage;

	public int height { get; internal set; }
	public int width { get; internal set; }
    private WuQuantizer quantizer = new WuQuantizer();
    public Minimap()
	{

	}

	public void initialize()
	{
        bmp = new Bitmap(width, height);
        Graphics gr = Graphics.FromImage(bmp);
		gr.Clear(Color.Black); // Set the empty color: black
		//gr.DrawImage(bmp, new Rectangle(0, 0, width, height));

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

        // Skip blank blocks
        if (!((c[0] == 0) && (c[1] == 0) && (c[2] == 0)))
            stage.SetPixel(x, y, c);
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
}