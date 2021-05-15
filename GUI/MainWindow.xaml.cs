using Engine;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace GUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Game.Initialize();
            this.Width = Field.Width;
            EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyDownEvent,
                new KeyEventHandler(Window_KeyPressed), true);
            MainView.PaintSurface += this.MainView_PaintSurface;
            this.Loaded += this.MainWindow_Loaded;
        }

        private bool shouldSave = false;
        private bool picSaved = false;
        private bool isKeyDown = false;
        private const int blockHeight = 40;
        private const uint darkGray = 0xFF272836;

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await Start();
        }

        private async void Window_KeyPressed(object sender, KeyEventArgs e)
        {
            if (Game.Lost)
            {
                switch (e.Key)
                {
                    case Key.R:
                        Game.Initialize();
                        picSaved = false;
                        await Start();
                        break;
                    case Key.S when !picSaved:
                        shouldSave = true;
                        this.MainView.InvalidateVisual();
                        break;
                    default: 
                        break;
                }

                return;
            }

            if (e.Key == Key.Space)
                isKeyDown = true;
        }

        private async Task Start()
        {
            await GameCycle();
            // leaves cycle if lost
            if (Game.Field.Levels.Count > Properties.Settings.Default.Highscore)
            {
                Properties.Settings.Default.Highscore = Game.Field.Levels.Count;
                Properties.Settings.Default.Save();
            }
        }

        private async Task GameCycle()
        {
            while (true)
            {
                if (Game.Lost)
                {
                    return;
                }

                var task = new Task(() =>
                {
                    Game.Tick(isKeyDown);
                    isKeyDown = false;
                });

                task.Start();
                await task;
                this.MainView.InvalidateVisual();
                this.Title = $"{Game.Field.Levels.Count} / {Properties.Settings.Default.Highscore}";
                await Task.Delay(10);
            }
        }

        private void MainView_PaintSurface(object sender, SkiaSharp.Views.Desktop.SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.White);

            canvas.DrawRect(0, 0, Field.Width, (float) (this.Top + this.Height),
                new SKPaint {Color = new SKColor(darkGray)});
            for (int i = Game.Field.Levels.Count - 1; i >= 0; --i)
            {
                var block = Game.Field.Levels[i];

                int indexFromTop = Game.Field.Levels.Count - i - 1;
                var levelRect = new SKRect(
                    (float) block.X,
                    (blockHeight * 1) + blockHeight * indexFromTop,
                    (float) block.Right,
                    (blockHeight * 1) + blockHeight + blockHeight * indexFromTop);

                var levelPaint = new SKPaint {Color = GetColor(i)};
                canvas.DrawRect(levelRect, levelPaint);
            }

            // save pic
            if (shouldSave)
            {
                shouldSave = false;
                picSaved = true;
                using var img = CreateSnapshot();
                using var data = img.Encode();
                using var fs = File.Create(DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".png");
                var bytes = data.ToArray();
                fs.Write(bytes, 0, bytes.Length);
            }

            if (Game.Lost)
            {
                canvas.DrawRect(0, 0, (float) MainView.ActualWidth, (float) MainView.ActualHeight,
                    new SKPaint {Color = new SKColor(0x88FFFFFF)});
                string text = "You lost. R - restart" + (picSaved ? ". Pic saved." : ", S - save pic");
                canvas.DrawText(text, (float) (MainView.ActualWidth / 2), (float) (MainView.ActualHeight / 2),
                    new SKPaint {Color = SKColors.Black, TextAlign = SKTextAlign.Center, TextSize = 20});
                return;
            }

            var floatingRect = new SKRect(
                (float) Game.Field.Floating.X,
                0,
                (float) Game.Field.Floating.X + (float) Game.Field.Floating.Width,
                blockHeight);
            // var floatingPaint = new SKPaint {Color = GenerateColor(Game.Field.Floating)};
            var floatingPaint = new SKPaint {Color = GetColor(Game.Field.Levels.Count)};
            canvas.DrawRect(floatingRect, floatingPaint);
        }

        private static SKColor GetColor(int level)
        {
            const int allColorsCount =
                255 + // red #FF0000 -> yellow #FFFF00
                255 + // yellow #FFFF00 -> green #00FF00
                255 + // green #00FF00 -> cyan #00FFFF
                255 + // cyan #00FFFF -> dark blue #0000FF
                255 + // dark blue #0000FF -> purple #FF00FF
                255;  // purple #FF00FF -> red #FF0000

            const int colorsPerLevel = 51;
            const int fullCycle = allColorsCount / colorsPerLevel;

            int currentColor = level;
            while (currentColor > fullCycle)
            {
                currentColor -= fullCycle;
            }

            int red = 255;
            int green = 0;
            int blue = 0;

            for (int i = 0; i < currentColor; i++)
            {
                if (red == 255 && green != 255 && blue == 0) // red -> yellow
                {
                    green += colorsPerLevel;
                    if (green > 255)
                        green = 255;
                }
                else if (red != 0 && green == 255 && blue == 0) // yellow -> green
                {
                    red -= colorsPerLevel;
                    if (red < 0)
                        red = 0;
                }
                else if (red == 0 && green == 255 && blue != 255) // green -> cyan
                {
                    blue += colorsPerLevel;
                    if (blue > 255)
                        blue = 255;
                }
                else if (red == 0 && green != 0 && blue == 255) // cyan -> blue
                {
                    green -= colorsPerLevel;
                    if (green < 0)
                        green = 0;
                }
                else if (red != 255 && green == 0 && blue == 255) // blue -> purple
                {
                    red += colorsPerLevel;
                    if (red > 255)
                        red = 255;
                }
                else if (red == 255 && green == 0 && blue != 0) // purple -> red
                {
                    blue -= colorsPerLevel;
                    if (blue < 0)
                        blue = 0;
                }
            }

            return new SKColor(Convert.ToByte(red), Convert.ToByte(green), Convert.ToByte(blue), 128);
        }

        private static SKImage CreateSnapshot()
        {
            var sKSurface = SKSurface.Create(new SKImageInfo(Field.Width, blockHeight * Game.Field.Levels.Count));
            var canvas = sKSurface.Canvas;
            canvas.Clear(new SKColor(darkGray));

            canvas.DrawRect(0, 0, Field.Width, blockHeight * Game.Field.Levels.Count,
                new SKPaint {Color = new SKColor(darkGray)});
            for (int i = Game.Field.Levels.Count - 1; i >= 0; --i)
            {
                var block = Game.Field.Levels[i];

                int indexFromTop = Game.Field.Levels.Count - i - 1;
                var levelRect = new SKRect(
                    (float) block.X,
                    blockHeight * indexFromTop,
                    (float) block.Right,
                    blockHeight + blockHeight * indexFromTop);

                var levelPaint = new SKPaint {Color = GetColor(i)};
                canvas.DrawRect(levelRect, levelPaint);
            }

            return sKSurface.Snapshot();
        }
    }
}