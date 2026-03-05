using System;
using System.Collections.Generic;
using UnityEngine;

public enum EnemyType
{
    Earth,
    Fire,
    Water
}


[Serializable]
public class WaveJsonData
{
    public string waveName;
    public bool runGroupsSequentially = true;
    public float postWaveDelay = 2f;
    public bool requireKillAllToComplete = true;
    public List<WaveGroupJson> groups = new List<WaveGroupJson>();
}

[Serializable]
public class WaveGroupJson
{
    public string enemyType;
    public int count = 5;
    public float interval = 0.5f;
    public float startDelay = 0f;
}