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
@"using StatusEffects
using StatusEffects.Entities;
using Unity.Entities;
using UnityEngine;

[CreateAssetMenu(fileName = ""#DISPLAYNAME#"", menuName = ""Status Effect Framework/Modules/#DISPLAYNAME#"", order = 1)]
//[AttachModuleInstance(typeof(#SCRIPTNAME#Instance))]
public class #SCRIPTNAME# : Module, IEntityModule
{
    public void ModifyCommandBuffer(ref EntityCommandBuffer commandBuffer, in Entity entity, ModuleInstance moduleInstance)
    {
        //#SCRIPTNAME#Instance instance = moduleInstance as #SCRIPTNAME#Instance;
            
        commandBuffer.AddComponent(entity, new Entity#DISPLAYNAME#() 
        { 
                
        });
    }

    public struct Entity#DISPLAYNAME# : IComponentData
    {
            
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