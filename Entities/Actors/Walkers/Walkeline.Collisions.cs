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
    private void OnPlayer(Player player)
    {
        if (dead || inPipe)
        {
            return;
        }

        if (bouncy)
        {
            player.PointBounce(this.Center);
            if (bounceSfxDelay <= 0f)
            {
                if (!isMute)
                {
                    ((ISourceSpatialAudio)engine).Play("event:/game/general/crystalheart_bounce", walker.Position);
                }

                bounceSfxDelay = 0.1f;
            }

            ((Celeste.Input.Gameplay.Feedback.ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
        }

        if (dangerous)
        {
            player.Die(
                ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(player.Position - this.Position),
                false, true);
        }
        else if (weak)
        {
            Die();
        }
    }

    public void HitSeeker(Seeker seeker)
    {
        Die();
    }

    public bool HitSpring(Spring spring)
    {
        if ((int)spring.Orientation == 0 && speed.Y >= 0f)
        {
            this.MoveTowardsX(spring.CenterX, 4f, null);
            speed.Y = -160f;
            noGravityTimer = 0.15f;
            return true;
        }

        if ((int)spring.Orientation == 1 && speed.X <= 0f)
        {
            this.MoveTowardsY(spring.CenterY + 5f, 4f, null);
            speed.X = 220f;
            speed.Y = -80f;
            noGravityTimer = 0.1f;
            walker.Sprite.Scale.X = 1f;
            return true;
        }

        if ((int)spring.Orientation == 2 && speed.X >= 0f)
        {
            this.MoveTowardsY(spring.CenterY + 5f, 4f, null);
            speed.X = -220f;
            speed.Y = -80f;
            noGravityTimer = 0.1f;
            walker.Sprite.Scale.X = -1f;
            return true;
        }

        return false;
    }

    protected override void OnSquish(CollisionData data)
    {
        if (!this.TrySquishWiggle(data) &&
            !((IOriginalGameplayProcess)engine).SaveContext.CurrentSave.Assists.Invincible && !dead)
        {
            Die();
        }
    }

    public void Die()
    {
        if (dead)
        {
            return;
        }

        if (ally)
        {
            var entities = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(Player));
            Player entity = entities.Count == 0 ? null : (Player)entities[0];
            if (entity != null)
            {
                entity.Die(-Vector2.UnitX * (float)entity.Facing, false, true);
            }
        }

        ((ISourceSpatialAudio)engine).Play("event:/char/madeline/death", this.Position);
        (Scene as Level).Session.SetFlag(deathflag, true);
        this.Add(new DeathEffect(hairColor, engine, (Vector2?)(this.Center - this.Position)));
        walker.RemoveSelf();
        (holdable).RemoveSelf();
        dead = true;
        this.Depth = -1000000;
        this.Collidable = false;
        base.AllowPushing = false;
        TriggerHappy = false;
    }
}
