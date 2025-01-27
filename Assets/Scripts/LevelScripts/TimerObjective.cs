﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class TimerObjective : TimedObjective { 
    /* Win condition: Reach a score of 10
     * Loss condition: Run out of time */

    public static int winningScore = 5;
    public const string type = "Timer";

    private const float TIMERBAR_WIDTH = 200;

    public TimerObjective() : base() {
        // Nothing (else) to do
	}

    //make sure there are enough point collectibles spawned to finish this objective
    public override int NumPointItems { get { return winningScore / PointCollectible.points + 1; } }

    public override GroundPathEnemy.Behavior EnemyBehaviorOnComplete { get { return GroundPathEnemy.Behavior.NORMAL; } }

    public override string Type { get { return type; } }

    public override bool ObjectiveComplete () {
        if (PersistentPlayerSettings.settings.levelScore >= winningScore) {
            return true;
        }
        return false;
    }

    public override bool ObjectiveFailed () {
        UpdateTimer();

        if (timeRemaining <= 0.0f) {
            return true;
        }
        return false;
    }

    public override string ToString()
    {
        string currentGoal = PersistentPlayerSettings.settings.levelScore < winningScore ?
                             "Collect " + winningScore + " gold " + "(" + (winningScore - PersistentPlayerSettings.settings.levelScore) + " remaining)":
                             RETURN_TO_SHIP;
        return currentGoal + "\n Time remaining: " + (int)timeRemaining;
    }
}
