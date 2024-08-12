namespace PC0_k_visualizer
{
    internal class SudokuCellSurface : ScreenSurface
    {
        static float redHue;
        static float greenHue;
        static float hueStep;

        static Color invalidColor = new Color(1.0f, 0.0f, 1.0f);

        static SudokuCellSurface()
        {
            var red = new Color(1.0f, 0.0f, 0.0f);
            var green = new Color(0.0f, 1.0f, 0.0f);

            redHue = red.GetHSLHue();
            greenHue = green.GetHSLHue();
            hueStep = (greenHue - redHue) / 9;
        }

        public SudokuCellSurface(int width, int height) : base(width, height)
        {
            this.DrawBox(new Rectangle(new Point(0, 0), new Point(width-1, height-1)),
                                ShapeParameters.CreateStyledBoxThin(Color.Green));
        }

        public void DrawDomain(List<int> domain)
        {
            var hue = greenHue - domain.Count * hueStep;
            var col = Color.FromHSL(hue, 1, 0.5f);

            if (domain.Count == 0)
                col = invalidColor;

            this.DrawBox(new Rectangle(new Point(0, 0), new Point(Width - 1, Height - 1)),
                                ShapeParameters.CreateStyledBoxThin(col));


            this.Clear(new Rectangle(1, 1, Width - 2, Height - 2));
            if (domain.Count != 1)
            {
                var k = 1;
                for (int j = 1; j <= 3; j++)
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        if (domain.Contains(k))
                            this.SetCellAppearance(i, j, new ColoredGlyph(Color.Yellow, Color.Transparent, k.ToString()[0]));
                        else
                            this.SetCellAppearance(i, j, new ColoredGlyph(Color.Transparent, Color.Transparent, ' '));
                        k++;
                    }
                }
            }
            else
            {
                var val = domain[0];
                this.Print(1, 1, "" + (char)((14 + 3 * (val - 1)) * 16 + 0));
                this.Print(2, 1, "" + (char)((14 + 3 * (val - 1)) * 16 + 1));
                this.Print(3, 1, "" + (char)((14 + 3 * (val - 1)) * 16 + 2));
                                                
                this.Print(1, 2, "" + (char)((15 + 3 * (val - 1)) * 16 + 0));
                this.Print(2, 2, "" + (char)((15 + 3 * (val - 1)) * 16 + 1));
                this.Print(3, 2, "" + (char)((15 + 3 * (val - 1)) * 16 + 2));
                                                
                this.Print(1, 3, "" + (char)((16 + 3 * (val - 1)) * 16 + 0));
                this.Print(2, 3, "" + (char)((16 + 3 * (val - 1)) * 16 + 1));
                this.Print(3, 3, "" + (char)((16 + 3 * (val - 1)) * 16 + 2));
            }
        }
    }
}
