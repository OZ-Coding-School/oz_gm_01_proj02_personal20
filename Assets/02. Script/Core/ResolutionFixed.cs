using UnityEngine;

/*
ResolutionFixed는씬/오브젝트에붙는MonoBehaviour컴포넌트다.
-외부에서는Refresh을호출해이기능을사용한다.
-컴포넌트참조는Awake에서캐싱하고,null을가드한다.
-Update에서GC유발패턴을피한다.
*/
public class ResolutionFixed : MonoBehaviour
{
    [Header("Target(16:9)")]
    [SerializeField] private int targetWidth = 1920;//목표 너비
    [SerializeField] private int targetHeight = 1080;//목표 높이

    [Header("Apply(SetResolution)")]
    [SerializeField] private bool applySetResolutionOnStart = true;//시작 시 1회 SetResolution 요청
    [SerializeField] private FullScreenMode fullScreenMode = FullScreenMode.FullScreenWindow;//PC에서 권장

    [Header("Camera")]
    [SerializeField] private Camera targetCamera;//비율 보정 적용 대상(비우면 Camera.main)
    [SerializeField] private Color barColor = Color.black;//빈 영역 색

    private int lastWidth;//변경 감지용 캐시
    private int lastHeight;//변경 감지용 캐시
    private FullScreenMode lastMode;//변경 감지용 캐시

    private Camera backgroundCamera;//빈 영역을 항상 검정으로 클리어하는 카메라

    private void Awake()
    {
        //카메라가 지정되지 않았으면 MainCamera를 한 번만 캐싱한다
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        //PC/콘솔 계열에서만 해상도 요청을 의미있게 적용한다(모바일은 OS 정책상 무시될 수 있음)
        if (applySetResolutionOnStart)
        {
            ApplyTargetResolution();
        }

        EnsureBackgroundCamera();
        ApplyViewport();
        CacheScreenState();
    }

    private void Update()
    {
        //창 크기 변경/전체화면 모드 전환 등을 감지해서 다시 비율을 맞춘다
        if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.fullScreenMode != lastMode)
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            EnsureBackgroundCamera();
            ApplyViewport();
            CacheScreenState();
        }
    }

    //외부에서 강제로 재적용하고 싶을 때 호출한다(옵션 메뉴 등)
    public void Refresh()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        EnsureBackgroundCamera();
        ApplyViewport();
        CacheScreenState();
    }

    //PC에서만 목표 해상도를 요청한다(실제 적용은 OS/드라이버/모드에 따라 달라질 수 있음)
    private void ApplyTargetResolution()
    {
        if (targetWidth <= 0 || targetHeight <= 0)
        {
            Debug.LogError("ResolutionFixed:targetWidth/targetHeight가 올바르지 않아.");
            return;
        }

        if (Application.isMobilePlatform)
        {
            return;
        }

        Screen.SetResolution(targetWidth, targetHeight, fullScreenMode);
    }

    //현재 화면 상태 저장(변경 감지에 사용)
    private void CacheScreenState()
    {
        lastWidth = Screen.width;
        lastHeight = Screen.height;
        lastMode = Screen.fullScreenMode;
    }

    //레터/필러박스로 카메라 viewport를 조정해 목표 비율을 유지한다
    private void ApplyViewport()
    {
        if (targetCamera == null)
        {
            Debug.LogError("ResolutionFixed:targetCamera를 찾지 못했어.");
            return;
        }

        float targetAspect = targetWidth / (float)targetHeight;
        float windowAspect = Screen.width / (float)Screen.height;

        //기기(창)가 더 가로로 길면 좌우가 남으니 필러박스(좌우 검정)
        if (windowAspect > targetAspect)
        {
            float newWidth = targetAspect / windowAspect;
            targetCamera.rect = new Rect((1f - newWidth) * 0.5f, 0f, newWidth, 1f);
            return;
        }

        //기기(창)가 더 세로로 길면 상하가 남으니 레터박스(상하 검정)
        if (windowAspect < targetAspect)
        {
            float newHeight = windowAspect / targetAspect;
            targetCamera.rect = new Rect(0f, (1f - newHeight) * 0.5f, 1f, newHeight);
            return;
        }

        //비율이 같으면 전체 화면 사용
        targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
    }

    //카메라 rect로 생긴 빈 영역에 잔상이 남지 않도록 검정 배경 카메라를 만든다
    private void EnsureBackgroundCamera()
    {
        if (backgroundCamera != null)
        {
            backgroundCamera.backgroundColor = barColor;
            return;
        }

        if (targetCamera == null)
        {
            return;
        }

        GameObject go = new GameObject("ResolutionFixed_BackgroundCamera");
        DontDestroyOnLoad(go);

        backgroundCamera = go.AddComponent<Camera>();
        backgroundCamera.depth = targetCamera.depth - 100f; //항상 먼저 클리어
        backgroundCamera.cullingMask = 0;                   //아무것도 렌더링하지 않음
        backgroundCamera.clearFlags = CameraClearFlags.SolidColor;  //단색으로 전체 화면 클리어
        backgroundCamera.backgroundColor = barColor;
        backgroundCamera.rect = new Rect(0f, 0f, 1f, 1f);   //전체 화면
    }
}
