#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using System.Collections.Generic;
using System;

namespace StatusEffects.Inspector
{
    public class StatusEffectGroupProcessor : OdinAttributeProcessor<StatusEffectGroup>
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new DrawWithUnityAttribute() { PreferImGUI = true });
        }
    }

    public class StatusVariableProcessor : OdinAttributeProcessor<StatusVariable>
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new DrawWithUnityAttribute() { PreferImGUI = true });
        }
    }
}
#endif