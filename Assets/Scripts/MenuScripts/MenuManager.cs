using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public void PlayGame()
    {
        SceneManager.LoadScene("Level1");
    }

    public void PlayTutorial()
    {
        SceneManager.LoadScene("TutorialScene");
    }

    public void OpenSettings()
    {
        Debug.Log("Open settings");
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}