using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CreateAssetMenu(fileName = "Movement_Deck", menuName = "ScriptableObjects/Movement Deck")]
public class MovementDeck : ScriptableObject
{
    public MoveCard[] deck;

#if UNITY_EDITOR
    [MenuItem(  "Assets/ID Cards in MovementDeck")]
    public static void IDCards()
    {
        MovementDeck myDeck = Selection.activeObject as MovementDeck;
        if (null == myDeck) return;

        for (int i = 0; i < myDeck.deck.Length; i++)
            myDeck.deck[i].id = i;

        EditorUtility.SetDirty(Selection.activeObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }
#endif
}
