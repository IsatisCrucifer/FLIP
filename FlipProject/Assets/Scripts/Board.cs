using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

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
			/* TARPIT */    5,
			/* INPUT */     4,
			/* OUTPUT */    1,
			/* WALL */      1,
		};

		public CellType type;
		public int param;
		public int tarpitId;

		public Cell(CellType type, int param)
		{
			this.type = type;
			this.param = param;
			this.tarpitId = 0;
		}

		public Cell RotateForward()
		{
			return new Cell(type, param.IncrementModM(CellRotationCount[(int)type]));
		}

		public Cell RotateBackward()
		{
			return new Cell(type, param.DecrementModM(CellRotationCount[(int)type]));
		}

		public Cell SetTarpitId(int id)
		{
			Cell newCell = this;
			newCell.tarpitId = id;
			return newCell;
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
				case '_': return new Cell(CellType.TARPIT, 1);
				case '*': return new Cell(CellType.TARPIT, 2);
				case '?': return new Cell(CellType.TARPIT, 3);
				case '%': return new Cell(CellType.TARPIT, 4);
				case 'i': return new Cell(CellType.INPUT, 3);
				case 'o': return new Cell(CellType.OUTPUT, 0);
				case '#': return new Cell(CellType.WALL, 0);
				default: throw new ArgumentException($"Unrecognized cell character '{c}'.");
			}
		}
	}

	public static Cell EmptyCell => new Cell(CellType.EMPTY, 0);

	private static Vector2Int DirectionIdToVector(int dir)
	{
		switch (dir)
		{
			case 0: return Vector2Int.up;
			case 1: return Vector2Int.left;
			case 2: return Vector2Int.down;
			case 3: return Vector2Int.right;
			case 4: default: return Vector2Int.zero;
		}
	}

	private static Vector2Int DirRotateCCW90(Vector2Int dir) => new Vector2Int(-dir.y, dir.x);
	private static Vector2Int DirReflect(Vector2Int dir) => new Vector2Int(-dir.x, -dir.y);
	private static Vector2Int DirRotateCW90(Vector2Int dir) => new Vector2Int(dir.y, -dir.x);

	public struct Photon
	{
		public int value;
		public Vector2Int position;
		public Vector2Int direction;

		public override string ToString()
		{
			return $"Photon value {value} at {position} with direction {direction}";
		}

		public void SetDirection(Vector2Int dir)
		{
			direction = dir;
		}

		public void Advance() => position += direction;

		public void TurnLeft() => direction = DirRotateCCW90(direction);

		public void Reflect() => direction = DirReflect(direction);

		public void TurnRight() => direction = DirRotateCW90(direction);

		public Vector2 PositionAtFraction(float fraction)
		{
			return position + fraction * (Vector2)direction;
		}
	}

	private Cell[,] board;
	private Cell[,] boardBackup;
	private int height, width;

	public Dictionary<int, Photon> Photons { get; private set; }

	public List<Cell> Tools { get; private set; }

	private int MinX => -height / 2;
	private int MaxX => height - 1 + MinX;
	private int MinY => -width / 2;
	private int MaxY => width - 1 + MinY;

	// Board evaluation members
	private Script luaEnvironment;
	private DynValue luaGetIO;
	private List<int> input, output, golden;
	private Vector2Int inputPosition;
	private Vector2Int inputDirection;
	private int nextPhotonId;
	public int CurrentTime { get; private set; }
	public int InputSpeed;
	public int InputIndex => CurrentTime / InputSpeed;

	public Board(string levelDescription)
	{
		Photons = new Dictionary<int, Photon>();
		CurrentTime = -1;
		InputSpeed = 3;

		ParseFromLua(levelDescription);
	}

	// Load level from level description.
	// See Resources/Level/Mockup.txt for syntax.
	private void ParseFromLua(string levelDescription)
	{
		luaEnvironment = new Script();
		//List<ToolItem> tools = new List<ToolItem>();
		Tools = new List<Cell>();

		luaEnvironment.Globals["SetSize"] = (Action<int>)((int size) =>
		{
			board = new Cell[size, size];
			width = height = size;
		});
		luaEnvironment.Globals["AddTool"] = (Action<string>)((string tool) =>
		{
			Tools.Add(Cell.FromCharacter(tool[0]));
		});
		luaEnvironment.Globals["SetPreset"] = (Action<string>)((string preset) =>
		{
			int x, y = height - 1;
			foreach (string s in preset.Split('\n'))
			{
				if (s == "") continue;
				x = 0;
				foreach (char c in s.Substring(0, width))
				{
					board[x, y] = Cell.FromCharacter(c);
					x++;
				}
				y--;
				if (y < 0) break;
			}
		});

		luaEnvironment.DoString(levelDescription);

		luaGetIO = luaEnvironment.Globals.Get("GetIO");
	}

	public string GetBeforeLevelDialogScript()
	{
		DynValue value = luaEnvironment.Globals.Get("BeforeLevelDialog");
		if (value.Type != DataType.String)
			return "";
		else
			return value.String;
	}

	public string GetAfterLevelDialogScript()
	{
		DynValue value = luaEnvironment.Globals.Get("AfterLevelDialog");
		if (value.Type != DataType.String)
			return "";
		else
			return value.String;
	}

	/// <summary>
	/// Take an item from Toolbox.
	/// </summary>
	/// <param name="toolIndex">The index of tool.</param>
	/// <returns>The new item.</returns>
	public Cell TakeTool(int toolIndex)
	{
		if (toolIndex >= 0 && toolIndex < Tools.Count)
		{
			return Tools[toolIndex];
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
		return cell.type != CellType.INPUT && cell.type != CellType.OUTPUT && cell.type != CellType.WALL;
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

	#region Board search/traverse functions
	public void ForEachCell(Action<int, int, Cell> action)
	{
		for(int x = MinX; x <= MaxX; x++)
		{
			for(int y = MinY; y <= MaxY; y++)
			{
				action(x, y, board[x - MinX, y - MinY]);
			}
		}
	}

	public void FindCell(Func<int, int, Cell, bool> condition, Action<int, int, Cell> action)
	{
		ForEachCell((int x, int y, Cell cell) => {
			if (condition(x, y, cell))
				action(x, y, cell);
		});
	}
	#endregion

	#region Board evolving functions
	public void Start(int seed)
	{
		// Bookkeeping
		CurrentTime = 0;
		nextPhotonId = 0;
		Photons.Clear();

		// Get input/output from script
		System.Random random = new System.Random(seed);
		DynValue iovalue = luaEnvironment.Call(
			luaGetIO, (Func<int, int>)((max) => random.Next(max))
		);
		input = iovalue.Tuple[0].ToObject<List<int>>();
		golden = iovalue.Tuple[1].ToObject<List<int>>();
		output = new List<int>();

		// Save the original state of board
		boardBackup = board.Clone() as Cell[,];

		// Find out where input is
		FindCell((x, y, cell) => cell.type == CellType.INPUT,
			(x, y, cell) =>
			{
				inputPosition = new Vector2Int(x, y);
				inputDirection = DirectionIdToVector(cell.param);
			});
		//Debug.Log($"Input coordinate: ({inputX}, {inputY})");

		// Generate first input Photon
		GeneratePhoton(input[0], inputPosition, inputDirection);
	}

	private void GeneratePhoton(int value, Vector2Int position, Vector2Int dir)
	{
		Photons.Add(++nextPhotonId, new Photon
		{
			value = value,
			position = position,
			direction = dir
		});
		//Debug.Log($"Generated Photon id {nextPhotonId}: {Photons[nextPhotonId]}");
	}

	public void Step()
	{
		if (CurrentTime < 0) return;
		++CurrentTime;

		// Do emulation
		foreach (int id in Photons.Keys.ToList())
		{
			if (!Photons.ContainsKey(id)) continue;
			Photon p = Photons[id];

			// update position
			p.Advance();

			if (!IsInBound(p.position))
			{
				Photons.Remove(id);
			}
			else
			{
				// Find out what is there
				Cell there = this[p.position];
				switch (there.type)
				{
					case CellType.EMPTY:
						break;
					case CellType.MIRROR:
						switch (there.param)
						{
							case 0: // - 
								if (p.direction.y == 0)
									Photons.Remove(id);
								else
									p.Reflect();
								break;
							case 1: // / 
								if (p.direction.y == 0)
									p.TurnLeft();
								else
									p.TurnRight();
								break;
							case 2: // | 
								if (p.direction.x == 0)
									Photons.Remove(id);
								else
									p.Reflect();
								break;
							case 3: // \ 
								if (p.direction.x == 0)
									p.TurnLeft();
								else
									p.TurnRight();
								break;
						}
						break;
					case CellType.GENERATOR:
						GeneratePhoton(there.param, p.position, p.direction);
						p.Reflect();
						break;
					case CellType.SLUICE:
						p.SetDirection(DirectionIdToVector(there.param));
						break;
					case CellType.PROCESS:
						GeneratePhoton(p.value, p.position, DirRotateCCW90(p.direction));
						GeneratePhoton(p.value, p.position, DirRotateCW90(p.direction));
						Photons.Remove(id);
						break;
					case CellType.TARPIT:
						// I am stuck in the pit
						if (p.direction == Vector2Int.zero)
							break;
						// I come to the pit
						if (there.tarpitId == 0)
						{
							// No others are in the pit
							p.SetDirection(Vector2Int.zero); // I'm stuck
							this[p.position] = there.SetTarpitId(id);
						}
						else
						{
							// Someone is in the pit
							int thereValue = Photons[there.tarpitId].value;
							int newValue;
							switch (there.param)
							{
								case 0: newValue = thereValue + p.value; break;
								case 1: newValue = thereValue - p.value; break;
								case 2: newValue = thereValue * p.value; break;
								case 3: newValue = p.value == 0 ? 0 : thereValue / p.value; break;
								case 4: newValue = p.value == 0 ? 0 : thereValue % p.value; break;
								default: newValue = 0; break;
							}
							GeneratePhoton(newValue, p.position, p.direction);
							Photons.Remove(id);
							Photons.Remove(there.tarpitId);
							this[p.position] = there.SetTarpitId(0);
							//Debug.Log($"Photon id {there.tarpitId} in tarpit removed");
						}
						break;
					case CellType.INPUT:
						Photons.Remove(id);
						break;
					case CellType.OUTPUT:
						output.Add(p.value);
						Photons.Remove(id);
						break;
					case CellType.WALL:
						Photons.Remove(id);
						break;
				}
			}

			if (Photons.ContainsKey(id))
			{
				Photons[id] = p;
				//Debug.Log($"Photon id {id} updated to {p}");
			}
			else
			{
				//Debug.Log($"Photon id {id} removed");
			}
		}

		// Generate photon from input
		if (CurrentTime % InputSpeed == 0)
		{
			int id = InputIndex;
			if (id < input.Count)
			{
				GeneratePhoton(input[id], inputPosition, inputDirection);
			}
		}
	}

	// If all inputs dispensed and all photons disappeared
	public bool IsComplete()
	{
		return (Photons.Count == 0 && CurrentTime >= InputSpeed * input.Count);
	}

	// If the output matches golden
	public bool IsOutputMatch()
	{
		return golden.SequenceEqual(output);
	}

	// Preview to be displayed on screen
	public List<int> InputPreview()
	{
		if (CurrentTime < 0) return new List<int>();
		return input.Slice(InputIndex + 1).ToList();
	}

	// Recent output to be displayed on screen
	public List<Tuple<int, bool>> RecentOutput()
	{
		if (CurrentTime < 0) return new List<Tuple<int, bool>>();
		return output.Zip(golden,
			(outElem, goldenElem) => Tuple.Create<int, bool>(outElem, outElem == goldenElem)).ToList();
	}

	public void Stop()
	{
		CurrentTime = -1;
		board = boardBackup;
		boardBackup = null;
		Photons.Clear();
	}
#endregion
}
