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
    [RuntimeTypeAlias("Celeste.Mod.EmHelper.Entities.MonumentBooster")]
    [CustomEntity("EmHelper/Monumentbooster")]
    public class MonumentBooster : SourceActorEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly MonumentActivator activator;

        public MonumentBooster(ISourceEngineContext engine, EntityData data, Vector2 levelOffset)
            : this(engine, data.Position + levelOffset, data.Int("pattern", 0), data.Bool("red", false),
                SourceHexColor.HexToColor(data.Attr("color", "82d9ff")), data.Bool("active", true))
        {
        }

        public MonumentBooster(ISourceEngineContext engine, Vector2 position, int pattern, bool red, Color color,
            bool active)
            : base(new PixelActor(null, position), engine,
                ((ISourcePlatformTargetMotionEnvironment)engine).TargetMotionPolicy)
        {
            monumentOutline = new List<Image>();
            this.red = red;
            this.engine = engine;
            this.pattern = pattern;

            Add(activator = new MonumentActivator(color, active, (activated) => OnToggle(activated)));
        }

        public override void Awake(object scene)
        {
            SetImage();
            foreach (Image image in monumentOutline)
            {
                image.Color = activator.Index;
            }

            OnToggle(activator.Activated);
        }

        private void SetImage()
        {
            List<MTexture> atlasSubtextures =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(
                    "objects/monumentbooster/outline");
            monumentOutline.Add(CreateImage(atlasSubtextures[pattern % atlasSubtextures.Count]));
        }

        private Image CreateImage(MTexture tex)
        {
            Image image = new(tex)
            {
                Color = activator.Index
            };
            image.Origin.X = image.Width / 2f;
            image.Origin.Y = image.Height / 2f;
            Add(image);
            return image;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            bubble = new Booster(engine, Position, red);
            ((ISourceScene)Scene).SourceScene.AddEntity(bubble);
            bubble.Position = Position;
        }

        public void OnToggle(bool flag)
        {
            if (!bubble.BoostingPlayer)
            {
                bubble.Collidable = flag;
                bubble.Visible = flag;
                foreach (Image image in monumentOutline)
                {
                    image.Visible = !flag;
                }
            }
            else
            {
                // If the player is inside the bubble when we activate, delay activation
                activationQueue = true;
                queueflag = flag;
            }
        }

        public override void Update()
        {
            base.Update();
            if (activationQueue && !bubble.BoostingPlayer)
            {
                OnToggle(queueflag); //activates it later
                activationQueue = false;
                queueflag = false;
            }
        }

        private bool activationQueue = false;
        private bool queueflag = false;
        private Booster bubble;
        private readonly int pattern = 0;
        private readonly bool red = false;
        private readonly List<Image> monumentOutline;
    }
}
