using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameplayManager : MonoBehaviour
{
    [SerializeField] InputManager input;

    private void Start()
    {
        if (input != null)
        {
            input.OnInputMainMenu += BackToMainMenu;
        }
    }

    private void OnDestroy()
    {
        if (input != null)
        {
            input.OnInputMainMenu -= BackToMainMenu;
        }
    }

    void BackToMainMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        SceneManager.LoadScene("MainMenu");
    }
}
