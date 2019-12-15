using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using MoonSharp.Interpreter;

public class LevelSelectControl : MonoBehaviour
{
	// Unity Objects
	public Transform ScrollContent;
	public GameObject ChapterTitlePrefab, ChapterLevelContainerPrefab, LevelButtonPrefab;

	void Awake()
	{
		this.CheckInspectorConnection();

		if (Global.currentSave == null)
		{
			Global.currentSave = new SaveData("TestSave.json");
		}
	}

	void Start()
	{
		Script luaEnvironment = new Script();
		TextAsset allLevel = Resources.Load<TextAsset>("Level/Levels");

		GameObject CurrentLevelContainer = null;

		luaEnvironment.Globals["AddGroup"] = (Action<string>)((string groupTitle) =>
		{
			GameObject title = Instantiate(ChapterTitlePrefab, ScrollContent);
			title.GetComponent<Text>().text = groupTitle;

			CurrentLevelContainer = null;
		});

		luaEnvironment.Globals["AddLevel"] = (Action<int, string>)((int levelNumber, string levelId) =>
		{
			if (CurrentLevelContainer == null)
				CurrentLevelContainer = Instantiate(ChapterLevelContainerPrefab, ScrollContent);

			GameObject LevelButton = Instantiate(LevelButtonPrefab, CurrentLevelContainer.transform);
			LevelButton.GetComponentInChildren<Text>().text = levelNumber.ToString();
			bool IsThisLevelCleared = Global.currentSave.IsCleared(levelId);
			LevelButton.GetComponent<Image>().color = IsThisLevelCleared ? Color.green : Color.red;
			LevelButton.GetComponent<Button>().onClick.AddListener(() =>
			{
				string levelAssetName = "Level/" + levelNumber.ToString() + "_" + levelId;
				TextAsset asset = Resources.Load<TextAsset>(levelAssetName);
				if (asset is null)
				{
					Debug.LogError($"Cannot find level asset {levelAssetName}!");
				}
				else
				{
					Global.levelAsset = asset;
					Global.levelId = levelId;
					SceneManager.LoadScene("BoardScreen");
				}
			});
		});

		luaEnvironment.DoString(allLevel.text);
	}
}
