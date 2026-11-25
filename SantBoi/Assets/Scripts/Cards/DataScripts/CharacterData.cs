using UnityEngine;

/// <summary>
/// A character that appears on cards (name + portrait).
/// Path: Assets > Create > Cards > Character Data.
/// </summary>
[CreateAssetMenu(fileName = "NewCharacter", menuName = "Cards/Character Data")]
public class CharacterData : ScriptableObject
{
    [SerializeField] string displayName;
    [SerializeField] Sprite portrait;

    public string DisplayName => displayName;
    public Sprite Portrait => portrait;
}