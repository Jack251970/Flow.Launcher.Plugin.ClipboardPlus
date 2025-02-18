﻿// Copyright (c) 2025 Jack251970
// Licensed under the Apache License. See the LICENSE.

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public class ScoreHelper
{
    public int CurrentScore { get; private set; } = 1;

    private readonly int ScoreInterval;

    public ScoreHelper(int scoreInterval)
    {
        ScoreInterval = scoreInterval;
    }

    public void Reset()
    {
        CurrentScore = 1;
    }

    public void Add()
    {
        CurrentScore += ScoreInterval;
    }

    public void Subtract()
    {
        CurrentScore -= ScoreInterval;
    }

    public void Max(LinkedList<ClipboardDataPair> recordsList)
    {
        CurrentScore = recordsList.Max(r => r.ClipboardData.InitScore);
    }
}
