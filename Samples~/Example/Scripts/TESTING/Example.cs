//using EditorAttributes;
using StatusEffects;
using System;
using UnityEngine;

public class Example : MonoBehaviour
{
    //public SerializedType<ISystem> type;
    //[GUIColor(GUIColor.Orange)]
    public float TEST;
    public LayerMask layermask;
    public StatusEffectGroup effectGroup;
    //[SerializeField, GUIColor(GUIColor.Blue)]
    private float unserializedFloat;
    //[SerializeField, GUIColor(GUIColor.Green)]
    private LayerMask masktest;
    // Dont forget to use the StatusEffect namespace!
    // Example variables
    //[PropertyOrder(-1)]
    public StatusFloat maxHealth;
    //[GUIColor(GUIColor.Pink)]
    //[UnitField(Unit.CentimetersPerSecond, Unit.CentimetersPerSecond)]
    public StatusInt coinMultiplier;
    //[GUIColor(GUIColor.Default)]
    public StatusBool stunned;
    //[Unit/*, GUIColor(GUIColor.Default)*/]
    public float ANOTHER;
    
    //[MinMaxSlider(0, 25)]
    //[UnitField(Unit.Percent, Unit.PercentMultiplier)]
    public Vector2 MinMaxTest;
    //[GUIColor(GUIColor.Default)]
    //[UnitField(Unit.Percent, Unit.PercentMultiplier)]
    //public float Percent;
    //[ShowInInspector]
    //public float SerializedPercent => Percent;

    //[UnitField(Unit.MetersPerSecond, Unit.MetersPerSecond)]
    //public string test;
    //public MYCLASS TESTER;

    //[Serializable]
    //public class MYCLASS
    //{
    //    public string name;
    //    public int Value;
    //}
    //[Button]
    //public void AddValue(float value) => Percent += value;
    ////[Button, GUIColor(GUIColor.Pink)]
    //private void Start()
    //{
    //    //Add this to your Start() or Awake() method

    //    // Obtain the StatusManager, this doesn't need to be on the
    //    // same GameObject but you need to get the reference for whatever 
    //    // you want to store this class's status effects.
    //    StatusManager statusManager = GetComponent<StatusManager>();
    //    maxHealth.SetManager(statusManager);
    //    coinMultiplier.SetManager(statusManager);
    //    stunned.SetManager(statusManager);
    //}
}
