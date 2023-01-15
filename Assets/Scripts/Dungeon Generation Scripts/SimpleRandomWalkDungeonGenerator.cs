using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Unity.VisualScripting;
using Random = UnityEngine.Random;

public class SimpleRandomWalkDungeonGenerator : AbstractDungeonGenerator
{
    protected SimpleRandomWalkSO _randomWalkParameters;

    [DoNotSerialize]
    public ValueInput randomWalkParameters;
    [DoNotSerialize]
    public ValueInput tilemapVisualizer;

    protected override void Definition()
    {
        //Making the ControlInput port visible, setting its key and running the anonymous action method to pass the flow to the outputTrigger port.
        inputTrigger = ControlInput("inputTrigger", (flow) => 
        {
            _randomWalkParameters = flow.GetValue<SimpleRandomWalkSO>(randomWalkParameters);
            _tilemapVisualizer = flow.GetValue<TilemapVisualizer>(tilemapVisualizer);
            GenerateDungeon();
            return outputTrigger; 
        });
        //Making the ControlOutput port visible and setting its key.
        outputTrigger = ControlOutput("outputTrigger");
        randomWalkParameters = ValueInput<SimpleRandomWalkSO>("RandomWalkSCriptableObject", null);
        tilemapVisualizer = ValueInput<TilemapVisualizer>("Tilemap Visualizer", null);

        //relations
        Requirement(randomWalkParameters, inputTrigger);
        Requirement(tilemapVisualizer, inputTrigger);
    }

    protected override void RunProceduralGeneration()
    {
        //run the random walk and store the floor positions in our hash set
        HashSet<Vector2Int> floorPositions = RunRandomWalk(_randomWalkParameters, startPosition);
        //paint walls by passing in our floor positions and tilemap visualizer reference
        WallGenerator.CreateWalls(floorPositions, _tilemapVisualizer);
    }

    protected HashSet<Vector2Int> RunRandomWalk(SimpleRandomWalkSO parameters, Vector2Int position)
    {
        //set our current position equal to our start position, since we are just starting
        var currentPosition = position;
        //create hash set for our floor positions
        HashSet<Vector2Int> floorPositions = new HashSet<Vector2Int>();
        //iterate over the number of iterations we have and run our random walk algorithm
        for (int i = 0; i < _randomWalkParameters.iterations; i++)
        {
            //run the simple random walk algorithm, passing in our current position and walk length
            var path = ProceduralGenerationAlgorithms.SimpleRandomWalk(currentPosition, _randomWalkParameters.walkLength);
            //Add this new path to our floor positions hash set without the duplicates
            floorPositions.UnionWith(path);
            //if start randomly each iteration is on select a random element
            //of our floor positions hash set to start our walk at again
            if (_randomWalkParameters.startRandomlyEachIteration) 
            {
                //select a random element of our floor positions hash set
                //and set our current position equal to it for a "random" iteration start
                currentPosition = floorPositions.ElementAt(Random.Range(0, floorPositions.Count));
            }
        }
        return floorPositions;
    }
}
