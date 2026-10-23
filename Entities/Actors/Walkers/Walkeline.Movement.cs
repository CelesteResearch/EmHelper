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
    public override void Update()
    {
        base.Update();
        if (dead)
        {
            return;
        }

        holdable.CheckAgainstColliders();
        if (this.OnGround(1))
        {
            if (speed.Y > 0f)
            {
                speed.Y = 0f;
            }

            if (walker.Sprite.Scale.X == 1f && !idle)
            {
                speed.X = 40f;
            }
            else if (!idle)
            {
                speed.X = -40f;
            }

            if (speed.X == 0f)
            {
                if (!inPipe)
                {
                    spinningHairSprite.Play("idle", false, false);
                    walker.Sprite.Play("idle", false, false);
                }
                else
                {
                    walker.Sprite.Play("spin", false, false);
                    spinningHairSprite.Play("spin", false, false);
                }
            }
            else if (!inPipe)
            {
                walker.Sprite.Play("walk", false, false);
                spinningHairSprite.Play("idle", false, false);
            }
            else
            {
                walker.Sprite.Play("spin", false, false);
                spinningHairSprite.Play("spin", false, false);
            }
        }
        else
        {
            float num = 800f;
            if (Math.Abs(speed.Y) <= 30f)
            {
                num *= 0.5f;
            }

            float num2 = 350f;
            if (speed.Y < 0f)
            {
                num2 *= 0.5f;
            }

            speed.X = ScalarMath.Approach(speed.X, 0f, num2 * engine.DeltaTime);
            if (noGravityTimer > 0f)
            {
                noGravityTimer -= engine.DeltaTime;
            }
            else
            {
                speed.Y = ScalarMath.Approach(speed.Y, 200f, num * engine.DeltaTime);
            }

            if (!inPipe)
            {
                walker.Sprite.Play("idle", false, false);
                spinningHairSprite.Play("idle", false, false);
            }
            else
            {
                walker.Sprite.Play("spin", false, false);
                spinningHairSprite.Play("spin", false, false);
            }
        }

        float top = this.Top;
        var bounds = (Scene as Level).Bounds;
        if (top > (float)bounds.Bottom)
        {
            Die();
        }
        else
        {
            float right = this.Right;
            bounds = (Scene as Level).Bounds;
            if (right > (float)bounds.Right && walker.Sprite.Scale.X == 1f)
            {
                Flip();
            }
            else
            {
                float num3 = this.Left;
                bounds = (Scene as Level).Bounds;
                if (num3 < (float)bounds.Left && walker.Sprite.Scale.X == -1f)
                {
                    Flip();
                }
            }
        }

        if (this.OnGround(1))
        {
            CheckSmartFlip();
        }

        bounceSfxDelay -= engine.DeltaTime;
        this.MoveH(speed.X * engine.DeltaTime, onCollideH, null);
        this.MoveV(speed.Y * engine.DeltaTime, onCollideV, null);
        walker.Position = this.Position;
        spinningHairSprite.Scale.X = walker.Sprite.Scale.X;
    }

    private void OnCollideH(CollisionData data)
    {
        if (data.Hit is DashSwitch)
        {
            ((DashSwitch)data.Hit).OnDashCollide.Invoke(null, Vector2.UnitX * (float)Math.Sign(speed.X));
        }

        Flip();
        if (Math.Abs(speed.X) > 100f)
        {
            ImpactParticles(data.Direction);
        }
    }

    private void OnCollideV(CollisionData data)
    {
        if (data.Hit is DashSwitch)
        {
            ((DashSwitch)data.Hit).OnDashCollide.Invoke(null, Vector2.UnitX * (float)Math.Sign(speed.Y));
        }

        if (speed.Y > 160f)
        {
            ImpactParticles(data.Direction);
        }
    }

    private void CheckSmartFlip()
    {
        if (!smart || !this.OnGround(1) || idle)
        {
            return;
        }

        if (walker.Sprite.Scale.X == 1f)
        {
            if (!this.OnGround(this.Position + new Vector2(8f, 0f), 1))
            {
                Flip();
            }
        }
        else if (!this.OnGround(this.Position + new Vector2(-8f, 0f), 1))
        {
            Flip();
        }
    }

    private void Flip()
    {
        if (!inPipe)
        {
            walker.Sprite.Scale.X = 0f - walker.Sprite.Scale.X;
        }
    }

    private void ImpactParticles(Vector2 dir)
    {
        float num;
        Vector2 val = default(Vector2);
        Vector2 val2;
        if (dir.X > 0f)
        {
            num = (float)Math.PI;
            val = new Vector2(this.Right, this.Y - 4f);
            val2 = Vector2.UnitY * 6f;
        }
        else if (dir.X < 0f)
        {
            num = 0f;
            val = new Vector2(this.Left, this.Y - 4f);
            val2 = Vector2.UnitY * 6f;
        }
        else if (dir.Y > 0f)
        {
            num = -(float)Math.PI / 2f;
            val = new Vector2(this.X, this.Bottom);
            val2 = Vector2.UnitX * 6f;
        }
        else
        {
            num = (float)Math.PI / 2f;
            val = new Vector2(this.X, this.Top);
            val2 = Vector2.UnitX * 6f;
        }

        (Scene as Level).Particles.Emit(((IOriginalGameplayProcess)engine).ParticleTypes.TheoCrystal.P_Impact, 12, val,
            val2, num);
    }
}
