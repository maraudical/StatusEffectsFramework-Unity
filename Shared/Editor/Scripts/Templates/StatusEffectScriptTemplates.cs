//#EXCLUDEFROMPROCESSING#
#if UNITY_2023_1_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StatusEffects.Templates
{
    public static class StatusEffectScriptTemplates
    {
        [MenuItem("Assets/Create/Status Effect Framework/Module Script", secondaryPriority = -2)]
        static void MenuCreateModuleScript()
        {
            Texture2D icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptTemplateAssetsAction>(), "NewModuleScript.cs", icon,
#if ENTITIES
                EntityModuleScriptContent
#elif UNITASK
                UniTaskModuleScriptContent
#else
                ModuleScriptContent
#endif
                );
        }

        [MenuItem("Assets/Create/Status Effect Framework/Module Instance Script", secondaryPriority = -1)]
        static void MenuCreateModuleInstanceScript()
        {
            Texture2D icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, ScriptableObject.CreateInstance<CreateScriptTemplateAssetsAction>(), "NewModuleInstanceScript.cs", icon, ModuleInstanceScriptContent);
        }

        internal class CreateScriptTemplateAssetsAction : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string userPath, string resourceFile)
            {
                string directoryPath = Path.GetDirectoryName(userPath);
                string enteredName = Path.GetFileNameWithoutExtension(userPath);
                string cleanedEnteredNamed = enteredName.Replace(" ", "");

                try
                {
                    AssetDatabase.StartAssetEditing();

                    Object o = CreateScriptAssetFromContent(resourceFile, Path.Combine(directoryPath, cleanedEnteredNamed + ".cs"), enteredName, cleanedEnteredNamed);
                    ProjectWindowUtil.ShowCreatedAsset(o);
                }
                finally
                {
                    AssetDatabase.StopAssetEditing();
                }
            }
        }

        internal static Object CreateScriptAssetFromContent(string content, string targetPath, string displayName, string scriptName)
        {
            return ProjectWindowUtil.CreateScriptAssetWithContent(targetPath, PreprocessScriptTemplate(content, displayName, scriptName));
        }

        static string PreprocessScriptTemplate(string content, string displayName, string scriptName)
        {
            content = content.Replace("#SCRIPTNAME#", scriptName);
            content = content.Replace("#DISPLAYNAME#", displayName);

            return content;
        }

        internal const string UniTaskModuleScriptContent =
@"using StatusEffects
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effect Framework/Modules/#DISPLAYNAME#"", order = 1)]
//[AttachModuleInstance(typeof(#DISPLAYNAME#Instance))]
public class #SCRIPTNAME# : Module
{
    public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
    {
        await UniTask.CompletedTask;
    }
}";

        internal const string ModuleScriptContent =
@"using StatusEffects
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effect Framework/Modules/#DISPLAYNAME#"", order = 1)]
//[AttachModuleInstance(typeof(#DISPLAYNAME#Instance))]
public class #SCRIPTNAME# : Module
{
    public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
    {
        await Task.CompletedTask; return;
    }
}";

        internal const string EntityModuleScriptContent =
@"using StatusEffects;
using StatusEffects.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Modules<#SCRIPTNAME#Struct>))]

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effect Framework/Modules/#DISPLAYNAME#"", order = 1)]
//[AttachModuleInstance(typeof(#SCRIPTNAME#Instance))]
public class #SCRIPTNAME# : Module, IEntityModule
{
    public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
    {
        //var instance = moduleInstance as #SCRIPTNAME#Instance;
        var moduleStruct = new #SCRIPTNAME#Struct
        {
            // Add in struct specific values and/or copy them from the module instance.
        };
        return (this as IEntityModule).AllocateModule(moduleStruct);
    }
}

public struct #SCRIPTNAME#Struct
{
            
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct #SCRIPTNAME#System : ISystem
{
    private EntityQuery m_EventQuery;

    private TypeIndex m_TypeIndex;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_EventQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects, StatusEffectEvents>().WithAll<Simulate>().Build();

        m_TypeIndex = TypeManager.GetTypeIndex<Modules<#SCRIPTNAME#Struct>>();

        state.RequireForUpdate<StatusReferences>();
        state.RequireForUpdate(m_EventQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
        var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        var lookup = SystemAPI.GetBufferLookup<Modules<#SCRIPTNAME#Struct>>();

        var eventJob = new #SCRIPTNAME#EventJob
        {
            TypeIndex = m_TypeIndex,
            References = statusReferences,
            CommandBuffer = commandBuffer,
            Lookup = lookup,
        };
        state.Dependency = eventJob.ScheduleParallelByRef(m_EventQuery, state.Dependency);
    }

    [BurstCompile]
    partial struct #SCRIPTNAME#EventJob : IJobEntity
    {
        public TypeIndex TypeIndex;
        public StatusReferences References;
        public EntityCommandBuffer.ParallelWriter CommandBuffer;
        [NativeDisableParallelForRestriction]
        public BufferLookup<Modules<#SCRIPTNAME#Struct>> Lookup;

        public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<ActiveStatusEffects> statusEffects, in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
        {
            bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);

            foreach (var statusEffectEvent in statusEffectEvents)
            {
                if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                    continue;

                ref var data = ref reference.Value;

                if (!StatusEffectsUtility.ModuleInfosContainType(ref data.Modules, TypeIndex))
                    continue;

                switch (statusEffectEvent.Event)
                {
                    case StatusEffectEvent.Added:
                        ref var modules = ref data.Modules;
                        for (int i = 0; i < modules.Length; i++)
                        {
                            var moduleInfo = modules[i];

                            if (moduleInfo.TypeIndex != TypeIndex)
                                continue;

                            if (!foundBuffer)
                            {
                                foundBuffer = true;
                                buffer = CommandBuffer.AddBuffer<Modules<#SCRIPTNAME#Struct>>(sortKey, entity);
                            }
                            var module = StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
                            // Put specific logic when added here.
                        }

                        break;
                    case StatusEffectEvent.Removed:
                        if (foundBuffer)
                            StatusEffectsUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                        // Put specific logic when removed here. Do not use the buffer since the modules have already been removed.
                        break;
                        /*case StatusEffectEvent.Updated:
                            foreach (var module in buffer)
                                if (module.Id == statusEffectEvent.Id)
                                {
                                    Put specific logic when updated here.
                                }
                            break;*/
                }
            }

            if (foundBuffer && buffer.Length <= 0)
                CommandBuffer.RemoveComponent<Modules<#SCRIPTNAME#Struct>>(sortKey, entity);
        }
    }
}";

        internal const string ModuleInstanceScriptContent =
@"using StatusEffects

public class #SCRIPTNAME# : ModuleInstance
{
        
}";
    }
}
#endif