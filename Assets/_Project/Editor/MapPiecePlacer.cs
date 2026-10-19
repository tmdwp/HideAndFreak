using System.Collections.Generic;
using GuardVsMannequin.Core;
using GuardVsMannequin.Map;
using GuardVsMannequin.Props;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 직접 설계한 맵에 에셋(프리팹·모델)을 놓는 도구.
    /// - 에셋의 원점 위치와 상관없이 렌더러 경계 기준으로 "바닥 중심"을 지정한 위치에 맞춘다.
    /// - 구조물 모델의 머티리얼을 이름으로 바꿔 끼운다. (MapKit 기본 색 → 맵 테마 색)
    /// - 변신 사물·벽 후보를 모아 두었다가 정리(스크립트 제거) 뒤에 한꺼번에 등록한다.
    /// - 스폰·전원 장치 지점이 사물과 겹치지 않는 빈자리를 찾는다.
    /// 놓은 에셋의 실제 크기는 로그로 남겨 배치를 다듬을 때 참고한다.
    /// </summary>
    internal sealed class MapPiecePlacer
    {
        private readonly Dictionary<string, Material> materialRemap;
        private readonly Dictionary<string, Vector3> measuredSizes = new Dictionary<string, Vector3>();
        private readonly List<GameObject> propCandidates = new List<GameObject>();
        private readonly List<GameObject> wallCandidates = new List<GameObject>();

        /// <summary>찾지 못한 에셋 수.</summary>
        public int MissingCount { get; private set; }

        /// <param name="materialRemap">원래 머티리얼 이름 → 바꿀 머티리얼. 구조물(remap 지정)에만 적용한다.</param>
        public MapPiecePlacer(Dictionary<string, Material> materialRemap)
        {
            this.materialRemap = materialRemap ?? new Dictionary<string, Material>();
        }

        #region 배치

        /// <summary>
        /// 에셋을 놓는다. 경계의 바닥 중심이 floorCenter에 오도록 옮긴다. 에셋이 없으면 null.
        /// </summary>
        /// <param name="fitHeight">0보다 크면 높이가 이 값이 되도록 균일하게 키우거나 줄인다.</param>
        /// <param name="scale">에셋 원래 크기에 곱할 배율. (null이면 그대로)</param>
        /// <param name="remap">true면 머티리얼을 이름표대로 바꿔 끼운다.</param>
        public GameObject Place(string assetPath, Transform parent, Vector3 floorCenter, float yaw,
            float fitHeight = 0f, Vector3? scale = null, bool remap = false, string name = null)
        {
            GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (asset == null)
            {
                MissingCount++;
                Debug.LogWarning($"[MapPiecePlacer] 에셋을 찾지 못해 건너뜁니다: {assetPath}");
                return null;
            }

            var instance = PrefabUtility.InstantiatePrefab(asset, parent) as GameObject;
            if (instance == null)
                instance = Object.Instantiate(asset, parent);
            if (PrefabUtility.IsOutermostPrefabInstanceRoot(instance))
                PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            instance.name = name ?? asset.name;
            instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * asset.transform.rotation;
            if (scale.HasValue)
                instance.transform.localScale = Vector3.Scale(instance.transform.localScale, scale.Value);

            Bounds bounds = GetBounds(instance);
            if (!measuredSizes.ContainsKey(assetPath))
                measuredSizes[assetPath] = Quaternion.Euler(0f, -yaw, 0f) * bounds.size;
            if (fitHeight > 0f && bounds.size.y > 0.01f)
            {
                instance.transform.localScale *= fitHeight / bounds.size.y;
                bounds = GetBounds(instance);
            }

            instance.transform.position += floorCenter - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
            if (remap)
                RemapMaterials(instance);
            return instance;
        }

        /// <summary>
        /// 받침 물체의 윗면 위에 에셋을 놓는다. offset은 받침 경계 중심에서의 XZ 거리.
        /// </summary>
        public GameObject PlaceOn(string assetPath, GameObject support, Vector2 offset, float yaw, float fitHeight = 0f)
        {
            if (support == null)
                return null;
            Bounds top = GetBounds(support);
            var position = new Vector3(top.center.x + offset.x, top.max.y, top.center.z + offset.y);
            return Place(assetPath, support.transform.parent, position, yaw, fitHeight);
        }

        /// <summary>
        /// 놓은 오브젝트를 변신 사물 후보로 표시한다. (정리 뒤 <see cref="RegisterCandidates"/>에서 등록)
        /// </summary>
        public GameObject AsProp(GameObject placed)
        {
            if (placed != null)
                propCandidates.Add(placed);
            return placed;
        }

        /// <summary>
        /// 놓은 오브젝트를 벽 후보로 표시한다. (변신하면 고정 크기 벽 조각)
        /// </summary>
        public GameObject AsWall(GameObject placed)
        {
            if (placed != null)
                wallCandidates.Add(placed);
            return placed;
        }

        /// <summary>
        /// 모아 둔 후보를 등록한다. 너무 크거나 복사할 수 없는 부분(조명·파티클·스킨 메시)이 있는 사물은 건너뛴다.
        /// </summary>
        public void RegisterCandidates(out int propCount, out int wallCount, out int skipped)
        {
            propCount = 0;
            wallCount = 0;
            skipped = 0;
            foreach (GameObject wall in wallCandidates)
            {
                if (wall != null && PropRegistrar.Register(wall, isWall: true, absorbChildren: true))
                    wallCount++;
            }
            foreach (GameObject prop in propCandidates)
            {
                if (prop == null)
                    continue;
                Bounds bounds = GetBounds(prop);
                float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
                if (largest > GameTuning.DisguiseMaxSize || HasUncopiedParts(prop))
                {
                    skipped++;
                    continue;
                }
                if (PropRegistrar.Register(prop, isWall: false, absorbChildren: true))
                    propCount++;
            }
        }

        /// <summary>
        /// 처음 놓을 때 잰 에셋별 크기(회전 전 기준)를 한 줄씩 돌려준다.
        /// </summary>
        public string DescribeSizes()
        {
            var builder = new System.Text.StringBuilder();
            foreach (KeyValuePair<string, Vector3> entry in measuredSizes)
                builder.AppendLine($"{System.IO.Path.GetFileNameWithoutExtension(entry.Key)}: {entry.Value.x:F2} x {entry.Value.y:F2} x {entry.Value.z:F2}");
            return builder.ToString();
        }

        #endregion

        #region 경계·머티리얼

        /// <summary>
        /// 메시 렌더러(스킨 포함) 전체의 월드 경계. 파티클은 크기가 들쭉날쭉해 뺀다.
        /// </summary>
        public static Bounds GetBounds(GameObject root)
        {
            bool found = false;
            var bounds = new Bounds(root.transform.position, Vector3.zero);
            foreach (Renderer target in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(target is MeshRenderer) && !(target is SkinnedMeshRenderer))
                    continue;
                if (!found)
                {
                    bounds = target.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(target.bounds);
                }
            }
            return bounds;
        }

        /// <summary>
        /// 이름표에 있는 머티리얼을 바꿔 끼운다. (임포트된 모델의 머티리얼 이름 기준)
        /// </summary>
        private void RemapMaterials(GameObject instance)
        {
            foreach (Renderer target in instance.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = target.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materialRemap.TryGetValue(materials[i].name, out Material replacement))
                    {
                        materials[i] = replacement;
                        changed = true;
                    }
                }
                if (changed)
                    target.sharedMaterials = materials;
            }
        }

        /// <summary>
        /// 변신 외형으로 복사되지 않는 부분(파티클·조명·스킨 메시)이 있는지 확인한다.
        /// </summary>
        private static bool HasUncopiedParts(GameObject target)
        {
            return target.GetComponentInChildren<ParticleSystem>(true) != null
                   || target.GetComponentInChildren<Light>(true) != null
                   || target.GetComponentInChildren<SkinnedMeshRenderer>(true) != null;
        }

        #endregion

        #region 빈자리

        /// <summary>
        /// 원하는 위치 근처에서 사람이 설 수 있는 빈자리를 찾는다. 못 찾으면 원래 위치.
        /// (원하는 위치부터 0.5m 간격 고리로 넓혀 가며 찾는다. 충돌체가 동기화된 뒤에 호출한다)
        /// </summary>
        /// <param name="bodyRadius">비어 있어야 하는 기둥 반지름.</param>
        /// <param name="propClearance">0보다 크면 이 거리 안에 변신 사물(벽 제외)이 없어야 한다.</param>
        public static Vector3 FindFreeSpot(Vector3 desired, float bodyRadius, float propClearance, string label)
        {
            const int angleSteps = 12;
            for (float ring = 0f; ring <= 4f; ring += 0.5f)
            {
                int steps = ring <= 0f ? 1 : angleSteps;
                for (int i = 0; i < steps; i++)
                {
                    float angle = i * Mathf.PI * 2f / steps;
                    Vector3 candidate = desired + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * ring;
                    if (IsFree(candidate, bodyRadius, propClearance))
                        return candidate;
                }
            }
            Debug.LogWarning($"[MapPiecePlacer] {label}: {desired} 근처에서 빈자리를 찾지 못해 그대로 둡니다.");
            return desired;
        }

        /// <summary>
        /// 바닥이 있고, 사람 높이 기둥이 비어 있고, 가까운 변신 사물이 없는지 확인한다.
        /// </summary>
        private static bool IsFree(Vector3 point, float bodyRadius, float propClearance)
        {
            if (!Physics.Raycast(point + Vector3.up * 0.5f, Vector3.down, 1f))
                return false;

            Vector3 bottom = point + Vector3.up * (bodyRadius + 0.15f);
            Vector3 top = point + Vector3.up * Mathf.Max(bodyRadius + 0.15f, 1.9f - bodyRadius);
            if (Physics.CheckCapsule(bottom, top, bodyRadius, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                return false;

            if (propClearance <= 0f)
                return true;
            foreach (Collider nearby in Physics.OverlapSphere(point + Vector3.up * 0.8f, propClearance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                TransformableProp prop = nearby.GetComponentInParent<TransformableProp>();
                if (prop != null && !prop.IsWall)
                    return false;
            }
            return true;
        }

        #endregion

        #region 조명

        /// <summary>
        /// 천장에 붙는 판형 형광등(발광 판 + 포인트 조명)을 만든다. 사냥 시간에 꺼지는 MapLight이며 충돌체가 없다.
        /// </summary>
        public static GameObject CreatePanelLamp(Transform parent, Vector3 ceilingPoint, Color color, float intensity, float range,
            Material onMaterial, Material offMaterial, float yaw = 0f)
        {
            var lamp = new GameObject("PanelLamp");
            lamp.transform.SetParent(parent, false);
            lamp.transform.SetPositionAndRotation(ceilingPoint, Quaternion.Euler(0f, yaw, 0f));

            GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
            panel.name = "Bulb";
            Object.DestroyImmediate(panel.GetComponent<Collider>());
            panel.transform.SetParent(lamp.transform, false);
            panel.transform.localPosition = Vector3.down * 0.03f;
            panel.transform.localScale = new Vector3(1.4f, 0.05f, 0.3f);
            Renderer bulb = panel.GetComponent<Renderer>();
            bulb.sharedMaterial = onMaterial;
            bulb.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            AddPointLight(lamp.transform, Vector3.down * 0.3f, color, intensity, range);

            MapLight mapLight = lamp.AddComponent<MapLight>();
            TestEnvironmentSetupTool.SetField(mapLight, "bulbRenderer", bulb);
            TestEnvironmentSetupTool.SetField(mapLight, "onMaterial", onMaterial);
            TestEnvironmentSetupTool.SetField(mapLight, "offMaterial", offMaterial);
            return lamp;
        }

        /// <summary>
        /// 외형 없이 빛만 내는 MapLight를 만든다. (발광 장식 안쪽의 은은한 조명 등)
        /// </summary>
        public static GameObject CreateBareLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            var holder = new GameObject(name);
            holder.transform.SetParent(parent, false);
            holder.transform.position = position;
            AddPointLight(holder.transform, Vector3.zero, color, intensity, range);
            holder.AddComponent<MapLight>();
            return holder;
        }

        /// <summary>
        /// 그림자 없는 실시간 포인트 조명을 자식으로 붙인다.
        /// </summary>
        private static void AddPointLight(Transform parent, Vector3 localPosition, Color color, float intensity, float range)
        {
            var lightObject = new GameObject("PointLight");
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = localPosition;
            Light point = lightObject.AddComponent<Light>();
            point.type = LightType.Point;
            point.color = color;
            point.intensity = intensity;
            point.range = range;
            point.shadows = LightShadows.None;
            point.lightmapBakeType = LightmapBakeType.Realtime;
        }

        #endregion
    }
}
