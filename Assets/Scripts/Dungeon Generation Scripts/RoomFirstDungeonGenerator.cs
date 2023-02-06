using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

public class RoomFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    //generation parameters
    private int _minRoomWidth = 4, _minRoomHeight = 4;
    private int _dungeonWidth = 20, _dungeonHeight = 20;
    private int _offset = 1;
    private bool _usingRandomWalkRooms = false;

    //node ports
    [DoNotSerialize]
    public ValueInput minRoomWidth;
    [DoNotSerialize]
    public ValueInput minRoomHeight;
    [DoNotSerialize]
    public ValueInput dungeonWidth;
    [DoNotSerialize]
    public ValueInput dungeonHeight;
    [DoNotSerialize]
    public ValueInput offset;
    [DoNotSerialize]
    public ValueInput usingRandomWalkRooms;

    protected override void Definition()
    {
        //Making the ControlInput port visible, setting its key and running the anonymous action method to pass the flow to the outputTrigger port.
        inputTrigger = ControlInput("inputTrigger", (flow) =>
        {
            _minRoomWidth = flow.GetValue<int>(minRoomWidth);
            _minRoomHeight = flow.GetValue<int>(minRoomHeight);
            _dungeonWidth = flow.GetValue<int>(dungeonWidth);
            _dungeonHeight = flow.GetValue<int>(dungeonHeight);
            _offset = flow.GetValue<int>(offset);
            _randomWalkParameters = flow.GetValue<SimpleRandomWalkSO>(randomWalkParameters);
            _usingRandomWalkRooms = flow.GetValue<bool>(usingRandomWalkRooms);
            _tilemapVisualizer = flow.GetValue<TilemapVisualizer>(tilemapVisualizer);
            GenerateDungeon();
            return outputTrigger; 
        });
        //Making the ControlOutput port visible and setting its key.
        outputTrigger = ControlOutput("outputTrigger");

        //Making Input ports visible, setting labels and defaults
        minRoomWidth = ValueInput<int>("MinRoomWidth", 4);
        minRoomHeight = ValueInput<int>("MinRoomHeight", 4);
        dungeonWidth = ValueInput<int>("DungeonWidth", 20);
        dungeonHeight = ValueInput<int>("DungeonHeight", 20);
        offset = ValueInput<int>("Offset", 1);
        tilemapVisualizer = ValueInput<TilemapVisualizer>("Tilemap Visualizer", null);
        randomWalkParameters = ValueInput<SimpleRandomWalkSO>("RandomWalkScriptableObject", null);
        usingRandomWalkRooms = ValueInput<bool>("UseRandomWalkRooms", false);

        //relations
        Requirement(minRoomWidth, inputTrigger);
        Requirement(minRoomHeight, inputTrigger);
        Requirement(dungeonWidth, inputTrigger);
        Requirement(dungeonHeight, inputTrigger);
        Requirement(offset, inputTrigger);
        Requirement(randomWalkParameters, inputTrigger);
        Requirement(usingRandomWalkRooms, inputTrigger);
        Requirement(tilemapVisualizer, inputTrigger);
        Succession(inputTrigger, outputTrigger);
    }

    protected override void RunProceduralGeneration()
    {
        CreateRooms();
    }

    private void CreateRooms()
    {
        //create our dungeon to begin BSP generation
        var roomsList = ProceduralGenerationAlgorithms.BinarySpacePartitioning(new BoundsInt((Vector3Int)startPosition, 
            new Vector3Int(_dungeonWidth, _dungeonHeight, 0)), _minRoomWidth, _minRoomHeight);

        //create a new hash set to store our floor positions
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        //if we are using random walk rooms
        if (_usingRandomWalkRooms) 
        {
            //create random walk rooms and add them to our floor set
            floor = CreateRandomWalkRooms(roomsList);
        }
        else
        {
            //create simple square rooms and add them to our floor set
            floor = CreateSimpleRooms(roomsList); 
        }
        //create a list to store room centers for corridor connection later
        List<Vector2Int> roomCenters = new List<Vector2Int>();
        //for each room in the list of rooms
        foreach (var room in roomsList)
        {
            //add their centers to the room centers list and convert them to Vector2Int
            roomCenters.Add((Vector2Int)Vector3Int.RoundToInt(room.center));
        }
        //create hash set for corridors
        HashSet<Vector2Int> corridors = ConnectRooms(roomCenters);
        //add the corridors to the floor set since they are now floors
        floor.UnionWith(corridors);
        //have the tilemapvisualizer paint it for view in editor
        _tilemapVisualizer.PaintFloorTiles(floor);
        //pass our wall generator our floors and tilemap visualizer
        WallGenerator.CreateWalls(floor, _tilemapVisualizer);
    }

    private HashSet<Vector2Int> CreateRandomWalkRooms(List<BoundsInt> roomsList)
    {
        //hash set for room floor return
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        //iterate through every room in room list
        for (int i = 0; i < roomsList.Count; i++)
        {
            //get the bounds for each room in the list
            var roomBounds = roomsList[i];
            //find its center
            var roomCenter = new Vector2Int(Mathf.RoundToInt(roomBounds.center.x), Mathf.RoundToInt(roomBounds.center.y));
            //create a room at the center according to the random walk scriptable object selected
            var roomFloor = RunRandomWalk(_randomWalkParameters, roomCenter);
            //for each floor position in our room floor set
            foreach (var position in roomFloor)
            {
                //if it is within bounds and offset
                if (position.x >= (roomBounds.xMin + _offset) && position.x <= (roomBounds.xMax - _offset)
                    && position.y >= (roomBounds.yMin - _offset) && position.y <= (roomBounds.yMax - _offset)) 
                {
                    //add it to our floor set
                    floor.Add(position);
                }
            }
        }
        return floor;
    }

    private HashSet<Vector2Int> ConnectRooms(List<Vector2Int> roomCenters)
    {
        //create a hash set to return for later floor union
        HashSet<Vector2Int> corridors = new HashSet<Vector2Int>();
        //select a random room center from our list
        var currentRoomCenter = roomCenters[Random.Range(0, roomCenters.Count)];
        //then remove it from the list
        roomCenters.Remove(currentRoomCenter);
        //while there are still centers in our room center list
        while (roomCenters.Count > 0) 
        {
            //find the closest other room center 
            Vector2Int closest = FindClosestPointTo(currentRoomCenter, roomCenters);
            //remove the closest from the list so we dont find it again on accident
            roomCenters.Remove(closest);
            //create a new set of corridors
            HashSet<Vector2Int> newCorridor = CreateCorridor(currentRoomCenter, closest);
            //set our current room center to the one thats currently stored in closest
            currentRoomCenter = closest;
            //union our new corridors set with our corridors set to remove duplicates before return
            corridors.UnionWith(newCorridor);
        }
        return corridors;
    }

    private HashSet<Vector2Int> CreateCorridor(Vector2Int currentRoomCenter, Vector2Int destination)
    {
        //create a hash set to define corridors
        HashSet<Vector2Int> corridor = new HashSet<Vector2Int>();
        //set the start position for our corridor
        var position = currentRoomCenter;
        //add our current position to the corridor
        corridor.Add(position);
        //while our current position's y value IS NOT equal to our destination y
        while (position.y != destination.y)
        {
            //and if our destination's y is greater than our current position's y
            if (destination.y > position.y)
            {
                //our destination is above us, move up
                position += Vector2Int.up;
            }
            //otherwise if our destination's y is less than our current position's y
            else if (destination.y < position.y)
            {
                position += Vector2Int.down;
            }
            //add the new position to the corridor
            corridor.Add(position);
        }
        //while our current position's x value IS NOT equal to our destination x
        while (position.x != destination.x)
        {
            //and if our destination's x is greater than our current position's x
            if (destination.x > position.x)
            {
                //it is to our right so move to the right
                position += Vector2Int.right;
            }
            //otherwise if our destination's x is less than our current position's x
            else if (destination.x < position.x) 
            {
                //it is to our left so move to the left
                position += Vector2Int.left;
            }
            corridor.Add(position);
        }
        return corridor;
    }

    private Vector2Int FindClosestPointTo(Vector2Int currentRoomCenter, List<Vector2Int> roomCenters)
    {
        //create a new vector for our closest center
        Vector2Int closest = Vector2Int.zero;
        //float for our current center's distance to the closest one
        float distance = float.MaxValue;
        //for each center in our room centers list
        foreach (var position in roomCenters)
        {
            //get the distance between our current center and all other centers
            float currentDistance = Vector2.Distance(position, currentRoomCenter);
            //if the current distance is less the our max distance
            if (currentDistance < distance) 
            {
                //set max distance to current distance
                distance = currentDistance;
                //set our closest to the position that we just found
                closest = position;
            }
        }
        return closest;
    }

    private HashSet<Vector2Int> CreateSimpleRooms(List<BoundsInt> roomsList)
    {
        //create new hash set for our floors inside the rooms
        HashSet<Vector2Int> floor = new HashSet<Vector2Int>();
        //for each room in roomslist
        foreach (var room in roomsList)
        {
            //iterate through each column minus the offset
            for (int col = _offset; col < room.size.x - _offset; col++)
            {
                //iterate through each row minus the offset
                for (int row = _offset; row < room.size.y - _offset; row++)
                {
                    //create a new position at each point in the room
                    Vector2Int position = (Vector2Int)room.min + new Vector2Int(col, row);
                    //add it to our floor set
                    floor.Add(position);
                }
            }
        }
        return floor;
    }
}
