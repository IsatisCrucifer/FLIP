using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoteIconLineFitter : MonoBehaviour
{
	void Update()
	{
		float height = GetComponentInChildren<Text>().rectTransform.rect.height + 8f;
		Rect oldRect = GetComponent<RectTransform>().rect;
		GetComponent<RectTransform>().sizeDelta = new Vector2(oldRect.width, height);
	}
}
