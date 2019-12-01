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
			/* TARPIT */    2,
			/* INPUT */     1,
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
		public int value;
		public int positionX, positionY;
		public int velocityX, velocityY;

		public override string ToString()
		{
			return $"Photon value {value} at ({positionX}, {positionY}) with velocity ({velocityX}, {velocityY})";
		}

		public void SetVelocity(int vx, int vy)
		{
			velocityX = vx;
			velocityY = vy;
		}

		public Vector2 PositionAtFraction(float fraction)
		{
			return new Vector2(positionX + fraction * velocityX, positionY + fraction * velocityY);
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

	private Cell[,] board;
	private Cell[,] boardBackup;
	private int height, width;

	public Dictionary<int, Photon> Photons { get; private set; }

	public ToolItem[] Tools { get; private set; }

	public int MinX => -height / 2;
	public int MaxX => height - 1 + MinX;
	public int MinY => -width / 2;
	public int MaxY => width - 1 + MinY;

	// Board evaluation members
	private Script luaEnvironment;
	private DynValue luaGetIO;
	private List<int> input, output, golden;
	private int inputX, inputY;
	private int nextPhotonId;
	public int CurrentTime { get; private set; }
	public int InputSpeed;

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
		List<ToolItem> tools = new List<ToolItem>();

		luaEnvironment.Globals["SetSize"] = (Action<int>)((int size) =>
		{
			board = new Cell[size, size];
			width = height = size;
		});
		luaEnvironment.Globals["AddTool"] = (Action<string, int>)((string tool, int count) =>
		{
			tools.Add(new ToolItem { Count = count, NewCell = Cell.FromCharacter(tool[0]) });
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

		Tools = tools.ToArray();
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
		void FindInput(ref int inputX, ref int inputY)
		{
			for (int x = MinX; x <= MaxX; x++)
			{
				for (int y = MinY; y <= MaxY; y++)
				{
					if (board[x - MinX, y - MinY].type == CellType.INPUT)
					{
						inputX = x;
						inputY = y;
						return;
					}
				}
			}
		}
		FindInput(ref inputX, ref inputY);
		//Debug.Log($"Input coordinate: ({inputX}, {inputY})");

		// Generate first input Photon
		GeneratePhoton(input[0], inputX, inputY, 1, 0);
	}

	private void GeneratePhoton(int value, int posx, int posy, int velx, int vely)
	{
		Photons.Add(++nextPhotonId, new Photon
		{
			value = value,
			positionX = posx,
			positionY = posy,
			velocityX = velx,
			velocityY = vely
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
			p.positionX += p.velocityX;
			p.positionY += p.velocityY;

			if (!IsInBound(p.positionX, p.positionY))
			{
				Photons.Remove(id);
			}
			else
			{
				// Find out what is there
				Cell there = board[p.positionX - MinX, p.positionY - MinY];
				switch (there.type)
				{
					case CellType.EMPTY:
						break;
					case CellType.MIRROR:
						switch (there.param)
						{
							case 0: // - 
								if (p.velocityY == 0)
									Photons.Remove(id);
								else
									p.velocityY = -p.velocityY;
								break;
							case 1: // / 
								p.SetVelocity(p.velocityY, p.velocityX);
								break;
							case 2: // | 
								if (p.velocityX == 0)
									Photons.Remove(id);
								else
									p.velocityX = -p.velocityX;
								break;
							case 3: // \ 
								p.SetVelocity(-p.velocityY, -p.velocityX);
								break;
						}
						break;
					case CellType.GENERATOR:
						GeneratePhoton(there.param, p.positionX, p.positionY, p.velocityX, p.velocityY);
						p.SetVelocity(-p.velocityX, -p.velocityY);
						break;
					case CellType.SLUICE:
						switch (there.param)
						{
							case 0: // ^
								p.SetVelocity(0, 1);
								break;
							case 1: // <
								p.SetVelocity(-1, 0);
								break;
							case 2: // v
								p.SetVelocity(0, -1);
								break;
							case 3: // >
								p.SetVelocity(1, 0);
								break;
						}
						break;
					case CellType.PROCESS:
						GeneratePhoton(p.value, p.positionX, p.positionY, p.velocityY, p.velocityX);
						GeneratePhoton(p.value, p.positionX, p.positionY, -p.velocityY, -p.velocityX);
						Photons.Remove(id);
						break;
					case CellType.TARPIT:
						// I am stuck in the pit
						if (p.velocityX == 0 && p.velocityY == 0)
							break;
						// I come to the pit
						if (there.tarpitId == 0)
						{
							// No others are in the pit
							p.SetVelocity(0, 0); // I'm stuck
							board[p.positionX - MinX, p.positionY - MinY] = there.SetTarpitId(id);
						}
						else
						{
							// Someone is in the pit
							int thereValue = Photons[there.tarpitId].value;
							int newValue = there.param == 0 ? thereValue + p.value : thereValue * p.value;
							GeneratePhoton(newValue, p.positionX, p.positionY, p.velocityX, p.velocityY);
							Photons.Remove(id);
							Photons.Remove(there.tarpitId);
							board[p.positionX - MinX, p.positionY - MinY] = there.SetTarpitId(0);
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
			int id = CurrentTime / InputSpeed;
			if (id < input.Count)
			{
				GeneratePhoton(input[id], inputX, inputY, 1, 0);
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

	public void Stop()
	{
		CurrentTime = -1;
		board = boardBackup.Clone() as Cell[,];
		Photons.Clear();
	}
#endregion
}
