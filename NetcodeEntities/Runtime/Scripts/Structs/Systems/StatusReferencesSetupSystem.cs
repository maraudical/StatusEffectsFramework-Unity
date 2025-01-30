#if ENTITIES && ADDRESSABLES
using StatusEffects.Modules;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.AddressableAssets;

namespace StatusEffects.NetCode.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [CreateBefore(typeof(StatusManagerSystem))]
    public partial class StatusReferencesSetupSystem : SystemBase
    {
        protected override void OnCreate()
        {
            var statusEffectDataHandle = Addressables.LoadAssetsAsync<global::StatusEffects.StatusEffectData>(StatusEffectAddressableLabels.StatusEffectData);
            statusEffectDataHandle.WaitForCompletion();

            IList<global::StatusEffects.StatusEffectData> statusEffectDatas = statusEffectDataHandle.Result;
            
            var commandBuffer = new EntityCommandBuffer(Allocator.Temp);
            var references = commandBuffer.CreateEntity();
            commandBuffer.SetName(references, "Status References");

            BlobAssetReference<BlobHashMap<FixedString64Bytes, BlobAssetReference<StatusEffectData>>> statusEffectDataHashMapReferences;
            // Setup status effect datas
            var builder = new BlobBuilder(Allocator.Temp);
            ref var statusEffectDataHashMapRoot = ref builder.ConstructRoot<BlobHashMap<FixedString64Bytes, BlobAssetReference<StatusEffectData>>>();
            var statusEffectDataHashMap = builder.AllocateHashMap(ref statusEffectDataHashMapRoot, statusEffectDatas.Count);
            global::StatusEffects.Effect effect;
            global::StatusEffects.Condition condition;
            commandBuffer.AddBuffer<ModulePrefabs>(references);
            int moduleIndex = 0;

            foreach (var statusEffectData in statusEffectDatas)
            {
                var subBuilder = new BlobBuilder(Allocator.Temp);

                ref StatusEffectData statusEffectDataRoot = ref subBuilder.ConstructRoot<StatusEffectData>();
                statusEffectDataRoot.Id = statusEffectData.Id;
                statusEffectDataRoot.Group = statusEffectData.Group;
                statusEffectDataRoot.ComparableName = statusEffectData.ComparableName.Id;
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
                        Id = effect.StatusName.Id,
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
                        SearchableData = condition.SearchableData?.Id ?? default(FixedString64Bytes),
                        SearchableComparableName = condition.SearchableComparableName?.Id ?? default(FixedString64Bytes),
                        SearchableGroup = condition.SearchableGroup,
                        Exists = condition.Exists,
                        Add = condition.Add,
                        Scaled = condition.Scaled,
                        UseStacks = condition.UseStacks,
                        Stacks = condition.Stacks,
                        ActionConfigurable = condition.ActionConfigurable,
                        ActionData = condition.ActionData?.Id ?? default(FixedString64Bytes),
                        ActionComparableName = condition.ActionComparableName?.Id ?? default(FixedString64Bytes),
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
                    foreach (var entityModule in entityModules)
                        // Call each modify command buffer on each module so that it adds
                        // whatever component/buffers it wants. This way, a custom system
                        // can be made to act on those components/buffers when they get
                        // instantiated as children of the entity they effect.
                        (entityModule.Module as IEntityModule).ModifyCommandBuffer(ref commandBuffer, moduleEntity, entityModule.ModuleInstance);

                    commandBuffer.AppendToBuffer(references, new ModulePrefabs() { Entity = moduleEntity });
                    statusEffectDataRoot.Modules = moduleIndex;
                    moduleIndex++;
                }
                else
                {
                    // There weren't any modules
                    statusEffectDataRoot.Modules = -1;
                }

                statusEffectDataHashMap.Add(statusEffectData.Id, subBuilder.CreateBlobAssetReference<StatusEffectData>(Allocator.Persistent));
                subBuilder.Dispose();
            }

            statusEffectDataHashMapReferences = builder.CreateBlobAssetReference<BlobHashMap<FixedString64Bytes, BlobAssetReference<StatusEffectData>>>(Allocator.Persistent);
            builder.Dispose();
            
            commandBuffer.AddComponent(references, new StatusReferences
            {
                StatusEffectDatas = statusEffectDataHashMapReferences,
            });

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();

            statusEffectDataHandle.Release();

            Enabled = false;
        }

        protected override void OnUpdate()
        {
            return;
        }

        protected override void OnDestroy()
        {
            var references = SystemAPI.GetSingleton<StatusReferences>();

            var statusEffectDatas = references.StatusEffectDatas.Value.GetValueArray(Allocator.Temp);
            foreach (var reference in statusEffectDatas)
                reference.Dispose();
            references.StatusEffectDatas.Dispose();
        }
    }
}
#endif