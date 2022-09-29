using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCTS
{
    [System.Serializable]
    public class Node
    {
        // game data
        public Move moveToState; // move that results in this game state
        public GameHandler game;

        public float nodePrior = 0; // Currently unused
        public int nodeVisits = 0;
        public float nodeValue = 0;
        public float avgValue { get { return nodeValue / (float)nodeVisits; } }

        // Node references
        public Node parent;
        public List<Node> children = new List<Node>();
        public bool isLeaf { get { return children.Count == 0; } }

        public Node(GameHandler state, Move move)
        {
            moveToState = move;
            game = new GameHandler(state);
        }

        // Constructor used for root state- No moveToState
        public Node(GameHandler state)
        {
            game = new GameHandler(state);
        }

        public void AddChild(GameHandler parentState, Move newMove)
        {
            GameHandler newState = new GameHandler(parentState);
            newState.ApplyMove(newState.activePlayer, newMove);

            Node child = new Node(newState, newMove);
            child.parent = this;
            children.Add(child);
        }

        public void Expand()
        {
            if (!isLeaf)
            {
                Debug.LogError("Cannot expand node- It is already expanded.");
                return;
            }

            if (game.gameState != GameStatus.Playing)
            {
                //Debug.LogError("Attempted to expand a terminal state.");
                return;
            }

            List<Move> allMoves = game.GetLegalMoves(game.activePlayer);

            // Shuffle list of moves to randomize children positions.
            // This will result in seemingly random choices when UCB1 values are equal.
            // Disabled for now until we're sure things are working properly.
            //allMoves.Sort((a, b) => 1 - 2 * Random.Range(0, 1));

            for (int i = 0; i < allMoves.Count; i++)
            {
                AddChild(game, allMoves[i]);
            }

            //Debug.Log(string.Format("Expanded node: Contains {0} children.", children.Count));
        }

        public void BackPropagate(float value)
        {
            nodeValue += value;
            nodeVisits++;
            parent?.BackPropagate(-value);
        }

        public float Simulate(int totalMoves)
        {
            // if node is a terminal state, evaluate more strongly.
            if (game.gameState != GameStatus.Playing)
                return 10f;

            GameHandler simulation = new GameHandler(game);

            for (int i = 0; i < totalMoves; i++)
            {
                // TODO: Why is there an "index out of range" error here?
                List<Move> moves = simulation.GetLegalMoves(simulation.activePlayer);
                simulation.ApplyMove(simulation.activePlayer, moves[Random.Range(0, moves.Count)]);

                // If game ended, set value to the maximum
                if (simulation.gameState != GameStatus.Playing)
                // Active player changes immediately after move. So if Blue wins, the active player will be Red
                    return 10f * (simulation.activePlayer != game.activePlayer ? 1f : -1f);
            }

            float boardSum = 0;
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    boardSum += simulation.boardState[i, j];
            
            return boardSum / 6f; // Total pieces adds up to 6. Higher positive means larger piece advantage
        }
    }
}
