using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoardEngine : MonoBehaviour
{
	// Unity UI Objects
	private struct ToolGameObjects
	{
		public GameObject TheTool;
		public Image ImageIcon;
		public Text TextAmount;
	}
	private List<ToolGameObjects> GameObject_Tools;

	// Enum index into sprite
	private enum BoardObjects
	{
		// Generators
		GEN_0, GEN_1, GEN_2, GEN_3, GEN_4, GEN_5, GEN_6, GEN_7, GEN_8, GEN_9,
		// Mirrors
		MIRROR_HORIZONTAL, MIRROR_FORWARD, MIRROR_VERTICAL, MIRROR_BACKWARD,
		// Sluices
		SLUICE_UP, SLUICE_LEFT, SLUICE_DOWN, SLUICE_RIGHT,
		// Grille
		GRILLE,
		// Processor
		PROCESS_INCREMENT, PROCESS_DECREMENT, PROCESS_RESET, PROCESS_NEGATE, PROCESS_SPLIT,
		// Tarpit
		TARPIT_PLUS, TARPIT_MULTIPLY,
		// Misc
		STOP, OUTPUT, INPUT, WALL,
		// Modifier (TODO: remove?)
		MODIFIER_TRUE, MODIFIER_POSITIVE, MODIFIER_NEGATIVE, MODIFIER_ZERO, MODIFIER_ODD, MODIFIER_RANDOM,
		// Level (TODO: Really remove)
		LEVEL_UP, LEVEL_DOWN,
		// Misc
		BACKGROUND, UNKNOWN,
	}
	// The Sprite
	Sprite[] SpriteTools;

	// Icon list
	private struct ToolItem
	{
		public BoardObjects Icon { get; set; }
		public int Count { get; set; }
	}

	private List<ToolItem> Tools;

	// Start is called before the first frame update
	void Start()
	{
		GameObject GOToolbox = GameObject.Find("Toolbox");
		GameObject_Tools = new List<ToolGameObjects>();
		foreach (Transform t in GOToolbox.transform)
		{
			GameObject_Tools.Add(new ToolGameObjects
			{
				TheTool = t.gameObject,
				ImageIcon = t.Find("Icon").GetComponent<Image>(),
				TextAmount = t.Find("Amount").GetComponent<Text>(),
			});
		}

		SpriteTools = Resources.LoadAll<Sprite>("Sprites/Legacy Tile");

		LoadLevel();
	}

	// Update is called once per frame
	void Update()
	{
		for (int i = 0; i < Tools.Count; i++)
		{
			if (Tools[i].Count < 0)
				GameObject_Tools[i].TextAmount.text = "INF";
			else
				GameObject_Tools[i].TextAmount.text = Tools[i].Count.ToString();
		}
	}

	private void LoadLevel()
	{
		Tools = new List<ToolItem> {
			new ToolItem { Icon = BoardObjects.MIRROR_HORIZONTAL, Count = 99 },
			new ToolItem { Icon = BoardObjects.GEN_0,  Count = 99 },
			new ToolItem { Icon = BoardObjects.SLUICE_UP, Count = 99 },
			new ToolItem { Icon = BoardObjects.PROCESS_SPLIT, Count = 99 },
			new ToolItem { Icon = BoardObjects.TARPIT_PLUS, Count = 99 },
		};

		for (int i = 0; i < GameObject_Tools.Count; i++)
		{
			if (i >= Tools.Count)
			{
				GameObject_Tools[i].TheTool.SetActive(false);
			}
			else
			{
				GameObject_Tools[i].ImageIcon.sprite = SpriteTools[(int)(Tools[i].Icon)];
				GameObject_Tools[i].TextAmount.text = "INF";
			}
		}
	}
}
