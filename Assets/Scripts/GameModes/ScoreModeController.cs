using Mirror;
using UnityEngine;

public class ScoreModeController : GameModeController
{
    public int endGameScore = 10;
    public override GameModeType Type => GameModeType.ScoreCompete;

    public override bool FinishPredicate()
    {
        var maxScore = 0f;

        foreach (var conn in NetworkServer.connections.Values)
            maxScore = Mathf.Max(EventManager.GetScore(conn.identity.netId));

        return maxScore >= endGameScore;
    }
    public override void Activate()
    {
        base.Activate();

    }
}
