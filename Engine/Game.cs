namespace Engine
{
	public static class Game
	{
		public static Field Field { get; private set; }
		public static bool DidLose {get; private set;}
		private static double _speed { get; set; }

		public static void Initialize()
		{
			Field = new Field();
			DidLose = false;
			_speed = 1.0;
		}

		public static void Tick(bool shouldPut)
		{
			if (DidLose)
				return;

			if (shouldPut)
			{
				bool didLand = Field.Put();
				if (!didLand)
				{
					DidLose = true;
					return;
				}

				_speed += 0.1;
				Field.CreateFloating();
			}

			Field.Floating.Move(_speed);
		}
	}
}
