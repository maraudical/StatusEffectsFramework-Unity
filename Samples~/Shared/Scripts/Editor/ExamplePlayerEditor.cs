using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectFramework.Samples.Editor
{
    [CustomEditor(typeof(ExamplePlayer))]
    [CanEditMultipleObjects]
    public class ExamplePlayerEditor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            IExamplePlayer[] players = targets.OfType<IExamplePlayer>().ToArray();

            var root = new VisualElement();

            var helpBox = new HelpBox() { text = "Please view the code in this script for example implementation!", messageType = HelpBoxMessageType.Info };
            root.Add(helpBox);

            var script = MonoScript.FromMonoBehaviour(serializedObject.targetObject as MonoBehaviour);
            var openScriptButton = new Button(() => AssetDatabase.OpenAsset(script)) { text = "Open Script" };
            root.Add(openScriptButton);

            var space = new VisualElement { style = { height = 10 } };
            root.Add(space);

            var iterator = serializedObject.GetIterator();
            iterator.NextVisible(true); // Skip the script field

            if (iterator.NextVisible(true))
            {
                do
                {
                    var propertyField = new PropertyField(iterator.Copy()) { name = "property-field: " + iterator.propertyPath };

                    if (iterator.propertyPath == "m_Script" && serializedObject.targetObject != null)
                        propertyField.SetEnabled(false);

                    root.Add(propertyField);
                }
                while (iterator.NextVisible(false));
            }

            var health = new PropertyField() { bindingPath = nameof(IExamplePlayer.Health) };
            health.SetEnabled(EditorApplication.isPlaying);
            root.Add(health);

            var debugLabel = new Label() { text = "<b>Debug Buttons" };
            debugLabel.style.marginTop = 10;
            debugLabel.style.marginLeft = 3;
            debugLabel.style.paddingLeft = 1;
            debugLabel.AddToClassList("unity-label");

            root.Add(debugLabel);

            Button addEffect = new Button() { text = "Add Effect" };
            foreach (var player in players)
                addEffect.clicked += player.DebugAddStatusEffect;
            root.Add(addEffect);

            Button addEffectTimed = new Button() { text = "Add Effect Timed" };
            foreach (var player in players)
                addEffectTimed.clicked += player.DebugAddStatusEffectTimed;
            root.Add(addEffectTimed);

            Button addEffectTimedEvent = new Button() { text = "Add Effect Timed Event" };
            foreach (var player in players)
                addEffectTimedEvent.clicked += player.DebugAddStatusEffectTimedEvent;
            root.Add(addEffectTimedEvent);

            Button invokeEvent = new Button() { text = "Invoke Event" };
            foreach (var player in players)
                invokeEvent.clicked += player.InvokeEvent;
            root.Add(invokeEvent);

            Button addEffectPredicate = new Button() { text = "Add Effect Predicate" };
            foreach (var player in players)
                addEffectPredicate.clicked += player.DebugAddStatusEffectPredicate;
            root.Add(addEffectPredicate);

            Button removeEffect = new Button() { text = "Remove Effect" };
            foreach (var player in players)
                removeEffect.clicked += player.DebugRemoveStatusEffect;
            root.Add(removeEffect);

            Button removeEffectGroup = new Button() { text = "Remove Effect Group" };
            foreach (var player in players)
                removeEffectGroup.clicked += player.DebugRemoveStatusEffectGroup;
            root.Add(removeEffectGroup);

            return root;
        }
    }
}