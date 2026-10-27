using System.Collections.Generic;
using Nyonyo.Framework.UI;
using UnityEngine.UI;

namespace Nyonyo.Framework.Tests
{
    public class TestChildUI : UIView
    {
        public readonly List<string> Log = new();

        protected override void OnShow() => Log.Add("show");

        protected override void OnHide() => Log.Add("hide");

        protected override void OnButtonClick(Button button) => Log.Add($"click:{button.name}");
    }
}
