#if ENTITIES
using StatusEffects.Modules;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginStatusEffectEntityCommandBufferSystem))]
    public partial class StatusReferencesSetupSystem : SystemBase
    {
        public EntityQuery m_RequestQuery;

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(StatusReferencesSetupRequest));

            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<StatusReferencesSetupRequest>().Build();

            RequireForUpdate(m_RequestQuery);
        }

        protected override void OnUpdate() 
        {
            var statusEffectDatas = StatusEffectDatabase.Get().ReadOnlyDictionary.Values;

            if (statusEffectDatas.Count <= 0)
                return;

            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            commandBuffer.DestroyEntity(m_RequestQuery, EntityQueryCaptureMode.AtPlayback);

            var referencesEntity = commandBuffer.CreateEntity();
            
            commandBuffer.SetName(referencesEntity, "Status References");
            commandBuffer.AddBuffer<ModulePrefabs>(referencesEntity);

            BlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>> statusEffectDataHashMapReferences;
            // Setup status effect datas
            var builder = new BlobBuilder(Allocator.Temp);
            ref var statusEffectDataHashMapRoot = ref builder.ConstructRoot<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>>();
            var statusEffectDataHashMap = builder.AllocateHashMap(ref statusEffectDataHashMapRoot, statusEffectDatas.Count);
            global::StatusEffects.Effect effect;
            global::StatusEffects.Condition condition;
            int moduleIndex = 0;

            bool foundSingleton = SystemAPI.TryGetSingleton<StatusReferences>(out var references);

            // Dispose of old blobs after copying
            if (foundSingleton)
            {
                var buffer = SystemAPI.GetSingletonBuffer<ModulePrefabs>();
                for (int i = buffer.Length - 1; i >= 0; i--)
                    commandBuffer.AppendToBuffer(referencesEntity, buffer[i]);

                var statusEffectDataBlobs = references.BlobAsset.Value.GetValueArray(Allocator.Temp);
                foreach (var blob in statusEffectDataBlobs)
                {
                    statusEffectDataHashMap.Add(blob.Value.Id, blob);
                    if (blob.Value.ModulePrefabIndex >= 0)
                        moduleIndex++;
                }

                references.BlobAsset.Dispose();
                commandBuffer.DestroyEntity(SystemAPI.GetSingletonEntity<StatusReferences>());
            }

            foreach (var statusEffectData in statusEffectDatas)
            {
                // Rare case where data is null. This should never happen.
                if (!statusEffectData || statusEffectDataHashMap.ContainsKey(statusEffectData.Id))
                    continue;

                var subBuilder = new BlobBuilder(Allocator.Temp);

                ref StatusEffectData statusEffectDataRoot = ref subBuilder.ConstructRoot<StatusEffectData>();
                statusEffectDataRoot.Id = statusEffectData.Id;
                statusEffectDataRoot.Group = statusEffectData.Group;
                statusEffectDataRoot.ComparableName = statusEffectData.ComparableName ? statusEffectData.ComparableName.Id : default;
                statusEffectDataRoot.BaseValue = statusEffectData.BaseValue;
                statusEffectDataRoot.Icon = statusEffectData.Icon;
                UnityEngine.Color color = statusEffectData.Color;
                statusEffectDataRoot.Color = new(color.r, color.g, color.b, color.a);
#if LOCALIZED
                subBuilder.AllocateString(ref statusEffectDataRoot.StatusEffectNameTable, statusEffectData.StatusEffectName.TableReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.StatusEffectNameEntry, statusEffectData.StatusEffectName.TableEntryReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.AcronymTable, statusEffectData.Acronym.TableReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.AcronymEntry, statusEffectData.Acronym.TableReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.DescriptionTable, statusEffectData.Description.TableReference.ToString());
                subBuilder.AllocateString(ref statusEffectDataRoot.DescriptionEntry, statusEffectData.Description.TableReference.ToString());
#else
                subBuilder.AllocateString(ref statusEffectDataRoot.StatusEffectName, statusEffectData.StatusEffectName);
                subBuilder.AllocateString(ref statusEffectDataRoot.Acronym, statusEffectData.Acronym);
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

                    commandBuffer.AppendToBuffer(referencesEntity, new ModulePrefabs() { Entity = moduleEntity });
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

            commandBuffer.AddComponent(referencesEntity, new StatusReferences
            {
                BlobAsset = statusEffectDataHashMapReferences,
            });
        }

        protected override void OnDestroy()
        {
            if (SystemAPI.TryGetSingleton<StatusReferences>(out var references))
            {
                var statusEffectDataBlobs = references.BlobAsset.Value.GetValueArray(Allocator.Temp);
                foreach (var blob in statusEffectDataBlobs)
                    blob.Dispose();
                references.BlobAsset.Dispose();
            }
        }
    }
}
#endif