#if ENTITIES
using StatusEffects.Modules;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
#if NETCODE
    [CreateAfter(typeof(DefaultVariantSystemGroup))]
#endif
    public partial class StatusReferencesSetupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var statusEffectDatas = StatusEffectDatabase.Get().Values.Values;
            
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var references = commandBuffer.CreateEntity();
            commandBuffer.SetName(references, "Status References");
            commandBuffer.AddBuffer<ModulePrefabs>(references);

            BlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>> statusEffectDataHashMapReferences;
            // Setup status effect datas
            var builder = new BlobBuilder(Allocator.Temp);
            ref var statusEffectDataHashMapRoot = ref builder.ConstructRoot<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>>();
            var statusEffectDataHashMap = builder.AllocateHashMap(ref statusEffectDataHashMapRoot, statusEffectDatas.Count);
            global::StatusEffects.Effect effect;
            global::StatusEffects.Condition condition;
            int moduleIndex = 0;

            foreach (var statusEffectData in statusEffectDatas)
            {
                // Rare case where data is null. This should never happen.
                if (!statusEffectData)
                    continue;

                var subBuilder = new BlobBuilder(Allocator.Temp);

                ref StatusEffectData statusEffectDataRoot = ref subBuilder.ConstructRoot<StatusEffectData>();
                statusEffectDataRoot.Id = statusEffectData.Id;
                statusEffectDataRoot.Group = statusEffectData.Group;
                statusEffectDataRoot.ComparableName = statusEffectData.ComparableName ? statusEffectData.ComparableName.Id : default;
                statusEffectDataRoot.BaseValue = statusEffectData.BaseValue;
                statusEffectDataRoot.Icon = statusEffectData.Icon;
#if LOCALIZED
                subBuilder.AllocateString(ref statusEffectDataRoot.StatusEffectNameTable, statusEffectData.StatusEffectName.TableReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.StatusEffectNameEntry, statusEffectData.StatusEffectName.TableEntryReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.DescriptionTable, statusEffectData.StatusEffectName.TableReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.DescriptionEntry, statusEffectData.StatusEffectName.TableReference.ToString());
#else
                subBuilder.AllocateString(ref statusEffectDataRoot.StatusEffectName, statusEffectData.StatusEffectName);
                subBuilder.AllocateString(ref statusEffectDataRoot.Description, statusEffectData.Description);
#endif
                statusEffectDataRoot.AllowEffectStacking = statusEffectData.AllowEffectStacking;
                statusEffectDataRoot.NonStackingBehaviour = statusEffectData.NonStackingBehaviour;
                statusEffectDataRoot.MaxStacks = statusEffectData.MaxStacks;
                var effects = subBuilder.Allocate(ref statusEffectDataRoot.Effects, statusEffectData.Effects.Count);
                for (int i = 0; i < effects.Length; i++)
                {
                    effect = statusEffectData.Effects[i];
                    effects[i] = new Effect
                    {
                        Id = effect.StatusName ? effect.StatusName.Id : default,
                        ValueModifier = effect.ValueModifier,
                        UseBaseValue = effect.UseBaseValue,
                        FloatValue = effect.FloatValue,
                        IntValue = effect.IntValue,
                        BoolValue = effect.BoolValue,
                        Priority = effect.Priority
                    };
                }
                var conditions = subBuilder.Allocate(ref statusEffectDataRoot.Conditions, statusEffectData.Conditions.Count);
                for (int i = 0; i < conditions.Length; i++)
                {
                    condition = statusEffectData.Conditions[i];
                    conditions[i] = new Condition()
                    {
                        SearchableConfigurable = condition.SearchableConfigurable,
                        SearchableData = condition.SearchableData ? condition.SearchableData.Id : default,
                        SearchableComparableName = condition.SearchableComparableName ? condition.SearchableComparableName.Id : default,
                        SearchableGroup = condition.SearchableGroup,
                        Exists = condition.Exists,
                        Add = condition.Add,
                        Scaled = condition.Scaled,
                        UseStacks = condition.UseStacks,
                        Stacks = condition.Stacks,
                        ActionConfigurable = condition.ActionConfigurable,
                        ActionData = condition.ActionData ? condition.ActionData.Id : default,
                        ActionComparableName = condition.ActionComparableName ? condition.ActionComparableName.Id : default,
                        ActionGroup = condition.ActionGroup,
                        Timing = condition.Timing,
                        Duration = condition.Duration
                    };
                }
                // Modules just stores the buffer index for the module. This is
                // because we cannot store Entity references directly on a blob asset.
                IList<ModuleContainer> entityModules = statusEffectData.Modules.Where((m) => m.Module is IEntityModule).ToList();
                
                if (entityModules.Count > 0)
                {
                    var moduleEntity = commandBuffer.CreateEntity();
                    commandBuffer.SetName(moduleEntity, $"{statusEffectData.name} Module");
                    commandBuffer.AddComponent<Prefab>(moduleEntity);
                    commandBuffer.AddComponent<Module>(moduleEntity);
                    commandBuffer.AddComponent<ModuleUpdateTag>(moduleEntity);
                    commandBuffer.AddComponent<ModuleDestroyTag>(moduleEntity);
                    commandBuffer.AddComponent<ModuleCleanupTag>(moduleEntity);
                    foreach (var entityModule in entityModules)
                        // Call each modify command buffer on each module so that it adds
                        // whatever component/buffers it wants. This way, a custom system
                        // can be made to act on those components/buffers when they get
                        // instantiated as children of the entity they effect.
                        (entityModule.Module as IEntityModule).ModifyCommandBuffer(ref commandBuffer, moduleEntity, entityModule.ModuleInstance);

                    commandBuffer.AppendToBuffer(references, new ModulePrefabs() { Entity = moduleEntity });
                    statusEffectDataRoot.ModulePrefabIndex = moduleIndex;
                    moduleIndex++;
                }
                else
                {
                    // There weren't any modules.
                    statusEffectDataRoot.ModulePrefabIndex = -1;
                }

                statusEffectDataHashMap.Add(statusEffectData.Id, subBuilder.CreateBlobAssetReference<StatusEffectData>(Allocator.Persistent));
                subBuilder.Dispose();
            }

            statusEffectDataHashMapReferences = builder.CreateBlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>>(Allocator.Persistent);
            builder.Dispose();
            
            commandBuffer.AddComponent(references, new StatusReferences
            {
                BlobAsset = statusEffectDataHashMapReferences,
            });

            RequireForUpdate<ModulePrefabs>();
        }

        protected override void OnUpdate() 
        {
#if NETCODE
            var prefabs = SystemAPI.GetSingletonBuffer<ModulePrefabs>().Reinterpret<Entity>().ToNativeArray(Allocator.Temp);
            var blobAssets = SystemAPI.GetSingleton<StatusReferences>().BlobAsset.Value.GetValueArray(Allocator.Temp);

            foreach (var blobAsset in blobAssets)
            {
                ref var data = ref blobAsset.Value;

                if (data.ModulePrefabIndex < 0)
                    continue;

                GhostPrefabCreation.ConvertToGhostPrefab(EntityManager, prefabs[data.ModulePrefabIndex], new GhostPrefabCreation.Config()
                {
                    Name = data.Id.ToString(),
                    DefaultGhostMode = GhostMode.Interpolated,
                    SupportedGhostModes = GhostModeMask.Interpolated,
                    OptimizationMode = GhostOptimizationMode.Static
                });
            }

            blobAssets.Dispose();
#endif
            Enabled = false;
        }

        protected override void OnDestroy()
        {
            var references = SystemAPI.GetSingleton<StatusReferences>();

            var statusEffectDatas = references.BlobAsset.Value.GetValueArray(Allocator.Temp);
            foreach (var reference in statusEffectDatas)
                reference.Dispose();
            references.BlobAsset.Dispose();
            // Now in case this isn't being destroyed due
            // to exiting play mode we destroy the singleton.
            EntityManager.DestroyEntity(GetEntityQuery(typeof(StatusReferences)));
        }
    }
}
#endif