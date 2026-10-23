using Celeste.Everest.Reflection.Runtime;
using System;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Interop;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.EmHelper.Interop;

namespace Celeste.Mods.EmHelper.Modules
{
    public sealed partial class EmHelperModule : SourceEverestModule
    {
        private IDisposable gravityImports;
        public bool LevelHasTriggerHappyWalkeline;
        internal Celeste.Mods.EmHelper.Runtime.Walkers.WalkelineProcess Walkelines { get; private set; }
        internal Type WalkelineType { get; private set; }
        internal string WalkelineRegistryKey { get; private set; }
        internal GravityHelperImports Gravity { get; } = new();

        public EmHelperModule(ISourceEngineContext engine) : base(engine)
        {
        }

        public override void Load()
        {
            Walkelines = new(this, SourceEngine);
            WalkelineType =
                ((ISourceRuntimeTypeDiscoveryEnvironment)SourceEngine).RuntimeTypeDiscovery.ResolveRuntimeTypeAlias(
                    "Celeste.Mod.EmHelper.Entities.Walkeline");
            WalkelineRegistryKey = WalkelineType != null
                ? ((Celeste.Scene.Tracking.ISourceSceneCatalogEnvironment)SourceEngine).SceneCatalog.Find(WalkelineType)
                .RegistryKey
                : null;
            gravityImports =
                ((ISourceModInteropEnvironment)SourceEngine).ModInterop.RegisterOwned(
                    new SourceModInteropActualTypeBinding(typeof(GravityHelperImports), Gravity));
        }

        public override void Unload()
        {
            Walkelines?.Dispose();
            Walkelines = null;
            gravityImports?.Dispose();
            gravityImports = null;
        }

        internal static EmHelperModule For(ISourceEngineContext engine)
        {
            foreach (var module in ((ISourceEverestModulesEnvironment)engine).Modules)
                if (module is EmHelperModule owner)
                    return owner;
            throw new InvalidOperationException("EmHelper module has not loaded.");
        }
    }
}
