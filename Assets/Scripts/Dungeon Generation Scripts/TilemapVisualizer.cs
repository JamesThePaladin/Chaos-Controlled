using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TilemapVisualizer : Unit
{
    //tile parameters
    [SerializeField]
    private Tilemap floorTilemap, wallTilemap; //tilemap for our floor
    [SerializeField]
    private TileBase floorTile, wallTop, wallRight, wallLeft, wallBottom, wallFull, 
        wallInnerCornerDownLeft, wallInnerCornerDownRight, 
        wallDiagonalCornerDownRight, wallDiagonalCornerDownLeft, wallDiagonalCornerUpRight, wallDiagonalCornerUpLeft; //tiles for all of tilemaps

    //node ports
    [DoNotSerialize]
    public ValueInput _floorTilemap, _wallTilemap, _floorTile, _wallTop, _wallRight, _wallLeft, _wallBottom, _wallFull,
        _wallInnerCornerDownLeft, _wallInnerCornerDownRight, _wallDiagonalCornerDownRight, _wallDiagonalCornerDownLeft, 
        _wallDiagonalCornerUpRight, _wallDiagonalCornerUpLeft;
    [DoNotSerialize]
    public ValueOutput tilemapVisualizer;
    [DoNotSerialize]
    public ControlInput inputTrigger;
    [DoNotSerialize]
    public ControlOutput outputTrigger;
    protected override void Definition()
    {
        //Making the ControlInput port visible, setting its key and running the anonymous action method to pass the flow to the outputTrigger port.
        inputTrigger = ControlInput("inputTrigger", (flow) => 
        {
            floorTilemap = flow.GetValue<Tilemap>(_floorTilemap);
            wallTilemap = flow.GetValue<Tilemap>(_wallTilemap);
            floorTile = flow.GetValue<TileBase>(_floorTile);
            wallTop = flow.GetValue<TileBase>(_wallTop);
            wallRight = flow.GetValue<TileBase>(_wallRight);
            wallLeft = flow.GetValue<TileBase>(_wallLeft);
            wallBottom = flow.GetValue<TileBase>(_wallBottom);
            wallFull = flow.GetValue<TileBase>(_wallFull);
            wallInnerCornerDownLeft = flow.GetValue<TileBase>(_wallInnerCornerDownLeft);
            wallInnerCornerDownRight = flow.GetValue<TileBase>(_wallInnerCornerDownRight);
            wallDiagonalCornerDownRight = flow.GetValue<TileBase>(_wallDiagonalCornerDownRight);
            wallDiagonalCornerDownLeft = flow.GetValue<TileBase>(_wallDiagonalCornerDownLeft);
            wallDiagonalCornerUpRight = flow.GetValue<TileBase>(_wallDiagonalCornerUpRight);
            wallDiagonalCornerUpLeft = flow.GetValue<TileBase>(_wallDiagonalCornerUpLeft);
            return outputTrigger;
        });
        //Making the ControlOutput port visible and setting its key.
        outputTrigger = ControlOutput("outputTrigger");

        //set values and labels to make visible
        _floorTilemap = ValueInput<Tilemap>("Floor Tilemap", null);
        _wallTilemap = ValueInput<Tilemap>("Wall Tilemap", null);
        _floorTile = ValueInput<TileBase>("Floor Tile", null);
        _wallTop = ValueInput<TileBase>("Wall Top", null);
        _wallRight = ValueInput<TileBase>("Wall Right", null);
        _wallLeft = ValueInput<TileBase>("Wall Left", null);
        _wallBottom = ValueInput<TileBase>("Wall Bottom", null);
        _wallFull = ValueInput<TileBase>("Wall Full", null);
        _wallInnerCornerDownLeft = ValueInput<TileBase>("Wall Inner Corner Down Left", null);
        _wallInnerCornerDownRight = ValueInput<TileBase>("Wall Inner Corner Down Right", null);
        _wallDiagonalCornerDownRight = ValueInput<TileBase>("Wall Diag Corner Down Right", null);
        _wallDiagonalCornerDownLeft = ValueInput<TileBase>("Wall Diag Corner Down Left", null);
        _wallDiagonalCornerUpRight = ValueInput<TileBase>("Wall Diag Corner Up Right", null);
        _wallDiagonalCornerUpLeft = ValueInput<TileBase>("Wall Diag Corner Up Left", null);
        tilemapVisualizer = ValueOutput<TilemapVisualizer>("TilemapVisualizer"); //TODO: Fix this tilemap assignment stuff

        //relations
        Requirement(_floorTilemap, inputTrigger);
        Requirement(_wallTilemap, inputTrigger);
        Requirement(_floorTile, inputTrigger);
        Requirement(_wallTop, inputTrigger);
        Requirement(_wallRight, inputTrigger);
        Requirement(_wallLeft, inputTrigger);
        Requirement(_wallBottom, inputTrigger);
        Requirement(_wallFull, inputTrigger);
        Requirement(_wallInnerCornerDownLeft, inputTrigger);
        Requirement(_wallInnerCornerDownRight, inputTrigger);
        Requirement(_wallDiagonalCornerDownRight, inputTrigger);
        Requirement(_wallDiagonalCornerDownLeft, inputTrigger);
        Requirement(_wallDiagonalCornerUpRight, inputTrigger);
        Requirement(_wallDiagonalCornerUpLeft, inputTrigger);
    }

    /// <summary>
    /// Paints all the floor tiles in a list
    /// </summary>
    /// <param name="floorPositions"></param>
    public void PaintFloorTiles(IEnumerable<Vector2Int> floorPositions) 
    {
        PaintTiles(floorPositions, floorTilemap, floorTile);
    }

    /// <summary>
    /// loops through all the tiles in the passed in list and paints them
    /// </summary>
    /// <param name="positions"></param>
    /// <param name="tilemap"></param>
    /// <param name="tile"></param>
    private void PaintTiles(IEnumerable<Vector2Int> positions, Tilemap tilemap, TileBase tile)
    {
        foreach (var position in positions)
        {
            PaintSingleTile(tilemap, tile, position);
        }
    }

    /// <summary>
    /// paints wall tiles with the same method as floor tiles,
    /// using the wall tilemap and wall tiles instead
    /// </summary>
    /// <param name="position"></param>
    internal void PaintSingleBasicWall(Vector2Int position, string binaryType)
    {
        //convert our binary type into a 32-bit integer
        int typeAsInt = Convert.ToInt32(binaryType, 2);
        //create an empty tile to assign our correct tile to
        TileBase tile = null;
        //if our type value is contained in our set of top wall values
        if (WallTypesHelper.wallTop.Contains(typeAsInt)) 
        {
            //assign the wall top tile to our to-be-pained tile
            tile = wallTop;
        }
        //otherwise if our type value is contained in our set of right side wall values
        else if (WallTypesHelper.wallSideRight.Contains(typeAsInt))
        {
            //assign the wall right tile to our to-be-pained tile
            tile = wallRight;
        }
        //otherwise if our type value is contained in our set of left side wall values
        else if (WallTypesHelper.wallSideLeft.Contains(typeAsInt))
        {
            //assign the wall left tile to our to-be-pained tile
            tile = wallLeft;
        }
        //otherwise if our type value is contained in our set of bottom wall values
        else if (WallTypesHelper.wallBottom.Contains(typeAsInt))
        {
            //assign the wall bottom tile to our to-be-pained tile
            tile = wallBottom;
        }
        //if our tile is NOT null
        if (tile != null) 
        {
            //paint the tile
            PaintSingleTile(wallTilemap, tile, position);
        }
    }

    private void PaintSingleTile(Tilemap tilemap, TileBase tile, Vector2Int position)
    {
        //set tile position equal to its cell position on the tilemap
        var tilePosition = tilemap.WorldToCell((Vector3Int)position);
        //paint the specified tile in its specified position
        tilemap.SetTile(tilePosition, tile);
    }

    public void Clear() 
    {
        floorTilemap.ClearAllTiles();
        wallTilemap.ClearAllTiles();
    }

    internal void PaintSingleCornerWall(Vector2Int position, string binaryType)
    {
        //convert our binary type into a 32-bit integer
        int typeAsInt = Convert.ToInt32(binaryType, 2);
        //create an empty tile to assign our correct tile to
        TileBase tile = null;
        //if our type value is contained in our set of inner corner bottom left wall values
        if (WallTypesHelper.wallInnerCornerDownLeft.Contains(typeAsInt)) 
        {
            //assign that tile to our tile-to-be-painted
            tile = wallInnerCornerDownLeft;
        }
        //Otherwise if our type value is contained in our set of inner corner bottom right wall values
        else if (WallTypesHelper.wallInnerCornerDownRight.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallInnerCornerDownRight;
        }
        //Otherwise if our type value is contained in our set of diagonal corner bottom left wall values
        else if (WallTypesHelper.wallDiagonalCornerDownLeft.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallDiagonalCornerDownLeft;
        }
        //Otherwise if our type value is contained in our set of diagonal corner bottom right wall values
        else if (WallTypesHelper.wallDiagonalCornerDownRight.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallDiagonalCornerDownRight;
        }
        //Otherwise if our type value is contained in our set of diagonal corner top right wall values
        else if (WallTypesHelper.wallDiagonalCornerUpRight.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallDiagonalCornerUpRight;
        }
        //Otherwise if our type value is contained in our set of diagonal corner top left wall values
        else if (WallTypesHelper.wallDiagonalCornerUpLeft.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallDiagonalCornerUpLeft;
        }
        //Otherwise if our type value is contained in our set of full wall values
        else if (WallTypesHelper.wallFullEightDirections.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallFull;
        }
        //Otherwise if our type value is contained in our set of bottom wall values
        else if (WallTypesHelper.wallBottomEightDirections.Contains(typeAsInt))
        {
            //assign that tile to our tile-to-be-painted
            tile = wallBottom;
        }
        //if our tile is NOT null
        if (tile != null)
        {
            //paint the tile
            PaintSingleTile(wallTilemap, tile, position);
        }
    }
}
