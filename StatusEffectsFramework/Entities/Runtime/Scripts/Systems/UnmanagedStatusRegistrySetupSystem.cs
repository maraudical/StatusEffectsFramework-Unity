#if ENTITIES
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    public partial class StatusReferencesSetupSystem : SystemBase
    {
        public EntityQuery m_RegistryQuery;
        public EntityQuery m_RequestQuery;
        private ushort m_Version;
        
        protected override void OnCreate()
        {
            m_RegistryQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistry>().Build();
            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistrySetupRequest>().Build();

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

            var registryEntity = commandBuffer.CreateEntity();
            commandBuffer.SetName(registryEntity, "Status Registry");

            var keyToIdBuilder = new BlobBuilder(Allocator.Temp);
            ref var keyToIdRoot = ref keyToIdBuilder.ConstructRoot<BlobHashMap<Hash128, ushort>>();
            var keyToIdMap = keyToIdBuilder.AllocateHashMap(ref keyToIdRoot, keyToIds.Count);

            foreach (var kvp in keyToIds)
                keyToIdMap.Add(kvp.Key, kvp.Value);

            var keyToIdReference = keyToIdBuilder.CreateBlobAssetReference<BlobHashMap<Hash128, ushort>>(Allocator.Persistent);

            var idToStatusEffectDataBuilder = new BlobBuilder(Allocator.Temp);
            ref var idToStatusEffectDataRoot = ref idToStatusEffectDataBuilder.ConstructRoot<BlobHashMap<ushort, UnmanagedStatusEffectData>>();
            var idToStatusEffectDataMap = idToStatusEffectDataBuilder.AllocateHashMap(ref idToStatusEffectDataRoot, idToStatusEffectDatas.Count);

            // Dispose of old blobs after copying
            if (SystemAPI.TryGetSingletonEntity<UnmanagedStatusRegistry>(out var oldRegistryEntity))
            {
                Cleanup();
                m_Version++;
                commandBuffer.DestroyEntity(oldRegistryEntity);
            }

            // Setup status effect datas
            foreach (var kvp in idToStatusEffectDatas)
            {
                var statusEffectData = kvp.Value;

                // Case where data is null. This should never happen.
                if (!statusEffectData)
                    continue;
                
                ref UnmanagedStatusEffectData unmanagedStatusEffectData = ref idToStatusEffectDataMap.AddByRef(kvp.Key);
                unmanagedStatusEffectData.InternalId = kvp.Key;
                unmanagedStatusEffectData.InternalGroup = statusEffectData.Group;
                ushort comparableName = default;
                if (statusEffectData.ComparableName && !keyToIds.TryGetValue(statusEffectData.ComparableName.GetUniqueKeyHash(), out comparableName))
                    DebugError(statusEffectData.ComparableName.UniqueKey);
                unmanagedStatusEffectData.InternalComparableName = comparableName;
                unmanagedStatusEffectData.InternalBaseValue = statusEffectData.BaseValue;
                unmanagedStatusEffectData.InternalIcon = statusEffectData.Icon;
                UnityEngine.Color color = statusEffectData.Color;
                unmanagedStatusEffectData.InternalColor = new(color.r, color.g, color.b, color.a);
#if LOCALIZED
                idToStatusEffectDataBuilder.AllocateString(ref statusEffectDataRoot.InternalStatusEffectNameTable, statusEffectData.StatusEffectName.TableReference.ToString());
                idToStatusEffectDataBuilder.AllocateString(ref statusEffectDataRoot.InternalStatusEffectNameEntry, statusEffectData.StatusEffectName.TableEntryReference.ToString());
                idToStatusEffectDataBuilder.AllocateString(ref statusEffectDataRoot.InternalAcronymTable, statusEffectData.Acronym.TableReference.ToString());
                idToStatusEffectDataBuilder.AllocateString(ref statusEffectDataRoot.InternalAcronymEntry, statusEffectData.Acronym.TableReference.ToString());
                idToStatusEffectDataBuilder.AllocateString(ref statusEffectDataRoot.InternalDescriptionTable, statusEffectData.Description.TableReference.ToString());
                idToStatusEffectDataBuilder.AllocateString(ref statusEffectDataRoot.InternalDescriptionEntry, statusEffectData.Description.TableReference.ToString());
#else
                idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.InternalStatusEffectName, statusEffectData.StatusEffectName);
                idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.InternalAcronym, statusEffectData.Acronym);
                idToStatusEffectDataBuilder.AllocateString(ref unmanagedStatusEffectData.InternalDescription, statusEffectData.Description);
#endif
                unmanagedStatusEffectData.InternalAllowEffectStacking = statusEffectData.AllowEffectStacking;
                unmanagedStatusEffectData.InternalNonStackingBehaviour = statusEffectData.NonStackingBehaviour;
                unmanagedStatusEffectData.InternalMaxStacks = statusEffectData.MaxStacks;
                var effects = idToStatusEffectDataBuilder.Allocate(ref unmanagedStatusEffectData.InternalEffects, statusEffectData.Effects.Count);
                for (int i = 0; i < effects.Length; i++)
                {
                    var effect = statusEffectData.Effects[i];
                    ValueType valueType = default;
                    DynamicEffectInfo info = default;
                    bool postEvaluate = default;

                    if (effect.StatusName == null)
                        continue;

                    switch (effect.StatusName)
                    {
                        case StatusNameFloat:
                            if (effect.ValueSource is ValueSource.DynamicValue)
                                if (effect.DynamicFloatEffect && effect.DynamicFloatEffect is IEntityDynamicEffect entityDynamicEffect)
                                {
                                    info = entityDynamicEffect.CreateDynamicEffectInfo();
                                    postEvaluate = effect.DynamicFloatEffect.PostEvaluate;
                                }  
                                else
                                    continue;
                            valueType = ValueType.Float;
                            break;
                        case StatusNameInt:
                            if (effect.ValueSource is ValueSource.DynamicValue)
                                if (effect.DynamicIntEffect && effect.DynamicIntEffect is IEntityDynamicEffect entityDynamicEffect)
                                {
                                    info = entityDynamicEffect.CreateDynamicEffectInfo();
                                    postEvaluate = effect.DynamicIntEffect.PostEvaluate;
                                }
                                else
                                    continue;
                            valueType = ValueType.Int;
                            break;
                        case StatusNameBool:
                            if (effect.ValueSource is ValueSource.DynamicValue)
                                if (effect.DynamicBoolEffect && effect.DynamicBoolEffect is IEntityDynamicEffect entityDynamicEffect)
                                {
                                    info = entityDynamicEffect.CreateDynamicEffectInfo();
                                    postEvaluate = effect.DynamicBoolEffect.PostEvaluate;
                                }
                                else
                                    continue;
                            valueType = ValueType.Bool;
                            break;
                    }

                    ref var unmanagedEffect = ref effects[i];
                    ushort statusName = default;
                    if (effect.StatusName && !keyToIds.TryGetValue(effect.StatusName.GetUniqueKeyHash(), out statusName))
                        DebugError(effect.StatusName.UniqueKey);
                    unmanagedEffect = new UnmanagedEffect
                    {
                        Id = statusName,
                        ValueType = valueType,
                        ValueModifier = effect.ValueModifier,
                        ValueSource = effect.ValueSource,
                        PostEvaluate = postEvaluate,
                        Priority = effect.Priority,
                        FloatValue = effect.FloatValue,
                        IntValue = effect.IntValue,
                        BoolValue = effect.BoolValue,
                        DynamicEffectInfo = info
                    };
                }
                var conditions = idToStatusEffectDataBuilder.Allocate(ref unmanagedStatusEffectData.InternalConditions, statusEffectData.Conditions.Count);
                for (int i = 0; i < conditions.Length; i++)
                {
                    var condition = statusEffectData.Conditions[i];

                    ushort searchableData = default;
                    if (condition.SearchableData && !keyToIds.TryGetValue(condition.SearchableData.GetUniqueKeyHash(), out searchableData))
                        DebugError(condition.SearchableData.UniqueKey);
                    ushort searchableComparableName = default;
                    if (condition.SearchableComparableName && !keyToIds.TryGetValue(condition.SearchableComparableName.GetUniqueKeyHash(), out searchableComparableName))
                        DebugError(condition.SearchableComparableName.UniqueKey);
                    ushort actionData = default;
                    if (condition.ActionData && !keyToIds.TryGetValue(condition.ActionData.GetUniqueKeyHash(), out actionData))
                        DebugError(condition.ActionData.UniqueKey);
                    ushort actionComparableName = default;
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
                List<ModuleContainer> entityModuleContainers = statusEffectData.Modules.Where((m) => m.Module is IEntityModule).OrderBy(m => m.Module.GetType().AssemblyQualifiedName).ToList();
                var modules = idToStatusEffectDataBuilder.Allocate(ref unmanagedStatusEffectData.InternalModules, entityModuleContainers.Count);

                if (entityModuleContainers.Count > 0)
                {
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var moduleContainer = entityModuleContainers[i];
                        var entityModule = (IEntityModule)moduleContainer.Module;

                        modules[i] = entityModule.CreateModuleInfo(moduleContainer.ModuleInstance);
                    }
                }

                void DebugError(string uniqueKey) => UnityEngine.Debug.LogError($"Trying to setup an {nameof(UnmanagedStatusEffectData)} with the {nameof(Registrant)} that contains the unique key \"{uniqueKey}\" but the registry doesn't contain the ID associated with it.");
            }

            var idToStatusEffectDataReference = idToStatusEffectDataBuilder.CreateBlobAssetReference<BlobHashMap<ushort, UnmanagedStatusEffectData>>(Allocator.Persistent);

            Type moduleType = typeof(Modules<int>);
            Type dynamicFloatType = typeof(DynamicFloats<int>);
            Type dynamicIntType = typeof(DynamicInts<int>);
            Type dynamicBoolType = typeof(DynamicBools<int>);
            
            commandBuffer.AddComponent(registryEntity, new UnmanagedStatusRegistry
            {
                Version = m_Version,
                IdToStatusEffectData = idToStatusEffectDataReference,
                KeyToId = keyToIdReference,
                ModuleOffsets = new ModuleOffsets
                {
                    Struct = UnsafeUtility.GetFieldOffset(moduleType.GetField(nameof(ModuleOffsets.Struct)))
                },
                DynamicFloatOffsets = new DynamicFloatOffsets
                {
                    Id = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Id))),
                    ValueModifier = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.ValueModifier))),
                    PostEvaluate = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.PostEvaluate))),
                    Priority = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Priority))),
                    Value = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Value))),
                    Struct = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Struct)))
                },
                DynamicIntOffsets = new DynamicIntOffsets
                {
                    Id = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Id))),
                    ValueModifier = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.ValueModifier))),
                    PostEvaluate = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.PostEvaluate))),
                    Priority = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Priority))),
                    Value = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Value))),
                    Struct = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Struct)))
                },
                DynamicBoolOffsets = new DynamicBoolOffsets
                {
                    Id = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Id))),
                    PostEvaluate = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.PostEvaluate))),
                    Priority = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Priority))),
                    Value = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Value))),
                    Struct = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Struct)))
                },
            });

            keyToIdBuilder.Dispose();
            idToStatusEffectDataBuilder.Dispose();
        }

        protected override void OnDestroy()
        {
            StatusRegistry.Get().RegistryRebuilt += OnRegistryRebuilt;
            Cleanup();
        }

        private void OnRegistryRebuilt()
        {
            EntityManager.CreateEntity(typeof(UnmanagedStatusRegistrySetupRequest));
        }

        private void Cleanup()
        {
            if (SystemAPI.TryGetSingleton<UnmanagedStatusRegistry>(out var references))
            {
                using var datas = references.IdToStatusEffectData.Value.GetValueArray(Allocator.Temp);

                foreach (var data in datas)
                {
                    for (int i = 0; i < data.Modules.Length; i++)
                        unsafe
                        {
                            var modulePtr = data.Modules[i];
                            UnsafeUtility.Free(modulePtr.Ptr.ToPointer(), Allocator.Persistent);
                        }
                    for (int i = 0; i < data.Effects.Length; i++)
                        unsafe
                        {
                            ref var dynamicEffectInfo = ref data.Effects[i].DynamicEffectInfo;
                            if (dynamicEffectInfo.Ptr != IntPtr.Zero)
                                UnsafeUtility.Free(dynamicEffectInfo.Ptr.ToPointer(), Allocator.Persistent);
                        }
                }

                if (references.IdToStatusEffectData.IsCreated)
                    references.IdToStatusEffectData.Dispose();
            }
        }
    }
}
#endif