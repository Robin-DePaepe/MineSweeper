using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using static UnityEditor.PlayerSettings;
using Random = UnityEngine.Random;

public class MapBehaviour : MonoBehaviour
{
    #region Variables
    //Serialized variables
    [Header("Map settings")]
    [SerializeField] private int m_MapWidth;
    [SerializeField] private int m_MapHeight;
    [SerializeField] private int m_MineCount;

    //regular variables
    private Tilemap m_Tilemap;
    private MapCell[,] m_Cells;
    private string m_TilesPath = "Tiles/";
    #endregion

    #region LifeTime
    private void Awake()
    {
        m_Tilemap = GetComponentInChildren<Tilemap>();

        //Check if the game is valid to start
        if (m_Tilemap == null)
        {
            Debug.LogError("The tilemap for the map is missing.");
            return;
        }

        if (m_MineCount > ((m_MapWidth * m_MapHeight) - 9))//Our first click is a 0 tile which means at least 9 tiles can't be a mine
        {
            Debug.LogError("There are to many bombs for the size of the map");
            //Todo - Add player feedback why game won't start
            return;
        }
        //Create the map
        GenerateMap();
        GenerateMines();
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))//left mouse click occured
            RevealTile();
        else if (Input.GetMouseButtonDown(1))//right mouse click occured
            SetFlag();
    }
    #endregion

    #region Core Functionality
    private void RevealTile()
    {
        //Find the correct position
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int cellPos = m_Tilemap.WorldToCell(worldPos);

        //we ignore invalid positions or when tile is not unkown (this means it is revealed or a flag(flagged tiles also can't be revealed as safe guard for the player))
        if (!IsValidPos(cellPos) || m_Cells[cellPos.x, cellPos.y].State != MapCellState.TileUnknown)
            return;

        //Player hit a mine
        if (m_Cells[cellPos.x, cellPos.y].IsMine)
        {
            UpdateCellState(cellPos, MapCellState.TileExploded);
            //Todo- run end game 
        }
        //we reveal the number of adjecent mines to the player
        else
            UpdateCellState(cellPos, CalculateMineNumber(cellPos));
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
            UpdateCellState(cellPos, MapCellState.TileUnknown);
        else if (m_Cells[cellPos.x, cellPos.y].State == MapCellState.TileUnknown)
            UpdateCellState(cellPos, MapCellState.TileFlag);
    }

    #region Generation
    private void GenerateMap()
    {
        //setup variables
        m_Cells = new MapCell[m_MapWidth, m_MapHeight];
        Tile unknownTile = Resources.Load<Tile>(m_TilesPath + MapCellState.TileUnknown.ToString() + "");

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

    private void GenerateMines()
    {
        for (int i = 0; i < m_MineCount; i++)
        {
            int xPos = Random.Range(0, m_MapWidth);
            int yPos = Random.Range(0, m_MapHeight);

            //if the current position is a mine we iterate untill we find a none mine spot (avoids getting unvalid positions for unkown attempts in a row)
            while (m_Cells[xPos, yPos].IsMine)
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

    #region helperFunctions
    private void UpdateCellState(Vector3Int pos, MapCellState state)
    {
        m_Cells[pos.x, pos.y].State = state;
        m_Tilemap.SetTile(m_Cells[pos.x, pos.y].Position, Resources.Load<Tile>(m_TilesPath + state.ToString() + ""));
    }

    private bool IsValidPos(Vector3Int cellPos)
    {
        if (cellPos.x < 0 || cellPos.x >= m_MapWidth)
            return false;
        if (cellPos.y < 0 || cellPos.y >= m_MapHeight)
            return false;

        return true;
    }

    private MapCellState CalculateMineNumber(Vector3Int cellPos)
    {
        int mineCount = 0;
        int cellStateValue = (int)MapCellState.TileEmpty;//the int value corresponding with 0 adjecent mines in the int

        //loop over all the surrounding tiles
        for (int xAdjustment = -1; xAdjustment <= 1; xAdjustment++)
        {
            for (int yAdjustment = -1; yAdjustment <= 1; yAdjustment++)
            {
                //skip if it is the tile itself
                if (yAdjustment == 0 && xAdjustment == 0)
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

                //Update minecount if the adjecent cell is a mine
                if (m_Cells[xPos, yPos].IsMine)
                    ++mineCount;
            }
        }
        return (MapCellState)(cellStateValue + mineCount);
    }
    #endregion
}

