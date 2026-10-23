using Celeste.Scene.Scheduling;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Spikes = Celeste.Simulation.Gameplay.Hazards.Spikes.SpikesState;
using Spring = Celeste.Game.Gameplay.Interaction.Springs.OriginalSpringState;
using TheoCrystal = Celeste.Game.Gameplay.Holdables.TheoCrystal.TheoCrystalState;
using Wiggler = Celeste.Scene.Components.Oscillation.WigglerComponent;
using LightOcclude = Celeste.Simulation.Presentation.Lighting.LightOccludeState;
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
    [CustomEntity("EmHelper/Monumentswitchblock = Load")]
    [RuntimeTypeAlias("Celeste.Mod.EmHelper.Entities.MonumentSwitchBlock")]
    public partial class MonumentSwitchBlock : SourceSolidEntityState
    {
        private static readonly string sourcePlayerRegistryKey =
            typeof(global::Celeste.Simulation.RoomPlayerSession).FullName;

        private readonly ISourceEngineContext engine;
        private readonly EmHelperModule module;
        private readonly MonumentActivator activator;

        public MonumentSwitchBlock(ISourceEngineContext engine, ISourceSolidWorldFactory worlds, Vector2 position,
            float width, float height, int pattern, bool active, Color color)
            : base(position, width, height, false, worlds.Create(position, false))
        {
            this.engine = engine;
            module = EmHelperModule.For(engine);
            this.pattern = pattern;
            blockHeight = 2;
            pressed = new List<Image>();
            solid = new List<Image>();
            all = new List<Image>();
            SurfaceSoundIndex = 35;
            Add(occluder = new LightOcclude(1f));

            activator = new MonumentActivator(color, active);
            Add(activator);
        }

        public MonumentSwitchBlock(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorldFactory worlds)
            : this(engine, worlds, data.Position + offset, data.Width, data.Height, data.Int("pattern", 0),
                data.Bool("active", false), SourceHexColor.HexToColor(data.Attr("color", "82d9ff")))
        {
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            Color color = SourceHexColor.HexToColor("667da5");
            Color disabledColor = new(color.R / 255f * (activator.Index.R / 255f),
                color.G / 255f * (activator.Index.G / 255f), color.B / 255f * (activator.Index.B / 255f), 1f);
            ((ISourceScene)scene).SourceScene.AddEntity(side = new BoxSide(this, disabledColor));
            foreach (SourceStaticMoverState staticMover in staticMovers)
            {
                if (staticMover.Entity is Spikes spikes)
                {
                    spikes.EnabledColor = activator.Index;
                    spikes.DisabledColor = disabledColor;
                    spikes.VisibleWhenDisabled = true;
                    spikes.SetSpikeColor(activator.Index);
                }

                if (staticMover.Entity is Spring spring)
                {
                    spring.DisabledColor = disabledColor;
                    spring.VisibleWhenDisabled = true;
                }
            }

            if (group == null)
            {
                groupLeader = true;
                group = new List<MonumentSwitchBlock> { this };
                FindInGroup(this);
                float num = float.MaxValue;
                float num2 = float.MinValue;
                float num3 = float.MaxValue;
                float num4 = float.MinValue;
                foreach (MonumentSwitchBlock switchblock in group)
                {
                    if (switchblock.Left < num)
                    {
                        num = switchblock.Left;
                    }

                    if (switchblock.Right > num2)
                    {
                        num2 = switchblock.Right;
                    }

                    if (switchblock.Bottom > num4)
                    {
                        num4 = switchblock.Bottom;
                    }

                    if (switchblock.Top < num3)
                    {
                        num3 = switchblock.Top;
                    }
                }

                groupOrigin = new Vector2((int)(num + ((num2 - num) / 2f)), (int)num4);
                wigglerScaler = new Vector2(ClampedMap(num2 - num, 32f, 96f, 1f, 0.2f),
                    ClampedMap(num4 - num3, 32f, 96f, 1f, 0.2f));
                Add(wiggler = Wiggler.Create(((ISourceWigglerPoolScope)engine).WigglerPool, 0.3f, 3f, null, false,
                    false));
                foreach (MonumentSwitchBlock switchblock2 in group)
                {
                    switchblock2.wiggler = wiggler;
                    switchblock2.wigglerScaler = wigglerScaler;
                    switchblock2.groupOrigin = groupOrigin;
                }
            }

            foreach (SourceStaticMoverState staticMover2 in staticMovers)
            {
                if (staticMover2.Entity is Spikes spikes2)
                {
                    spikes2.SetOrigins(groupOrigin);
                }
            }

            for (float num5 = Left; num5 < Right; num5 += 8f)
            {
                for (float num6 = Top; num6 < Bottom; num6 += 8f)
                {
                    bool flag = CheckForSame(num5 - 8f, num6);
                    bool flag2 = CheckForSame(num5 + 8f, num6);
                    bool flag3 = CheckForSame(num5, num6 - 8f);
                    bool flag4 = CheckForSame(num5, num6 + 8f);
                    if (flag && flag2 && flag3 && flag4)
                    {
                        if (!CheckForSame(num5 + 8f, num6 - 8f))
                        {
                            SetImage(num5, num6, 3, 0);
                        }
                        else if (!CheckForSame(num5 - 8f, num6 - 8f))
                        {
                            SetImage(num5, num6, 3, 1);
                        }
                        else if (!CheckForSame(num5 + 8f, num6 + 8f))
                        {
                            SetImage(num5, num6, 3, 2);
                        }
                        else if (!CheckForSame(num5 - 8f, num6 + 8f))
                        {
                            SetImage(num5, num6, 3, 3);
                        }
                        else
                        {
                            SetImage(num5, num6, 1, 1);
                        }
                    }
                    else if (flag && flag2 && !flag3 && flag4)
                    {
                        SetImage(num5, num6, 1, 0);
                    }
                    else if (flag && flag2 && flag3 && !flag4)
                    {
                        SetImage(num5, num6, 1, 2);
                    }
                    else if (flag && !flag2 && flag3 && flag4)
                    {
                        SetImage(num5, num6, 2, 1);
                    }
                    else if (!flag && flag2 && flag3 && flag4)
                    {
                        SetImage(num5, num6, 0, 1);
                    }
                    else if (flag && !flag2 && !flag3 && flag4)
                    {
                        SetImage(num5, num6, 2, 0);
                    }
                    else if (!flag && flag2 && !flag3 && flag4)
                    {
                        SetImage(num5, num6, 0, 0);
                    }
                    else if (flag && !flag2 && flag3 && !flag4)
                    {
                        SetImage(num5, num6, 2, 2);
                    }
                    else if (!flag && flag2 && flag3 && !flag4)
                    {
                        SetImage(num5, num6, 0, 2);
                    }
                }
            }

            UpdateVisualState();
        }

        private void FindInGroup(MonumentSwitchBlock block)
        {
            foreach (MonumentSwitchBlock switchblock in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(MonumentSwitchBlock)))
            {
                if (switchblock != this && switchblock != block && switchblock.activator.Index == activator.Index &&
                    (SourceEntityGeometryCollision.Rectangle(switchblock,
                         new IntRect((int)block.X - 1, (int)block.Y, (int)block.Width + 2, (int)block.Height)) ||
                     SourceEntityGeometryCollision.Rectangle(switchblock,
                         new IntRect((int)block.X, (int)block.Y - 1, (int)block.Width, (int)block.Height + 2))) &&
                    !group.Contains(switchblock))
                {
                    group.Add(switchblock);
                    FindInGroup(switchblock);
                    switchblock.group = group;
                }
            }
        }

        private bool CheckForSame(float x, float y)
        {
            foreach (MonumentSwitchBlock switchblock in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(MonumentSwitchBlock)))
            {
                if (switchblock.activator.Index == activator.Index &&
                    switchblock.Collider.Collide(new IntRect((int)x, (int)y, 8, 8)))
                {
                    return true;
                }
            }

            return false;
        }

        private void SetImage(float x, float y, int tx, int ty)
        {
            List<MTexture> atlasSubtextures =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(
                    "objects/monumentswitchblock/pressed");
            pressed.Add(CreateImage(x, y, tx, ty, atlasSubtextures[pattern % atlasSubtextures.Count]));
            atlasSubtextures =
                ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas.GetAtlasSubtextures(
                    "objects/monumentswitchblock/solid");
            solid.Add(CreateImage(x, y, tx, ty, atlasSubtextures[pattern % atlasSubtextures.Count]));
        }

        private Image CreateImage(float x, float y, int tx, int ty, MTexture tex)
        {
            Vector2 value = new(x - X, y - Y);
            Image image = new(tex.GetSubtexture(tx * 8, ty * 8, 8, 8, null));
            Vector2 vector = groupOrigin - Position;
            image.Origin = vector - value;
            image.Position = vector;
            image.Color = activator.Index;
            Add(image);
            all.Add(image);
            return image;
        }

        public override void Update()
        {
            base.Update();
            if (activator.Activated && !Collidable)
            {
                bool flag = false;
                if (BlockedCheck())
                {
                    flag = true;
                }

                if (!flag)
                {
                    Collidable = true;
                    ShiftSize(-1);
                    EnableStaticMovers();
                    if (groupLeader)
                    {
                        wiggler.Start();
                    }
                }
            }
            else if (!activator.Activated && Collidable)
            {
                Collidable = false;
                ShiftSize(1);
                DisableStaticMovers();
            }

            UpdateVisualState();
        }

        public bool BlockedCheck()
        {
            TheoCrystal theoCrystal = CollideFirst<TheoCrystal>("Celeste.TheoCrystal");
            if (theoCrystal != null && !TryActorWiggleUp(theoCrystal))
            {
                return true;
            }

            Entity walkeline = module.WalkelineRegistryKey != null
                ? CollideFirst<Entity>(module.WalkelineRegistryKey)
                : null;
            if (walkeline != null && !TryActorWiggleUp(walkeline))
            {
                return true;
            }

            Player player = CollideFirst<Player>(sourcePlayerRegistryKey);
            return player != null && !TryActorWiggleUp(player);
        }

        private void UpdateVisualState()
        {
            if (!Collidable)
            {
                Depth = 8990;
            }
            else
            {
                Player entity =
                    ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<Player>(sourcePlayerRegistryKey);
                Depth = entity != null && entity.Top >= Bottom - 1f ? 10 : -10;
            }

            foreach (SourceStaticMoverState staticMover in staticMovers)
            {
                ((Entity)staticMover.Entity).Depth = Depth + 1;
            }

            side.Depth = Depth + 5;
            side.Visible = blockHeight > 0;
            occluder.Visible = Collidable;
            foreach (Image image in solid)
            {
                image.Visible = Collidable;
            }

            foreach (Image image2 in pressed)
            {
                image2.Visible = !Collidable;
            }

            if (groupLeader)
            {
                Vector2 scale = new(1f + (wiggler.Value * 0.05f * wigglerScaler.X),
                    1f + (wiggler.Value * 0.15f * wigglerScaler.Y));
                foreach (MonumentSwitchBlock switchblock in group)
                {
                    foreach (Image image3 in switchblock.all)
                    {
                        image3.Scale = scale;
                    }

                    foreach (SourceStaticMoverState staticMover2 in switchblock.staticMovers)
                    {
                        if (staticMover2.Entity is Spikes spikes)
                        {
                            foreach (Component component in spikes.Components)
                            {
                                if (component is Image image4)
                                {
                                    image4.Scale = scale;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void ShiftSize(int amount)
        {
            MoveV(amount);
            blockHeight -= amount;
        }

        private bool TryActorWiggleUp(Entity actor)
        {
            foreach (MonumentSwitchBlock switchblock in group)
            {
                if (switchblock != this && switchblock.CheckAt(actor, switchblock.Position + (Vector2.UnitY * 4f)))
                {
                    return false;
                }
            }

            bool collidable = Collidable;
            Collidable = true;
            for (int i = 1; i <= 4; i++)
            {
                if (!actor.CollideCheck("Celeste.Solid", actor.Position - (Vector2.UnitY * i)))
                {
                    actor.Position -= Vector2.UnitY * i;
                    Collidable = collidable;
                    return true;
                }
            }

            Collidable = collidable;
            return false;
        }

        public static MonumentSwitchBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(level.EngineContext, data, offset, level.SolidWorldFactory);

        private bool CheckAt(ISourceEntityCollider other, Vector2 at)
        {
            Vector2 previous = Position;
            Position = at;
            bool result = SourceEntityCollision.Check(this, other);
            Position = previous;
            return result;
        }

        private static float ClampedMap(float value, float min, float max, float targetMin, float targetMax)
            => targetMin + (Math.Clamp((value - min) / (max - min), 0f, 1f) * (targetMax - targetMin));

        private int blockHeight;

        private List<MonumentSwitchBlock> group;

        private readonly int pattern;

        private bool groupLeader;

        private Vector2 groupOrigin;

        private readonly List<Image> pressed;

        private readonly List<Image> solid;

        private readonly List<Image> all;

        private readonly LightOcclude occluder;

        private Wiggler wiggler;

        private Vector2 wigglerScaler;

        private BoxSide side;

        private class BoxSide : Entity
        {
            public BoxSide(MonumentSwitchBlock block, Color color) : base(
                ((ISourceEntitySourceDataEnvironment)block.engine).EntitySourceDataRegistration)
            {
                this.block = block;
                this.color = color;
            }

            public override void Render(ISourceRenderContext context)
            {
                SourcePrimitiveDrawing.Rect(context, this, block.X, block.Y + block.Height - 8f, block.Width,
                    8 + block.blockHeight, color);
            }

            private readonly MonumentSwitchBlock block;

            private Color color;
        }
    }
}
