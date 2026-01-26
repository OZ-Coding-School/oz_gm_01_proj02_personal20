using UnityEngine;

//PopupPanel 아래에 붙여두면
//하위에 있는 모든 UIPopup을 자동으로 UIManager에 등록
public class PopupRegistry : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("//PopupRegistry Start");

        if (UIManager.Instance == null)
        {
            Debug.LogError("//UIManager.Instance == null in PopupRegistry(Start)");
            return;
        }

        //비활성화된 팝업까지 전부 검색
        UIPopup[] popups = GetComponentsInChildren<UIPopup>(true);
        Debug.Log($"//Popups Found Count: {popups.Length}");

        for (int i = 0; i < popups.Length; i++)
        {
            Debug.Log($"//Found Popup: {popups[i].name}");
            Debug.Log($"//Found PopupId: {popups[i].PopupId}");
            UIManager.Instance.RegisterPopup(popups[i]);
        }
    }
}
