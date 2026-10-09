#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

namespace StatusEffectsFramework.Mirror.Editor
{
    public class SyncStatusVariableProcessor : OdinAttributeProcessor<SyncStatusVariable>
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new DrawWithUnityAttribute() { PreferImGUI = true });
        }
    }
}
#endif
