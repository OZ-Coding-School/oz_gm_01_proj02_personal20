using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/*
MenuNavigator는세로메뉴선택이동을처리하는MonoBehaviour컴포넌트다.
-↑/↓및W/S입력으로선택항목을이동한다.
-Z/Space/Enter로선택을확정한다.
-선택항목에맞춰왼쪽화살표RectTransform을이동시킨다.
*/
public class MenuNavigator : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private RectTransform[] items;//메뉴항목루트배열
    [SerializeField] private RectTransform arrow;//화살표루트
    [SerializeField] private int startIndex;//시작인덱스
    [SerializeField] private bool wrap = true;//순환이동

    [Header("ArrowPlacement")]
    [SerializeField] private float arrowXOffset = -40f;//화살표X오프셋
    [SerializeField] private float arrowYOffset;//화살표Y오프셋
    [SerializeField] private float moveAnimSeconds = 0.08f;//이동애니시간

    [Header("HoldRepeat")]
    [SerializeField] private bool enableHoldRepeat = true;//홀드반복사용
    [SerializeField] private float holdRepeatDelay = 0.35f;//홀드시작지연
    [SerializeField] private float holdRepeatInterval = 0.09f;//홀드간격

    [Header("Events")]
    [SerializeField] private UnityEvent<int> onSubmitIndex;//선택이벤트

    private int selectedIndex;//현재선택
    private Coroutine moveRoutine;//이동코루틴
    private float holdTimer;//홀드타이머
    private int holdDir;//홀드방향(-1,1,0)

    private void Awake()
    {
        if (items == null || items.Length == 0)
        {
            Debug.LogError("//MenuNavigator items empty");
            enabled = false;
            return;
        }

        if (arrow == null)
        {
            Debug.LogError("//MenuNavigator arrow missing");
            enabled = false;
            return;
        }

        selectedIndex = Mathf.Clamp(startIndex, 0, items.Length - 1);
    }

    private void OnEnable()
    {
        MoveArrowToSelected(true);
        ResetHold();
    }

    private void Update()
    {
        HandleMove();
        HandleSubmit();
    }

    public int GetSelectedIndex()
    {
        return selectedIndex;
    }

    public void SetSelectedIndex(int index, bool immediateArrow = false)
    {
        int clamped = Mathf.Clamp(index, 0, items.Length - 1);
        if (clamped == selectedIndex)
        {
            return;
        }

        selectedIndex = clamped;
        MoveArrowToSelected(immediateArrow);
    }

    private void HandleMove()
    {
        int down = 0;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
        {
            down = -1;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
        {
            down = 1;
        }

        if (down != 0)
        {
            Step(down);
            ResetHold();
            holdDir = down;
            return;
        }

        if (!enableHoldRepeat)
        {
            return;
        }

        bool upHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool downHeld = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

        int dir = 0;
        if (upHeld && !downHeld)
        {
            dir = -1;
        }
        else if (downHeld && !upHeld)
        {
            dir = 1;
        }

        if (dir == 0)
        {
            ResetHold();
            return;
        }

        if (holdDir != dir)
        {
            ResetHold();
            holdDir = dir;
        }

        holdTimer += Time.unscaledDeltaTime;

        if (holdTimer < holdRepeatDelay)
        {
            return;
        }

        float afterDelay = holdTimer - holdRepeatDelay;
        if (afterDelay >= holdRepeatInterval)
        {
            holdTimer = holdRepeatDelay;
            Step(dir);
        }
    }

    private void HandleSubmit()
    {
        if (Input.GetKeyDown(KeyCode.Z) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            onSubmitIndex?.Invoke(selectedIndex);
        }
    }

    private void Step(int dir)
    {
        int next = selectedIndex + dir;

        if (wrap)
        {
            if (next < 0)
            {
                next = items.Length - 1;
            }
            else if (next >= items.Length)
            {
                next = 0;
            }
        }
        else
        {
            next = Mathf.Clamp(next, 0, items.Length - 1);
        }

        if (next == selectedIndex)
        {
            return;
        }

        selectedIndex = next;
        MoveArrowToSelected(false);
    }

    private void MoveArrowToSelected(bool immediate)
    {
        RectTransform target = items[selectedIndex];
        if (target == null)
        {
            return;
        }

        Vector2 targetPos = new Vector2(target.anchoredPosition.x + arrowXOffset, target.anchoredPosition.y + arrowYOffset);

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        if (immediate || moveAnimSeconds <= 0f)
        {
            arrow.anchoredPosition = targetPos;
            return;
        }

        moveRoutine = StartCoroutine(MoveArrowRoutine(targetPos, moveAnimSeconds));
    }

    private IEnumerator MoveArrowRoutine(Vector2 targetPos, float seconds)
    {
        Vector2 start = arrow.anchoredPosition;

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / seconds);
            arrow.anchoredPosition = Vector2.Lerp(start, targetPos, p);
            yield return null;
        }

        arrow.anchoredPosition = targetPos;
        moveRoutine = null;
    }

    private void ResetHold()
    {
        holdTimer = 0f;
        holdDir = 0;
    }
}
