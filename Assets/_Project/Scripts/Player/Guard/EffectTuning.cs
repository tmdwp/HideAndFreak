using UnityEngine;

namespace GuardVsMannequin.Player.Guard
{
    /// <summary>
    /// 생성한 파티클 이펙트의 밝기와 크기를 조정한다. (원본 프리팹은 바꾸지 않고 생성된 사본만 바꾼다)
    /// 밝기는 시작 색의 RGB와 알파를 함께 곱해, 가산 합성 이펙트도 연해지게 한다.
    /// </summary>
    public static class EffectTuning
    {
        /// <summary>
        /// 이펙트 오브젝트 아래 모든 파티클의 밝기와 크기(시작 크기)를 조정한다.
        /// </summary>
        /// <param name="effect">생성된 이펙트.</param>
        /// <param name="brightness">밝기 배율. 1 = 원본, 0.5 = 절반.</param>
        /// <param name="scale">크기 배율. 1 = 원본.</param>
        public static void Apply(GameObject effect, float brightness, float scale)
        {
            if (effect == null)
                return;

            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                ParticleSystem.MainModule main = particles.main;
                main.startColor = Scale(main.startColor, brightness);
                main.startSizeMultiplier *= scale;
            }
        }

        /// <summary>
        /// 시작 색(단색·두 색·그라디언트)의 RGB와 알파에 배율을 곱한다.
        /// </summary>
        private static ParticleSystem.MinMaxGradient Scale(ParticleSystem.MinMaxGradient source, float factor)
        {
            switch (source.mode)
            {
                case ParticleSystemGradientMode.Color:
                    return new ParticleSystem.MinMaxGradient(Scale(source.color, factor));
                case ParticleSystemGradientMode.TwoColors:
                    return new ParticleSystem.MinMaxGradient(Scale(source.colorMin, factor), Scale(source.colorMax, factor));
                case ParticleSystemGradientMode.Gradient:
                    return new ParticleSystem.MinMaxGradient(Scale(source.gradient, factor));
                case ParticleSystemGradientMode.TwoGradients:
                    return new ParticleSystem.MinMaxGradient(Scale(source.gradientMin, factor), Scale(source.gradientMax, factor));
                default:
                    return source;
            }
        }

        /// <summary>
        /// 색의 RGB와 알파에 배율을 곱한다.
        /// </summary>
        private static Color Scale(Color color, float factor)
        {
            return new Color(color.r * factor, color.g * factor, color.b * factor, color.a * factor);
        }

        /// <summary>
        /// 그라디언트 키의 색과 알파에 배율을 곱한 사본을 만든다.
        /// </summary>
        private static Gradient Scale(Gradient source, float factor)
        {
            if (source == null)
                return null;

            GradientColorKey[] colors = source.colorKeys;
            for (int i = 0; i < colors.Length; i++)
                colors[i].color = new Color(colors[i].color.r * factor, colors[i].color.g * factor, colors[i].color.b * factor);

            GradientAlphaKey[] alphas = source.alphaKeys;
            for (int i = 0; i < alphas.Length; i++)
                alphas[i].alpha *= factor;

            var result = new Gradient { mode = source.mode };
            result.SetKeys(colors, alphas);
            return result;
        }
    }
}
