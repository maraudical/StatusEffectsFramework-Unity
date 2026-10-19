using System;
using System.Linq;
using UnityEditor;
using UnityEngine.UIElements;

namespace StatusEffectsFramework.Editor
{
    internal class StatusNamePopup : PopupWindowContent
    {
        public event Action<StatusNameSelection> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            var floatButton = new Button(() => SelectionType(StatusNameSelection.Float));
            floatButton.text = "Float";
            root.Add(floatButton);

            var intButton = new Button(() => SelectionType(StatusNameSelection.Int));
            intButton.text = "Int";
            root.Add(intButton);

            var boolButton = new Button(() => SelectionType(StatusNameSelection.Bool));
            boolButton.text = "Bool";
            root.Add(boolButton);

            return root;
        }

        public enum StatusNameSelection
        {
            Float,
            Int,
            Bool,
        }
    }

    internal class ModulePopup : PopupWindowContent
    {
        public event Action<ModuleSelection> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            var moduleButton = new Button(() => SelectionType(ModuleSelection.Module)) { text = "Module" };
            moduleButton.SetEnabled(TypeCache.GetTypesDerivedFrom<Module>()
                .Any(type => !type.IsAbstract && !type.ContainsGenericParameters));
            root.Add(moduleButton);

            var moduleScriptButton = new Button(() => SelectionType(ModuleSelection.ModuleScript)) { text = "Module Script" };
            root.Add(moduleScriptButton);

            var moduleInstanceScript = new Button(() => SelectionType(ModuleSelection.ModuleInstanceScript)) { text = "Module Instance Script" };
            root.Add(moduleInstanceScript);

            return root;
        }

        public enum ModuleSelection
        {
            Module,
            ModuleScript,
            ModuleInstanceScript
        }
    }

    internal class DynamicEffectPopup : PopupWindowContent
    {
        public event Action<DynamicEffectSelection> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            var dynamicEffectButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffect)) { text = "Dynamic Effect" };
            dynamicEffectButton.SetEnabled(TypeCache.GetTypesDerivedFrom<DynamicEffect>()
                .Any(type => !type.IsAbstract && !type.ContainsGenericParameters));
            root.Add(dynamicEffectButton);

            var dynamicEffectFloatScriptButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffectFloatScript)) { text = "Dynamic Effect Float Script" };
            root.Add(dynamicEffectFloatScriptButton);

            var dynamicEffectIntScriptButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffectIntScript)) { text = "Dynamic Effect Int Script" };
            root.Add(dynamicEffectIntScriptButton);

            var dynamicEffectBoolScriptButton = new Button(() => SelectionType(DynamicEffectSelection.DynamicEffectBoolScript)) { text = "Dynamic Effect Bool Script" };
            root.Add(dynamicEffectBoolScriptButton);

            return root;
        }

        public enum DynamicEffectSelection
        {
            DynamicEffect,
            DynamicEffectFloatScript,
            DynamicEffectIntScript,
            DynamicEffectBoolScript
        }
    }

    /// <summary>
    /// Lists every type that derives from <typeparamref name="TBase"/> and can be created as an asset.
    /// </summary>
    internal class TypePopup<TBase> : PopupWindowContent
    {
        public event Action<Type> SelectionType;

        public override VisualElement CreateGUI()
        {
            var root = new VisualElement();

            var types = TypeCache.GetTypesDerivedFrom<TBase>()
                .Where(type => !type.IsAbstract && !type.ContainsGenericParameters)
                .OrderBy(type => type.Name);
            foreach (var type in types)
            {
                var typeButton = new Button(() => SelectionType?.Invoke(type)) { text = type.Name };
                root.Add(typeButton);
            }

            return root;
        }
    }
}
