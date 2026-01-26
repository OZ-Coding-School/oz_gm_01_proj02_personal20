using UnityEngine;

/*
ScreenRegistry는ScreenRoot아래UIScreen들을자동으로UIManager에등록한다.
-비활성포함탐색후등록한다.
-등록후UIManager가현재씬에맞는Screen을선택해표시한다.
*/
public class ScreenRegistry : MonoBehaviour
{
    [SerializeField] private bool registerOnAwake = true;//Awake등록여부

    private void Awake()
    {
        if (!registerOnAwake)
        {
            return;
        }

        RegisterAll();
    }

    public void RegisterAll()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        UIScreen[] screens = GetComponentsInChildren<UIScreen>(true);
        for (int i = 0; i < screens.Length; i++)
        {
            UIScreen s = screens[i];
            if (s == null)
            {
                continue;
            }

            UIManager.Instance.RegisterScreen(s);
        }

        UIManager.Instance.ApplyScreenForActiveScene();
    }
}
