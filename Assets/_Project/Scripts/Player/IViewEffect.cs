namespace GuardVsMannequin.Player
{
    /// <summary>
    /// 특정 플레이어의 시야에만 적용되는 화면 효과. (예: 사냥 시간 마네킹의 제한된 시야)
    /// 본인 화면뿐 아니라 그 플레이어를 관전하는 사람의 화면에도 똑같이 적용할 수 있다.
    /// </summary>
    public interface IViewEffect
    {
        /// <summary>효과를 이 클라이언트 화면에 적용한다.</summary>
        void Activate();

        /// <summary>효과를 이 클라이언트 화면에서 제거한다.</summary>
        void Deactivate();
    }
}
