using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
RunManager는씬/오브젝트에붙는MonoBehaviour컴포넌트다.
-게임흐름을중앙에서조율하고,다른시스템의진입점을제공한다.
-컴포넌트참조는Awake에서캐싱하고,null을가드한다.
-Update에서GC유발패턴을피한다.
*/
public class RunManager : MonoBehaviour
{
    
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }
}
