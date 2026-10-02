using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [UpdateInGroup(typeof(StatusEffectInitializationSystemGroup), OrderFirst = true)]
    public partial class UnmanagedStatusRegistrySetupSystem : SystemBase
    {
        public const string RegistryName = "StatusRegistry";
        private EntityQuery m_RegistryQuery;
        private EntityQuery m_RequestQuery;
        private EntityQuery m_ResolverQuery;
        private ushort m_Version;
        private StatusRegistry m_StatusRegistry;

        protected override void OnCreate()
        {
            m_RegistryQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistry>().Build();
            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistrySetupRequest>().Build();
            m_ResolverQuery = SystemAPI.QueryBuilder().WithAllRW<StatusFloats>().WithAllRW<StatusInts>().WithAllRW<StatusBools>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState).Build();

            m_Version = 1;

            // In builds the registry is only loaded from Resources, so it can be missing. Without it there is
            // nothing to build, so disable the system. Systems that require the unmanaged registry won't run either.
            m_StatusRegistry = StatusRegistry.Get();
            if (!m_StatusRegistry)
            {
                UnityEngine.Debug.LogError($"{nameof(UnmanagedStatusRegistrySetupSystem)} could not load the {nameof(StatusRegistry)}. The {nameof(UnmanagedStatusRegistry)} will not be created and status effects will not run on entities.");
                Enabled = false;
                return;
            }

            m_StatusRegistry.RegistryRebuilt += OnRegistryRebuilt;
            OnRegistryRebuilt();

            RequireForUpdate(m_RequestQuery);
        }

        protected override void OnUpdate()
        {
            var commandBuffer = SystemAPI.GetSingleton<EndInitializationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            
            commandBuffer.DestroyEntity(m_RequestQuery, EntityQueryCaptureMode.AtPlayback);

            var idToStatusEffectDatas = m_StatusRegistry.IdToStatusEffectData;
            var keyToIds = m_StatusRegistry.KeyToId;

            // All registry data is built into a single blob so it is created and disposed as one unit.
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<UnmanagedStatusRegistryData>();

            var keyToIdMap = builder.AllocateHashMap(ref root.KeyToId, keyToIds.Count);

            foreach (var kvp in keyToIds)
                keyToIdMap.Add(kvp.Key, kvp.Value);

            var idToStatusEffectDataMap = builder.AllocateHashMap(ref root.IdToStatusEffectData, idToStatusEffectDatas.Count);

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
                    builder.AllocateString(ref unmanagedStatusEffectData.StatusEffectNameTable, statusEffectData.StatusEffectName.TableReference.ToString());
                    builder.AllocateString(ref unmanagedStatusEffectData.StatusEffectNameEntry, statusEffectData.StatusEffectName.TableEntryReference.ToString());
                    builder.AllocateString(ref unmanagedStatusEffectData.AcronymTable, statusEffectData.Acronym.TableReference.ToString());
                    builder.AllocateString(ref unmanagedStatusEffectData.AcronymEntry, statusEffectData.Acronym.TableEntryReference.ToString());
                    builder.AllocateString(ref unmanagedStatusEffectData.DescriptionTable, statusEffectData.Description.TableReference.ToString());
                    builder.AllocateString(ref unmanagedStatusEffectData.DescriptionEntry, statusEffectData.Description.TableEntryReference.ToString());
#else
                    builder.AllocateString(ref unmanagedStatusEffectData.StatusEffectName, statusEffectData.StatusEffectName);
                    builder.AllocateString(ref unmanagedStatusEffectData.Acronym, statusEffectData.Acronym);
                    builder.AllocateString(ref unmanagedStatusEffectData.Description, statusEffectData.Description);
#endif
                    unmanagedStatusEffectData.AllowEffectStacking = statusEffectData.AllowEffectStacking;
                    unmanagedStatusEffectData.NonStackingBehaviour = statusEffectData.NonStackingBehaviour;
                    unmanagedStatusEffectData.MaxStacks = statusEffectData.MaxStacks;

                    // Validate every effect up front so the blob array only contains fully populated entries.
                    var entityEffects = new List<(Effect Effect, ushort Id, ValueType ValueType, DynamicEffect DynamicEffect)>(statusEffectData.Effects.Count);
                    foreach (var effect in statusEffectData.Effects)
                        if (TryResolveEffect(effect, keyToIds, out var resolvedEffect))
                            entityEffects.Add(resolvedEffect);

                    var effects = builder.Allocate(ref unmanagedStatusEffectData.Effects, entityEffects.Count);

                    for (int i = 0; i < effects.Length; i++)
                    {
                        var (effect, statusName, valueType, dynamicEffect) = entityEffects[i];

                        ref var unmanagedEffect = ref effects[i];

                        unmanagedEffect = new UnmanagedEffect
                        {
                            Id = statusName,
                            ValueType = valueType,
                            ValueModifier = effect.ValueModifier,
                            ValueSource = effect.ValueSource,
                            Priority = effect.Priority,
                            FloatValue = effect.FloatValue,
                            IntValue = effect.IntValue,
                            BoolValue = effect.BoolValue,
                        };

                        if (dynamicEffect)
                        {
                            ((IEntityDynamicEffect)dynamicEffect).CreateDynamicEffectInfo(valueType, ref unmanagedEffect.DynamicEffectInfo, ref builder);
                            unmanagedEffect.PostEvaluate = dynamicEffect.PostEvaluate;

                            if (unmanagedEffect.DynamicEffectInfo.TypeIndex != TypeIndex.Null)
                                dynamicEffectTypes.Add(unmanagedEffect.DynamicEffectInfo.TypeIndex);
                        }
                    }

                    var conditions = builder.Allocate(ref unmanagedStatusEffectData.Conditions, statusEffectData.Conditions.Count);

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

                    List<ModuleContainer> entityModuleContainers = statusEffectData.Modules.Where((m) => m.Module is IEntityModule).ToList();
                    var modules = builder.Allocate(ref unmanagedStatusEffectData.Modules, entityModuleContainers.Count);

                    for (int i = 0; i < modules.Length; i++)
                    {
                        var moduleContainer = entityModuleContainers[i];
                        var entityModule = (IEntityModule)moduleContainer.Module;

                        entityModule.CreateModuleInfo(moduleContainer.ModuleInstance, ref modules[i], ref builder);

                        if (modules[i].TypeIndex != TypeIndex.Null)
                            moduleTypes.Add(modules[i].TypeIndex);
                    }
                }

                // The type sets are only complete once every status effect data has been built.
                AllocateSortedTypeArray(ref builder, ref root.DynamicEffectTypes, dynamicEffectTypes);
                AllocateSortedTypeArray(ref builder, ref root.ModuleTypes, moduleTypes);

                var unmanagedRegistry = new UnmanagedStatusRegistry
                {
                    Data = builder.CreateBlobAssetReference<UnmanagedStatusRegistryData>(Allocator.Persistent)
                };

                bool hasOldRegistry = !m_RegistryQuery.IsEmptyIgnoreFilter;
                if (hasOldRegistry)
                    m_Version++;

                unmanagedRegistry.Version = m_Version;

                if (hasOldRegistry)
                {
                    // Swap the singleton in place so nothing can read the old blob after this point,
                    // then dispose it once every job that could still be reading them has completed.
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
                builder.Dispose();
            }
        }

        protected override void OnDestroy()
        {
            if (m_StatusRegistry)
                m_StatusRegistry.RegistryRebuilt -= OnRegistryRebuilt;

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
            if (registry.Data.IsCreated)
                registry.Data.Dispose();
        }

        /// <summary>
        /// Resolves an effect's status name ID, value type and dynamic effect. Returns false if the effect
        /// cannot be represented on an entity, in which case it is excluded rather than left as a default entry.
        /// </summary>
        private static bool TryResolveEffect(Effect effect, IReadOnlyDictionary<Hash128, ushort> keyToIds, out (Effect Effect, ushort Id, ValueType ValueType, DynamicEffect DynamicEffect) resolvedEffect)
        {
            resolvedEffect = default;

            if (!effect.StatusName)
                return false;

            ValueType valueType;
            DynamicEffect dynamicEffect;
            switch (effect.StatusName)
            {
                case StatusNameFloat:
                    valueType = ValueType.Float;
                    dynamicEffect = effect.DynamicFloatEffect;
                    break;
                case StatusNameInt:
                    valueType = ValueType.Int;
                    dynamicEffect = effect.DynamicIntEffect;
                    break;
                case StatusNameBool:
                    valueType = ValueType.Bool;
                    dynamicEffect = effect.DynamicBoolEffect;
                    break;
                default:
                    return false;
            }

            if (effect.ValueSource is ValueSource.DynamicValue)
            {
                // Missing or non-entity dynamic effects are skipped. The Unity null check also catches destroyed assets.
                if (!dynamicEffect || dynamicEffect is not IEntityDynamicEffect)
                    return false;
            }
            else
                dynamicEffect = null;

            if (!keyToIds.TryGetValue(effect.StatusName.GetUniqueKeyHash(), out var id))
            {
                DebugError(effect.StatusName.UniqueKey);
                return false;
            }

            resolvedEffect = (effect, id, valueType, dynamicEffect);
            return true;
        }

        private static void DebugError(string uniqueKey) => UnityEngine.Debug.LogError($"Trying to setup an {nameof(UnmanagedStatusEffectData)} with the {nameof(Registrant)} that contains the unique key \"{uniqueKey}\" but the registry doesn't contain the ID associated with it.");

        /// <summary>
        /// Writes the types sorted so the blob contents don't depend on <see cref="HashSet{T}"/> iteration order.
        /// </summary>
        private static void AllocateSortedTypeArray(ref BlobBuilder builder, ref BlobArray<TypeIndex> blobArray, HashSet<TypeIndex> types)
        {
            var sortedTypes = new List<TypeIndex>(types);
            sortedTypes.Sort();

            var array = builder.Allocate(ref blobArray, sortedTypes.Count);
            for (int i = 0; i < sortedTypes.Count; i++)
                array[i] = sortedTypes[i];
        }
    }
}