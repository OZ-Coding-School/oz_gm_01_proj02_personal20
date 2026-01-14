using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuButtonController : MonoBehaviour
{
    [SerializeField] private List<MenuButtonView> buttons = new List<MenuButtonView>();
    [SerializeField] private int startIndex = 0;

    [Header("Navigation Layout")]
    [SerializeField] private int columns = 1;//1이면 세로목록,2이상이면 그리드
    [SerializeField] private bool mapLeftRightToUpDownWhenSingleColumn = true;//1열일때 좌=위,우=아래

    [Header("Key Settings")]
    [SerializeField] private KeyCode upKey = KeyCode.UpArrow;
    [SerializeField] private KeyCode downKey = KeyCode.DownArrow;
    [SerializeField] private KeyCode leftKey = KeyCode.LeftArrow;
    [SerializeField] private KeyCode rightKey = KeyCode.RightArrow;

    [SerializeField] private KeyCode confirmKey1 = KeyCode.Z;
    [SerializeField] private KeyCode confirmKey2 = KeyCode.Return;
    [SerializeField] private KeyCode cancelKey1 = KeyCode.X;
    [SerializeField] private KeyCode cancelKey2 = KeyCode.Escape;

    public event Action CancelRequested;

    private int currentIndex = -1;

    private void Start()
    {
        if (buttons.Count == 0) return;
        if (columns < 1) columns = 1;
        Select(Mathf.Clamp(startIndex, 0, buttons.Count - 1));
    }

    private void Update()
    {
        if (buttons.Count == 0) return;

        if (Input.GetKeyDown(upKey)) MoveUp();
        if (Input.GetKeyDown(downKey)) MoveDown();
        if (Input.GetKeyDown(leftKey)) MoveLeft();
        if (Input.GetKeyDown(rightKey)) MoveRight();

        if (Input.GetKeyDown(confirmKey1) || Input.GetKeyDown(confirmKey2))
            ExecuteCurrent();

        if (Input.GetKeyDown(cancelKey1) || Input.GetKeyDown(cancelKey2))
            OnCancel();
    }

    private void MoveUp()
    {
        if (columns <= 1)
        {
            Select(WrapIndex(currentIndex - 1));
            return;
        }

        int next = currentIndex - columns;
        if (next < 0) next = currentIndex;
        Select(next);
    }

    private void MoveDown()
    {
        if (columns <= 1)
        {
            Select(WrapIndex(currentIndex + 1));
            return;
        }

        int next = currentIndex + columns;
        if (next >= buttons.Count) next = currentIndex;
        Select(next);
    }

    private void MoveLeft()
    {
        if (columns <= 1)
        {
            if (mapLeftRightToUpDownWhenSingleColumn)
                Select(WrapIndex(currentIndex - 1));
            return;
        }

        int col = currentIndex % columns;
        if (col == 0) return;
        Select(currentIndex - 1);
    }

    private void MoveRight()
    {
        if (columns <= 1)
        {
            if (mapLeftRightToUpDownWhenSingleColumn)
                Select(WrapIndex(currentIndex + 1));
            return;
        }

        int col = currentIndex % columns;
        if (col == columns - 1) return;

        int next = currentIndex + 1;
        if (next >= buttons.Count) return;
        Select(next);
    }

    public void Select(int index)
    {
        if (index < 0 || index >= buttons.Count) return;
        if (currentIndex == index) return;

        if (currentIndex >= 0 && currentIndex < buttons.Count && buttons[currentIndex] != null)
            buttons[currentIndex].SetSelected(false);

        currentIndex = index;

        if (buttons[currentIndex] != null)
            buttons[currentIndex].SetSelected(true);

        if (EventSystem.current != null && buttons[currentIndex]?.Button != null)
            EventSystem.current.SetSelectedGameObject(buttons[currentIndex].Button.gameObject);
    }

    private void ExecuteCurrent()
    {
        if (currentIndex < 0 || currentIndex >= buttons.Count) return;

        var view = buttons[currentIndex];
        if (view == null || view.Button == null) return;

        view.Button.onClick.Invoke();
    }

    private void OnCancel()
    {
        UIManager.Instance.Back();//한단계뒤로
        CancelRequested?.Invoke();
    }

    private int WrapIndex(int index)
    {
        if (buttons.Count == 0) return 0;
        if (index < 0) return buttons.Count - 1;
        if (index >= buttons.Count) return 0;
        return index;
    }
}
