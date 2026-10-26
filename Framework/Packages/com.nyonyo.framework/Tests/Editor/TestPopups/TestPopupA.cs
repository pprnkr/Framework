using System.Collections.Generic;
using Nyonyo.Framework.UI;
using UnityEngine.UI;

namespace Nyonyo.Framework.Editor.Tests
{
    public class TestPopupA : PopupBase
    {
        public readonly List<string> Log = new();

        protected override void OnOpen() => Log.Add("open");

        public override void OnButtonEvent(Button button)
        {
            Log.Add($"button:{button.name}");
            base.OnButtonEvent(button);
        }
    }
}
