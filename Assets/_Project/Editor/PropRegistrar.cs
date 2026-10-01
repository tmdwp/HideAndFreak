using System.Collections.Generic;
using System.Linq;
using GuardVsMannequin.Props;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 맵 오브젝트를 변신 가능 사물로 등록한다. (자동 맵 생성과 맵 제작 도우미가 함께 쓴다)
    /// - 사물: 그 메시·머티리얼·크기 그대로 변신 외형을 사물 목록(PropDatabase)에 추가한다. 같은 모양은 한 번만 추가한다.
    /// - 벽: 고정 크기 벽 조각(큐브 1.2×2×0.25)으로 변신한다. 머티리얼마다 한 번만 추가한다.
    /// 조준 판정을 위해 충돌체가 없으면 MeshCollider를 붙이고, 외곽선 표시를 위해 정적 배칭에서 뺀다.
    /// </summary>
    public sealed class PropRegistrar
    {
        private static readonly Vector3 WallPieceScale = new Vector3(1.2f, 2f, 0.25f);

        private readonly PropDatabase database;
        private readonly Dictionary<string, int> propIds = new();
        private readonly Dictionary<Material, int> wallIds = new();
        private Mesh cubeMesh;

        /// <summary>
        /// 사물 목록에 등록할 도우미를 만든다. 이미 있는 항목은 같은 모양이면 다시 쓴다.
        /// </summary>
        public PropRegistrar(PropDatabase database)
        {
            this.database = database;
            for (int id = 0; id < database.Count; id++)
            {
                if (!database.TryGet(id, out PropDefinition definition))
                    continue;
                if (definition.displayName != null && definition.displayName.StartsWith("WallPiece_") && definition.materials.Length > 0 && definition.materials[0] != null)
                    wallIds[definition.materials[0]] = id;
                else
                    propIds[Key(definition.mesh, definition.materials, definition.scale, definition.rotation)] = id;
            }
        }

        /// <summary>
        /// 렌더러를 그 모양 그대로의 변신 사물로 등록한다. 메시가 없으면 false.
        /// </summary>
        public bool RegisterProp(MeshRenderer target)
        {
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                return false;

            Vector3 scale = target.transform.lossyScale;
            Vector3 rotation = TiltOf(target.transform);
            string key = Key(filter.sharedMesh, target.sharedMaterials, scale, rotation);
            if (!propIds.TryGetValue(key, out int propId))
            {
                propId = database.Add(new PropDefinition
                {
                    displayName = target.name,
                    mesh = filter.sharedMesh,
                    materials = target.sharedMaterials,
                    scale = scale,
                    rotation = rotation
                });
                propIds[key] = propId;
            }

            Attach(target.gameObject, filter.sharedMesh, propId);
            return true;
        }

        /// <summary>
        /// 렌더러를 벽으로 등록한다. (변신하면 이 벽의 머티리얼을 입은 고정 크기 벽 조각이 된다)
        /// </summary>
        public bool RegisterWall(MeshRenderer target)
        {
            Material material = target.sharedMaterial;
            MeshFilter filter = target.GetComponent<MeshFilter>();
            if (material == null || filter == null || filter.sharedMesh == null)
                return false;

            if (!wallIds.TryGetValue(material, out int wallId))
            {
                wallId = database.Add(new PropDefinition
                {
                    displayName = $"WallPiece_{material.name}",
                    mesh = GetCubeMesh(),
                    materials = new[] { material },
                    scale = WallPieceScale
                });
                wallIds[material] = wallId;
            }

            Attach(target.gameObject, filter.sharedMesh, wallId);
            return true;
        }

        /// <summary>
        /// 변경한 사물 목록을 저장 대상으로 표시한다.
        /// </summary>
        public void Save()
        {
            EditorUtility.SetDirty(database);
        }

        /// <summary>
        /// 충돌체와 TransformableProp을 붙이고 정적 배칭에서 뺀다. (이미 있으면 사물 번호만 바꾼다)
        /// </summary>
        private static void Attach(GameObject target, Mesh mesh, int propId)
        {
            if (target.GetComponent<Collider>() == null)
                Undo.AddComponent<MeshCollider>(target).sharedMesh = mesh;

            TransformableProp prop = target.GetComponent<TransformableProp>();
            if (prop == null)
                prop = Undo.AddComponent<TransformableProp>(target);
            Undo.RecordObject(prop, "변신 사물 등록");
            prop.SetPropId(propId);
            EditorUtility.SetDirty(prop);

            StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(target);
            GameObjectUtility.SetStaticEditorFlags(target, flags & ~StaticEditorFlags.BatchingStatic);
        }

        /// <summary>
        /// 이미 등록된 사물의 기울기를 맵에 놓인 모습으로 다시 채운다. (기울기 저장 전에 만든 사물 목록 갱신용)
        /// 같은 사물 번호를 여러 오브젝트가 쓰면 처음 찾은 오브젝트 기준. 갱신한 개수를 반환한다.
        /// </summary>
        public int RefreshRotations(IEnumerable<TransformableProp> props)
        {
            var visited = new HashSet<int>();
            int updated = 0;
            foreach (TransformableProp prop in props)
            {
                if (!visited.Add(prop.PropId) || !database.TryGet(prop.PropId, out PropDefinition definition))
                    continue;
                if (definition.displayName != null && definition.displayName.StartsWith("WallPiece_"))
                    continue;

                Vector3 rotation = TiltOf(prop.transform);
                if (definition.rotation == rotation)
                    continue;
                definition.rotation = rotation;
                updated++;
            }
            return updated;
        }

        /// <summary>
        /// 오브젝트의 월드 회전에서 좌우 방향을 뺀 기울기(오일러 각). 거의 0인 값은 0으로 정리한다.
        /// </summary>
        private static Vector3 TiltOf(Transform target)
        {
            Vector3 euler = PropDefinition.ExtractTilt(target.rotation).eulerAngles;
            return new Vector3(Snap(euler.x), Snap(euler.y), Snap(euler.z));
        }

        /// <summary>
        /// 각도를 -180~180으로 바꾸고 0.1° 단위로 반올림한다. (부동소수 오차로 키가 갈라지지 않게)
        /// </summary>
        private static float Snap(float angle)
        {
            float normalized = Mathf.DeltaAngle(0f, angle);
            return Mathf.Round(normalized * 10f) / 10f;
        }

        /// <summary>
        /// 같은 모양인지 구분하는 키. (메시 + 머티리얼 + 크기 + 기울기)
        /// </summary>
        private static string Key(Mesh mesh, Material[] materials, Vector3 scale, Vector3 rotation)
        {
            string materialKey = string.Join(",", (materials ?? new Material[0]).Select(AssetKey));
            return $"{AssetKey(mesh)}|{materialKey}|{scale.x:F2},{scale.y:F2},{scale.z:F2}|{rotation.x:F1},{rotation.y:F1},{rotation.z:F1}";
        }

        /// <summary>
        /// 에셋 경로 + 이름. (에셋이 아니면 이름만)
        /// </summary>
        private static string AssetKey(Object asset)
        {
            return asset == null ? "null" : $"{AssetDatabase.GetAssetPath(asset)}:{asset.name}";
        }

        /// <summary>
        /// Unity 기본 큐브 메시를 한 번만 가져온다.
        /// </summary>
        private Mesh GetCubeMesh()
        {
            if (cubeMesh != null)
                return cubeMesh;

            GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cubeMesh = temp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(temp);
            return cubeMesh;
        }
    }
}
