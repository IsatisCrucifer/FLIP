using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public class BoardScript : MonoBehaviour,
	IPointerDownHandler, IPointerClickHandler,
	IBeginDragHandler, IDragHandler, IEndDragHandler
{
	// Current level. Set by LevelSelectControl.
	public static TextAsset levelAsset = null;
	public static string levelId = null;

	// Unity UI Objects
	private struct ToolGameObjects
	{
		public GameObject ToolObject;
		public Image Icon;
	}
	private List<ToolGameObjects> LGOTools;

	private Dictionary<int, GameObject> GOPhotons;

	// Properties should be set via Inspector
	public Camera MainCamera;
	public Tilemap BoardTilemap;
	public GameObject Toolbox;
	public Transform DragIcon;
	public Transform PhotonScaler;
	public DialogScript TheDialog;
	public TargetDialog TheTargetDialog;
	public GameObject PhotonPrefab;
	public Toggle PlayToggle, PauseToggle, StopToggle;

	// The Sprite
	Sprite[] SpritePhotons;

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
		BoardTilemap.RefreshTile((Vector3Int)pos);
	}

	#region Monobehaviour callbacks

	void Awake()
	{
		this.CheckInspectorConnection();
	}

	void Start()
	{
		// Finding all Unity GameObjects we need.
		LGOTools = new List<ToolGameObjects>();
		foreach (Transform t in Toolbox.transform)
		{
			LGOTools.Add(new ToolGameObjects
			{
				ToolObject = t.gameObject,
				Icon = t.Find("Icon").GetComponent<Image>(),
			});
		}

		SpritePhotons = Resources.LoadAll<Sprite>("Sprites/Photon");

		GOPhotons = new Dictionary<int, GameObject>();

		// Get the mockup level if no level is set
		if (levelAsset is null)
			levelAsset = Resources.Load<TextAsset>("Level/0_Mockup");

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
						GOThisPhoton = Instantiate(PhotonPrefab, PhotonScaler);
						GOThisPhoton.SetActive(true);
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
							if (!(levelId is null))
							{
								LevelSelectControl.currentSave.SetCleared(levelId);
								LevelSelectControl.currentSave.Save();
							}
							SceneManager.LoadScene("LevelSelect");
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
				LGOTools[i].ToolObject.SetActive(false);
			}
			else
			{
				LGOTools[i].Icon.sprite = BoardTile.CellToSprite(TheBoard.Tools[i]);
			}
		}

		IsDragging = false;

		TheBoard.ForEachCell((x, y, cell)=>
		{
			BoardTile tile = ScriptableObject.CreateInstance<BoardTile>();
			BoardTilemap.SetTile(new Vector3Int(x, y, 0), tile);
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
			toolbox => eventData.hovered.FindIndex(hover => toolbox.ToolObject == hover) != -1
		);
		//Debug.LogFormat("FindPosition: Tool {0}", toolID);
		if (toolID != -1)
		{
			//Debug.Log("FindPosition: Decided at TOOL");
			return new BoardPosition { place = BoardPosition.Place.TOOLBOX, x = toolID };
		}
		// Check for board
		Vector3Int mousePosOnBoard = BoardTilemap.WorldToCell(MainCamera.ScreenToWorldPoint(eventData.position));
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

	public void PlayButtonToggle(bool state)
	{
		// Reject glitch state
		if (IsRunning == state) return;

		if (state == true)
		{
			StartSimulation();
		}
		else
		{
			StopSimulation();
		}
	}

	private void StartSimulation()
	{
		SimulationTime = 0;
		IsRunning = true;
		IsPausing = false;
		TheBoard.Start(42);
	}

	public void PauseSimulation()
	{
		if (!IsRunning)
		{
			PauseToggle.SetIsOnWithoutNotify(false);
		}
		else
		{
			IsPausing = !IsPausing;
		}
	}

	public void StopSimulation()
	{
		if (!IsRunning) return;
		IsRunning = IsPausing = false;
		PlayToggle.SetIsOnWithoutNotify(false);
		PauseToggle.SetIsOnWithoutNotify(false);
		StopToggle.SetIsOnWithoutNotify(false);
		TheBoard.Stop();
		foreach (var kv in GOPhotons)
		{
			Destroy(kv.Value);
		}
		GOPhotons.Clear();
	}

	public void ShowTarget()
	{
		TheTargetDialog.SetTargetText(TheBoard.GetTargetDescription());
		TheTargetDialog.Show();
	}
	#endregion

	#region Photon to Unity Object
	private Sprite PhotonToSprite(Board.Photon p)
	{
		return SpritePhotons[p.value.ModM(10)];
	}
	#endregion
}

