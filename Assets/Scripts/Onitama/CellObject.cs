using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CellObject : MonoBehaviour
{
    Vector2Int redHome = new Vector2Int(2, 0);
    Vector2Int blueHome = new Vector2Int(2, 4);
    [HideInInspector] public GameGUI ui;

    public Button button;
    public Image cellImg;
    public Image pieceImg;
    public Image selectable;
    public Vector2Int coords;

    [Header("Art")]
    public Color emptyColor;
    public Color spaceColorRed;
    public Color spaceColorBlue;
    public Color pieceColorRed;
    public Color pieceColorBlue;
    public Sprite masterSprite;
    public Sprite pawnSprite;

    public void SetCoords(Vector2Int newCoords)
    {
        coords = newCoords;
        if (coords == redHome)
            cellImg.color = spaceColorRed;
        else if (coords == blueHome)
            cellImg.color = spaceColorBlue;
        else
            cellImg.color = emptyColor;
    }

    public void SetButton(bool enable)
    {
        button.enabled = enable;
        selectable.enabled = enable;
    }

    public void UpdateCell(int value)
    {
        // Resets cell colors
        if (ui.game.gameState == GameStatus.Playing)
            SetCoords(coords);

        switch (value)
        {
            case -1:
                pieceImg.enabled = true;
                pieceImg.color = pieceColorBlue;
                pieceImg.sprite = pawnSprite;
                break;
            case -2:
                pieceImg.enabled = true;
                pieceImg.color = pieceColorBlue;
                pieceImg.sprite = masterSprite;
                break;
            case 1:
                pieceImg.enabled = true;
                pieceImg.color = pieceColorRed;
                pieceImg.sprite = pawnSprite;
                break;
            case 2:
                pieceImg.enabled = true;
                pieceImg.color = pieceColorRed;
                pieceImg.sprite = masterSprite;
                break;
            case 0:
                pieceImg.enabled = false;
                break;
        }
    }

    public void SetColor(Color c)
    {
        cellImg.color = c;
    }

    public void Clicked()
    {
        if (ui.currentState == TurnState.ChoosePiece)
            ui.ChoosePiece(coords);
        else if (ui.currentState == TurnState.ChooseSpace)
            ui.ChooseSpace(coords);
    }
}
