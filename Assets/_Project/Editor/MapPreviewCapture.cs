using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 맵을 임시 카메라로 찍어 PNG로 저장한다. (맵 빌더가 만든 결과를 Unity를 열지 않고 확인하기 위함)
    /// 저장 위치는 프로젝트의 Temp 폴더라 버전 관리에 들어가지 않는다. 카메라는 찍은 뒤 바로 지운다.
    /// </summary>
    internal static class MapPreviewCapture
    {
        private const int Width = 1600;
        private const int Height = 1000;

        /// <summary>한 장면: 이름, 카메라 위치, 바라볼 점.</summary>
        public readonly struct Shot
        {
            public readonly string name;
            public readonly Vector3 position;
            public readonly Vector3 target;

            public Shot(string name, Vector3 position, Vector3 target)
            {
                this.name = name;
                this.position = position;
                this.target = target;
            }
        }

        /// <summary>
        /// 위에서 내려다본 배치도(천장 숨김, 밝은 환경광)와 눈높이 장면들을 찍어 folder에 저장한다. 저장한 파일 경로를 돌려준다.
        /// </summary>
        /// <param name="hideFromTop">배치도를 찍는 동안 숨길 렌더러. (천장 등)</param>
        public static List<string> Capture(string folder, Bounds area, IReadOnlyList<Renderer> hideFromTop, IReadOnlyList<Shot> shots)
        {
            Directory.CreateDirectory(folder);
            var saved = new List<string>();
            var cameraObject = new GameObject("PreviewCamera (임시)");
            try
            {
                Camera camera = cameraObject.AddComponent<Camera>();
                camera.nearClipPlane = 0.05f;
                camera.farClipPlane = 200f;
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;

                saved.Add(CaptureTopDown(camera, folder, area, hideFromTop));
                camera.orthographic = false;
                camera.fieldOfView = 75f;
                foreach (Shot shot in shots)
                {
                    camera.transform.SetPositionAndRotation(shot.position, Quaternion.LookRotation(shot.target - shot.position));
                    saved.Add(Render(camera, Path.Combine(folder, shot.name + ".png")));
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
            }
            return saved;
        }

        /// <summary>
        /// 천장을 숨기고 환경광을 밝힌 채 위에서 정사영으로 찍는다. 끝나면 원래대로 돌린다.
        /// </summary>
        private static string CaptureTopDown(Camera camera, string folder, Bounds area, IReadOnlyList<Renderer> hidden)
        {
            AmbientMode ambientMode = RenderSettings.ambientMode;
            Color ambientLight = RenderSettings.ambientLight;
            var wasEnabled = new List<bool>();
            foreach (Renderer target in hidden)
            {
                wasEnabled.Add(target.enabled);
                target.enabled = false;
            }
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.75f, 0.75f, 0.75f);

            try
            {
                camera.orthographic = true;
                camera.orthographicSize = Mathf.Max(area.extents.z, area.extents.x * Height / Width) + 1f;
                camera.transform.SetPositionAndRotation(new Vector3(area.center.x, area.max.y + 30f, area.center.z), Quaternion.Euler(90f, 0f, 0f));
                return Render(camera, Path.Combine(folder, "00_TopDown.png"));
            }
            finally
            {
                RenderSettings.ambientMode = ambientMode;
                RenderSettings.ambientLight = ambientLight;
                for (int i = 0; i < hidden.Count; i++)
                    hidden[i].enabled = wasEnabled[i];
            }
        }

        /// <summary>
        /// 카메라 한 장을 렌더 텍스처에 그려 PNG로 저장한다.
        /// </summary>
        private static string Render(Camera camera, string path)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            try
            {
                var request = new RenderPipeline.StandardRequest { destination = target };
                if (RenderPipeline.SupportsRenderRequest(camera, request))
                {
                    RenderPipeline.SubmitRenderRequest(camera, request);
                }
                else
                {
                    camera.targetTexture = target;
                    camera.Render();
                    camera.targetTexture = null;
                }

                RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                image.Apply();
                File.WriteAllBytes(path, image.EncodeToPNG());
                return path;
            }
            finally
            {
                RenderTexture.active = previous;
                Object.DestroyImmediate(image);
                target.Release();
                Object.DestroyImmediate(target);
            }
        }
    }
}
