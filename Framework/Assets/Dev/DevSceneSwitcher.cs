using System.IO;
using Nyonyo.Framework;
using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Save;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 화면 왼쪽 위에 씬 전환, 팝업, 사운드 버튼을 띄워 프레임워크 동작을 확인합니다.
    /// </summary>
    public class DevSceneSwitcher : MonoBehaviour
    {
        void Start()
        {
            var events = Services.Get<IEventBus>();
            events.Subscribe<SceneLoaded>(e => Debug.Log($"[Dev] SceneLoaded: {e.SceneName}"), destroyCancellationToken);

            PopupManager.Instance.BackKeyUnhandled += () => Debug.Log("[Dev] Back key: no popup to close");
        }

        [System.Serializable]
        class DevSaveData
        {
            public int clicks;
        }

        void OnGUI()
        {
            if (!Services.TryGet(out ISceneService scenes) || !Services.TryGet(out PopupManager popups) ||
                !Services.TryGet(out SoundManager sound) || !Services.TryGet(out SaveService save))
                return;

            GUILayout.BeginArea(new Rect(10, 10, 260, 1200));
            if (!Bootstrapper.IsReady)
            {
                // 팝업 미리 불러오기 같은 비동기 초기화가 끝나기 전입니다.
                GUILayout.Label("Initializing...");
                GUILayout.EndArea();
                return;
            }

            DrawScenes(scenes);
            GUILayout.Space(10);
            DrawPopups(popups);
            GUILayout.Space(10);
            DrawSound(sound);
            GUILayout.Space(10);
            DrawSave(save);
            GUILayout.EndArea();
        }

        static void DrawSave(SaveService save)
        {
            var data = save.Load("dev", new DevSaveData());
            GUILayout.Label($"Save slot {save.CurrentSlot}: clicks {data.clicks}");
            if (GUILayout.Button("Clicks +1 (save)"))
            {
                data.clicks++;
                save.Save("dev", data);
            }

            GUILayout.BeginHorizontal();
            for (var slot = 0; slot < 3; slot++)
            {
                if (GUILayout.Button($"Slot {slot}"))
                    save.CurrentSlot = slot;
            }

            GUILayout.EndHorizontal();
            if (GUILayout.Button("Delete current slot"))
                save.DeleteSlot(save.CurrentSlot);
        }

        static void DrawScenes(ISceneService scenes)
        {
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");
            if (scenes.IsLoading)
            {
                GUILayout.Label($"Loading... {scenes.Progress:P0}");
                return;
            }

            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                var sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                if (GUILayout.Button($"Load {sceneName}"))
                    _ = scenes.LoadAsync(sceneName);
            }
        }

        static void DrawPopups(PopupManager popups)
        {
            if (GUILayout.Button("Show PopupTest"))
                popups.Show<PopupTest>();
            if (GUILayout.Button("Show PopupTest (System layer)"))
                popups.Show<PopupTest>(UILayer.System);
            if (GUILayout.Button("Show Notice"))
                popups.Show<PopupNotice>().SetMessage("Notice is drawn above every popup.");
            if (GUILayout.Button("Close All"))
                popups.CloseAll();

            GUILayout.Label($"Popup: {popups.GetOpenPopups(UILayer.Popup).Count}  " +
                            $"System: {popups.GetOpenPopups(UILayer.System).Count}  " +
                            $"Notice: {popups.GetOpenPopups(UILayer.Notice).Count}");
            GUILayout.Label("ESC: close top popup");
        }

        static void DrawSound(SoundManager sound)
        {
            GUILayout.Label($"BGM: {sound.CurrentBgm ?? "-"}");
            if (GUILayout.Button("Play BGM A (fade 1s)"))
                _ = sound.PlayBGM("DevBgmA", fadeSeconds: 1f);
            if (GUILayout.Button("Play BGM B (fade 1s)"))
                _ = sound.PlayBGM("DevBgmB", fadeSeconds: 1f);
            if (GUILayout.Button("Stop BGM (fade 1s)"))
                _ = sound.StopBGM(1f);
            if (GUILayout.Button("Play SFX Beep"))
                _ = sound.PlaySFX("Beep");
            if (GUILayout.Button("Play SFX x12 (8 channels)"))
            {
                for (var i = 0; i < 12; i++)
                    _ = sound.PlaySFX("Beep", 0.5f);
            }

            sound.MasterVolume = Slider("Master", sound.MasterVolume);
            sound.BgmVolume = Slider("BGM", sound.BgmVolume);
            sound.SfxVolume = Slider("SFX", sound.SfxVolume);
        }

        static float Slider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value:0.00}", GUILayout.Width(90));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
