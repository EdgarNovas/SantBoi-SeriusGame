using System.Collections.Generic;
using Dialogo;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueStoryDemo : MonoBehaviour
{
    private readonly struct FollowUp
    {
        public readonly DialogoNode Node;
        public readonly int EligibleAfter;

        public FollowUp(DialogoNode node, int eligibleAfter)
        {
            Node = node;
            EligibleAfter = eligibleAfter;
        }
    }

    private static readonly string[] ValueNames =
    {
        "Proactivitat", "Comunicació", "Participació", "Reconeixement", "Empatia", "Cura"
    };

    [SerializeField] private DialoguesAssetMenu[] stories;
    [SerializeField] private CardData[] everydayCards;
    [SerializeField, Min(1)] private int cardsPerShift = 15;
    [SerializeField, Min(1)] private int cardsBetweenFollowUps = 2;
    [SerializeField] private Card card;
    [SerializeField] private ValueTracker tracker;
    [SerializeField] private RectTransform[] valueBars;
    [SerializeField] private Text[] valueArrows;
    [SerializeField] private Text counter;
    [SerializeField] private Text warning;
    [SerializeField] private Text preview;
    [SerializeField] private Text leftLabel;
    [SerializeField] private Text rightLabel;
    [SerializeField] private GameObject choices;
    [SerializeField] private GameObject shiftEnd;
    [SerializeField] private Text shiftResult;
    [SerializeField] private Text reflection;

    private readonly List<FollowUp> followUps = new List<FollowUp>();
    private readonly int[] lowSince = new int[ValueNames.Length];
    private DialogoNode currentNode;
    private int storyIndex;
    private int everydayIndex;
    private int completed;
    private bool waitingForChoice;
    private bool dealAfterExit;
    private bool criticalEnd;
    private string lastReflection;

    private void OnEnable()
    {
        card.OnChoiceMade += OnChoiceMade;
        card.OnExited += OnCardExited;
        card.Movement.OnDragProgress += PreviewChoice;
    }

    private void OnDisable()
    {
        if (card == null) return;

        card.OnChoiceMade -= OnChoiceMade;
        card.OnExited -= OnCardExited;
        if (card.Movement != null)
            card.Movement.OnDragProgress -= PreviewChoice;
    }

    private void Start() => StartShift();

    public void StartShift()
    {
        followUps.Clear();
        storyIndex = everydayIndex = completed = 0;
        criticalEnd = dealAfterExit = false;
        lastReflection = string.Empty;
        for (int index = 0; index < lowSince.Length; index++)
            lowSince[index] = -1;
        warning.text = string.Empty;
        tracker.ResetAll();
        RefreshValues();
        shiftEnd.SetActive(false);
        DealNext();
    }

    public void ChooseLeft() => card.Movement.ChooseSide(CardChoice.Left);
    public void ChooseRight() => card.Movement.ChooseSide(CardChoice.Right);

    private void DealNext()
    {
        if (criticalEnd || completed >= cardsPerShift)
        {
            EndShift();
            return;
        }

        currentNode = null;
        int ready = completed >= 5
            ? followUps.FindIndex(followUp => followUp.EligibleAfter <= completed)
            : -1;
        if (ready >= 0)
        {
            currentNode = followUps[ready].Node;
            followUps.RemoveAt(ready);
        }
        else if (storyIndex < stories.Length)
        {
            currentNode = stories[storyIndex++].GetRootNode();
        }

        CardData data = currentNode != null ? currentNode.FollowingCard : null;
        if (data == null && everydayIndex < everydayCards.Length)
            data = everydayCards[everydayIndex++];

        if (data == null)
        {
            EndShift();
            return;
        }

        counter.text = $"{completed + 1} / {cardsPerShift}";
        leftLabel.text = $"←  {data.GetDecision(CardChoice.Left).Text}";
        rightLabel.text = $"{data.GetDecision(CardChoice.Right).Text}  →";
        choices.SetActive(true);
        waitingForChoice = true;
        card.ShowNext(data);
    }

    private void OnChoiceMade(Card chosenCard, CardChoice choice)
    {
        if (!waitingForChoice) return;

        waitingForChoice = false;
        choices.SetActive(false);
        PreviewChoice(0f);
        tracker.Apply(chosenCard.Data.GetDecision(choice).Effects);
        completed++;
        RefreshValues();
        UpdateWarning();

        if (currentNode != null)
        {
            lastReflection = chosenCard.Data.Debrief;
            DialogoNode destination = currentNode.GetCardDestination(choice);
            if (destination != null)
                followUps.Add(new FollowUp(destination, completed + cardsBetweenFollowUps));
        }
    }

    private void OnCardExited(Card _) => dealAfterExit = true;

    private void LateUpdate()
    {
        if (!dealAfterExit) return;

        dealAfterExit = false;
        DealNext();
    }

    private void PreviewChoice(float progress)
    {
        foreach (Text arrow in valueArrows)
            arrow.text = string.Empty;
        preview.text = string.Empty;
        if (!waitingForChoice || Mathf.Abs(progress) < 0.08f) return;

        bool isLeft = progress < 0f;
        CardDecision decision = card.Data.GetDecision(isLeft ? CardChoice.Left : CardChoice.Right);
        preview.text = isLeft ? $"←  {decision.Text}" : $"{decision.Text}  →";
        foreach (ValueEffect effect in decision.Effects)
        {
            Text arrow = valueArrows[(int)effect.Value];
            bool increases = effect.Amount > 0;
            arrow.text = increases ? "▲" : "▼";
            arrow.color = increases ? new Color32(119, 221, 188, 255) : new Color32(255, 174, 125, 255);
            arrow.resizeTextForBestFit = false;
            arrow.fontSize = Mathf.Abs(effect.Amount) >= 20 ? 21 : Mathf.Abs(effect.Amount) >= 10 ? 17 : 14;
        }
    }

    private void RefreshValues()
    {
        for (int index = 0; index < valueBars.Length; index++)
            valueBars[index].anchorMax = new Vector2(tracker.Get((Values)index) / 100f, 1f);
    }

    private void UpdateWarning()
    {
        warning.text = string.Empty;
        int fewestRemaining = 3;
        for (int index = 0; index < lowSince.Length; index++)
        {
            if (tracker.Get((Values)index) >= 10)
            {
                lowSince[index] = -1;
                continue;
            }

            if (lowSince[index] < 0) lowSince[index] = completed;
            int remaining = 2 - (completed - lowSince[index]);
            if (remaining == 0) criticalEnd = true;
            if (remaining > 0 && remaining < fewestRemaining)
            {
                fewestRemaining = remaining;
                warning.text = $"{ValueNames[index]} en riesgo · {remaining} {(remaining == 1 ? "carta" : "cartas")}";
            }
        }
    }

    private void EndShift()
    {
        card.gameObject.SetActive(false);
        choices.SetActive(false);
        warning.text = string.Empty;
        shiftResult.text = criticalEnd
            ? $"La jornada se cierra tras {completed} decisiones. Un valor necesita recuperarse."
            : $"Has tomado {completed} decisiones durante la jornada.";
        reflection.text = lastReflection;
        shiftEnd.SetActive(true);
    }
}
