using GuardVsMannequin.Map;
using GuardVsMannequin.Objectives;
using GuardVsMannequin.Props;
using UnityEditor;
using UnityEngine;

namespace GuardVsMannequin.Editor
{
    /// <summary>
    /// 현재 씬의 변신 가능 사물 설정을 검증한다.
    /// 변신 불가 대상(천장, 바닥, 조명 기구, 전원 장치와 그 주변 구조물)에 TransformableProp이 붙어 있는지 확인한다.
    /// </summary>
    public static class TransformablePropValidator
    {
        /// <summary>전원 장치 후보 위치로부터 이 거리 안의 사물은 "주변 구조물"로 보고 변신 불가로 취급한다.</summary>
        private const float GeneratorSurroundRadius = 1.5f;

        /// <summary>
        /// 검증을 실행하고 결과를 콘솔에 출력한다.
        /// </summary>
        [MenuItem("GuardVsMannequin/변신 가능 사물 검증")]
        public static void Validate()
        {
            TransformableProp[] props = Object.FindObjectsByType<TransformableProp>();
            GeneratorSpawnPoint[] generatorPoints = Object.FindObjectsByType<GeneratorSpawnPoint>();
            int errorCount = 0;

            foreach (TransformableProp prop in props)
            {
                string reason = GetViolation(prop, generatorPoints);
                if (reason == null)
                    continue;

                errorCount++;
                Debug.LogError($"[PropValidator] {GetPath(prop.transform)}: {reason}", prop);
            }

            if (errorCount == 0)
                Debug.Log($"[PropValidator] 변신 가능 사물 {props.Length}개 검증 통과");
            else
                Debug.LogError($"[PropValidator] 변신 가능 사물 {props.Length}개 중 {errorCount}개 문제 발견");
        }

        /// <summary>
        /// 규칙 위반 사유를 반환한다. 문제가 없으면 null.
        /// </summary>
        private static string GetViolation(TransformableProp prop, GeneratorSpawnPoint[] generatorPoints)
        {
            if (prop.PropId < 0)
                return "PropId가 지정되지 않았습니다.";

            if (prop.GetComponentInChildren<Light>() != null)
                return "조명 기구는 변신 대상이 될 수 없습니다.";

            if (prop.GetComponentInParent<Generator>() != null)
                return "전원 장치는 변신 대상이 될 수 없습니다.";

            string lowerName = prop.name.ToLowerInvariant();
            if (lowerName.Contains("floor") || lowerName.Contains("ceiling"))
                return "천장과 바닥은 변신 대상이 될 수 없습니다.";

            foreach (GeneratorSpawnPoint point in generatorPoints)
            {
                Collider collider = prop.GetComponent<Collider>();
                Vector3 closest = collider != null ? collider.ClosestPoint(point.transform.position) : prop.transform.position;
                if (Vector3.Distance(closest, point.transform.position) <= GeneratorSurroundRadius)
                    return "전원 장치 주변 구조물은 변신 대상이 될 수 없습니다.";
            }

            return null;
        }

        /// <summary>
        /// 계층 경로 문자열을 만든다.
        /// </summary>
        private static string GetPath(Transform target)
        {
            return target.parent == null ? target.name : $"{GetPath(target.parent)}/{target.name}";
        }
    }
}
