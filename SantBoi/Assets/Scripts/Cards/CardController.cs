using UnityEngine;

/// <summary>
/// Connects the card the player is swiping with the game values:
/// when a side is chosen, the effects of that decision are applied.
/// </summary>
public class CardController : MonoBehaviour
{
    [SerializeField] Card card;
    [SerializeField] ValueTracker tracker;

    void OnEnable() => card.OnChoiceMade += HandleChoice;
    void OnDisable() => card.OnChoiceMade -= HandleChoice;

    void HandleChoice(Card chosenCard, CardChoice choice)
    {
        if (chosenCard.Data == null) return;

        CardDecision decision = chosenCard.Data.GetDecision(choice);
        tracker.Apply(decision.Effects);
    }
}