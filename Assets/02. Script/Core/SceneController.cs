using UnityEngine;
using UnityEngine.SceneManagement;

/*
GameManager를 통해 씬 전환만 호출하는 버튼 컨트롤러.
-씬 이름은 GameManager가 관리한다.
*/
public class SceneController : MonoBehaviour
{
    public void OnClickGoLobby()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 없어.BootScene부터 실행해줘.");
            return;
        }

        GameManager.Instance.LoadLobby();
    }

    public void OnClickGoGame()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance가 없어.BootScene부터 실행해줘.");
            return;
        }

        GameManager.Instance.LoadGame();
    }
}