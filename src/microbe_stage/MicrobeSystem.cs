﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

/// <summary>
///   Handles processing <see cref="Microbe"/>s in a multithreaded way
/// </summary>
public class MicrobeSystem
{
    private readonly List<Task> tasks = new();

    private readonly Node worldRoot;

    public MicrobeSystem(Node worldRoot)
    {
        this.worldRoot = worldRoot;
    }

    public void Process(float delta)
    {
        var microbes = worldRoot.GetTree().GetNodesInGroup(Constants.RUNNABLE_MICROBE_GROUP).Cast<Microbe>()
            .ToArray();

        // Start of async early processing
        var executor = TaskExecutor.Instance;

        for (int i = 0; i < microbes.Length; i += Constants.MICROBE_AI_OBJECTS_PER_TASK)
        {
            int start = i;

            var task = new Task(() =>
            {
                for (int a = start;
                     a < start + Constants.MICROBE_AI_OBJECTS_PER_TASK && a < microbes.Length;
                     ++a)
                {
                    var microbe = microbes[a];
                    microbe.ProcessEarlyAsync(delta);
                }
            });

            tasks.Add(task);
        }

        // Start and wait for tasks to finish
        executor.RunTasks(tasks);
        tasks.Clear();

        // And then process the synchronous part for all microbes
        foreach (var microbe in microbes)
        {
            microbe.NotifyExternalProcessingIsUsed();
            microbe.ProcessSync(delta);
        }
    }

    /// <summary>
    ///   Tries to find specified Species as close to the point as possible.
    /// </summary>
    /// <param name="position">Position to search around</param>
    /// <param name="species">What species to search for</param>
    /// <param name="searchRadius">How wide to search around the point</param>
    /// <returns>The nearest found point for the species or null</returns>

    public Vector3? FindSpeciesNearPoint(Vector3 position, Species species, float searchRadius = 200)
    {
        if (searchRadius < 1)
            throw new ArgumentException("searchRadius must be >= 1");

        Vector3? closestPoint = null;
        float nearestDistanceSquared = float.MaxValue;
        var microbes = worldRoot.GetTree().GetNodesInGroup(Constants.RUNNABLE_MICROBE_GROUP).Cast<Microbe>()
            .ToArray();

        foreach (var microbe in microbes)
        {
            if (Math.Abs(microbe.Translation.x - position.x) > searchRadius ||
                Math.Abs(microbe.Translation.y - position.y) > searchRadius)
            {
                continue;
            }

            var distance = (microbe.Translation - position).LengthSquared();

            if (distance < nearestDistanceSquared)
            {
                nearestDistanceSquared = distance;
                closestPoint = microbe.Translation;
            }
        }

        return closestPoint;
    }
}
