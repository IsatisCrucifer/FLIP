using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Level
{
	public struct ToolItem
	{
		public Board.Cell NewCell;
		public int Count;
	}

	public struct CellPreset
	{
		public Board.Cell Cell;
		public Vector2Int Position;
	}

	public ToolItem[] Tools { get; private set; }
	public CellPreset[] Preset { get; private set; }

	/// <summary>
	/// Get the mock level.
	/// </summary>
	/// <returns></returns>
	public static Level GetMockLevel()
	{
		Level level = new Level();
		level.Tools = new ToolItem[]
		{
			new ToolItem { Count = 99, NewCell = { type = Board.CellType.MIRROR, param = 0 } },
			new ToolItem { Count = 99, NewCell = { type = Board.CellType.GENERATOR, param = 0 } },
			new ToolItem { Count = 99, NewCell = { type = Board.CellType.SLUICE, param = 0 } },
			new ToolItem { Count = 99, NewCell = { type = Board.CellType.PROCESS, param = 0 } },
			new ToolItem { Count = 99, NewCell = { type = Board.CellType.TARPIT, param = 0 } },
		};
		level.Preset = new CellPreset[]
		{
			new CellPreset { Cell = { type = Board.CellType.INPUT, param = 0 }, Position = new Vector2Int(-4, 3) },
			new CellPreset { Cell = { type = Board.CellType.WALL,  param = 0 }, Position = new Vector2Int(0, 3) },
			new CellPreset { Cell = { type = Board.CellType.OUTPUT,param = 0 }, Position = new Vector2Int(3, -4) },
		};
		return level;
	}

	/// <summary>
	/// Take an item from Toolbox.
	/// </summary>
	/// <param name="toolIndex">The index of tool.</param>
	/// <returns>The new item.</returns>
	public Board.Cell TakeTool(int toolIndex)
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
}
