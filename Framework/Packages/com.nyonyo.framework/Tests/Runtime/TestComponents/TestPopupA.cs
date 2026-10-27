using System.Collections.Generic;
using System.Linq;
using Nyonyo.Framework.UI;
using UnityEngine.UI;

namespace Nyonyo.Framework.Tests
{
    public class TestPopupA : PopupBase
    {
        public readonly List<string> Log = new();

        /// <summary>훅이 불릴 때 이 팝업이 열린 팝업 목록에 있었는지 기록합니다.</summary>
        public readonly List<bool> ListedAtHook = new();

        protected override void PrevOpen() => Record("prev open");

        protected override void PostOpen() => Record("post open");

        protected override void PrevClose() => Record("prev close");

        protected override void PostClose() => Record("post close");

        protected override void OnButtonClick(Button button)
        {
            Log.Add($"button:{button.name}");
            base.OnButtonClick(button);
        }

        private void Record(string hook)
        {
            Log.Add(hook);
            ListedAtHook.Add(Manager != null && Manager.GetOpenPopups(Layer).Contains(this));
        }
    }
}
