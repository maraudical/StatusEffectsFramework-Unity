#if ENTITIES
using StatusEffects.Modules;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(BeginStatusEffectEntityCommandBufferSystem))]
    public partial class StatusReferencesSetupSystem : SystemBase
    {
        public EntityQuery m_ReferencesQuery;
        public EntityQuery m_RequestQuery;

        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(StatusReferencesSetupRequest));

            m_ReferencesQuery = SystemAPI.QueryBuilder().WithAll<StatusReferences>().Build();
            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<StatusReferencesSetupRequest>().Build();

            RequireForUpdate(m_RequestQuery);
        }

        protected override void OnUpdate() 
        {
            var statusEffectDatas = StatusEffectDatabase.Get().ReadOnlyDictionary.Values;

            if (statusEffectDatas.Count <= 0)
                return;

            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);

            commandBuffer.DestroyEntity(m_RequestQuery, EntityQueryCaptureMode.AtPlayback);

            var referencesEntity = commandBuffer.CreateEntity();
            
            commandBuffer.SetName(referencesEntity, "Status References");

            var idToStatusEffectDataMapBuilder = new BlobBuilder(Allocator.Temp);
            ref var idToStatusEffectDataMapRoot = ref idToStatusEffectDataMapBuilder.ConstructRoot<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>>();
            var idToStatusEffectDataMap = idToStatusEffectDataMapBuilder.AllocateHashMap(ref idToStatusEffectDataMapRoot, statusEffectDatas.Count);
            var moduleToSystemTypeDictionary = new Dictionary<ulong, ulong>();
            global::StatusEffects.Effect effect;
            global::StatusEffects.Condition condition;
            
            // Dispose of old blobs after copying
            OnDestroy();

            // Setup status effect datas
            foreach (var statusEffectData in statusEffectDatas)
            {
                // Rare case where data is null. This should never happen.
                if (!statusEffectData || idToStatusEffectDataMap.ContainsKey(statusEffectData.Id))
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
                List<ModuleContainer> entityModuleContainers = statusEffectData.Modules.Where((m) => m.Module is IEntityModule).ToList();
                var modules = subBuilder.Allocate(ref statusEffectDataRoot.Modules, entityModuleContainers.Count);

                if (entityModuleContainers.Count > 0)
                {
                    for (int i = 0; i < modules.Length; i++)
                    unsafe {
                        var moduleContainer = entityModuleContainers[i];
                        var entityModule = (IEntityModule)moduleContainer.Module;
                        
                        var moduleInfo = entityModule.CreateModuleInfo(moduleContainer.ModuleInstance);
                        var systemTypeHash = TypeManager.GetTypeInfo(TypeManager.GetTypeIndex(entityModule.ModuleSystemType())).StableTypeHash;
                        moduleToSystemTypeDictionary.TryAdd(moduleInfo.StableTypeHash, systemTypeHash);
                        
                        modules[i] = moduleInfo;
                    }
                }

                idToStatusEffectDataMap.Add(statusEffectData.Id, subBuilder.CreateBlobAssetReference<StatusEffectData>(Allocator.Persistent));
                subBuilder.Dispose();
            }

            var statusEffectDataMapBlob = idToStatusEffectDataMapBuilder.CreateBlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>>(Allocator.Persistent);
            idToStatusEffectDataMapBuilder.Dispose();

            // Copy module to system type dictionary to blob hash map
            var moduleToSystemTypeMapBlob = BlobAssetReference<BlobHashMap<ulong, ulong>>.Null;

            if (moduleToSystemTypeDictionary.Count > 0)
            {
                var moduleToSystemTypeMapBuilder = new BlobBuilder(Allocator.Temp);
                ref var moduleToSystemTypeMapRoot = ref moduleToSystemTypeMapBuilder.ConstructRoot<BlobHashMap<ulong, ulong>>();
                var moduleToSystemTypeMap = moduleToSystemTypeMapBuilder.AllocateHashMap(ref moduleToSystemTypeMapRoot, moduleToSystemTypeDictionary.Count);

                foreach (var kvp in moduleToSystemTypeDictionary)
                    moduleToSystemTypeMap.Add(kvp.Key, kvp.Value);

                moduleToSystemTypeMapBlob = moduleToSystemTypeMapBuilder.CreateBlobAssetReference<BlobHashMap<ulong, ulong>>(Allocator.Persistent);
                moduleToSystemTypeMapBuilder.Dispose();
            }

            commandBuffer.AddComponent(referencesEntity, new StatusReferences
            {
                IdToStatusEffectDataMap = statusEffectDataMapBlob,
                ModuleToSystemTypeMap = moduleToSystemTypeMapBlob
            });
        }

        protected override void OnDestroy()
        {
            if (SystemAPI.TryGetSingleton<StatusReferences>(out var references))
            {
                using var statusEffectDataBlobs = references.IdToStatusEffectDataMap.Value.GetValueArray(Allocator.Temp);

                foreach (var blob in statusEffectDataBlobs)
                {
                    ref var statusEffectData = ref blob.Value;
                    for (int i = 0; i < statusEffectData.Modules.Length; i++)
                    unsafe {
                        var modulePtr = statusEffectData.Modules[i];
                        UnsafeUtility.Free(modulePtr.Ptr.ToPointer(), Allocator.Persistent);
                    }
                    blob.Dispose();
                }

                if (references.IdToStatusEffectDataMap.IsCreated)
                    references.IdToStatusEffectDataMap.Dispose();

                if (references.ModuleToSystemTypeMap.IsCreated)
                    references.ModuleToSystemTypeMap.Dispose();
            }
        }
    }
}
#endif