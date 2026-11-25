using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps the current amount of every Value and applies the effects of the decisions.
/// Values are clamped between minValue and maxValue.
/// </summary>
public class ValueTracker : MonoBehaviour
{
    [SerializeField] int startValue = 50;
    [SerializeField] int minValue = 0;
    [SerializeField] int maxValue = 100;

    int[] current;

    /// <summary>Fired when a value changes.</summary>
    public event Action<Values, int, int> OnValueChanged;

    void Awake() => ResetAll();

    public void ResetAll()
    {
        current = new int[Enum.GetValues(typeof(Values)).Length];
        for (int i = 0; i < current.Length; i++)
            current[i] = startValue;
    }

    public int Get(Values value) => current[(int)value];

    public void Apply(IEnumerable<ValueEffect> effects)
    {
        foreach (ValueEffect effect in effects)
            Apply(effect);
    }

    public void Apply(ValueEffect effect)
    {
        int index = (int)effect.Value;
        int old = current[index];
        int updated = Mathf.Clamp(old + effect.Amount, minValue, maxValue);

        if (updated == old) return;

        current[index] = updated;

        Debug.Log(effect.Value.ToString() + " " + effect.Amount.ToString());

        OnValueChanged?.Invoke(effect.Value, updated, updated - old);
    }
}