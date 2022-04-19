using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MCTS;
using Tree = MCTS.Tree;

public class BotPlayer : MonoBehaviour
{
    [Header("Game References")]
    [SerializeField] GameGUI gameGUI;
    public Player playerID;
    [SerializeField] [Tooltip("How long should the botPlayer \"think\" for?")] float turnDelay = 2f;
    [SerializeField] [Tooltip("How long between clicks should the botPlayer pause?")] float turnSpeed = 0.75f;

    [Space]
    [SerializeField] Tree tree;
    Move nextMove;

    public void Initialize()
    {
        tree.Init(gameGUI.game);

        if (gameGUI.game.activePlayer == playerID)
            TakeTurn();
    }

    public void TakeTurn()
    {
        if (gameGUI.game.activePlayer != playerID)
        {
            Debug.LogError(string.Format("BotPlayer ({0}) is not the active player.", playerID));
            return;
        }

        nextMove = tree.SelectBestMove();
        StartCoroutine(AutomatedTurnCo());
    }

    public void PassMoveToTree(Move move)
    {
        tree.SwitchToNode(move);
    }

    IEnumerator AutomatedTurnCo()
    {
        yield return new WaitForSecondsRealtime(IsAuto(turnDelay));

        bool passing = nextMove.piece.x < 0;
        gameGUI.ChooseCard(nextMove.card);

        if (passing) yield break;

        yield return new WaitForSecondsRealtime(IsAuto(turnSpeed));

        Vector2Int selectedPiece;
        if (gameGUI.game.activePlayer == gameGUI.game.topOfBoard)
            selectedPiece = new Vector2Int(4 - nextMove.piece.x, 4 - nextMove.piece.y);
        else
            selectedPiece = nextMove.piece;

        gameGUI.ChoosePiece(selectedPiece);

        yield return new WaitForSecondsRealtime(IsAuto(turnSpeed));

        Vector2Int selectedSpace;
        if (gameGUI.game.activePlayer == gameGUI.game.topOfBoard)
            selectedSpace = new Vector2Int(4 - nextMove.moveTo.x, 4 - nextMove.moveTo.y);
        else
            selectedSpace = nextMove.moveTo;

        gameGUI.ChooseSpace(selectedSpace);

        yield break;
    }

    float IsAuto(float v)
    {
        if (gameGUI.autoPlay) return 0f;
        else return v;
    }
}
