using UnityEngine;
using UnityEngine.SceneManagement;

public class ButtonScript : MonoBehaviour
{
	public void LoadScene(string scene)
	{
		SceneManager.LoadScene(scene);
	}

	public void ExitApp()
	{
		Application.Quit();
	}
}
