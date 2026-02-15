//using EditorAttributes;
using StatusEffects.Entities;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class PredictedDestroyTest : MonoBehaviour
{
    /*[Button]
    public void Test(bool enabled)
    {
        foreach (var world in World.All)
        {
            using var entities = world.EntityManager.CreateEntityQuery(typeof(PredictedDestroy), typeof(StatusVariableUpdate)).ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
                world.EntityManager.SetComponentEnabled<PredictedDestroy>(entity, enabled);
        }  
    }*/

    /*[Button]
    public void Destroy()
    {
        foreach (var world in World.All)
        {
            using var entities = world.EntityManager.CreateEntityQuery(typeof(StatusEffects.Entities.Example.ExamplePlayer)).ToEntityArray(Allocator.Temp);
            foreach (var entity in entities)
                world.EntityManager.DestroyEntity(entity);
        }
    }*/
}