using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ValueNetwork : MonoBehaviour
{
    public NeuralNetwork v_network;

    public void Start()
    {
        v_network = new NeuralNetwork(30, 1, 20, 10);
    }

    float[] GenerateInputs(GameState gameState)
    {
        float[] inputs = new float[30];

        // Add boardState to inputs as 1D array
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                inputs[j + (i * 5)] = gameState.boardState[i,j];
            }
        }

        inputs[25] = gameState.myCards[0].id;
        inputs[26] = gameState.myCards[1].id;
        inputs[27] = gameState.yourCards[0].id;
        inputs[28] = gameState.yourCards[1].id;
        inputs[29] = gameState.flexCard.id;

        return inputs;
    }

    public float CalculateValue(GameState gameState)
    {
        float[] inputs = GenerateInputs(gameState);

        Matrix r_mat = v_network.Calculate(inputs);

        return r_mat[0,0];
    }

    public void TeachBoardState(GameState gameState, float expectedValue)
    {
        float[] inputs = GenerateInputs(gameState);
        Matrix calculated_mat = v_network.Calculate(inputs);

        Matrix expected_mat = new Matrix(1, 1);
        expected_mat[0, 0] = expectedValue;

        v_network.CalculateError(expected_mat, calculated_mat);
    }
}