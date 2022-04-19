using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MCTS
{
    [System.Serializable]
    public class Tree
    {
        [SerializeField] float explorationValue = 0.6f;
        [SerializeField] int maxSimulationMoves = 30;
        [SerializeField] int processIterations = 50;
        [SerializeField] Node rootNode;

        public void Init(GameHandler startState)
        {
            rootNode = new Node(startState);
            rootNode.Expand();
        }

        public Move SelectBestMove()
        {
            Process(processIterations);

            float bestValue = Mathf.NegativeInfinity;
            int worstI = 0;

            for (int i = 0; i < rootNode.children.Count; i++)
            {
                if (rootNode.children[i].avgValue > bestValue)
                {
                    bestValue = rootNode.children[i].avgValue;
                    worstI = i;
                }
            }

            return rootNode.children[worstI].moveToState;
        }

        public void SwitchToNode(Move move)
        {
            for (int i = 0; i < rootNode.children.Count; i++)
            {
                if (rootNode.children[i].moveToState.Equals(move))
                {
                    rootNode = rootNode.children[i];

                    if (rootNode.isLeaf)
                        rootNode.Expand();

                    return;
                }
            }

            Debug.LogError("No matching legal move found.");
            return;
        }

        private void Process(int iterations)
        {
            for (int i = 0; i < iterations; i++)
            {
                Node node = FindLeaf(rootNode, rootNode.game.activePlayer);

                if (node.nodeVisits > 0)
                {
                    node.Expand();
                    if (!node.isLeaf) node = FindLeaf(node, rootNode.game.activePlayer);
                }

                Simulate(node);
            }
        }

        private void Simulate(Node node)
        {
            float v = node.Simulate(maxSimulationMoves);
            node.BackPropagate(v);
        }

        private Node FindLeaf(Node node, Player activePlayer)
        {
            if (node.isLeaf)
                return node;
            
            float bestUCB1 = Mathf.NegativeInfinity;
            int bestI = -1;

            for (int i = 0; i < node.children.Count; i++)
            {
                float testUCB1 = UCB1(node.children[i], activePlayer);
                if (testUCB1 > bestUCB1)
                {
                    bestUCB1 = testUCB1;
                    bestI = i;
                }
            }
            
            return FindLeaf(node.children[bestI], activePlayer);
        }

        // Calculate UCB1 value
        private float UCB1(Node node, Player activePlayer)
        {
            // Avoid divide-by-zero, prioritize unexplored nodes
            if (node.nodeVisits <= 0)
                return Mathf.Infinity;

            // Used to keep UCB1 value relative to the active player searching the tree
            float inverter = (activePlayer == node.game.activePlayer ? 1f : -1f);

            return (inverter * node.avgValue) + explorationValue * Mathf.Sqrt(Mathf.Log((float)node.parent.nodeVisits) / (float)node.nodeVisits);
        }
    }
}