using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum TurnState
{
    ChooseCard,
    ChoosePiece,
    ChooseSpace
}

public class GameGUI : MonoBehaviour
{
    private static GameGUI p_instance;
    public static GameGUI Instance
    {
        get
        {
            if (null != p_instance)
                return p_instance;
            else
            {
                p_instance = GameObject.FindObjectOfType<GameGUI>();
                return p_instance;
            }
        }
    }

    public Transform canvas;
    public GameObject cardPrefab;

    [Header("Cards")]
    public MovementDeck deck;

    [Header("Cells")]
    public GameObject cellPrefab;
    public float cellSize;
    public float cellPadding;

    [Header("Colors")]
    public Color redWinsColor;
    public Color blueWinsColor;
    
    [Header("Text")]
    [SerializeField] Text passText;

    [Space]
    public CardRenderer[] redCards = new CardRenderer[2];
    public CardRenderer[] blueCards = new CardRenderer[2];
    public CardRenderer flexCard;
    public CellObject[,] cells = new CellObject[5, 5];

    [Header("BotPlayer")]
    public List<BotPlayer> botPlayers;

    // Turn order
    public TurnState currentState;
    MoveCard selectedCard;
    Vector2Int selectedPiece;
    Vector2Int selectedMoveTo;

    // Passing
    private bool p_needToPass;
    public bool needToPass {
        get { return p_needToPass; }
        private set {
            p_needToPass = value;
            passText.enabled = value;
        }
    }

    [Header("Game")]
    public GameHandler game;
    private Coroutine betweenGamesCo = null;
    private bool enterAnalysisMode = false;
    private bool analysisMode = false;
    [SerializeField] private int analysisMove;  

    // Automatically start the next game
    [SerializeField] private bool p_autoPlay;
    public bool autoPlay { get { return p_autoPlay; } }

    bool startingNextGame = false;
    [SerializeField] float timeToNextGame = 1f;

    void Start()
    {
        needToPass = false;
        game = new GameHandler(deck.deck);
        DrawBoard();

        redCards[0] = AddCard(game.redCards[0], new Vector2(-150, -400), "Red Card 1");
        redCards[1] = AddCard(game.redCards[1], new Vector2(150, -400), "Red Card 2");
        blueCards[0] = AddCard(game.blueCards[0], new Vector2(-150, 400), "Blue Card 1", true);
        blueCards[1] = AddCard(game.blueCards[1], new Vector2(150, 400), "Blue Card 2", true);
        flexCard = AddCard(game.flexCard, new Vector2(500, 0), "Flex Card");

        UpdateCells();
        game.onUpdateVisual += UpdateCells;
        SetState(TurnState.ChooseCard);

        InitializeBots();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (analysisMode)
            {
                analysisMode = false;
                StartNewGame();
            }
            else enterAnalysisMode = true;
        }

        if (analysisMode)
        {
            AnalysisModeInput();
        }

        if (autoPlay && !analysisMode)
        {
            if (game.gameState != GameStatus.Playing)
            {
                if (enterAnalysisMode)
                {
                    enterAnalysisMode = false;
                    analysisMode = true;
                    analysisMove = game.gameHistory.Count - 1;
                }
                else if (!startingNextGame)
                {
                    if (null == betweenGamesCo) betweenGamesCo = StartCoroutine(WaitingRoutine());
                    startingNextGame = true;
                }
            }
        }
    }

    private IEnumerator WaitingRoutine()
    {
        float startTime = Time.time;
        while (Time.time - startTime < timeToNextGame)
        {
            if (analysisMode)
            {
                betweenGamesCo = null;
                yield break;
            }
            yield return null;
        }

        StartNewGame();
        betweenGamesCo = null;
        yield break;
    }

    private void AnalysisModeInput()
    {
        int next = 0;
        if (Input.GetKeyDown(KeyCode.LeftArrow)) next = -1;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) next = 1;

        if (next == 0) return;

        analysisMove = Mathf.Clamp(analysisMove + next, 0, game.gameHistory.Count - 1);
        game.SetGameState(game.gameHistory[analysisMove], true);
    }

    void StartNewGame()
    {
        game.StartGame(deck.deck);
        InitializeBots();

        UpdateAllCards();
        UpdateCells();

        SetState(TurnState.ChooseCard);
        
        // For autoStart
        startingNextGame = false;
    }

    // Initialize all bots in the array
    void InitializeBots()
    {
        // Remove disabled bots
        for (int i = botPlayers.Count - 1; i >= 0; i--)
        {
            if (!botPlayers[i].gameObject.activeInHierarchy)
                botPlayers.RemoveAt(i);
        }

        for (int i = 0; i < botPlayers.Count; i++)
            botPlayers[i].Initialize();
    }

    CardRenderer AddCard(MoveCard card, Vector3 position, string name = "", bool flipped = false)
    {
        Quaternion rot = flipped ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
        CardRenderer newCard = Instantiate(cardPrefab, Vector3.zero, rot, canvas).GetComponent<CardRenderer>();
        newCard.transform.name = name;
        newCard.transform.localPosition = position;
        newCard.ui = this;
        newCard.ApplyCard(card);

        return newCard;
    }

    void DrawBoard()
    {
        Vector3 offset = new Vector2(-1, 1) * (cellSize + cellPadding) * 2f;

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                string cellName = string.Format("Cell_{0},{1}", i, 4 - j);
                CellObject newCell = Instantiate(cellPrefab, Vector3.zero, Quaternion.identity, canvas).GetComponent<CellObject>();
                newCell.GetComponent<RectTransform>().sizeDelta = new Vector2(cellSize, cellSize) * transform.localScale.x;
                newCell.transform.localPosition = (offset + new Vector3(i * (cellSize + cellPadding), -j * (cellSize + cellPadding))) * transform.localScale.x;

                // 4 - j inverts the y axis to align properly (up is +, down is -)
                newCell.ui = this;
                newCell.SetCoords(new Vector2Int(i, 4 - j));
                cells[i, 4 - j] = newCell;
            }
        }
    }

    void SetState(TurnState state)
    {
        currentState = state;
        switch (state)
        {
            case TurnState.ChooseCard:
                // disable all spaces
                SetAllSpaces(false);

                // Check if there are no legal moves
                if (game.GetLegalMoves(game.activePlayer)[0].piece.x < 0)
                {
                    //Debug.Log("No legal moves! Choose a card to pass.");
                    needToPass = true;
                }
                else
                    needToPass = false;

                //enable only active player's cards
                if (game.activePlayer == Player.Red)
                {
                    redCards[0].SetButton(true);
                    redCards[1].SetButton(true);
                    blueCards[0].SetButton(false);
                    blueCards[1].SetButton(false);
                    flexCard.SetButton(false);
                }
                else
                {
                    redCards[0].SetButton(false);
                    redCards[1].SetButton(false);
                    blueCards[0].SetButton(true);
                    blueCards[1].SetButton(true);
                    flexCard.SetButton(false);
                }

                break;
            case TurnState.ChoosePiece:
                // Disable all cards
                SetAllCards(false);
                // Set pieces active if they have a valid move with the chosen card
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        Vector2Int checkV = (game.activePlayer == game.topOfBoard) ? new Vector2Int(4 - i, 4 - j) : new Vector2Int(i, j);

                        bool active = (game.boardState[checkV.x, checkV.y] > 0 && game.GetLegalMoves(selectedCard, checkV).Count > 0);
                        cells[i, j].SetButton(active);
                    }
                }
                break;
            case TurnState.ChooseSpace:
                // Disable all cards and spaces
                SetAllCards(false);
                SetAllSpaces(false);

                // Debug.Log(selectedPiece.x + ", " + selectedPiece.y);
                List<Move> legalMoves = game.GetLegalMoves(selectedCard, selectedPiece);
                for (int i = 0; i < legalMoves.Count; i++)
                {
                    // Set spaces active if legal move
                    Vector2Int checkV = (game.activePlayer == game.topOfBoard) ? new Vector2Int(4 - legalMoves[i].moveTo.x, 4 - legalMoves[i].moveTo.y) : legalMoves[i].moveTo;
                    cells[checkV.x, checkV.y].SetButton(true);
                }
                break;
        }
    }

    void SetAllCards (bool active)
    {
        redCards[0].SetButton(active);
        redCards[1].SetButton(active);
        blueCards[0].SetButton(active);
        blueCards[1].SetButton(active);
        flexCard.SetButton(active);
    }

    void UpdateAllCards()
    {
        redCards[0].ApplyCard(game.redCards[0]);
        redCards[1].ApplyCard(game.redCards[1]);
        blueCards[0].ApplyCard(game.blueCards[0]);
        blueCards[1].ApplyCard(game.blueCards[1]);
        flexCard.ApplyCard(game.flexCard);
    }

    void SetAllSpaces (bool active)
    {
        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                cells[i, j].SetButton(active);
    }

    public void ChooseCard (MoveCard c)
    {

        selectedCard = c;
        
        if (needToPass)
        {
            selectedMoveTo = selectedPiece = Vector2Int.one * -1;
            ApplySelectedTurn();
            return;
        }

        SetState(TurnState.ChoosePiece);
        
    }

    public void ChoosePiece(Vector2Int c)
    {
        if (game.activePlayer == game.topOfBoard)
            selectedPiece = new Vector2Int(4 - c.x, 4 - c.y);
        else
            selectedPiece = c;

        SetState(TurnState.ChooseSpace);
    }

    public void ChooseSpace(Vector2Int c)
    {
        if (game.activePlayer == game.topOfBoard)
            selectedMoveTo = new Vector2Int(4 - c.x, 4 - c.y);
        else
            selectedMoveTo = c;

        ApplySelectedTurn();
    }

    void ApplySelectedTurn()
    {
        Move move = new Move(selectedCard, selectedPiece, selectedMoveTo);

        game.ApplyMove(game.activePlayer, move, true);
        UpdateAllCards();
        SetState(TurnState.ChooseCard);

        if (game.gameState == GameStatus.Playing)
        {
            PassMoveToTrees(move);
            TakeBotTurn(game.activePlayer);
        }
    }

    void PassMoveToTrees(Move move)
    {
        for (int i = 0; i < botPlayers.Count; i++)
            botPlayers[i].PassMoveToTree(move);
    }

    void TakeBotTurn(Player player)
    {
        for (int i = 0; i < botPlayers.Count; i++)
            if (botPlayers[i].playerID == player)
                botPlayers[i].TakeTurn();
    }

    public void BackOut()
    {
        if (autoPlay) return;

        if (game.gameState != GameStatus.Playing)
        {
            StartNewGame();
        }

        switch (currentState)
        {
            case TurnState.ChoosePiece:
                SetState(TurnState.ChooseCard);
                break;
            case TurnState.ChooseSpace:
                SetState(TurnState.ChoosePiece);
                break;
        }
    }

    void UpdateCells()
    {
        if (game.gameState != GameStatus.Playing)
        {
            SetAllCards(false);
            SetAllSpaces(false);

            Color winner = game.gameState == GameStatus.RedWins ? redWinsColor : blueWinsColor;

            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    cells[i, j].SetColor(winner);
        }

        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                cells[i, j].UpdateCell(game.boardVisual[i, j]);
    }
}
