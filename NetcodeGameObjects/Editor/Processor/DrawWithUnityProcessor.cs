#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

namespace StatusEffects.NetCode.GameObjects.Inspector
{
    public class NetworkStatusVariableProcessor : OdinAttributeProcessor<NetworkStatusVariable>
    {
        public override void ProcessSelfAttributes(InspectorProperty property, List<Attribute> attributes)
        {
            attributes.Add(new DrawWithUnityAttribute() { PreferImGUI = true });
        }
    }
}
#endif