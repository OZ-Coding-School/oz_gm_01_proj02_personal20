using System.Collections;
using UnityEngine;

/*
ShopScreenController는RunState에따라상점UI를켜고끈다
-InShopOrReward가되면로그가끝난뒤상점을연다
*/
public sealed class ShopScreenController : MonoBehaviour
{
    [SerializeField] private GameObject shopRoot;//상점루트(평소OFF)
    [SerializeField] private BattleLogUI battleLogUI;//로그UI(선택)

    private RunManager runManager;
    private Coroutine routine;

    //OnEnable은RunManager상태변화를구독한다
    private void OnEnable()
    {
        runManager = RunManager.Instance;
        if (runManager == null) return;

        runManager.OnStateChanged -= OnStateChanged;
        runManager.OnStateChanged += OnStateChanged;
    }

    //OnDisable은구독을해제한다
    private void OnDisable()
    {
        if (runManager != null)
        {
            runManager.OnStateChanged -= OnStateChanged;
        }

        runManager = null;
        if (routine != null) StopCoroutine(routine);
        routine = null;
    }

    //OnStateChanged는상태에따라상점을연다
    private void OnStateChanged(RunState state)
    {
        if (state != RunState.InShopOrReward) return;

        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(CoOpenAfterLog());
    }

    //CoOpenAfterLog는로그가끝난뒤상점을연다
    private IEnumerator CoOpenAfterLog()
    {
        if (battleLogUI != null)
        {
            while (battleLogUI.IsBusy)
            {
                yield return null;
            }
        }

        if (shopRoot != null)
        {
            shopRoot.SetActive(true);
        }
    }
}
