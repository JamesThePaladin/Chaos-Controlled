using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class CorridorsFirstDungeonGenerator : SimpleRandomWalkDungeonGenerator
{
    private int _corridorLength = 14, _corridorCount = 5; //length and number of corridors
    private float _roomPercent = 0.8f; //percent of rooms we create

    [DoNotSerialize]
    public ValueInput corridorLength;
    [DoNotSerialize]
    public ValueInput corridorCount;
    [DoNotSerialize]
    public ValueInput roomPercent;
    [DoNotSerialize]
    public ValueInput randomWalkParameters;
    [DoNotSerialize]
    public ValueInput tilemapVisualizer;

    protected override void Definition() 
    {
        //Making the ControlInput port visible, setting its key and running the anonymous action method to pass the flow to the outputTrigger port.
        inputTrigger = ControlInput("inputTrigger", (flow) => 
        {
            _corridorLength = flow.GetValue<int>(corridorLength);
            _corridorCount = flow.GetValue<int>(corridorCount);
            _roomPercent = flow.GetValue<float>(roomPercent);
            _randomWalkParameters = flow.GetValue<SimpleRandomWalkSO>(randomWalkParameters);
            _tilemapVisualizer = flow.GetValue<TilemapVisualizer>(tilemapVisualizer);
            GenerateDungeon();
            return outputTrigger; 
        });
        //Making the ControlOutput port visible and setting its key.
        outputTrigger = ControlOutput("outputTrigger");
        //visibility, defaults and labels
        corridorLength = ValueInput<int>("Corridor Length", 14);
        corridorCount = ValueInput<int>("Corridor Count", 5);
        roomPercent = ValueInput<float>("Room Percent", 0.8f);
        tilemapVisualizer = ValueInput<TilemapVisualizer>("Tilemap Visualizer", null);
        randomWalkParameters = ValueInput<SimpleRandomWalkSO>("RandomWalkSCriptableObject", null);
        //relations
        Requirement(corridorLength, inputTrigger);
        Requirement(corridorCount, inputTrigger);
        Requirement(roomPercent, inputTrigger);
        Requirement(randomWalkParameters, inputTrigger);
        Requirement(tilemapVisualizer, inputTrigger);
    }
    /// <summary>
    /// Override of RunProceduralGeneration that creates corridors first
    /// </summary>
    protected override void RunProceduralGeneration()
    {
        CorridorFirstGeneration();
    }

    /// <summary>
    /// Random Generation method that creates corridors connecting the rooms first
    /// </summary>
    private void CorridorFirstGeneration()
    {
        //create a new HashSet of floor positions
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        //create hash set for storing starting positions of potential rooms
        HashSet<Vector2Int> potentialRoomPositions = new HashSet<Vector2Int>();
        //create corridors
        CreateCorridors(floorPositions, potentialRoomPositions);
        //create rooms and add them to the room positions hash set
        HashSet<Vector2Int> roomPositions = CreateRooms(potentialRoomPositions);
        //find all the dead ends that were just generated and store them in a list
        List<Vector2Int> deadEnds = FindAllDeadEnds(floorPositions);
        //create rooms at these dead ends
        CreateRoomsAtDeadEnd(deadEnds, roomPositions);
        //add the room positions set to the floor positions, because they are now part of the floor
        floorPositions.UnionWith(roomPositions);
        //paint the floors in the corridor
        _tilemapVisualizer.PaintFloorTiles(floorPositions);
        //paint the walls in the corridor
        WallGenerator.CreateWalls(floorPositions, _tilemapVisualizer);
    }

    /// <summary>
    /// Creates rooms at dead end positions
    /// </summary>
    /// <param name="deadEnds"></param>
    /// <param name="roomFloors"></param>
    private void CreateRoomsAtDeadEnd(List<Vector2Int> deadEnds, HashSet<Vector2Int> roomFloors)
    {
        //for each position in the deadEnds list
        foreach (var position in deadEnds)
        {
            //if the room floors set DOES NOT contain this position
            if (roomFloors.Contains(position) == false) 
            {
                //make a room there using the SO parameters
                var room = RunRandomWalk(_randomWalkParameters, position);
                //add this room's floors to the roomFloors set
                roomFloors.UnionWith(room);
            }
        }
    }

    /// <summary>
    /// This method uses the cardinal directions list 
    /// to determine if a position in floorPositions is a dead end
    /// </summary>
    /// <param name="floorPositions"></param>
    /// <returns></returns>
    private List<Vector2Int> FindAllDeadEnds(HashSet<Vector2Int> floorPositions)
    {
        List<Vector2Int> deadEnds = new List<Vector2Int>();
        foreach (var position in floorPositions)
        {
            //variable for number of neighboring floor pieces
            int neighboursCount = 0;
            //for each cardinal direction
            foreach (var direction in Direction2D.cardinalDirectionsList)
            {
                //if a position in floor positions contains a neighbor in this direction
                if (floorPositions.Contains(position + direction)) 
                {
                    //increment the neighbor count
                    neighboursCount++;
                }
            }
            //if the amount of neighbors equal 1
            if (neighboursCount == 1) 
            {
                //it is a dead end, add it to the dead ends list
                deadEnds.Add(position);
            }
        }
        return deadEnds;
    }

    private HashSet<Vector2Int> CreateRooms(HashSet<Vector2Int> potentialRoomPositions)
    {
        //create a new hash set to return room positions
        HashSet<Vector2Int> roomPositions = new HashSet<Vector2Int>();
        //get the amount of rooms to create by multiplying our prp count 
        //by our room percentage and round to an int
        int roomsToCreateCount = Mathf.RoundToInt(potentialRoomPositions.Count * _roomPercent);
        //create a list of rooms to create out of our prp and order them using GUIDs for random selection
        List<Vector2Int> roomsToCreate = potentialRoomPositions.OrderBy(x => Guid.NewGuid()).Take(roomsToCreateCount).ToList();
        //for each room position in rooms to create
        foreach (var roomPosition in roomsToCreate)
        {
            //generate a random walk room at the room position according to parameters object
            var roomFloor = RunRandomWalk(_randomWalkParameters, roomPosition);
            //add to room positions set without duplicates
            roomPositions.UnionWith(roomFloor);
        }
        return roomPositions;
    }

    private void CreateCorridors(HashSet<Vector2Int> floorPositions, HashSet<Vector2Int> potentialRoomPositions)
    {
        //set our current position equal to our start position since we are starting here
        var currentPosition = startPosition;
        //add the start of each corridor to the set of potential room positions
        potentialRoomPositions.Add(currentPosition);
        //for the number of corridors specified
        for (int i = 0; i < _corridorCount; i++)
        {
            //generate a random walk corridor 
            var corridor = ProceduralGenerationAlgorithms.RandomWalkCorridor(currentPosition, _corridorLength);
            //set our current position to the last corridor in our count
            currentPosition = corridor[corridor.Count - 1];
            //add each end of a corridor to prp set
            potentialRoomPositions.Add(currentPosition);
            //add it to the floor positions without the duplicates
            floorPositions.UnionWith(corridor);
        }
    }
}
