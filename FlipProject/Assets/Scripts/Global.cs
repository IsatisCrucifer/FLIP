using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Global data that shared between scenes.
public static class Global
{
	// Current save data.
	public static SaveData currentSave = null;
	// Current level. Set by LevelSelectControl. and used by BoardScript.
	public static TextAsset levelAsset = null;
	public static string levelId = null;
}
