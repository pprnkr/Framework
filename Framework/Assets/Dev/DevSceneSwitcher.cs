using System.IO;
using Nyonyo.Framework;
using Nyonyo.Framework.Events;
using Nyonyo.Framework.Scenes;
using Nyonyo.Framework.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nyonyo.Dev
{
    /// <summary>
    /// 화면 왼쪽 위에 씬 전환, 팝업 버튼을 띄워 프레임워크 동작을 확인합니다.
    /// </summary>
    public class DevSceneSwitcher : MonoBehaviour
    {
        void Start()
        {
            var events = Services.Get<IEventBus>();
            events.Subscribe<SceneLoaded>(e => Debug.Log($"[Dev] SceneLoaded: {e.SceneName}"), destroyCancellationToken);

            PopupManager.Instance.BackKeyUnhandled += () => Debug.Log("[Dev] Back key: no popup to close");
        }

        void OnGUI()
        {
            if (!Services.TryGet(out ISceneService scenes) || !Services.TryGet(out PopupManager popups))
                return;

            GUILayout.BeginArea(new Rect(10, 10, 240, 600));
            GUILayout.Label($"Scene: {SceneManager.GetActiveScene().name}");

            if (scenes.IsLoading)
            {
                GUILayout.Label($"Loading... {scenes.Progress:P0}");
            }
            else
            {
                for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
                {
                    var sceneName = Path.GetFileNameWithoutExtension(SceneUtility.GetScenePathByBuildIndex(i));
                    if (GUILayout.Button($"Load {sceneName}"))
                        _ = scenes.LoadAsync(sceneName);
                }
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Show PopupTest"))
                PopupManager.Instance.Show<PopupTest>();
            if (GUILayout.Button("Show PopupTest (System layer)"))
                PopupManager.Instance.Show<PopupTest>(UILayer.System);
            if (GUILayout.Button("Show Notice"))
                PopupManager.Instance.Show<PopupNotice>().SetMessage("Notice is drawn above every popup.");
            if (GUILayout.Button("Close All"))
                popups.CloseAll();

            GUILayout.Label($"Popup: {popups.GetOpenPopups(UILayer.Popup).Count}  " +
                            $"System: {popups.GetOpenPopups(UILayer.System).Count}  " +
                            $"Notice: {popups.GetOpenPopups(UILayer.Notice).Count}");
            GUILayout.Label("ESC: close top popup");
            GUILayout.EndArea();
        }
    }
}
