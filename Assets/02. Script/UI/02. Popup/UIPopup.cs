using UnityEngine;
using static EnumData;

/*
모든 팝업 UI의 베이스 클래스
 - 씬에 미리 배치된 팝업 인스턴스를 전제로 함
 - Instantiate / Destroy 책임 없음
 - UIManager가 Open/Close 타이밍만 제어
*/
public abstract class UIPopup : MonoBehaviour
{
    [Header("Popup Identity")]
    //UIManager에서 구분하기 위한 고유 ID
    //EnumData 안에 있는 PopupId 사용
    [SerializeField] private EnumData.PopupId popupId;
    public EnumData.PopupId PopupId => popupId;

    //최초 1회 초기화 여부
    private bool initialized;

    //팝업 열기(UIManager에서 호출)
    public void Open()
    {
        //최초 1회만 초기화
        if (!initialized)
        {
            initialized = true;
            OnInit();
        }

        //활성화 후 열기 처리
        gameObject.SetActive(true);
        OnOpen();
    }

    //팝업 닫기(UIManager에서 호출)
    public void Close()
    {
        OnClose();
        gameObject.SetActive(false);
    }

    //Override 포인트

    //최초 1회만 호출
    //버튼 바인딩, 초기 값 세팅 용도
    protected virtual void OnInit() { }

    //팝업이 열릴 때마다 호출
    protected virtual void OnOpen() { }

    //팝업이 닫힐 때마다 호출
    protected virtual void OnClose() { }
}
