namespace Engine
{
	public class Block
	{
		public Block(double width, double x = 0, bool isMovingRight = true)
		{
			X = x;
			this.Width = width;
			this.IsMovingRight = isMovingRight;
		}

		public bool IsMovingRight { get; private set; }
		public double Width { get; internal set; }
		public double X { get; internal set; }
		public double Right => Width + X;

		public void Move(double speed)
		{
			if (IsMovingRight && Right + speed > Field.Width)
			{
				IsMovingRight = false;
			}

			if (!IsMovingRight && X - speed < 0)
			{
				IsMovingRight = true;
			}

			X = IsMovingRight ? X + speed : X - speed;
		}
	}
}
