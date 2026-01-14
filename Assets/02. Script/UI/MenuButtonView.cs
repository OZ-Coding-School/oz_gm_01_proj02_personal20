using UnityEngine;
using UnityEngine.UI;

public class MenuButtonView : MonoBehaviour
{
    [SerializeField] private GameObject arrowObject;//버튼 안의 ▶오브젝트(이미지)
    [SerializeField] private Button button;//같은 오브젝트의 Button

    public Button Button => button;

    private void Reset()
    {
        //에디터에서 컴포넌트 붙이면 자동 세팅 시도
        button = GetComponent<Button>();
    }

    private void Awake()
    {
        if (button == null) button = GetComponent<Button>();
        if (arrowObject != null) arrowObject.SetActive(false);//기본 비활성화
    }

    public void SetSelected(bool selected)
    {
        if (arrowObject == null) return;
        arrowObject.SetActive(selected);
    }
}
