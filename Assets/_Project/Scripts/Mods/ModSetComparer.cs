using System;
using System.Collections.Generic;

namespace GuardVsMannequin.Mods
{
    /// <summary>호스트 모드를 맞출 수 없는 이유.</summary>
    public enum MissingModReason : byte
    {
        /// <summary>설치되어 있지 않음.</summary>
        NotInstalled,

        /// <summary>설치했지만 버전이 다름.</summary>
        VersionDiffers,

        /// <summary>버전은 같지만 번들 파일 내용이 다름.</summary>
        FilesDiffer,

        /// <summary>설치했지만 오류로 쓸 수 없음. (필요한 모드 없음, Unity 버전 다름 등)</summary>
        Unusable
    }

    /// <summary>
    /// 호스트 모드 목록의 항목 하나와, 내 PC에서 그 모드를 맞출 수 없는 이유.
    /// </summary>
    public readonly struct MissingMod
    {
        public readonly string id;
        public readonly string hostVersion;
        public readonly MissingModReason reason;

        /// <summary>내 PC의 모드 정보. 설치되지 않았으면 null.</summary>
        public readonly ModInfo local;

        /// <summary>항목을 만든다.</summary>
        public MissingMod(string id, string hostVersion, MissingModReason reason, ModInfo local)
        {
            this.id = id;
            this.hostVersion = hostVersion;
            this.reason = reason;
            this.local = local;
        }
    }

    /// <summary>
    /// 호스트 모드 목록 설명("id@버전#번들지문, ...", <see cref="ModService.ModSetDescription"/>)을 읽고,
    /// 내 PC에 설치된 모드와 비교해 부족하거나 다른 모드를 찾는다.
    /// </summary>
    public static class ModSetComparer
    {
        /// <summary>호스트 모드 목록의 항목 하나. (ID, 버전, 번들 지문)</summary>
        public readonly struct Entry
        {
            public readonly string id;
            public readonly string version;
            public readonly string contentHash;

            /// <summary>항목을 만든다.</summary>
            public Entry(string id, string version, string contentHash)
            {
                this.id = id;
                this.version = version;
                this.contentHash = contentHash;
            }
        }

        /// <summary>
        /// 모드 목록 설명을 항목으로 나눈다. 비었으면 빈 목록.
        /// </summary>
        public static List<Entry> Parse(string modList)
        {
            var entries = new List<Entry>();
            if (string.IsNullOrEmpty(modList))
                return entries;

            foreach (string raw in modList.Split(new[] { ", " }, StringSplitOptions.RemoveEmptyEntries))
            {
                int hashStart = raw.IndexOf('#');
                string idAndVersion = hashStart >= 0 ? raw.Substring(0, hashStart) : raw;
                string contentHash = hashStart >= 0 ? raw.Substring(hashStart + 1) : string.Empty;

                // ID에는 '@'가 들어갈 수 있으니 마지막 '@'를 버전 구분자로 본다.
                int versionStart = idAndVersion.LastIndexOf('@');
                string id = versionStart >= 0 ? idAndVersion.Substring(0, versionStart) : idAndVersion;
                string version = versionStart >= 0 ? idAndVersion.Substring(versionStart + 1) : string.Empty;
                entries.Add(new Entry(id.Trim(), version.Trim(), contentHash.Trim()));
            }
            return entries;
        }

        /// <summary>
        /// 호스트가 켠 모드 중 내 PC에서 맞출 수 없는 모드를 찾는다. (내가 꺼 둔 모드는 접속 동안 켜지므로 문제로 보지 않는다)
        /// 모두 맞는데도 접속이 거부됐다면 번들 파일 크기나 내장 맵 목록이 다른 경우라 빈 목록이 나온다.
        /// </summary>
        /// <param name="hostModList">호스트의 모드 목록 설명.</param>
        /// <param name="findLocal">ID로 내 PC의 모드를 찾는 함수. 없으면 null.</param>
        public static List<MissingMod> FindMissing(string hostModList, Func<string, ModInfo> findLocal)
        {
            var missing = new List<MissingMod>();
            foreach (Entry entry in Parse(hostModList))
            {
                ModInfo local = findLocal(entry.id);
                if (local == null || local.Manifest == null)
                    missing.Add(new MissingMod(entry.id, entry.version, MissingModReason.NotInstalled, local));
                else if (local.Manifest.version != entry.version)
                    missing.Add(new MissingMod(entry.id, entry.version, MissingModReason.VersionDiffers, local));
                else if ((local.Manifest.contentHash ?? string.Empty) != entry.contentHash)
                    missing.Add(new MissingMod(entry.id, entry.version, MissingModReason.FilesDiffer, local));
                else if (local.Status == ModStatus.Error)
                    missing.Add(new MissingMod(entry.id, entry.version, MissingModReason.Unusable, local));
            }
            return missing;
        }
    }
}
