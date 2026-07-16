using UnityEngine;
using System.Collections.Generic;

public class DefaultFSM<T> where T : System.Enum
{
    public delegate void DelegateFunc();
    
    public class CState
    {
        public DelegateFunc m_OnEnterFunc = null;
        public DelegateFunc m_OnExitFunc = null;
        
        public virtual void Initialize(DelegateFunc func)
        {
            m_OnEnterFunc = new DelegateFunc(func);
        }
        
        public virtual void OnEnter()
        {
            if (m_OnEnterFunc != null)
                m_OnEnterFunc();
        }
        
        public virtual void OnUpdate() { }
        
        public virtual void OnExit()
        {
            if (m_OnExitFunc != null)
                m_OnExitFunc();
        }
    }

    protected Dictionary<T, CState> m_states = new Dictionary<T, CState>();
    protected CState m_curState = null;
    protected CState m_newState = null;
    protected T m_curStateType;   // 현재 상태의 enum 값 (세이브 등 외부에서 조회용)
    protected T m_newStateType;

    protected void AddState(T stateType, CState stateObj)
    {
        m_states[stateType] = stateObj;
    }

    public void InitializeState(T stateType, DelegateFunc enterFunc)
    {
        if (m_states.ContainsKey(stateType))
        {
            m_states[stateType].Initialize(enterFunc);
        }
    }

    public void SetState(T stateType)
    {
        if (m_states.ContainsKey(stateType))
        {
            m_newState = m_states[stateType];
            m_newStateType = stateType;
        }
    }

    public virtual void OnUpdate()
    {
        if (m_newState != null)
        {
            if (m_curState != null)
                m_curState.OnExit();

            m_curState = m_newState;
            m_curStateType = m_newStateType;
            m_newState = null;
            m_curState.OnEnter();
        }
        
        if (m_curState != null)
        {
            m_curState.OnUpdate();
        }
    }

    public bool IsCurState(T stateType)
    {
        if (m_curState == null || !m_states.ContainsKey(stateType))
            return false;
        
        return m_curState == m_states[stateType];
    }
    
    public CState GetCurState()
    {
        return m_curState;
    }

    public T GetCurStateType()
    {
        return m_curStateType;
    }
    
    public void SetNoneState()
    {
        m_newState = null;
        m_curState = null;
    }
    
    public bool IsNoneState() 
    { 
        return m_curState == null; 
    }
}
