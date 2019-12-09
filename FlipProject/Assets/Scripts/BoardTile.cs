using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BoardTile : TileBase
{
	// Enum index into sprite
	private enum SpriteIndex
	{
		// Generators
		GEN_0, GEN_1, GEN_2, GEN_3, GEN_4, GEN_5, GEN_6, GEN_7, GEN_8, GEN_9,
		// Misc
		WALL, BACKGROUND,
		// Hit Generators
		GEN_HIT_0, GEN_HIT_1, GEN_HIT_2, GEN_HIT_3, GEN_HIT_4, GEN_HIT_5, GEN_HIT_6, GEN_HIT_7, GEN_HIT_8, GEN_HIT_9,
		// Sluices
		SLUICE_UP, SLUICE_LEFT, SLUICE_DOWN, SLUICE_RIGHT,
		SLUICE_UP_L, SLUICE_LEFT_L, SLUICE_DOWN_L, SLUICE_RIGHT_L,
		SLUICE_UP_R, SLUICE_LEFT_R, SLUICE_DOWN_R, SLUICE_RIGHT_R,
		// Mirrors
		MIRROR_HORIZONTAL, MIRROR_FORWARD, MIRROR_VERTICAL, MIRROR_BACKWARD,
		// Inputs
		INPUT_UP, INPUT_LEFT, INPUT_DOWN, INPUT_RIGHT,
		// Tarpit
		TARPIT_PLUS, TARPIT_MINUS, TARPIT_MULTIPLY, TARPIT_DIVIDE, TARPIT_MOD,
		// Processor
		PROCESS_INCREMENT, PROCESS_DECREMENT, PROCESS_DOUBLE, PROCESS_HALF, PROCESS_NEGATE, PROCESS_SPLIT,
		// Modifier (Composite)
		MODIFIER_C_TRUE, MODIFIER_C_FALSE,
		MODIFIER_C_ZERO, MODIFIER_C_NONZERO, MODIFIER_C_POSITIVE, MODIFIER_C_NEGATIVE,
		MODIFIER_C_NONNEGATIVE, MODIFIER_C_NONPOSITIVE, MODIFIER_C_ODD, MODIFIER_C_EVEN,
		// Modifier (Bare)
		MODIFIER_TRUE, MODIFIER_FALSE,
		MODIFIER_ZERO, MODIFIER_NONZERO, MODIFIER_POSITIVE, MODIFIER_NEGATIVE,
		MODIFIER_NONNEGATIVE, MODIFIER_NONPOSITIVE, MODIFIER_ODD, MODIFIER_EVEN,
		// Animates
		OUTPUT1, OUTPUT2, OUTPUT3, OUTPUT4, OUTPUT5, OUTPUT6,
		MODIFIER1, MODIFIER2, MODIFIER3, MODIFIER4,
	}

	static Sprite[] SpriteTools;
	static Sprite[] OutputSprite;
	static Board TheBoard;

	#region Static interfaces
	public static void Initialize(Board theBoard)
	{
		SpriteTools = Resources.LoadAll<Sprite>("Sprites/NewTile");
		TheBoard = theBoard;

		OutputSprite = SpriteTools.Subsequence((int)SpriteIndex.OUTPUT1, 6).ToArray();
	}

	/// <summary>
	/// Find the sprite index of a certain item.
	/// </summary>
	/// <param name="cell">The item.</param>
	/// <returns>The sprite index, -1 if not applicable for whatever reason.</returns>
	public static int CellToSpriteIndex(Board.Cell cell)
	{
		switch (cell.type)
		{
			case Board.CellType.GENERATOR:
				return (int)SpriteIndex.GEN_0 + cell.param;
			case Board.CellType.MIRROR:
				return (int)SpriteIndex.MIRROR_HORIZONTAL + cell.param;
			case Board.CellType.PROCESS:
				return (int)SpriteIndex.PROCESS_SPLIT;
			case Board.CellType.SLUICE:
				return (int)SpriteIndex.SLUICE_UP + cell.param;
			case Board.CellType.TARPIT:
				return (int)SpriteIndex.TARPIT_PLUS + cell.param;
			case Board.CellType.MODIFIER_BOOLEAN:
				return (int)SpriteIndex.MODIFIER_TRUE + cell.param;
			case Board.CellType.MODIFIER_COMPARE:
				return (int)SpriteIndex.MODIFIER_ZERO + cell.param;
			case Board.CellType.MODIFIER_PARITY:
				return (int)SpriteIndex.MODIFIER_ODD + cell.param;
			case Board.CellType.INPUT:
				return (int)SpriteIndex.INPUT_UP + cell.param;
			case Board.CellType.OUTPUT:
				return (int)SpriteIndex.OUTPUT1;
			case Board.CellType.WALL:
				return (int)SpriteIndex.WALL;
			default:
				return -1;
		}
	}

	/// <summary>
	/// Find the corresponding Sprite of a certain item.
	/// </summary>
	/// <param name="cell">The item.</param>
	/// <returns>The Sprite.</returns>
	public static Sprite CellToSprite(Board.Cell cell)
	{
		int index = CellToSpriteIndex(cell);
		if (index == -1) return null;
		return SpriteTools[index];
	}
	#endregion

	#region TileBase overrides
	public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
	{
		Board.Cell mycell = TheBoard[(Vector2Int)position];
		if (mycell.type != Board.CellType.OUTPUT)
			return false;
		tileAnimationData.animatedSprites = OutputSprite;
		tileAnimationData.animationSpeed = 4f;
		tileAnimationData.animationStartTime = 0f;
		return true;
	}

	public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
	{
		Board.Cell mycell = TheBoard[(Vector2Int)position];
		int spriteIndex = CellToSpriteIndex(mycell);

		if (spriteIndex == -1)
			tileData.sprite = null;
		else
			tileData.sprite = SpriteTools[CellToSpriteIndex(mycell)];
		tileData.color = Color.white;
		tileData.transform = Matrix4x4.identity;
		tileData.flags = 0;
		tileData.gameObject = null;
	}

	public override void RefreshTile(Vector3Int position, ITilemap tilemap)
	{
		tilemap.RefreshTile(position);
	}
	#endregion
}
