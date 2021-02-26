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
			EventManager.RegisterClassHandler(typeof(Window), Keyboard.KeyDownEvent, new KeyEventHandler(Window_KeyPressed), true);
			MainView.PaintSurface += this.MainView_PaintSurface;
			this.Loaded += this.MainWindow_Loaded;
		}

		private bool shouldSave = false;
		private bool picSaved = false;
		private bool isMouseDown = false;
		const int blockHeight = 20;

		private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
		{
			await Start();
		}

		private async void Window_KeyPressed(object sender, KeyEventArgs e)
		{
			if (Game.DidLose)
			{
				if (e.Key == Key.R)
				{
					Game.Initialize();
					picSaved = false;
					await Start();
				}
				else
				if (e.Key == Key.S && !picSaved)
				{
					shouldSave = true;
					this.MainView.InvalidateVisual();
				}

				return;
			}

			if (e.Key == Key.Space)
				isMouseDown = true;
		}

		private async Task Start()
		{
			await GameCycle();
			// lost
			Properties.Settings.Default.Highscore = Game.Field.Levels.Count;
			Properties.Settings.Default.Save();
		}

		private async Task GameCycle()
		{
			while (true)
			{
				if (Game.DidLose)
				{
					return;
				}

				Task task = new Task(() =>
				{
					Game.Tick(isMouseDown);
					isMouseDown = false;
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

			canvas.DrawRect(0, 0, Field.Width, (float)(this.Top + this.Height), new SKPaint { Color = new SKColor(0xFFF8F8F8) });
			for (int i = Game.Field.Levels.Count - 1; i >= 0; --i)
			{
				var block = Game.Field.Levels[i];

				int indexFromTop = Game.Field.Levels.Count - i - 1;
				var levelRect = new SKRect(
					(float)block.X,
					(blockHeight * 1) + blockHeight * indexFromTop,
					(float)block.Right,
					(blockHeight * 1) + blockHeight + blockHeight * indexFromTop);

				var levelPaint = new SKPaint { Color = GenerateColor(block) };
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

			if (Game.DidLose)
			{
				canvas.DrawRect(0, 0, (float)MainView.ActualWidth, (float)MainView.ActualHeight, new SKPaint() { Color = new SKColor(0x88FFFFFF) });
				string text = "You lost. R - restart" + (picSaved ? ". Pic saved." : ", S - save pic"); 
				canvas.DrawText(text, (float)(MainView.ActualWidth / 2), (float)(MainView.ActualHeight / 2),
					new SKPaint() { Color = SKColors.Black, TextAlign = SKTextAlign.Center, TextSize = 20 });
				return;
			}

			var floatingRect = new SKRect(
				(float)Game.Field.Floating.X,
				0,
				(float)Game.Field.Floating.X + (float)Game.Field.Floating.Width,
				blockHeight);
			var floatingPaint = new SKPaint { Color = GenerateColor(Game.Field.Floating) };
			canvas.DrawRect(floatingRect, floatingPaint);
		}

		private static SKColor GenerateColor(object obj)
		{
			int hashCode = obj.GetHashCode();
			long zeroBasedHash = (long)hashCode + (long)int.MaxValue;
			double ratio = zeroBasedHash / (double)((long)int.MaxValue + (long)int.MaxValue);
			int colorCode = (int)(0xFFFFFF * ratio);
			return new SKColor(0xFF000000 + (uint)colorCode);
		}

		private static SKImage CreateSnapshot()
		{
			SKSurface sKSurface = SKSurface.Create(new SKImageInfo(Field.Width, blockHeight * Game.Field.Levels.Count));
			var canvas = sKSurface.Canvas;
			canvas.Clear(SKColors.White);

			canvas.DrawRect(0, 0, Field.Width, (float)(blockHeight * Game.Field.Levels.Count), new SKPaint { Color = new SKColor(0xFFF8F8F8) });
			for (int i = Game.Field.Levels.Count - 1; i >= 0; --i)
			{
				var block = Game.Field.Levels[i];

				int indexFromTop = Game.Field.Levels.Count - i - 1;
				var levelRect = new SKRect(
					(float)block.X,
					blockHeight * indexFromTop,
					(float)block.Right,
					blockHeight + blockHeight * indexFromTop);

				var levelPaint = new SKPaint { Color = GenerateColor(block) };
				canvas.DrawRect(levelRect, levelPaint);
			}

			return sKSurface.Snapshot();
		}
	}
}
