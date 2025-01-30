#if ENTITIES && ADDRESSABLES
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StatusEffects.NetCode.Entities
{
    public interface IEntityStatus
    {
        public FixedString64Bytes ComponentId { get; }
        public void OnBake(Entity entity, StatusManagerBaker baker);
    }
}
#endif