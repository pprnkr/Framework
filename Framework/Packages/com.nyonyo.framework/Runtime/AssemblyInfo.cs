using System.Runtime.CompilerServices;
using UnityEngine.Scripting;

[assembly: InternalsVisibleTo("Nyonyo.Framework.Editor")]
[assembly: InternalsVisibleTo("Nyonyo.Framework.Tests")]
[assembly: InternalsVisibleTo("Nyonyo.Framework.Editor.Tests")]

// Bootstrapper와 TweenManager는 [RuntimeInitializeOnLoadMethod]로 시작합니다.
// 게임 코드가 아직 이 어셈블리를 참조하지 않아도 IL2CPP 빌드에서 어셈블리가 빠지지 않도록 합니다.
[assembly: AlwaysLinkAssembly]
