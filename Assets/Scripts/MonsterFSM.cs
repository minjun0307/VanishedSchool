using UnityEngine;

public enum MonsterState
{
    Ready,
    Patrol,
    Alert,
    Chase
}

public class MonsterFSM : DefaultFSM<MonsterState>
{
    public MonsterFSM()
    {
        AddState(MonsterState.Ready, new CState());
        AddState(MonsterState.Patrol, new CState());
        AddState(MonsterState.Alert, new CState());
        AddState(MonsterState.Chase, new CState());
    }

    public void Initialize(DelegateFunc kReady, DelegateFunc kAlert, DelegateFunc kChase, DelegateFunc kPatrol = null)
    {
        InitializeState(MonsterState.Ready, kReady);
        InitializeState(MonsterState.Alert, kAlert);
        InitializeState(MonsterState.Chase, kChase);
        
        if (kPatrol != null)
            InitializeState(MonsterState.Patrol, kPatrol);
    }

    // Backward-compatibility wrappers
    public void SetReadyState() { SetState(MonsterState.Ready); }
    public void SetPatrolState() { SetState(MonsterState.Patrol); }
    public void SetAlertState() { SetState(MonsterState.Alert); }
    public void SetChasingState() { SetState(MonsterState.Chase); }

    public bool IsReadyState() { return IsCurState(MonsterState.Ready); }
    public bool IsPatrolState() { return IsCurState(MonsterState.Patrol); }
    public bool IsAlertState() { return IsCurState(MonsterState.Alert); }
    public bool IsChaseState() { return IsCurState(MonsterState.Chase); }
}
