using UnityEngine;

public enum MapCellState
{
    //Keep enum names equal to the tile assets in the Resources/Tiles
    TileUnknown,
    TileFlag,
    TileMine,
    TileExploded,

    //keep the following region in this order, the position in the enum does not matter
    #region TileNumbers
    TileEmpty,
    Tile1,
    Tile2,
    Tile3,
    Tile4,
    Tile5,
    Tile6,
    Tile7,
    Tile8,
    #endregion
}

public struct MapCell
{
    public Vector3Int Position;
    public MapCellState State;
    public bool IsMine;

    public MapCell(Vector3Int position)
    {
        State = MapCellState.TileUnknown;
        IsMine = false;
        Position = position;
    }
    public bool IsRevealed()
    {
        return (State != MapCellState.TileUnknown && State != MapCellState.TileFlag);
    }
}


