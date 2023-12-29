using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class MapBehaviour : MonoBehaviour
{
    #region Variables
    //Serialized variables
    [Header("Map settings")]
    [SerializeField] private int m_MapWidth;
    [SerializeField] private int m_MapHeight;
    [SerializeField] private int m_MineCount;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI m_MineCounterText;
    [SerializeField] private TextMeshProUGUI m_TimerText;
    [SerializeField] private TextMeshProUGUI m_ErrorMessage;

    [Header("UI - input fields")]
    [SerializeField] private TextMeshProUGUI m_WidthInputField;
    [SerializeField] private TextMeshProUGUI m_HeightInputField;
    [SerializeField] private TextMeshProUGUI m_MineCountInputField;

    [Header("UI - smiley")]
    [SerializeField] private Sprite m_GameNeutralSmiley;
    [SerializeField] private Sprite m_GameWonSmiley;
    [SerializeField] private Sprite m_GameLossSmiley;
    [SerializeField] private Image m_GameSmileyRepresentation;

    //regular variables
    private Tilemap m_Tilemap;
    private string m_TilesPath = "Tiles/";
    private TileBase m_TransitionAnimationTile;
    private MapCell[,] m_Cells;

    private bool m_FirstClick = true;
    private bool m_GameOver = false;

    private int m_TilesToRevealForWin;
    private int m_MineCounter;
    private float m_Timer = 0f;

    private int m_UpdatedMapWidth;
    private int m_UpdatedMapHeight;
    private int m_UpdatedMineCount;
    #endregion

    #region LifeTime
    private void Awake()
    {
        //setup variables
        m_Tilemap = GetComponentInChildren<Tilemap>();
        m_TransitionAnimationTile = Resources.Load<TileBase>(m_TilesPath + "TileRevealAnimation");

        m_WidthInputField.text = m_MapWidth.ToString();
        m_HeightInputField.text = m_MapHeight.ToString();
        m_MineCountInputField.text = m_MineCount.ToString();

        m_UpdatedMapWidth = m_MapWidth;
        m_UpdatedMapHeight = m_MapHeight;
        m_UpdatedMineCount = m_MineCount;

        //start the game
        StartGame();
    }

    private void Update()
    {
        m_MineCounterText.text = m_MineCounter.ToString();

        if (m_GameOver)
            return;

        if (!m_FirstClick)
        {
            m_Timer += Time.deltaTime;
            m_TimerText.text = m_Timer.ToString("F2");
        }

        if (Input.GetMouseButtonDown(0))//left mouse click occured
            HandleLeftMouseClick();
        else if (Input.GetMouseButtonDown(1))//right mouse click occured
            SetFlag();
    }
    #endregion

    #region Core Functionality
    private void HandleLeftMouseClick()
    {
        //Find the correct position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = m_Tilemap.WorldToCell(worldPos);

        //we ignore invalid positions
        if (!IsValidPos(cellPos))
            return;

        //First click gets special treatment
        if (m_FirstClick)
        {
            HandleFirstClick(cellPos);
            return;
        }

        //Check if we can flood the surrounding tiles if the correct amount of mines are flagged
        if (m_Cells[cellPos.x, cellPos.y].IsNumber())
        {
            CheckNumberFlooding(cellPos);
            return;
        }

        //when tile is not unkown(this means it is revealed or a flag(flagged tiles also can't be revealed as safe guard for the player))
        if (m_Cells[cellPos.x, cellPos.y].State != MapCellState.TileUnknown)
            return;

        //if we reach this point we want to reveal the tile
        RevealTile(cellPos);
    }

    private void RevealTile(Vector3Int cellPos)
    {
        //Player hit a mine
        if (m_Cells[cellPos.x, cellPos.y].IsMine)
        {
            GameEnd(false);
            UpdateCellState(cellPos, MapCellState.TileExploded, true);
        }
        //we reveal the number of adjecent mines to the player
        else
        {
            //if the player thought this was a mine but was wrong we correct the mine counter
            if (m_Cells[cellPos.x, cellPos.y].State == MapCellState.TileFlag)
                ++m_MineCounter;

            UpdateCellState(cellPos, CalculateMineNumber(cellPos), true);
            --m_TilesToRevealForWin;

            if (m_TilesToRevealForWin == 0)
                GameEnd(true);
        }

        //start flooding if we revealed a zero tile
        if (m_Cells[cellPos.x, cellPos.y].State == MapCellState.TileEmpty)
            Flooding(cellPos, true);
    }

    private void GameEnd(bool hasWon)
    {
        m_GameOver = true;

        //Reveal all mines 
        for (int x = 0; x < m_MapWidth; x++)
        {
            for (int y = 0; y < m_MapHeight; y++)
            {
                if (m_Cells[x, y].IsMine)
                {
                    if (hasWon)
                    {
                        UpdateCellState(new Vector3Int(x, y), MapCellState.TileFlag, true);
                        m_GameSmileyRepresentation.sprite = m_GameWonSmiley;
                    }
                    else
                    {
                        UpdateCellState(new Vector3Int(x, y), MapCellState.TileMine, true);
                        m_GameSmileyRepresentation.sprite = m_GameLossSmiley;
                    }
                }
            }
        }
    }

    //If a number has the correct amount of flags around it we will reveal the surrounding tiles
    private void CheckNumberFlooding(Vector3Int cellPos)
    {
        //Find the amount of flagged tiles around the cell
        int flagCounter = 0;
        List<Vector3Int> surroundingCellPositions = GetSurroundingCellPositions(cellPos, false);

        foreach (Vector3Int pos in surroundingCellPositions)
        {
            if (m_Cells[pos.x, pos.y].State == MapCellState.TileFlag)
                ++flagCounter;
        }
        //Check if the amount of flagged tiles matches the number
        int desiredFlagTiles = (int)m_Cells[cellPos.x, cellPos.y].State - (int)MapCellState.TileEmpty;

        if (desiredFlagTiles == flagCounter)
            Flooding(cellPos, false);
    }

    private void HandleFirstClick(Vector3Int cellPos)
    {
        m_FirstClick = false;

        //The first clicked cell is always zero
        UpdateCellState(cellPos, MapCellState.TileEmpty, true);
        --m_TilesToRevealForWin;

        //Now we can generate the bombs 
        GenerateMines(GetSurroundingCellPositions(cellPos, true));

        //Now that the bombs are known we can start the flooding 
        Flooding(cellPos, true);
    }

    private void SetFlag()
    {
        //Find the correct position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = m_Tilemap.WorldToCell(worldPos);

        if (!IsValidPos(cellPos))
            return;

        //Set or unset the flag depending on the state
        if (m_Cells[cellPos.x, cellPos.y].State == MapCellState.TileFlag)
        {
            UpdateCellState(cellPos, MapCellState.TileUnknown, false);
            ++m_MineCounter;
        }
        else if (m_Cells[cellPos.x, cellPos.y].State == MapCellState.TileUnknown)
        {
            UpdateCellState(cellPos, MapCellState.TileFlag, true);
            --m_MineCounter;
        }
    }

    //Reveal all tiles that are allowed by game rules
    private void Flooding(Vector3Int cellPos, bool shouldRevealFlags)
    {
        List<Vector3Int> surroundingCellPositions = GetSurroundingCellPositions(cellPos, false);

        foreach (Vector3Int pos in surroundingCellPositions)
        {
            //check if we need to skip
            if (shouldRevealFlags) //we only reveal the unknown and flag tiles
            {
                if (m_Cells[pos.x, pos.y].IsRevealed())
                    continue;
            }
            else //We only reveal the unknown tiles
            {
                if (m_Cells[pos.x, pos.y].State != MapCellState.TileUnknown)
                    continue;
            }
            //This function contains a call to Flooding so it is recursive
            RevealTile(pos);
        }
    }

    #region Generation
    private void GenerateMap()
    {
        //setup variables
        m_Cells = new MapCell[m_MapWidth, m_MapHeight];
        m_Tilemap.ClearAllTiles();

        Tile unknownTile = Resources.Load<Tile>(m_TilesPath + MapCellState.TileUnknown.ToString());

        //Loop over the map cells
        for (int x = 0; x < m_MapWidth; x++)
        {
            for (int y = 0; y < m_MapHeight; y++)
            {
                //create a map cell and set the correct tile
                Vector3Int pos = new Vector3Int(x, y, 0);

                m_Cells[x, y] = new MapCell(pos);
                m_Tilemap.SetTile(pos, unknownTile);
            }
        }
    }

    private void GenerateMines(List<Vector3Int> nonMineCells)
    {
        for (int i = 0; i < m_MineCount; i++)
        {
            int xPos = Random.Range(0, m_MapWidth);
            int yPos = Random.Range(0, m_MapHeight);

            //if the current position can't be a mine or is already a mine we iterate untill we find a none mine spot (avoids getting unvalid positions for unkown attempts in a row)
            while (m_Cells[xPos, yPos].IsMine || nonMineCells.Contains(new Vector3Int(xPos, yPos)))
            {
                ++xPos;

                if (xPos >= m_MapWidth)
                {
                    xPos = 0;
                    ++yPos;
                }
                if (yPos >= m_MapHeight)
                    yPos = 0;
            }
            //We have a valid position for a new mine
            m_Cells[xPos, yPos].IsMine = true;
        }
    }
    #endregion
    #endregion

    #region Public functions
    public void StartGame()
    {
        //Check if the game is valid to start
        if (m_Tilemap == null)
        {
            LogError("The tilemap for the map is missing.");
            return;
        }

        if (m_UpdatedMineCount > ((m_UpdatedMapWidth * m_UpdatedMapHeight) - 9))//Our first click is a 0 tile which means at least 9 tiles can't be a mine
        {
            LogError("There are to many bombs for the size of the map");
            return;
        }

        //setup camera size
        Camera camera = Camera.main;
        float minSizeForHeight = (m_UpdatedMapHeight / 2f) * 1.2f; //Divided by 2 because 2 cells can fit into one orthographicSize height multiplied by a ratio that looks good with current UI setup
        float minSizeForWidth = (m_UpdatedMapWidth / 4f) * 1.5f; //Divided by 4 because 4 cells can fit into one orthographicSize width multiplied by a ratio that looks good with current UI setup

        //Use the heighest size number to make sure it always fits onto the screen
        camera.orthographicSize = Mathf.Max(minSizeForHeight, minSizeForWidth);

        //setup our location to always start from the bottom left of our camera
        float screenOffsetRatio = 0.05f; //Just a small value that looks good
        Vector3 screenPos = new Vector3(camera.pixelWidth * screenOffsetRatio, camera.pixelHeight * screenOffsetRatio, 10f);//
        transform.position = camera.ScreenToWorldPoint(screenPos);

        //Setup variables correctly
        m_MineCounter = m_MineCount = m_UpdatedMineCount;
        m_MapHeight = m_UpdatedMapHeight;
        m_MapWidth = m_UpdatedMapWidth;

        m_TilesToRevealForWin = (m_MapWidth * m_MapHeight) - m_MineCount;
        m_Timer = 0f;
        m_FirstClick = true;
        m_GameOver = false;

        m_ErrorMessage.gameObject.SetActive(false);
        m_TimerText.text = m_Timer.ToString("F2");
        m_GameSmileyRepresentation.sprite = m_GameNeutralSmiley;

        //Create the map structure 
        GenerateMap();
    }

    public void UpdateGameWidth(string newValue)
    {
        int temp = CheckInputFieldDataForValidNumber(newValue);

        if (temp > 0)
            m_UpdatedMapWidth = temp;
    }

    public void UpdateGameHeight(string newValue)
    {
        int temp = CheckInputFieldDataForValidNumber(newValue);

        if (temp > 0)
            m_UpdatedMapHeight = temp;
    }

    public void UpdateMineCounter(string newValue)
    {
        int temp = CheckInputFieldDataForValidNumber(newValue);

        if (temp > 0)
            m_UpdatedMineCount = temp;
    }
    #endregion

    #region helperFunctions

    private void LogError(string message)
    {
        m_ErrorMessage.text = message;
        m_ErrorMessage.gameObject.SetActive(true);
    }

    private int CheckInputFieldDataForValidNumber(string value)
    {
        int newIntValue = -1;

        int.TryParse(value, out newIntValue);

        if (newIntValue <= 0)
            LogError("Invalid input given, the data has to be a number larger than 0");
        else
            m_ErrorMessage.gameObject.SetActive(false);

        return newIntValue;
    }

    private void UpdateCellState(Vector3Int pos, MapCellState state, bool revealCell)
    {
        if (!IsValidPos(pos))
        {
            LogError("Something went wrong with the game");
            return;
        }
        m_Cells[pos.x, pos.y].State = state;

        if (revealCell)
        {
            m_Tilemap.SetTile(m_Cells[pos.x, pos.y].Position, m_TransitionAnimationTile);
            StartCoroutine(SetTileAfterAnimation(m_Cells[pos.x, pos.y].Position, state, m_Tilemap));
        }
        else
            m_Tilemap.SetTile(m_Cells[pos.x, pos.y].Position, Resources.Load<Tile>(m_TilesPath + state.ToString()));
    }

    private IEnumerator SetTileAfterAnimation(Vector3Int pos, MapCellState state, Tilemap tileMap)
    {
        yield return new WaitForSeconds(0.4f); //duration of the animation
        tileMap.SetTile(pos, Resources.Load<Tile>(m_TilesPath + state.ToString()));
    }

    private bool IsValidPos(Vector3Int cellPos)
    {
        if (cellPos.x < 0 || cellPos.x >= m_MapWidth)
            return false;
        if (cellPos.y < 0 || cellPos.y >= m_MapHeight)
            return false;

        return true;
    }

    //retuns a list of a cell position (if included is true) and all its adjecent cells their position
    private List<Vector3Int> GetSurroundingCellPositions(Vector3Int cellPos, bool includeSelf)
    {
        List<Vector3Int> surroundingCellPositions = new List<Vector3Int>();

        //loop over all the surrounding tiles
        for (int xAdjustment = -1; xAdjustment <= 1; xAdjustment++)
        {
            for (int yAdjustment = -1; yAdjustment <= 1; yAdjustment++)
            {
                //skip if it is the tile itself is not wanted
                if (includeSelf == false && (yAdjustment == 0 && xAdjustment == 0))
                    continue;

                //calculate the new position
                int xPos = cellPos.x + xAdjustment;
                int yPos = cellPos.y + yAdjustment;

                //make sure the generation wraps around the edges
                if (xPos >= m_MapWidth)
                    xPos = 0;
                else if (xPos < 0)
                    xPos = m_MapWidth - 1;

                if (yPos >= m_MapHeight)
                    yPos = 0;
                else if (yPos < 0)
                    yPos = m_MapHeight - 1;

                surroundingCellPositions.Add(new Vector3Int(xPos, yPos));
            }
        }
        return surroundingCellPositions;
    }

    private MapCellState CalculateMineNumber(Vector3Int cellPos)
    {
        int mineCount = 0;
        int cellStateValue = (int)MapCellState.TileEmpty;//the int value corresponding with 0 adjecent mines in the int

        //Get the adjecent cell positions and check if they are mines
        List<Vector3Int> surroundingCellPositions = GetSurroundingCellPositions(cellPos, false);

        foreach (Vector3Int pos in surroundingCellPositions)
        {
            //Update minecount if the adjecent cell is a mine
            if (m_Cells[pos.x, pos.y].IsMine)
                ++mineCount;
        }
        return (MapCellState)(cellStateValue + mineCount);
    }
    #endregion
}

