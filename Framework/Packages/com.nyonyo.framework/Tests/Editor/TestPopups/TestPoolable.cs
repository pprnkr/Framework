using System.Collections.Generic;
using Nyonyo.Framework.Pooling;
using UnityEngine;

namespace Nyonyo.Framework.Editor.Tests
{
    public class TestPoolable : MonoBehaviour, IPoolable
    {
        public readonly List<string> Log = new();

        public void OnSpawned() => Log.Add("spawned");

        public void OnDespawned() => Log.Add("despawned");
    }
}
