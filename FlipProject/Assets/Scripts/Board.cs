using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Board
{
	public enum CellType
	{
		EMPTY,
		MIRROR,
		GENERATOR,
		SLUICE,
		PROCESS,
		TARPIT,
		INPUT,
		OUTPUT,
		WALL,
	}

	private static int[] CellRotationCount =
	{
		/* EMPTY */     1,
		/* MIRROR */    4,
		/* GENERATOR */ 10,
		/* SLUICE */    4,
		/* PROCESS */   1,
		/* TARPIT */    2,
		/* INPUT */     1,
		/* OUTPUT */    1,
		/* WALL */      1,
	};

	public struct Cell
	{
		public CellType type;
		public int param;
	}

	public static Cell EmptyCell => new Cell { type = CellType.EMPTY, param = 0 };

	public static Cell RotateCellForward(Cell now)
	{
		return new Cell { type = now.type, param = now.param.IncrementModM(CellRotationCount[(int)now.type]) };
	}

	public static Cell RotateCellBackward(Cell now)
	{
		return new Cell { type = now.type, param = now.param.DecrementModM(CellRotationCount[(int)now.type]) };
	}

	private Cell[,] board;
	private int height, width;

	public struct Photon
	{
		public int value;
		public int positionX, positionY;
		public int velocityX, velocityY;
	}

	public List<Photon> photons { get; private set; }

	private int MinX => -height / 2;
	private int MaxX => height - 1 + MinX;
	private int MinY => -width / 2;
	private int MaxY => width - 1 + MinY;

	public Board(int height, int width)
	{
		board = new Cell[height, width];
		this.height = height;
		this.width = width;
		/*Debug.Log(string.Format("Board ctor: {0}x{1}, Range X: [{2}, {3}], Y: [{4}, {5}]",
			height, width, MinX, MaxX, MinY, MaxY));*/
		photons = new List<Photon>();
	}

	public bool IsInBound(int x, int y)
	{
		return x >= MinX && x <= MaxX && y >= MinY && y <= MaxY;
	}

	public bool IsInBound(Vector2Int pos)
	{
		return IsInBound(pos.x, pos.y);
	}

	public Cell this[int x, int y]
	{
		get
		{
			if (IsInBound(x, y))
				return board[x - MinX, y - MinY];
			else
				return EmptyCell;
		}
		set
		{
			if (IsInBound(x, y))
				board[x - MinX, y - MinY] = value;
		}
	}

	public Cell this[Vector2Int pos]
	{
		get
		{
			if (IsInBound(pos))
				return board[pos.x - MinX, pos.y - MinY];
			else
				return EmptyCell;
		}
		set
		{
			if (IsInBound(pos))
				board[pos.x - MinX, pos.y - MinY] = value;
		}
	}

	#region Board evolving functions
	public void Start()
	{
	}

	public void Step()
	{
	}

	public void Stop()
	{
	}
	#endregion
}
