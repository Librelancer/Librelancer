// MIT License - Copyright (c) Callum McGing
// This file is subject to the terms and conditions defined in
// LICENSE, which is part of this source code package

using System;
using System.Collections.Generic;
namespace LibreLancer
{
	public class DebugGraph
	{
		public float X;
		public float Y;
		public float Width;
		public float Height;

		class GraphLine
		{
			public Color4 Color;
			public float ValMin;
			public float ValMax;
			public CircularBuffer<float> Points;

			public GraphLine(int points) { Points = new CircularBuffer<float>(points); }
		}

		List<GraphLine> graphLines = new List<GraphLine>();

		public int AddLine(Color4 color, int maxpoints, float min, float max)
		{
			var idx = graphLines.Count;
			graphLines.Add(new GraphLine(maxpoints) { Color = color, ValMin = min, ValMax = max });
			return idx;
		}

		public void PlotPoint(int index, float point)
		{
			graphLines[index].Points.Enqueue(point);
		}

		public void Draw(Renderer2D renderer)
		{
			var rect = new Rectangle((int)X + 2, (int)Y + 2, (int)Width, (int)Height);
			renderer.FillRectangle(rect, Color4.Black);
			rect.X -= 2; rect.Y -= 2;
			renderer.FillRectangle(rect, Color4.White);

			var off = 2;
			var heightTrue = Height - 2 * off;

			foreach (var line in graphLines)
			{
				if (line.Points.Count < 2) continue;
				var dX = Width / line.Points.Count;
				for (int i = 0; i < line.Points.Count - 1; i++)
				{
					var point0 = new Vector2(X + dX * i, Y + off + PlotY(line, i, heightTrue));
					var point1 = new Vector2(X + dX * (i + 1), Y + off + PlotY(line, i + 1, heightTrue));
					renderer.DrawLine(line.Color, point0, point1);
				}
			}
		}

		static float PlotY(GraphLine line, int pointIndex, float heightTrue)
		{
			return Utf.Ale.AlchemyEasing.Ease(Utf.Ale.EasingTypes.Linear,
									   line.Points[pointIndex],
									   line.ValMin,
									   line.ValMax,
									   0,
									   heightTrue);
		}
	}
}
