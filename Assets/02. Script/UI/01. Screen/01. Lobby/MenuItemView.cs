/*
MenuItemView는메뉴항목의선택상태표현을담당한다.
-선택시스케일/알파를조정해강조한다.
*/
using UnityEngine;
using UnityEngine.UI;

public sealed class MenuItemView : MonoBehaviour
{
    [SerializeField] private bool useScale = true;//스케일강조사용
    [SerializeField] private Vector3 selectedScale = new Vector3(1.05f, 1.05f, 1f);//선택스케일
    [SerializeField] private Vector3 normalScale = Vector3.one;//기본스케일

    [SerializeField] private bool useAlpha = false;//알파강조사용
    [SerializeField] private float selectedAlpha = 1f;//선택알파
    [SerializeField] private float normalAlpha = 0.75f;//기본알파
    [SerializeField] private Graphic[] graphics;//대상그래픽들

    public void SetSelected(bool isSelected)
    {
        if (useScale)
        {
            transform.localScale = isSelected ? selectedScale : normalScale;
        }

        if (useAlpha)
        {
            ApplyAlpha(isSelected ? selectedAlpha : normalAlpha);
        }
    }

    private void ApplyAlpha(float a)
    {
        if (graphics == null || graphics.Length == 0)
        {
            return;
        }

        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null)
            {
                continue;
            }

            Color c = g.color;
            c.a = a;
            g.color = c;
        }
    }
}
