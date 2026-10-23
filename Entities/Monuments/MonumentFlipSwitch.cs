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

namespace Celeste.Mods.EmHelper.Entities.Monuments
{
    [SceneTracked]
    [RuntimeTypeAlias("Celeste.Mod.EmHelper.Entities.MonumentFlipSwitch")]
    [CustomEntity("EmHelper/Monumentflipswitch")]
    public class MonumentFlipSwitch : Entity
    {
        public MonumentFlipSwitch(ISourceEngineContext engine, Vector2 position, bool onlyEnable, bool onlyDisable,
            Color color, bool mute, int pattern)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            this.pattern = pattern;
            this.mute = mute;
            this.onlyEnable = onlyEnable;
            this.onlyDisable = onlyDisable;
            Collider = new Hitbox(16f, 24f, -8f, -12f);
            Add(new PlayerCollider(new Action<Player>(OnPlayer), null, null));
            Add(new HoldableCollider(new Action<Holdable>(OnHoldable), null));
            string spritepath = "monumentflipswitch" + this.pattern.ToString();
            Add(sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create(spritepath));
            Depth = 2000;

            activator = new MonumentActivator(color, false, (activated) => SetSprite(true));
            Add(activator);
        }

        public MonumentFlipSwitch(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data.Position + offset, data.Bool("onlyEnable", false), data.Bool("onlyDisable", false),
                SourceHexColor.HexToColor(data.Attr("color", "82d9ff")), data.Bool("mute", false),
                data.Int("pattern", 0))
        {
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            sprite.Color = activator.Index;
            SetSprite(false);
        }

        public void SetSprite(bool animate)
        {
            if (animate)
            {
                if (playSounds && !mute)
                {
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                        activator.Activated
                            ? "event:/game/09_core/switch_to_cold"
                            : "event:/game/09_core/switch_to_hot", Position);
                }

                if (Usable)
                {
                    sprite.Play(activator.Activated ? "ice" : "hot", false, false);
                }
                else
                {
                    if (playSounds && !mute)
                    {
                        ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/09_core/switch_dies",
                            Position);
                    }

                    sprite.Play(activator.Activated ? "iceOff" : "hotOff", false, false);
                }
            }
            else if (Usable)
            {
                sprite.Play(activator.Activated ? "iceLoop" : "hotLoop", false, false);
            }
            else
            {
                sprite.Play(activator.Activated ? "iceOffLoop" : "hotOffLoop", false, false);
            }

            playSounds = false;
        }

        private void OnPlayer(Player player)
        {
            if (Usable && cooldownTimer <= 0f)
            {
                playSounds = true;
                MonumentActivator.ToggleAll(Scene, activator.Index);
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
                cooldownTimer = 1f;
            }
        }

        private void OnHoldable(Holdable holdable)
        {
            if (Usable && cooldownTimer <= 0f)
            {
                playSounds = true;
                MonumentActivator.ToggleAll(Scene, activator.Index);
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
                cooldownTimer = 1f;
            }
        }

        public override void Update()
        {
            base.Update();
            if (cooldownTimer > 0f)
            {
                cooldownTimer -= engine.DeltaTime;
            }
        }

        private bool Usable => (!onlyEnable || activator.Activated) && (!onlyDisable || !activator.Activated);


        private float cooldownTimer;

        private readonly bool mute;

        private readonly bool onlyEnable;

        private readonly bool onlyDisable;

        private bool playSounds;

        private readonly Sprite sprite;

        private readonly int pattern;

        private readonly ISourceEngineContext engine;
        private readonly MonumentActivator activator;
    }
}
