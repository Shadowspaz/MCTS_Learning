using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardRenderer : MonoBehaviour
{
    public MoveCard card;

    [Header("References")]
    public Text titleBox;
    public Image cardBG;
    public Button button;
    [HideInInspector] public GameGUI ui;

    [Header("Grid Fields")]
    public Vector2 gridCenter;
    public float cellSize;
    public float cellPadding;

    [Header("Player Colors")]
    public Color redColor;
    public Color blueColor;

    [Header("Grid Colors")]
    public Color emptyCell = Color.white;
    public Color centerCell = Color.black;
    public Color moveCell = Color.gray;
    Image[,] cells = new Image[5,5];

    bool waitingOnStart = false;
    bool ready = false;

    void Start()
    {
        // Create sample grid
        Transform gridParent = new GameObject("Grid").transform;
        gridParent.SetParent(this.transform);
        gridParent.localPosition = new Vector3(gridCenter.x, gridCenter.y) * transform.localScale.x;

        Vector3 offset = new Vector2(-1, 1) * (cellSize + cellPadding) * 2f;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                string cellName = string.Format("Cell_{0},{1}", i, 4 - j);
                Transform newCell = new GameObject(cellName, typeof(Image)).transform;
                newCell.SetParent(gridParent);
                newCell.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSize, cellSize) * transform.localScale.x;
                newCell.localPosition = (offset + new Vector3(i * (cellSize + cellPadding), -j * (cellSize + cellPadding))) * transform.localScale.x;

                // 4 - j inverts the y axis to align properly (up is +, down is -)
                cells[i, 4 - j] = newCell.GetComponent<Image>();
            }
        }

        gridParent.localRotation = Quaternion.identity;

        ready = true;

        // Apply card if waiting
        if (waitingOnStart)
            ApplyCard(card);
    }

    public void ApplyCard(MoveCard newCard)
    {
        // Delay until after start if references are not fully set yet
        if (!ready)
        {
            card = newCard;
            waitingOnStart = true;
            return;
        }

        card = newCard;
        titleBox.text = card.name.ToUpper();

        // Get list of possible moves, relative to center (2, 2)
        List<Vector2Int> moveToCells = new List<Vector2Int>();
        for (int i = 0; i < card.availableMoves.Length; i++)
            moveToCells.Add(card.availableMoves[i] + new Vector2Int(2, 2));

        // Loop through all cells
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                // Color accordingly
                if (moveToCells.Contains(new Vector2Int(i, j)))
                    cells[i, j].color = moveCell;
                else
                    cells[i, j].color = emptyCell;
            }
        }

        // Color center cell
        cells[2, 2].color = centerCell;

        // Color cardBG based on card's startPlayer
        if (card.startPlayer == Player.Red)
            cardBG.color = redColor;
        else
            cardBG.color = blueColor;
    }

    public void SetButton(bool active)
    {
        // If this card isn't playable but the other one is, disable
        if (ui.game.GetLegalMoves(card).Count < 1 && !ui.needToPass)
            active = false;

        button.enabled = active;
    }

    public void ChooseCard()
    {
        if (ui.currentState == TurnState.ChooseCard)
            ui.ChooseCard(card);
    }
}
