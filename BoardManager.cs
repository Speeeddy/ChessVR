using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using SimpleFirebaseUnity;
using UnityEngine.SceneManagement;

//WILL HAVE TO CLEAR DATABASE EVERY SINGLE DAY AS TIMESTAMP ARRANGED ACCORDING TO TIME SINCE START OF DAY
//TODO Add moving around chess board functionality
//TODO Unit promotion
//TODO 
public class BoardManager : MonoBehaviour
{

    public GameObject chessBoard;
    public GameObject[] whitePawn = new GameObject[8];
    public GameObject[] whiteRook = new GameObject[2];
    public GameObject[] whiteBishop = new GameObject[2];
    public GameObject[] whiteKnight = new GameObject[2];
    public GameObject whiteQueen;
    public GameObject whiteKing;
    public GameObject[] blackPawn = new GameObject[8];
    public GameObject[] blackRook = new GameObject[2];
    public GameObject[] blackBishop = new GameObject[2];
    public GameObject[] blackKnight = new GameObject[2];
    public GameObject blackQueen;
    public GameObject blackKing;
    private int moveNumber;
    private int IAmWhiteOrBlack = 0;
    private float latestTimeQuery = -1;
    private float  firebasePollLast = -1;
    public float POLLINGFREQUENCY = 3f;
    private bool fetchActive = false;
    private int turnPendingUpdation = 0;
    private Dictionary<GameObject, string> white = new Dictionary<GameObject, string>();
    private Dictionary<GameObject, string> black = new Dictionary<GameObject, string>();
    private Dictionary<GameObject, string> typeDic = new Dictionary<GameObject, string>();
    //this saves the locations of all units
    private Firebase firebase;
    private string queryResult = "";
    private bool movingAnimation = false;
    private string selectedCoordNew = "dead", pressedSquare = "dead";
    private GameObject glowCube, glowCubeSelected;
    private bool squareSelected = false;
    private int turn = 0;		//white moves first
    private bool turnOver = false;
    public GameObject turnIndicator;
    public Camera cam, notOurCam;
    public GvrReticlePointer g;
    private bool checkMateLock = false;

    private float gazeStartTime;
    private string gazeAt;
    public float GAZETIME = 1f;

    private bool blackCheck = false;
    private bool whiteCheck = false;
    private bool checkmate = false;

    private bool whiteKingMoved = false;
    private bool whiteRook0Moved = false;
    private bool whiteRook1Moved = false;

    private bool blackKingMoved = false;
    private bool blackRook0Moved = false;
    private bool blackRook1Moved = false;

    public float MOVEUPHEIGHT = 5f;

    private class KVP
    {
        public GameObject Key;
        public string Value;

        public KVP()
        {
            this.Key = null;
            this.Value = null;
        }
        public KVP(GameObject k, string v)
        {
            this.Key = k;
            this.Value = v;
        }

        public KVP(KVP copier)
        {
            this.Key = copier.Key;
            this.Value = copier.Value;
        }
    }

    private KVP lastMoveFrom = new KVP();
    private bool elPassantePossible = false;

    void ChessPiecesPositionInitialise()
    {
        white[whitePawn[0]] = "a2";
        white[whitePawn[1]] = "b2";
        white[whitePawn[2]] = "c2";
        white[whitePawn[3]] = "d2";
        white[whitePawn[4]] = "e2";
        white[whitePawn[5]] = "f2";
        white[whitePawn[6]] = "g2";
        white[whitePawn[7]] = "h2";
        white[whiteRook[0]] = "a1";
        white[whiteRook[1]] = "h1";
        white[whiteKnight[0]] = "b1";
        white[whiteKnight[1]] = "g1";
        white[whiteBishop[0]] = "c1";
        white[whiteBishop[1]] = "f1";
        white[whiteQueen] = "d1";
        white[whiteKing] = "e1";

        black[blackPawn[0]] = "a7";
        black[blackPawn[1]] = "b7";
        black[blackPawn[2]] = "c7";
        black[blackPawn[3]] = "d7";
        black[blackPawn[4]] = "e7";
        black[blackPawn[5]] = "f7";
        black[blackPawn[6]] = "g7";
        black[blackPawn[7]] = "h7";
        black[blackRook[0]] = "a8";
        black[blackRook[1]] = "h8";
        black[blackKnight[0]] = "b8";
        black[blackKnight[1]] = "g8";
        black[blackBishop[0]] = "c8";
        black[blackBishop[1]] = "f8";
        black[blackQueen] = "d8";
        black[blackKing] = "e8";

        typeDic[whitePawn[0]] = "WP";
        typeDic[whitePawn[1]] = "WP";
        typeDic[whitePawn[2]] = "WP";
        typeDic[whitePawn[3]] = "WP";
        typeDic[whitePawn[4]] = "WP";
        typeDic[whitePawn[5]] = "WP";
        typeDic[whitePawn[6]] = "WP";
        typeDic[whitePawn[7]] = "WP";
        typeDic[blackPawn[0]] = "BP";
        typeDic[blackPawn[1]] = "BP";
        typeDic[blackPawn[2]] = "BP";
        typeDic[blackPawn[3]] = "BP";
        typeDic[blackPawn[4]] = "BP";
        typeDic[blackPawn[5]] = "BP";
        typeDic[blackPawn[6]] = "BP";
        typeDic[blackPawn[7]] = "BP";

        typeDic[whiteRook[0]] = "R";
        typeDic[whiteRook[1]] = "R";
        typeDic[whiteKnight[0]] = "K";
        typeDic[whiteKnight[1]] = "K";
        typeDic[whiteBishop[0]] = "B";
        typeDic[whiteBishop[1]] = "B";
        typeDic[whiteQueen] = "Q";
        typeDic[whiteKing] = "King";

        typeDic[blackRook[0]] = "R";
        typeDic[blackRook[1]] = "R";
        typeDic[blackKnight[0]] = "K";
        typeDic[blackKnight[1]] = "K";
        typeDic[blackBishop[0]] = "B";
        typeDic[blackBishop[1]] = "B";
        typeDic[blackQueen] = "Q";
        typeDic[blackKing] = "King";

        lastMoveFrom.Key = null;
        lastMoveFrom.Value = null;
    }

    float parseXCoord(string chessCoord)
    {
        float retVal = 100;
        switch (chessCoord[0])
        {
            case 'a':
                retVal = -35f;
                break;
            case 'b':
                retVal = -25f;
                break;
            case 'c':
                retVal = -15f;
                break;
            case 'd':
                retVal = -5f;
                break;
            case 'e':
                retVal = 5f;
                break;
            case 'f':
                retVal = 15f;
                break;
            case 'g':
                retVal = 25f;
                break;
            case 'h':
                retVal = 35f;
                break;
            default:
                retVal = 100f;
                break;
        }

        return retVal;
    }

    float parseZCoord(string chessCoord)
    {
        float retVal = 100;
        switch (chessCoord[1])
        {
            case '1':
                retVal = -35f;
                break;
            case '2':
                retVal = -25f;
                break;
            case '3':
                retVal = -15f;
                break;
            case '4':
                retVal = -5f;
                break;
            case '5':
                retVal = 5f;
                break;
            case '6':
                retVal = 15f;
                break;
            case '7':
                retVal = 25f;
                break;
            case '8':
                retVal = 35f;
                break;
            default:
                retVal = 100f;
                break;
        }

        return retVal;
    }

    bool DrawChessPiecesOld()
    {

        foreach (KeyValuePair<GameObject, string> temp in white)
        {
            if (temp.Value == "dead")
            {
                //Destroy (temp.Key);
                continue;
            }
            else
            {
                string val = (string)(temp.Value);
                Vector3 old = temp.Key.transform.position;
                Vector3 pos = new Vector3(parseXCoord(val), old.y, parseZCoord(val));
                //Vector3 pos = new Vector3(parseXCoord(val), 0.02f, parseZCoord(val));
                ((GameObject)(temp.Key)).transform.position = pos;
            }
        }

        foreach (KeyValuePair<GameObject, string> temp in black)
        {
            if (temp.Value == "dead")
            {
                //Destroy (temp.Key);
                continue;
            }
            else
            {
                string val = (string)(temp.Value);
                Vector3 old = temp.Key.transform.position;
                Vector3 pos = new Vector3(parseXCoord(val), old.y, parseZCoord(val));
                ((GameObject)(temp.Key)).transform.position = pos;
            }
        }

        return true;
    }

    //changes y coord to 0.02
    bool DrawChessPieces()
    {

        foreach (KeyValuePair<GameObject, string> temp in white)
        {
            if (temp.Value == "dead")
            {
                //Destroy (temp.Key);
                continue;
            }
            else
            {
                string val = (string)(temp.Value);
                Vector3 old = temp.Key.transform.position;
                //Vector3 pos = new Vector3(parseXCoord(val), old.y, parseZCoord(val));
                Vector3 pos = new Vector3(parseXCoord(val), 0.02f, parseZCoord(val));
                ((GameObject)(temp.Key)).transform.position = pos;
            }
        }

        foreach (KeyValuePair<GameObject, string> temp in black)
        {
            if (temp.Value == "dead")
            {
                //Destroy (temp.Key);
                continue;
            }
            else
            {
                string val = (string)(temp.Value);
                Vector3 old = temp.Key.transform.position;
                Vector3 pos = new Vector3(parseXCoord(val), 0.02f, parseZCoord(val));
                ((GameObject)(temp.Key)).transform.position = pos;
            }
        }
        return true;
    }
    void DrawChessboard()
    {

        Vector3 boardPos = new Vector3(-4.34f * 8, 0.01f, -4.34f * 8);
        Vector3 boardScale = new Vector3(0.99f, 1f, 0.99f);
        //*8 because of scaling factor of the chess plane

        chessBoard.transform.position = boardPos;
        chessBoard.transform.localScale = boardScale;
    }

    void clearChessBoard()
    {
        foreach (var key in black.Keys.ToList())
        {
            black[key] = "dead";
        }

        foreach (var key in white.Keys.ToList())
        {
            white[key] = "dead";
        }
    }

    bool killPiece(int killBW, GameObject key)
    {
        Dictionary<GameObject, string> k;
        if (killBW == 1)
        {
            k = white;
        }
        else if (killBW == 2)
        {
            k = black;
        }
        else
        {
            k = null;
        }

        try
        {
            if (key == whiteKing || key == blackKing)
            {
                //prohibit
                Debug.Log("King killing not allowed!");
                return false;
            }
            k[key] = "dead";
            Debug.Log("Killed a unit!");
        }
        catch (Exception e)
        {
            Debug.Log("error: " + e.StackTrace);
            return false;
        }

        return true;
    }

    string convertIntToPos(int posInt)
    {
        int letter = posInt / 10, numeral = posInt % 10;
        char l;
        switch (letter)
        {
            case 1:
                l = 'a';
                break;
            case 2:
                l = 'b';
                break;
            case 3:
                l = 'c';
                break;
            case 4:
                l = 'd';
                break;
            case 5:
                l = 'e';
                break;
            case 6:
                l = 'f';
                break;
            case 7:
                l = 'g';
                break;
            case 8:
                l = 'h';
                break;
            default:
                l = 'x';
                break;
        }

        return string.Concat(l, numeral.ToString());

    }

    int convertPosToInt(string pos)
    {
        if (pos == "dead")
            return -1;

        char l = pos[0], n = pos[1];
        if (l == 'x' || n == 'x')
            return 0;
        int ans;
        ans = int.Parse(n.ToString());
        switch (l)
        {
            case 'h':
                ans += 80;
                break;
            case 'g':
                ans += 70;
                break;
            case 'f':
                ans += 60;
                break;
            case 'e':
                ans += 50;
                break;
            case 'd':
                ans += 40;
                break;
            case 'c':
                ans += 30;
                break;
            case 'b':
                ans += 20;
                break;
            case 'a':
                ans += 10;
                break;
            //should work
            default:
                ans = 0;
                break;
        }
        return ans;
    }

    HashSet<int> validMovesStraight(string coord)
    {
        if (coord == "dead")
            return null;
        int oldInt = convertPosToInt(coord);
        HashSet<int> validMoves = new HashSet<int>();

        int pos;

        pos = oldInt + 1;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos++;
        }

        pos = oldInt - 1;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos--;
        }

        pos = oldInt + 10;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos += 10;
        }

        pos = oldInt - 10;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos -= 10;
        }

        return validMoves;
    }

    //for straight moves, like a rook
    bool validStraight(string oldPos, string newPos)
    {
        if (oldPos == "dead")
            return false;
        bool ans;
        int newInt = convertPosToInt(newPos);

        HashSet<int> validMoves = validMovesStraight(oldPos);

        if (validMoves.Contains(newInt))
        {
            ans = true;
        }
        else
        {
            ans = false;
        }

        return ans;
    }

    HashSet<int> validMovesDiagonal(string coord)
    {
        if (coord == "dead")
            return null;
        int oldInt = convertPosToInt(coord);
        HashSet<int> validMoves = new HashSet<int>();

        int pos;

        pos = oldInt + 11;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos += 11;
        }

        pos = oldInt - 11;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos -= 11;
        }

        pos = oldInt - 9;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos -= 9;
        }

        pos = oldInt + 9;
        while (intInChessBoard(pos))
        {
            validMoves.Add(pos);
            GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keywhite != null)
                break;
            GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(pos)).Key);
            if (keyblack != null)
                break;
            pos += 9;
        }

        return validMoves;
    }
    //for diagonal moves, like bishop
    bool validDiagonal(string oldPos, string newPos)
    {
        if (oldPos == "dead")
            return false;
        bool ans;
        int newInt = convertPosToInt(newPos);

        HashSet<int> validMoves = validMovesDiagonal(oldPos);
        if (validMoves.Contains(newInt))
        {
            ans = true;
        }
        else
        {
            ans = false;
        }

        return ans;
    }

    bool intInChessBoard(int p)
    {
        if (p % 10 >= 1 && p % 10 <= 8 && p / 10 >= 1 && p / 10 <= 8)
            return true;
        return false;
    }

    HashSet<int> validMovesKing(string coord, bool ignoreKingCastle = false)
    {
        //Debug.Log ("Finding valid moves for king " + coord);
        if (coord == "dead")
            return null;
        int oldInt = convertPosToInt(coord);
        HashSet<int> validMoves = new HashSet<int>();

        if (intInChessBoard(oldInt + 1))
            validMoves.Add(oldInt + 1);
        if (intInChessBoard(oldInt - 1))
            validMoves.Add(oldInt - 1);
        if (intInChessBoard(oldInt + 10))
            validMoves.Add(oldInt + 10);
        if (intInChessBoard(oldInt - 10))
            validMoves.Add(oldInt - 10);
        if (intInChessBoard(oldInt + 11))
            validMoves.Add(oldInt + 11);
        if (intInChessBoard(oldInt - 11))
            validMoves.Add(oldInt - 11);
        if (intInChessBoard(oldInt + 9))
            validMoves.Add(oldInt + 9);
        if (intInChessBoard(oldInt - 9))
            validMoves.Add(oldInt - 9);

        //castling
        GameObject keywhite = (GameObject)(white.FirstOrDefault(x => x.Value == coord).Key);
        GameObject keyblack = (GameObject)(black.FirstOrDefault(x => x.Value == coord).Key);

        //multiple redundant checks
        if (keywhite == whiteKing && whiteKingMoved == false && coord == "e1" && ignoreKingCastle == false)
        {
            if (whiteRook0Moved == false)
            {
                GameObject k1;
                k1 = (GameObject)(white.FirstOrDefault(x => x.Value == "a1").Key);
                //checks for check on castling positions
                if (k1 == whiteRook[0] && validStraight("a1", "e1") == true && EvalCheckOnPosition(1, "e1") == false &&
                    EvalCheckOnPosition(1, "d1") == false && EvalCheckOnPosition(1, "c1") == false)
                    validMoves.Add(31); //c1
            }
            if (whiteRook1Moved == false)
            {
                GameObject k1;
                k1 = (GameObject)(white.FirstOrDefault(x => x.Value == "h1").Key);
                //checks for check on castling positions
                if (k1 == whiteRook[1] && validStraight("h1", "e1") == true && EvalCheckOnPosition(1, "e1") == false &&
                    EvalCheckOnPosition(1, "f1") == false && EvalCheckOnPosition(1, "g1") == false)
                    validMoves.Add(71); //g1	
            }
        }
        else if (keyblack == blackKing && blackKingMoved == false && coord == "e8" && ignoreKingCastle == false)
        {
            if (blackRook0Moved == false)
            {
                GameObject k1;
                k1 = (GameObject)(black.FirstOrDefault(x => x.Value == "a8").Key);
                if (k1 == blackRook[0] && validStraight("a8", "e8") == true && EvalCheckOnPosition(2, "e8") == false &&
                    EvalCheckOnPosition(2, "d8") == false && EvalCheckOnPosition(2, "c8") == false)
                    validMoves.Add(38); //c8
            }
            if (blackRook1Moved == false)
            {
                GameObject k1;
                k1 = (GameObject)(black.FirstOrDefault(x => x.Value == "h8").Key);
                if (k1 == blackRook[1] && validStraight("h8", "e8") == true && EvalCheckOnPosition(2, "e8") == false &&
                    EvalCheckOnPosition(2, "f8") == false && EvalCheckOnPosition(2, "g8") == false)
                    validMoves.Add(78); //g8
            }
        }
        return validMoves;
    }

    bool validKingMove(string oldPos, string newPos, bool ignoreKingCastle = false)
    {
        if (oldPos == "dead")
            return false;
        bool ans;

        int newInt = convertPosToInt(newPos);

        HashSet<int> validMoves = validMovesKing(oldPos, ignoreKingCastle);
        if (validMoves.Contains(newInt))
            ans = true;
        else
            ans = false;

        return ans;
    }

    HashSet<int> validMovesKnight(string coord)
    {
        if (coord == "dead")
            return null;
        int oldInt = convertPosToInt(coord);
        HashSet<int> validMoves = new HashSet<int>();

        if (intInChessBoard(oldInt - 21))
            validMoves.Add(oldInt - 21);
        if (intInChessBoard(oldInt - 19))
            validMoves.Add(oldInt - 19);
        if (intInChessBoard(oldInt - 12))
            validMoves.Add(oldInt - 12);
        if (intInChessBoard(oldInt - 8))
            validMoves.Add(oldInt - 8);
        if (intInChessBoard(oldInt + 8))
            validMoves.Add(oldInt + 8);
        if (intInChessBoard(oldInt + 12))
            validMoves.Add(oldInt + 12);
        if (intInChessBoard(oldInt + 19))
            validMoves.Add(oldInt + 19);
        if (intInChessBoard(oldInt + 21))
            validMoves.Add(oldInt + 21);

        return validMoves;
    }

    bool validKnightMove(string oldPos, string newPos)
    {
        if (oldPos == "dead")
            return false;
        bool ans;
        int newInt = convertPosToInt(newPos);

        HashSet<int> validMoves = validMovesKnight(oldPos);

        if (validMoves.Contains(newInt))
            ans = true;
        else
            ans = false;

        return ans;
    }

    HashSet<int> validMovesBlackPawn(string coord)
    {
        if (coord == "dead")
            return null;
        //Debug.Log ("validMovesBlackPawn with coord " + coord);
        int oldInt = convertPosToInt(coord);
        HashSet<int> validMoves = new HashSet<int>();

        //two space move
        if (oldInt % 10 == 7)
        {
            if (intInChessBoard(oldInt - 2))
            {   //move only if space empty
                GameObject keyopp = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(oldInt - 2)).Key);
                if (keyopp == null)
                    validMoves.Add(oldInt - 2);
            }
        }

        //el passante
        if (oldInt % 10 == 4)
        {
            GameObject t = lastMoveFrom.Key;
            if (t == whitePawn[0] || t == whitePawn[1] || t == whitePawn[2] || t == whitePawn[3] || t == whitePawn[4] || t == whitePawn[5] || t == whitePawn[6] || t == whitePawn[7])
            {//last moved was a pawn
                if (convertPosToInt(lastMoveFrom.Value) % 10 == 2)
                {//that pawn was moved from its base row
                    GameObject pleft = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(oldInt - 10)).Key);
                    //piece on left of pawn
                    GameObject pright = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(oldInt + 10)).Key);
                    //pawn on right of pawn
                    if (pleft == t)
                    {   //if left piece is the same pawn moved in the last move (moved from base row)
                        validMoves.Add(oldInt - 11);
                        elPassantePossible = true;
                    }
                    else if (pright == t)
                    {       //right
                        validMoves.Add(oldInt + 9);
                        elPassantePossible = true;
                    } //else 
                      //{
                      //	Debug.Log ("pleft pright failed.");
                      //}
                }
            }
        }

        //regular one move
        if (intInChessBoard(oldInt - 1))
        {
            //move only if space empty
            GameObject keyopp = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(oldInt - 1)).Key);
            if (keyopp == null)
                validMoves.Add(oldInt - 1);
        }
        //kill diagonal move
        if (intInChessBoard(oldInt + 9))
        {
            GameObject keyopp = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(oldInt + 9)).Key);
            if (keyopp != null)
                validMoves.Add(oldInt + 9);
        }

        if (intInChessBoard(oldInt - 11))
        {
            GameObject keyopp = (GameObject)(white.FirstOrDefault(x => x.Value == convertIntToPos(oldInt - 11)).Key);
            if (keyopp != null)
                validMoves.Add(oldInt - 11);
        }
        return validMoves;
    }

    bool validBlackPawnMove(string oldPos, string newPos)
    {
        if (oldPos == "dead")
            return false;
        bool ans = false;
        int newInt = convertPosToInt(newPos);

        HashSet<int> validMoves = validMovesBlackPawn(oldPos);
        if (validMoves.Contains(newInt))
            ans = true;
        else
            ans = false;

        return ans;
    }

    HashSet<int> validMovesWhitePawn(string coord)
    {
        if (coord == "dead")
            return null;
        //Debug.Log ("validMovesWhitePawn with coord " + coord);
        int oldInt = convertPosToInt(coord);
        HashSet<int> validMoves = new HashSet<int>();

        //first move with two spaces
        if (oldInt % 10 == 2)
        {
            if (intInChessBoard(oldInt + 2))
            {
                //move only if space empty
                GameObject keyopp = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(oldInt + 2)).Key);
                if (keyopp == null)
                    validMoves.Add(oldInt + 2);
            }
        }

        //el passante
        if (oldInt % 10 == 5)
        {
            GameObject t = lastMoveFrom.Key;
            if (t == blackPawn[0] || t == blackPawn[1] || t == blackPawn[2] || t == blackPawn[3] || t == blackPawn[4] || t == blackPawn[5] || t == blackPawn[6] || t == blackPawn[7])
            {//last moved was a pawn
                if (convertPosToInt(lastMoveFrom.Value) % 10 == 7)
                {//that pawn was moved from its base row
                    GameObject pleft = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(oldInt - 10)).Key);
                    //piece on left of pawn
                    GameObject pright = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(oldInt + 10)).Key);
                    //pawn on right of pawn
                    if (pleft == t)
                    {   //if left piece is the same pawn moved in the last move (moved from base row)
                        validMoves.Add(oldInt - 9);
                        elPassantePossible = true;
                    }
                    else if (pright == t)
                    {       //right
                        validMoves.Add(oldInt + 11);
                        elPassantePossible = true;
                    }
                }
            }
        }

        //regular one move
        if (intInChessBoard(oldInt + 1))
        {
            //move only if space empty
            GameObject keyopp = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(oldInt + 1)).Key);
            if (keyopp == null)
                validMoves.Add(oldInt + 1);
        }
        //kill diagonal move
        if (intInChessBoard(oldInt - 9))
        {
            GameObject keyopp = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(oldInt - 9)).Key);
            if (keyopp != null)
                validMoves.Add(oldInt - 9);
        }

        if (intInChessBoard(oldInt + 11))
        {
            GameObject keyopp = (GameObject)(black.FirstOrDefault(x => x.Value == convertIntToPos(oldInt + 11)).Key);
            if (keyopp != null)
                validMoves.Add(oldInt + 11);
        }

        return validMoves;
    }

    bool validWhitePawnMove(string oldPos, string newPos)
    {
        if (oldPos == "dead")
            return false;
        bool ans = false;
        int newInt = convertPosToInt(newPos);

        HashSet<int> validMoves = validMovesWhitePawn(oldPos);
        if (validMoves.Contains(newInt))
            ans = true;
        else
            ans = false;

        return ans;
    }

    bool moveValidOrNot(GameObject k, string oldPos, string newPos, bool ignoreKingCastle = false)
    {
        //Debug.Log ("Inside moveValidOrNot for the piece " + k.ToString() + " pos " + oldPos + " to " + newPos);
        bool ans = false;
        try
        {
            string type = typeDic[k];
            switch (type)
            {
                case "R":
                    ans = validStraight(oldPos, newPos);
                    break;
                case "B":
                    ans = validDiagonal(oldPos, newPos);
                    break;
                case "Q":
                    ans = validDiagonal(oldPos, newPos) || validStraight(oldPos, newPos);
                    break;
                case "King":
                    ans = validKingMove(oldPos, newPos, ignoreKingCastle);
                    break;
                case "K":
                    //Debug.Log("Knight selected at " + oldPos);
                    ans = validKnightMove(oldPos, newPos);
                    break;
                case "BP":
                    ans = validBlackPawnMove(oldPos, newPos);
                    break;
                case "WP":
                    ans = validWhitePawnMove(oldPos, newPos);
                    break;
                default:
                    ans = false;        //CHANGED
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.Log("error at move valid: " + e.StackTrace);
            return false;
        }

        return ans;

    }

    bool moveChessPiece(int turnBW, string oldPos, string newPos, bool actuallyMove = true)
    {
        //checking is for when evaluating check mate to prevent recursive calls
        //Debug.Log("Got request to try move " + oldPos + " to " + newPos);
        if (oldPos == "dead")
            return false;

        //Debug.Log ("Checking = " + checking.ToString ());
        elPassantePossible = false;
        Dictionary<GameObject, string> dsel, dopp;
        int turnOpp = 0;
        //bool check = false;

        //complete this
        if (turnBW == 1)
        {
            dsel = white;
            dopp = black;
            turnOpp = 2;
            //if (checkMateLock == false)
            //	EvalCheckWhite();		//trying
            //	check = whiteCheck;
        }
        else if (turnBW == 2)
        {
            dsel = black;
            dopp = white;
            turnOpp = 1;
            //if (checkMateLock == false)			//this is for if king already under check, don't allow random piece movement
            //	EvalCheckBlack();
            //	check = blackCheck;
        }
        else
        {
            dsel = null;
            dopp = null;
            //	check = false;
        }
        //1 implies white turn, 2 implies black. Can move only those pieces whose turn it is.

        GameObject keysel = (GameObject)(dsel.FirstOrDefault(x => x.Value == oldPos).Key);
        GameObject keyselfriendly = (GameObject)(dsel.FirstOrDefault(x => x.Value == newPos).Key);
        GameObject keyopp = (GameObject)(dopp.FirstOrDefault(x => x.Value == newPos).Key);
        //checks for element at the old position at selected dict, newpos in opposite dictionary

        bool allowMove = true;

        //if (checking == 0 || checking == 1)
        //Debug.Log ("1");
        try
        {
            Dictionary<GameObject, string> backupDicWhite = new Dictionary<GameObject, string>(white);
            Dictionary<GameObject, string> backupDicBlack = new Dictionary<GameObject, string>(black);

            if (keysel == null)
            {
                //Debug.Log("No piece at this position!");
                //if (checking == 0 || checking == 1)
                //		Debug.Log ("2, " + oldPos + " " + newPos);
                allowMove = false;
            }
            else
            {
                //if (checking == 0 || checking == 1)
                //		Debug.Log ("3");
                if (moveValidOrNot(keysel, oldPos, newPos))
                {
                    //Some key present at the selected square
                    //if (checking == 0 || checking == 1)
                    //			Debug.Log ("4");
                    if (keyopp != null)
                    //element exists in the opposite dictionary at newPos
                    {
                        allowMove = killPiece(turnOpp, keyopp);
                        //kill piece
                    }
                    else if (keyselfriendly != null)
                    {
                        allowMove = false;
                        //Debug.Log("Friendly fire not allowed!");
                    }
                }
                else
                    allowMove = false;
                //true means move successful, false means failed
            }

            if (allowMove)
            {
                //if (checking == 0 || checking == 1)
                //		Debug.Log ("5");
                bool whiteKingMoveBackup = whiteKingMoved;
                bool blackKingMoveBackup = blackKingMoved;
                bool whiteRook0MovedBackup = whiteRook0Moved;
                bool whiteRook1MovedBackup = whiteRook1Moved;
                bool blackRook0MovedBackup = blackRook0Moved;
                bool blackRook1MovedBackup = blackRook1Moved;

                dsel[keysel] = newPos;
                //if (actuallyMove)
                //    MoveAnimator(keysel, oldPos, newPos);
                /*
				//this is just for debugging one sequence, REMOVE
				if (keysel == whiteKnight[0] || keysel == whiteKnight[1])
				{
					DrawChessPieces();
					return allowMove;
				}
				*/
                if (keysel == whiteRook[0])
                    whiteRook0Moved = true;
                if (keysel == whiteRook[1])
                    whiteRook1Moved = true;
                if (keysel == whiteKing)
                {
                    whiteKingMoved = true;
                    //castling, move rook. Conditions checked in validKingMove
                    if (oldPos == "e1" && newPos == "c1")
                    {
                        white[whiteRook[0]] = "d1";
                        whiteRook0Moved = true;
                    }
                    else if (oldPos == "e1" && newPos == "g1")
                    {
                        white[whiteRook[1]] = "f1";
                        whiteRook1Moved = true;
                    }
                }
                if (keysel == blackRook[0])
                    blackRook0Moved = true;
                if (keysel == blackRook[1])
                    blackRook1Moved = true;
                if (keysel == blackKing)
                {
                    blackKingMoved = true;
                    //castling, move rook. Conditions checked in validKingMove
                    if (oldPos == "e8" && newPos == "c8")
                    {
                        black[blackRook[0]] = "d8";
                        blackRook0Moved = true;
                    }
                    else if (oldPos == "e8" && newPos == "g8")
                    {
                        black[blackRook[1]] = "f8";
                        blackRook1Moved = true;
                    }
                }

                if (elPassantePossible == true)
                {
                    //Debug.Log("El Passante possibility!");
                    if (turnBW == 1 && convertPosToInt(newPos) == convertPosToInt(lastMoveFrom.Value) - 1)      //white turn
                        killPiece(turnOpp, lastMoveFrom.Key);
                    else if (turnBW == 2 && convertPosToInt(newPos) == convertPosToInt(lastMoveFrom.Value) + 1)
                        killPiece(turnOpp, lastMoveFrom.Key);
                }

                //if (checking == 0 || checking == 1)
                //		Debug.Log ("6");
                if (turnBW == 1)
                {
                    //if (checking == 0 || checking == 1)
                    //			Debug.Log ("7");
                    EvalCheckWhite();       //try
                                            //this is just for debugging one sequence, REMOVE
                                            /*if (keysel == whiteKnight[0] || keysel == whiteKnight[1])
                                            {
                                                DrawChessPieces();
                                                return allowMove;
                                            }*/
                    if (whiteCheck)
                    {
                        Debug.Log("This move results in check to own white king, backtracking changes (not allowed)");
                        //if (checking == 0 || checking == 1)
                        //				Debug.Log ("8");

                        //white.Clear();
                        //black.Clear();
                        white = new Dictionary<GameObject, string>(backupDicWhite);
                        black = new Dictionary<GameObject, string>(backupDicBlack);
                        whiteKingMoved = whiteKingMoveBackup;
                        blackKingMoved = blackKingMoveBackup;
                        whiteRook0Moved = whiteRook0MovedBackup;
                        whiteRook1Moved = whiteRook1MovedBackup;
                        blackRook0Moved = blackRook0MovedBackup;
                        blackRook1Moved = blackRook1MovedBackup;

                        //DrawChessPieces ();       //COMMENTED
                        EvalCheckWhite();
                        allowMove = false;
                    }
                }
                else if (turnBW == 2)
                {
                    //if (checking == 0 || checking == 1)
                    //			Debug.Log ("9");
                    EvalCheckBlack();       //try
                    if (blackCheck)
                    {
                        //if (checking == 0 || checking == 1)
                        //				Debug.Log ("10");
                        Debug.Log("This move results in check to own black king, backtracking changes (not allowed)");

                        //white.Clear();
                        //black.Clear();
                        white = new Dictionary<GameObject, string>(backupDicWhite);
                        black = new Dictionary<GameObject, string>(backupDicBlack);
                        whiteKingMoved = whiteKingMoveBackup;
                        blackKingMoved = blackKingMoveBackup;
                        whiteRook0Moved = whiteRook0MovedBackup;
                        whiteRook1Moved = whiteRook1MovedBackup;
                        blackRook0Moved = blackRook0MovedBackup;
                        blackRook1Moved = blackRook1MovedBackup;

                        //DrawChessPieces ();   //COMMENTED
                        EvalCheckBlack();
                        allowMove = false;
                    }
                }

                if (turnBW == 1 && allowMove)
                {
                    Debug.Log("White: " + oldPos + " to " + newPos);
                    lastMoveFrom.Key = keysel;
                    lastMoveFrom.Value = oldPos;
                }
                else if (turnBW == 2 && allowMove)
                {
                    Debug.Log("Black: " + oldPos + " to " + newPos);
                    lastMoveFrom.Key = keysel;
                    lastMoveFrom.Value = oldPos;
                }

                /*foreach (KeyValuePair<GameObject, string> temp in black)
                {
                    if (temp.Key!=null) 
                    {
                        Debug.Log(temp.Key.ToString() + " " + temp.Value);
                    }
                }*/


                foreach (KeyValuePair<GameObject, string> temp in white)
                {
                    if (temp.Value == "dead" && temp.Key != null && checkMateLock == false)
                    {
                        Debug.Log(temp.Key.ToString() + " destroyed");
                        Destroy(temp.Key);
                    }
                }
                foreach (KeyValuePair<GameObject, string> temp in black)
                {
                    if (temp.Value == "dead" && temp.Key != null && checkMateLock == false)
                    {
                        Debug.Log(temp.Key.ToString() + " destroyed");
                        Destroy(temp.Key);
                    }
                }

                if (checkMateLock == false)
                {
                    EvalCheckBlack(true);
                    EvalCheckWhite(true);
                }

            }
            //if (checking == 0 || checking == 1)
            //	Debug.Log ("11");

            //Debug.Log("The last move here is " + lastMoveFrom.Key.ToString() + " from " + lastMoveFrom.Value + " and elpassante variable is " + elPassantePossible.ToString());
        }
        catch (Exception e)
        {
            Debug.Log("error at move: " + e.StackTrace);
            return false;
            //error caught if element not found
        }
        //clearChessBoard ();
        //DrawChessPieces ();
        if (allowMove && actuallyMove)
        {
            MoveAnimator(keysel, oldPos, newPos);
            Debug.Log("Move of " + keysel.ToString() + " from " + oldPos + " to " + newPos + " approved");
            if (turn == IAmWhiteOrBlack)
                UpdateToFirebase(oldPos, newPos);
        }
        return allowMove;

    }


    void Start()
    {
        Debug.Log("Started");
        firebase = Firebase.CreateNew("chessvr-sdpd.firebaseio.com");
        firebase.OnGetSuccess += GetOKHandler;
        firebase.OnGetFailed += GetFailHandler;
        moveNumber = -1;

        if (String.Compare(SceneManager.GetActiveScene().name, "GameSceneWhite") == 0)
        {
            IAmWhiteOrBlack = 1;
            Debug.Log("I am white");
            turnIndicator.transform.localRotation = new Quaternion(0f, 0f, 0f, 0f);
        }
        else if (String.Compare(SceneManager.GetActiveScene().name, "GameSceneBlack") == 0)
        {
            IAmWhiteOrBlack = 2;
            turnIndicator.transform.localRotation = new Quaternion(0f, 180f, 0f, 0f);
            Debug.Log("I am black");
        }

        //turnIndicator.transform.localRotation = new Quaternion(0f, 180f, 0f, 0f);

        //getDataFromFB();
        DrawChessboard();
        ChessPiecesPositionInitialise();
        DrawChessPieces();
        glowCube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowCubeSelected = GameObject.CreatePrimitive(PrimitiveType.Cube);
        glowCubeSelected.GetComponent<Renderer>().material.color = getColor(153, 101, 21, 255);
        //Debug.Log(getColor(218, 165, 32, 1).ToString());
        glowCubeSelected.transform.localScale = new Vector3(10, 1, 10);
        glowCubeSelected.transform.position = new Vector3(0f, 0.02f, 0f);
        glowCubeSelected.SetActive(false);
        //glowCubeSelected.GetComponent<Material>().color = 
        Camera.main.enabled = false;
        notOurCam.enabled = false;
        cam.enabled = true;
        gazeStartTime = -1f;
        gazeAt = null;
    }

    private void LateUpdate()
    {
        //Debug.Log("CamWhite" + camWhite.transform.position.ToString());
        //incomplete
        /*if (camWhite.enabled)
            Debug.Log("CamWhite" + camWhite.transform.position.ToString());
        if (camBlack.enabled)
            Debug.Log("CamBlack");
        */
        //moveCam();

        if (turn == 3)
        {
            //Debug.Log("wtf");
            return;
        }
        if (movingAnimation == true)// && selectedCoordNew != "dead")
        {
            //Debug.Log("moving");
            GameObject keysel = null;
            try
            {

                //GameObject keysel = null;
                if (turn == IAmWhiteOrBlack)
                {
                    if (turn == 2)      //update selection changes the turn, and selectedCoordNew is the new pos
                        keysel = (GameObject)(black.FirstOrDefault(x => x.Value == selectedCoordNew).Key);
                    else if (turn == 1)
                        keysel = (GameObject)(white.FirstOrDefault(x => x.Value == selectedCoordNew).Key);
                }
                else
                {
                    if (turn == 2)      //update selection changes the turn, and selectedCoordNew is the new pos
                        keysel = (GameObject)(white.FirstOrDefault(x => x.Value == selectedCoordNew).Key);
                    else if (turn == 1)
                        keysel = (GameObject)(black.FirstOrDefault(x => x.Value == selectedCoordNew).Key);

                }
                if (keysel == null)
                    Debug.Log("Keysel is null???");
                MoveAnimator(keysel, lastMoveFrom.Value, selectedCoordNew);
            }
            catch (Exception e)
            {
                Debug.Log("Exception in moving the piece: " + " moves from " + lastMoveFrom.Value + " to " + selectedCoordNew + "exception=" + e.StackTrace.ToString());
                movingAnimation = false;
                DrawChessPieces();
            }
        }
        else if (UpdateSelection())
        {
            Debug.Log("Update selection returned true");
            //forceTurnOver = false;
            turnOver = false;
            glowCubeSelected.SetActive(false);
            if (turn == 1)
            {
                turn = 2;
                //Debug.Log ("WhiteCheck = " + whiteCheck.ToString () + " and BlackCheck = " + blackCheck.ToString ());
                updateTextBox();

                //camWhite.enabled = false;
                //camBlack.enabled = true;
            }
            else if (turn == 2)     //loop probably doesnt go here in white game and vice versa
            {
                turn = 1;
                updateTextBox();
                //Camera.main.enabled = false;
                //camBlack.enabled = false;
                //camWhite.enabled = true;
            }
            else
            {
                turn = 0;
                Debug.Log("Game over");
                //camWhite.enabled = false;
                //camBlack.enabled = false;
                Camera.main.enabled = true;
            }
            //DrawChessPiecesOld();

        }
        //else
        //   Debug.Log("wtfx2");
        //Debug.Log ("Update called.");
    }

    public void updateTextBox()
    {
        if (turn == 1)
        {
            turnIndicator.GetComponent<TextMesh>().text = "Player 2";
            if (whiteCheck)
                turnIndicator.GetComponent<TextMesh>().text = "Check on white!";
            if (blackCheck)
                turnIndicator.GetComponent<TextMesh>().text = "Check on black!";
        }
        else if (turn == 2)
        {
            turnIndicator.GetComponent<TextMesh>().text = "Player 1";
            //Debug.Log ("WhiteCheck = " + whiteCheck.ToString () + " and BlackCheck = " + blackCheck.ToString ());
            if (whiteCheck)
                turnIndicator.GetComponent<TextMesh>().text = "Check on white!";
            if (blackCheck)
                turnIndicator.GetComponent<TextMesh>().text = "Check on black!";
        }
    }

   /* public void moveCam()
    {
        g.overridePointerCamera = cam;
        g.transform.position = cam.transform.position;
        g.transform.rotation = cam.transform.rotation;
        var boo = g.GetComponent<GvrPointerPhysicsRaycaster>().GetLastRay().ray;
        Vector3 o = cam.transform.position;

        boo.origin = boo.GetPoint(400);
        boo.direction = -boo.direction;


        RaycastHit hit;
        //var x = currCam.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(boo, out hit))
        {
            Debug.Log("I hit " + hit.point.ToString());
            float x = cam.transform.position.x;
            float y = cam.transform.position.y;
            float z = cam.transform.position.z;
            cam.transform.position = new Vector3(o.x + ((x-o.x)/10), o.y + ((y - o.y) / 10), o.z + ((z - o.z) / 10));
            Debug.Log("New pos: " + (o.x + (x - o.x) / 10).ToString()+ (o.y + (y - o.y) / 10).ToString()+ (o.z + (z - o.z) / 10).ToString());
        }
    }*/

    public bool UpdateSelection()
    {
        //Debug.Log(turn.ToString());
        //Debug.Log("Here");
        if (turnOver == false)
        {
            //Debug.Log("Here");
            if (checkmate == true)
            {
                if (whiteCheck == true)
                {
                    Debug.Log("Black wins!");
                    turnIndicator.GetComponent<TextMesh>().text = "Black wins!";
                    turn = 3;
                    return false;
                }
                else if (blackCheck == true)
                {
                    Debug.Log("White wins!");
                    turnIndicator.GetComponent<TextMesh>().text = "White wins!";
                    turn = 3;
                    return false;
                }
                else
                {
                    Debug.Log("Some checkmate error occurred");
                    return false;
                }
            }
            //incomplete
            //if (!Camera.main)
            //	return false;
            //Debug.Log("Hello"); 
            //BLACK IS NOT MOVING
            //Camera currCam;
            var x = new Ray();
            if (g == null)
            {
                Debug.Log("Reticles null");
                return false;
            }

            //if (camWhite.enabled == true)
            //{
            //currCam = camWhite;

     
            g.overridePointerCamera = cam;
            g.transform.position = cam.transform.position;
            g.transform.rotation = cam.transform.rotation;
            x = g.GetComponent<GvrPointerPhysicsRaycaster>().GetLastRay().ray;
            //}
            /*else if (camBlack.enabled == true)
            {
                currCam = camBlack;
                g.overridePointerCamera = camBlack;
                g.transform.position = camBlack.transform.position;
                g.transform.rotation = camBlack.transform.rotation;
                x = g.GetComponent<GvrPointerPhysicsRaycaster>().GetLastRay().ray;
            }
            else
            {
                Debug.Log("Some camera error");
                currCam = Camera.main;
            }*/
            //var x = currCam.GetComponentInChildren<GvrPointerPhysicsRaycaster>().GetLastRay().ray;
            RaycastHit hit;
            //var x = currCam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(x, out hit))
            {

                //Debug.Log("I hit " + hit.point.ToString());   
                if (turn == IAmWhiteOrBlack)
                {
                    Debug.Log("My move! Turn is " + turn.ToString());
                    selectedCoordNew = ReverseParseCoord(hit.point);
                    glowCoord(selectedCoordNew, Color.red);		//for debugging, glows the hovered coordinate
                    if (selectedCoordNew != "dead" && selectedCoordNew != "xx")
                    {
                        //if (turn == 1)
                        glowCoord(selectedCoordNew, Color.blue);
                        //else if (turn == 2)
                        //    glowCoord(selectedCoordNew, Color.green);
                    }
                    if (selectedCoordNew != gazeAt)
                    {
                        if (gazeAt != null)
                            glowCoord(gazeAt, Color.clear);
                        gazeAt = selectedCoordNew;
                        gazeStartTime = Time.time;
                    }
                }
                else
                {
                    selectedCoordNew = "dead";
                }
            }
            else
            {
                selectedCoordNew = "dead";
            }

            float tempTime = Time.time;
            if (turn == IAmWhiteOrBlack)
            {
                if (Input.GetMouseButtonDown(0) || (gazeStartTime > 0 && (Time.time - gazeStartTime) > GAZETIME))
                {
                    Debug.Log(selectedCoordNew + " selected");
                    glowCubeSelected.transform.position = new Vector3(parseXCoord(selectedCoordNew), 0.02f, parseZCoord(selectedCoordNew));
                    glowCubeSelected.SetActive(true);
                    //Debug.Log ("selectedCoordold=" + selectedCoordNew + ", selectedCoordNew=" + selectedCoordNew + ", pressedSquare=" + pressedSquare + ", checkMateLock=" + checkMateLock.ToString() + ", blackCheck=" + blackCheck.ToString() + ", whiteCheck=" + whiteCheck.ToString() + ", checkmate=" + checkmate.ToString());
                    gazeStartTime = -1f;
                    glowCoord(selectedCoordNew, Color.blue);
                    
                    if (squareSelected == false)
                    {
                        if (PieceAtSquare(turn, selectedCoordNew))
                        {   //replace 1 with turn variable
                            squareSelected = true;
                            pressedSquare = selectedCoordNew;
                        }
                    }
                    else
                    {
                        //Debug.Log ("Attempting moving to " + selectedCoordNew + " from " + pressedSquare);
                        if (selectedCoordNew != pressedSquare)
                        {
                            if (moveChessPiece(turn, pressedSquare, selectedCoordNew))
                            {
                                squareSelected = false;
                                pressedSquare = "dead";
                                glowCoord(selectedCoordNew, Color.clear);
                                turnOver = true;
                            }
                            else
                            {
                                //GameObject keysel = (GameObject)(white.FirstOrDefault(y => y.Value == selectedCoordOld).Key);
                                //if (keysel != null)
                                //StartCoroutine(MoveDown(VERTICAL_TIME, keysel));
                                pressedSquare = selectedCoordNew;
                                if (turn == 1)
                                    glowCoord(selectedCoordNew, Color.blue);
                                else if (turn == 2)
                                    glowCoord(selectedCoordNew, Color.green);
                            }
                        }
                        //else
                        //{
                        //   DrawChessPiecesOld();
                        //}
                    }

                }

                //selectedCoordOld = selectedCoordNew;
                /*if (turnOver == true) 
                {
                    turnIndicator.GetComponent<TextMesh> ().text = "Player 2's turn";
                    StartCoroutine(JustWait (1f));
                }*/
            }

            //not out turn
            else if (tempTime - firebasePollLast > POLLINGFREQUENCY)     //polled at frequency of 3
            {
                firebasePollLast = tempTime;
                getDataFromFB();
            }
        }
        if (movingAnimation == true)
            return false;
        else
            return turnOver;

        //Debug.Log (selectedCoordNew);
    }


    bool PieceAtSquare(int turnBW, string coord)
    {
        Dictionary<GameObject, string> dsel;

        if (turnBW == 1)
        {
            dsel = white;
        }
        else if (turnBW == 2)
        {
            dsel = black;
        }
        else
        {
            dsel = null;
        }

        GameObject keysel = (GameObject)(dsel.FirstOrDefault(x => x.Value == coord).Key);
        if (keysel == null)
            return false;
        else
            return true;
    }

    bool EvalCheckOnPosition(int turnBW, string Pos)
    {
        //true implies check or not allowed, false imples no check
        bool ans = false;
        if (turnBW == 1)
        {
            //Debug.Log("EvalCheckonPos called, king coord = " + white[whiteKing]);
            string whiteKingPosBackup = white[whiteKing];
            bool checkWhiteBackup = whiteCheck;

            GameObject keysel = (GameObject)(white.FirstOrDefault(x => x.Value == Pos).Key);
            if (PieceAtSquare(1, Pos) == false || keysel == whiteKing)
                white[whiteKing] = Pos;
            else
                return true;
            //Piece at that square already, ambiguous. Return OK as no object modified.
            //operates only on king

            //PROBLEM IN CASTLING CASE, EVALCHECK CALLED AGAIN AND AGAIN
            EvalCheckWhite(false, true);
            ans = whiteCheck;

            white[whiteKing] = whiteKingPosBackup;
            whiteCheck = checkWhiteBackup;
        }
        else if (turnBW == 2)
        {
            //Debug.Log("EvalCheckonPos called, king coord = " + black[blackKing]);
            string blackKingPosBackup = black[blackKing];
            bool checkBlackBackup = blackCheck;

            GameObject keysel = (GameObject)(black.FirstOrDefault(x => x.Value == Pos).Key);
            if (PieceAtSquare(1, Pos) == false || keysel == blackKing)
                black[blackKing] = Pos;
            else
                return true;    //Piece at that square already, ambiguous

            EvalCheckBlack(false, true);
            ans = blackCheck;

            black[blackKing] = blackKingPosBackup;
            blackCheck = checkBlackBackup;
        }
        //DrawChessPieces ();
        return ans;
    }

    void EvalCheckWhite(bool actualMove = false, bool ignoreKingCastle = false)
    {
        //evaluate mate only if its an actual move, actual move when =true
        //Debug.Log ("King coordinate = " + white [whiteKing]);
        whiteCheck = false;
        string kingCoord = white[whiteKing];
        GameObject key_save = null;
        //int i = 0;
        foreach (GameObject key in black.Keys.ToList())
        {
            //i++;
            //if (i > 16)
            //	break;
            if (key == null || black[key] == "dead")
                continue;
            whiteCheck = whiteCheck || moveValidOrNot(key, black[key], kingCoord, ignoreKingCastle);
            if (whiteCheck)
            {
                key_save = key;
                break;
            }
        }
        if (whiteCheck && actualMove)
        {
            evalCheckMate(1);
            Debug.Log("Check on white by unit " + key_save.ToString() + " from square " + black[key_save] + " at king " + kingCoord);
        }
    }

    void EvalCheckBlack(bool actualMove = false, bool ignoreKingCastle = false)
    {
        //Debug.Log ("King coordinate = " + black [blackKing]);
        blackCheck = false;
        string kingCoord = black[blackKing];
        GameObject key_save = null;
        foreach (GameObject key in white.Keys.ToList())
        {
            if (key == null || white[key] == "dead")
                continue;
            blackCheck = blackCheck || moveValidOrNot(key, white[key], kingCoord, ignoreKingCastle);
            if (blackCheck)
            {
                key_save = key;
                break;
            }
        }
        if (blackCheck && actualMove)
        {
            evalCheckMate(2);
            Debug.Log("Check on black by unit " + key_save.ToString() + " from square " + white[key_save] + " at king " + kingCoord);
        }
    }

    void evalCheck(int whiteOrBlack)
    {
        //this is also lockable
        //if (checkMateLock == true)
        //	return;
        //checkMateLock = true;

        if (whiteOrBlack == 1)
            EvalCheckWhite();
        else if (whiteOrBlack == 2)
            EvalCheckBlack();

        //checkMateLock = false;

    }

    void evalCheckMate(int turnBW)
    {
        if (checkMateLock == true)
            return;
        //only one checkmate evaluation at once
        checkMateLock = true;
        //if (checking != 0)
        //	return;
        //implies the moveChessPiece was called due to a checkmate checking call itself
        Debug.Log("Evaluating checkmate");
        Dictionary<GameObject, string> backupWhite = new Dictionary<GameObject, string>(white);
        Dictionary<GameObject, string> backupBlack = new Dictionary<GameObject, string>(black);
        bool whiteKingMoveBackup = whiteKingMoved;
        bool blackKingMoveBackup = blackKingMoved;
        bool whiteRook0MovedBackup = whiteRook0Moved;
        bool whiteRook1MovedBackup = whiteRook1Moved;
        bool blackRook0MovedBackup = blackRook0Moved;
        bool blackRook1MovedBackup = blackRook1Moved;
        bool whiteCheckBackup = whiteCheck;
        bool blackCheckBackup = blackCheck;
        KVP lastMoveFromBackup = new KVP(lastMoveFrom);
        bool elPassantePossibleBackup = elPassantePossible;
        int turnBackup = turn;

        bool ans = true;

        Dictionary<GameObject, string> dsel = null;
        if (turnBW == 1)
        {
            dsel = white;
        }
        else if (turnBW == 2)
        {
            dsel = black;

        }
        else
        {
            checkMateLock = false;
            return;
        }
        foreach (GameObject g in dsel.Keys.ToList())
        {
            //Debug.Log ("Attempting to move " + g.ToString ());
            if (g == null)
                continue;
            HashSet<int> validMoves = null;
            string coord = dsel[g];
            if (coord == "dead")
                continue;
            string type = typeDic[g];
            switch (type)
            {
                case "R":
                    validMoves = validMovesStraight(coord);
                    break;
                case "B":
                    validMoves = validMovesDiagonal(coord);
                    break;
                case "Q":
                    validMoves = validMovesDiagonal(coord);
                    validMoves.UnionWith(validMovesStraight(coord));
                    break;
                case "King":
                    validMoves = validMovesKing(coord);
                    break;
                case "K":
                    validMoves = validMovesKnight(coord);
                    break;
                case "BP":
                    validMoves = validMovesBlackPawn(coord);
                    break;
                case "WP":
                    validMoves = validMovesWhitePawn(coord);
                    break;
                default:
                    validMoves = null;
                    break;
            }

            if (validMoves == null)
                continue;

            foreach (int i in validMoves)
            {
                Debug.Log("Checking piece " + g.ToString() + " for move " + coord + " to " + convertIntToPos(i));
                string newPos = convertIntToPos(i);
                if (moveChessPiece(turnBW, coord, newPos, false) == false)     //actually move set to false
                {   //1 for 	dont evaluate checkmate in a loop
                    Debug.Log("Wasn't allowed");
                    white = new Dictionary<GameObject, string>(backupWhite);
                    black = new Dictionary<GameObject, string>(backupBlack);
                    whiteKingMoved = whiteKingMoveBackup;
                    blackKingMoved = blackKingMoveBackup;
                    whiteRook0Moved = whiteRook0MovedBackup;
                    whiteRook1Moved = whiteRook1MovedBackup;
                    blackRook0Moved = blackRook0MovedBackup;
                    blackRook1Moved = blackRook1MovedBackup;
                    whiteCheck = whiteCheckBackup;
                    blackCheck = blackCheckBackup;
                    lastMoveFrom = new KVP(lastMoveFromBackup);
                    elPassantePossible = elPassantePossibleBackup;
                    turn = turnBackup;
                    continue;
                }
                evalCheck(turnBW);
                if (turnBW == 1)
                    ans = ans && whiteCheck;
                else if (turnBW == 2)
                    ans = ans && blackCheck;

                if (ans == false)
                    break;

                white = new Dictionary<GameObject, string>(backupWhite);
                black = new Dictionary<GameObject, string>(backupBlack);
                whiteKingMoved = whiteKingMoveBackup;
                blackKingMoved = blackKingMoveBackup;
                whiteRook0Moved = whiteRook0MovedBackup;
                whiteRook1Moved = whiteRook1MovedBackup;
                blackRook0Moved = blackRook0MovedBackup;
                blackRook1Moved = blackRook1MovedBackup;
                whiteCheck = whiteCheckBackup;
                blackCheck = blackCheckBackup;
                lastMoveFrom = new KVP(lastMoveFromBackup);
                elPassantePossible = elPassantePossibleBackup;
                turn = turnBackup;

            }

            if (ans == false)
                break;
        }

        white = new Dictionary<GameObject, string>(backupWhite);
        black = new Dictionary<GameObject, string>(backupBlack);
        whiteKingMoved = whiteKingMoveBackup;
        blackKingMoved = blackKingMoveBackup;
        whiteRook0Moved = whiteRook0MovedBackup;
        whiteRook1Moved = whiteRook1MovedBackup;
        blackRook0Moved = blackRook0MovedBackup;
        blackRook1Moved = blackRook1MovedBackup;
        whiteCheck = whiteCheckBackup;
        blackCheck = blackCheckBackup;
        lastMoveFrom = new KVP(lastMoveFromBackup);
        elPassantePossible = elPassantePossibleBackup;
        turn = turnBackup;

        checkmate = ans;
        if (checkmate == true)
            Debug.Log("Mated!");
        else
            Debug.Log("Not a mate");

        checkMateLock = false;
    }

    /*
	//Original
	void glowCoord(string coord, Color c)
	{
		glowCube.transform.position = new Vector3 (parseXCoord (coord), 0.02f, parseZCoord (coord));
		glowCube.transform.localScale = new Vector3 (10, 1, 10);
		Material m = glowCube.GetComponent<Renderer> ().material;
		m.color = c;
		//StartCoroutine (MoveUp (coord));
		if (PieceAtSquare (1, coord) && turn==1) 
		{
			GameObject keysel = (GameObject)(white.FirstOrDefault(x => x.Value == coord).Key);
			if (c == Color.clear) 
			{
				//Debug.Log ("Moving down");
				StartCoroutine (MoveDown (VERTICAL_TIME, keysel));
			}
			else
				StartCoroutine (MoveUp (VERTICAL_TIME, keysel));
		}
		else if (PieceAtSquare (2, coord) && turn==2) 
		{
			GameObject keysel = (GameObject)(black.FirstOrDefault(x => x.Value == coord).Key);
			if (c == Color.clear) 
			{
				//Debug.Log ("Moving down");
				StartCoroutine (MoveDown (VERTICAL_TIME, keysel));
			}
			else
				StartCoroutine (MoveUp (VERTICAL_TIME, keysel));
		}

		//m.color = new Color (153f/255f, 101f/255f, 21f/255f);
	}
	*/

    void glowCoord(string coord, Color c)
    {
        if (coord == "dead" || coord == "xx")
        {
            return;
        }
        glowCube.transform.position = new Vector3(parseXCoord(coord), 0.02f, parseZCoord(coord));
        glowCube.transform.localScale = new Vector3(10, 1, 10);
        Material m = glowCube.GetComponent<Renderer>().material;
        m.color = c;
        //StartCoroutine (MoveUp (coord));
        if (PieceAtSquare(1, coord) && turn == 1)
        {
            GameObject keysel = (GameObject)(white.FirstOrDefault(x => x.Value == coord).Key);
            if (c == Color.clear)
            {
                //Debug.Log ("Moving down");
                MoveDown(keysel);
                //DrawChessPieces();
                //Debug.Log("Drawing chess board again");
            }
            else
                MoveUp(keysel);
        }
        else if (PieceAtSquare(2, coord) && turn == 2)
        {
            GameObject keysel = (GameObject)(black.FirstOrDefault(x => x.Value == coord).Key);
            if (c == Color.clear)
            {
                MoveDown(keysel);
                //Debug.Log ("Moving down");
                //StartCoroutine (MoveDown (VERTICAL_TIME, keysel));
                //DrawChessPieces();
            }
            else
                MoveUp(keysel);

            //StartCoroutine (MoveUp (VERTICAL_TIME, keysel));
        }

        //m.color = new Color (153f/255f, 101f/255f, 21f/255f);
    }

    Color getColor(float red, float green, float blue, int alpha)
    {
        //Debug.Log(((float)(red/255)).ToString() + green.ToString() + blue.ToString() + alpha.ToString());
        Color r = new Color(((float)(red / 255)), ((float)(green / 255)), ((float)(blue / 255)), ((float)(alpha / 255)));
        //  Debug.Log(r.ToString());
        return r;
    }

    /*IEnumerator MoveUp(float waitTime, GameObject g)
	{
		if (g == null)
			yield break;
		if (g.transform.position.y == 0.02f) 
		{
            g.GetComponent<Rigidbody>().isKinematic = false;
			Rigidbody rb = g.GetComponent<Rigidbody> ();
            Vector3 backup = rb.velocity;
            rb.velocity = new Vector3 (backup.x, 30, backup.z);
			//while (g.transform.position.y < 4f)
			yield return new WaitForSecondsRealtime (waitTime);
			rb.velocity = new Vector3 (backup.x, 0, backup.z);
            g.GetComponent<Rigidbody>().isKinematic = true;
        }
        //else 
		//{
		//	Debug.Log ("Element already up");
		//}
    }*/

    void MoveUp(GameObject g)
    {
        if (g == null)
            return;
        Vector3 pos = g.transform.position;
        if (pos.y == 0.02f)
        {
            g.transform.position = new Vector3(pos.x, MOVEUPHEIGHT, pos.z);
        }
        //else 
        //{
        //	Debug.Log ("Element already up");
        //}
    }

    void MoveAnimator(GameObject g, string oldPos, string newPos)
    {
        //Debug.Log("moving in fn");
        if (oldPos == "dead" || newPos == "dead")
        {
            movingAnimation = false;
            DrawChessPieces();
            return;
        }

        int oldInt = convertPosToInt(oldPos), newInt = convertPosToInt(newPos);
        //c1 gets converte d to 31, d5 gets converted to 45
        int xold = oldInt / 10, zold = oldInt % 10, xnew = newInt / 10, znew = newInt % 10;
        int velx = (xnew - xold), velz = (znew - zold);
        movingAnimation = true;
        //Debug.Log("Moving starting");

        g.transform.Translate(velx * 10 * Time.deltaTime, 0, velz * 10 * Time.deltaTime);

        if (intInChessBoard(convertPosToInt(ReverseParseCoord(g.transform.position))) == false)
        {
            movingAnimation = false;
            DrawChessPieces();
        }
        else if (oldInt / 10 <= xnew && oldInt % 10 <= znew)
        {
            if (convertPosToInt(ReverseParseCoord(g.transform.position)) / 10 >= xnew && convertPosToInt(ReverseParseCoord(g.transform.position)) % 10 >= znew)
            {
                movingAnimation = false;
                DrawChessPieces();
            }
        }
        else if (oldInt / 10 <= xnew && oldInt % 10 >= znew)
        {
            if (convertPosToInt(ReverseParseCoord(g.transform.position)) / 10 >= xnew && convertPosToInt(ReverseParseCoord(g.transform.position)) % 10 <= znew)
            {
                movingAnimation = false;
                DrawChessPieces();
            }
            //g.transform.Translate(velx * 10 * Time.deltaTime, 0, velz * 10 * Time.deltaTime * (-1));
        }
        else if (oldInt / 10 >= xnew && oldInt % 10 <= znew)
        {
            if (convertPosToInt(ReverseParseCoord(g.transform.position)) / 10 <= xnew && convertPosToInt(ReverseParseCoord(g.transform.position)) % 10 >= znew)
            {
                movingAnimation = false;
                DrawChessPieces();
            }
            //g.transform.Translate(velx * 10 * Time.deltaTime * (-1), 0, velz * 10 * Time.deltaTime);
        }
        else if (oldInt / 10 >= xnew && oldInt % 10 >= znew)
        {
            if (convertPosToInt(ReverseParseCoord(g.transform.position)) / 10 <= xnew && convertPosToInt(ReverseParseCoord(g.transform.position)) % 10 <= znew)
            {
                movingAnimation = false;
                DrawChessPieces();
            }
            //g.transform.Translate(velx * 10 * Time.deltaTime * (-1), 0, velz * 10 * Time.deltaTime * (-1));
        }
        else
        {
            movingAnimation = false;
            DrawChessPieces();
        }
        //Debug.Log("Moving ending");
        //make a lock, and call this function of update if lock is true. Need movement on every from, not in one frame.
        //INCOMPLETE
    }

    void MoveDown(GameObject g)
    {
        if (g == null)
            return;
        Vector3 pos = g.transform.position;
        if (pos.y != 0.02f)
        {
            g.transform.position = new Vector3(pos.x, 0.02f, pos.z);
        }
    }

    /*IEnumerator JustWait(float waitTime)
	{
		yield return new WaitForSeconds (waitTime);
	}*/

    string ReverseParseCoord(Vector3 pt)
    {
        char x, z;
        //int tempx, tempz;
        //tempx = (int)pt.x / 10;
        //tempz = (int)pt.z / 10;

        if (30 < pt.x && pt.x <= 40)
            x = 'h';
        else if (20 < pt.x && pt.x <= 30)
            x = 'g';
        else if (10 < pt.x && pt.x <= 20)
            x = 'f';
        else if (0 < pt.x && pt.x <= 10)
            x = 'e';
        else if (-10 < pt.x && pt.x <= 0)
            x = 'd';
        else if (-20 < pt.x && pt.x <= -10)
            x = 'c';
        else if (-30 < pt.x && pt.x <= -20)
            x = 'b';
        else if (-40 <= pt.x && pt.x <= -30)
            x = 'a';
        else
            x = 'x';

        if (30 < pt.z && pt.z <= 40)
            z = '8';
        else if (20 < pt.z && pt.z <= 30)
            z = '7';
        else if (10 < pt.z && pt.z <= 20)
            z = '6';
        else if (0 < pt.z && pt.z <= 10)
            z = '5';
        else if (-10 < pt.z && pt.z <= 0)
            z = '4';
        else if (-20 < pt.z && pt.z <= -10)
            z = '3';
        else if (-30 < pt.z && pt.z <= -20)
            z = '2';
        else if (-40 <= pt.z && pt.z <= -30)
            z = '1';
        else
            z = 'x';

        return string.Concat(x, z);
    }

    
    //firebase.Child("Test", true).GetValue(FirebaseParam.Empty.OrderByChild("time").LimitToLast(1));
    void UpdateToFirebase(string oldPos, string newPos)
    {
        Debug.Log("Pushing data");
        TimeSpan t = DateTime.UtcNow - DateTime.Today;
        string secondsSinceToday = ((int)t.TotalSeconds).ToString();
        moveNumber += 1;
        firebase.Child("Test").Push("{ \"turn\": \"" + (moveNumber).ToString() + "\", \"datafrom\": \"" + oldPos + "\", \"datato\": \"" + newPos + "\", \"time\": " + secondsSinceToday + "}", true);

        latestTimeQuery = float.Parse(secondsSinceToday);

        /*
        if (moveNumber % 2 == 0)
            turn = 1;
        else if (moveNumber % 2 == 1)
            turn = 2;       //white moved and pushed its data
        */
    }

    void UpdateFromData()
    {
        Debug.Log("From inside UpdateFromData. Turn rn is " + turn.ToString());
        //turnIndicator.GetComponent<TextMesh>().text = "From inside UpdateFromData. Turn rn is " + turn.ToString();

        if (queryResult.Length == 0)
            return;

        int a = queryResult.IndexOf("\"datafrom\":\"");
        int b = queryResult.IndexOf("\",\"datato\":\"");
        int c = queryResult.IndexOf("\",\"time\":");
        int d = queryResult.IndexOf(",\"turn\":\"");
        int e = queryResult.Length;

        //Debug.Log(a.ToString() + " " + b.ToString() + " " + c.ToString() + " " + d.ToString() + " " + e.ToString());
        float timequery = float.Parse(queryResult.Substring(c + 9, d - c - 9));
        Debug.Log("Time is " + timequery.ToString());
        if (timequery <= latestTimeQuery)
        {
            Debug.Log("No updates");
            //turnIndicator.GetComponent<TextMesh>().text = "No updates";
            return;
        }
        else
        {
            latestTimeQuery = timequery;
        }
        string datafrom = queryResult.Substring(a + 12, b - a - 12);     //fetches what is inside "data" (hopefully)
        string datato = queryResult.Substring(b + 12, c - b - 12);
        moveNumber = int.Parse(queryResult.Substring(d + 9, e - d - 9 - 3));
        Debug.Log("Data from: " + datafrom + " to " + datato + " on move " + moveNumber.ToString());
        //turnIndicator.GetComponent<TextMesh>().text = "Data from: " + datafrom + " to " + datato + " on move " + moveNumber.ToString();

        queryResult = "";
        //make it empty

        int localTurn = 0;

        //turn updation
        if (moveNumber == 0)
            localTurn = 0;
        else if (moveNumber % 2 == 1)
            localTurn = 1;       //white moved and pushed its data
        else if (moveNumber % 2 == 0)
            localTurn = 2;

        //pieces updation0
        if (String.Compare(datafrom, "connect!") == 0)
        {
            Debug.Log("Game not started yet");
            turn = 1;
        }
        else if (localTurn == turn)
        {
            if (moveChessPiece(turn, datafrom, datato, true))
                turnOver = true;
            //DrawChessPieces();
            //turn not updated yet, so moving for the previos player
        }

    }
  
    void getDataFromFB()
    {
        if (fetchActive == true)
            return;
        
        fetchActive = true;
        Debug.Log("Fetching data");
        //turnIndicator.GetComponent<TextMesh>().text = "Fetching Data";
        //saves data received into queryResult
        firebase.Child("Test", true).GetValue(FirebaseParam.Empty.OrderByChild("time").LimitToLast(1));
        DrawChessPieces();
        fetchActive = false;
        UpdateFromData();
        
    }

    #region Firebase shenanigans

    void GetOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Get from key: <" + sender.FullKey + ">");
        DoDebug("[OK] Raw Json: " + snapshot.RawJson);
        queryResult = snapshot.RawJson;

        /*Dictionary<string, object> dict = snapshot.Value<Dictionary<string, object>>();
        List<string> keys = snapshot.Keys;

        if (keys != null)
            foreach (string key in keys)
            {
                DoDebug(key + " = " + dict[key].ToString());
            }
            */
        //ConfirmConnectionToFB();
    }

    void GetFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Get from key: <" + sender.FullKey + ">,  " + err.Message + " (" + (int)err.Status + ")");
    }

    void SetOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Set from key: <" + sender.FullKey + ">");
    }

    void SetFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Set from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void UpdateOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Update from key: <" + sender.FullKey + ">");
    }

    void UpdateFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Update from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void DelOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Del from key: <" + sender.FullKey + ">");
    }

    void DelFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Del from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void PushOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] Push from key: <" + sender.FullKey + ">");
    }

    void PushFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] Push from key: <" + sender.FullKey + ">, " + err.Message + " (" + (int)err.Status + ")");
    }

    void GetRulesOKHandler(Firebase sender, DataSnapshot snapshot)
    {
        DoDebug("[OK] GetRules");
        DoDebug("[OK] Raw Json: " + snapshot.RawJson);
    }

    void GetRulesFailHandler(Firebase sender, FirebaseError err)
    {
        DoDebug("[ERR] GetRules,  " + err.Message + " (" + (int)err.Status + ")");
    }

    void GetTimeStamp(Firebase sender, DataSnapshot snapshot)
    {
        long timeStamp = snapshot.Value<long>();
        DateTime dateTime = Firebase.TimeStampToDateTime(timeStamp);

        DoDebug("[OK] Get on timestamp key: <" + sender.FullKey + ">");
        DoDebug("Date: " + timeStamp + " --> " + dateTime.ToString());
    }

    void DoDebug(string str)
    {
        Debug.Log(str);
        /*if (textMesh != null)
        {
            textMesh.text += (++debug_idx + ". " + str) + "\n";
        }*/
    }

    #endregion
}
