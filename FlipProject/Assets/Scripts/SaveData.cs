using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using SimpleJSON;
using UnityEngine;

[Serializable]
public class SaveData
{
	private readonly string fullpath;
	private readonly Dictionary<string, bool> cleared;

	public SaveData(string filename)
	{
		fullpath = Path.Combine(Application.persistentDataPath, filename);
		cleared = new Dictionary<string, bool>();
		if (File.Exists(fullpath))
		{
			var parsed = JSON.Parse(File.ReadAllText(fullpath));
			var dict = parsed["cleared"];
			foreach (var kv in dict)
			{
				cleared[kv.Key] = kv.Value;
			}
		}
		else
		{
			Save();
		}
	}

	public void Save()
	{
		JSONObject jso = new JSONObject();
		JSONObject cleared = new JSONObject();
		foreach (var kv in this.cleared)
		{
			cleared.Add(kv.Key, kv.Value);
		}
		jso.Add("cleared", cleared);

		File.WriteAllText(fullpath, jso.ToString());
	}

	public bool IsCleared(string levelId)
	{
		return cleared.TryGetValue(levelId, out bool output) ? output : false;
	}

	public void SetCleared(string levelId)
	{
		cleared[levelId] = true;
	}
}
