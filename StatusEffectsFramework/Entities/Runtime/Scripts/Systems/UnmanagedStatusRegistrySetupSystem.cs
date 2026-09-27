#if ENTITIES
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    public partial class UnmanagedStatusRegistrySetupSystem : SystemBase
    {
        public const string RegistryName = "StatusRegistry";
        public EntityQuery m_RegistryQuery;
        public EntityQuery m_RequestQuery;
        public EntityQuery m_ResolverQuery;
        private ushort m_Version;

        protected override void OnCreate()
        {
            m_RegistryQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistry>().Build();
            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistrySetupRequest>().Build();
            m_ResolverQuery = SystemAPI.QueryBuilder().WithAllRW<StatusFloats>().WithAllRW<StatusInts>().WithAllRW<StatusBools>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState).Build();

            m_Version = 1;

            StatusRegistry.Get().RegistryRebuilt += OnRegistryRebuilt;
            OnRegistryRebuilt();

            RequireForUpdate(m_RequestQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            
            commandBuffer.DestroyEntity(m_RequestQuery, EntityQueryCaptureMode.AtPlayback);

            var registry = StatusRegistry.Get();
            var idToStatusEffectDatas = registry.IdToStatusEffectData;
            var keyToIds = registry.KeyToId;

            var keyToIdBuilder = new BlobBuilder(Allocator.Temp);
            ref var keyToIdRoot = ref keyToIdBuilder.ConstructRoot<BlobHashMap<Hash128, ushort>>();
            var keyToIdMap = keyToIdBuilder.AllocateHashMap(ref keyToIdRoot, keyToIds.Count);

            foreach (var kvp in keyToIds)
                keyToIdMap.Add(kvp.Key, kvp.Value);

            var idToStatusEffectDataBuilder = new BlobBuilder(Allocator.Temp);
            ref var idToStatusEffectDataRoot = ref idToStatusEffectDataBuilder.ConstructRoot<BlobHashMap<ushort, UnmanagedStatusEffectData>>();
            var idToStatusEffectDataMap = idToStatusEffectDataBuilder.AllocateHashMap(ref idToStatusEffectDataRoot, idToStatusEffectDatas.Count);

            // Every dynamic effect and module buffer type used by the registry. Systems that access
            // these buffers through raw pointers register them so job dependencies are tracked.
            var dynamicEffectTypes = new HashSet<TypeIndex>();
            var moduleTypes = new HashSet<TypeIndex>();

            try
            {
                // Setup status effect datas
                foreach (var kvp in idToStatusEffectDatas)
                {
                    var statusEffectData = kvp.Value;

                    // Case where data is null. This should never happen.
                    if (!statusEffectData)
                        continue;

                    ref UnmanagedStatusEffectData unmanagedStatusEffectData = ref idToStatusEffectDataMap.AddByRef(kvp.Key);
                    unmanagedStatusEffectData.Id = kvp.Key;
                    unmanagedStatusEffectData.Group = statusEffectData.Group;
                    ushort comparableName = StatusRegistry.NullId;
                    if (statusEffectData.ComparableName && !keyToIds.TryGetValue(statusEffectData.ComparableName.GetUniqueKeyHash(), out comparableName))
                        DebugError(statusEffectData.ComparableName.UniqueKey);
                    unmanagedStatusEffectData.ComparableName = comparableName;
                    unmanagedStatusEffectData.BaseValue = statusEffectData.BaseValue;
                    unmanagedStatusEffectData.Icon = statusEffectData.Icon;
                    UnityEngine.Color color = statusEffectData.Color;
                    unmanagedStatusEffectData.Color = new(color.r, color.g, color.b, color.a);
#if LOCALIZED
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.StatusEffectNameTable, statusEffectData.StatusEffectName.TableReference.ToString());
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.StatusEffectNameEntry, statusEffectData.StatusEffectName.TableEntryReference.ToString());
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.AcronymTable, statusEffectData.Acronym.TableReference.ToString());
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.AcronymEntry, statusEffectData.Acronym.TableReference.ToString());
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.DescriptionTable, statusEffectData.Description.TableReference.ToString());
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.DescriptionEntry, statusEffectData.Description.TableReference.ToString());
#else
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.StatusEffectName, statusEffectData.StatusEffectName);
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.Acronym, statusEffectData.Acronym);
                    idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.Description, statusEffectData.Description);
#endif
                    unmanagedStatusEffectData.AllowEffectStacking = statusEffectData.AllowEffectStacking;
                    unmanagedStatusEffectData.NonStackingBehaviour = statusEffectData.NonStackingBehaviour;
                    unmanagedStatusEffectData.MaxStacks = statusEffectData.MaxStacks;

                    List<Effect> entityEffects = statusEffectData.Effects.Where((e) => e.ValueSource is not ValueSource.DynamicValue || e.StatusName switch 
                    { 
                        StatusNameInt => e.DynamicIntEffect is IEntityDynamicEffect,
                        StatusNameBool => e.DynamicBoolEffect is IEntityDynamicEffect,
                        _ => e.DynamicFloatEffect is IEntityDynamicEffect,
                    }).ToList();
                    var effects = idToStatusEffectDataBuilder.Allocate(ref unmanagedStatusEffectData.Effects, entityEffects.Count);

                    for (int i = 0; i < effects.Length; i++)
                    {
                        var effect = entityEffects[i];

                        if (effect.StatusName == null)
                            continue;

                        if (!keyToIds.TryGetValue(effect.StatusName.GetUniqueKeyHash(), out var statusName))
                            DebugError(effect.StatusName.UniqueKey);

                        ref var unmanagedEffect = ref effects[i];

                        unmanagedEffect = new UnmanagedEffect
                        {
                            Id = statusName,
                            ValueModifier = effect.ValueModifier,
                            ValueSource = effect.ValueSource,
                            Priority = effect.Priority,
                            FloatValue = effect.FloatValue,
                            IntValue = effect.IntValue,
                            BoolValue = effect.BoolValue,
                        };

                        switch (effect.StatusName)
                        {
                            case StatusNameFloat:
                                if (effect.ValueSource is ValueSource.DynamicValue)
                                    if (effect.DynamicFloatEffect && effect.DynamicFloatEffect is IEntityDynamicEffect entityDynamicEffect)
                                    {
                                        entityDynamicEffect.CreateDynamicEffectInfo(ValueType.Float, ref unmanagedEffect.DynamicEffectInfo, ref idToStatusEffectDataBuilder);
                                        unmanagedEffect.PostEvaluate = effect.DynamicFloatEffect.PostEvaluate;
                                    }
                                    else
                                        continue;
                                unmanagedEffect.ValueType = ValueType.Float;
                                break;
                            case StatusNameInt:
                                if (effect.ValueSource is ValueSource.DynamicValue)
                                    if (effect.DynamicIntEffect && effect.DynamicIntEffect is IEntityDynamicEffect entityDynamicEffect)
                                    {
                                        entityDynamicEffect.CreateDynamicEffectInfo(ValueType.Int, ref unmanagedEffect.DynamicEffectInfo, ref idToStatusEffectDataBuilder);
                                        unmanagedEffect.PostEvaluate = effect.DynamicIntEffect.PostEvaluate;
                                    }
                                    else
                                        continue;
                                unmanagedEffect.ValueType = ValueType.Int;
                                break;
                            case StatusNameBool:
                                if (effect.ValueSource is ValueSource.DynamicValue)
                                    if (effect.DynamicBoolEffect && effect.DynamicBoolEffect is IEntityDynamicEffect entityDynamicEffect)
                                    {
                                        entityDynamicEffect.CreateDynamicEffectInfo(ValueType.Bool, ref unmanagedEffect.DynamicEffectInfo, ref idToStatusEffectDataBuilder);
                                        unmanagedEffect.PostEvaluate = effect.DynamicBoolEffect.PostEvaluate;
                                    }
                                    else
                                        continue;
                                unmanagedEffect.ValueType = ValueType.Bool;
                                break;
                        }

                        if (unmanagedEffect.ValueSource is ValueSource.DynamicValue && unmanagedEffect.DynamicEffectInfo.TypeIndex != TypeIndex.Null)
                            dynamicEffectTypes.Add(unmanagedEffect.DynamicEffectInfo.TypeIndex);
                    }

                    var conditions = idToStatusEffectDataBuilder.Allocate(ref unmanagedStatusEffectData.Conditions, statusEffectData.Conditions.Count);

                    for (int i = 0; i < conditions.Length; i++)
                    {
                        var condition = statusEffectData.Conditions[i];

                        ushort searchableData = StatusRegistry.NullId;
                        if (condition.SearchableData && !keyToIds.TryGetValue(condition.SearchableData.GetUniqueKeyHash(), out searchableData))
                            DebugError(condition.SearchableData.UniqueKey);
                        ushort searchableComparableName = StatusRegistry.NullId;
                        if (condition.SearchableComparableName && !keyToIds.TryGetValue(condition.SearchableComparableName.GetUniqueKeyHash(), out searchableComparableName))
                            DebugError(condition.SearchableComparableName.UniqueKey);
                        ushort actionData = StatusRegistry.NullId;
                        if (condition.ActionData && !keyToIds.TryGetValue(condition.ActionData.GetUniqueKeyHash(), out actionData))
                            DebugError(condition.ActionData.UniqueKey);
                        ushort actionComparableName = StatusRegistry.NullId;
                        if (condition.ActionComparableName && !keyToIds.TryGetValue(condition.ActionComparableName.GetUniqueKeyHash(), out actionComparableName))
                            DebugError(condition.ActionComparableName.UniqueKey);

                        conditions[i] = new UnmanagedCondition()
                        {
                            SearchableConfigurable = condition.SearchableConfigurable,
                            SearchableData = searchableData,
                            SearchableComparableName = searchableComparableName,
                            SearchableGroup = condition.SearchableGroup,
                            Exists = condition.Exists,
                            Add = condition.Add,
                            Scaled = condition.Scaled,
                            UseStacks = condition.UseStacks,
                            Stacks = condition.Stacks,
                            ActionConfigurable = condition.ActionConfigurable,
                            ActionData = actionData,
                            ActionComparableName = actionComparableName,
                            ActionGroup = condition.ActionGroup,
                            Timing = condition.Timing,
                            Duration = condition.Duration
                        };
                    }

                    // Modules just stores the buffer index for the module. This is
                    // because we cannot store Entity references directly on a blob asset.
                    List<ModuleContainer> entityModuleContainers = statusEffectData.Modules.Where((m) => m.Module is IEntityModule).ToList();
                    var modules = idToStatusEffectDataBuilder.Allocate(ref unmanagedStatusEffectData.Modules, entityModuleContainers.Count);
                    
                    if (entityModuleContainers.Count > 0)
                    {
                        for (int i = 0; i < modules.Length; i++)
                        {
                            var moduleContainer = entityModuleContainers[i];
                            var entityModule = (IEntityModule)moduleContainer.Module;

                            entityModule.CreateModuleInfo(moduleContainer.ModuleInstance, ref modules[i], ref idToStatusEffectDataBuilder);

                            if (modules[i].TypeIndex != TypeIndex.Null)
                                moduleTypes.Add(modules[i].TypeIndex);
                        }
                    }

                    void DebugError(string uniqueKey) => UnityEngine.Debug.LogError($"Trying to setup an {nameof(UnmanagedStatusEffectData)} with the {nameof(Registrant)} that contains the unique key \"{uniqueKey}\" but the registry doesn't contain the ID associated with it.");
                }

                // Create all blobs together so a failure partway through doesn't leak the ones already created.
                var unmanagedRegistry = new UnmanagedStatusRegistry();
                try
                {
                    unmanagedRegistry.KeyToId = keyToIdBuilder.CreateBlobAssetReference<BlobHashMap<Hash128, ushort>>(Allocator.Persistent);
                    unmanagedRegistry.IdToStatusEffectData = idToStatusEffectDataBuilder.CreateBlobAssetReference<BlobHashMap<ushort, UnmanagedStatusEffectData>>(Allocator.Persistent);
                    unmanagedRegistry.DynamicEffectTypes = CreateTypeArray(dynamicEffectTypes);
                    unmanagedRegistry.ModuleTypes = CreateTypeArray(moduleTypes);
                }
                catch
                {
                    Dispose(unmanagedRegistry);
                    throw;
                }

                bool hasOldRegistry = !m_RegistryQuery.IsEmptyIgnoreFilter;
                if (hasOldRegistry)
                    m_Version++;

                unmanagedRegistry.Version = m_Version;

                if (hasOldRegistry)
                {
                    // Swap the singleton in place so nothing can read the old blobs after this point,
                    // then dispose them once every job that could still be reading them has completed.
                    m_RegistryQuery.CompleteDependency();
                    var oldRegistry = m_RegistryQuery.GetSingleton<UnmanagedStatusRegistry>();
                    m_RegistryQuery.SetSingleton(unmanagedRegistry);
                    Dispose(oldRegistry);
                }
                else
                {
                    var registryEntity = EntityManager.CreateEntity();
                    EntityManager.SetName(registryEntity, RegistryName);
                    EntityManager.AddComponentData(registryEntity, unmanagedRegistry);
                }

                // Resolve all status variables.
                var job = new StatusVariableIdResolverJob
                {
                    Registry = unmanagedRegistry
                };
                Dependency = job.ScheduleParallelByRef(m_ResolverQuery, Dependency);
            }
            finally
            {
                keyToIdBuilder.Dispose();
                idToStatusEffectDataBuilder.Dispose();
            }
        }

        protected override void OnDestroy()
        {
            StatusRegistry.Get().RegistryRebuilt -= OnRegistryRebuilt;

            if (m_RegistryQuery.TryGetSingleton<UnmanagedStatusRegistry>(out var registry))
            {
                m_RegistryQuery.CompleteDependency();
                Dispose(registry);
            }
        }

        private void OnRegistryRebuilt()
        {
            EntityManager.CreateEntity(typeof(UnmanagedStatusRegistrySetupRequest));
        }

        private static void Dispose(in UnmanagedStatusRegistry registry)
        {
            if (registry.IdToStatusEffectData.IsCreated)
                registry.IdToStatusEffectData.Dispose();

            if (registry.KeyToId.IsCreated)
                registry.KeyToId.Dispose();

            if (registry.DynamicEffectTypes.IsCreated)
                registry.DynamicEffectTypes.Dispose();

            if (registry.ModuleTypes.IsCreated)
                registry.ModuleTypes.Dispose();
        }

        private static BlobAssetReference<BlobArray<TypeIndex>> CreateTypeArray(HashSet<TypeIndex> types)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            try
            {
                ref var root = ref builder.ConstructRoot<BlobArray<TypeIndex>>();
                var array = builder.Allocate(ref root, types.Count);

                int i = 0;
                foreach (var type in types)
                    array[i++] = type;

                return builder.CreateBlobAssetReference<BlobArray<TypeIndex>>(Allocator.Persistent);
            }
            finally
            {
                builder.Dispose();
            }
        }
    }
}
#endif