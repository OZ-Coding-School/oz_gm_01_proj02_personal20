using UnityEngine;

// Unity MonoBehaviour singleton base.
// - If the instance should survive scene loads, the instance GameObject must be a root object.
public class Singleton<T> : MonoBehaviour where T : Singleton<T>
{
    [SerializeField] protected bool _IsDestroyOnLoad = true;

    protected static T _instance;
    public static T Instance => _instance;

    protected void Awake()
    {
        Init();
    }

    protected virtual void Init()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;

        if (_IsDestroyOnLoad)
        {
            //why: DontDestroyOnLoad only works for root GameObjects
            if (transform.parent != null)
            {
                transform.SetParent(null, true);
            }

            DontDestroyOnLoad(gameObject);
        }
    }

    protected virtual void OnDestroy()
    {
        Dispose();
    }

    protected virtual void Dispose()
    {
        if (_instance == this) _instance = null;
    }
}
