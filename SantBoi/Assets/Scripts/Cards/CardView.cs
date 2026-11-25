using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Card))]
public sealed class CardView : MonoBehaviour
{
    [SerializeField] Text characterName;
    [SerializeField] Image portrait;
    [SerializeField] Text situation;
    [SerializeField] Text leftOption;
    [SerializeField] Text rightOption;
    [SerializeField] Text sceneCaption;
    [SerializeField] Text portraitFallback;

    Card card;

    void OnEnable()
    {
        card = GetComponent<Card>();
        card.OnDataChanged += Show;
        Show(card.Data);
    }

    void OnDisable() => card.OnDataChanged -= Show;

    void Show(CardData data)
    {
        CharacterData character = data != null ? data.Character : null;
        characterName.text = character != null ? character.DisplayName : string.Empty;
        Sprite sprite = character != null ? character.Portrait : null;
        portrait.sprite = sprite;
        portrait.enabled = sprite != null;
        if (portraitFallback != null)
        {
            string displayName = characterName.text;
            portraitFallback.text = displayName.Length > 0 ? displayName.Substring(0, 1) : string.Empty;
            portraitFallback.enabled = sprite == null;
        }
        if (sceneCaption != null)
            sceneCaption.text = data != null ? data.Scene : string.Empty;
        situation.text = data != null ? data.Text : string.Empty;
        if (leftOption != null)
            leftOption.text = data != null ? data.GetDecision(CardChoice.Left)?.Text : string.Empty;
        if (rightOption != null)
            rightOption.text = data != null ? data.GetDecision(CardChoice.Right)?.Text : string.Empty;
    }
}
