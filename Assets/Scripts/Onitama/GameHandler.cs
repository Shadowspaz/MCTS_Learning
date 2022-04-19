using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum Player
{
    Red,
    Blue
}

public enum GameStatus
{
    Playing,
    RedWins,
    BlueWins
}

[System.Serializable]
public struct MoveCard
{
    public int id;
    public string name;
    public Vector2Int[] availableMoves;
    public Player startPlayer;
}

[System.Serializable]
public struct GameState
{
    public Player activePlayer;
    public int[,] boardState;
    public List<MoveCard> myCards;
    public List<MoveCard> yourCards;
    public MoveCard flexCard;

    public int[] ConvertToInputs()
    {
        int[] r = new int[30];
        int runningIndex = 0;

        for (int i = 0; i < boardState.GetLength(0); i++)
        {
            for (int j = 0; j < boardState.GetLength(1); j++)
            {
                r[runningIndex] = boardState[i, j];
                runningIndex++;
            }
        }

        r[runningIndex] = System.Array.IndexOf(GameGUI.Instance.deck.deck, myCards[0]);
        r[runningIndex + 1] = System.Array.IndexOf(GameGUI.Instance.deck.deck, myCards[1]);
        r[runningIndex + 2] = System.Array.IndexOf(GameGUI.Instance.deck.deck, yourCards[0]);
        r[runningIndex + 3] = System.Array.IndexOf(GameGUI.Instance.deck.deck, yourCards[1]);
        r[runningIndex + 4] = System.Array.IndexOf(GameGUI.Instance.deck.deck, flexCard);

        return r;
    }
}

public struct Move
{
    public MoveCard card;
    public Vector2Int piece;
    public Vector2Int moveTo;

    public Move (MoveCard c, Vector2Int p, Vector2Int m)
    {
        card = c;
        piece = p;
        moveTo = m;
    }
}

[System.Serializable]
public class GameHandler
{
    //  0 = Empty
    // -1 = Opponent pawn
    // -2 = Opponent master
    //  1 = Active pawn
    //  2 = Active master
    static readonly int[,] boardStart = new int[5, 5]
    {
        {  1,  0,  0,  0, -1},
        {  1,  0,  0,  0, -1},
        {  2,  0,  0,  0, -2},
        {  1,  0,  0,  0, -1},
        {  1,  0,  0,  0, -1}
    };

    public List<GameState> gameHistory = new List<GameState>();
    public int[,] boardState = new int[5, 5];
    public int[,] boardVisual = new int[5, 5];
    public UnityAction onUpdateVisual; 

    //Cards
    public List<MoveCard> redCards = new List<MoveCard>();
    public List<MoveCard> blueCards = new List<MoveCard>();
    public MoveCard flexCard;

    public GameStatus gameState;
    public Player startPlayer;
    public Player activePlayer = Player.Red;

    // For visual display
    public Player topOfBoard = Player.Blue;

    // Instantiate a new GameHandler as a copy of source
    public GameHandler(GameHandler source)
    {
        boardState = CopyBoard(source.boardState);
        gameState = source.gameState;
        startPlayer = source.startPlayer;
        activePlayer = source.activePlayer;

        for (int i = 0; i < source.redCards.Count; i++)
            redCards.Add(source.redCards[i]);

        for (int i = 0; i < source.blueCards.Count; i++)
            blueCards.Add(source.blueCards[i]);

        flexCard = source.flexCard;   
    }

    // Instantiate a new GameHandler using a given deck of cards. Start the game.
    public GameHandler(MoveCard[] deck)
    {
        StartGame(deck);
    }

    // Copy boardStart values into a new boardState, randomly choose cards from deck
    public void StartGame(MoveCard[] deck)
    {
        boardState = CopyBoard(boardStart);
        boardVisual = CopyBoard(boardStart);

        gameHistory.Clear();

        GetRandomCards(deck);
        gameState = GameStatus.Playing;
    }
    
    // Distribute two random cards to each player, plus one to flex. Choose start player
    void GetRandomCards(MoveCard[] deck)
    {
        // Reset player's cards
        redCards.Clear();
        blueCards.Clear();

        // Get 5 unique indices
        List<int> cards = new List<int>();

        for (int i = 0; i < 5; i++)
        {
            int n = -1;
            while (n < 0 || cards.Contains(n))
                n = Random.Range(0, deck.Length);  
            cards.Add(n);
        }

        // Distribute starting two cards
        redCards.Add(deck[cards[0]]);
        redCards.Add(deck[cards[2]]);
        blueCards.Add(deck[cards[1]]);
        blueCards.Add(deck[cards[3]]);

        // Set flex card and decide start player
        flexCard = deck[cards[4]];
        startPlayer = flexCard.startPlayer;

        activePlayer = startPlayer;
    }

    // Inverts all values to swap Opponent/Active pieces, rotates board to keep relative positions the same.
    public int[,] InvertBoard(int[,] source)
    {
        int[,] r = new int[5, 5];

        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                r[i, j] = source[4 - i, 4 - j] * -1;

        return r;
    }

    // Copy 2D array. Returns copy.
    public int[,] CopyBoard(int[,] source)
    {
        int[,] r = new int[5, 5];

        for (int i = 0; i < 5; i++)
            for (int j = 0; j < 5; j++)
                r[i, j] = source[i, j];

        return r;
    }

#region GetLegalMoves
    // Returns all legal moves for the specified player.
    public List<Move> GetLegalMoves (Player player)
    {
        switch (player)
        {
            case Player.Red:
                return GetLegalMoves(ref redCards);
            default:
                return GetLegalMoves(ref blueCards);
        }    
    }

    // Helper function for getting player moves.
    List<Move> GetLegalMoves (ref List<MoveCard> cardList)
    {
        List<Move> r = new List<Move>();

        for (int i = 0; i < cardList.Count; i++)
        {
            List<Move> movesForCard = GetLegalMoves(cardList[i]);
            for (int j = 0; j < movesForCard.Count; j++)
                r.Add(movesForCard[j]);
        }

        if (r.Count > 0)
            return r;

        // Add "pass" options for each card.
        r.Add(new Move(cardList[0], new Vector2Int(-1, -1), new Vector2Int(-1, -1)));
        r.Add(new Move(cardList[1], new Vector2Int(-1, -1), new Vector2Int(-1, -1)));

        return r;
    }

    // Returns all legal moves for a given card.
    public List<Move> GetLegalMoves (MoveCard card)
    {
        List<Move> r = new List<Move>();

        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 5; j++)
            {
                if (boardState[i, j] > 0)
                {
                    Vector2Int piece = new Vector2Int(i, j);
                    List<Move> movesForPiece = GetLegalMoves(card, piece);
                    for (int k = 0; k < movesForPiece.Count; k++)
                        r.Add(movesForPiece[k]);
                }
            }
        }

        return r;       
    }

    // Returns all legal moves for a given card and piece.
    public List<Move> GetLegalMoves(MoveCard card, Vector2Int piece)
    {
        List<Move> r = new List<Move>();

        for (int i = 0; i < card.availableMoves.Length; i++)
        {
            Vector2Int moveTo = (piece + card.availableMoves[i]);

            if (IsBetween(moveTo.x, 0, 5) && IsBetween(moveTo.y, 0, 5))
                if (boardState[moveTo.x, moveTo.y] <= 0)
                    r.Add(new Move(card, piece, moveTo));
        }

        return r;
    }
#endregion

    public void ApplyMove(Player player, Move move, bool visual = false)
    {
        // Rotate cards
        switch (player)
        {
            case Player.Red:
                RemoveCard(ref redCards, player, move.card);
                redCards.Add(flexCard);
                break;
            default:
                RemoveCard(ref blueCards, player, move.card);
                blueCards.Add(flexCard);
                break;
        }
        flexCard = move.card;

        if (move.piece.x >= 0 && move.piece.y >= 0)
        {
            int p = boardState[move.piece.x, move.piece.y];
            boardState[move.piece.x, move.piece.y] = 0;
            boardState[move.moveTo.x, move.moveTo.y] = p;
        }

        if (visual) gameHistory.Add(GetGameState());

        CheckForWin();
        if (visual) ApplyToVisual(player == topOfBoard);
        
        SwitchPlayer();
    }

    void RemoveCard(ref List<MoveCard> cards, Player player, MoveCard cardToRemove)
    {
        if (!cards.Remove(cardToRemove))
            Debug.LogError(string.Format("Cannot remove card \'{0}\' from the {1} player!", cardToRemove.name, player.ToString()));
    }

    public void SwitchPlayer()
    {
        boardState = InvertBoard(boardState);
        activePlayer = (activePlayer == Player.Red) ? Player.Blue : Player.Red;
    }

    public bool IsBetween(int value, int min, int max)
    {
        return value >= min && value < max;
    }

    void ApplyToVisual(bool invert)
    {
        if (invert)
            boardVisual = InvertBoard(boardState);
        else
            boardVisual = CopyBoard(boardState);

        onUpdateVisual?.Invoke();
    }

    public GameState GetGameState()
    {
        GameState r = new GameState();
        
        r.boardState = CopyBoard(boardState);

        r.myCards = new List<MoveCard>();
        r.yourCards = new List<MoveCard>();

        if (activePlayer == Player.Red)
        {
            r.activePlayer = Player.Blue;
            r.myCards.Add(blueCards[0]);
            r.myCards.Add(blueCards[1]);
            r.yourCards.Add(redCards[0]);
            r.yourCards.Add(redCards[1]);
        } else {
            r.activePlayer = Player.Red;
            r.myCards.Add(redCards[0]);
            r.myCards.Add(redCards[1]);
            r.yourCards.Add(blueCards[0]);
            r.yourCards.Add(blueCards[1]);
        }

        r.flexCard = flexCard;

        return r;
    }

    void CheckForWin()
    {
        // If your master occupies the opponent master's start space
        if (boardState[2, 4] == 2)
            gameState = (activePlayer == Player.Red) ? GameStatus.RedWins : GameStatus.BlueWins;
        else
        {
            // If the opponent master is captured
            for (int i = 0; i < 5; i++)
                for (int j = 0; j < 5; j++)
                    if (boardState[i, j] == -2)
                        return;
            gameState = (activePlayer == Player.Red) ? GameStatus.RedWins : GameStatus.BlueWins;
        }
    }
}
