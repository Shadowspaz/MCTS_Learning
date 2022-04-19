using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Matrix
{
    private float[,] mat;

    public Matrix(int width, int height)
    {
        mat = new float[width, height];
    }

    public Matrix(Matrix a)
    {
        mat = new float[a.x, a.y];

        for (int i = 0; i < a.x; i++)
        {
            for (int j = 0; j < a.y; j++)
            {
                mat[i, j] = a[i, j];
            }
        }
    }

    public float this[int i1, int i2]
    {
        get { return mat[i1, i2]; }
        set { mat[i1, i2] = value; }
    }

    public Matrix this[params float[] f]
    {
        get
        {
            for (int i = 0; i < y; i++)
            {
                for (int j = 0; j < x; j++)
                {
                    if (i * x + j >= f.Length) return this;
                    mat[j,i] = f[i * x + j];
                }
            }
            return this;
        }
    }

    public int x { get { return mat.GetLength(0); }}
    public int y { get { return mat.GetLength(1); }}

    // public static Matrix operator =(Matrix a)
    // {
        
    //     Matrix r = new Matrix(a.x, a.y);

    //     for (int i = 0; i < a.x; i++)
    //     {
    //         for (int j = 0; j < a.y; j++)
    //         {
    //             r[i, j] = a[i, j];
    //         }
    //     }

    //     return r;
    // }

    public static Matrix operator *(Matrix a, Matrix b)
    {
        if (a.x != b.y) 
        {
            Debug.Log("Size mismatch. Cannot multiply matrices.");
            return 0;
        }

        int len = a.x;

        Matrix r = new Matrix(b.x, a.y);

        for (int i = 0; i < b.x; i++)
        {
            for (int j = 0; j < a.y; j++)
            {
                float v = 0;
                for (int k = 0; k < len; k++)
                {
                    v += a[k, j] * b[i, k];
                }
                r[i, j] = v;
            }
        }

        return r;
    }

    public static Matrix operator *(Matrix a, float b)
    {
        
        Matrix r = new Matrix(a.x, a.y);

        for (int i = 0; i < a.x; i++)
        {
            for (int j = 0; j < a.y; j++)
            {
                r[i, j] = a[i, j] * b;
            }
        }

        return r;
    }

    public static Matrix operator*(float a, Matrix b)
    {
        return b * a;
    }

    
    public static Matrix operator -(Matrix a)
    {
        return -1 * a;
    }

    public static Matrix operator +(Matrix a, Matrix b)
    {
        if (a.x != b.x || a.y != b.y)
        {
            Debug.Log("Size mismatch. Cannot add matrices.");
            return 0;
        }

        Matrix r = new Matrix(a.x, a.y);
        
        for (int i = 0; i < a.x; i++)
        {
            for (int j = 0; j < a.y; j++)
            {
                r[i, j] = a[i, j] + b[i, j];
            }
        }

        return r;
    }

    public static Matrix operator -(Matrix a, Matrix b)
    {
        return (a + -b);
    }

    public static implicit operator Matrix(float i)
    {
        return new Matrix(1, 1) { [0,0] = i };
    }

    public static Matrix Transpose(Matrix a)
    {
        Matrix r = new Matrix(a.y, a.x);
        
        for (int i = 0; i < a.x; i++)
        {
            for (int j = 0; j < a.y; j++)
            {
                r[j, i] = a[i, j];
            }
        }

        return r;
    }

    public override string ToString()
    {
        string r = "{";
        for (int i = 0; i < y; i++)
        {
            r += " [ ";
            for (int j = 0; j < x; j++)
            {
                r += mat[j, i];
                if (j < x - 1) r += ", ";
            }
            r += " ]";
            if (i < y - 1) r += ",";
        }
        r += " }";

        return r;
    }
}

public class NeuralNetwork
{
    public int inCount;
    public int outCount;
    public int[] layerCounts;
    private List<Matrix> layers = new List<Matrix>();
    private List<Matrix> weights = new List<Matrix>();

    public NeuralNetwork(int inputs, int outputs, params int[] hiddenLayers)
    {
        inCount = inputs;
        outCount = outputs;
        layerCounts = hiddenLayers;

        InitLists(inputs, outputs, hiddenLayers);

        Randomize();

        // Debug.Log("Layers: " + layers.Count);
        // for (int i = 0; i < layers.Count; i++)
        //     Debug.Log(layers[i]);

        // Debug.Log("Weights: " + weights.Count);
        // for (int i = 0; i < weights.Count; i++)
        //     Debug.Log(weights[i]);
    }

    public NeuralNetwork(NeuralNetwork clone)
    {
        inCount = clone.inCount;
        outCount = clone.outCount;
        layerCounts = new int[clone.layerCounts.Length];

        for (int i = 0; i < layerCounts.Length; i++)
            layerCounts[i] = clone.layerCounts[i];

        InitLists(inCount, outCount, layerCounts);

        CopyWeights(clone);
    }

    void InitLists(int inputs, int outputs, params int[] hiddenLayers)
    {
        // Set input matrix
        layers.Add(new Matrix(1, inputs));

        // Loop through hidden layers, building a matrix for each one
        for (int i = 0; i < hiddenLayers.Length; i++)
            layers.Add(new Matrix(1, hiddenLayers[i]));

        // Create output matrix
        layers.Add(new Matrix(1, outputs));

        // Create list of weight matrices
        for (int i = 1; i < layers.Count; i++)
            weights.Add(new Matrix(layers[i - 1].y, layers[i].y));
    }

    void CopyWeights(NeuralNetwork clone)
    {
        for (int i = 0; i < weights.Count; i++)
            for (int j = 0; j < weights[i].x; j++)
                for (int k = 0; k < weights[i].y; k++)
                    weights[i][j, k] = clone.weights[i][j, k];
    }

    public Matrix Calculate(params float[] inputs)
    {
        // Set inputs
        for (int i = 0; i < inputs.Length; i++)
            layers[0][0, i] = inputs[i];
    
        for (int i = 1; i < layers.Count; i++)
            layers[i] = SigmoidMatrix(weights[i - 1] * layers[i - 1]);

        return layers[layers.Count - 1];
    }

    Matrix SigmoidMatrix(Matrix m)
    {
        Matrix r = new Matrix(m.x, m.y);

        for (int i = 0; i < m.x; i++)
            for (int j = 0; j < m.y; j++)
                r[i, j] = Sigmoid(m[i, j]);

        return r;
    }

    float Sigmoid(float x)
    {
        return 1f / (1f + Mathf.Exp(x * -1f));
    }

    public void MutateWeights()
    {
        for (int i = 0; i < weights.Count; i++)
            for (int j = 0; j < weights[i].x; j++)
                for (int k = 0; k < weights[i].y; k++)
                    Mutate(i, j, k);
    }

    public void Mutate(int i, int x, int y)
    {
        int d = Random.Range(0, 3);
        switch (d)
        {
            case 0:
                weights[i][x, y] *= -1f;
                break;
            case 1:
                weights[i][x, y] *= Random.Range(0.8f, 1.2f);
                break;
            case 2:
                weights[i][x, y] *= Random.Range(0.5f, 1.5f);
                break;
        }
    }

    public void Randomize()
    {
        for (int i = 0; i < weights.Count; i++)
            for (int j = 0; j < weights[i].x; j++)
                for (int k = 0; k < weights[i].y; k++)
                    weights[i][j, k] = Random.Range(-30f, 30f);
    }

    #region Back Propagation

    public List<Matrix> CalculateError(Matrix expectedValue, Matrix calculatedValue)
    {
        List<Matrix> errorValues = new List<Matrix>();

        errorValues.Add(expectedValue - calculatedValue);

        for (int i = weights.Count - 1; i >= 0; i++)
        {
            errorValues.Insert(0, errorValues[errorValues.Count - 1] * Matrix.Transpose(weights[i]));
        }

        return errorValues;
    }

    public void BackPropagate(Matrix expectedValue)
    {
        
    }

    #endregion
}