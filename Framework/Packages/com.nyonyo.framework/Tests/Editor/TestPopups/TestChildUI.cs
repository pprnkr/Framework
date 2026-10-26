using System.Collections.Generic;
using Nyonyo.Framework.UI;

namespace Nyonyo.Framework.Editor.Tests
{
    public class TestChildUI : UIBase
    {
        public readonly List<string> Log = new();

        protected override void OnShow() => Log.Add("show");

        protected override void OnHide() => Log.Add("hide");
    }
}
