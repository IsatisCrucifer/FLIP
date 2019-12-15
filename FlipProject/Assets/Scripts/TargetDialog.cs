using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class TargetDialog : MonoBehaviour
{
	public Text TargetText;

	private void Awake()
	{
		this.CheckInspectorConnection();
	}

	public void Show()
	{
		gameObject.SetActive(true);
	}

	private void Update()
	{
		if (Input.anyKey)
			gameObject.SetActive(false);
	}

	public void SetTargetText(string text)
	{
		TargetText.text = text;
	}

}
