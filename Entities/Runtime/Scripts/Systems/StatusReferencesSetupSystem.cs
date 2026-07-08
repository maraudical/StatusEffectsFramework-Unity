#if ENTITIES
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
            ref var idToStatusEffectDataMapRoot = ref idToStatusEffectDataMapBuilder.ConstructRoot<BlobHashMap<Hash128, BlobAssetReference<UnmanagedStatusEffectData>>>();
            var idToStatusEffectDataMap = idToStatusEffectDataMapBuilder.AllocateHashMap(ref idToStatusEffectDataMapRoot, statusEffectDatas.Count);
            Effect effect;
            Condition condition;
            
            // Dispose of old blobs after copying
            if (SystemAPI.TryGetSingletonEntity<StatusReferences>(out var oldReferencesEntity))
            {
                OnDestroy();
                commandBuffer.DestroyEntity(oldReferencesEntity);
            }

            // Setup status effect datas
            foreach (var statusEffectData in statusEffectDatas)
            {
                // Rare case where data is null. This should never happen.
                if (!statusEffectData || idToStatusEffectDataMap.ContainsKey(statusEffectData.Id))
                    continue;

                var subBuilder = new BlobBuilder(Allocator.Temp);

                ref UnmanagedStatusEffectData statusEffectDataRoot = ref subBuilder.ConstructRoot<UnmanagedStatusEffectData>();
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
                    unmanagedEffect = new UnmanagedEffect
                    {
                        StatusName = effect.StatusName ? effect.StatusName.Id : default,
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
                var conditions = subBuilder.Allocate(ref statusEffectDataRoot.Conditions, statusEffectData.Conditions.Count);
                for (int i = 0; i < conditions.Length; i++)
                {
                    condition = statusEffectData.Conditions[i];
                    conditions[i] = new UnmanagedCondition()
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
                List<ModuleContainer> entityModuleContainers = statusEffectData.Modules.Where((m) => m.Module is IEntityModule).OrderBy(m => m.Module.GetType().AssemblyQualifiedName).ToList();
                var modules = subBuilder.Allocate(ref statusEffectDataRoot.Modules, entityModuleContainers.Count);

                if (entityModuleContainers.Count > 0)
                {
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var moduleContainer = entityModuleContainers[i];
                        var entityModule = (IEntityModule)moduleContainer.Module;

                        modules[i] = entityModule.CreateModuleInfo(moduleContainer.ModuleInstance);
                    }
                }

                idToStatusEffectDataMap.Add(statusEffectData.Id, subBuilder.CreateBlobAssetReference<UnmanagedStatusEffectData>(Allocator.Persistent));
                subBuilder.Dispose();
            }

            var statusEffectDataMapBlob = idToStatusEffectDataMapBuilder.CreateBlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<UnmanagedStatusEffectData>>>(Allocator.Persistent);
            idToStatusEffectDataMapBuilder.Dispose();

            Type moduleType = typeof(Modules<int>);
            Type dynamicFloatType = typeof(DynamicFloats<int>);
            Type dynamicIntType = typeof(DynamicInts<int>);
            Type dynamicBoolType = typeof(DynamicBools<int>);
            
            commandBuffer.AddComponent(referencesEntity, new StatusReferences
            {
                ModuleOffsets = new ModuleOffsets
                {
                    Struct = UnsafeUtility.GetFieldOffset(moduleType.GetField(nameof(ModuleOffsets.Struct)))
                },
                DynamicFloatOffsets = new DynamicFloatOffsets
                {
                    StatusName = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.StatusName))),
                    ValueModifier = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.ValueModifier))),
                    PostEvaluate = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.PostEvaluate))),
                    Priority = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Priority))),
                    Value = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Value))),
                    Struct = UnsafeUtility.GetFieldOffset(dynamicFloatType.GetField(nameof(DynamicFloatOffsets.Struct)))
                },
                DynamicIntOffsets = new DynamicIntOffsets
                {
                    StatusName = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.StatusName))),
                    ValueModifier = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.ValueModifier))),
                    PostEvaluate = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.PostEvaluate))),
                    Priority = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Priority))),
                    Value = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Value))),
                    Struct = UnsafeUtility.GetFieldOffset(dynamicIntType.GetField(nameof(DynamicIntOffsets.Struct)))
                },
                DynamicBoolOffsets = new DynamicBoolOffsets
                {
                    StatusName = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.StatusName))),
                    PostEvaluate = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.PostEvaluate))),
                    Priority = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Priority))),
                    Value = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Value))),
                    Struct = UnsafeUtility.GetFieldOffset(dynamicBoolType.GetField(nameof(DynamicBoolOffsets.Struct)))
                },
                IdToStatusEffectDataMap = statusEffectDataMapBlob,
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
                    for (int i = 0; i < statusEffectData.Effects.Length; i++)
                        unsafe
                        {
                            ref var dynamicEffectInfo = ref statusEffectData.Effects[i].DynamicEffectInfo;
                            if (dynamicEffectInfo.Ptr != IntPtr.Zero)
                                UnsafeUtility.Free(dynamicEffectInfo.Ptr.ToPointer(), Allocator.Persistent);
                        }
                    blob.Dispose();
                }

                if (references.IdToStatusEffectDataMap.IsCreated)
                    references.IdToStatusEffectDataMap.Dispose();
            }
        }
    }
}
#endif