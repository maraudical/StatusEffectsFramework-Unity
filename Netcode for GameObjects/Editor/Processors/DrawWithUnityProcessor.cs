#if NETCODE && ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
using Sirenix.OdinInspector;
using System.Collections.Generic;
using System;

namespace StatusEffectFramework.NetCode.Editor
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