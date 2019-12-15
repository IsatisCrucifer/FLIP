using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class NotePanel : MonoBehaviour
{
	public Transform Content;
	public GameObject NoteIconWithTextPrefab, NoteTextPrefab;
	public Transform VerticalScroll;

	static Sprite[] NewTile;
	static Sprite[] Buttons;
	static Sprite[] Photon;

	private void Initialize()
	{
		NewTile = Resources.LoadAll<Sprite>("Sprites/NewTile");
		Buttons = Resources.LoadAll<Sprite>("Sprites/Buttons");
		Photon = Resources.LoadAll<Sprite>("Sprites/Photon");
	}

	private void Update()
	{
		if (Input.anyKey)
		{
			if (EventSystem.current.currentSelectedGameObject == null)
				CleanUp();
		}
	}

	public void Show()
	{
		gameObject.SetActive(true);
	}

	public void SetNote(string noteScript)
	{
		if (NewTile is null) Initialize();
		foreach (string line in noteScript.Split('\n'))
		{
			Sprite icon = null;
			string text = null;
			if (line == "") continue;
			if (line[0] == '{')
			{
				// has icon
				int closePosition = line.IndexOf('}');
				string iconPrimitive = line.Substring(1, closePosition - 1);
				string remain = line.Substring(closePosition + 1);
				string[] parse = iconPrimitive.Split(',');
				int index = Int32.Parse(parse[1]);
				//Debug.Log(parse[0] + "," + index);
				switch (parse[0])
				{
					case "NewTile":
						icon = NewTile[index];
						break;
					case "Buttons":
						icon = Buttons[index];
						break;
					case "Photon":
						icon = Photon[index];
						break;
				}
				text = remain;
			}
			else
			{
				text = line;
			}

			if (icon is null)
			{
				GameObject Newline = Instantiate(NoteTextPrefab, Content);
				Newline.GetComponentInChildren<Text>().text = text;
			}
			else
			{
				GameObject Newline = Instantiate(NoteIconWithTextPrefab, Content);
				Newline.GetComponentInChildren<Image>().sprite = icon;
				Newline.GetComponentInChildren<Text>().text = text;
			}
		}
		LayoutRebuilder.MarkLayoutForRebuild(Content as RectTransform);
	}

	private void CleanUp()
	{
		// Remove all note
		foreach (Transform child in Content.transform)
		{
			GameObject.Destroy(child.gameObject);
		}
		// Hide panel
		gameObject.SetActive(false);
	}
}
