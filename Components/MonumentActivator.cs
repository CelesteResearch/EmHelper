using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Audio;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Rendering;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Mods.EmHelper.Components;
using Celeste.Mods.EmHelper.Modules;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Scene = System.Object;
using Player = Celeste.Simulation.RoomPlayerSession;
using Color = Celeste.Core.Display.PackedColor;
using Image = Celeste.Graphics.Animation.SpriteImage;
using Hitbox = Celeste.Physics.Collision.Shapes.HitboxColliderGeometry;
using PlayerCollider = Celeste.Simulation.Gameplay.Player.Collision.Components.PlayerColliderComponentState;
using Holdable = Celeste.Simulation.Gameplay.Holdables.HoldableState;
using HoldableCollider = Celeste.Simulation.Gameplay.Holdables.HoldableColliderState;
using Component = Celeste.Core.Components.SourceComponentState;
using MTexture = Celeste.Graphics.Content.Atlas.AtlasEntry;
using Booster = Celeste.Game.Entities.Interaction.Boosters.OriginalBoosterState;

namespace Celeste.Mods.EmHelper.Components
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.EmHelper.Entities.MonumentActivator")]
    public class MonumentActivator : SourceComponentState
    {
        public Color Index;
        public bool Activated;
        public Action<bool> OnToggle;

        public MonumentActivator(Color index, bool startActive = false, Action<bool> onToggle = null)
            : base(false, false)
        {
            Index = index;
            Activated = startActive;
            OnToggle = onToggle;
        }

        public static void ToggleAll(object scene, Color index)
        {
            var tracker = ((ISourceScene)scene).SourceScene.Components;
            foreach (var component in
                     tracker.GetTrackedComponents(tracker.Catalog.Find(typeof(MonumentActivator)).Name))
                ((MonumentActivator)component).Toggle(index);
        }

        public void Toggle(Color index)
        {
            if (index == Index)
            {
                Activated = !Activated;
                OnToggle?.Invoke(Activated);
            }
        }
    }
}
