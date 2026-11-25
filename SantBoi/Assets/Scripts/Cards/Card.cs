using System;
using UnityEngine;

/// <summary>
/// A card in the game: holds its information (CardData) and exposes what happens to it.
/// </summary>
[RequireComponent(typeof(CardMovement))]
public class Card : MonoBehaviour
{
    [Tooltip("Information of this card (character, text, decisions...). Can also be set from code with SetData().")]
    [SerializeField] CardData data;

    CardMovement movement;

    /// <summary>Information of this card. Can be null if none has been assigned.</summary>
    public CardData Data => data;

    /// <summary>The component that moves this card.</summary>
    public CardMovement Movement => movement != null ? movement : (movement = GetComponent<CardMovement>());

    /// <summary>Fired when the card's data changes.</summary>
    public event Action<CardData> OnDataChanged;
    /// <summary>Fired the moment the player confirms a side.</summary>
    public event Action<Card, CardChoice> OnChoiceMade;
    /// <summary>Fired when the card has finished leaving the screen.</summary>
    public event Action<Card> OnExited;

    void OnEnable()
    {
        Movement.OnChoiceMade += HandleChoice;
        Movement.OnExited += HandleExited;
    }

    void OnDisable()
    {
        Movement.OnChoiceMade -= HandleChoice;
        Movement.OnExited -= HandleExited;
    }

    void Start()
    {
        if (data != null) OnDataChanged?.Invoke(data);
    }

    /// <summary>Assigns new data to the card without touching its position.</summary>
    public void SetData(CardData newData)
    {
        data = newData;
        OnDataChanged?.Invoke(data);
    }

    /// <summary>Reuses this card for another one: new data and back to its home position.</summary>
    public void ShowNext(CardData newData)
    {
        SetData(newData);
        Movement.ResetToHome();
    }

    void HandleChoice(CardMovement cd, CardChoice choice) => OnChoiceMade?.Invoke(this, choice);
    void HandleExited(CardMovement cd) => OnExited?.Invoke(this);
}