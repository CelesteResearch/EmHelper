using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Mathematics;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display.Colors;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Mods.EmHelper.Modules;
using Celeste.Mods.PandorasBox.Components.Transport.Pipes;
using static Celeste.Mods.PandorasBox.Runtime.Transport.Pipes.ClearPipeGeometry;
using Actor = Celeste.Simulation.Gameplay.Actors.Foundation.SourceActorEntityState;
using Player = Celeste.Simulation.RoomPlayerSession;
using Level = Celeste.Game.Execution.Levels.OriginalLevelSceneRoot;
using BadelineDummy = Celeste.Game.Entities.BadelineDummy.BadelineDummyPresentation;
using Color = Celeste.Core.Display.PackedColor;
using Hitbox = Celeste.Physics.Collision.Shapes.HitboxColliderGeometry;
using PlayerCollider = Celeste.Simulation.Gameplay.Player.Collision.Components.PlayerColliderComponentState;
using VertexLight = Celeste.Simulation.Presentation.Lighting.VertexLightState;
using Holdable = Celeste.Simulation.Gameplay.Holdables.HoldableState;
using Collision = Celeste.Physics.Collision.SourceCollisionHandler;
using CollisionData = Celeste.Physics.Collision.PixelCollision;
using Seeker = Celeste.Game.Gameplay.Enemies.Seekers.OriginalSeekerState;
using Spring = Celeste.Game.Gameplay.Interaction.Springs.OriginalSpringState;
using DashSwitch = Celeste.Game.Gameplay.Mechanisms.DashSwitches.OriginalDashSwitchState;
using DeathEffect = Celeste.Simulation.Presentation.Player.Death.DeathEffect;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using MarioClearPipeInteraction = Celeste.Mods.PandorasBox.Components.Transport.Pipes.ClearPipeInteraction;
using Direction = Celeste.Mods.PandorasBox.Runtime.Transport.Pipes.ClearPipeDirection;
using OnPipeInteractionDelegate =
    Celeste.Mods.PandorasBox.Components.Transport.Pipes.ClearPipeInteraction.OnPipeInteractionDelegate;
using CanEnterPipeDelegate =
    Celeste.Mods.PandorasBox.Components.Transport.Pipes.ClearPipeInteraction.CanEnterPipeDelegate;

namespace Celeste.Mods.EmHelper.Entities.Actors.Walkers;

public partial class Walkeline : Actor
{
    public static Walkeline Load(Level level, Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room,
        Vector2 offset, Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data) => new(level, data, offset);

    public Walkeline(Level level, EntityData data, Vector2 offset) : this(level, data.Position + offset,
        SourceHexColor.HexToColor(data.Attr("haircolor", "212121")), data.Bool("left", false), data.Bool("weak", false),
        data.Bool("dangerous", false), data.Bool("ally", false), data.Bool("bouncy", false), data.Bool("smart", false),
        data.Bool("mute", false), data.Bool("nobackpack", false), data.Bool("idle", false),
        data.Attr("deathflag", "WalkelineIsDead"), data.Bool("triggerhappy", false))
    {
    }

    public Walkeline(Level level, Vector2 position, Color haircolor, bool left, bool weak, bool dangerous, bool ally,
        bool bouncy, bool smart, bool mute, bool nobackpack, bool idle, string deathflag, bool triggerhappy)
        : base(new PixelActor(((ISourcePlatformCollisionScene)level).PlatformWorld, position), level.EngineContext,
            ((ISourcePlatformTargetMotionEnvironment)level.EngineContext).TargetMotionPolicy)
    {
        engine = level.EngineContext;
        module = EmHelperModule.For(engine);
        module.Walkelines.PrepareSpriteMetadata();
        graphics = ((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine).GameGraphics;
        TriggerHappy = triggerhappy;
        hairColor = haircolor;
        this.deathflag = deathflag;
        this.idle = idle;
        isMute = mute;
        this.bouncy = bouncy;
        this.left = left;
        this.ally = ally;
        this.dangerous = dangerous;
        this.weak = weak;
        this.smart = smart;
        noBackpack = nobackpack;
        this.Collider = new Hitbox(8f, 11f, -4f, -11f);
        this.Add(new PlayerCollider((Action<Player>)OnPlayer, null, null));
        onCollideH = new Collision(OnCollideH);
        onCollideV = new Collision(OnCollideV);
        this.Add(new VertexLight(engine, this.Collider.Center, Color.White, 1f, 32, 64));
        Holdable val = new Holdable(engine, 0.1f);
        Holdable val2 = val;
        holdable = val;
        this.Add(val2);
        holdable.PickupCollider = new Hitbox(0f, 0f, 0f, 0f);
        holdable.OnHitSeeker = s => HitSeeker((Seeker)s);
        holdable.OnHitSpring = s => HitSpring((Spring)s);
        pandyInstalled = ((ISourceEverestModulesEnvironment)engine).Modules.Any(mod =>
            mod.GetType().Name.Equals("PandorasBoxMod") || mod.GetType().Name.Equals("PandorasBoxModule"));
        if (pandyInstalled)
        {
            CreatePipeInteraction();
        }
    }
}
