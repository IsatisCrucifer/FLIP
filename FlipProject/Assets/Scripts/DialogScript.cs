using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

struct DialogScriptCommand
{
	public string command;
	public string param;
}

public class DialogScript : MonoBehaviour, IPointerClickHandler
{
	private IEnumerator<DialogScriptCommand> scriptParser;
	private string displayText;
	public float dialogSpeed = 0.125f;

	private bool playerClicked;

	// Unity objects
	private Text DialogText, DialogNameText;
	private RectTransform RTNextArrow;

	void Start()
	{
		DialogText = GameObject.Find("DialogText").GetComponent<Text>();
		DialogNameText = GameObject.Find("DialogName").GetComponent<Text>();
		RTNextArrow = GameObject.Find("CursorText").GetComponent<RectTransform>();
		RTNextArrow.gameObject.SetActive(false);
	}
	
	void Update()
	{
		if (Input.GetKeyDown("space") || Input.GetKeyDown("return")) playerClicked = true;

		if (!RTNextArrow.gameObject.activeInHierarchy) return;

		float fracTime = (Time.time % 0.5f) * 2;
		if (fracTime < .5f)
			RTNextArrow.anchoredPosition3D = RTNextArrow.anchoredPosition3D.SetY(40 + 20 * fracTime);
		else
			RTNextArrow.anchoredPosition3D = RTNextArrow.anchoredPosition3D.SetY(60 - 20 * fracTime);
	}

	public void SetDialog(string dialogScript)
	{
		scriptParser = ParseCommand(dialogScript);
		displayText = "";

		playerClicked = false;
	}

	public void StartDialog(Action afterDialog)
	{
		gameObject.SetActive(true);
		StartCoroutine(DialogCoroutine(afterDialog));
	}

	private IEnumerator<DialogScriptCommand> ParseCommand(string dialogScript)
	{
		int scriptPosition = 0, scriptLength = dialogScript.Length;
		while (scriptPosition < scriptLength)
		{
			if (dialogScript[scriptPosition] == '[')
			{
				int closePosition = dialogScript.IndexOf(']', scriptPosition);
				string command = dialogScript.Substring(scriptPosition + 1, closePosition - scriptPosition - 1);
				scriptPosition = closePosition + 1;
				if (dialogScript[scriptPosition] == '\n') scriptPosition++;
				string[] token = command.Split('=');
				if (token.Length == 1)
					yield return new DialogScriptCommand { command = token[0], param = "" };
				else
					yield return new DialogScriptCommand { command = token[0], param = token[1] };
			}
			else
			{
				yield return new DialogScriptCommand { command = "", param = new string(dialogScript[scriptPosition++], 1) };
			}
		}
	}

	string InitDisplayText()
	{
		if (DialogNameText.text == "")
		{
			displayText = "";
		}
		else
		{
			displayText = "\n";
		}
		return displayText;
	}

	IEnumerator DialogCoroutine(Action afterDialog)
	{
		bool skippingToWait = false;
		var interval = new WaitForSeconds(dialogSpeed);
		yield return interval;
		while (scriptParser.MoveNext())
		{
			if (playerClicked)
			{
				playerClicked = false;
				skippingToWait = true;
			}
			DialogScriptCommand nextCommand = scriptParser.Current;
			if (nextCommand.command == "") // Ordinary text
			{
				displayText += nextCommand.param;
				DialogText.text = displayText;
				if (!skippingToWait)
					yield return interval;
			}
			else if (nextCommand.command == "name") // speaker name
			{
				DialogNameText.text = nextCommand.param;
				DialogText.text = InitDisplayText();
				if (!skippingToWait)
					yield return interval;
			}
			else if (nextCommand.command == "w") // wait for click
			{
				skippingToWait = false;
				RTNextArrow.gameObject.SetActive(true);
				while (!playerClicked)
				{
					yield return null;
				}
				RTNextArrow.gameObject.SetActive(false);
				playerClicked = false;
				DialogText.text = InitDisplayText();
			}
		}
		gameObject.SetActive(false);
		afterDialog();
	}

	public void OnPointerClick(PointerEventData eventData)
	{
		playerClicked = true;
	}
}
