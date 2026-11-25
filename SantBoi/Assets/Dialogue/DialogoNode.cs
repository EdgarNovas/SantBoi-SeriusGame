using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using Dialogo.Editor;
using UnityEditor;
#endif

namespace Dialogo
{  
    public class DialogoNode : ScriptableObject
    {
        [SerializeField]
        bool isPlayerSpeaking = false;
        
        [SerializeField]
        LocalizedString speakerName = new LocalizedString();

        [SerializeField]
        LocalizedString dialogo = new LocalizedString();

        [SerializeField]
        CardData followingCard;

        [SerializeField]
        DialogoNode leftCardDestination;

        [SerializeField]
        DialogoNode rightCardDestination;
        
        [SerializeField]
        List<string> respuestas = new List<string>();
        
        [SerializeField]
        Rect rect = new Rect(0,0,200, 160);


        public Rect GetRect() => rect;

        public CardData FollowingCard => followingCard;

        /// <summary>Returns the continuation for the selected side, or null at the end of the branch.</summary>
        public DialogoNode GetCardDestination(CardChoice choice)
        {
            return choice == CardChoice.Left ? leftCardDestination : rightCardDestination;
        }
        
        /// <summary>
        /// Obtiene el diálogo en el idioma actual
        /// </summary>
        public string GetDialogo() => dialogo.GetText();
        
        /// <summary>
        /// Obtiene el diálogo en un idioma específico
        /// </summary>
        public string GetDialogo(SystemLanguage language) => dialogo.GetText(language);
        
        /// <summary>
        /// Obtiene el objeto LocalizedString completo para edición
        /// </summary>
        public LocalizedString GetLocalizedDialogo() => dialogo;
        
        public List<string> GetRespuestas() => respuestas;
        public bool IsPlayerSpeaking() => isPlayerSpeaking;
        
        /// <summary>
        /// Obtiene el nombre del hablante en el idioma actual
        /// </summary>
        public string GetSpeakerName() => speakerName.GetText();
        
        /// <summary>
        /// Obtiene el nombre del hablante en un idioma específico
        /// </summary>
        public string GetSpeakerName(SystemLanguage language) => speakerName.GetText(language);
        
        /// <summary>
        /// Obtiene el objeto LocalizedString completo del nombre para edición
        /// </summary>
        public LocalizedString GetLocalizedSpeakerName() => speakerName;

#if UNITY_EDITOR

        public void SetFollowingCard(CardData card)
        {
            if (followingCard == card) return;

            Undo.RecordObject(this, "Change Following Card");
            followingCard = card;
            EditorUtility.SetDirty(this);
        }

        public void SetCardDestination(CardChoice choice, DialogoNode destination)
        {
            if (GetCardDestination(choice) == destination) return;

            Undo.RecordObject(this, "Change Card Destination");
            if (choice == CardChoice.Left)
                leftCardDestination = destination;
            else
                rightCardDestination = destination;
            EditorUtility.SetDirty(this);
        }

        public void SetPosition(Vector2 newPosition)
        {
            Undo.RecordObject(this, "Move Dialogue Node");
            rect.position = newPosition;
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Establece el diálogo para el idioma actual del editor
        /// </summary>
        public void SetDialogo(string newDialogo)
        {
            SetDialogo(newDialogo, DialogEditorSettings.GetEditorLanguage());
        }

        /// <summary>
        /// Establece el diálogo para un idioma específico
        /// </summary>
        public void SetDialogo(string newDialogo, SystemLanguage language)
        {
            if (newDialogo != dialogo.GetText(language))
            {
                Undo.RecordObject(this, "Update Dialogue Text");
                dialogo.SetText(language, newDialogo);
                EditorUtility.SetDirty(this);
            }
        }

        public void AddRespuesta(string childID)
        {
            Undo.RecordObject(this, "Add Dialogue Link");
            respuestas.Add(childID);
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Establece el nombre del hablante para el idioma actual del editor
        /// </summary>
        public void SetSpeakerName(string newName)
        {
            SetSpeakerName(newName, DialogEditorSettings.GetEditorLanguage());
        }

        /// <summary>
        /// Establece el nombre del hablante para un idioma específico
        /// </summary>
        public void SetSpeakerName(string newName, SystemLanguage language)
        {
            if (newName != speakerName.GetText(language))
            {
                Undo.RecordObject(this, "Update Speaker Name");
                speakerName.SetText(language, newName);
                EditorUtility.SetDirty(this);
            }
        }

        public void SetSize(Vector2 newSize)
        {
            if (rect.size != newSize)
            {
                Undo.RecordObject(this, "Resize Node");
                rect.size = newSize;
                EditorUtility.SetDirty(this);
            }
        }

        public void RemoveRespuesta(string childID)
        {
            Undo.RecordObject(this, "Remove Dialogue Link");
            respuestas.Remove(childID);
            EditorUtility.SetDirty(this);
        }

        public void SetPlayerSpeaking(bool newIsPlayerSpeaking)
        {
            Undo.RecordObject(this, "Change Dialog Speaker");
            isPlayerSpeaking = newIsPlayerSpeaking;
            EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Asegura que existen entradas para todos los idiomas soportados
        /// </summary>
        public void EnsureAllLanguages()
        {
            foreach (SystemLanguage lang in LocalizationManager.SupportedLanguages)
            {
                dialogo.EnsureLanguageExists(lang);
                speakerName.EnsureLanguageExists(lang);
            }
        }

#endif
    }
}
