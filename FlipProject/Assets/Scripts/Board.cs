using System;
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

	public struct Cell
	{
		private static readonly int[] CellRotationCount =
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

		public CellType type;
		public int param;

		public Cell(CellType type, int param)
		{
			this.type = type;
			this.param = param;
		}

		public Cell RotateForward()
		{
			return new Cell(type, param.IncrementModM(CellRotationCount[(int)type]));
		}

		public Cell RotateBackward()
		{
			return new Cell(type, param.DecrementModM(CellRotationCount[(int)type]));
		}

		public bool IsEmpty()
		{
			return type == CellType.EMPTY;
		}

		public override string ToString()
		{
			return $"{type}/{param}";
		}

		public static Cell FromCharacter(char c)
		{
			switch (c)
			{
				case ' ': case '.': return new Cell(CellType.EMPTY, 0);
				case '-': return new Cell(CellType.MIRROR, 0);
				case '/': return new Cell(CellType.MIRROR, 1);
				case '|': return new Cell(CellType.MIRROR, 2);
				case '\\': return new Cell(CellType.MIRROR, 3);
				case Char _ when c >= '0' && c <= '9':
					return new Cell(CellType.GENERATOR, (int)(c - '0'));
				case '^': return new Cell(CellType.SLUICE, 0);
				case '<': return new Cell(CellType.SLUICE, 1);
				case 'v': return new Cell(CellType.SLUICE, 2);
				case '>': return new Cell(CellType.SLUICE, 3);
				case 'X': return new Cell(CellType.PROCESS, 0);
				case '+': return new Cell(CellType.TARPIT, 0);
				case '*': return new Cell(CellType.TARPIT, 1);
				case 'i': return new Cell(CellType.INPUT, 0);
				case 'o': return new Cell(CellType.OUTPUT, 0);
				case '#': return new Cell(CellType.WALL, 0);
				default: throw new ArgumentException($"Unrecognized cell character '{c}'.");
			}
		}
	}

	public static Cell EmptyCell => new Cell(CellType.EMPTY, 0);

	public struct Photon
	{
		public readonly int id;
		public int value;
		public int positionX, positionY;
		public int velocityX, velocityY;

		public override string ToString()
		{
			return $"Photon ID {id} value {value} at ({positionX}, {positionY}) with velocity ({velocityX}, {velocityY})";
		}
	}

	public struct ToolItem
	{
		public Cell NewCell;
		public int Count;

		public override string ToString()
		{
			return $"[{NewCell}]x{Count}";
		}
	}

	private readonly Cell[,] board;
	private readonly int height, width;

	public List<Photon> Photons { get; private set; }

	public ToolItem[] Tools { get; private set; }

	public int MinX => -height / 2;
	public int MaxX => height - 1 + MinX;
	public int MinY => -width / 2;
	public int MaxY => width - 1 + MinY;

	// Load level from level description.
	// See Resources/Level/Mockup.txt for format & syntax.
	private enum ParsingState { NONE, TOOLS, PRESET };
	public Board(string levelDescription)
	{
		Photons = new List<Photon>();

		ParsingState state = ParsingState.NONE;
		int y = 0, count = 0;
		int lineNumber = 0;

		try
		{
			foreach (string line in levelDescription.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None))
			{
				lineNumber++;
				if (line.StartsWith(";")) continue;
				if (line == "")
				{
					if (state == ParsingState.TOOLS)
						throw new ArgumentException("Not enough tools");
					if (state == ParsingState.PRESET)
						throw new ArgumentException("Preset don't have enough rows");
					continue;
				}

				string[] token = line.Split(' ');

				switch (state)
				{
					case ParsingState.NONE:
						if (token[0] == "Size")
						{
							int value = Int32.Parse(token[1]);
							board = new Cell[value, value];
							width = height = value;
							//Debug.Log($"BoardParser: Size {value}");
						}
						else if (token[0] == "Tools")
						{
							int toolCount = Int32.Parse(token[1]);
							if (toolCount < 0 || toolCount > 9)
							{
								throw new ArgumentException("ToolCount should be in [0,9].");
							}
							Tools = new ToolItem[toolCount];
							count = 0;
							state = ParsingState.TOOLS;
							//Debug.Log($"BoardParser: Tools count = {toolCount}");
						}
						else if (token[0] == "Preset")
						{
							y = height - 1;
							state = ParsingState.PRESET;
							//Debug.Log("BoardParser: Start Preset");
						}
						else
						{
							throw new ArgumentException($"Unknown line: \"{line}\"");
						}
						break;
					case ParsingState.TOOLS:
						Tools[count] = new ToolItem { Count = Int32.Parse(token[1]), NewCell = Cell.FromCharacter(token[0][0]) };
						//Debug.Log($"BoardParser: Tool #{count}: {Tools[count].ToString()}");
						count++;
						if (count == Tools.Length) state = ParsingState.NONE;
						break;
					case ParsingState.PRESET:
						string row = line.Substring(0, width);
						int x = 0;
						//Debug.Log($"BoardParser: Row #{x} (x = {x + MinX}): \"{row}\"");
						foreach (char c in row)
						{
							board[x, y] = Cell.FromCharacter(c);
							//Debug.Log($"BoardParser: Item ({x + MinX}, {y + MinY}): {board[x, y]}");
							x++;
						}
						y--;
						if (y < 0) state = ParsingState.NONE;
						break;
					default:
						break;
				}
			}
		}
		catch (Exception e)
		{
			throw new System.IO.InvalidDataException($"Parse level failed on line {lineNumber}", e);
		}

	}

	/// <summary>
	/// Take an item from Toolbox.
	/// </summary>
	/// <param name="toolIndex">The index of tool.</param>
	/// <returns>The new item.</returns>
	public Cell TakeTool(int toolIndex)
	{
		if (toolIndex >= 0 && toolIndex < Tools.Length && Tools[toolIndex].Count != 0)
		{
			Tools[toolIndex].Count--;
			return Tools[toolIndex].NewCell;
		}
		else
		{
			return Board.EmptyCell;
		}
	}

	/// <summary>
	/// Return an item to Toolbox.
	/// </summary>
	/// <param name="cell">The returned item.</param>
	/// <returns>Whether the item is successfully returned.</returns>
	public bool ReturnTool(Board.Cell cell)
	{
		for (int i = 0; i < Tools.Length; i++)
		{
			if (Tools[i].NewCell.type == cell.type)
			{
				Tools[i].Count++;
				return true;
			}
		}
		return false;
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
