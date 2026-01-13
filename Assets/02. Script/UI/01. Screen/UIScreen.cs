using UnityEngine;
using static EnumData;

/*
UIScreen은ScreenRoot아래에배치되는큰화면UI의공통베이스다.
-UIManager가등록후Show/Hide를호출한다.
-씬전환/버튼입력에의해화면이교체될수있다.
*/
public class UIScreen : MonoBehaviour
{
    [SerializeField] private ScreenId screenId;//스크린식별자

    private bool initialized;//초기화여부

    public ScreenId ScreenId => screenId;

    public void Show()
    {
        if (!initialized)
        {
            initialized = true;
            OnInit();
        }

        gameObject.SetActive(true);
        OnShow();
    }

    public void Hide()
    {
        OnHide();
        gameObject.SetActive(false);
    }

    protected virtual void OnInit() { }
    protected virtual void OnShow() { }
    protected virtual void OnHide() { }
}
