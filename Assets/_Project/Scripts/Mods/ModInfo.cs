using System.IO;

namespace GuardVsMannequin.Mods
{
    /// <summary>모드 하나의 상태.</summary>
    public enum ModStatus : byte
    {
        /// <summary>켜져 있고 쓸 수 있다.</summary>
        Active,

        /// <summary>플레이어가 껐다.</summary>
        Disabled,

        /// <summary>오류로 쓸 수 없다. (<see cref="ModInfo.Error"/>)</summary>
        Error
    }

    /// <summary>
    /// 설치된 모드 하나. 정보 파일·폴더·상태를 가진다. (로비 "모드" 화면과 모드 일치 확인에 쓴다)
    /// </summary>
    public sealed class ModInfo
    {
        /// <summary>모드 정보. 정보 파일을 읽지 못했으면 null.</summary>
        public ModManifest Manifest { get; }

        /// <summary>모드 폴더 경로.</summary>
        public string FolderPath { get; }

        /// <summary>현재 상태.</summary>
        public ModStatus Status { get; private set; }

        /// <summary>오류 내용. (오류가 아니면 null)</summary>
        public string Error { get; private set; }

        /// <summary>모드 ID. 정보 파일을 읽지 못했으면 폴더 이름.</summary>
        public string Id => Manifest?.id ?? Path.GetFileName(FolderPath);

        /// <summary>표시 이름.</summary>
        public string DisplayName => string.IsNullOrEmpty(Manifest?.name) ? Id : Manifest.name;

        /// <param name="manifest">모드 정보. (읽지 못했으면 null)</param>
        /// <param name="folderPath">모드 폴더.</param>
        public ModInfo(ModManifest manifest, string folderPath)
        {
            Manifest = manifest;
            FolderPath = folderPath;
            Status = ModStatus.Active;
        }

        /// <summary>
        /// 오류로 표시한다.
        /// </summary>
        public void MarkError(string error)
        {
            Status = ModStatus.Error;
            Error = error;
        }

        /// <summary>
        /// 꺼진 상태로 표시한다. (오류가 있는 모드는 오류 상태를 유지)
        /// </summary>
        public void MarkDisabled()
        {
            if (Status != ModStatus.Error)
                Status = ModStatus.Disabled;
        }

        /// <summary>
        /// 모드 폴더 안 파일의 전체 경로. 파일 이름이 비었으면 null.
        /// </summary>
        public string GetFilePath(string fileName)
        {
            return string.IsNullOrEmpty(fileName) ? null : Path.Combine(FolderPath, fileName);
        }
    }
}
