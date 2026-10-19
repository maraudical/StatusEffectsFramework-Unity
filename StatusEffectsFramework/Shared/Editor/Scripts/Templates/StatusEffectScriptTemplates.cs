//#EXCLUDEFROMPROCESSING#
#if UNITY_2023_1_OR_NEWER
using System.IO;
using UnityEditor;
using UnityEngine;

namespace StatusEffectsFramework.Templates
{
    public static class StatusEffectScriptTemplates
    {
        [MenuItem("Assets/Create/Status Effects Framework/Module Script", secondaryPriority = -2)]
        static void MenuCreateModuleScript()
        {
            Texture2D icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None, ScriptableObject.CreateInstance<CreateScriptTemplateAssetsAction>(), "NewModuleScript.cs", icon,
#if ENTITIES
                EntityModuleScriptContent
#else
                ModuleScriptContent
#endif
                );
        }

        [MenuItem("Assets/Create/Status Effects Framework/Module Instance Script", secondaryPriority = -1)]
        static void MenuCreateModuleInstanceScript()
        {
            Texture2D icon = EditorGUIUtility.IconContent("cs Script Icon").image as Texture2D;

            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(EntityId.None, ScriptableObject.CreateInstance<CreateScriptTemplateAssetsAction>(), "NewModuleInstanceScript.cs", icon, ModuleInstanceScriptContent);
        }

        internal class CreateScriptTemplateAssetsAction : UnityEditor.ProjectWindowCallback.AssetCreationEndAction
        {
            public override void Action(EntityId entityId, string pathName, string resourceFile)
            {
                string directoryPath = Path.GetDirectoryName(pathName);
                string enteredName = Path.GetFileNameWithoutExtension(pathName);
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

        internal const string ModuleScriptContent =
@"using StatusEffectsFramework;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Modules/#DISPLAYNAME#"", order = 1)]
//[AttachModuleInstance(typeof(#SCRIPTNAME#Instance))]
public class #SCRIPTNAME# : Module
{
    public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
    {
        // Called when the status effect starts.
    }

    public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
    {
        // Called when the status effect ends.
    }
}";

        internal const string EntityModuleScriptContent =
@"using StatusEffectsFramework;
using StatusEffectsFramework.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Modules<#SCRIPTNAME#Struct>))]

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Modules/#DISPLAYNAME#"", order = 1)]
//[AttachModuleInstance(typeof(#SCRIPTNAME#Instance))]
public class #SCRIPTNAME# : Module, IEntityModule
{
    public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
    {
        //var instance = moduleInstance as #SCRIPTNAME#Instance;
        var moduleStruct = new #SCRIPTNAME#Struct
        {
            // Add in struct specific values and/or copy them from the module instance.
        };
        ModuleInfo.AllocateModule(moduleStruct, ref info, ref builder);
    }
}

public struct #SCRIPTNAME#Struct
{

}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct #SCRIPTNAME#System : ISystem
{
    private EntityQuery m_EntityQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents, Modules<#SCRIPTNAME#Struct>>().WithAll<Simulate>().Build();

        state.RequireForUpdate(m_EntityQuery);
        state.RequireForUpdate<UnmanagedStatusRegistry>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new #SCRIPTNAME#Job
        {
            Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>(),
        };
        state.Dependency = job.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
    }

    [BurstCompile]
    partial struct #SCRIPTNAME#Job : IJobEntity
    {
        public UnmanagedStatusRegistry Registry;

        public void Execute(in DynamicBuffer<StatusEffects> statusEffects,
            in DynamicBuffer<StatusEffectEvents> statusEffectEvents,
            ref DynamicBuffer<Modules<#SCRIPTNAME#Struct>> modules)
        {
            // AsNativeArray does not create a copy of the data so any changes will effect the source buffer.
            var modulesArray = modules.AsNativeArray();
            modulesArray.Sort();

            foreach (var statusEffectEvent in statusEffectEvents)
            {
                ref var data = ref Registry.GetStatusEffectDataOrNullRef(statusEffectEvent.Id, out bool exists);
                if (!exists)
                    continue;

                switch (statusEffectEvent.Event)
                {
                    case StatusEffectEvent.Added:
                        // Put specific logic when added here. The modules of this status effect are the
                        // elements with Id equal to statusEffectEvent.InstanceId, found with
                        // modulesArray.BinarySearchFirst(statusEffectEvent.InstanceId).
                        break;
                    case StatusEffectEvent.Updated:
                        // Put specific logic when the stacks change here. The previous stack count
                        // is statusEffectEvent.PreviousStacks.
                        break;
                    case StatusEffectEvent.Removed:
                        // Put specific logic when removed here. The modules have already been removed from the buffer.
                        break;
                }
            }
        }
    }
}";

        internal const string ModuleInstanceScriptContent =
@"using StatusEffectsFramework;

public class #SCRIPTNAME# : ModuleInstance
{

}";

        internal const string DynamicEffectFloatScriptContent =
@"using StatusEffectsFramework;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Dynamic Effects/#DISPLAYNAME#"", order = 1)]
public class #SCRIPTNAME# : DynamicEffectFloat
{
    // Whether the value is applied after all other effects instead of together with them.
    public override bool PostEvaluate => false;

    public override DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
    {
        var dynamicFloat = new DynamicFloat(this, effect);

        // Set dynamicFloat.Value now and whenever it changes. Stop updating it when statusEffect.Stopped is raised.

        return dynamicFloat;
    }
}";

        internal const string EntityDynamicEffectFloatScriptContent =
@"using StatusEffectsFramework;
using StatusEffectsFramework.Entities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(DynamicFloats<#SCRIPTNAME#Struct>))]

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Dynamic Effects/#DISPLAYNAME#"", order = 1)]
public class #SCRIPTNAME# : DynamicEffectFloat, IEntityDynamicEffect
{
    // Whether the value is applied after all other effects instead of together with them.
    // Update the values in the matching system group below: DynamicEffectPostEvaluateSystemGroup
    // when this is true, DynamicEffectPreEvaluateSystemGroup when it is false.
    public override bool PostEvaluate => false;

    public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
    {
        var dynamicEffectStruct = new #SCRIPTNAME#Struct
        {
            // Copy values from this dynamic effect to the struct here.
        };
        DynamicEffectInfo.AllocateDynamicEffect(dynamicEffectStruct, valueType, ref info, ref builder);
    }

    // Only used when Entities is not running the status effects.
    public override DynamicFloat ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
    {
        return new DynamicFloat(this, effect);
    }
}

public struct #SCRIPTNAME#Struct
{

}

[UpdateInGroup(typeof(DynamicEffectPreEvaluateSystemGroup))]
[BurstCompile]
public partial struct #SCRIPTNAME#System : ISystem
{
    private EntityQuery m_EntityQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_EntityQuery = SystemAPI.QueryBuilder().WithAll<DynamicFloats<#SCRIPTNAME#Struct>>().WithAll<Simulate>().Build();

        state.RequireForUpdate(m_EntityQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new #SCRIPTNAME#Job();
        state.Dependency = job.ScheduleParallel(m_EntityQuery, state.Dependency);
    }

    [BurstCompile]
    partial struct #SCRIPTNAME#Job : IJobEntity
    {
        public void Execute(ref DynamicBuffer<DynamicFloats<#SCRIPTNAME#Struct>> dynamicValues)
        {
            for (int i = 0; i < dynamicValues.Length; i++)
            {
                ref var dynamicValue = ref dynamicValues.ElementAt(i);
                // Set dynamicValue.Value here.
            }
        }
    }
}";

        internal const string DynamicEffectIntScriptContent =
@"using StatusEffectsFramework;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Dynamic Effects/#DISPLAYNAME#"", order = 1)]
public class #SCRIPTNAME# : DynamicEffectInt
{
    // Whether the value is applied after all other effects instead of together with them.
    public override bool PostEvaluate => false;

    public override DynamicInt ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
    {
        var dynamicInt = new DynamicInt(this, effect);

        // Set dynamicInt.Value now and whenever it changes. Stop updating it when statusEffect.Stopped is raised.

        return dynamicInt;
    }
}";

        internal const string EntityDynamicEffectIntScriptContent =
@"using StatusEffectsFramework;
using StatusEffectsFramework.Entities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(DynamicInts<#SCRIPTNAME#Struct>))]

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Dynamic Effects/#DISPLAYNAME#"", order = 1)]
public class #SCRIPTNAME# : DynamicEffectInt, IEntityDynamicEffect
{
    // Whether the value is applied after all other effects instead of together with them.
    // Update the values in the matching system group below: DynamicEffectPostEvaluateSystemGroup
    // when this is true, DynamicEffectPreEvaluateSystemGroup when it is false.
    public override bool PostEvaluate => false;

    public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
    {
        var dynamicEffectStruct = new #SCRIPTNAME#Struct
        {
            // Copy values from this dynamic effect to the struct here.
        };
        DynamicEffectInfo.AllocateDynamicEffect(dynamicEffectStruct, valueType, ref info, ref builder);
    }

    // Only used when Entities is not running the status effects.
    public override DynamicInt ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
    {
        return new DynamicInt(this, effect);
    }
}

public struct #SCRIPTNAME#Struct
{

}

[UpdateInGroup(typeof(DynamicEffectPreEvaluateSystemGroup))]
[BurstCompile]
public partial struct #SCRIPTNAME#System : ISystem
{
    private EntityQuery m_EntityQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_EntityQuery = SystemAPI.QueryBuilder().WithAll<DynamicInts<#SCRIPTNAME#Struct>>().WithAll<Simulate>().Build();

        state.RequireForUpdate(m_EntityQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new #SCRIPTNAME#Job();
        state.Dependency = job.ScheduleParallel(m_EntityQuery, state.Dependency);
    }

    [BurstCompile]
    partial struct #SCRIPTNAME#Job : IJobEntity
    {
        public void Execute(ref DynamicBuffer<DynamicInts<#SCRIPTNAME#Struct>> dynamicValues)
        {
            for (int i = 0; i < dynamicValues.Length; i++)
            {
                ref var dynamicValue = ref dynamicValues.ElementAt(i);
                // Set dynamicValue.Value here.
            }
        }
    }
}";

        internal const string DynamicEffectBoolScriptContent =
@"using StatusEffectsFramework;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Dynamic Effects/#DISPLAYNAME#"", order = 1)]
public class #SCRIPTNAME# : DynamicEffectBool
{
    // Whether the value is applied after all other effects instead of together with them.
    public override bool PostEvaluate => false;

    public override DynamicBool ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
    {
        var dynamicBool = new DynamicBool(this, effect);

        // Set dynamicBool.Value now and whenever it changes. Stop updating it when statusEffect.Stopped is raised.

        return dynamicBool;
    }
}";

        internal const string EntityDynamicEffectBoolScriptContent =
@"using StatusEffectsFramework;
using StatusEffectsFramework.Entities;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(DynamicBools<#SCRIPTNAME#Struct>))]

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effects Framework/Dynamic Effects/#DISPLAYNAME#"", order = 1)]
public class #SCRIPTNAME# : DynamicEffectBool, IEntityDynamicEffect
{
    // Whether the value is applied after all other effects instead of together with them.
    // Update the values in the matching system group below: DynamicEffectPostEvaluateSystemGroup
    // when this is true, DynamicEffectPreEvaluateSystemGroup when it is false.
    public override bool PostEvaluate => false;

    public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
    {
        var dynamicEffectStruct = new #SCRIPTNAME#Struct
        {
            // Copy values from this dynamic effect to the struct here.
        };
        DynamicEffectInfo.AllocateDynamicEffect(dynamicEffectStruct, valueType, ref info, ref builder);
    }

    // Only used when Entities is not running the status effects.
    public override DynamicBool ValueEvent(StatusManager manager, StatusEffect statusEffect, Effect effect)
    {
        return new DynamicBool(this, effect);
    }
}

public struct #SCRIPTNAME#Struct
{

}

[UpdateInGroup(typeof(DynamicEffectPreEvaluateSystemGroup))]
[BurstCompile]
public partial struct #SCRIPTNAME#System : ISystem
{
    private EntityQuery m_EntityQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        m_EntityQuery = SystemAPI.QueryBuilder().WithAll<DynamicBools<#SCRIPTNAME#Struct>>().WithAll<Simulate>().Build();

        state.RequireForUpdate(m_EntityQuery);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var job = new #SCRIPTNAME#Job();
        state.Dependency = job.ScheduleParallel(m_EntityQuery, state.Dependency);
    }

    [BurstCompile]
    partial struct #SCRIPTNAME#Job : IJobEntity
    {
        public void Execute(ref DynamicBuffer<DynamicBools<#SCRIPTNAME#Struct>> dynamicValues)
        {
            for (int i = 0; i < dynamicValues.Length; i++)
            {
                ref var dynamicValue = ref dynamicValues.ElementAt(i);
                // Set dynamicValue.Value here.
            }
        }
    }
}";
    }
}
#endif