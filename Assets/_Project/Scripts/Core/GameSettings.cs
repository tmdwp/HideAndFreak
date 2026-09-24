using System;

namespace GuardVsMannequin.Core
{
    /// <summary>
    /// 호스트가 룸 씬에서 조정하는 게임 설정값.
    /// 네트워크로 동기화되므로 직렬화 가능한 값 타입 필드만 사용한다.
    /// </summary>
    [Serializable]
    public struct GameSettings
    {
        public int mapId;
        public float preparationTime;
        public float playTime;
        public float huntTime;
        public int guardCount;
        public int objectiveCount;
        public int generatorCount;
        public float generatorDestroyTime;
        public float gunCooldown;
        public float detectorCooldown;
        public int guardMaxHealth;
        public int mannequinMaxHealth;
        public float huntHealthMultiplier;
        public float mannequinAttackRange;

        /// <summary>
        /// 개발 계획서(2.1)에 정의된 기본값으로 채운 설정을 만든다.
        /// </summary>
        public static GameSettings CreateDefault()
        {
            return new GameSettings
            {
                mapId = 0,
                preparationTime = 20f,
                playTime = 180f,
                huntTime = 60f,
                guardCount = 1,
                objectiveCount = 3,
                generatorCount = 5,
                generatorDestroyTime = 10f,
                gunCooldown = 3f,
                detectorCooldown = 10f,
                guardMaxHealth = 3,
                mannequinMaxHealth = 1,
                huntHealthMultiplier = 10f,
                mannequinAttackRange = 2f
            };
        }
    }
}
