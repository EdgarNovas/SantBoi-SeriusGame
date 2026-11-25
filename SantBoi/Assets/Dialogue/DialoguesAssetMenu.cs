using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Dialogo
{
    [CreateAssetMenu(fileName = "Dialogo", menuName = "Torbellino Studio/Dialogo", order = 0)]
    public class DialoguesAssetMenu : ScriptableObject
    {
        [SerializeField]
        List<DialogoNode> nodes = new List<DialogoNode>();
        [SerializeField]
        Vector2 newNodeOffset = new Vector2(200, 0);

        Dictionary<string, DialogoNode> nodeLookup = new Dictionary<string, DialogoNode>();

        private void Awake()
        {
            OnValidate();
        }

        private void OnValidate()
        {
            nodeLookup.Clear();
            foreach(DialogoNode node in GetAllNodes())
            {
                if (node != null) nodeLookup[node.name] = node;
            }
        }

        public IEnumerable<DialogoNode> GetAllNodes()
        {
            return nodes;
        }

        public DialogoNode GetRootNode()
        {
            return nodes.Count > 0 ? nodes[0] : null;
        }

        public IEnumerable<DialogoNode> GetAllChildren(DialogoNode parentNode)
        {
            BuildLookup();

            foreach (string childID in parentNode.GetRespuestas())
            {
                if (nodeLookup.ContainsKey(childID))
                {
                    yield return nodeLookup[childID];
                }
            }
        }

        /// <summary>
        /// Obtiene un nodo por su nombre/ID
        /// Útil para importación de CSV
        /// </summary>
        public DialogoNode GetNodeByName(string nodeName)
        {
            BuildLookup();
            
            if (nodeLookup.ContainsKey(nodeName))
            {
                return nodeLookup[nodeName];
            }
            
            return null;
        }

        private void BuildLookup()
        {
            if (nodeLookup == null)
            {
                nodeLookup = new Dictionary<string, DialogoNode>();
            }
            if (nodeLookup.Count == 0 || nodeLookup.Count != nodes.Count)
            {
                nodeLookup.Clear();
                foreach (DialogoNode node in GetAllNodes())
                {
                    if (node != null)
                    {
                        nodeLookup[node.name] = node;
                    }
                }
            }
        }

#if UNITY_EDITOR
        public void EnsureRootNode()
        {
            if (nodes.Count != 0 || !AssetDatabase.Contains(this)) return;

            AddNode(MakeNode(null));
            AssetDatabase.SaveAssetIfDirty(this);
        }

        public void CreateNode(DialogoNode parent) => CreateNode(parent, null);

        public void CreateCardDestination(DialogoNode parent, CardChoice choice)
        {
            if (parent == null || parent.FollowingCard == null || !nodes.Contains(parent)) return;

            CreateNode(parent, choice);
        }

        private void CreateNode(DialogoNode parent, CardChoice? choice)
        {
            DialogoNode newNode = MakeNode(parent, choice);
            AddNode(newNode, recordUndo: true);

            if (choice.HasValue)
                parent.SetCardDestination(choice.Value, newNode);
            else if (parent != null)
                parent.AddRespuesta(newNode.name);
        }

        public void DeleteNode(DialogoNode nodeToDelete)
        {
            Undo.RecordObject(this, "Borrado Dialogo");
            nodes.Remove(nodeToDelete);
            OnValidate();
            CleanNodeChildren(nodeToDelete);
            Undo.DestroyObjectImmediate(nodeToDelete);
            EditorUtility.SetDirty(this);
        }

        private void AddNode(DialogoNode newNode, bool recordUndo = false)
        {
            AssetDatabase.AddObjectToAsset(newNode, this);
            if (recordUndo)
            {
                Undo.RegisterCreatedObjectUndo(newNode, "Created Dialogue Node");
                Undo.RecordObject(this, "Added Dialogue Node");
            }
            nodes.Add(newNode);
            OnValidate();
            EditorUtility.SetDirty(this);
        }

        private DialogoNode MakeNode(DialogoNode parent, CardChoice? choice = null)
        {
            DialogoNode newNode = CreateInstance<DialogoNode>();
            newNode.name = Guid.NewGuid().ToString();
            
            if (parent != null)
            {
                Vector2 offset = newNodeOffset;
                if (choice.HasValue)
                {
                    Rect parentRect = parent.GetRect();
                    offset.x = Mathf.Max(offset.x, parentRect.width + 80f);
                    float separation = Mathf.Max(parentRect.height, 280f) * 0.5f;
                    offset.y += choice.Value == CardChoice.Left ? -separation : separation;
                }
                newNode.SetPlayerSpeaking(!parent.IsPlayerSpeaking());
                newNode.SetPosition(parent.GetRect().position + offset);
            }

            // Asegurar que el nuevo nodo tiene entradas para todos los idiomas
            newNode.EnsureAllLanguages();

            return newNode;
        }

        private void CleanNodeChildren(DialogoNode nodeToDelete)
        {
            foreach (DialogoNode node in GetAllNodes())
            {
                if (node == null) continue;

                if (node.GetRespuestas().Contains(nodeToDelete.name))
                    node.RemoveRespuesta(nodeToDelete.name);
                if (node.GetCardDestination(CardChoice.Left) == nodeToDelete)
                    node.SetCardDestination(CardChoice.Left, null);
                if (node.GetCardDestination(CardChoice.Right) == nodeToDelete)
                    node.SetCardDestination(CardChoice.Right, null);
            }
        }

#endif
    }
}
