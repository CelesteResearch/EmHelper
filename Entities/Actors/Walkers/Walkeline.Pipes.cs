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
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private void CreatePipeInteraction()
    {
        MarioClearPipeInteraction val = new MarioClearPipeInteraction(new Vector2(0f, 8f))
        {
            OnPipeEnter = new OnPipeInteractionDelegate(OnPipeEnter),
            OnPipeExit = new OnPipeInteractionDelegate(OnPipeExit),
            OnPipeBlocked = new OnPipeInteractionDelegate(OnPipeBlocked),
            OnPipeUpdate = new OnPipeInteractionDelegate(OnPipeUpdate),
            CanEnterPipe = new CanEnterPipeDelegate(CanEnterPipe)
        };
        this.Add(val);
    }

    private bool CanEnterPipe(Entity entity, Direction direction)
    {
        return this.OnGround(1);
    }

    private void OnPipeBlocked(Entity entity, MarioClearPipeInteraction interaction)
    {
        Die();
    }

    private void OnPipeUpdate(Entity entity, MarioClearPipeInteraction interaction)
    {
        if (dead)
        {
            interaction.ExitEarly = true;
        }
    }

    private void OnPipeEnter(Entity entity, MarioClearPipeInteraction interaction)
    {
        inPipe = true;
    }

    private void OnPipeExit(Entity entity, MarioClearPipeInteraction interaction)
    {
        inPipe = false;
        speed = interaction.DirectionVector * interaction.CurrentClearPipe.TransportSpeed;
        walker.Sprite.Scale.X = ((speed.X > 0f) ? 1 : (-1));
    }
}
