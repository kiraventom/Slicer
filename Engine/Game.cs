using System.Linq;

namespace Engine
{
	public static class Game
	{
		public static Field Field { get; private set; }
		public static bool Lost {get; private set;}
		private static double _speed { get; set; }

		public static void Initialize()
		{
			Field = new Field();
			Lost = false;
			_speed = 2.0;
		}

		public static void Tick(bool shouldPut)
		{
			if (Lost)
				return;

			if (shouldPut)
			{
				double floatingX = Field.Floating.X;
				bool didLand = Field.Put();
				if (!didLand)
				{
					Lost = true;
					return;
				}

				_speed += 0.05;

				var topBlock = Field.Levels.Last();
				double x = floatingX < topBlock.X ? topBlock.X : floatingX;
				
				Field.CreateFloating(x);
			}

			Field.Floating.Move(_speed);
		}
	}
}
