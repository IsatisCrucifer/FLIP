using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;

public class BoardScript : MonoBehaviour,
	IPointerDownHandler, IPointerClickHandler,
	IBeginDragHandler, IDragHandler, IEndDragHandler
{
	// Possible public Unity interface
	public static TextAsset levelAsset = null;

	// Unity UI Objects
	private struct ToolGameObjects
	{
		public GameObject GOTool;
		public Image ImageIcon;
	}
	private List<ToolGameObjects> LGOTools;

	private Camera MainCamera;
	private Tilemap TMBoard;

	private Image ImageRunButton, ImageStopButton;
	private Transform DragIcon;
	private GameObject GOPhotonTemplate;
	private Transform PhotonScaler;
	private Dictionary<int, GameObject> GOPhotons;

	public DialogScript TheDialog;

	// The Sprite
	Sprite[] SpritePhotons;
	Sprite[] SpriteButtons;

	// The Board & Gamestate
	private Board TheBoard;
	private BoardPosition MouseDownPos;
	private bool IsDragging;
	private Vector2Int DraggingFrom;
	private Board.Cell DraggingCell;
	private bool IsRunning;
	private bool IsPausing;
	private float SimulationTime;
	private float SimulationSpeed;

	/// <summary>
	/// Set a board position to a cell, and update the corresponding tile.
	/// </summary>
	/// <param name="pos">Position in the board.</param>
	/// <param name="cell">The content.</param>
	private void SetBoardAndTile(Vector2Int pos, Board.Cell cell)
	{
		TheBoard[pos] = cell;
		TMBoard.RefreshTile((Vector3Int)pos);
	}

	#region Monobehaviour callbacks
	void Awake()
	{
#if UNITY_EDITOR
		// Check only in editor if all references are all set
		do
		{
			if (TheDialog is null)
			{
				Debug.LogError($"TheDialog is not set in {gameObject.name}!");
				break;
			}
			return;
		} while(false);
		UnityEditor.EditorApplication.isPlaying = false;
#endif
	}

	void Start()
	{
		// Finding all Unity GameObjects we need.
		GameObject GOToolbox = GameObject.Find("Toolbox");
		LGOTools = new List<ToolGameObjects>();
		foreach (Transform t in GOToolbox.transform)
		{
			LGOTools.Add(new ToolGameObjects
			{
				GOTool = t.gameObject,
				ImageIcon = t.Find("Icon").GetComponent<Image>(),
				//TextAmount = t.Find("Amount").GetComponent<Text>(),
			});
		}

		SpritePhotons = Resources.LoadAll<Sprite>("Sprites/Photon");
		SpriteButtons = Resources.LoadAll<Sprite>("Sprites/RunImage");

		MainCamera = GameObject.Find("Main Camera").GetComponent<Camera>();

		TMBoard = GameObject.Find("Grid/BoardTilemap").GetComponent<Tilemap>();

		ImageRunButton = GameObject.Find("RunButton").GetComponent<Image>();
		ImageStopButton = GameObject.Find("StopButton").GetComponent<Image>();
		GOPhotonTemplate = GameObject.Find("Photon Template");
		PhotonScaler = GOPhotonTemplate.transform.parent;
		DragIcon = GameObject.Find("DragIcon").transform;

		GOPhotons = new Dictionary<int, GameObject>();

		// Get the mockup level if no level is set
		if (levelAsset is null)
			levelAsset = Resources.Load<TextAsset>("Level/Mockup");

		// TheDialog = GameObject.Find("DialogCanvas").GetComponent<DialogScript>();
		System.Diagnostics.Debug.Assert(TheDialog is null is false, "");

		LoadLevel();
	}

	void Update()
	{
		if (IsRunning)
		{
			if (!IsPausing) // Update only when not pausing
			{
				SimulationTime += Time.deltaTime * SimulationSpeed;
				while (TheBoard.CurrentTime < (int)(Mathf.Floor(SimulationTime)))
				{
					TheBoard.Step();
				}
				float fractionTime = Mathf.Repeat(SimulationTime, 1f);
				//Debug.Log($"Simulation Time {SimulationTime}, fraction time {fractionTime}");
				foreach (int key in GOPhotons.Keys.ToList())
				{
					if (!TheBoard.Photons.ContainsKey(key))
					{
						Destroy(GOPhotons[key]);
						GOPhotons.Remove(key);
					}
				}
				foreach (var kv in TheBoard.Photons)
				{
					GameObject GOThisPhoton;
					if (!GOPhotons.ContainsKey(kv.Key))
					{
						GOThisPhoton = Instantiate(GOPhotonTemplate);
						GOThisPhoton.SetActive(true);
						GOThisPhoton.transform.localScale = new Vector3(2, 2, 1);
						GOThisPhoton.transform.SetParent(PhotonScaler);
						GOThisPhoton.GetComponent<SpriteRenderer>().sortingOrder = kv.Key;
						GOPhotons[kv.Key] = GOThisPhoton;
						//Debug.Log($"Generated new UI Object Photon {kv.Key}");
					}
					else
					{
						GOThisPhoton = GOPhotons[kv.Key];
					}
					GOThisPhoton.GetComponent<SpriteRenderer>().sprite = PhotonToSprite(kv.Value);
					GOThisPhoton.transform.localPosition = (Vector3)(kv.Value.PositionAtFraction(fractionTime));
					//Debug.Log($"UI Object Photon {kv.Key} has position {GOThisPhoton.transform.localPosition}");
				}
				if (TheBoard.IsComplete())
				{
					if (TheBoard.IsOutputMatch())
					{
						// Pause simulation to prevent reinvoke
						IsPausing = true;
						TheDialog.SetDialog(TheBoard.GetAfterLevelDialogScript());
						TheDialog.StartDialog(() =>
						{
							Debug.Log("Level Complete!");
							StopSimulation();
						});
					}
				}
			}
		}
		else // Not Running
		{
			// Currently nothing to do
		}
	}
	#endregion

	/// <summary>
	/// Load the level from levelAsset.
	/// </summary>
	private void LoadLevel()
	{
		TheBoard = new Board(levelAsset.text);
		BoardTile.Initialize(TheBoard);

		for (int i = 0; i < LGOTools.Count; i++)
		{
			if (i >= TheBoard.Tools.Count)
			{
				LGOTools[i].GOTool.SetActive(false);
			}
			else
			{
				LGOTools[i].ImageIcon.sprite = BoardTile.CellToSprite(TheBoard.Tools[i]);
			}
		}

		IsDragging = false;

		TheBoard.ForEachCell((x, y, cell)=>
		{
			BoardTile tile = ScriptableObject.CreateInstance<BoardTile>();
			TMBoard.SetTile(new Vector3Int(x, y, 0), tile);
		});

		SimulationTime = -1;
		IsRunning = IsPausing = false;
		SimulationSpeed = 1;

		string beforeLevelDialog = TheBoard.GetBeforeLevelDialogScript();
		if (beforeLevelDialog != "")
		{
			TheDialog.SetDialog(beforeLevelDialog);
			TheDialog.StartDialog(() => { Debug.Log("Level start!"); });
		}
	}

	/// <summary>
	/// The position in a board.
	/// There are three possible positions:
	/// BOARD - Inside the board. (x, y) denotes the position.
	///         Can be implicitly cast to Vector2Int or Vector3Int.
	/// TOOLBOX - Some tool in the Toolbox. (x) denotes index.
	/// OUTSIDE - None of the above.
	/// </summary>
	private struct BoardPosition
	{
		public enum Place { OUTSIDE, BOARD, TOOLBOX };
		public Place place;
		public int x, y;
		public static implicit operator Vector2Int (BoardPosition pos) => new Vector2Int(pos.x, pos.y);
		public static implicit operator Vector3Int (BoardPosition pos) => new Vector3Int(pos.x, pos.y, 0);
		public bool Equals(BoardPosition other)
		{
			if (place != other.place) return false;
			switch(place)
			{
				case Place.OUTSIDE: return true;
				case Place.BOARD: return x == other.x && y == other.y;
				case Place.TOOLBOX: return x == other.x;
				default: return false;
			}
		}
		public override string ToString()
		{
			switch(place)
			{
				case Place.OUTSIDE: return "OUTSIDE";
				case Place.BOARD: return $"BOARD ({x}, {y})";
				case Place.TOOLBOX: return $"TOOLBOX #{x}";
				default: return "?????";
			}
		}
	}

	/// <summary>
	/// Find where the position from an PointerEventData is.
	/// </summary>
	/// <param name="eventData">PointerEventData from event.</param>
	/// <returns>The UI position.</returns>
	private BoardPosition FindPosition(PointerEventData eventData)
	{
		//Debug.LogFormat("FindPosition: Mouse position: {0}", eventData.position);
		// Check for toolboxes
		int toolID = LGOTools.FindIndex(
			toolbox => eventData.hovered.FindIndex(hover => toolbox.GOTool == hover) != -1
		);
		//Debug.LogFormat("FindPosition: Tool {0}", toolID);
		if (toolID != -1)
		{
			//Debug.Log("FindPosition: Decided at TOOL");
			return new BoardPosition { place = BoardPosition.Place.TOOLBOX, x = toolID };
		}
		// Check for board
		Vector3Int mousePosOnBoard = TMBoard.WorldToCell(MainCamera.ScreenToWorldPoint(eventData.position));
		//Debug.LogFormat("FindPosition: Board {0}", mousePosOnBoard);
		if (TheBoard.IsInBound(mousePosOnBoard.x, mousePosOnBoard.y))
		{
			//Debug.Log("FindPosition: Decided at BOARD");
			return new BoardPosition
			{
				place = BoardPosition.Place.BOARD,
				x = mousePosOnBoard.x,
				y = mousePosOnBoard.y
			};
		}
		//Debug.LogFormat("FindPosition: Decided at OUTSIDE");
		return new BoardPosition { place = BoardPosition.Place.OUTSIDE };
	}

	#region Drag & Click Handler
	public void OnBeginDrag(PointerEventData eventData)
	{
		if (IsRunning) return;
		if (eventData.button != PointerEventData.InputButton.Left) return;
		BoardPosition pos = MouseDownPos; // Use position when mouse is down, not start dragging
		//BoardPosition pos = FindPosition(eventData);
		//Debug.LogFormat("OnBeginDrag: {0}", pos);
		do
		{
			if (pos.place == BoardPosition.Place.TOOLBOX)
			{
				int toolID = pos.x;
				Board.Cell NewCell = TheBoard.TakeTool(toolID);
				if (!NewCell.IsEmpty())
				{
					DraggingCell = NewCell;
					DraggingFrom = new Vector2Int(-999, -999);
					break;
				}
			}
			else if (pos.place == BoardPosition.Place.BOARD)
			{
				// Begin drag on a board
				Board.Cell cell = TheBoard[pos];
				if (cell.IsMovable())
				{
					DraggingCell = cell;
					DraggingFrom = pos;
					SetBoardAndTile(DraggingFrom, Board.EmptyCell);
					break;
				}
			}
			eventData.pointerDrag = null;
			return;
		} while (false);
		IsDragging = true;
		Sprite draggingSprite = BoardTile.CellToSprite(DraggingCell);
		DragIcon.gameObject.SetActive(true);
		DragIcon.GetComponent<SpriteRenderer>().sprite = draggingSprite;
		DragIcon.position = MainCamera.ScreenToWorldPoint((Vector3)eventData.position).SetZ(0);
		//Debug.LogFormat("OnBeginDrag: Event position = {0}", eventData.position));
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (IsRunning) return;
		if (!IsDragging) return;
		DragIcon.position = MainCamera.ScreenToWorldPoint((Vector3)eventData.position).SetZ(0);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (IsRunning) return;
		if (!IsDragging) return;
		BoardPosition pos = FindPosition(eventData);
		//Debug.LogFormat("OnEndDrag: {0}", pos);
		if (pos.place == BoardPosition.Place.BOARD)
		{
			// Drop in board
			Board.Cell targetCell = TheBoard[pos];
			if (targetCell.IsEmpty())
			{
				// Set item at target position
				SetBoardAndTile(pos, DraggingCell);
			}
			else if (DraggingFrom.x != -999)
			{
				// Move from board
				if (!targetCell.IsMovable())
				{
					// Cannot swap with immovable object, spring back to original position
					SetBoardAndTile(DraggingFrom, DraggingCell);
				}
				else
				{
					// There are something there and is movable, swap them
					SetBoardAndTile(DraggingFrom, targetCell);
					SetBoardAndTile(pos, DraggingCell);
				}
			}
			else
			{
				// Move from toolbox but dropped on something. Discard it.
				TheBoard.ReturnTool(DraggingCell);
			}
		}
		else
		{
			// Drop outside board, try to return the item to toolbox
			if (!TheBoard.ReturnTool(DraggingCell))
			{
				// Item cannot be returned, spring back to original position (if there is)
				if (DraggingFrom.x != -999)
				{
					SetBoardAndTile(DraggingFrom, DraggingCell);
				}
			}
		}
		DragIcon.gameObject.SetActive(false);
		IsDragging = false;
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (IsRunning) return;
		MouseDownPos = FindPosition(eventData);
		//Debug.LogFormat("OnPointerDown: {0}", MouseDownPos);
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		if (IsRunning) return;
		// If we are dragging, OnEndDrag will be called after OnPointerClick, 
		// so IsDragging will be set.
		if (IsDragging) return;
		BoardPosition pos = FindPosition(eventData);
		//Debug.LogFormat("OnPointerClick: {0}", pos);
		if (!MouseDownPos.Equals(pos)) return;
		if (pos.place == BoardPosition.Place.BOARD)
		{
			// Click on board. LClick rotate forward, RClick rotate backward.
			Board.Cell currentCell = TheBoard[pos];
			if (eventData.button == PointerEventData.InputButton.Left)
				SetBoardAndTile(pos, currentCell.RotateForward());
			else if (eventData.button == PointerEventData.InputButton.Right)
				SetBoardAndTile(pos, currentCell.RotateBackward());
		}
	}
	#endregion

	#region Button Handler
	public void SetSimulationSpeed(float speed)
	{
		SimulationSpeed = speed;
	}

	public void StartSimulation()
	{
		if (!IsRunning || IsPausing)
		{
			if (!IsRunning)
			{
				SimulationTime = 0;
			}
			IsRunning = true;
			IsPausing = false;
			ImageRunButton.sprite = SpriteButtons[1];
			ImageStopButton.sprite = SpriteButtons[2];
			TheBoard.Start(42);
		}
		else
		{
			IsPausing = true;
			ImageRunButton.sprite = SpriteButtons[0];
		}
	}

	public void StopSimulation()
	{
		if (!IsRunning) return;
		IsRunning = IsPausing = false;
		TheBoard.Stop();
		ImageRunButton.sprite = SpriteButtons[0];
		ImageStopButton.sprite = SpriteButtons[3];
		foreach (var kv in GOPhotons)
		{
			Destroy(kv.Value);
		}
		GOPhotons.Clear();
	}
	#endregion

	#region Photon to Unity Object
	private Sprite PhotonToSprite(Board.Photon p)
	{
		return SpritePhotons[p.value.ModM(10)];
	}
	#endregion
}

