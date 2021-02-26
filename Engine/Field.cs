using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
	public class Field
	{
		public Field()
		{
			_levels = new();
			_levels.Add(new Block(Width / 2, Width / 4));
			CreateFloating();
		}

		private List<Block> _levels { get; }
		public IList<Block> Levels => _levels.AsReadOnly();
		public Block Floating { get; private set; }
		public const int Width = 600;

		public void CreateFloating()
		{
			var topBlock = _levels.Last();
			Floating = new(topBlock.Width, Width / 4, topBlock.IsMovingRight);
		}

		public bool Put()
		{
			var topBlock = _levels.Last();
			if (Floating.X >= topBlock.Right || Floating.Right <= topBlock.X)
			{
				return false;
			}

			Floating.Width -= Math.Abs(Floating.X - topBlock.X);
			if (Floating.X < topBlock.X)
			{
				Floating.X = topBlock.X;
			}

			_levels.Add(Floating);
			Floating = null;
			return true;
		}
	}
}
