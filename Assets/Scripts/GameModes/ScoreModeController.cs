using Mirror;
using UnityEngine;

public class ScoreModeController : GameModeController
{
    [Header("Score end game")]
    public int endGameScore = 10;
    public override GameModeType Type => GameModeType.ScoreCompete;

    public override bool FinishPredicate()
    {
        var maxScore = 0f;

        foreach (var conn in NetworkServer.connections.Values)
            if (conn != null && conn.identity)
                maxScore = Mathf.Max(maxScore, EventManager.GetScore(conn.identity.netId));

        return maxScore >= endGameScore;
    }
    public override void Activate()
    {
        base.Activate();

    }
}
