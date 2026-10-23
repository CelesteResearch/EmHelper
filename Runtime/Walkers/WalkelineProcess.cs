using System;
using System.Linq.Expressions;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Scene.Triggers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Mods.EmHelper.Entities.Actors.Walkers;
using Celeste.Mods.EmHelper.Modules;

namespace Celeste.Mods.EmHelper.Runtime.Walkers;

public sealed class WalkelineProcess : IDisposable
{
    private readonly EmHelperModule module;
    private readonly ISourceEngineContext engine;
    private readonly Func<CassetteBlockState, ISourceEntityCollider, bool> wiggle;
    private ILHook triggerHook;
    private Hook cassetteHook;
    private Hook loadHook;
    private Hook metadataHook;
    private object metadataBank;

    public WalkelineProcess(EmHelperModule module, ISourceEngineContext engine)
    {
        this.module = module;
        this.engine = engine;
        var block = Expression.Parameter(typeof(CassetteBlockState));
        var actor = Expression.Parameter(typeof(ISourceEntityCollider));
        wiggle = Expression.Lambda<Func<CassetteBlockState, ISourceEntityCollider, bool>>(
            Expression.Call(block,
                typeof(CassetteBlockState).GetMethod("TryActorWiggleUp",
                    BindingFlags.Instance | BindingFlags.NonPublic), actor), block, actor).Compile();
        try
        {
            metadataHook = new Hook(
                typeof(OriginalGameGraphicsResources).GetMethod(nameof(OriginalGameGraphicsResources
                    .CreateFramesMetadata)),
                (Action<Action<OriginalGameGraphicsResources, string>, OriginalGameGraphicsResources, string>)
                CreateFramesMetadata);
            triggerHook =
                new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.CheckSourcePlayerTriggers)),
                    PatchTriggerContact);
            cassetteHook = new Hook(typeof(CassetteBlockState).GetMethod(nameof(CassetteBlockState.BlockedCheck)),
                (Func<Func<CassetteBlockState, bool>, CassetteBlockState, bool>)BlockedCheck);
            loadHook = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot, PlayerIntroType,
                    bool>)LoadLevel);
        }
        catch
        {
            Dispose();
            throw;
        }
    }

    private void CreateFramesMetadata(Action<OriginalGameGraphicsResources, string> original,
        OriginalGameGraphicsResources graphics, string sprite)
    {
        original(graphics, sprite);
        if (sprite != "player_playback" || !ReferenceEquals(graphics,
                ((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine).GameGraphics)) return;
        // Source graphics preparation ends with this vanilla metadata batch on every level load.
        metadataBank = null;
        PrepareSpriteMetadata();
    }

    public void PrepareSpriteMetadata()
    {
        var graphics =
            (OriginalGameGraphicsResources)((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine)
            .GameGraphics;
        if (ReferenceEquals(metadataBank, graphics.SpriteBank)) return;
        graphics.CreateFramesMetadata("walkeline");
        graphics.CreateFramesMetadata("windupwalkeline");
        metadataBank = graphics.SpriteBank;
    }

    private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> original, OriginalLevelSceneRoot level,
        PlayerIntroType intro, bool fromLoader)
    {
        if (ReferenceEquals(level.EngineContext, engine)) module.LevelHasTriggerHappyWalkeline = false;
        original(level, intro, fromLoader);
    }

    private void PatchTriggerContact(ILContext il)
    {
        var cursor = new ILCursor(il);
        if (!cursor.TryGotoNext(MoveType.Before,
                i => (i.OpCode == OpCodes.Call || i.OpCode == OpCodes.Callvirt) &&
                     i.Operand is Mono.Cecil.MethodReference method && method.Name == "Update" &&
                     method.DeclaringType.FullName.StartsWith("Celeste.Scene.Triggers.PlayerTriggerSet`1",
                         StringComparison.Ordinal)))
            throw new InvalidOperationException("Player trigger contact call was not found.");
        cursor.Remove();
        cursor
            .EmitDelegate<
                Action<PlayerTriggerSet<RoomPlayerSession>, RoomPlayerSession,
                    ISourcePlayerTrigger<RoomPlayerSession>>>(UpdateContact);
    }

    private void UpdateContact(PlayerTriggerSet<RoomPlayerSession> contacts, RoomPlayerSession player,
        ISourcePlayerTrigger<RoomPlayerSession> trigger)
    {
        bool contact = player.Collider != null && trigger.Collider != null && !ReferenceEquals(player, trigger) &&
                       trigger.Collidable && player.Collider.Collide(trigger.Collider);
        if (!contact && module.LevelHasTriggerHappyWalkeline && ReferenceEquals(player.ActorEngineContext, engine))
        {
            var walkers = ((ISourceScene)player.Scene).SourceScene.Entities.GetTrackedEntities(typeof(Walkeline));
            foreach (var entity in walkers)
                if (entity is Walkeline walker && SourceEntityCollision.Check(trigger, walker) && walker.TriggerHappy)
                {
                    contact = true;
                    break;
                }
        }

        contacts.UpdateContact(player, trigger, contact);
    }

    private bool BlockedCheck(Func<CassetteBlockState, bool> original, CassetteBlockState block)
    {
        if (block.Scene is OriginalLevelSceneRoot level && ReferenceEquals(level.EngineContext, engine))
        {
            var walkers = ((ISourceScene)block.Scene).SourceScene.Entities.GetTrackedEntities(typeof(Walkeline));
            foreach (var entity in walkers)
                if (entity is Walkeline walker && SourceEntityCollision.Check(block, walker))
                    return !wiggle(block, walker) || original(block);
        }

        return original(block);
    }

    public void Dispose()
    {
        metadataHook?.Dispose();
        metadataHook = null;
        loadHook?.Dispose();
        loadHook = null;
        cassetteHook?.Dispose();
        cassetteHook = null;
        triggerHook?.Dispose();
        triggerHook = null;
    }
}
