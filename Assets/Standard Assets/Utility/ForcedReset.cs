using System;
using UnityEngine;
using UnityStandardAssets.CrossPlatformInput;

// Unity 6에서 GUITexture가 제거되어 uGUI Image로 대체 (GuardVsMannequin 프로젝트 수정)
[RequireComponent(typeof (UnityEngine.UI.Image))]
public class ForcedReset : MonoBehaviour
{
    private void Update()
    {
        // if we have forced a reset ...
        if (CrossPlatformInputManager.GetButtonDown("ResetObject"))
        {
            //... reload the scene
            Application.LoadLevelAsync(Application.loadedLevelName);
        }
    }
}
