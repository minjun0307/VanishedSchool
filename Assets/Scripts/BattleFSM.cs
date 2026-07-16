using UnityEngine;

public enum BattleState
{
    Ready,
    Wave,
    Game,
    Result
}

public class BattleFSM : DefaultFSM<BattleState>
{
    public BattleFSM()
    {
        AddState(BattleState.Ready, new CState());
        AddState(BattleState.Wave, new CState());
        AddState(BattleState.Game, new CState());
        AddState(BattleState.Result, new CState());
    }

    public void Initialize(DelegateFunc kReady, DelegateFunc kGame, DelegateFunc kResult, DelegateFunc kWave = null)
    {
        InitializeState(BattleState.Ready, kReady);
        InitializeState(BattleState.Game, kGame);
        InitializeState(BattleState.Result, kResult);
        
        if (kWave != null)
            InitializeState(BattleState.Wave, kWave);
    }

    // Backward-compatibility wrappers
    public void SetReadyState() { SetState(BattleState.Ready); }
    public void SetWaveState() { SetState(BattleState.Wave); }
    public void SetGameState() { SetState(BattleState.Game); }
    public void SetResultState() { SetState(BattleState.Result); }

    public bool IsGameState() { return IsCurState(BattleState.Game); }
    public bool IsResultState() { return IsCurState(BattleState.Result); }
}
