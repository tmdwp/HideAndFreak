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

        /// <summary>가드 이동 속도(m/s).</summary>
        public float guardMoveSpeed;

        /// <summary>마네킹 기본 이동 속도(m/s). 사냥 시간에는 <see cref="GameTuning.MannequinHuntSpeedMultiplier"/>배가 된다.</summary>
        public float mannequinMoveSpeed;

        /// <summary>가드 총 한 발의 피해량.</summary>
        public int gunDamage;

        /// <summary>마네킹 공격 한 번의 피해량. (사냥 시간)</summary>
        public int mannequinAttackDamage;

        /// <summary>
        /// 코드에 적힌 기본값으로 채운 설정을 만든다.
        /// 실제 게임의 초기 설정은 Resources/Data/default_game_settings.json(<see cref="DefaultGameSettings"/>)을 따르며,
        /// 이 값은 그 파일을 읽지 못했을 때만 쓰인다.
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
                mannequinAttackRange = 2f,
                guardMoveSpeed = 2f,
                mannequinMoveSpeed = 1.5f,
                gunDamage = GameConstants.Damage,
                mannequinAttackDamage = GameConstants.Damage
            };
        }
    }
}
