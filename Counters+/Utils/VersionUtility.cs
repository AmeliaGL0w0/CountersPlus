using Hive.Versioning;
using IPA.Loader;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;

namespace CountersPlus.Utils
{
    public class VersionUtility
    {
        public Version PluginVersion { get; private set; } = new Version("0.0.0");
        public Version BeatModsVersion { get; private set; } = new Version("0.0.0");
        public bool HasLatestVersion => PluginVersion >= BeatModsVersion;

        public VersionUtility()
        {
            // I could grab this straight from PluginMetadata but this is for cleanness.
            PluginVersion = PluginManager.GetPlugin("Counters+").HVersion;

            SharedCoroutineStarter.Run(GetBeatModsVersion());
        }

        private IEnumerator GetBeatModsVersion()
        {
            using (UnityWebRequest www = UnityWebRequest.Get("https://beatmods.com/api/v1/mod?search=Counters%2B"))
            {
                www.SetRequestHeader("User-Agent", "BeatSaber-Mod/1.0");

                yield return www.SendWebRequest();
                if (www.result != UnityWebRequest.Result.Success)
                {
                    Plugin.Logger.Error(
                        $"Failed to download version info. " +
                        $"Result={www.result}, Code={www.responseCode}, Error={www.error}"
                    );
                    yield break;
                }
                BeatmodsResult[] results = JsonConvert.DeserializeObject<BeatmodsResult[]>(www.downloadHandler.text);
                foreach (BeatmodsResult result in results)
                {
                    if (result.status != "approved") continue;
                    BeatModsVersion = new Version(result.version);
                    break;
                }
            }
            if (!HasLatestVersion) Plugin.Logger.Warn("Uh oh! We aren't up to date!");
        }

        
    }
    class BeatmodsResult
    {
        [JsonProperty("status")] internal string status;
        [JsonProperty("version")] internal string version;
    }
}
