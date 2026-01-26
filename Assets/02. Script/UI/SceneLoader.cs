using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoader : Singleton<SceneLoader>
{
    protected override void Init()
    {
        base.Init();
    }

    public void LoadScene(EnumData.sceneType sct)
    {
        //enum으로 정의된 씬 로드
        //씬 이름이 '01. BootScene'처럼 숫자/점/공백이 포함되므로 GetSceneName 매핑을 사용한다.
        Time.timeScale = 1f;

        var sceneName = EnumData.GetSceneName(sct);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"SceneLoader.LoadScene: invalid sceneType={sct}");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }


    public void ReloadScene()
    {
        //해당 씬 리로드

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    //비동기 로드 함수
    public AsyncOperation LoadSceneAsync(EnumData.sceneType sct)
    {
        Time.timeScale = 1f;

        var sceneName = EnumData.GetSceneName(sct);
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"SceneLoader.LoadSceneAsync: invalid sceneType={sct}");
            return null;
        }

        return SceneManager.LoadSceneAsync(sceneName);
    }


}
