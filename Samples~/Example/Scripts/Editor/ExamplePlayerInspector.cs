#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace StatusEffects.Example.Inspector
{
    public static class ExamplePlayerInspector
    {
        public static VisualElement DrawInspector(SerializedObject serializedObject, IExamplePlayer player)
        {
            var root = new VisualElement();

            var helpBox = new HelpBox() { text = "Please view the code in this script for example implementation!", messageType = HelpBoxMessageType.Info };
            helpBox.style.marginBottom = 10;

            root.Add(helpBox);

            var iterator = serializedObject.GetIterator();

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

            root.Add(new PropertyField() { bindingPath = nameof(IExamplePlayer.Health), enabledSelf = EditorApplication.isPlaying });

            var debugLabel = new Label() { text = "<b>Debug Buttons" };
            debugLabel.style.marginTop = 10;
            debugLabel.style.marginLeft = 3;
            debugLabel.style.paddingLeft = 1;
            debugLabel.AddToClassList("unity-label");

            root.Add(debugLabel);

            Button addEffect = new Button() { text = "Add Effect" };
            addEffect.clicked += player.DebugAddStatusEffect;
            root.Add(addEffect);

            Button addEffectTimed = new Button() { text = "Add Effect Timed" };
            addEffectTimed.clicked += player.DebugAddStatusEffectTimed;
            root.Add(addEffectTimed);

            Button addEffectTimedEvent = new Button() { text = "Add Effect Timed Event" };
            addEffectTimedEvent.clicked += player.DebugAddStatusEffectTimedEvent;
            root.Add(addEffectTimedEvent);

            Button invokeEvent = new Button() { text = "Invoke Event" };
            invokeEvent.clicked += player.InvokeEvent;
            root.Add(invokeEvent);

            Button addEffectPredicate = new Button() { text = "Add Effect Predicate" };
            addEffectPredicate.clicked += player.DebugAddStatusEffectPredicate;
            root.Add(addEffectPredicate);

            Button removeEffect = new Button() { text = "Remove Effect" };
            removeEffect.clicked += player.DebugRemoveStatusEffect;
            root.Add(removeEffect);

            Button removeEffectGroup = new Button() { text = "Remove Effect Group" };
            removeEffectGroup.clicked += player.DebugRemoveStatusEffectGroup;
            root.Add(removeEffectGroup);

            return root;
        }
    }
}
#endif