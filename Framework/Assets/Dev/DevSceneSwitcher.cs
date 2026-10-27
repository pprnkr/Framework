using System.IO;
using Nyonyo.Framework;
using Nyonyo.Framework.Audio;
using Nyonyo.Framework.Debugging;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Logging;
using Nyonyo.Framework.Save;
using Nyonyo.Framework.Timing;
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
        private void Start()
        {
            var events = Services.Get<IEventBus>();
            events.Subscribe<SceneLoaded>(e => Debug.Log($"[Dev] SceneLoaded: {e.SceneName}"), destroyCancellationToken);

            PopupManager.Instance.BackKeyUnhandled += () => Debug.Log("[Dev] Back key: no popup to close");
        }

        [System.Serializable]
        private class DevSaveData
        {
            public int clicks;
        }

        private void OnGUI()
        {
            if (!Services.TryGet(out ISceneService scenes) || !Services.TryGet(out PopupManager popups) ||
                !Services.TryGet(out SoundManager sound) || !Services.TryGet(out SaveService save))
                return;

            // 고해상도 모바일 화면에서도 누를 수 있도록 짧은 변 720 기준으로 키웁니다.
            var scale = Mathf.Max(1f, Mathf.Min(Screen.width, Screen.height) / 720f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUILayout.BeginArea(new Rect(10, 10, 260, Screen.height / scale - 20));
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
            GUILayout.Space(10);
            DrawTools();
            GUILayout.EndArea();
        }

        private void DrawTools()
        {
            if (Services.TryGet(out PauseService pause) &&
                GUILayout.Button(pause.IsPaused ? "Resume (timeScale 1)" : "Pause (timeScale 0)"))
            {
                if (pause.IsPaused)
                    pause.Resume(this);
                else
                    pause.Pause(this);
            }

            if (Services.TryGet(out TimerService timers) && GUILayout.Button("Timer: log after 1s"))
                timers.After(1f, () => Log.Info("[Dev] Timer fired"), owner: this);

            if (Services.TryGet(out DebugConsole console) && GUILayout.Button("Toggle Console (` key)"))
                console.Toggle();
        }

        // [DebugCommand]는 static 함수만 등록합니다.
        [DebugCommand("dev.notice", "띠알람을 띄웁니다. 예: dev.notice \"Hello\"")]
        private static void ShowNotice(string message = "Notice from console") =>
            PopupManager.Instance.Open<PopupNotice>().SetMessage(message);

        private void DrawSave(SaveService save)
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

        private void DrawScenes(ISceneService scenes)
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
                    scenes.Load(sceneName);
            }
        }

        private void DrawPopups(PopupManager popups)
        {
            if (GUILayout.Button("Show PopupTest"))
                popups.Open<PopupTest>();
            if (GUILayout.Button("Show PopupTest (System layer)"))
                popups.Open<PopupTest>(UILayer.System);
            if (GUILayout.Button("Show Notice"))
                popups.Open<PopupNotice>().SetMessage("Notice is drawn above every popup.");
            if (GUILayout.Button("Close All"))
                popups.CloseAll();

            GUILayout.Label($"Popup: {popups.GetOpenPopups(UILayer.Popup).Count}  " +
                            $"System: {popups.GetOpenPopups(UILayer.System).Count}  " +
                            $"Notice: {popups.GetOpenPopups(UILayer.Notice).Count}");
            GUILayout.Label("ESC: close top popup");
        }

        private void DrawSound(SoundManager sound)
        {
            GUILayout.Label($"BGM: {sound.CurrentBgm ?? "-"}");
            if (GUILayout.Button("Play BGM A (fade 1s)"))
                sound.PlayBGM("DevBgmA", fadeSeconds: 1f);
            if (GUILayout.Button("Play BGM B (fade 1s)"))
                sound.PlayBGM("DevBgmB", fadeSeconds: 1f);
            if (GUILayout.Button("Stop BGM (fade 1s)"))
                sound.StopBGM(1f);
            if (GUILayout.Button("Play SFX Beep"))
                sound.PlaySFX("Beep");
            if (GUILayout.Button("Play SFX x12 (8 channels)"))
            {
                for (var i = 0; i < 12; i++)
                    sound.PlaySFX("Beep", 0.5f);
            }

            sound.MasterVolume = Slider("Master", sound.MasterVolume);
            sound.BgmVolume = Slider("BGM", sound.BgmVolume);
            sound.SfxVolume = Slider("SFX", sound.SfxVolume);
        }

        private float Slider(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value:0.00}", GUILayout.Width(90));
            value = GUILayout.HorizontalSlider(value, 0f, 1f);
            GUILayout.EndHorizontal();
            return value;
        }
    }
}
