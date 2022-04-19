using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Movement_Deck", menuName = "ScriptableObjects/Movement Deck")]
public class MovementDeck : ScriptableObject
{
    public MoveCard[] deck;
}
