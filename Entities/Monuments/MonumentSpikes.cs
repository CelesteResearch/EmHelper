using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
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
    [CustomEntity(new string[]
    {
        "EmHelper/MonumentspikesUp = LoadUp",
        "EmHelper/MonumentspikesDown = LoadDown",
        "EmHelper/MonumentspikesLeft = LoadLeft",
        "EmHelper/MonumentspikesRight = LoadRight"
    })]
    [RuntimeTypeAlias("Celeste.Mod.EmHelper.Entities.MonumentSpikes")]
    public class MonumentSpikes : Entity
    {
        private readonly ISourceEngineContext engine;
        private readonly EmHelperModule module;
        private readonly MonumentActivator activator;
        private bool forceDisabled;

        public static Entity LoadUp(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData, Vector2 offset,
            OriginalLevelEntityData entityData)
        {
            entityData.Values["type"] = entityData.Attr("type", "default");
            return new MonumentSpikes(level.EngineContext, entityData, offset, Directions.Up);
        }

        public static Entity LoadDown(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData, Vector2 offset,
            OriginalLevelEntityData entityData)
        {
            entityData.Values["type"] = entityData.Attr("type", "default");
            return new MonumentSpikes(level.EngineContext, entityData, offset, Directions.Down);
        }

        public static Entity LoadLeft(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData, Vector2 offset,
            OriginalLevelEntityData entityData)
        {
            entityData.Values["type"] = entityData.Attr("type", "default");
            return new MonumentSpikes(level.EngineContext, entityData, offset, Directions.Left);
        }

        public static Entity LoadRight(OriginalLevelSceneRoot level, OriginalLevelRoomData levelData, Vector2 offset,
            OriginalLevelEntityData entityData)
        {
            entityData.Values["type"] = entityData.Attr("type", "default");
            return new MonumentSpikes(level.EngineContext, entityData, offset, Directions.Right);
        }

        public MonumentSpikes(ISourceEngineContext engine, Vector2 position, int size, Directions direction,
            string type, Color color, bool active)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, position)
        {
            this.engine = engine;
            module = EmHelperModule.For(engine);
            Color disablecolor = SourceHexColor.HexToColor("667da5");
            disabledColor = new Color(disablecolor.R / 255f * (color.R / 255f),
                disablecolor.G / 255f * (color.G / 255f), disablecolor.B / 255f * (color.B / 255f), 1f);
            Depth = -1;
            this.direction = direction;
            this.size = size;
            overrideType = type;
            switch (direction)
            {
                case Directions.Up:
                    Collider = new Hitbox(size, 3f, 0f, -3f);
                    Add(new LedgeBlockerState(_ => CheckGravity(false)));
                    break;
                case Directions.Down:
                    Collider = new Hitbox(size, 3f, 0f, 0f);
                    Add(new LedgeBlockerState(_ => CheckGravity(true)));
                    break;
                case Directions.Left:
                    Collider = new Hitbox(3f, size, -3f, 0f);
                    Add(new LedgeBlockerState(null));
                    break;
                case Directions.Right:
                    Collider = new Hitbox(3f, size, 0f, 0f);
                    Add(new LedgeBlockerState(null));
                    break;
            }

            Add(new PlayerCollider(new Action<Player>(OnCollide), null, null));
            Add(new SourceStaticMoverState
            {
                OnShake = new Action<Vector2>(OnShake),
                SolidChecker = new Func<SourceSolidEntityState, bool>(IsRiding),
                JumpThruChecker = new Func<ISourceJumpThruEntity, bool>(IsRiding),
                OnEnable = new Action(OnStaticMoverEnable),
                OnDisable = new Action(OnStaticMoverDisable)
            });

            activator = new MonumentActivator(color, active, OnToggle);
            Add(activator);
        }

        private static string GetSpikeTexturePrefix(string spikeType) => "danger/spikes/" + spikeType;

        private bool CheckGravity(bool inverted) =>
            module.Gravity.IsPlayerInverted != null && module.Gravity.IsPlayerInverted() == inverted;

        public MonumentSpikes(ISourceEngineContext engine, EntityData data, Vector2 offset, Directions dir)
            : this(engine, data.Position + offset, GetSize(data, dir), dir, data.Attr("type", "default"),
                SourceHexColor.HexToColor(data.Attr("color", "82d9ff")), data.Bool("active", true))
        {
        }

        public void SetSpikeColor(Color color)
        {
            foreach (Component component in Components)
            {
                if (component is Image image)
                {
                    image.Color = color;
                }
            }
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            spikeType = ((ISourceSpikeScene)scene).SpikeType;
            if (!string.IsNullOrEmpty(overrideType) && !overrideType.Equals("default")) spikeType = overrideType;
            SpikeDecorationBuilder.Add(engine, this, size, (SpikeDirection)direction, spikeType, GetSpikeTexturePrefix,
                ((ISourceRandomEnvironment)engine).Random);
            if (spikeType == "tentacles" && size / 8 % 2 == 1) return;
            OnToggle(activator.Activated);
        }

        public void OnToggle(bool activated)
        {
            bool enabled = activated && !forceDisabled;
            Active = Collidable = enabled;
            SetSpikeColor(enabled ? activator.Index : disabledColor);
        }

        public void OnStaticMoverEnable()
        {
            forceDisabled = false;
            OnToggle(activator.Activated);
        }

        public void OnStaticMoverDisable()
        {
            forceDisabled = true;
            OnToggle(false);
        }

        private void OnShake(Vector2 amount)
        {
            imageOffset += amount;
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position += imageOffset;
            base.Render(context);
            Position = position;
        }

        public void SetOrigins(Vector2 origin)
        {
            foreach (Component component in Components)
            {
                if (component is Image image)
                {
                    Vector2 vector = origin - Position;
                    image.Origin = image.Origin + vector - image.Position;
                    image.Position = vector;
                }
            }
        }

        private void OnCollide(Player player)
        {
            var inverted = module.Gravity.IsPlayerInverted?.Invoke() ?? false;

            switch (direction)
            {
                case Directions.Up:
                    if (!inverted && player.Data.Motion.Normal.Speed.Y >= 0f && player.Bottom <= Bottom ||
                        inverted && player.Data.Motion.Normal.Speed.Y <= 0f)
                    {
                        player.Die(new Vector2(0f, -1f), false, true);
                        return;
                    }

                    break;
                case Directions.Down:
                    if (!inverted && player.Data.Motion.Normal.Speed.Y <= 0f ||
                        inverted && player.Data.Motion.Normal.Speed.Y >= 0f && player.Top >= Top)
                    {
                        player.Die(new Vector2(0f, 1f), false, true);
                        return;
                    }

                    break;
                case Directions.Left:
                    if (player.Data.Motion.Normal.Speed.X >= 0f)
                    {
                        player.Die(new Vector2(-1f, 0f), false, true);
                        return;
                    }

                    break;
                case Directions.Right:
                    if (player.Data.Motion.Normal.Speed.X <= 0f)
                    {
                        player.Die(new Vector2(1f, 0f), false, true);
                    }

                    break;
                default:
                    return;
            }
        }

        private static int GetSize(EntityData data, Directions dir)
        {
            return dir > Directions.Down ? data.Height : data.Width;
        }

        private bool IsRiding(SourceSolidEntityState solid)
        {
            return direction switch
            {
                Directions.Up => CheckOutside(solid, Position + Vector2.UnitY),
                Directions.Down => CheckOutside(solid, Position - Vector2.UnitY),
                Directions.Left => CheckOutside(solid, Position + Vector2.UnitX),
                Directions.Right => CheckOutside(solid, Position - Vector2.UnitX),
                _ => false,
            };
        }

        private bool IsRiding(ISourceJumpThruEntity jumpThru)
        {
            return direction == Directions.Up && CheckAt(jumpThru, Position + Vector2.UnitY);
        }

        private bool CheckAt(ISourceEntityCollider other, Vector2 at)
        {
            Vector2 previous = Position;
            Position = at;
            bool result = SourceEntityCollision.Check(this, other);
            Position = previous;
            return result;
        }

        private bool CheckOutside(ISourceEntityCollider other, Vector2 at) =>
            !SourceEntityCollision.Check(this, other) && CheckAt(other, at);

        private readonly Directions direction;

        private Vector2 imageOffset;

        private readonly int size;

        private readonly string overrideType;

        private string spikeType;

        private Color disabledColor;

        public enum Directions
        {
            Up,
            Down,
            Left,
            Right
        }
    }
}
