using System;
using System.Collections.Generic;
using UnityEngine;

public enum Values { PROACTIVITY, COMMUNICATION, PARTICIPATION, RECOGNITION, EMPATHY, CARE }

/// <summary>A change applied to one value.</summary>
[Serializable]
public struct ValueEffect
{
    [SerializeField] Values value;
    [SerializeField] int amount;

    public Values Value => value;
    public int Amount => amount;

    public ValueEffect(Values value, int amount)
    {
        this.value = value;
        this.amount = amount;
    }
}

/// <summary>One of the two options of a card: the text shown to the player and the effects it causes.</summary>
[Serializable]
public class CardDecision
{
    [SerializeField] string text;
    [SerializeField] List<ValueEffect> effects = new List<ValueEffect>();

    public string Text => text;
    public IReadOnlyList<ValueEffect> Effects => effects;
}

/// <summary>
/// Static information of a card. Create one asset per card:
/// Path: Assets > Create > Cards > Card Data.
/// </summary>
[CreateAssetMenu(fileName = "NewCard", menuName = "Cards/Card Data")]
public class CardData : ScriptableObject
{
    [Header("Base info")]
    [SerializeField] string id;
    [SerializeField] CharacterData character;
    [SerializeField] string scene;
    [SerializeField, TextArea(2, 6)] string text;
    [SerializeField] Values mainValue;
    [SerializeField, Min(0)] int difficulty;
    [SerializeField, TextArea(2, 6)] string debrief;

    [Header("Left decision")]
    [SerializeField] CardDecision left = new CardDecision();

    [Header("Right decision")]
    [SerializeField] CardDecision right = new CardDecision();

    public string Id => id;
    public CharacterData Character => character;
    public string Scene => scene;
    public string Text => text;
    public Values MainValue => mainValue;
    public int Difficulty => difficulty;
    public string Debrief => debrief;

    /// <summary>Returns the decision for the chosen side.</summary>
    public CardDecision GetDecision(CardChoice choice) => choice == CardChoice.Left ? left : right;

    // Default the id to the asset name so it is never empty.
    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) id = name;
    }
}
